// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Reflection.Emit
{
    public partial class CustomAttributeBuilder
    {
        public CustomAttributeBuilder(System.Reflection.ConstructorInfo con, object[] constructorArgs) { }
        public CustomAttributeBuilder(System.Reflection.ConstructorInfo con, object[] constructorArgs, System.Reflection.FieldInfo[] namedFields, object[] fieldValues) { }
        public CustomAttributeBuilder(System.Reflection.ConstructorInfo con, object[] constructorArgs, System.Reflection.PropertyInfo[] namedProperties, object[] propertyValues) { }
        public CustomAttributeBuilder(System.Reflection.ConstructorInfo con, object[] constructorArgs, System.Reflection.PropertyInfo[] namedProperties, object[] propertyValues, System.Reflection.FieldInfo[] namedFields, object[] fieldValues) { }
    }
    public partial class ILGenerator
    {
        internal ILGenerator() { }
        public virtual int ILOffset { get { return default(int); } }
        public virtual void BeginCatchBlock(System.Type exceptionType) { }
        public virtual void BeginExceptFilterBlock() { }
        public virtual System.Reflection.Emit.Label BeginExceptionBlock() { return default(System.Reflection.Emit.Label); }
        public virtual void BeginFaultBlock() { }
        public virtual void BeginFinallyBlock() { }
        public virtual void BeginScope() { }
        public virtual System.Reflection.Emit.LocalBuilder DeclareLocal(System.Type localType) { return default(System.Reflection.Emit.LocalBuilder); }
        public virtual System.Reflection.Emit.LocalBuilder DeclareLocal(System.Type localType, bool pinned) { return default(System.Reflection.Emit.LocalBuilder); }
        public virtual System.Reflection.Emit.Label DefineLabel() { return default(System.Reflection.Emit.Label); }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, byte arg) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, double arg) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, short arg) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, int arg) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, long arg) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.ConstructorInfo con) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.Emit.Label label) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.Emit.Label[] labels) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.Emit.LocalBuilder local) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.Emit.SignatureHelper signature) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.FieldInfo field) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Reflection.MethodInfo meth) { }
        [System.CLSCompliantAttribute(false)]
        public void Emit(System.Reflection.Emit.OpCode opcode, sbyte arg) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, float arg) { }
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, string str) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void Emit(System.Reflection.Emit.OpCode opcode, System.Type cls) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void EmitCall(System.Reflection.Emit.OpCode opcode, System.Reflection.MethodInfo methodInfo, System.Type[] optionalParameterTypes) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void EmitCalli(System.Reflection.Emit.OpCode opcode, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes, System.Type[] optionalParameterTypes) { }
        public virtual void EmitWriteLine(System.Reflection.Emit.LocalBuilder localBuilder) { }
        public virtual void EmitWriteLine(System.Reflection.FieldInfo fld) { }
        public virtual void EmitWriteLine(string value) { }
        public virtual void EndExceptionBlock() { }
        public virtual void EndScope() { }
        public virtual void MarkLabel(System.Reflection.Emit.Label loc) { }
        public virtual void ThrowException(System.Type excType) { }
        public virtual void UsingNamespace(string usingNamespace) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Label
    {
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Reflection.Emit.Label obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Reflection.Emit.Label a, System.Reflection.Emit.Label b) { return default(bool); }
        public static bool operator !=(System.Reflection.Emit.Label a, System.Reflection.Emit.Label b) { return default(bool); }
    }
    public sealed partial class LocalBuilder : System.Reflection.LocalVariableInfo
    {
        internal LocalBuilder() { }
        public override bool IsPinned { get { return default(bool); } }
        public override int LocalIndex { get { return default(int); } }
        public override System.Type LocalType { get { return default(System.Type); } }
    }
    public partial class ParameterBuilder
    {
        internal ParameterBuilder() { }
        public virtual int Attributes { get { return default(int); } }
        public bool IsIn { get { return default(bool); } }
        public bool IsOptional { get { return default(bool); } }
        public bool IsOut { get { return default(bool); } }
        public virtual string Name { get { return default(string); } }
        public virtual int Position { get { return default(int); } }
        [System.Security.SecuritySafeCriticalAttribute]
        public virtual void SetConstant(object defaultValue) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
    }
    public sealed partial class SignatureHelper
    {
        internal SignatureHelper() { }
        public void AddArgument(System.Type clsArgument) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void AddArgument(System.Type argument, bool pinned) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void AddArgument(System.Type argument, System.Type[] requiredCustomModifiers, System.Type[] optionalCustomModifiers) { }
        public void AddArguments(System.Type[] arguments, System.Type[][] requiredCustomModifiers, System.Type[][] optionalCustomModifiers) { }
        public void AddSentinel() { }
        public override bool Equals(object obj) { return default(bool); }
        public static System.Reflection.Emit.SignatureHelper GetFieldSigHelper(System.Reflection.Module mod) { return default(System.Reflection.Emit.SignatureHelper); }
        public override int GetHashCode() { return default(int); }
        public static System.Reflection.Emit.SignatureHelper GetLocalVarSigHelper() { return default(System.Reflection.Emit.SignatureHelper); }
        public static System.Reflection.Emit.SignatureHelper GetLocalVarSigHelper(System.Reflection.Module mod) { return default(System.Reflection.Emit.SignatureHelper); }
        public static System.Reflection.Emit.SignatureHelper GetMethodSigHelper(System.Reflection.CallingConventions callingConvention, System.Type returnType) { return default(System.Reflection.Emit.SignatureHelper); }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.SignatureHelper GetMethodSigHelper(System.Reflection.Module mod, System.Reflection.CallingConventions callingConvention, System.Type returnType) { return default(System.Reflection.Emit.SignatureHelper); }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.SignatureHelper GetMethodSigHelper(System.Reflection.Module mod, System.Type returnType, System.Type[] parameterTypes) { return default(System.Reflection.Emit.SignatureHelper); }
        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Reflection.Emit.SignatureHelper GetPropertySigHelper(System.Reflection.Module mod, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] requiredReturnTypeCustomModifiers, System.Type[] optionalReturnTypeCustomModifiers, System.Type[] parameterTypes, System.Type[][] requiredParameterTypeCustomModifiers, System.Type[][] optionalParameterTypeCustomModifiers) { return default(System.Reflection.Emit.SignatureHelper); }
        public static System.Reflection.Emit.SignatureHelper GetPropertySigHelper(System.Reflection.Module mod, System.Type returnType, System.Type[] parameterTypes) { return default(System.Reflection.Emit.SignatureHelper); }
        public static System.Reflection.Emit.SignatureHelper GetPropertySigHelper(System.Reflection.Module mod, System.Type returnType, System.Type[] requiredReturnTypeCustomModifiers, System.Type[] optionalReturnTypeCustomModifiers, System.Type[] parameterTypes, System.Type[][] requiredParameterTypeCustomModifiers, System.Type[][] optionalParameterTypeCustomModifiers) { return default(System.Reflection.Emit.SignatureHelper); }
        public byte[] GetSignature() { return default(byte[]); }
        public override string ToString() { return default(string); }
    }
}
