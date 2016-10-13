// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Reflection.Emit
{
    public sealed partial class AssemblyBuilder : System.Reflection.Assembly
    {
        internal AssemblyBuilder() { }
        public override string FullName { get { throw null; } }
        public override bool IsDynamic { get { throw null; } }
        public override System.Reflection.Module ManifestModule { get { throw null; } }
        public static System.Reflection.Emit.AssemblyBuilder DefineDynamicAssembly(System.Reflection.AssemblyName name, System.Reflection.Emit.AssemblyBuilderAccess access) { throw null; }
        public static System.Reflection.Emit.AssemblyBuilder DefineDynamicAssembly(System.Reflection.AssemblyName name, System.Reflection.Emit.AssemblyBuilderAccess access, System.Collections.Generic.IEnumerable<System.Reflection.Emit.CustomAttributeBuilder> assemblyAttributes) { throw null; }
        public System.Reflection.Emit.ModuleBuilder DefineDynamicModule(string name) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public System.Reflection.Emit.ModuleBuilder GetDynamicModule(string name) { throw null; }
        public override int GetHashCode() { throw null; }
        public override System.Reflection.ManifestResourceInfo GetManifestResourceInfo(string resourceName) { throw null; }
        public override string[] GetManifestResourceNames() { throw null; }
        public override System.IO.Stream GetManifestResourceStream(string name) { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
    }
    [System.FlagsAttribute]
    public enum AssemblyBuilderAccess
    {
        Run = 1,
        RunAndCollect = 9,
    }
    public sealed partial class ConstructorBuilder : System.Reflection.ConstructorInfo
    {
        internal ConstructorBuilder() { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        public override string Name { get { throw null; } }
        public System.Reflection.Emit.ParameterBuilder DefineParameter(int iSequence, System.Reflection.ParameterAttributes attributes, string strParamName) { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator(int streamSize) { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetImplementationFlags(System.Reflection.MethodImplAttributes attributes) { }
        public override string ToString() { throw null; }
    }
    public sealed partial class EnumBuilder : System.Reflection.TypeInfo
    {
        internal EnumBuilder() { }
        public override string AssemblyQualifiedName { get { throw null; } }
        public override string FullName { get { throw null; } }
        public override string Name { get { throw null; } }
        public System.Reflection.Emit.FieldBuilder UnderlyingField { get { throw null; } }
        public System.Reflection.TypeInfo CreateTypeInfo() { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineLiteral(string literalName, object literalValue) { throw null; }
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo) { throw null; }
        public override System.Type MakeArrayType() { throw null; }
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        public override System.Type MakePointerType() { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
    }
    public sealed partial class EventBuilder
    {
        internal EventBuilder() { }
        public void AddOtherMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public void SetAddOnMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetRaiseMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public void SetRemoveOnMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
    }
    public sealed partial class FieldBuilder : System.Reflection.FieldInfo
    {
        internal FieldBuilder() { }
        public override System.Reflection.FieldAttributes Attributes { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public override System.Type FieldType { get { throw null; } }
        public override string Name { get { throw null; } }
        public override object GetValue(object obj) { throw null; }
        public void SetConstant(object defaultValue) { }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetOffset(int iOffset) { }
    }
    public sealed partial class GenericTypeParameterBuilder : System.Reflection.TypeInfo
    {
        internal GenericTypeParameterBuilder() { }
        public override string AssemblyQualifiedName { get { throw null; } }
        public override string FullName { get { throw null; } }
        public override bool IsGenericParameter { get { throw null; } }
        public override string Name { get { throw null; } }
        public override bool Equals(object o) { throw null; }
        public override System.Type GetGenericTypeDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo) { throw null; }
        public override System.Type MakeArrayType() { throw null; }
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        public override System.Type MakeGenericType(params System.Type[] typeArguments) { throw null; }
        public override System.Type MakePointerType() { throw null; }
        public void SetBaseTypeConstraint(System.Type baseTypeConstraint) { }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetGenericParameterAttributes(System.Reflection.GenericParameterAttributes genericParameterAttributes) { }
        public void SetInterfaceConstraints(params System.Type[] interfaceConstraints) { }
        public override string ToString() { throw null; }
    }
    public sealed partial class MethodBuilder : System.Reflection.MethodInfo
    {
        internal MethodBuilder() { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override bool ContainsGenericParameters { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        public override bool IsGenericMethod { get { throw null; } }
        public override bool IsGenericMethodDefinition { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public override System.Type ReturnType { get { throw null; } }
        public System.Reflection.Emit.GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names) { throw null; }
        public System.Reflection.Emit.ParameterBuilder DefineParameter(int position, System.Reflection.ParameterAttributes attributes, string strParamName) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override System.Type[] GetGenericArguments() { throw null; }
        public override System.Reflection.MethodInfo GetGenericMethodDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator(int size) { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        public override System.Reflection.MethodInfo MakeGenericMethod(params System.Type[] typeArguments) { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetImplementationFlags(System.Reflection.MethodImplAttributes attributes) { }
        public void SetParameters(params System.Type[] parameterTypes) { }
        public void SetReturnType(System.Type returnType) { }
        public void SetSignature(System.Type returnType, System.Type[] returnTypeRequiredCustomModifiers, System.Type[] returnTypeOptionalCustomModifiers, System.Type[] parameterTypes, System.Type[][] parameterTypeRequiredCustomModifiers, System.Type[][] parameterTypeOptionalCustomModifiers) { }
        public override string ToString() { throw null; }
    }
    public partial class ModuleBuilder : System.Reflection.Module
    {
        internal ModuleBuilder() { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string FullyQualifiedName { get { throw null; } }
        public override string Name { get { throw null; } }
        public void CreateGlobalFunctions() { }
        public System.Reflection.Emit.EnumBuilder DefineEnum(string name, System.Reflection.TypeAttributes visibility, System.Type underlyingType) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] requiredReturnTypeCustomModifiers, System.Type[] optionalReturnTypeCustomModifiers, System.Type[] parameterTypes, System.Type[][] requiredParameterTypeCustomModifiers, System.Type[][] optionalParameterTypeCustomModifiers) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineInitializedData(string name, byte[] data, System.Reflection.FieldAttributes attributes) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent, int typesize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Reflection.Emit.PackingSize packsize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Reflection.Emit.PackingSize packingSize, int typesize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Type[] interfaces) { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineUninitializedData(string name, int size, System.Reflection.FieldAttributes attributes) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public System.Reflection.MethodInfo GetArrayMethod(System.Type arrayClass, string methodName, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public override int GetHashCode() { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
    }
    public sealed partial class PropertyBuilder : System.Reflection.PropertyInfo
    {
        internal PropertyBuilder() { }
        public override System.Reflection.PropertyAttributes Attributes { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override System.Type DeclaringType { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type PropertyType { get { throw null; } }
        public void AddOtherMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public override System.Reflection.ParameterInfo[] GetIndexParameters() { throw null; }
        public override object GetValue(object obj, object[] index) { throw null; }
        public void SetConstant(object defaultValue) { }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetGetMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public void SetSetMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        public override void SetValue(object obj, object value, object[] index) { }
    }
    public sealed partial class TypeBuilder : System.Reflection.TypeInfo
    {
        internal TypeBuilder() { }
        public const int UnspecifiedTypeSize = 0;
        public override string AssemblyQualifiedName { get { throw null; } }
        public override string FullName { get { throw null; } }
        public override bool IsGenericParameter { get { throw null; } }
        public override string Name { get { throw null; } }
        public System.Reflection.Emit.PackingSize PackingSize { get { throw null; } }
        public int Size { get { throw null; } }
        public void AddInterfaceImplementation(System.Type interfaceType) { }
        public System.Reflection.TypeInfo CreateTypeInfo() { throw null; }
        public System.Reflection.Emit.ConstructorBuilder DefineConstructor(System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.ConstructorBuilder DefineConstructor(System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type[] parameterTypes, System.Type[][] requiredCustomModifiers, System.Type[][] optionalCustomModifiers) { throw null; }
        public System.Reflection.Emit.ConstructorBuilder DefineDefaultConstructor(System.Reflection.MethodAttributes attributes) { throw null; }
        public System.Reflection.Emit.EventBuilder DefineEvent(string name, System.Reflection.EventAttributes attributes, System.Type eventtype) { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineField(string fieldName, System.Type type, System.Reflection.FieldAttributes attributes) { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineField(string fieldName, System.Type type, System.Type[] requiredCustomModifiers, System.Type[] optionalCustomModifiers, System.Reflection.FieldAttributes attributes) { throw null; }
        public System.Reflection.Emit.GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names) { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineInitializedData(string name, byte[] data, System.Reflection.FieldAttributes attributes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] returnTypeRequiredCustomModifiers, System.Type[] returnTypeOptionalCustomModifiers, System.Type[] parameterTypes, System.Type[][] parameterTypeRequiredCustomModifiers, System.Type[][] parameterTypeOptionalCustomModifiers) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public void DefineMethodOverride(System.Reflection.MethodInfo methodInfoBody, System.Reflection.MethodInfo methodInfoDeclaration) { }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent, int typeSize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Reflection.Emit.PackingSize packSize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Reflection.Emit.PackingSize packSize, int typeSize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, System.Type parent, System.Type[] interfaces) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[] returnTypeRequiredCustomModifiers, System.Type[] returnTypeOptionalCustomModifiers, System.Type[] parameterTypes, System.Type[][] parameterTypeRequiredCustomModifiers, System.Type[][] parameterTypeOptionalCustomModifiers) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Type returnType, System.Type[] parameterTypes) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Type returnType, System.Type[] returnTypeRequiredCustomModifiers, System.Type[] returnTypeOptionalCustomModifiers, System.Type[] parameterTypes, System.Type[][] parameterTypeRequiredCustomModifiers, System.Type[][] parameterTypeOptionalCustomModifiers) { throw null; }
        public System.Reflection.Emit.ConstructorBuilder DefineTypeInitializer() { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineUninitializedData(string name, int size, System.Reflection.FieldAttributes attributes) { throw null; }
        public static System.Reflection.ConstructorInfo GetConstructor(System.Type type, System.Reflection.ConstructorInfo constructor) { throw null; }
        public static System.Reflection.FieldInfo GetField(System.Type type, System.Reflection.FieldInfo field) { throw null; }
        public override System.Type GetGenericTypeDefinition() { throw null; }
        public static System.Reflection.MethodInfo GetMethod(System.Type type, System.Reflection.MethodInfo method) { throw null; }
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo) { throw null; }
        public bool IsCreated() { throw null; }
        public override System.Type MakeArrayType() { throw null; }
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        public override System.Type MakeGenericType(params System.Type[] typeArguments) { throw null; }
        public override System.Type MakePointerType() { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        public void SetParent(System.Type parent) { }
        public override string ToString() { throw null; }
    }
}
