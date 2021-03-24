// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection
{
    public abstract partial class DispatchProxy
    {
        protected DispatchProxy() { }
        public static T Create<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]  T, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TProxy>() where TProxy : System.Reflection.DispatchProxy { throw null; }
        protected abstract object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args);
    }
}
