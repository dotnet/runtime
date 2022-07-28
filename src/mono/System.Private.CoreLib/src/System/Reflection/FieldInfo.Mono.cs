// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    public partial class FieldInfo
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern FieldInfo internal_from_handle_type(IntPtr field_handle, IntPtr type_handle);

        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);
            return internal_from_handle_type(handle.Value, IntPtr.Zero);
        }

        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle, RuntimeTypeHandle declaringType)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);
            FieldInfo fi = internal_from_handle_type(handle.Value, declaringType.Value);
            if (fi == null)
                throw new ArgumentException("The field handle and the type handle are incompatible.");
            return fi;
        }

        internal virtual int GetFieldOffset()
        {
            throw NotImplemented.ByDesign;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern MarshalAsAttribute get_marshal_info();

        internal object[]? GetPseudoCustomAttributes()
        {
            int count = 0;

            if (IsNotSerialized)
                count++;

            if (DeclaringType!.IsExplicitLayout)
                count++;

            MarshalAsAttribute marshalAs = get_marshal_info();
            if (marshalAs != null)
                count++;

            if (count == 0)
                return null;
            object[] attrs = new object[count];
            count = 0;

            if (IsNotSerialized)
                attrs[count++] = new NonSerializedAttribute();
            if (DeclaringType.IsExplicitLayout)
                attrs[count++] = new FieldOffsetAttribute(GetFieldOffset());
            if (marshalAs != null)
                attrs[count++] = marshalAs;

            return attrs;
        }

        internal CustomAttributeData[]? GetPseudoCustomAttributesData()
        {
            int count = 0;

            if (IsNotSerialized)
                count++;

            if (DeclaringType!.IsExplicitLayout)
                count++;

            MarshalAsAttribute marshalAs = get_marshal_info();
            if (marshalAs != null)
                count++;

            if (count == 0)
                return null;
            CustomAttributeData[] attrsData = new CustomAttributeData[count];
            count = 0;

            if (IsNotSerialized)
                attrsData[count++] = new RuntimeCustomAttributeData((typeof(NonSerializedAttribute)).GetConstructor(Type.EmptyTypes)!);
            if (DeclaringType.IsExplicitLayout)
            {
                var ctorArgs = new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), GetFieldOffset()) };
                attrsData[count++] = new RuntimeCustomAttributeData(
                    (typeof(FieldOffsetAttribute)).GetConstructor(new[] { typeof(int) })!,
                    ctorArgs,
                    Array.Empty<CustomAttributeNamedArgument>());
            }

            if (marshalAs != null)
            {
                var ctorArgs = new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(UnmanagedType), marshalAs.Value) };
                attrsData[count++] = new RuntimeCustomAttributeData(
                    (typeof(MarshalAsAttribute)).GetConstructor(new[] { typeof(UnmanagedType) })!,
                    ctorArgs,
                    Array.Empty<CustomAttributeNamedArgument>());//FIXME Get named params
            }

            return attrsData;
        }
    }
}
