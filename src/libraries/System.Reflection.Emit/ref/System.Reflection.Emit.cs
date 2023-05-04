// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection.Emit
{
    public abstract partial class AssemblyBuilder : System.Reflection.Assembly
    {
        protected AssemblyBuilder() { }
        [System.ObsoleteAttribute("Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location instead.", DiagnosticId = "SYSLIB0012", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute("This member throws an exception for assemblies embedded in a single-file app")]
        public override string? CodeBase { get { throw null; } }
        public override System.Reflection.MethodInfo? EntryPoint { get { throw null; } }
        public override string? FullName { get { throw null; } }
        public override long HostContext { get { throw null; } }
        public override bool IsCollectible { get { throw null; } }
        public override bool IsDynamic { get { throw null; } }
        public override string Location { get { throw null; } }
        public override System.Reflection.Module ManifestModule { get { throw null; } }
        public override bool ReflectionOnly { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
        public static System.Reflection.Emit.AssemblyBuilder DefineDynamicAssembly(System.Reflection.AssemblyName name, System.Reflection.Emit.AssemblyBuilderAccess access) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
        public static System.Reflection.Emit.AssemblyBuilder DefineDynamicAssembly(System.Reflection.AssemblyName name, System.Reflection.Emit.AssemblyBuilderAccess access, System.Collections.Generic.IEnumerable<System.Reflection.Emit.CustomAttributeBuilder>? assemblyAttributes) { throw null; }
        public System.Reflection.Emit.ModuleBuilder DefineDynamicModule(string name) { throw null; }
        protected abstract System.Reflection.Emit.ModuleBuilder DefineDynamicModuleCore(string name);
        public override bool Equals(object? obj) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public System.Reflection.Emit.ModuleBuilder? GetDynamicModule(string name) { throw null; }
        protected abstract System.Reflection.Emit.ModuleBuilder? GetDynamicModuleCore(string name);
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public override System.Type[] GetExportedTypes() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute("This member throws an exception for assemblies embedded in a single-file app")]
        public override System.IO.FileStream GetFile(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute("This member throws an exception for assemblies embedded in a single-file app")]
        public override System.IO.FileStream[] GetFiles(bool getResourceModules) { throw null; }
        public override int GetHashCode() { throw null; }
        public override System.Reflection.Module[] GetLoadedModules(bool getResourceModules) { throw null; }
        public override System.Reflection.ManifestResourceInfo? GetManifestResourceInfo(string resourceName) { throw null; }
        public override string[] GetManifestResourceNames() { throw null; }
        public override System.IO.Stream? GetManifestResourceStream(string name) { throw null; }
        public override System.IO.Stream? GetManifestResourceStream(System.Type type, string name) { throw null; }
        public override System.Reflection.Module? GetModule(string name) { throw null; }
        public override System.Reflection.Module[] GetModules(bool getResourceModules) { throw null; }
        public override System.Reflection.AssemblyName GetName(bool copiedName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Assembly references might be removed")]
        public override System.Reflection.AssemblyName[] GetReferencedAssemblies() { throw null; }
        public override System.Reflection.Assembly GetSatelliteAssembly(System.Globalization.CultureInfo culture) { throw null; }
        public override System.Reflection.Assembly GetSatelliteAssembly(System.Globalization.CultureInfo culture, System.Version? version) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public override System.Type? GetType(string name, bool throwOnError, bool ignoreCase) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
    }
    [System.FlagsAttribute]
    public enum AssemblyBuilderAccess
    {
        Run = 1,
        RunAndCollect = 9,
    }
    public abstract partial class ConstructorBuilder : System.Reflection.ConstructorInfo
    {
        protected ConstructorBuilder() { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        protected abstract bool InitLocalsCore { get; set; }
        public override int MetadataToken { get { throw null; } }
        public override System.RuntimeMethodHandle MethodHandle { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type? ReflectedType { get { throw null; } }
        public System.Reflection.Emit.ParameterBuilder DefineParameter(int iSequence, System.Reflection.ParameterAttributes attributes, string? strParamName) { throw null; }
        protected abstract System.Reflection.Emit.ParameterBuilder DefineParameterCore(int iSequence, System.Reflection.ParameterAttributes attributes, string strParamName);
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator(int streamSize) { throw null; }
        protected abstract System.Reflection.Emit.ILGenerator GetILGeneratorCore(int streamSize);
        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        public override object Invoke(object? obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) { throw null; }
        public override object Invoke(System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
        public void SetImplementationFlags(System.Reflection.MethodImplAttributes attributes) { }
        protected abstract void SetImplementationFlagsCore(System.Reflection.MethodImplAttributes attributes);
        public override string ToString() { throw null; }
    }
    public abstract partial class EnumBuilder : System.Reflection.TypeInfo
    {
        protected EnumBuilder() { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string? AssemblyQualifiedName { get { throw null; } }
        public override System.Type? BaseType { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public override string? FullName { get { throw null; } }
        public override System.Guid GUID { get { throw null; } }
        public override bool IsByRefLike { get { throw null; } }
        public override bool IsConstructedGenericType { get { throw null; } }
        public override bool IsSZArray { get { throw null; } }
        public override bool IsTypeDefinition { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string? Namespace { get { throw null; } }
        public override System.Type? ReflectedType { get { throw null; } }
        public override System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public System.Reflection.Emit.FieldBuilder UnderlyingField { get { throw null; } }
        protected abstract System.Reflection.Emit.FieldBuilder UnderlyingFieldCore { get; }
        public override System.Type UnderlyingSystemType { get { throw null; } }
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public System.Type CreateType() { throw null; }
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public System.Reflection.TypeInfo CreateTypeInfo() { throw null; }
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        protected abstract System.Reflection.TypeInfo CreateTypeInfoCore();
        public System.Reflection.Emit.FieldBuilder DefineLiteral(string literalName, object? literalValue) { throw null; }
        protected abstract System.Reflection.Emit.FieldBuilder DefineLiteralCore(string literalName, object? literalValue);
        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        protected override System.Reflection.ConstructorInfo? GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type? GetElementType() { throw null; }
        public override System.Type GetEnumUnderlyingType() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo? GetEvent(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo[] GetEvents() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public override System.Reflection.FieldInfo? GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        public override System.Type? GetInterface(string name, bool ignoreCase) { throw null; }
        public override System.Reflection.InterfaceMapping GetInterfaceMap([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] System.Type interfaceType) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        public override System.Type[] GetInterfaces() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.MemberTypes type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        protected override System.Reflection.MethodInfo? GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[]? types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public override System.Type? GetNestedType(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public override System.Type[] GetNestedTypes(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        protected override System.Reflection.PropertyInfo GetPropertyImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Type? returnType, System.Type[]? types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        protected override bool HasElementTypeImpl() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object? target, object?[]? args, System.Reflection.ParameterModifier[]? modifiers, System.Globalization.CultureInfo? culture, string[]? namedParameters) { throw null; }
        protected override bool IsArrayImpl() { throw null; }
        public override bool IsAssignableFrom([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Reflection.TypeInfo? typeInfo) { throw null; }
        protected override bool IsByRefImpl() { throw null; }
        protected override bool IsCOMObjectImpl() { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        protected override bool IsPointerImpl() { throw null; }
        protected override bool IsPrimitiveImpl() { throw null; }
        protected override bool IsValueTypeImpl() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The code for an array of the specified type might not be available.")]
        public override System.Type MakeArrayType() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The code for an array of the specified type might not be available.")]
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        public override System.Type MakePointerType() { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
    }
    public abstract partial class EventBuilder
    {
        protected EventBuilder() { }
        public void AddOtherMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        protected abstract void AddOtherMethodCore(System.Reflection.Emit.MethodBuilder mdBuilder);
        public void SetAddOnMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        protected abstract void SetAddOnMethodCore(System.Reflection.Emit.MethodBuilder mdBuilder);
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
        public void SetRaiseMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        protected abstract void SetRaiseMethodCore(System.Reflection.Emit.MethodBuilder mdBuilder);
        public void SetRemoveOnMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        protected abstract void SetRemoveOnMethodCore(System.Reflection.Emit.MethodBuilder mdBuilder);
    }
    public abstract partial class FieldBuilder : System.Reflection.FieldInfo
    {
        protected FieldBuilder() { }
        public override System.Reflection.FieldAttributes Attributes { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public override System.RuntimeFieldHandle FieldHandle { get { throw null; } }
        public override System.Type FieldType { get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type? ReflectedType { get { throw null; } }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override object? GetValue(object? obj) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public void SetConstant(object? defaultValue) { }
        protected abstract void SetConstantCore(object? defaultValue);
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
        public void SetOffset(int iOffset) { }
        protected abstract void SetOffsetCore(int iOffset);
        public override void SetValue(object? obj, object? val, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, System.Globalization.CultureInfo? culture) { }
    }
    public abstract partial class GenericTypeParameterBuilder : System.Reflection.TypeInfo
    {
        protected GenericTypeParameterBuilder() { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string? AssemblyQualifiedName { get { throw null; } }
        public override System.Type? BaseType { get { throw null; } }
        public override bool ContainsGenericParameters { get { throw null; } }
        public override System.Reflection.MethodBase? DeclaringMethod { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public override string? FullName { get { throw null; } }
        public override System.Reflection.GenericParameterAttributes GenericParameterAttributes { get { throw null; } }
        public override int GenericParameterPosition { get { throw null; } }
        public override System.Guid GUID { get { throw null; } }
        public override bool IsByRefLike { get { throw null; } }
        public override bool IsConstructedGenericType { get { throw null; } }
        public override bool IsGenericParameter { get { throw null; } }
        public override bool IsGenericType { get { throw null; } }
        public override bool IsGenericTypeDefinition { get { throw null; } }
        public override bool IsSZArray { get { throw null; } }
        public override bool IsTypeDefinition { get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string? Namespace { get { throw null; } }
        public override System.Type? ReflectedType { get { throw null; } }
        public override System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public override System.Type UnderlyingSystemType { get { throw null; } }
        public override bool Equals(object? o) { throw null; }
        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        protected override System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type GetElementType() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo GetEvent(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo[] GetEvents() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public override System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type[] GetGenericArguments() { throw null; }
        public override System.Type GetGenericTypeDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        public override System.Type GetInterface(string name, bool ignoreCase) { throw null; }
        public override System.Reflection.InterfaceMapping GetInterfaceMap([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] System.Type interfaceType) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        public override System.Type[] GetInterfaces() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.MemberTypes type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        protected override System.Reflection.MethodInfo GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[]? types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public override System.Type GetNestedType(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public override System.Type[] GetNestedTypes(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        protected override System.Reflection.PropertyInfo GetPropertyImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Type? returnType, System.Type[]? types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        protected override bool HasElementTypeImpl() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public override object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object? target, object?[]? args, System.Reflection.ParameterModifier[]? modifiers, System.Globalization.CultureInfo? culture, string[]? namedParameters) { throw null; }
        protected override bool IsArrayImpl() { throw null; }
        public override bool IsAssignableFrom([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Reflection.TypeInfo? typeInfo) { throw null; }
        public override bool IsAssignableFrom([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Type? c) { throw null; }
        protected override bool IsByRefImpl() { throw null; }
        protected override bool IsCOMObjectImpl() { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        protected override bool IsPointerImpl() { throw null; }
        protected override bool IsPrimitiveImpl() { throw null; }
        public override bool IsSubclassOf(System.Type c) { throw null; }
        protected override bool IsValueTypeImpl() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The code for an array of the specified type might not be available.")]
        public override System.Type MakeArrayType() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The code for an array of the specified type might not be available.")]
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The native code for this instantiation might not be available at runtime.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override System.Type MakeGenericType(params System.Type[] typeArguments) { throw null; }
        public override System.Type MakePointerType() { throw null; }
        public void SetBaseTypeConstraint([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? baseTypeConstraint) { }
        protected abstract void SetBaseTypeConstraintCore([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] Type? baseTypeConstraint);
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
        public void SetGenericParameterAttributes(System.Reflection.GenericParameterAttributes genericParameterAttributes) { }
        protected abstract void SetGenericParameterAttributesCore(System.Reflection.GenericParameterAttributes genericParameterAttributes);
        public void SetInterfaceConstraints(params System.Type[]? interfaceConstraints) { }
        protected abstract void SetInterfaceConstraintsCore(params System.Type[]? interfaceConstraints);
        public override string ToString() { throw null; }
    }
    public abstract partial class MethodBuilder : System.Reflection.MethodInfo
    {
        protected MethodBuilder() { }
        public override System.Reflection.MethodAttributes Attributes { get { throw null; } }
        public override System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public override bool ContainsGenericParameters { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public bool InitLocals { get { throw null; } set { } }
        protected abstract bool InitLocalsCore { get; set; }
        public override bool IsGenericMethod { get { throw null; } }
        public override bool IsGenericMethodDefinition { get { throw null; } }
        public override bool IsSecurityCritical { get { throw null; } }
        public override bool IsSecuritySafeCritical { get { throw null; } }
        public override bool IsSecurityTransparent { get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override System.RuntimeMethodHandle MethodHandle { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type? ReflectedType { get { throw null; } }
        public override System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public override System.Type ReturnType { get { throw null; } }
        public override System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes { get { throw null; } }
        public System.Reflection.Emit.GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names) { throw null; }
        protected abstract System.Reflection.Emit.GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names);
        public System.Reflection.Emit.ParameterBuilder DefineParameter(int position, System.Reflection.ParameterAttributes attributes, string? strParamName) { throw null; }
        protected abstract System.Reflection.Emit.ParameterBuilder DefineParameterCore(int position, System.Reflection.ParameterAttributes attributes, string? strParamName);
        public override bool Equals(object? obj) { throw null; }
        public override System.Reflection.MethodInfo GetBaseDefinition() { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type[] GetGenericArguments() { throw null; }
        public override System.Reflection.MethodInfo GetGenericMethodDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator() { throw null; }
        public System.Reflection.Emit.ILGenerator GetILGenerator(int size) { throw null; }
        protected abstract System.Reflection.Emit.ILGenerator GetILGeneratorCore(int size);
        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags() { throw null; }
        public override System.Reflection.ParameterInfo[] GetParameters() { throw null; }
        public override object Invoke(object? obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override System.Reflection.MethodInfo MakeGenericMethod(params System.Type[] typeArguments) { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
        public void SetImplementationFlags(System.Reflection.MethodImplAttributes attributes) { }
        protected abstract void SetImplementationFlagsCore(System.Reflection.MethodImplAttributes attributes);
        public void SetParameters(params System.Type[] parameterTypes) { }
        public void SetReturnType(System.Type? returnType) { }
        public void SetSignature(System.Type? returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers) { }
        protected abstract void SetSignatureCore(System.Type? returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers);
        public override string ToString() { throw null; }
    }
    public abstract partial class ModuleBuilder : System.Reflection.Module
    {
        protected ModuleBuilder() { }
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute("Returns <Unknown> for modules with no file path")]
        public override string FullyQualifiedName { get { throw null; } }
        public override int MDStreamVersion { get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override System.Guid ModuleVersionId { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute("Returns <Unknown> for modules with no file path")]
        public override string Name { get { throw null; } }
        public override string ScopeName { get { throw null; } }
        public void CreateGlobalFunctions() { }
        protected abstract void CreateGlobalFunctionsCore();
        public System.Reflection.Emit.EnumBuilder DefineEnum(string name, System.Reflection.TypeAttributes visibility, System.Type underlyingType) { throw null; }
        protected abstract System.Reflection.Emit.EnumBuilder DefineEnumCore(string name, System.Reflection.TypeAttributes visibility, System.Type underlyingType);
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? requiredReturnTypeCustomModifiers, System.Type[]? optionalReturnTypeCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? requiredParameterTypeCustomModifiers, System.Type[][]? optionalParameterTypeCustomModifiers) { throw null; }
        protected abstract System.Reflection.Emit.MethodBuilder DefineGlobalMethodCore(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? requiredReturnTypeCustomModifiers, System.Type[]? optionalReturnTypeCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? requiredParameterTypeCustomModifiers, System.Type[][]? optionalParameterTypeCustomModifiers);
        public System.Reflection.Emit.MethodBuilder DefineGlobalMethod(string name, System.Reflection.MethodAttributes attributes, System.Type? returnType, System.Type[]? parameterTypes) { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineInitializedData(string name, byte[] data, System.Reflection.FieldAttributes attributes) { throw null; }
        protected abstract System.Reflection.Emit.FieldBuilder DefineInitializedDataCore(string name, byte[] data, System.Reflection.FieldAttributes attributes);
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public System.Reflection.Emit.MethodBuilder DefinePInvokeMethod(string name, string dllName, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes, System.Runtime.InteropServices.CallingConvention nativeCallConv, System.Runtime.InteropServices.CharSet nativeCharSet) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public System.Reflection.Emit.MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes, System.Runtime.InteropServices.CallingConvention nativeCallConv, System.Runtime.InteropServices.CharSet nativeCharSet) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected abstract System.Reflection.Emit.MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes, System.Runtime.InteropServices.CallingConvention nativeCallConv, System.Runtime.InteropServices.CharSet nativeCharSet);
        public System.Reflection.Emit.TypeBuilder DefineType(string name) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, int typesize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, System.Reflection.Emit.PackingSize packsize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, System.Reflection.Emit.PackingSize packingSize, int typesize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, System.Type[]? interfaces) { throw null; }
        protected abstract System.Reflection.Emit.TypeBuilder DefineTypeCore(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, System.Type[]? interfaces, System.Reflection.Emit.PackingSize packingSize, int typesize);
        public System.Reflection.Emit.FieldBuilder DefineUninitializedData(string name, int size, System.Reflection.FieldAttributes attributes) { throw null; }
        protected abstract System.Reflection.Emit.FieldBuilder DefineUninitializedDataCore(string name, int size, System.Reflection.FieldAttributes attributes);
        public override bool Equals(object? obj) { throw null; }
        public System.Reflection.MethodInfo GetArrayMethod(System.Type arrayClass, string methodName, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes) { throw null; }
        protected abstract System.Reflection.MethodInfo GetArrayMethodCore(System.Type arrayClass, string methodName, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes);
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Fields might be removed")]
        public override System.Reflection.FieldInfo? GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Fields might be removed")]
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingFlags) { throw null; }
        public abstract int GetFieldMetadataToken(System.Reflection.FieldInfo field);
        public override int GetHashCode() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Methods might be removed")]
        protected override System.Reflection.MethodInfo? GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[]? types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        public abstract int GetMethodMetadataToken(System.Reflection.ConstructorInfo constructor);
        public abstract int GetMethodMetadataToken(System.Reflection.MethodInfo method);
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Methods might be removed")]
        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingFlags) { throw null; }
        public override void GetPEKind(out System.Reflection.PortableExecutableKinds peKind, out System.Reflection.ImageFileMachine machine) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public override System.Type? GetType(string className) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public override System.Type? GetType(string className, bool ignoreCase) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public override System.Type? GetType(string className, bool throwOnError, bool ignoreCase) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public override System.Type[] GetTypes() { throw null; }
        public abstract int GetTypeMetadataToken(System.Type type);
        public abstract int GetSignatureMetadataToken(System.Reflection.Emit.SignatureHelper signature);
        public abstract int GetStringMetadataToken(string stringConstant);
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public override bool IsResource() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public override System.Reflection.FieldInfo? ResolveField(int metadataToken, System.Type[]? genericTypeArguments, System.Type[]? genericMethodArguments) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public override System.Reflection.MemberInfo? ResolveMember(int metadataToken, System.Type[]? genericTypeArguments, System.Type[]? genericMethodArguments) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public override System.Reflection.MethodBase? ResolveMethod(int metadataToken, System.Type[]? genericTypeArguments, System.Type[]? genericMethodArguments) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public override byte[] ResolveSignature(int metadataToken) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public override string ResolveString(int metadataToken) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public override System.Type ResolveType(int metadataToken, System.Type[]? genericTypeArguments, System.Type[]? genericMethodArguments) { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
    }
    public abstract partial class PropertyBuilder : System.Reflection.PropertyInfo
    {
        protected PropertyBuilder() { }
        public override System.Reflection.PropertyAttributes Attributes { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type PropertyType { get { throw null; } }
        public override System.Type? ReflectedType { get { throw null; } }
        public void AddOtherMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        protected abstract void AddOtherMethodCore(System.Reflection.Emit.MethodBuilder mdBuilder);
        public override System.Reflection.MethodInfo[] GetAccessors(bool nonPublic) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Reflection.MethodInfo? GetGetMethod(bool nonPublic) { throw null; }
        public override System.Reflection.ParameterInfo[] GetIndexParameters() { throw null; }
        public override System.Reflection.MethodInfo? GetSetMethod(bool nonPublic) { throw null; }
        public override object GetValue(object? obj, object?[]? index) { throw null; }
        public override object GetValue(object? obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? index, System.Globalization.CultureInfo? culture) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public void SetConstant(object? defaultValue) { }
        protected abstract void SetConstantCore(object? defaultValue);
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
        public void SetGetMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        protected abstract void SetGetMethodCore(System.Reflection.Emit.MethodBuilder mdBuilder);
        public void SetSetMethod(System.Reflection.Emit.MethodBuilder mdBuilder) { }
        protected abstract void SetSetMethodCore(System.Reflection.Emit.MethodBuilder mdBuilder);
        public override void SetValue(object? obj, object? value, object?[]? index) { }
        public override void SetValue(object? obj, object? value, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? index, System.Globalization.CultureInfo? culture) { }
    }
    public abstract partial class TypeBuilder : System.Reflection.TypeInfo
    {
        protected TypeBuilder() { }
        public const int UnspecifiedTypeSize = 0;
        public override System.Reflection.Assembly Assembly { get { throw null; } }
        public override string? AssemblyQualifiedName { get { throw null; } }
        public override System.Type? BaseType { get { throw null; } }
        public override System.Reflection.MethodBase? DeclaringMethod { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public override string? FullName { get { throw null; } }
        public override System.Reflection.GenericParameterAttributes GenericParameterAttributes { get { throw null; } }
        public override int GenericParameterPosition { get { throw null; } }
        public override System.Guid GUID { get { throw null; } }
        public override bool IsByRefLike { get { throw null; } }
        public override bool IsConstructedGenericType { get { throw null; } }
        public override bool IsGenericParameter { get { throw null; } }
        public override bool IsGenericType { get { throw null; } }
        public override bool IsGenericTypeDefinition { get { throw null; } }
        public override bool IsSecurityCritical { get { throw null; } }
        public override bool IsSecuritySafeCritical { get { throw null; } }
        public override bool IsSecurityTransparent { get { throw null; } }
        public override bool IsSZArray { get { throw null; } }
        public override bool IsTypeDefinition { get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string? Namespace { get { throw null; } }
        public System.Reflection.Emit.PackingSize PackingSize { get { throw null; } }
        protected abstract System.Reflection.Emit.PackingSize PackingSizeCore { get; }
        public override System.Type? ReflectedType { get { throw null; } }
        public int Size { get { throw null; } }
        protected abstract int SizeCore { get; }
        public override System.RuntimeTypeHandle TypeHandle { get { throw null; } }
        public override System.Type UnderlyingSystemType { get { throw null; } }
        public void AddInterfaceImplementation([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type interfaceType) { }
        protected abstract void AddInterfaceImplementationCore([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type interfaceType);
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public System.Type CreateType() { throw null; }
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public System.Reflection.TypeInfo CreateTypeInfo() { throw null; }
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        protected abstract System.Reflection.TypeInfo CreateTypeInfoCore();
        public System.Reflection.Emit.ConstructorBuilder DefineConstructor(System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type[]? parameterTypes) { throw null; }
        public System.Reflection.Emit.ConstructorBuilder DefineConstructor(System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type[]? parameterTypes, System.Type[][]? requiredCustomModifiers, System.Type[][]? optionalCustomModifiers) { throw null; }
        protected abstract System.Reflection.Emit.ConstructorBuilder DefineConstructorCore(System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type[]? parameterTypes, System.Type[][]? requiredCustomModifiers, System.Type[][]? optionalCustomModifiers);
        public System.Reflection.Emit.ConstructorBuilder DefineDefaultConstructor(System.Reflection.MethodAttributes attributes) { throw null; }
        protected abstract System.Reflection.Emit.ConstructorBuilder DefineDefaultConstructorCore(System.Reflection.MethodAttributes attributes);
        public System.Reflection.Emit.EventBuilder DefineEvent(string name, System.Reflection.EventAttributes attributes, System.Type eventtype) { throw null; }
        protected abstract System.Reflection.Emit.EventBuilder DefineEventCore(string name, System.Reflection.EventAttributes attributes, System.Type eventtype);
        public System.Reflection.Emit.FieldBuilder DefineField(string fieldName, System.Type type, System.Reflection.FieldAttributes attributes) { throw null; }
        public System.Reflection.Emit.FieldBuilder DefineField(string fieldName, System.Type type, System.Type[]? requiredCustomModifiers, System.Type[]? optionalCustomModifiers, System.Reflection.FieldAttributes attributes) { throw null; }
        protected abstract System.Reflection.Emit.FieldBuilder DefineFieldCore(string fieldName, System.Type type, System.Type[]? requiredCustomModifiers, System.Type[]? optionalCustomModifiers, System.Reflection.FieldAttributes attributes);
        public System.Reflection.Emit.GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names) { throw null; }
        protected abstract System.Reflection.Emit.GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names);
        public System.Reflection.Emit.FieldBuilder DefineInitializedData(string name, byte[] data, System.Reflection.FieldAttributes attributes) { throw null; }
        protected abstract System.Reflection.Emit.FieldBuilder DefineInitializedDataCore(string name, byte[] data, System.Reflection.FieldAttributes attributes);
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes) { throw null; }
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers) { throw null; }
        protected abstract System.Reflection.Emit.MethodBuilder DefineMethodCore(string name, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers);
        public System.Reflection.Emit.MethodBuilder DefineMethod(string name, System.Reflection.MethodAttributes attributes, System.Type? returnType, System.Type[]? parameterTypes) { throw null; }
        public void DefineMethodOverride(System.Reflection.MethodInfo methodInfoBody, System.Reflection.MethodInfo methodInfoDeclaration) { }
        protected abstract void DefineMethodOverrideCore(System.Reflection.MethodInfo methodInfoBody, System.Reflection.MethodInfo methodInfoDeclaration);
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, int typeSize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, System.Reflection.Emit.PackingSize packSize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, System.Reflection.Emit.PackingSize packSize, int typeSize) { throw null; }
        public System.Reflection.Emit.TypeBuilder DefineNestedType(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, System.Type[]? interfaces) { throw null; }
        protected abstract System.Reflection.Emit.TypeBuilder DefineNestedTypeCore(string name, System.Reflection.TypeAttributes attr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent, System.Type[]? interfaces, System.Reflection.Emit.PackingSize packSize, int typeSize);
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public System.Reflection.Emit.MethodBuilder DefinePInvokeMethod(string name, string dllName, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes, System.Runtime.InteropServices.CallingConvention nativeCallConv, System.Runtime.InteropServices.CharSet nativeCharSet) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public System.Reflection.Emit.MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? parameterTypes, System.Runtime.InteropServices.CallingConvention nativeCallConv, System.Runtime.InteropServices.CharSet nativeCharSet) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public System.Reflection.Emit.MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers, System.Runtime.InteropServices.CallingConvention nativeCallConv, System.Runtime.InteropServices.CharSet nativeCharSet) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected abstract System.Reflection.Emit.MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, System.Reflection.MethodAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type? returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers, System.Runtime.InteropServices.CallingConvention nativeCallConv, System.Runtime.InteropServices.CharSet nativeCharSet);
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[]? parameterTypes) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Type returnType, System.Type[]? parameterTypes) { throw null; }
        public System.Reflection.Emit.PropertyBuilder DefineProperty(string name, System.Reflection.PropertyAttributes attributes, System.Type returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers) { throw null; }
        protected abstract System.Reflection.Emit.PropertyBuilder DefinePropertyCore(string name, System.Reflection.PropertyAttributes attributes, System.Reflection.CallingConventions callingConvention, System.Type returnType, System.Type[]? returnTypeRequiredCustomModifiers, System.Type[]? returnTypeOptionalCustomModifiers, System.Type[]? parameterTypes, System.Type[][]? parameterTypeRequiredCustomModifiers, System.Type[][]? parameterTypeOptionalCustomModifiers);
        public System.Reflection.Emit.ConstructorBuilder DefineTypeInitializer() { throw null; }
        protected abstract System.Reflection.Emit.ConstructorBuilder DefineTypeInitializerCore();
        public System.Reflection.Emit.FieldBuilder DefineUninitializedData(string name, int size, System.Reflection.FieldAttributes attributes) { throw null; }
        protected abstract System.Reflection.Emit.FieldBuilder DefineUninitializedDataCore(string name, int size, System.Reflection.FieldAttributes attributes);
        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl() { throw null; }
        public static System.Reflection.ConstructorInfo GetConstructor(System.Type type, System.Reflection.ConstructorInfo constructor) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        protected override System.Reflection.ConstructorInfo? GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Type GetElementType() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo? GetEvent(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo[] GetEvents() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public override System.Reflection.FieldInfo? GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.FieldInfo GetField(System.Type type, System.Reflection.FieldInfo field) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public override System.Type[] GetGenericArguments() { throw null; }
        public override System.Type GetGenericTypeDefinition() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        public override System.Type? GetInterface(string name, bool ignoreCase) { throw null; }
        public override System.Reflection.InterfaceMapping GetInterfaceMap([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] System.Type interfaceType) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
        public override System.Type[] GetInterfaces() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.MemberInfo[] GetMember(string name, System.Reflection.MemberTypes type, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr) { throw null; }
        public static System.Reflection.MethodInfo GetMethod(System.Type type, System.Reflection.MethodInfo method) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        protected override System.Reflection.MethodInfo? GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[]? types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public override System.Type? GetNestedType(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public override System.Type[] GetNestedTypes(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public override System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        protected override System.Reflection.PropertyInfo GetPropertyImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Type? returnType, System.Type[]? types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        protected override bool HasElementTypeImpl() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object? target, object?[]? args, System.Reflection.ParameterModifier[]? modifiers, System.Globalization.CultureInfo? culture, string[]? namedParameters) { throw null; }
        protected override bool IsArrayImpl() { throw null; }
        public override bool IsAssignableFrom([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Reflection.TypeInfo? typeInfo) { throw null; }
        public override bool IsAssignableFrom([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Type? c) { throw null; }
        protected override bool IsByRefImpl() { throw null; }
        protected override bool IsCOMObjectImpl() { throw null; }
        public bool IsCreated() { throw null; }
        protected abstract bool IsCreatedCore();
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        protected override bool IsPointerImpl() { throw null; }
        protected override bool IsPrimitiveImpl() { throw null; }
        public override bool IsSubclassOf(System.Type c) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The code for an array of the specified type might not be available.")]
        public override System.Type MakeArrayType() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The code for an array of the specified type might not be available.")]
        public override System.Type MakeArrayType(int rank) { throw null; }
        public override System.Type MakeByRefType() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The native code for this instantiation might not be available at runtime.")]
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override System.Type MakeGenericType(params System.Type[] typeArguments) { throw null; }
        public override System.Type MakePointerType() { throw null; }
        public void SetCustomAttribute(System.Reflection.ConstructorInfo con, byte[] binaryAttribute) { }
        public void SetCustomAttribute(System.Reflection.Emit.CustomAttributeBuilder customBuilder) { }
        protected abstract void SetCustomAttributeCore(System.Reflection.ConstructorInfo con, System.ReadOnlySpan<byte> binaryAttribute);
        public void SetParent([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent) { }
        protected abstract void SetParentCore([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type? parent);
        public override string ToString() { throw null; }
    }
}
