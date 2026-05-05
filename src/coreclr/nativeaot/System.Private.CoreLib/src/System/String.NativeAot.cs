// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace System
{
    // This class is marked EagerStaticClassConstruction because it's nice to have this
    // eagerly constructed to avoid the cost of defered ctors. I can't imagine any app that doesn't use string
    //
    [EagerStaticClassConstruction]
    public partial class String
    {
        [Intrinsic]
        public static readonly string Empty = "";

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [RuntimeImport(RuntimeImports.RuntimeLibrary, "RhNewString")]
        private static extern unsafe string FastAllocateString(MethodTable *pMT, nint length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe string FastAllocateString(nint length)
        {
            // We allocate one extra char as an interop convenience so that our strings are null-
            // terminated, however, we don't pass the extra +1 to the string allocation because the base
            // size of this object includes the _firstChar field.
            string newStr = FastAllocateString(MethodTable.Of<string>(), length);
            Debug.Assert(newStr._stringLength == length);
            return newStr;
        }
    }
}
