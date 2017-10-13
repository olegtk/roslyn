// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BlockCommentEditing
{
    internal abstract class AbstractBlockCommentEditingCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        protected AbstractBlockCommentEditingCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);

            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public bool InterestedInReadOnlyBuffer => false;

        public CommandState GetCommandState(ReturnKeyCommandArgs args) => CommandState.CommandIsUnavailable;

        public bool ExecuteCommand(ReturnKeyCommandArgs args) => TryHandleReturnKey(args);

        private bool TryHandleReturnKey(ReturnKeyCommandArgs args)
        {
            var subjectBuffer = args.SubjectBuffer;
            var textView = args.TextView;

            if (!subjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString))
            {
                return false;
            }

            var caretPosition = textView.GetCaretPoint(subjectBuffer);
            if (caretPosition == null)
            {
                return false;
            }

            var exteriorText = GetExteriorTextForNextLine(caretPosition.Value);
            if (exteriorText == null)
            {
                return false;
            }

            using (var transaction = _undoHistoryRegistry.GetHistory(args.TextView.TextBuffer).CreateTransaction(EditorFeaturesResources.Insert_new_line))
            {
                var editorOperations = _editorOperationsFactoryService.GetEditorOperations(args.TextView);

                editorOperations.InsertNewLine();
                editorOperations.InsertText(exteriorText);

                transaction.Complete();
                return true;
            }
        }

        protected abstract string GetExteriorTextForNextLine(SnapshotPoint caretPosition);
    }
}
