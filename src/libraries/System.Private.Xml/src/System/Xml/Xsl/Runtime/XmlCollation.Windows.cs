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
            int idx;

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
            if (UpperFirst && bytesKey.Length != 0)
            {
                // By default lower-case is always sorted first for any locale (verified by empirical testing).
                // In order to place upper-case first, invert the case weights in the generated sort key.
                // Skip to case weight section (3rd weight section)
                idx = 0;
                while (bytesKey[idx] != 1)
                    idx++;

                do
                {
                    idx++;
                }
                while (bytesKey[idx] != 1);

                // Invert all case weights (including terminating 0x1)
                do
                {
                    idx++;
                    bytesKey[idx] ^= 0xff;
                }
                while (bytesKey[idx] != 0xfe);
            }

            return new XmlStringSortKey(bytesKey, DescendingOrder);
        }
    }
}
