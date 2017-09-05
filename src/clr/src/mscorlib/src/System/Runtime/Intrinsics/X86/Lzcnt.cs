// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel LZCNT hardware instructions via intrinsics
    /// </summary>
    public static class Lzcnt
    {
        public static bool IsSupported { get { return false; } }

        /// <summary>
        /// unsigned int _lzcnt_u32 (unsigned int a)
        /// </summary>
        public static uint LeadingZeroCount(uint value) { throw new NotImplementedException(); }
        /// <summary>
        /// unsigned __int64 _lzcnt_u64 (unsigned __int64 a)
        /// </summary>
        public static ulong LeadingZeroCount(ulong value) { throw new NotImplementedException(); }
    }
}
