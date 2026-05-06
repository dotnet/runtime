// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Marshalling information provider for <c>string</c> elements without any marshalling information on the element itself.
    /// </summary>
    /// <remarks>
    /// Provides only enough marshalling information to specify the encoding that the user requested.
    /// </remarks>
    internal sealed class DownlevelStringMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
    {
        private readonly DefaultMarshallingInfo _defaultMarshallingInfo;

        public DownlevelStringMarshallingInfoProvider(DefaultMarshallingInfo defaultMarshallingInfo)
        {
            _defaultMarshallingInfo = defaultMarshallingInfo;
        }

        public bool CanProvideMarshallingInfoForType(ITypeSymbol type) => type.SpecialType == SpecialType.System_String;

        public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            // No marshalling info was computed, but a character encoding was provided.
            // If the type is a character then pass on these details.
            return _defaultMarshallingInfo.CharEncoding == CharEncoding.Undefined ? NoMarshallingInfo.Instance : new MarshallingInfoStringSupport(_defaultMarshallingInfo.CharEncoding);
        }
    }
}
