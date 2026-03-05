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
        [RequiresUnsafe]
        private static partial void InitClassHelper(MethodTable* mt);

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [RequiresUnsafe]
        internal static void InitClassSlow(MethodTable* mt)
        {
            InitClassHelper(mt);
        }

        [DebuggerHidden]
        [RequiresUnsafe]
        private static void InitClass(MethodTable* mt)
        {
            if (mt->AuxiliaryData->IsClassInited)
                return;
            else
                InitClassSlow(mt);
        }

        [DebuggerHidden]
        [RequiresUnsafe]
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
    }
}
