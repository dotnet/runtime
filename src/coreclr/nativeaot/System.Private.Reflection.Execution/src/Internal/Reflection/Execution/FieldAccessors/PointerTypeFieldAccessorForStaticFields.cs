// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Runtime;
using Internal.Runtime.Augments;

using Debug = System.Diagnostics.Debug;

namespace Internal.Reflection.Execution.FieldAccessors
{
    internal sealed class PointerTypeFieldAccessorForStaticFields : RegularStaticFieldAccessor
    {
        public PointerTypeFieldAccessorForStaticFields(IntPtr cctorContext, IntPtr staticsBase, int fieldOffset, FieldTableFlags fieldBase, RuntimeTypeHandle fieldTypeHandle)
            : base(cctorContext, staticsBase, fieldOffset, fieldBase, fieldTypeHandle)
        {
        }

        protected sealed override unsafe object GetFieldBypassCctor()
        {
            if (FieldBase == FieldTableFlags.GCStatic)
            {
                object gcStaticsRegion = RuntimeAugments.LoadReferenceTypeField(StaticsBase);
                return RuntimeAugments.LoadPointerTypeField(gcStaticsRegion, FieldOffset, FieldTypeHandle);
            }
            else if (FieldBase == FieldTableFlags.NonGCStatic)
            {
                return RuntimeAugments.LoadPointerTypeField(StaticsBase + FieldOffset, FieldTypeHandle);
            }

            Debug.Assert(FieldBase == FieldTableFlags.ThreadStatic);
            object threadStaticRegion = RuntimeAugments.GetThreadStaticBase(StaticsBase);
            return RuntimeAugments.LoadPointerTypeField(threadStaticRegion, FieldOffset, FieldTypeHandle);
        }

        protected sealed override unsafe void UncheckedSetFieldBypassCctor(object value)
        {
            if (FieldBase == FieldTableFlags.GCStatic)
            {
                object gcStaticsRegion = RuntimeAugments.LoadReferenceTypeField(StaticsBase);
                RuntimeAugments.StoreValueTypeField(gcStaticsRegion, FieldOffset, value, typeof(IntPtr).TypeHandle);
            }
            else if (FieldBase == FieldTableFlags.NonGCStatic)
            {
                RuntimeAugments.StoreValueTypeField(StaticsBase + FieldOffset, value, typeof(IntPtr).TypeHandle);
            }
            else
            {
                Debug.Assert(FieldBase == FieldTableFlags.ThreadStatic);
                object threadStaticsRegion = RuntimeAugments.GetThreadStaticBase(StaticsBase);
                RuntimeAugments.StoreValueTypeField(threadStaticsRegion, FieldOffset, value, typeof(IntPtr).TypeHandle);
            }
        }
    }
}
