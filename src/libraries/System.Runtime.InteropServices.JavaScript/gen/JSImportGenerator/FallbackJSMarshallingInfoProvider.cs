// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.JavaScript
{
    /// <summary>
    /// Always returns a JSMissingMarshallingInfo.
    /// </summary>
    internal sealed class FallbackJSMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
    {
        public bool CanProvideMarshallingInfoForType(ITypeSymbol type) => true;
        public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            return new JSMissingMarshallingInfo(JSTypeInfo.CreateJSTypeInfoForTypeSymbol(type));
        }
    }
}
