// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System {

    // TypedReference is basically only ever seen on the call stack, and in param arrays.
    //  These are blob that must be dealt with by the compiler.
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using CultureInfo = System.Globalization.CultureInfo;
    using FieldInfo = System.Reflection.FieldInfo;
    using System.Security.Permissions;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [CLSCompliant(false)] 
    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Runtime.Versioning.NonVersionable] // This only applies to field layout
    public struct TypedReference
    {
        private IntPtr Value;
        private IntPtr Type;

        [System.Security.SecurityCritical]  // auto-generated_required
        [CLSCompliant(false)]
        public static TypedReference MakeTypedReference(Object target, FieldInfo[] flds) {
            if (target == null)
                throw new ArgumentNullException("target");
            if (flds == null)
                throw new ArgumentNullException("flds");
            Contract.EndContractBlock();
            if (flds.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayZeroError"));

            IntPtr[] fields = new IntPtr[flds.Length];
            // For proper handling of Nullable<T> don't change GetType() to something like 'IsAssignableFrom'
            // Currently we can't make a TypedReference to fields of Nullable<T>, which is fine.  
            RuntimeType targetType = (RuntimeType)target.GetType();
            for (int i = 0; i < flds.Length; i++)
            {
                RuntimeFieldInfo field = flds[i] as RuntimeFieldInfo;
                if (field == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeFieldInfo"));

                if (field.IsInitOnly || field.IsStatic)
                    throw new ArgumentException(Environment.GetResourceString("Argument_TypedReferenceInvalidField"));
                
                if (targetType != field.GetDeclaringTypeInternal() && !targetType.IsSubclassOf(field.GetDeclaringTypeInternal()))
                    throw new MissingMemberException(Environment.GetResourceString("MissingMemberTypeRef"));

                RuntimeType fieldType = (RuntimeType)field.FieldType;
                if (fieldType.IsPrimitive)
                    throw new ArgumentException(Environment.GetResourceString("Arg_TypeRefPrimitve"));
                
                if (i < (flds.Length - 1) && !fieldType.IsValueType)
                    throw new MissingMemberException(Environment.GetResourceString("MissingMemberNestErr"));
                
                fields[i] = field.FieldHandle.Value;
                targetType = fieldType;
            }

            TypedReference result = new TypedReference ();

            // reference to TypedReference is banned, so have to pass result as pointer
            unsafe 
            {
                InternalMakeTypedReference(&result, target, fields, targetType);
            }
            return result;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        // reference to TypedReference is banned, so have to pass result as pointer
        private unsafe static extern void InternalMakeTypedReference(void* result, Object target, IntPtr[] flds, RuntimeType lastFieldType);

        public override int GetHashCode()
        {
            if (Type == IntPtr.Zero)
                return 0;
            else
                return __reftype(this).GetHashCode();
        }

        public override bool Equals(Object o)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NYI"));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static Object ToObject(TypedReference value)
        {
            return InternalToObject(&value);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal unsafe extern static Object InternalToObject(void * value);

        internal bool IsNull 
        { 
            get
            {
                return Value.IsNull() && Type.IsNull(); 
            }
        }

        public static Type GetTargetType (TypedReference value)
        {
            return __reftype(value);
        }

        public static RuntimeTypeHandle TargetTypeToken (TypedReference value)
        {
            return __reftype(value).TypeHandle;
        }

        //  This may cause the type to be changed.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public unsafe static void SetTypedReference(TypedReference target, Object value)
        {
            InternalSetTypedReference(&target, value);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal unsafe extern static void InternalSetTypedReference(void * target, Object value);
    }

}
