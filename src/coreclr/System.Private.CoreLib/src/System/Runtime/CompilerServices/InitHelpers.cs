// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private static void* InitClass(MethodTable* mt)
        {
            if (!mt->AuxiliaryData->IsClassInited)
                InitClassSlow(mt);

            // The InitClass JIT helper is modeled as value-returning by the JIT and interpreter
            // (the result is pushed and then discarded). On targets that use portable entry points
            // (wasm), the call_indirect signature must match the compiled method exactly, including
            // return arity, so this helper returns a (dummy) value rather than void.
            return null;
        }

        [DebuggerHidden]
        private static void* InitInstantiatedClass(MethodTable* mt, MethodDesc* methodDesc)
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

            if (!pMT->AuxiliaryData->IsClassInitedAndActive)
                InitClassSlow(pMT);

            // See the comment in InitClass for why this helper returns a value rather than void.
            return null;
        }

        [DebuggerHidden]
        [UnmanagedCallersOnly]
        internal static void CallClassConstructor(void* cctor, void* instantiatingArg, Exception* pException)
        {
            try
            {
                if (instantiatingArg == null)
                {
                    ((delegate*<void>)cctor)();
                }
                else
                {
                    // Explicitly pass the instantiating argument as a regular argument to match the ABI of a non-instantiating stub.
                    ((delegate*<void*, void>)cctor)(instantiatingArg);
                }
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }
}
