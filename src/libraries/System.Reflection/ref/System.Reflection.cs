// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection
{
    public sealed partial class AmbiguousMatchException : System.SystemException
    {
        public AmbiguousMatchException() { }
        public AmbiguousMatchException(string? message) { }
        public AmbiguousMatchException(string? message, System.Exception? inner) { }
    }
    public abstract partial class Assembly : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.ISerializable
    {
        protected Assembly() { }
        public virtual string? CodeBase { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> DefinedTypes { [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")] get { throw null; } }
        public virtual System.Reflection.MethodInfo? EntryPoint { get { throw null; } }
        public virtual string EscapedCodeBase { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Type> ExportedTypes { [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")] get { throw null; } }
        public virtual string? FullName { get { throw null; } }
        public virtual bool GlobalAssemblyCache { get { throw null; } }
        public virtual long HostContext { get { throw null; } }
        public virtual string ImageRuntimeVersion { get { throw null; } }
        public virtual bool IsCollectible { get { throw null; } }
        public virtual bool IsDynamic { get { throw null; } }
        public bool IsFullyTrusted { get { throw null; } }
        public virtual string Location { get { throw null; } }
        public virtual System.Reflection.Module ManifestModule { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.Module> Modules { get { throw null; } }
        public virtual bool ReflectionOnly { get { throw null; } }
        public virtual System.Security.SecurityRuleSet SecurityRuleSet { get { throw null; } }
        public virtual event System.Reflection.ModuleResolveEventHandler? ModuleResolve { add { } remove { } }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Assembly.CreateInstance is not supported with trimming. Use Type.GetType instead.")]
        public object? CreateInstance(string typeName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Assembly.CreateInstance is not supported with trimming. Use Type.GetType instead.")]
        public object? CreateInstance(string typeName, bool ignoreCase) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Assembly.CreateInstance is not supported with trimming. Use Type.GetType instead.")]
        public virtual object? CreateInstance(string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object[]? args, System.Globalization.CultureInfo? culture, object[]? activationAttributes) { throw null; }
        public static string CreateQualifiedName(string? assemblyName, string? typeName) { throw null; }
        public override bool Equals(object? o) { throw null; }
        public static System.Reflection.Assembly? GetAssembly(System.Type type) { throw null; }
        public static System.Reflection.Assembly GetCallingAssembly() { throw null; }
        public virtual object[] GetCustomAttributes(bool inherit) { throw null; }
        public virtual object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public static System.Reflection.Assembly? GetEntryAssembly() { throw null; }
        public static System.Reflection.Assembly GetExecutingAssembly() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type[] GetExportedTypes() { throw null; }
        public virtual System.IO.FileStream? GetFile(string name) { throw null; }
        public virtual System.IO.FileStream[] GetFiles() { throw null; }
        public virtual System.IO.FileStream[] GetFiles(bool getResourceModules) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type[] GetForwardedTypes() { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Reflection.Module[] GetLoadedModules() { throw null; }
        public virtual System.Reflection.Module[] GetLoadedModules(bool getResourceModules) { throw null; }
        public virtual System.Reflection.ManifestResourceInfo? GetManifestResourceInfo(string resourceName) { throw null; }
        public virtual string[] GetManifestResourceNames() { throw null; }
        public virtual System.IO.Stream? GetManifestResourceStream(string name) { throw null; }
        public virtual System.IO.Stream? GetManifestResourceStream(System.Type type, string name) { throw null; }
        public virtual System.Reflection.Module? GetModule(string name) { throw null; }
        public System.Reflection.Module[] GetModules() { throw null; }
        public virtual System.Reflection.Module[] GetModules(bool getResourceModules) { throw null; }
        public virtual System.Reflection.AssemblyName GetName() { throw null; }
        public virtual System.Reflection.AssemblyName GetName(bool copiedName) { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Assembly references might be removed")]
        public virtual System.Reflection.AssemblyName[] GetReferencedAssemblies() { throw null; }
        public virtual System.Reflection.Assembly GetSatelliteAssembly(System.Globalization.CultureInfo culture) { throw null; }
        public virtual System.Reflection.Assembly GetSatelliteAssembly(System.Globalization.CultureInfo culture, System.Version? version) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type? GetType(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type? GetType(string name, bool throwOnError) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type? GetType(string name, bool throwOnError, bool ignoreCase) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type[] GetTypes() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly Load(byte[] rawAssembly) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly Load(byte[] rawAssembly, byte[]? rawSymbolStore) { throw null; }
        public static System.Reflection.Assembly Load(System.Reflection.AssemblyName assemblyRef) { throw null; }
        public static System.Reflection.Assembly Load(string assemblyString) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly LoadFile(string path) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly LoadFrom(string assemblyFile) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly LoadFrom(string assemblyFile, byte[]? hashValue, System.Configuration.Assemblies.AssemblyHashAlgorithm hashAlgorithm) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded module depends on might be removed")]
        public System.Reflection.Module LoadModule(string moduleName, byte[]? rawModule) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded module depends on might be removed")]
        public virtual System.Reflection.Module LoadModule(string moduleName, byte[]? rawModule, byte[]? rawSymbolStore) { throw null; }
        public static System.Reflection.Assembly? LoadWithPartialName(string partialName) { throw null; }
        public static bool operator ==(System.Reflection.Assembly? left, System.Reflection.Assembly? right) { throw null; }
        public static bool operator !=(System.Reflection.Assembly? left, System.Reflection.Assembly? right) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly ReflectionOnlyLoad(byte[] rawAssembly) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly ReflectionOnlyLoad(string assemblyString) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly ReflectionOnlyLoadFrom(string assemblyFile) { throw null; }
        public override string ToString() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types and members the loaded assembly depends on might be removed")]
        public static System.Reflection.Assembly UnsafeLoadFrom(string assemblyFile) { throw null; }
    }
    public enum AssemblyContentType
    {
        Default = 0,
        WindowsRuntime = 1,
    }
    public sealed partial class AssemblyName : System.ICloneable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public AssemblyName() { }
        public AssemblyName(string assemblyName) { }
        public string? CodeBase { get { throw null; } set { } }
        public System.Reflection.AssemblyContentType ContentType { get { throw null; } set { } }
        public System.Globalization.CultureInfo? CultureInfo { get { throw null; } set { } }
        public string? CultureName { get { throw null; } set { } }
        public string? EscapedCodeBase { get { throw null; } }
        public System.Reflection.AssemblyNameFlags Flags { get { throw null; } set { } }
        public string FullName { get { throw null; } }
        public System.Configuration.Assemblies.AssemblyHashAlgorithm HashAlgorithm { get { throw null; } set { } }
        public System.Reflection.StrongNameKeyPair? KeyPair { get { throw null; } set { } }
        public string? Name { get { throw null; } set { } }
        public System.Reflection.ProcessorArchitecture ProcessorArchitecture { get { throw null; } set { } }
        public System.Version? Version { get { throw null; } set { } }
        public System.Configuration.Assemblies.AssemblyVersionCompatibility VersionCompatibility { get { throw null; } set { } }
        public object Clone() { throw null; }
        public static System.Reflection.AssemblyName GetAssemblyName(string assemblyFile) { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public byte[]? GetPublicKey() { throw null; }
        public byte[]? GetPublicKeyToken() { throw null; }
        public void OnDeserialization(object? sender) { }
        public static bool ReferenceMatchesDefinition(System.Reflection.AssemblyName? reference, System.Reflection.AssemblyName? definition) { throw null; }
        public void SetPublicKey(byte[]? publicKey) { }
        public void SetPublicKeyToken(byte[]? publicKeyToken) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum BindingFlags
    {
        Default = 0,
        IgnoreCase = 1,
        DeclaredOnly = 2,
        Instance = 4,
        Static = 8,
        Public = 16,
        NonPublic = 32,
        FlattenHierarchy = 64,
        InvokeMethod = 256,
        CreateInstance = 512,
        GetField = 1024,
        SetField = 2048,
        GetProperty = 4096,
        SetProperty = 8192,
        PutDispProperty = 16384,
        PutRefDispProperty = 32768,
        ExactBinding = 65536,
        SuppressChangeType = 131072,
        OptionalParamBinding = 262144,
        IgnoreReturn = 16777216,
        DoNotWrapExceptions = 33554432,
    }
    public abstract partial class ConstructorInfo : System.Reflection.MethodBase
    {
        public static readonly string ConstructorName;
        public static readonly string TypeConstructorName;
        protected ConstructorInfo() { }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public object Invoke(object?[]? parameters) { throw null; }
        public abstract object Invoke(System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture);
        public static bool operator ==(System.Reflection.ConstructorInfo? left, System.Reflection.ConstructorInfo? right) { throw null; }
        public static bool operator !=(System.Reflection.ConstructorInfo? left, System.Reflection.ConstructorInfo? right) { throw null; }
    }
    public partial class CustomAttributeData
    {
        protected CustomAttributeData() { }
        public virtual System.Type AttributeType { get { throw null; } }
        public virtual System.Reflection.ConstructorInfo Constructor { get { throw null; } }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeTypedArgument> ConstructorArguments { get { throw null; } }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeNamedArgument> NamedArguments { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public static System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributes(System.Reflection.Assembly target) { throw null; }
        public static System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributes(System.Reflection.MemberInfo target) { throw null; }
        public static System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributes(System.Reflection.Module target) { throw null; }
        public static System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributes(System.Reflection.ParameterInfo target) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct CustomAttributeNamedArgument
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomAttributeNamedArgument(System.Reflection.MemberInfo memberInfo, object? value) { throw null; }
        public CustomAttributeNamedArgument(System.Reflection.MemberInfo memberInfo, System.Reflection.CustomAttributeTypedArgument typedArgument) { throw null; }
        public bool IsField { get { throw null; } }
        public System.Reflection.MemberInfo MemberInfo { get { throw null; } }
        public string MemberName { get { throw null; } }
        public System.Reflection.CustomAttributeTypedArgument TypedValue { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.CustomAttributeNamedArgument left, System.Reflection.CustomAttributeNamedArgument right) { throw null; }
        public static bool operator !=(System.Reflection.CustomAttributeNamedArgument left, System.Reflection.CustomAttributeNamedArgument right) { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct CustomAttributeTypedArgument
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public CustomAttributeTypedArgument(object value) { throw null; }
        public CustomAttributeTypedArgument(System.Type argumentType, object? value) { throw null; }
        public System.Type ArgumentType { get { throw null; } }
        public object? Value { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.CustomAttributeTypedArgument left, System.Reflection.CustomAttributeTypedArgument right) { throw null; }
        public static bool operator !=(System.Reflection.CustomAttributeTypedArgument left, System.Reflection.CustomAttributeTypedArgument right) { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class EventInfo : System.Reflection.MemberInfo
    {
        protected EventInfo() { }
        public virtual System.Reflection.MethodInfo? AddMethod { get { throw null; } }
        public abstract System.Reflection.EventAttributes Attributes { get; }
        public virtual System.Type? EventHandlerType { get { throw null; } }
        public virtual bool IsMulticast { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public virtual System.Reflection.MethodInfo? RaiseMethod { get { throw null; } }
        public virtual System.Reflection.MethodInfo? RemoveMethod { get { throw null; } }
        public virtual void AddEventHandler(object? target, System.Delegate? handler) { }
        public override bool Equals(object? obj) { throw null; }
        public System.Reflection.MethodInfo? GetAddMethod() { throw null; }
        public abstract System.Reflection.MethodInfo? GetAddMethod(bool nonPublic);
        public override int GetHashCode() { throw null; }
        public System.Reflection.MethodInfo[] GetOtherMethods() { throw null; }
        public virtual System.Reflection.MethodInfo[] GetOtherMethods(bool nonPublic) { throw null; }
        public System.Reflection.MethodInfo? GetRaiseMethod() { throw null; }
        public abstract System.Reflection.MethodInfo? GetRaiseMethod(bool nonPublic);
        public System.Reflection.MethodInfo? GetRemoveMethod() { throw null; }
        public abstract System.Reflection.MethodInfo? GetRemoveMethod(bool nonPublic);
        public static bool operator ==(System.Reflection.EventInfo? left, System.Reflection.EventInfo? right) { throw null; }
        public static bool operator !=(System.Reflection.EventInfo? left, System.Reflection.EventInfo? right) { throw null; }
        public virtual void RemoveEventHandler(object? target, System.Delegate? handler) { }
    }
    public abstract partial class FieldInfo : System.Reflection.MemberInfo
    {
        protected FieldInfo() { }
        public abstract System.Reflection.FieldAttributes Attributes { get; }
        public abstract System.RuntimeFieldHandle FieldHandle { get; }
        public abstract System.Type FieldType { get; }
        public bool IsAssembly { get { throw null; } }
        public bool IsFamily { get { throw null; } }
        public bool IsFamilyAndAssembly { get { throw null; } }
        public bool IsFamilyOrAssembly { get { throw null; } }
        public bool IsInitOnly { get { throw null; } }
        public bool IsLiteral { get { throw null; } }
        public bool IsNotSerialized { get { throw null; } }
        public bool IsPinvokeImpl { get { throw null; } }
        public bool IsPrivate { get { throw null; } }
        public bool IsPublic { get { throw null; } }
        public virtual bool IsSecurityCritical { get { throw null; } }
        public virtual bool IsSecuritySafeCritical { get { throw null; } }
        public virtual bool IsSecurityTransparent { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public bool IsStatic { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public static System.Reflection.FieldInfo GetFieldFromHandle(System.RuntimeFieldHandle handle) { throw null; }
        public static System.Reflection.FieldInfo GetFieldFromHandle(System.RuntimeFieldHandle handle, System.RuntimeTypeHandle declaringType) { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Type[] GetOptionalCustomModifiers() { throw null; }
        public virtual object? GetRawConstantValue() { throw null; }
        public virtual System.Type[] GetRequiredCustomModifiers() { throw null; }
        public abstract object? GetValue(object? obj);
        [System.CLSCompliantAttribute(false)]
        public virtual object? GetValueDirect(System.TypedReference obj) { throw null; }
        public static bool operator ==(System.Reflection.FieldInfo? left, System.Reflection.FieldInfo? right) { throw null; }
        public static bool operator !=(System.Reflection.FieldInfo? left, System.Reflection.FieldInfo? right) { throw null; }
        public void SetValue(object? obj, object? value) { }
        public abstract void SetValue(object? obj, object? value, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, System.Globalization.CultureInfo? culture);
        [System.CLSCompliantAttribute(false)]
        public virtual void SetValueDirect(System.TypedReference obj, object value) { }
    }
    public partial interface ICustomAttributeProvider
    {
        object[] GetCustomAttributes(bool inherit);
        object[] GetCustomAttributes(System.Type attributeType, bool inherit);
        bool IsDefined(System.Type attributeType, bool inherit);
    }
    public static partial class IntrospectionExtensions
    {
        public static System.Reflection.TypeInfo GetTypeInfo(this System.Type type) { throw null; }
    }
    public partial class InvalidFilterCriteriaException : System.ApplicationException
    {
        public InvalidFilterCriteriaException() { }
        protected InvalidFilterCriteriaException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidFilterCriteriaException(string? message) { }
        public InvalidFilterCriteriaException(string? message, System.Exception? inner) { }
    }
    public partial interface IReflectableType
    {
        System.Reflection.TypeInfo GetTypeInfo();
    }
    public partial class LocalVariableInfo
    {
        protected LocalVariableInfo() { }
        public virtual bool IsPinned { get { throw null; } }
        public virtual int LocalIndex { get { throw null; } }
        public virtual System.Type LocalType { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public partial class ManifestResourceInfo
    {
        public ManifestResourceInfo(System.Reflection.Assembly? containingAssembly, string? containingFileName, System.Reflection.ResourceLocation resourceLocation) { }
        public virtual string? FileName { get { throw null; } }
        public virtual System.Reflection.Assembly? ReferencedAssembly { get { throw null; } }
        public virtual System.Reflection.ResourceLocation ResourceLocation { get { throw null; } }
    }
    public delegate bool MemberFilter(System.Reflection.MemberInfo m, object? filterCriteria);
    public abstract partial class MemberInfo : System.Reflection.ICustomAttributeProvider
    {
        protected MemberInfo() { }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public abstract System.Type? DeclaringType { get; }
        public virtual bool IsCollectible { get { throw null; } }
        public abstract System.Reflection.MemberTypes MemberType { get; }
        public virtual int MetadataToken { get { throw null; } }
        public virtual System.Reflection.Module Module { get { throw null; } }
        public abstract string Name { get; }
        public abstract System.Type? ReflectedType { get; }
        public override bool Equals(object? obj) { throw null; }
        public abstract object[] GetCustomAttributes(bool inherit);
        public abstract object[] GetCustomAttributes(System.Type attributeType, bool inherit);
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual bool HasSameMetadataDefinitionAs(System.Reflection.MemberInfo other) { throw null; }
        public abstract bool IsDefined(System.Type attributeType, bool inherit);
        public static bool operator ==(System.Reflection.MemberInfo? left, System.Reflection.MemberInfo? right) { throw null; }
        public static bool operator !=(System.Reflection.MemberInfo? left, System.Reflection.MemberInfo? right) { throw null; }
    }
    [System.FlagsAttribute]
    public enum MemberTypes
    {
        Constructor = 1,
        Event = 2,
        Field = 4,
        Method = 8,
        Property = 16,
        TypeInfo = 32,
        Custom = 64,
        NestedType = 128,
        All = 191,
    }
    public abstract partial class MethodBase : System.Reflection.MemberInfo
    {
        protected MethodBase() { }
        public abstract System.Reflection.MethodAttributes Attributes { get; }
        public virtual System.Reflection.CallingConventions CallingConvention { get { throw null; } }
        public virtual bool ContainsGenericParameters { get { throw null; } }
        public bool IsAbstract { get { throw null; } }
        public bool IsAssembly { get { throw null; } }
        public virtual bool IsConstructedGenericMethod { get { throw null; } }
        public bool IsConstructor { get { throw null; } }
        public bool IsFamily { get { throw null; } }
        public bool IsFamilyAndAssembly { get { throw null; } }
        public bool IsFamilyOrAssembly { get { throw null; } }
        public bool IsFinal { get { throw null; } }
        public virtual bool IsGenericMethod { get { throw null; } }
        public virtual bool IsGenericMethodDefinition { get { throw null; } }
        public bool IsHideBySig { get { throw null; } }
        public bool IsPrivate { get { throw null; } }
        public bool IsPublic { get { throw null; } }
        public virtual bool IsSecurityCritical { get { throw null; } }
        public virtual bool IsSecuritySafeCritical { get { throw null; } }
        public virtual bool IsSecurityTransparent { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public bool IsStatic { get { throw null; } }
        public bool IsVirtual { get { throw null; } }
        public abstract System.RuntimeMethodHandle MethodHandle { get; }
        public virtual System.Reflection.MethodImplAttributes MethodImplementationFlags { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public static System.Reflection.MethodBase? GetCurrentMethod() { throw null; }
        public virtual System.Type[] GetGenericArguments() { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Reflection.MethodBody? GetMethodBody() { throw null; }
        public static System.Reflection.MethodBase? GetMethodFromHandle(System.RuntimeMethodHandle handle) { throw null; }
        public static System.Reflection.MethodBase? GetMethodFromHandle(System.RuntimeMethodHandle handle, System.RuntimeTypeHandle declaringType) { throw null; }
        public abstract System.Reflection.MethodImplAttributes GetMethodImplementationFlags();
        public abstract System.Reflection.ParameterInfo[] GetParameters();
        public object? Invoke(object? obj, object?[]? parameters) { throw null; }
        public abstract object? Invoke(object? obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture);
        public static bool operator ==(System.Reflection.MethodBase? left, System.Reflection.MethodBase? right) { throw null; }
        public static bool operator !=(System.Reflection.MethodBase? left, System.Reflection.MethodBase? right) { throw null; }
    }
    public abstract partial class MethodInfo : System.Reflection.MethodBase
    {
        protected MethodInfo() { }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public virtual System.Reflection.ParameterInfo ReturnParameter { get { throw null; } }
        public virtual System.Type ReturnType { get { throw null; } }
        public abstract System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes { get; }
        public virtual System.Delegate CreateDelegate(System.Type delegateType) { throw null; }
        public virtual System.Delegate CreateDelegate(System.Type delegateType, object? target) { throw null; }
        public T CreateDelegate<T>() where T : System.Delegate { throw null; }
        public T CreateDelegate<T>(object? target) where T : System.Delegate { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public abstract System.Reflection.MethodInfo GetBaseDefinition();
        public override System.Type[] GetGenericArguments() { throw null; }
        public virtual System.Reflection.MethodInfo GetGenericMethodDefinition() { throw null; }
        public override int GetHashCode() { throw null; }
        public virtual System.Reflection.MethodInfo MakeGenericMethod(params System.Type[] typeArguments) { throw null; }
        public static bool operator ==(System.Reflection.MethodInfo? left, System.Reflection.MethodInfo? right) { throw null; }
        public static bool operator !=(System.Reflection.MethodInfo? left, System.Reflection.MethodInfo? right) { throw null; }
    }
    public abstract partial class Module : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.ISerializable
    {
        public static readonly System.Reflection.TypeFilter FilterTypeName;
        public static readonly System.Reflection.TypeFilter FilterTypeNameIgnoreCase;
        protected Module() { }
        public virtual System.Reflection.Assembly Assembly { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public virtual string FullyQualifiedName { get { throw null; } }
        public virtual int MDStreamVersion { get { throw null; } }
        public virtual int MetadataToken { get { throw null; } }
        public System.ModuleHandle ModuleHandle { get { throw null; } }
        public virtual System.Guid ModuleVersionId { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual string ScopeName { get { throw null; } }
        public override bool Equals(object? o) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type[] FindTypes(System.Reflection.TypeFilter? filter, object? filterCriteria) { throw null; }
        public virtual object[] GetCustomAttributes(bool inherit) { throw null; }
        public virtual object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Fields might be removed")]
        public System.Reflection.FieldInfo? GetField(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Fields might be removed")]
        public virtual System.Reflection.FieldInfo? GetField(string name, System.Reflection.BindingFlags bindingAttr) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Fields might be removed")]
        public System.Reflection.FieldInfo[] GetFields() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Fields might be removed")]
        public virtual System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingFlags) { throw null; }
        public override int GetHashCode() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Methods might be removed")]
        public System.Reflection.MethodInfo? GetMethod(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Methods might be removed")]
        public System.Reflection.MethodInfo? GetMethod(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[] types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Methods might be removed")]
        public System.Reflection.MethodInfo? GetMethod(string name, System.Type[] types) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Methods might be removed")]
        protected virtual System.Reflection.MethodInfo? GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, System.Reflection.CallingConventions callConvention, System.Type[]? types, System.Reflection.ParameterModifier[]? modifiers) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Methods might be removed")]
        public System.Reflection.MethodInfo[] GetMethods() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Methods might be removed")]
        public virtual System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingFlags) { throw null; }
        protected virtual System.ModuleHandle GetModuleHandleImpl() { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public virtual void GetPEKind(out System.Reflection.PortableExecutableKinds peKind, out System.Reflection.ImageFileMachine machine) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type? GetType(string className) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type? GetType(string className, bool ignoreCase) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type? GetType(string className, bool throwOnError, bool ignoreCase) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Types might be removed")]
        public virtual System.Type[] GetTypes() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public virtual bool IsResource() { throw null; }
        public static bool operator ==(System.Reflection.Module? left, System.Reflection.Module? right) { throw null; }
        public static bool operator !=(System.Reflection.Module? left, System.Reflection.Module? right) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public System.Reflection.FieldInfo? ResolveField(int metadataToken) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public virtual System.Reflection.FieldInfo? ResolveField(int metadataToken, System.Type[]? genericTypeArguments, System.Type[]? genericMethodArguments) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public System.Reflection.MemberInfo? ResolveMember(int metadataToken) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public virtual System.Reflection.MemberInfo? ResolveMember(int metadataToken, System.Type[]? genericTypeArguments, System.Type[]? genericMethodArguments) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public System.Reflection.MethodBase? ResolveMethod(int metadataToken) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public virtual System.Reflection.MethodBase? ResolveMethod(int metadataToken, System.Type[]? genericTypeArguments, System.Type[]? genericMethodArguments) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public virtual byte[] ResolveSignature(int metadataToken) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public virtual string ResolveString(int metadataToken) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public System.Type ResolveType(int metadataToken) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Trimming changes metadata tokens")]
        public virtual System.Type ResolveType(int metadataToken, System.Type[]? genericTypeArguments, System.Type[]? genericMethodArguments) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class ParameterInfo : System.Reflection.ICustomAttributeProvider, System.Runtime.Serialization.IObjectReference
    {
        protected System.Reflection.ParameterAttributes AttrsImpl;
        protected System.Type? ClassImpl;
        protected object? DefaultValueImpl;
        protected System.Reflection.MemberInfo MemberImpl;
        protected string? NameImpl;
        protected int PositionImpl;
        protected ParameterInfo() { }
        public virtual System.Reflection.ParameterAttributes Attributes { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> CustomAttributes { get { throw null; } }
        public virtual object? DefaultValue { get { throw null; } }
        public virtual bool HasDefaultValue { get { throw null; } }
        public bool IsIn { get { throw null; } }
        public bool IsLcid { get { throw null; } }
        public bool IsOptional { get { throw null; } }
        public bool IsOut { get { throw null; } }
        public bool IsRetval { get { throw null; } }
        public virtual System.Reflection.MemberInfo Member { get { throw null; } }
        public virtual int MetadataToken { get { throw null; } }
        public virtual string? Name { get { throw null; } }
        public virtual System.Type ParameterType { get { throw null; } }
        public virtual int Position { get { throw null; } }
        public virtual object? RawDefaultValue { get { throw null; } }
        public virtual object[] GetCustomAttributes(bool inherit) { throw null; }
        public virtual object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public virtual System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public virtual System.Type[] GetOptionalCustomModifiers() { throw null; }
        public object GetRealObject(System.Runtime.Serialization.StreamingContext context) { throw null; }
        public virtual System.Type[] GetRequiredCustomModifiers() { throw null; }
        public virtual bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct ParameterModifier
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ParameterModifier(int parameterCount) { throw null; }
        public bool this[int index] { get { throw null; } set { } }
    }
    public abstract partial class PropertyInfo : System.Reflection.MemberInfo
    {
        protected PropertyInfo() { }
        public abstract System.Reflection.PropertyAttributes Attributes { get; }
        public abstract bool CanRead { get; }
        public abstract bool CanWrite { get; }
        public virtual System.Reflection.MethodInfo? GetMethod { get { throw null; } }
        public bool IsSpecialName { get { throw null; } }
        public override System.Reflection.MemberTypes MemberType { get { throw null; } }
        public abstract System.Type PropertyType { get; }
        public virtual System.Reflection.MethodInfo? SetMethod { get { throw null; } }
        public override bool Equals(object? obj) { throw null; }
        public System.Reflection.MethodInfo[] GetAccessors() { throw null; }
        public abstract System.Reflection.MethodInfo[] GetAccessors(bool nonPublic);
        public virtual object? GetConstantValue() { throw null; }
        public System.Reflection.MethodInfo? GetGetMethod() { throw null; }
        public abstract System.Reflection.MethodInfo? GetGetMethod(bool nonPublic);
        public override int GetHashCode() { throw null; }
        public abstract System.Reflection.ParameterInfo[] GetIndexParameters();
        public virtual System.Type[] GetOptionalCustomModifiers() { throw null; }
        public virtual object? GetRawConstantValue() { throw null; }
        public virtual System.Type[] GetRequiredCustomModifiers() { throw null; }
        public System.Reflection.MethodInfo? GetSetMethod() { throw null; }
        public abstract System.Reflection.MethodInfo? GetSetMethod(bool nonPublic);
        public object? GetValue(object? obj) { throw null; }
        public virtual object? GetValue(object? obj, object?[]? index) { throw null; }
        public abstract object? GetValue(object? obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? index, System.Globalization.CultureInfo? culture);
        public static bool operator ==(System.Reflection.PropertyInfo? left, System.Reflection.PropertyInfo? right) { throw null; }
        public static bool operator !=(System.Reflection.PropertyInfo? left, System.Reflection.PropertyInfo? right) { throw null; }
        public void SetValue(object? obj, object? value) { }
        public virtual void SetValue(object? obj, object? value, object?[]? index) { }
        public abstract void SetValue(object? obj, object? value, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder? binder, object?[]? index, System.Globalization.CultureInfo? culture);
    }
    public abstract partial class ReflectionContext
    {
        protected ReflectionContext() { }
        public virtual System.Reflection.TypeInfo GetTypeForObject(object value) { throw null; }
        public abstract System.Reflection.Assembly MapAssembly(System.Reflection.Assembly assembly);
        public abstract System.Reflection.TypeInfo MapType(System.Reflection.TypeInfo type);
    }
    public sealed partial class ReflectionTypeLoadException : System.SystemException, System.Runtime.Serialization.ISerializable
    {
        public ReflectionTypeLoadException(System.Type?[]? classes, System.Exception?[]? exceptions) { }
        public ReflectionTypeLoadException(System.Type?[]? classes, System.Exception?[]? exceptions, string? message) { }
        public System.Exception?[] LoaderExceptions { get { throw null; } }
        public override string Message { get { throw null; } }
        public System.Type?[] Types { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum ResourceLocation
    {
        Embedded = 1,
        ContainedInAnotherAssembly = 2,
        ContainedInManifestFile = 4,
    }
    public partial class TargetException : System.ApplicationException
    {
        public TargetException() { }
        protected TargetException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public TargetException(string? message) { }
        public TargetException(string? message, System.Exception? inner) { }
    }
    public sealed partial class TargetInvocationException : System.ApplicationException
    {
        public TargetInvocationException(System.Exception? inner) { }
        public TargetInvocationException(string? message, System.Exception? inner) { }
    }
    public sealed partial class TargetParameterCountException : System.ApplicationException
    {
        public TargetParameterCountException() { }
        public TargetParameterCountException(string? message) { }
        public TargetParameterCountException(string? message, System.Exception? inner) { }
    }
    public delegate bool TypeFilter(System.Type m, object? filterCriteria);
    public abstract partial class TypeInfo : System.Type, System.Reflection.IReflectableType
    {
        protected TypeInfo() { }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.ConstructorInfo> DeclaredConstructors { [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.EventInfo> DeclaredEvents { [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)] get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.FieldInfo> DeclaredFields { [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.MemberInfo> DeclaredMembers { [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> DeclaredMethods { [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.TypeInfo> DeclaredNestedTypes { [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)] get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> DeclaredProperties { [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] get { throw null; } }
        public virtual System.Type[] GenericTypeParameters { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<System.Type> ImplementedInterfaces { get { throw null; } }
        public virtual System.Type AsType() { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)]
        public virtual System.Reflection.EventInfo? GetDeclaredEvent(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public virtual System.Reflection.FieldInfo? GetDeclaredField(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public virtual System.Reflection.MethodInfo? GetDeclaredMethod(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public virtual System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> GetDeclaredMethods(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        public virtual System.Reflection.TypeInfo? GetDeclaredNestedType(string name) { throw null; }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public virtual System.Reflection.PropertyInfo? GetDeclaredProperty(string name) { throw null; }
        public virtual bool IsAssignableFrom([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Reflection.TypeInfo? typeInfo) { throw null; }
        System.Reflection.TypeInfo System.Reflection.IReflectableType.GetTypeInfo() { throw null; }
    }
}
