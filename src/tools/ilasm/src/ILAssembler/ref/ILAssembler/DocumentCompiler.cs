// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILAssembler
{
    public sealed class DocumentCompiler
    {
        public (System.Collections.Immutable.ImmutableArray<Diagnostic>, CompilationResult?) Compile(
            System.Collections.Immutable.ImmutableArray<SourceText> documents,
            System.Func<string, SourceText> includedDocumentLoader,
            System.Func<string, byte[]> resourceLocator,
            Options options) { throw null; }

        public (System.Collections.Immutable.ImmutableArray<Diagnostic>, CompilationResult?) Compile(
            SourceText document,
            System.Func<string, SourceText> includedDocumentLoader,
            System.Func<string, byte[]> resourceLocator,
            Options options) { throw null; }
    }
}
