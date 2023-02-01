// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        private void InitNativeSortHandle(string interopCultureName)
        {
            if (GlobalizationMode.Invariant)
            {
                _isAsciiEqualityOrdinal = true;
            }
            else
            {
                Debug.Assert(!GlobalizationMode.UseNls);
                Debug.Assert(interopCultureName != null);

                // Inline the following condition to avoid potential implementation cycles within globalization
                //
                // _isAsciiEqualityOrdinal = _sortName == "" || _sortName == "en" || _sortName.StartsWith("en-", StringComparison.Ordinal);
                //
               /* _isAsciiEqualityOrdinal = _sortName.Length == 0 ||
                    (_sortName.Length >= 2 && _sortName[0] == 'e' && _sortName[1] == 'n' && (_sortName.Length == 2 || _sortName[2] == '-'));

                _sortHandle = SortHandleCache.GetCachedSortHandle(interopCultureName);*/
            }
        }
    }
}
