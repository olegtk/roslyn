// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export]
    //[ExportCommandHandler(PredefinedCommandHandlerNames.Completion, ContentTypeNames.RoslynContentType)]
    [Export(typeof(ICommandHandler))]
    [Name(PredefinedCommandHandlerNames.Completion)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Order(After = PredefinedCommandHandlerNames.SignatureHelp,
           Before = PredefinedCommandHandlerNames.DocumentationComments)]
    internal sealed class CompletionCommandHandler : AbstractCompletionCommandHandler
    {
        [ImportingConstructor]
        public CompletionCommandHandler(IAsyncCompletionService completionService)
            : base(completionService)
        {
        }
    }
}
