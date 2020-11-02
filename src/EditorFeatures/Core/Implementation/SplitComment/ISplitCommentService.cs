﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
{
    internal interface ISplitCommentService : ILanguageService
    {
        string CommentStart { get; }

        bool IsAllowed(SyntaxNode root, SyntaxTrivia trivia);
    }
}
