// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static unsafe partial class InitHelpers
    {
        [LibraryImport(RuntimeHelpers.QCall)]
        private static partial void InitClassHelper(MethodTable* mt);

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void InitClassSlow(MethodTable* mt)
        {
            InitClassHelper(mt);
        }

        [DebuggerHidden]
        private static void InitClass(MethodTable* mt)
        {
            if (mt->AuxiliaryData->IsClassInited)
                return;
            else
                InitClassSlow(mt);
        }

        [DebuggerHidden]
        private static void InitInstantiatedClass(MethodTable* mt, MethodDesc* methodDesc)
        {
            MethodTable *pTemplateMT = methodDesc->MethodTable;
            MethodTable *pMT;

            if (pTemplateMT->IsSharedByGenericInstantiations)
            {
                pMT = mt->GetMethodTableMatchingParentClass(pTemplateMT);
            }
            else
            {
                pMT = pTemplateMT;
            }

            if (pMT->AuxiliaryData->IsClassInitedAndActive)
                return;
            else
                InitClassSlow(pMT);
        }

        [DebuggerHidden]
        [UnmanagedCallersOnly]
        internal static void CallClassConstructor(void* cctor, MethodTable* instantiatingArg, Exception* pException)
        {
            try
            {
                if (instantiatingArg == null)
                {
                    ((delegate* unmanaged<void>)cctor)();
                }
                else
                {
                    // Explicitly pass the instantiating argument as a regular argument to match the ABI of a non-instantiating stub.
                    ((delegate* unmanaged<void*, void>)cctor)(instantiatingArg);
                }
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }
}
