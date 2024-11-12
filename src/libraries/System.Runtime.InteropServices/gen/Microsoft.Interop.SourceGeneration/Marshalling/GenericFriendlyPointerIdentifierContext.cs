// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Interop
{
    internal sealed record GenericFriendlyPointerIdentifierContext : StubIdentifierContext
    {
        private readonly StubIdentifierContext _innerContext;
        private readonly TypePositionInfo _adaptedInfo;
        private readonly string _nativeIdentifier;

        public GenericFriendlyPointerIdentifierContext(StubIdentifierContext inner, TypePositionInfo adaptedInfo, string baseIdentifier)
        {
            _innerContext = inner;
            _adaptedInfo = adaptedInfo;
            _nativeIdentifier = baseIdentifier + "_exactType";
            CurrentStage = inner.CurrentStage;
        }

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            if (info.PositionsEqual(_adaptedInfo))
            {
                (string managed, _) = _innerContext.GetIdentifiers(info);
                return (managed, _nativeIdentifier);
            }

            return _innerContext.GetIdentifiers(info);
        }

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name) => _innerContext.GetAdditionalIdentifier(info, name);
    }
}
