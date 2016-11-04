// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Xml.Xsl.Runtime
{
    public sealed partial class XmlCollation
    {
        /// <summary>
        /// Create a sort key that can be compared quickly with other keys.
        /// </summary>
        internal XmlSortKey CreateSortKey(string s)
        {
            SortKey sortKey;
            byte[] bytesKey;

            sortKey = Culture.CompareInfo.GetSortKey(s, _compops);

            // Create an XmlStringSortKey using the SortKey if possible
#if DEBUG
            // In debug-only code, test other code path more frequently
            if (!UpperFirst && DescendingOrder)
                return new XmlStringSortKey(sortKey, DescendingOrder);
#else
            if (!UpperFirst)
                return new XmlStringSortKey(sortKey, DescendingOrder);
#endif

            // Get byte buffer from SortKey and modify it
            bytesKey = sortKey.KeyData;

            // TODO: Add Linux implementation for modifying SortKey; issue #13236

            return new XmlStringSortKey(bytesKey, DescendingOrder);
        }
    }
}
