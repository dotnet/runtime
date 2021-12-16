// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Delegate helpers for generated code.
    /// </summary>
    internal static class DelegateHelpers
    {
        private static object[] s_emptyObjectArray = Array.Empty<object>();

        internal static object[] GetEmptyObjectArray()
        {
            return s_emptyObjectArray;
        }
    }
}
