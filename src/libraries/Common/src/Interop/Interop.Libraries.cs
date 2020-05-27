// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Libraries
    {
#if MONO
        internal const string GlobalizationNative = "__Internal";
#else
        internal const string GlobalizationNative = "QCall";
#endif
    }
}
