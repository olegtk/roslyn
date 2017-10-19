// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        // Cut and Paste should always dismiss completion

        CommandState IChainedCommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        bool IChainedCommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            return false;
        }

        CommandState IChainedCommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        bool IChainedCommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            return false;
        }
    }
}
