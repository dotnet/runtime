// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Runtime;
using Internal.Runtime.Augments;

using Debug = System.Diagnostics.Debug;

namespace Internal.Reflection.Execution.FieldAccessors
{
    internal sealed class ValueTypeFieldAccessorForStaticFields : RegularStaticFieldAccessor
    {
        public ValueTypeFieldAccessorForStaticFields(IntPtr cctorContext, IntPtr staticsBase, int fieldOffset, FieldTableFlags fieldBase, RuntimeTypeHandle fieldTypeHandle)
            : base(cctorContext, staticsBase, fieldOffset, fieldBase, fieldTypeHandle)
        {
        }

        protected sealed override unsafe object GetFieldBypassCctor()
        {
            if (FieldBase == FieldTableFlags.GCStatic)
            {
                object gcStaticsRegion = RuntimeAugments.LoadReferenceTypeField(StaticsBase);
                return RuntimeAugments.LoadValueTypeField(gcStaticsRegion, FieldOffset, FieldTypeHandle);
            }
            else if (FieldBase == FieldTableFlags.NonGCStatic)
            {
                return RuntimeAugments.LoadValueTypeField(StaticsBase + FieldOffset, FieldTypeHandle);
            }

            Debug.Assert(FieldBase == FieldTableFlags.ThreadStatic);
            object threadStaticRegion = RuntimeAugments.GetThreadStaticBase(StaticsBase);
            return RuntimeAugments.LoadValueTypeField(threadStaticRegion, FieldOffset, FieldTypeHandle);
        }

        protected sealed override unsafe void UncheckedSetFieldBypassCctor(object value)
        {
            if (FieldBase == FieldTableFlags.GCStatic)
            {
                object gcStaticsRegion = RuntimeAugments.LoadReferenceTypeField(StaticsBase);
                RuntimeAugments.StoreValueTypeField(gcStaticsRegion, FieldOffset, value, FieldTypeHandle);
            }
            else if (FieldBase == FieldTableFlags.NonGCStatic)
            {
                RuntimeAugments.StoreValueTypeField(StaticsBase + FieldOffset, value, FieldTypeHandle);
            }
            else
            {
                Debug.Assert(FieldBase == FieldTableFlags.ThreadStatic);
                object threadStaticsRegion = RuntimeAugments.GetThreadStaticBase(StaticsBase);
                RuntimeAugments.StoreValueTypeField(threadStaticsRegion, FieldOffset, value, FieldTypeHandle);
            }
        }
    }
}
