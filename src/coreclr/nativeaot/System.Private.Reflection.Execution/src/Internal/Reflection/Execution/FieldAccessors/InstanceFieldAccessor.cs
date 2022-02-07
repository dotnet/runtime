// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

using Internal.Runtime.Augments;
using Internal.Reflection.Core.Execution;

namespace Internal.Reflection.Execution.FieldAccessors
{
    internal abstract class InstanceFieldAccessor : FieldAccessor
    {
        public InstanceFieldAccessor(RuntimeTypeHandle declaringTypeHandle, RuntimeTypeHandle fieldTypeHandle, int offsetPlusHeader)
        {
            this.DeclaringTypeHandle = declaringTypeHandle;
            this.FieldTypeHandle = fieldTypeHandle;
            this.OffsetPlusHeader = offsetPlusHeader;
        }

        public sealed override int Offset => OffsetPlusHeader - RuntimeAugments.ObjectHeaderSize;

        public sealed override object GetField(object obj)
        {
            if (obj == null)
                throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
            if (!RuntimeAugments.IsAssignable(obj, this.DeclaringTypeHandle))
                throw new ArgumentException();
            return UncheckedGetField(obj);
        }

        public sealed override object GetFieldDirect(TypedReference typedReference)
        {
            if (RuntimeAugments.IsValueType(this.DeclaringTypeHandle))
            {
                // We're being asked to read a field from the value type pointed to by the TypedReference. This code path
                // avoids boxing that value type by adding this field's offset to the TypedReference's managed pointer.
                Type targetType = TypedReference.GetTargetType(typedReference);
                if (!(targetType.TypeHandle.Equals(this.DeclaringTypeHandle)))
                    throw new ArgumentException();
                return UncheckedGetFieldDirectFromValueType(typedReference);
            }
            else
            {
                // We're being asked to read a field from a reference type. There's no boxing to optimize out in that case so just handle it as
                // if this was a FieldInfo.GetValue() call.
                object obj = TypedReference.ToObject(typedReference);
                return GetField(obj);
            }
        }

        protected abstract object UncheckedGetFieldDirectFromValueType(TypedReference typedReference);

        public sealed override void SetField(object obj, object value, BinderBundle binderBundle)
        {
            if (obj == null)
                throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
            if (!RuntimeAugments.IsAssignable(obj, this.DeclaringTypeHandle))
                throw new ArgumentException();
            value = RuntimeAugments.CheckArgument(value, this.FieldTypeHandle, binderBundle);
            UncheckedSetField(obj, value);
        }

        public sealed override void SetFieldDirect(TypedReference typedReference, object value)
        {
            if (RuntimeAugments.IsValueType(this.DeclaringTypeHandle))
            {
                // We're being asked to store a field into the value type pointed to by the TypedReference. This code path
                // bypasses boxing that value type by adding this field's offset to the TypedReference's managed pointer.
                // (Otherwise, the store would go into a useless temporary copy rather than the intended destination.)
                Type targetType = TypedReference.GetTargetType(typedReference);
                if (!(targetType.TypeHandle.Equals(this.DeclaringTypeHandle)))
                    throw new ArgumentException();
                value = RuntimeAugments.CheckArgumentForDirectFieldAccess(value, this.FieldTypeHandle);
                UncheckedSetFieldDirectIntoValueType(typedReference, value);
            }
            else
            {
                // We're being asked to store a field from a reference type. There's no boxing to bypass in that case so just handle it as
                // if this was a FieldInfo.SetValue() call (but using SetValueDirect's argument coercing semantics)
                object obj = TypedReference.ToObject(typedReference);
                if (obj == null)
                    throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
                if (!RuntimeAugments.IsAssignable(obj, this.DeclaringTypeHandle))
                    throw new ArgumentException();
                value = RuntimeAugments.CheckArgumentForDirectFieldAccess(value, this.FieldTypeHandle);
                UncheckedSetField(obj, value);
            }
        }

        protected abstract void UncheckedSetFieldDirectIntoValueType(TypedReference typedReference, object value);

        protected abstract object UncheckedGetField(object obj);
        protected abstract void UncheckedSetField(object obj, object value);

        protected int OffsetPlusHeader { get; }
        protected RuntimeTypeHandle DeclaringTypeHandle { get; }
        protected RuntimeTypeHandle FieldTypeHandle { get; }
    }
}
