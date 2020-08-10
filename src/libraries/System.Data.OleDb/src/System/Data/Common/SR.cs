// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static partial class SR
    {
        internal static string GetString(string value)
        {
            return value;
        }

        internal static string GetString(string format, params object?[] args)
        {
            return SR.Format(format, args);
        }
    }
}
