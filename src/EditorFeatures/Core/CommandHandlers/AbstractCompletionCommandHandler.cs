// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractCompletionCommandHandler :
        ForegroundThreadAffinitizedObject,
        IChainedCommandHandler<TabKeyCommandArgs>,
        IChainedCommandHandler<ToggleCompletionModeCommandArgs>,
        IChainedCommandHandler<TypeCharCommandArgs>,
        IChainedCommandHandler<ReturnKeyCommandArgs>,
        IChainedCommandHandler<InvokeCompletionListCommandArgs>,
        IChainedCommandHandler<CommitUniqueCompletionListItemCommandArgs>,
        IChainedCommandHandler<PageUpKeyCommandArgs>,
        IChainedCommandHandler<PageDownKeyCommandArgs>,
        IChainedCommandHandler<CutCommandArgs>,
        IChainedCommandHandler<PasteCommandArgs>,
        IChainedCommandHandler<BackspaceKeyCommandArgs>,
        IChainedCommandHandler<InsertSnippetCommandArgs>,
        IChainedCommandHandler<SurroundWithCommandArgs>,
        IChainedCommandHandler<AutomaticLineEnderCommandArgs>,
        IChainedCommandHandler<SaveCommandArgs>,
        IChainedCommandHandler<DeleteKeyCommandArgs>,
        IChainedCommandHandler<SelectAllCommandArgs>
    {
        private readonly IAsyncCompletionService _completionService;

        public bool InterestedInReadOnlyBuffer => true;

        protected AbstractCompletionCommandHandler(IAsyncCompletionService completionService)
        {
            _completionService = completionService;
        }

        private bool TryGetController(CommandArgs args, out Controller controller)
        {
            return _completionService.TryGetController(args.TextView, args.SubjectBuffer, out controller);
        }

        private bool TryGetControllerCommandHandler<TCommandArgs>(TCommandArgs args, out IChainedCommandHandler<TCommandArgs> commandHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();
            if (!TryGetController(args, out var controller))
            {
                commandHandler = null;
                return false;
            }

            commandHandler = (IChainedCommandHandler<TCommandArgs>)controller;
            return true;
        }

        private CommandState GetCommandStateWorker<TCommandArgs>(
            TCommandArgs args,
            Func<CommandState> nextHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();
            if (TryGetControllerCommandHandler(args, out var commandHandler))
            {
                return commandHandler.GetCommandState(args, nextHandler);
            }

            return nextHandler();
        }

        private bool ExecuteCommandWorker<TCommandArgs>(
            TCommandArgs args,
            Func<bool> nextHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();
            if (TryGetControllerCommandHandler(args, out var commandHandler))
            {
                return commandHandler.ExecuteCommand(args, nextHandler);
            }

            return nextHandler();
        }

        CommandState IChainedCommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<ToggleCompletionModeCommandArgs>.GetCommandState(ToggleCompletionModeCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<ToggleCompletionModeCommandArgs>.ExecuteCommand(ToggleCompletionModeCommandArgs args, System.Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, System.Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args, System.Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<InvokeCompletionListCommandArgs>.GetCommandState(InvokeCompletionListCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<InvokeCompletionListCommandArgs>.ExecuteCommand(InvokeCompletionListCommandArgs args, System.Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<PageUpKeyCommandArgs>.GetCommandState(PageUpKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<PageUpKeyCommandArgs>.ExecuteCommand(PageUpKeyCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<PageDownKeyCommandArgs>.GetCommandState(PageDownKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<PageDownKeyCommandArgs>.ExecuteCommand(PageDownKeyCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<CommitUniqueCompletionListItemCommandArgs>.GetCommandState(CommitUniqueCompletionListItemCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<CommitUniqueCompletionListItemCommandArgs>.ExecuteCommand(CommitUniqueCompletionListItemCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<InsertSnippetCommandArgs>.GetCommandState(InsertSnippetCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<InsertSnippetCommandArgs>.ExecuteCommand(InsertSnippetCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        CommandState IChainedCommandHandler<SurroundWithCommandArgs>.GetCommandState(SurroundWithCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public CommandState GetCommandState(AutomaticLineEnderCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public bool ExecuteCommand(AutomaticLineEnderCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        bool IChainedCommandHandler<SurroundWithCommandArgs>.ExecuteCommand(SurroundWithCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        internal bool TryHandleEscapeKey(EscapeKeyCommandArgs commandArgs)
        {
            if (!TryGetController(commandArgs, out var controller))
            {
                return false;
            }

            return controller.TryHandleEscapeKey();
        }

        internal bool TryHandleUpKey(UpKeyCommandArgs commandArgs)
        {
            if (!TryGetController(commandArgs, out var controller))
            {
                return false;
            }

            return controller.TryHandleUpKey();
        }

        internal bool TryHandleDownKey(DownKeyCommandArgs commandArgs)
        {
            if (!TryGetController(commandArgs, out var controller))
            {
                return false;
            }

            return controller.TryHandleDownKey();
        }

        public CommandState GetCommandState(SaveCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public bool ExecuteCommand(SaveCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        public CommandState GetCommandState(DeleteKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public bool ExecuteCommand(DeleteKeyCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }

        public CommandState GetCommandState(SelectAllCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public bool ExecuteCommand(SelectAllCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args, nextHandler);
        }
    }
}
