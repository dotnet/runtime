// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public abstract partial class FieldInfo : MemberInfo
    {
        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle, nameof(handle));

            FieldInfo f = RuntimeType.GetFieldInfo(handle.GetRuntimeFieldInfo());

            Type? declaringType = f.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType)
                throw new ArgumentException(SR.Format(
                    SR.Argument_FieldDeclaringTypeGeneric,
                    f.Name, declaringType.GetGenericTypeDefinition()));

            return f;
        }

        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle, RuntimeTypeHandle declaringType)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);

            return RuntimeType.GetFieldInfo(declaringType.GetRuntimeType(), handle.GetRuntimeFieldInfo());
        }
    }
}
