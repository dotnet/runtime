// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace Internal.IL
{
    public class LinkageMetadata
    {
        public LinkageMetadata(Utf8String symbol, bool isIndirection = false)
        {
            Symbol = symbol;
            IsIndirection = isIndirection;
        }

        // IsNull means there is no specific symbol name.
        public Utf8String Symbol { get; }
        public bool IsIndirection { get; }
    }

    public sealed class WasmLinkageMetadata : LinkageMetadata
    {
        public WasmLinkageMetadata(Utf8String importName, Utf8String importModule, bool isIndirection = false)
            : base(default, isIndirection)
        {
            ImportName = importName;
            ImportModule = importModule;
        }

        public Utf8String ImportName { get; }
        public Utf8String ImportModule { get; }
    }
}
