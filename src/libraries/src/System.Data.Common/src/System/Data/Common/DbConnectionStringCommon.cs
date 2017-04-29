// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Data.Common
{
    internal static class DbConnectionStringBuilderUtil
    {
        internal static string ConvertToString(object value)
        {
            try
            {
                return ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                throw ADP.ConvertFailed(value.GetType(), typeof(string), e);
            }
        }
    }

    internal static class DbConnectionStringDefaults
    {
        internal const int ConnectTimeout = 15;
    }

    internal static class DbConnectionStringKeywords
    {
        internal const string Driver = "Driver";
        internal const string Password = "Password";
    }

    internal static class DbConnectionStringSynonyms
    {
        internal const string Pwd = "pwd";
    }
}
