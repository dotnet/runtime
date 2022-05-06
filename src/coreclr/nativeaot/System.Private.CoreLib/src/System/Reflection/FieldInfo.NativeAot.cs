// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Reflection.Augments;

namespace System.Reflection
{
    public abstract partial class FieldInfo : MemberInfo
    {
        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle) => ReflectionAugments.ReflectionCoreCallbacks.GetFieldFromHandle(handle);
        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle, RuntimeTypeHandle declaringType) => ReflectionAugments.ReflectionCoreCallbacks.GetFieldFromHandle(handle, declaringType);
    }
}
