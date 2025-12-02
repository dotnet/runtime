// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO
{
    public abstract unsafe partial class Stream
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Stream_HasOverriddenSlow")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool HasOverriddenSlow(MethodTable* pMT, [MarshalAs(UnmanagedType.Bool)] bool isRead);

        private bool HasOverriddenBeginEndRead()
        {
            MethodTable* pMT = RuntimeHelpers.GetMethodTable(this);
            bool res = pMT->AuxiliaryData->HasCheckedStreamOverride
                ? pMT->AuxiliaryData->IsStreamOverriddenRead
                : HasOverriddenReadSlow(pMT);
            GC.KeepAlive(this);
            return res;

            [MethodImpl(MethodImplOptions.NoInlining)]
            static bool HasOverriddenReadSlow(MethodTable* pMT)
                => HasOverriddenSlow(pMT, isRead: true);
        }

        private bool HasOverriddenBeginEndWrite()
        {
            MethodTable* pMT = RuntimeHelpers.GetMethodTable(this);
            bool res = pMT->AuxiliaryData->HasCheckedStreamOverride
                ? pMT->AuxiliaryData->IsStreamOverriddenWrite
                : HasOverriddenWriteSlow(pMT);
            GC.KeepAlive(this);
            return res;

            [MethodImpl(MethodImplOptions.NoInlining)]
            static bool HasOverriddenWriteSlow(MethodTable* pMT)
                => HasOverriddenSlow(pMT, isRead: false);
        }
    }
}
