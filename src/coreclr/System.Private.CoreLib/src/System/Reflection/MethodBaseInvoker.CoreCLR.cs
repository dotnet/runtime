// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
    {
        internal static MethodBaseInvoker GetOrCreate(RuntimeMethodInfo method) =>
            MethodBaseInvoker.GetOrCreate(method, (RuntimeType)method.ReturnType, method.ArgumentTypes);

        internal static MethodBaseInvoker GetOrCreate(RuntimeConstructorInfo constructor) =>
            MethodBaseInvoker.GetOrCreate(constructor, (RuntimeType)typeof(void), constructor.ArgumentTypes);

        internal static MethodBaseInvoker GetOrCreate(DynamicMethod dm) =>
            MethodBaseInvoker.GetOrCreate(dm, (RuntimeType)dm.ReturnType, dm.ArgumentTypes);
    }
}
