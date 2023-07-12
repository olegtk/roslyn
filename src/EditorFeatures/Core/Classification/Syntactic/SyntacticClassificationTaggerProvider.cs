﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TagType(typeof(IClassificationTag))]
    [method: ImportingConstructor]
    [method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    internal partial class SyntacticClassificationTaggerProvider(
        IThreadingContext threadingContext,
        SyntacticClassificationTypeMap typeMap,
        IGlobalOptionService globalOptions,
        IAsynchronousOperationListenerProvider listenerProvider) : ITaggerProvider
    {
        private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.Classification);
        private readonly IThreadingContext _threadingContext = threadingContext;
        private readonly SyntacticClassificationTypeMap _typeMap = typeMap;
        private readonly IGlobalOptionService _globalOptions = globalOptions;

        private readonly ConditionalWeakTable<ITextBuffer, TagComputer> _tagComputers = new();

        public ITagger<T>? CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            _threadingContext.ThrowIfNotOnUIThread();
            if (!_globalOptions.GetOption(SyntacticColorizerOptionsStorage.SyntacticColorizer))
                return null;

            if (!_tagComputers.TryGetValue(buffer, out var tagComputer))
            {
                tagComputer = new TagComputer(this, (ITextBuffer2)buffer, _listener, _typeMap, TaggerDelay.NearImmediate.ComputeTimeDelay());
                _tagComputers.Add(buffer, tagComputer);
            }

            tagComputer.IncrementReferenceCount();

            var tagger = new Tagger(tagComputer);

            if (tagger is ITagger<T> typedTagger)
                return typedTagger;

            // Oops, we can't actually return this tagger, so just clean up
            tagger.Dispose();
            return null;
        }

        private void DisconnectTagComputer(ITextBuffer buffer)
            => _tagComputers.Remove(buffer);
    }
}
