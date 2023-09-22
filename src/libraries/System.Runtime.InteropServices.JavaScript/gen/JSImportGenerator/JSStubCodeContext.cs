// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop.JavaScript
{
    internal abstract record JSStubCodeContext : StubCodeContext
    {
        public StubCodeContext _inner;
        public override bool SingleFrameSpansNativeContext => _inner.SingleFrameSpansNativeContext;

        public override bool AdditionalTemporaryStateLivesAcrossStages => _inner.AdditionalTemporaryStateLivesAcrossStages;

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            return _inner.GetIdentifiers(info);
        }
    }

    internal sealed record JSImportCodeContext : JSStubCodeContext
    {
        public JSImportCodeContext(JSImportData attributeData, StubCodeContext inner)
        {
            _inner = inner;
            Direction = MarshalDirection.ManagedToUnmanaged;
            AttributeData = attributeData;
            CodeEmitOptions = inner.CodeEmitOptions;
        }

        public JSImportData AttributeData { get; set; }
    }

    internal sealed record JSExportCodeContext : JSStubCodeContext
    {
        public JSExportCodeContext(JSExportData attributeData, StubCodeContext inner)
        {
            _inner = inner;
            Direction = MarshalDirection.UnmanagedToManaged;
            AttributeData = attributeData;
            CodeEmitOptions = inner.CodeEmitOptions;
        }

        public JSExportData AttributeData { get; set; }
    }
}
