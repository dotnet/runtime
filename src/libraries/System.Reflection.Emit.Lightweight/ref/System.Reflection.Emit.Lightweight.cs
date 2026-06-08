// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection.Emit
{
    public sealed partial class DynamicILInfo
    {
        internal DynamicILInfo() { }
        public System.Reflection.Emit.DynamicMethod DynamicMethod { get { throw null; } }
        public int GetTokenFor(byte[] signature) { throw null; }
        public int GetTokenFor(System.Reflection.Emit.DynamicMethod method) { throw null; }
        public int GetTokenFor(System.RuntimeFieldHandle field) { throw null; }
        public int GetTokenFor(System.RuntimeFieldHandle field, System.RuntimeTypeHandle contextType) { throw null; }
        public int GetTokenFor(System.RuntimeMethodHandle method) { throw null; }
        public int GetTokenFor(System.RuntimeMethodHandle method, System.RuntimeTypeHandle contextType) { throw null; }
        public int GetTokenFor(System.RuntimeTypeHandle type) { throw null; }
        public int GetTokenFor(string literal) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe void SetCode(byte* code, int codeSize, int maxStackSize) { }
        public void SetCode(byte[]? code, int maxStackSize) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe void SetExceptions(byte* exceptions, int exceptionsSize) { }
        public void SetExceptions(byte[]? exceptions) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe void SetLocalSignature(byte* localSignature, int signatureSize) { }
        public void SetLocalSignature(byte[]? localSignature) { }
    }
    public sealed partial class DynamicMethod : System.Reflection.MethodInfo
    {
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes, System.Reflection.Module m, bool skipVisibility) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes, System.Type owner, bool skipVisibility) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, System.Type? returnType, System.Type[]? parameterTypes) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, System.Type? returnType, System.Type[]? parameterTypes, bool restrictedSkipVisibility) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, System.Type? returnType, System.Type[]? parameterTypes, System.Reflection.Module m) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, System.Type? returnType, System.Type[]? parameterTypes, System.Reflection.Module m, bool skipVisibility) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, System.Type? returnType, System.Type[]? parameterTypes, System.Type owner) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, System.Type? returnType, System.Type[]? parameterTypes, System.Type owner, bool skipVisibility) { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        public override bool IsSecurityCritical { get { throw null; } }
        public override bool IsSecuritySafeCritical { get { throw null; } }
        public override bool IsSecurityTransparent { get { throw null; } }
        public override System.RuntimeMethodHandle MethodHandle { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type? ReflectedType { get { throw null; } }
        public override System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public override System.Type ReturnType { get { throw null; } }
        public override System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes { get { throw null; } }
        public sealed override System.Delegate CreateDelegate(System.Type delegateType) { throw null; }
        public sealed override System.Delegate CreateDelegate(System.Type delegateType, object? target) { throw null; }
        public System.Reflection.Emit.ParameterBuilder? DefineParameter(int position, System.Reflection.ParameterAttributes attributes, string? parameterName) { throw null; }
        public override System.Reflection.MethodInfo GetBaseDefinition() { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public System.Reflection.Emit.DynamicILInfo GetDynamicILInfo() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator(int streamSize) { throw null; }
        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        public override object? Invoke(object? obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public override string ToString() { throw null; }
    }
}
