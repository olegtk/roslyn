﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal class CSharpIfSnippetProvider : AbstractIfSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpIfSnippetProvider()
        {
        }

        protected override void GetIfStatementConditionAndCursorPosition(SyntaxNode node, out SyntaxNode condition, out int cursorPositionNode)
        {
            var ifStatement = (IfStatementSyntax)node;
            condition = ifStatement.Condition;
            var blockStatement = (BlockSyntax)ifStatement.Statement;
            cursorPositionNode = blockStatement.CloseBraceToken.SpanStart;
        }

        protected override string? GetIndentation(Document document, SyntaxNode node, SyntaxFormattingOptions syntaxFormattingOptions, CancellationToken cancellationToken)
        {
            var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
            var ifStatementSyntax = (IfStatementSyntax)node;
            var openBraceLine = parsedDocument.Text.Lines.GetLineFromPosition(ifStatementSyntax.Statement.SpanStart).LineNumber;

            var indentationOptions = new IndentationOptions(syntaxFormattingOptions);
            var newLine = indentationOptions.FormattingOptions.NewLine;

            var indentationService = parsedDocument.LanguageServices.GetRequiredService<IIndentationService>();
            var indentation = indentationService.GetIndentation(parsedDocument, openBraceLine + 1, indentationOptions, cancellationToken);
            var newIndentation = new IndentationResult(indentation.BasePosition, indentation.Offset + syntaxFormattingOptions.TabSize);
            return newIndentation.GetIndentationString(parsedDocument.Text, syntaxFormattingOptions.UseTabs, syntaxFormattingOptions.TabSize) + newLine;
        }

        protected override async Task<Document> AddIndentationToDocumentAsync(Document document, int position, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nearestStatement = FindAddedSnippetSyntaxNode(root, position, syntaxFacts);
            var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions: null, cancellationToken).ConfigureAwait(false);

            var indentationString = GetIndentation(document, nearestStatement, syntaxFormattingOptions, cancellationToken);

            if (indentationString is null)
            {
                return document;
            }

            var ifStatementSyntax = (IfStatementSyntax)nearestStatement;
            var blockStatement = (BlockSyntax)ifStatementSyntax.Statement;
            blockStatement = blockStatement.WithCloseBraceToken(blockStatement.CloseBraceToken.WithPrependedLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, indentationString)));
            var newIfStatementSyntax = ifStatementSyntax.ReplaceNode(ifStatementSyntax.Statement, blockStatement);

            var newRoot = root.ReplaceNode(ifStatementSyntax, newIfStatementSyntax);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
