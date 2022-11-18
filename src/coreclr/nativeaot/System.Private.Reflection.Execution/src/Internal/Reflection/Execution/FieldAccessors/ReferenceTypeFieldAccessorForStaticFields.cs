// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Runtime;
using Internal.Runtime.Augments;

using Debug = System.Diagnostics.Debug;

namespace Internal.Reflection.Execution.FieldAccessors
{
    internal sealed class ReferenceTypeFieldAccessorForStaticFields : RegularStaticFieldAccessor
    {
        public ReferenceTypeFieldAccessorForStaticFields(IntPtr cctorContext, IntPtr staticsBase, int fieldOffset, FieldTableFlags fieldBase, RuntimeTypeHandle fieldTypeHandle)
            : base(cctorContext, staticsBase, fieldOffset, fieldBase, fieldTypeHandle)
        {
        }

        protected sealed override unsafe object GetFieldBypassCctor()
        {
            if (FieldBase == FieldTableFlags.GCStatic)
            {
                object gcStaticsRegion = RuntimeAugments.LoadReferenceTypeField(StaticsBase);
                return RuntimeAugments.LoadReferenceTypeField(gcStaticsRegion, FieldOffset);
            }
            else if (FieldBase == FieldTableFlags.NonGCStatic)
            {
                return RuntimeAugments.LoadReferenceTypeField(StaticsBase + FieldOffset);
            }

            Debug.Assert(FieldBase == FieldTableFlags.ThreadStatic);
            object threadStaticRegion = RuntimeAugments.GetThreadStaticBase(StaticsBase);
            return RuntimeAugments.LoadReferenceTypeField(threadStaticRegion, FieldOffset);
        }

        protected sealed override unsafe void UncheckedSetFieldBypassCctor(object value)
        {
            if (FieldBase == FieldTableFlags.GCStatic)
            {
                object gcStaticsRegion = RuntimeAugments.LoadReferenceTypeField(StaticsBase);
                RuntimeAugments.StoreReferenceTypeField(gcStaticsRegion, FieldOffset, value);
                return;
            }
            else if (FieldBase == FieldTableFlags.NonGCStatic)
            {
                RuntimeAugments.StoreReferenceTypeField(StaticsBase + FieldOffset, value);
            }
            else
            {
                Debug.Assert(FieldBase == FieldTableFlags.ThreadStatic);
                object threadStaticsRegion = RuntimeAugments.GetThreadStaticBase(StaticsBase);
                RuntimeAugments.StoreReferenceTypeField(threadStaticsRegion, FieldOffset, value);
            }
        }
    }
}
