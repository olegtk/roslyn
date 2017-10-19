// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState IChainedCommandHandler<SaveCommandArgs>.GetCommandState(SaveCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        bool IChainedCommandHandler<SaveCommandArgs>.ExecuteCommand(SaveCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();

            if (sessionOpt != null)
            {
                DismissSessionIfActive();
            }

            return false;
        }
    }
}
