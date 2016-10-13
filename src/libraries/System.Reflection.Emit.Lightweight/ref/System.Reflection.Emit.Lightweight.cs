// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Reflection.Emit
{
    public sealed partial class DynamicMethod : System.Reflection.MethodInfo
    {
        [System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes, System.Reflection.Module m, bool skipVisibility) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes, System.Type owner, bool skipVisibility) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, bool restrictedSkipVisibility) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, System.Reflection.Module m) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, System.Reflection.Module m, bool skipVisibility) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, System.Type owner) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public DynamicMethod(string name, System.Type returnType, System.Type[] parameterTypes, System.Type owner, bool skipVisibility) { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        public override string Name { get { throw null; } }
        public override System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public override System.Type ReturnType { get { throw null; } }
        [System.Security.SecuritySafeCriticalAttribute]
        public sealed override System.Delegate CreateDelegate(System.Type delegateType) { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public sealed override System.Delegate CreateDelegate(System.Type delegateType, object target) { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        [System.Security.SecuritySafeCriticalAttribute]
        public System.Reflection.Emit.ILGenerator GetILGenerator(int streamSize) { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        public override string ToString() { throw null; }
    }
}
