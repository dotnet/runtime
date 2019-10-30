// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class SteParam
    {
        // Map values from a permutation into a parameter (doesn't currently map values)
        public static void Map(StePermutation perm, SqlParameter param)
        {
            object attr;
            bool didSetSqlDbType = false;
            if (perm.TryGetValue(SteAttributeKey.SqlDbType, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                param.SqlDbType = (SqlDbType)attr;
                didSetSqlDbType = true;
            }

            if (perm.TryGetValue(SteAttributeKey.MaxLength, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                param.Size = (int)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.Precision, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                param.Precision = (byte)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.Scale, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                param.Scale = (byte)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.LocaleId, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                param.LocaleId = (int)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.CompareOptions, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                param.CompareInfo = (SqlCompareOptions)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.TypeName, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                if (didSetSqlDbType && SqlDbType.Structured == param.SqlDbType)
                {
                    param.TypeName = (string)attr;
                }
                else
                {
                    // assume UdtType by default.
                    //param.UdtTypeName = (string)attr;
                    throw new NotSupportedException("SqlParameter.UdtTypeName is not supported");
                }
            }

            if (perm.TryGetValue(SteAttributeKey.Offset, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                param.Offset = (int)attr;
            }
        }
    }
}
