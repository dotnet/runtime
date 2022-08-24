// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Marshalling information provider for <c>bool</c> elements without any marshalling information.
    /// </summary>
    public sealed class BooleanMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
    {
        public bool CanProvideMarshallingInfoForType(ITypeSymbol type) => type.SpecialType == SpecialType.System_Boolean;

        public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            // We intentionally don't support marshalling bool with no marshalling info
            // as treating bool as a non-normalized 1-byte value is generally not a good default.
            // Additionally, that default is different than the runtime marshalling, so by explicitly
            // blocking bool marshalling without additional info, we make it a little easier
            // to transition by explicitly notifying people of changing behavior.
            return NoMarshallingInfo.Instance;
        }
    }
}
