﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseNullPropagation
{
    internal static class UseNullPropagationConstants
    {
        public const string WhenPartIsNullable = nameof(WhenPartIsNullable);
    }

    /// <summary>
    /// Looks for code snippets similar to <c>x == null ? null : x.Y()</c> and converts it to <c>x?.Y()</c>.  This form is also supported:
    /// <code>
    /// if (x != null)
    ///     x.Y();
    /// </code>
    /// </summary>
    internal abstract partial class AbstractUseNullPropagationDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TConditionalExpressionSyntax,
        TBinaryExpressionSyntax,
        TInvocationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TConditionalAccessExpressionSyntax,
        TElementAccessExpressionSyntax,
        TIfStatementSyntax,
        TExpressionStatementSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TConditionalAccessExpressionSyntax : TExpressionSyntax
        where TElementAccessExpressionSyntax : TExpressionSyntax
        where TIfStatementSyntax : TStatementSyntax
        where TExpressionStatementSyntax : TStatementSyntax
    {
        private static readonly ImmutableDictionary<string, string?> s_whenPartIsNullableProperties =
            ImmutableDictionary<string, string?>.Empty.Add(UseNullPropagationConstants.WhenPartIsNullable, "");

        protected AbstractUseNullPropagationDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseNullPropagationDiagnosticId,
                   EnforceOnBuildValues.UseNullPropagation,
                   CodeStyleOptions2.PreferNullPropagation,
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_null_propagation), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract bool ShouldAnalyze(Compilation compilation);

        protected abstract ISyntaxFacts GetSyntaxFacts();
        protected abstract bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol? expressionTypeOpt, CancellationToken cancellationToken);

        protected abstract bool TryAnalyzePatternCondition(
            ISyntaxFacts syntaxFacts, SyntaxNode conditionNode,
            [NotNullWhen(true)] out TExpressionSyntax? conditionPartToCheck, out bool isEquals);

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                if (!ShouldAnalyze(context.Compilation))
                    return;

                var expressionType = context.Compilation.ExpressionOfTType();

                var objectType = context.Compilation.GetSpecialType(SpecialType.System_Object);
                var referenceEqualsMethod = objectType?.GetMembers(nameof(ReferenceEquals))
                                                          .OfType<IMethodSymbol>()
                                                          .FirstOrDefault(m => m.DeclaredAccessibility == Accessibility.Public &&
                                                                               m.Parameters.Length == 2);

                var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
                context.RegisterSyntaxNodeAction(
                    context => AnalyzeTernaryConditionalExpression(context, expressionType, referenceEqualsMethod),
                    syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.TernaryConditionalExpression));
                context.RegisterSyntaxNodeAction(
                    context => AnalyzeIfStatement(context, referenceEqualsMethod),
                    syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.IfStatement));
            });

        }

        private void AnalyzeTernaryConditionalExpression(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? expressionType,
            IMethodSymbol? referenceEqualsMethod)
        {
            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;

            var option = context.GetAnalyzerOptions().PreferNullPropagation;
            if (!option.Value)
                return;

            var syntaxFacts = GetSyntaxFacts();
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out var condition, out var whenTrue, out var whenFalse);

            var conditionNode = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(condition);
            var whenTrueNode = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(whenTrue);
            var whenFalseNode = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(whenFalse);

            var conditionIsNegated = false;
            if (syntaxFacts.IsLogicalNotExpression(conditionNode))
            {
                conditionIsNegated = true;
                conditionNode = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(
                    syntaxFacts.GetOperandOfPrefixUnaryExpression(conditionNode));
            }

            if (!TryAnalyzeCondition(
                    context, syntaxFacts, referenceEqualsMethod, (TExpressionSyntax)conditionNode,
                    out var conditionPartToCheck, out var isEquals))
            {
                return;
            }

            if (conditionIsNegated)
            {
                isEquals = !isEquals;
            }

            // Needs to be of the form:
            //      x == null ? null : ...    or
            //      x != null ? ...  : null;
            if (isEquals && !syntaxFacts.IsNullLiteralExpression(whenTrueNode))
                return;

            if (!isEquals && !syntaxFacts.IsNullLiteralExpression(whenFalseNode))
                return;

            var whenPartToCheck = isEquals ? whenFalseNode : whenTrueNode;

            var semanticModel = context.SemanticModel;
            var whenPartMatch = GetWhenPartMatch(syntaxFacts, semanticModel, conditionPartToCheck, whenPartToCheck);
            if (whenPartMatch == null)
            {
                return;
            }

            // ?. is not available in expression-trees.  Disallow the fix in that case.

            var type = semanticModel.GetTypeInfo(conditionalExpression).Type;
            if (type?.IsValueType == true)
            {
                if (type is not INamedTypeSymbol namedType || namedType.ConstructedFrom.SpecialType != SpecialType.System_Nullable_T)
                {
                    // User has something like:  If(str is nothing, nothing, str.Length)
                    // In this case, converting to str?.Length changes the type of this from
                    // int to int?
                    return;
                }
                // But for a nullable type, such as  If(c is nothing, nothing, c.nullable)
                // converting to c?.nullable doesn't affect the type
            }

            if (IsInExpressionTree(semanticModel, conditionNode, expressionType, context.CancellationToken))
            {
                return;
            }

            var locations = ImmutableArray.Create(
                conditionalExpression.GetLocation(),
                conditionPartToCheck.GetLocation(),
                whenPartToCheck.GetLocation());

            var whenPartIsNullable = semanticModel.GetTypeInfo(whenPartMatch).Type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
            var properties = whenPartIsNullable
                ? s_whenPartIsNullableProperties
                : ImmutableDictionary<string, string?>.Empty;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                conditionalExpression.GetLocation(),
                option.Notification.Severity,
                locations,
                properties));
        }

        private bool TryAnalyzeCondition(
            SyntaxNodeAnalysisContext context,
            ISyntaxFacts syntaxFacts,
            IMethodSymbol? referenceEqualsMethod,
            TExpressionSyntax conditionNode,
            [NotNullWhen(true)] out TExpressionSyntax? conditionPartToCheck,
            out bool isEquals)
        {
            switch (conditionNode)
            {
                case TBinaryExpressionSyntax binaryExpression:
                    return TryAnalyzeBinaryExpressionCondition(
                        syntaxFacts, binaryExpression, out conditionPartToCheck, out isEquals);

                case TInvocationExpressionSyntax invocation:
                    return TryAnalyzeInvocationCondition(
                        context, syntaxFacts, referenceEqualsMethod, invocation,
                        out conditionPartToCheck, out isEquals);

                default:
                    return TryAnalyzePatternCondition(
                        syntaxFacts, conditionNode, out conditionPartToCheck, out isEquals);
            }
        }

        private static bool TryAnalyzeBinaryExpressionCondition(
            ISyntaxFacts syntaxFacts, TBinaryExpressionSyntax condition,
            [NotNullWhen(true)] out TExpressionSyntax? conditionPartToCheck, out bool isEquals)
        {
            var syntaxKinds = syntaxFacts.SyntaxKinds;
            isEquals = syntaxKinds.ReferenceEqualsExpression == condition.RawKind;
            var isNotEquals = syntaxKinds.ReferenceNotEqualsExpression == condition.RawKind;
            if (!isEquals && !isNotEquals)
            {
                conditionPartToCheck = null;
                return false;
            }
            else
            {
                syntaxFacts.GetPartsOfBinaryExpression(condition, out var conditionLeft, out var conditionRight);
                conditionPartToCheck = GetConditionPartToCheck(syntaxFacts, (TExpressionSyntax)conditionLeft, (TExpressionSyntax)conditionRight);
                return conditionPartToCheck != null;
            }
        }

        private static bool TryAnalyzeInvocationCondition(
            SyntaxNodeAnalysisContext context,
            ISyntaxFacts syntaxFacts,
            IMethodSymbol? referenceEqualsMethod,
            TInvocationExpressionSyntax invocation,
            [NotNullWhen(true)] out TExpressionSyntax? conditionPartToCheck,
            out bool isEquals)
        {
            conditionPartToCheck = null;
            isEquals = true;

            if (referenceEqualsMethod == null)
                return false;

            var expression = syntaxFacts.GetExpressionOfInvocationExpression(invocation);
            var nameNode = syntaxFacts.IsIdentifierName(expression)
                ? expression
                : syntaxFacts.IsSimpleMemberAccessExpression(expression)
                    ? syntaxFacts.GetNameOfMemberAccessExpression(expression)
                    : null;

            if (!syntaxFacts.IsIdentifierName(nameNode))
            {
                return false;
            }

            syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out _);
            if (!syntaxFacts.StringComparer.Equals(name, nameof(ReferenceEquals)))
            {
                return false;
            }

            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
            if (arguments.Count != 2)
            {
                return false;
            }

            var conditionLeft = (TExpressionSyntax)syntaxFacts.GetExpressionOfArgument(arguments[0]);
            var conditionRight = (TExpressionSyntax)syntaxFacts.GetExpressionOfArgument(arguments[1]);
            if (conditionLeft == null || conditionRight == null)
            {
                return false;
            }

            conditionPartToCheck = GetConditionPartToCheck(syntaxFacts, conditionLeft, conditionRight);
            if (conditionPartToCheck == null)
            {
                return false;
            }

            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
            return referenceEqualsMethod.Equals(symbol);
        }

        private static TExpressionSyntax? GetConditionPartToCheck(
            ISyntaxFacts syntaxFacts, TExpressionSyntax conditionLeft, TExpressionSyntax conditionRight)
        {
            var conditionLeftIsNull = syntaxFacts.IsNullLiteralExpression(conditionLeft);
            var conditionRightIsNull = syntaxFacts.IsNullLiteralExpression(conditionRight);

            if (conditionRightIsNull && conditionLeftIsNull)
            {
                // null == null    nothing to do here.
                return null;
            }

            if (!conditionRightIsNull && !conditionLeftIsNull)
            {
                return null;
            }

            return conditionRightIsNull ? conditionLeft : conditionRight;
        }

        internal static TExpressionSyntax? GetWhenPartMatch(
            ISyntaxFacts syntaxFacts, SemanticModel semanticModel, TExpressionSyntax expressionToMatch, TExpressionSyntax whenPart)
        {
            expressionToMatch = RemoveObjectCastIfAny(syntaxFacts, semanticModel, expressionToMatch);
            var current = whenPart;
            while (true)
            {
                var unwrapped = Unwrap(syntaxFacts, current);
                if (unwrapped == null)
                    return null;

                if (current is TMemberAccessExpressionSyntax or TElementAccessExpressionSyntax &&
                    syntaxFacts.AreEquivalent(unwrapped, expressionToMatch))
                {
                    return unwrapped;
                }

                current = unwrapped;
            }
        }

        private static TExpressionSyntax RemoveObjectCastIfAny(ISyntaxFacts syntaxFacts, SemanticModel semanticModel, TExpressionSyntax node)
        {
            if (syntaxFacts.IsCastExpression(node))
            {
                syntaxFacts.GetPartsOfCastExpression(node, out var type, out var expression);
                var typeSymbol = semanticModel.GetTypeInfo(type).Type;

                if (typeSymbol?.SpecialType == SpecialType.System_Object)
                    return (TExpressionSyntax)expression;
            }

            return node;
        }

        private static TExpressionSyntax? Unwrap(ISyntaxFacts syntaxFacts, TExpressionSyntax node)
        {
            node = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(node);

            if (node is TInvocationExpressionSyntax invocation)
                return (TExpressionSyntax)syntaxFacts.GetExpressionOfInvocationExpression(invocation);

            if (node is TMemberAccessExpressionSyntax memberAccess)
                return (TExpressionSyntax?)syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess);

            if (node is TConditionalAccessExpressionSyntax conditionalAccess)
                return (TExpressionSyntax)syntaxFacts.GetExpressionOfConditionalAccessExpression(conditionalAccess);

            if (node is TElementAccessExpressionSyntax elementAccess)
                return (TExpressionSyntax?)syntaxFacts.GetExpressionOfElementAccessExpression(elementAccess);

            return null;
        }
    }
}
