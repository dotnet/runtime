// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static unsafe partial class StaticsHelpers
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetNonGCStaticBaseSlow(MethodTable* mt)
        {
            InitHelpers.InitClassSlow(mt);
            return ref MethodTable.MaskStaticsPointer(mt->AuxiliaryData->DynamicStaticsInfo._pNonGCStatics);
        }

        [DebuggerHidden]
        private static ref byte GetNonGCStaticBase(MethodTable* mt)
        {
            ref byte nonGCStaticBase = ref mt->AuxiliaryData->DynamicStaticsInfo._pNonGCStatics;

            if ((((nuint)Unsafe.AsPointer(ref nonGCStaticBase)) & DynamicStaticsInfo.ISCLASSINITED) != 0)
                return ref GetNonGCStaticBaseSlow(mt);
            else
                return ref nonGCStaticBase;
        }

        [DebuggerHidden]
        private static ref byte GetDynamicNonGCStaticBase(DynamicStaticsInfo *dynamicStaticsInfo)
        {
            ref byte nonGCStaticBase = ref dynamicStaticsInfo->_pNonGCStatics;

            if ((((nuint)Unsafe.AsPointer(ref nonGCStaticBase)) & DynamicStaticsInfo.ISCLASSINITED) != 0)
                return ref GetNonGCStaticBaseSlow(dynamicStaticsInfo->_methodTable);
            else
                return ref nonGCStaticBase;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetGCStaticBaseSlow(MethodTable* mt)
        {
            InitHelpers.InitClassSlow(mt);
            return ref MethodTable.MaskStaticsPointer(mt->AuxiliaryData->DynamicStaticsInfo._pGCStatics);
        }

        [DebuggerHidden]
        private static ref byte GetGCStaticBase(MethodTable* mt)
        {
            ref byte gcStaticBase = ref mt->AuxiliaryData->DynamicStaticsInfo._pNonGCStatics;

            if ((((nuint)Unsafe.AsPointer(ref gcStaticBase)) & DynamicStaticsInfo.ISCLASSINITED) != 0)
                return ref GetGCStaticBaseSlow(dynamicStaticsInfo->_methodTable);
            else
                return ref gcStaticBase;
        }

        [DebuggerHidden]
        private static ref byte GetDynamicGCStaticBase(DynamicStaticsInfo *dynamicStaticsInfo)
        {
            ref byte gcStaticBase = ref dynamicStaticsInfo->_pNonGCStatics;

            if ((((nuint)Unsafe.AsPointer(ref gcStaticBase)) & DynamicStaticsInfo.ISCLASSINITED) != 0)
                return ref GetGCStaticBaseSlow(dynamicStaticsInfo->_methodTable);
            else
                return ref gcStaticBase;
        }
    }
}
