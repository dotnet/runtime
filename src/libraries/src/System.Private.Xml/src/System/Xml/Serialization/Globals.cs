// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Reflection;


namespace System.Xml.Serialization
{
    internal static class Globals
    {
        [SecurityCritical]
        private static Type s_typeOfDBNull;
        internal static Type TypeOfDBNull
        {
            [SecuritySafeCritical]
            get
            {
                if (s_typeOfDBNull == null)
                    s_typeOfDBNull = Type.GetType("System.DBNull, System.Data.Common, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                return s_typeOfDBNull;
            }
        }

        [SecurityCritical]
        private static object s_valueOfDBNull;
        internal static object ValueOfDBNull
        {
            [SecuritySafeCritical]
            get
            {
                if (s_valueOfDBNull == null && TypeOfDBNull != null)
                    s_valueOfDBNull = TypeOfDBNull.GetField("Value").GetValue(null);
                return s_valueOfDBNull;
            }
        }

        internal static bool IsDBNullValue(object o)
        {
            return o != null && ValueOfDBNull != null && ValueOfDBNull.Equals(o);
        }
        internal static Exception NotSupported(string msg)
        {
            System.Diagnostics.Debug.Assert(false, msg);
            return new NotSupportedException(msg);
        }
    }
}
