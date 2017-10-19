﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState IChainedCommandHandler<SelectAllCommandArgs>.GetCommandState(SelectAllCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        bool IChainedCommandHandler<SelectAllCommandArgs>.ExecuteCommand(SelectAllCommandArgs args, Func<bool> nextHandler)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            return false;
        }
    }
}
