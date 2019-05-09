// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    // TypedReference is basically only ever seen on the call stack, and in param arrays.
    //  These are blob that must be dealt with by the compiler.

    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using CultureInfo = System.Globalization.CultureInfo;
    using FieldInfo = System.Reflection.FieldInfo;
    using System.Runtime.Versioning;

    [CLSCompliant(false)]
    [System.Runtime.Versioning.NonVersionable] // This only applies to field layout
    public ref struct TypedReference
    {
        private IntPtr Value;
        private IntPtr Type;

        [CLSCompliant(false)]
        public static TypedReference MakeTypedReference(object target, FieldInfo[] flds)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (flds == null)
                throw new ArgumentNullException(nameof(flds));
            if (flds.Length == 0)
                throw new ArgumentException(SR.Arg_ArrayZeroError, nameof(flds));

            IntPtr[] fields = new IntPtr[flds.Length];
            // For proper handling of Nullable<T> don't change GetType() to something like 'IsAssignableFrom'
            // Currently we can't make a TypedReference to fields of Nullable<T>, which is fine.  
            RuntimeType targetType = (RuntimeType)target.GetType();
            for (int i = 0; i < flds.Length; i++)
            {
                RuntimeFieldInfo? field = flds[i] as RuntimeFieldInfo;
                if (field == null)
                    throw new ArgumentException(SR.Argument_MustBeRuntimeFieldInfo);

                if (field.IsStatic)
                    throw new ArgumentException(SR.Format(SR.Argument_TypedReferenceInvalidField, field.Name));

                if (targetType != field.GetDeclaringTypeInternal() && !targetType.IsSubclassOf(field.GetDeclaringTypeInternal()))
                    throw new MissingMemberException(SR.MissingMemberTypeRef);

                RuntimeType fieldType = (RuntimeType)field.FieldType;
                if (fieldType.IsPrimitive)
                    throw new ArgumentException(SR.Format(SR.Arg_TypeRefPrimitve, field.Name));

                if (i < (flds.Length - 1) && !fieldType.IsValueType)
                    throw new MissingMemberException(SR.MissingMemberNestErr);

                fields[i] = field.FieldHandle.Value;
                targetType = fieldType;
            }

            TypedReference result = new TypedReference();

            // reference to TypedReference is banned, so have to pass result as pointer
            unsafe
            {
                InternalMakeTypedReference(&result, target, fields, targetType);
            }
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        // reference to TypedReference is banned, so have to pass result as pointer
        private static extern unsafe void InternalMakeTypedReference(void* result, object target, IntPtr[] flds, RuntimeType lastFieldType);

        public override int GetHashCode()
        {
            if (Type == IntPtr.Zero)
                return 0;
            else
                return __reftype(this).GetHashCode();
        }

        public override bool Equals(object? o)
        {
            throw new NotSupportedException(SR.NotSupported_NYI);
        }

        public static unsafe object ToObject(TypedReference value)
        {
            return InternalToObject(&value);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe object InternalToObject(void* value);

        internal bool IsNull
        {
            get
            {
                return Value == IntPtr.Zero && Type == IntPtr.Zero;
            }
        }

        public static Type GetTargetType(TypedReference value)
        {
            return __reftype(value);
        }

        public static RuntimeTypeHandle TargetTypeToken(TypedReference value)
        {
            return __reftype(value).TypeHandle;
        }

        //  This may cause the type to be changed.
        [CLSCompliant(false)]
        public static unsafe void SetTypedReference(TypedReference target, object? value)
        {
            InternalSetTypedReference(&target, value);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void InternalSetTypedReference(void* target, object? value);
    }
}
