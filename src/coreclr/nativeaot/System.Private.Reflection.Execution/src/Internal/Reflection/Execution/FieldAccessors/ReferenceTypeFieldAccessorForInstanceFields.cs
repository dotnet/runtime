// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Runtime.Augments;

namespace Internal.Reflection.Execution.FieldAccessors
{
    internal sealed class ReferenceTypeFieldAccessorForInstanceFields : InstanceFieldAccessor
    {
        public ReferenceTypeFieldAccessorForInstanceFields(int offsetPlusHeader, RuntimeTypeHandle declaringTypeHandle, RuntimeTypeHandle fieldTypeHandle)
            : base(declaringTypeHandle, fieldTypeHandle, offsetPlusHeader)
        {
        }

        protected sealed override object UncheckedGetField(object obj)
        {
            return RuntimeAugments.LoadReferenceTypeField(obj, OffsetPlusHeader);
        }

        protected sealed override object UncheckedGetFieldDirectFromValueType(TypedReference typedReference)
        {
            return RuntimeAugments.LoadReferenceTypeFieldValueFromValueType(typedReference, this.Offset);
        }

        protected sealed override void UncheckedSetField(object obj, object value)
        {
            RuntimeAugments.StoreReferenceTypeField(obj, OffsetPlusHeader, value);
        }

        protected sealed override void UncheckedSetFieldDirectIntoValueType(TypedReference typedReference, object value)
        {
            RuntimeAugments.StoreReferenceTypeFieldValueIntoValueType(typedReference, this.Offset, value);
        }
    }
}
