// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition.Hosting.Core;

namespace System.Composition.TypedParts
{
    internal sealed class ImportInfo
    {
        private readonly CompositionContract _exportKey;
        private readonly bool _allowDefault;

        public ImportInfo(CompositionContract exportKey, bool allowDefault)
        {
            _exportKey = exportKey;
            _allowDefault = allowDefault;
        }

        public bool AllowDefault { get { return _allowDefault; } }

        public CompositionContract Contract { get { return _exportKey; } }
    }
}
