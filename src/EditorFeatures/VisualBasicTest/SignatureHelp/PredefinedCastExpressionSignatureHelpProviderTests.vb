﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class PredefinedCastExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New PredefinedCastExpressionSignatureHelpProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForCBool() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = CBool($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"CBool({VBWorkspaceResources.expression}) As Boolean",
                                     String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Boolean"),
                                     VBWorkspaceResources.The_expression_to_be_evaluated_and_converted,
                                     currentParameterIndex:=0))
            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace
