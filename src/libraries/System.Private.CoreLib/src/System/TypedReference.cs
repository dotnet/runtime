// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TypedReference is basically only ever seen on the call stack, and in param arrays.
//  These are blob that must be dealt with by the compiler.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    [CLSCompliant(false)]
    public ref partial struct TypedReference
    {
        public static TypedReference MakeTypedReference(object target, FieldInfo[] flds)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(flds);

            if (flds.Length == 0)
            {
                throw new ArgumentException(SR.Arg_ArrayZeroError, nameof(flds));
            }

            ref byte targetRef = ref Unsafe.NullRef<byte>();

            // For proper handling of Nullable<T> don't change GetType() to something like 'IsAssignableFrom'
            // Currently we can't make a TypedReference to fields of Nullable<T>, which is fine.
            RuntimeType targetType = (RuntimeType)target.GetType();
            for (int i = 0; i < flds.Length; i++)
            {
                RuntimeFieldInfo field = flds[i] as RuntimeFieldInfo ?? throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo);
                if (field.IsStatic)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_TypedReferenceInvalidField, field.Name));
                }

                if (targetType != field.GetDeclaringTypeInternal() && !targetType.IsSubclassOf(field.GetDeclaringTypeInternal()))
                {
                    throw new MissingMemberException(SR.MissingMemberTypeRef);
                }

                RuntimeType fieldType = (RuntimeType)field.FieldType;
                if (i < (flds.Length - 1) && !fieldType.IsValueType)
                {
                    throw new MissingMemberException(SR.MissingMemberNestErr);
                }

                if (i == 0)
                {
                    targetRef = ref RuntimeFieldHandle.GetFieldDataReference(target, field);
                }
                else
                {
                    Debug.Assert(targetType.IsValueType);
                    targetRef = ref RuntimeFieldHandle.GetFieldDataReference(ref targetRef, field);
                }
                targetType = fieldType;
            }

            return new TypedReference(ref targetRef, targetType);
        }

        public override int GetHashCode()
        {
            if (_type == IntPtr.Zero)
                return 0;
            else
                return __reftype(this).GetHashCode();
        }

        public override bool Equals(object? o)
        {
            throw new NotSupportedException(SR.NotSupported_NYI);
        }

        internal bool IsNull => Unsafe.IsNullRef(ref _value) && _type == IntPtr.Zero;

        public static Type GetTargetType(TypedReference value)
        {
            return __reftype(value);
        }

        public static RuntimeTypeHandle TargetTypeToken(TypedReference value)
        {
            return __reftype(value).TypeHandle;
        }

        public static void SetTypedReference(TypedReference target, object? value)
        {
            throw new NotSupportedException();
        }
    }
}
