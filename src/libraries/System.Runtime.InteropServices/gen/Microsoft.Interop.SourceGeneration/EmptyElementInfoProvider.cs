// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// An <see cref="IElementInfoProvider"/> that exposes no peer elements.
    /// Used for signatures whose elements cannot legitimately reference one another
    /// (e.g. property accessors, where the getter has no parameters and the setter
    /// has only its own value parameter), so that peer-element lookups always fail.
    /// </summary>
    internal sealed class EmptyElementInfoProvider : IElementInfoProvider
    {
        public static EmptyElementInfoProvider Instance { get; } = new EmptyElementInfoProvider();

        private EmptyElementInfoProvider() { }

        public string FindNameForParamIndex(int paramIndex) => string.Empty;

        public bool TryGetInfoForElementName(AttributeData attrData, string elementName, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, out TypePositionInfo info)
        {
            info = null;
            return false;
        }

        public bool TryGetInfoForParamIndex(AttributeData attrData, int paramIndex, GetMarshallingInfoCallback marshallingInfoCallback, IElementInfoProvider rootProvider, out TypePositionInfo info)
        {
            info = null;
            return false;
        }
    }
}
