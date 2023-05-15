// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

#if !BUILDING_CORELIB_REFERENCE
namespace System.Runtime.CompilerServices
{
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter, Inherited=false)]
    public sealed partial class IDispatchConstantAttribute : System.Runtime.CompilerServices.CustomConstantAttribute
    {
        public IDispatchConstantAttribute() { }
        public override object Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter, Inherited=false)]
    public sealed partial class IUnknownConstantAttribute : System.Runtime.CompilerServices.CustomConstantAttribute
    {
        public IUnknownConstantAttribute() { }
        public override object Value { get { throw null; } }
    }
}
namespace System.Runtime.InteropServices
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Interface, Inherited=false)]
    public sealed partial class AutomationProxyAttribute : System.Attribute
    {
        public AutomationProxyAttribute(bool val) { }
        public bool Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, Inherited=false)]
    public sealed partial class ComAliasNameAttribute : System.Attribute
    {
        public ComAliasNameAttribute(string alias) { }
        public string Value { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class ComAwareEventInfo : System.Reflection.EventInfo
    {
        public ComAwareEventInfo([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents)] System.Type type, string eventName) { }
        public override System.Reflection.EventAttributes Attributes { get { throw null; } }
        public override System.Type? DeclaringType { get { throw null; } }
        public override int MetadataToken { get { throw null; } }
        public override System.Reflection.Module Module { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Type? ReflectedType { get { throw null; } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public override void AddEventHandler(object target, System.Delegate handler) { }
        public override System.Reflection.MethodInfo? GetAddMethod(bool nonPublic) { throw null; }
        public override object[] GetCustomAttributes(bool inherit) { throw null; }
        public override object[] GetCustomAttributes(System.Type attributeType, bool inherit) { throw null; }
        public override System.Collections.Generic.IList<System.Reflection.CustomAttributeData> GetCustomAttributesData() { throw null; }
        public override System.Reflection.MethodInfo[] GetOtherMethods(bool nonPublic) { throw null; }
        public override System.Reflection.MethodInfo? GetRaiseMethod(bool nonPublic) { throw null; }
        public override System.Reflection.MethodInfo? GetRemoveMethod(bool nonPublic) { throw null; }
        public override bool IsDefined(System.Type attributeType, bool inherit) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public override void RemoveEventHandler(object target, System.Delegate handler) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, Inherited=false)]
    public sealed partial class ComCompatibleVersionAttribute : System.Attribute
    {
        public ComCompatibleVersionAttribute(int major, int minor, int build, int revision) { }
        public int BuildNumber { get { throw null; } }
        public int MajorVersion { get { throw null; } }
        public int MinorVersion { get { throw null; } }
        public int RevisionNumber { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All, Inherited=false)]
    public sealed partial class ComConversionLossAttribute : System.Attribute
    {
        public ComConversionLossAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false)]
    public sealed partial class ComRegisterFunctionAttribute : System.Attribute
    {
        public ComRegisterFunctionAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false)]
    public sealed partial class ComUnregisterFunctionAttribute : System.Attribute
    {
        public ComUnregisterFunctionAttribute() { }
    }
    public sealed partial class HandleCollector
    {
        public HandleCollector(string? name, int initialThreshold) { }
        public HandleCollector(string? name, int initialThreshold, int maximumThreshold) { }
        public int Count { get { throw null; } }
        public int InitialThreshold { get { throw null; } }
        public int MaximumThreshold { get { throw null; } }
        public string Name { get { throw null; } }
        public void Add() { }
        public void Remove() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, Inherited=false)]
    public sealed partial class ImportedFromTypeLibAttribute : System.Attribute
    {
        public ImportedFromTypeLibAttribute(string tlbFile) { }
        public string Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false, AllowMultiple=false)]
    public sealed partial class ManagedToNativeComInteropStubAttribute : System.Attribute
    {
        public ManagedToNativeComInteropStubAttribute(System.Type classType, string methodName) { }
        public System.Type ClassType { get { throw null; } }
        public string MethodName { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, Inherited=false, AllowMultiple=true)]
    public sealed partial class PrimaryInteropAssemblyAttribute : System.Attribute
    {
        public PrimaryInteropAssemblyAttribute(int major, int minor) { }
        public int MajorVersion { get { throw null; } }
        public int MinorVersion { get { throw null; } }
    }
    public static partial class RuntimeEnvironment
    {
        [System.ObsoleteAttribute("RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0019", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static string SystemConfigurationFile { get { throw null; } }
        public static bool FromGlobalAccessCache(System.Reflection.Assembly a) { throw null; }
        public static string GetRuntimeDirectory() { throw null; }
        [System.ObsoleteAttribute("RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0019", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.IntPtr GetRuntimeInterfaceAsIntPtr(System.Guid clsid, System.Guid riid) { throw null; }
        [System.ObsoleteAttribute("RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0019", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static object GetRuntimeInterfaceAsObject(System.Guid clsid, System.Guid riid) { throw null; }
        public static string GetSystemVersion() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false)]
    public sealed partial class TypeLibFuncAttribute : System.Attribute
    {
        public TypeLibFuncAttribute(short flags) { }
        public TypeLibFuncAttribute(System.Runtime.InteropServices.TypeLibFuncFlags flags) { }
        public System.Runtime.InteropServices.TypeLibFuncFlags Value { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum TypeLibFuncFlags
    {
        FRestricted = 1,
        FSource = 2,
        FBindable = 4,
        FRequestEdit = 8,
        FDisplayBind = 16,
        FDefaultBind = 32,
        FHidden = 64,
        FUsesGetLastError = 128,
        FDefaultCollelem = 256,
        FUiDefault = 512,
        FNonBrowsable = 1024,
        FReplaceable = 2048,
        FImmediateBind = 4096,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Interface, Inherited=false)]
    public sealed partial class TypeLibImportClassAttribute : System.Attribute
    {
        public TypeLibImportClassAttribute(System.Type importClass) { }
        public string Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Struct, Inherited=false)]
    public sealed partial class TypeLibTypeAttribute : System.Attribute
    {
        public TypeLibTypeAttribute(short flags) { }
        public TypeLibTypeAttribute(System.Runtime.InteropServices.TypeLibTypeFlags flags) { }
        public System.Runtime.InteropServices.TypeLibTypeFlags Value { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum TypeLibTypeFlags
    {
        FAppObject = 1,
        FCanCreate = 2,
        FLicensed = 4,
        FPreDeclId = 8,
        FHidden = 16,
        FControl = 32,
        FDual = 64,
        FNonExtensible = 128,
        FOleAutomation = 256,
        FRestricted = 512,
        FAggregatable = 1024,
        FReplaceable = 2048,
        FDispatchable = 4096,
        FReverseBind = 8192,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field, Inherited=false)]
    public sealed partial class TypeLibVarAttribute : System.Attribute
    {
        public TypeLibVarAttribute(short flags) { }
        public TypeLibVarAttribute(System.Runtime.InteropServices.TypeLibVarFlags flags) { }
        public System.Runtime.InteropServices.TypeLibVarFlags Value { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum TypeLibVarFlags
    {
        FReadOnly = 1,
        FSource = 2,
        FBindable = 4,
        FRequestEdit = 8,
        FDisplayBind = 16,
        FDefaultBind = 32,
        FHidden = 64,
        FRestricted = 128,
        FDefaultCollelem = 256,
        FUiDefault = 512,
        FNonBrowsable = 1024,
        FReplaceable = 2048,
        FImmediateBind = 4096,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, Inherited=false)]
    public sealed partial class TypeLibVersionAttribute : System.Attribute
    {
        public TypeLibVersionAttribute(int major, int minor) { }
        public int MajorVersion { get { throw null; } }
        public int MinorVersion { get { throw null; } }
    }
}
namespace System.Runtime.InteropServices.ComTypes
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum ADVF
    {
        ADVF_NODATA = 1,
        ADVF_PRIMEFIRST = 2,
        ADVF_ONLYONCE = 4,
        ADVFCACHE_NOHANDLER = 8,
        ADVFCACHE_FORCEBUILTIN = 16,
        ADVFCACHE_ONSAVE = 32,
        ADVF_DATAONSTOP = 64,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum DATADIR
    {
        DATADIR_GET = 1,
        DATADIR_SET = 2,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum DVASPECT
    {
        DVASPECT_CONTENT = 1,
        DVASPECT_THUMBNAIL = 2,
        DVASPECT_ICON = 4,
        DVASPECT_DOCPRINT = 8,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FORMATETC
    {
        public short cfFormat;
        public System.Runtime.InteropServices.ComTypes.DVASPECT dwAspect;
        public int lindex;
        public System.IntPtr ptd;
        public System.Runtime.InteropServices.ComTypes.TYMED tymed;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IAdviseSink
    {
        void OnClose();
        void OnDataChange(ref System.Runtime.InteropServices.ComTypes.FORMATETC format, ref System.Runtime.InteropServices.ComTypes.STGMEDIUM stgmedium);
        void OnRename(System.Runtime.InteropServices.ComTypes.IMoniker moniker);
        void OnSave();
        void OnViewChange(int aspect, int index);
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IDataObject
    {
        int DAdvise(ref System.Runtime.InteropServices.ComTypes.FORMATETC pFormatetc, System.Runtime.InteropServices.ComTypes.ADVF advf, System.Runtime.InteropServices.ComTypes.IAdviseSink adviseSink, out int connection);
        void DUnadvise(int connection);
        int EnumDAdvise(out System.Runtime.InteropServices.ComTypes.IEnumSTATDATA? enumAdvise);
        System.Runtime.InteropServices.ComTypes.IEnumFORMATETC EnumFormatEtc(System.Runtime.InteropServices.ComTypes.DATADIR direction);
        int GetCanonicalFormatEtc(ref System.Runtime.InteropServices.ComTypes.FORMATETC formatIn, out System.Runtime.InteropServices.ComTypes.FORMATETC formatOut);
        void GetData(ref System.Runtime.InteropServices.ComTypes.FORMATETC format, out System.Runtime.InteropServices.ComTypes.STGMEDIUM medium);
        void GetDataHere(ref System.Runtime.InteropServices.ComTypes.FORMATETC format, ref System.Runtime.InteropServices.ComTypes.STGMEDIUM medium);
        int QueryGetData(ref System.Runtime.InteropServices.ComTypes.FORMATETC format);
        void SetData(ref System.Runtime.InteropServices.ComTypes.FORMATETC formatIn, ref System.Runtime.InteropServices.ComTypes.STGMEDIUM medium, bool release);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IEnumFORMATETC
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumFORMATETC newEnum);
        int Next(int celt, System.Runtime.InteropServices.ComTypes.FORMATETC[] rgelt, int[] pceltFetched);
        int Reset();
        int Skip(int celt);
    }
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IEnumSTATDATA
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumSTATDATA newEnum);
        int Next(int celt, System.Runtime.InteropServices.ComTypes.STATDATA[] rgelt, int[] pceltFetched);
        int Reset();
        int Skip(int celt);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct STATDATA
    {
        public System.Runtime.InteropServices.ComTypes.ADVF advf;
        public System.Runtime.InteropServices.ComTypes.IAdviseSink advSink;
        public int connection;
        public System.Runtime.InteropServices.ComTypes.FORMATETC formatetc;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct STGMEDIUM
    {
        public object? pUnkForRelease;
        public System.Runtime.InteropServices.ComTypes.TYMED tymed;
        public System.IntPtr unionmember;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum TYMED
    {
        TYMED_NULL = 0,
        TYMED_HGLOBAL = 1,
        TYMED_FILE = 2,
        TYMED_ISTREAM = 4,
        TYMED_ISTORAGE = 8,
        TYMED_GDI = 16,
        TYMED_MFPICT = 32,
        TYMED_ENHMF = 64,
    }
}
namespace System.Runtime.InteropServices.Marshalling
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited = false)]
    [System.CLSCompliantAttribute(false)]
    public sealed partial class ComExposedClassAttribute<T> : System.Attribute, System.Runtime.InteropServices.Marshalling.IComExposedDetails where T : System.Runtime.InteropServices.Marshalling.IComExposedClass
    {
        public ComExposedClassAttribute() { }
        public unsafe System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count) { throw null; }
    }
    public sealed partial class ComObject : System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider
    {
        internal ComObject() { }
        public void FinalRelease() { }
        ~ComObject() { }
        System.RuntimeTypeHandle System.Runtime.InteropServices.IDynamicInterfaceCastable.GetInterfaceImplementation(System.RuntimeTypeHandle interfaceType) { throw null; }
        bool System.Runtime.InteropServices.IDynamicInterfaceCastable.IsInterfaceImplemented(System.RuntimeTypeHandle interfaceType, bool throwIfNotImplemented) { throw null; }
        System.Runtime.InteropServices.Marshalling.VirtualMethodTableInfo System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(System.Type type) { throw null; }
    }
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(System.Exception), System.Runtime.InteropServices.Marshalling.MarshalMode.UnmanagedToManagedOut, typeof(System.Runtime.InteropServices.Marshalling.ExceptionAsDefaultMarshaller<>))]
    public static partial class ExceptionAsDefaultMarshaller<T> where T : struct
    {
        public static T ConvertToUnmanaged(System.Exception e) { throw null; }
    }
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(System.Exception), System.Runtime.InteropServices.Marshalling.MarshalMode.UnmanagedToManagedOut, typeof(System.Runtime.InteropServices.Marshalling.ExceptionAsHResultMarshaller<>))]
    public static partial class ExceptionAsHResultMarshaller<T> where T : struct
    {
        public static T ConvertToUnmanaged(System.Exception e) { throw null; }
    }
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(System.Exception), System.Runtime.InteropServices.Marshalling.MarshalMode.UnmanagedToManagedOut, typeof(System.Runtime.InteropServices.Marshalling.ExceptionAsNaNMarshaller<>))]
    public static partial class ExceptionAsNaNMarshaller<T> where T : struct
    {
        public static T ConvertToUnmanaged(System.Exception e) { throw null; }
    }
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(System.Exception), System.Runtime.InteropServices.Marshalling.MarshalMode.UnmanagedToManagedOut, typeof(System.Runtime.InteropServices.Marshalling.ExceptionAsVoidMarshaller))]
    public static partial class ExceptionAsVoidMarshaller
    {
        public static void ConvertToUnmanaged(System.Exception e) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class GeneratedComClassAttribute : System.Attribute
    {
        public GeneratedComClassAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Interface)]
    public partial class GeneratedComInterfaceAttribute : System.Attribute
    {
        public GeneratedComInterfaceAttribute() { }
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IComExposedClass
    {
        unsafe static abstract System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count);
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IComExposedDetails
    {
        unsafe System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count);
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IIUnknownCacheStrategy
    {
        void Clear(System.Runtime.InteropServices.Marshalling.IIUnknownStrategy unknownStrategy);
        unsafe System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy.TableInfo ConstructTableInfo(System.RuntimeTypeHandle handle, System.Runtime.InteropServices.Marshalling.IIUnknownDerivedDetails interfaceDetails, void* ptr);
        bool TryGetTableInfo(System.RuntimeTypeHandle handle, out System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy.TableInfo info);
        bool TrySetTableInfo(System.RuntimeTypeHandle handle, System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy.TableInfo info);
        public readonly partial struct TableInfo
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.RuntimeTypeHandle ManagedType { get { throw null; } init { } }
            public unsafe void** Table { get { throw null; } init { } }
            public unsafe void* ThisPtr { get { throw null; } init { } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IIUnknownDerivedDetails
    {
        System.Guid Iid { get; }
        System.Type Implementation { get; }
        unsafe void** ManagedVirtualMethodTable { get; }
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IIUnknownInterfaceDetailsStrategy
    {
        System.Runtime.InteropServices.Marshalling.IComExposedDetails? GetComExposedTypeDetails(System.RuntimeTypeHandle type);
        System.Runtime.InteropServices.Marshalling.IIUnknownDerivedDetails? GetIUnknownDerivedDetails(System.RuntimeTypeHandle type);
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IIUnknownInterfaceType
    {
        static abstract System.Guid Iid { get; }
        unsafe static abstract void** ManagedVirtualMethodTable { get; }
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IIUnknownStrategy
    {
        unsafe void* CreateInstancePointer(void* unknown);
        unsafe int QueryInterface(void* instancePtr, in System.Guid iid, out void* ppObj);
        unsafe int Release(void* instancePtr);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Interface, Inherited = false)]
    [System.CLSCompliantAttribute(false)]
    public partial class IUnknownDerivedAttribute<T, TImpl> : System.Attribute, System.Runtime.InteropServices.Marshalling.IIUnknownDerivedDetails where T : System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType
    {
        public IUnknownDerivedAttribute() { }
        public System.Guid Iid { get { throw null; } }
        public System.Type Implementation { get { throw null; } }
        public unsafe void** ManagedVirtualMethodTable { get { throw null; } }
    }
    [System.CLSCompliantAttribute(false)]
    public partial interface IUnmanagedVirtualMethodTableProvider
    {
        System.Runtime.InteropServices.Marshalling.VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(System.Type type);
    }
    [System.CLSCompliantAttribute(false)]
    public partial class StrategyBasedComWrappers : System.Runtime.InteropServices.ComWrappers
    {
        public StrategyBasedComWrappers() { }
        public static System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceDetailsStrategy DefaultIUnknownInterfaceDetailsStrategy { get { throw null; } }
        public static System.Runtime.InteropServices.Marshalling.IIUnknownStrategy DefaultIUnknownStrategy { get { throw null; } }
        protected unsafe sealed override System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry* ComputeVtables(object obj, System.Runtime.InteropServices.CreateComInterfaceFlags flags, out int count) { throw null; }
        protected virtual System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy CreateCacheStrategy() { throw null; }
        protected static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy CreateDefaultCacheStrategy() { throw null; }
        protected sealed override object CreateObject(nint externalComObject, System.Runtime.InteropServices.CreateObjectFlags flags) { throw null; }
        protected virtual System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceDetailsStrategy GetOrCreateInterfaceDetailsStrategy() { throw null; }
        protected virtual System.Runtime.InteropServices.Marshalling.IIUnknownStrategy GetOrCreateIUnknownStrategy() { throw null; }
        protected sealed override void ReleaseObjects(System.Collections.IEnumerable objects) { }
    }
    [System.CLSCompliantAttribute(false)]
    public readonly partial struct VirtualMethodTableInfo
    {
        private readonly int _dummyPrimitive;
        public unsafe VirtualMethodTableInfo(void* thisPointer, void** virtualMethodTable) { throw null; }
        public unsafe void* ThisPointer { get { throw null; } }
        public unsafe void** VirtualMethodTable { get { throw null; } }
        public unsafe void Deconstruct(out void* thisPointer, out void** virtualMethodTable) { throw null; }
    }
}
namespace System.Security
{
    public static partial class SecureStringMarshal
    {
        public static System.IntPtr SecureStringToCoTaskMemAnsi(System.Security.SecureString s) { throw null; }
        public static System.IntPtr SecureStringToCoTaskMemUnicode(System.Security.SecureString s) { throw null; }
        public static System.IntPtr SecureStringToGlobalAllocAnsi(System.Security.SecureString s) { throw null; }
        public static System.IntPtr SecureStringToGlobalAllocUnicode(System.Security.SecureString s) { throw null; }
    }
}
#endif // !BUILDING_CORELIB_REFERENCE
namespace System
{
    public sealed partial class DataMisalignedException : System.SystemException
    {
        public DataMisalignedException() { }
        public DataMisalignedException(string? message) { }
        public DataMisalignedException(string? message, System.Exception? innerException) { }
    }
    public partial class DllNotFoundException : System.TypeLoadException
    {
        public DllNotFoundException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected DllNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DllNotFoundException(string? message) { }
        public DllNotFoundException(string? message, System.Exception? inner) { }
    }
}
namespace System.IO
{
    public partial class UnmanagedMemoryAccessor : System.IDisposable
    {
        protected UnmanagedMemoryAccessor() { }
        public UnmanagedMemoryAccessor(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity) { }
        public UnmanagedMemoryAccessor(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity, System.IO.FileAccess access) { }
        public bool CanRead { get { throw null; } }
        public bool CanWrite { get { throw null; } }
        public long Capacity { get { throw null; } }
        protected bool IsOpen { get { throw null; } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected void Initialize(System.Runtime.InteropServices.SafeBuffer buffer, long offset, long capacity, System.IO.FileAccess access) { }
        public int ReadArray<T>(long position, T[] array, int offset, int count) where T : struct { throw null; }
        public bool ReadBoolean(long position) { throw null; }
        public byte ReadByte(long position) { throw null; }
        public char ReadChar(long position) { throw null; }
        public decimal ReadDecimal(long position) { throw null; }
        public double ReadDouble(long position) { throw null; }
        public short ReadInt16(long position) { throw null; }
        public int ReadInt32(long position) { throw null; }
        public long ReadInt64(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public sbyte ReadSByte(long position) { throw null; }
        public float ReadSingle(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ushort ReadUInt16(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public uint ReadUInt32(long position) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public ulong ReadUInt64(long position) { throw null; }
        public void Read<T>(long position, out T structure) where T : struct { throw null; }
        public void Write(long position, bool value) { }
        public void Write(long position, byte value) { }
        public void Write(long position, char value) { }
        public void Write(long position, decimal value) { }
        public void Write(long position, double value) { }
        public void Write(long position, short value) { }
        public void Write(long position, int value) { }
        public void Write(long position, long value) { }
        [System.CLSCompliantAttribute(false)]
        public void Write(long position, sbyte value) { }
        public void Write(long position, float value) { }
        [System.CLSCompliantAttribute(false)]
        public void Write(long position, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public void Write(long position, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public void Write(long position, ulong value) { }
        public void WriteArray<T>(long position, T[] array, int offset, int count) where T : struct { }
        public void Write<T>(long position, ref T structure) where T : struct { }
    }
}
namespace System.Runtime.InteropServices
{
    [System.ObsoleteAttribute("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
    public sealed partial class AllowReversePInvokeCallsAttribute : System.Attribute
    {
        public AllowReversePInvokeCallsAttribute() { }
    }
    public readonly partial struct ArrayWithOffset : System.IEquatable<System.Runtime.InteropServices.ArrayWithOffset>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ArrayWithOffset(object? array, int offset) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Runtime.InteropServices.ArrayWithOffset obj) { throw null; }
        public object? GetArray() { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetOffset() { throw null; }
        public static bool operator ==(System.Runtime.InteropServices.ArrayWithOffset a, System.Runtime.InteropServices.ArrayWithOffset b) { throw null; }
        public static bool operator !=(System.Runtime.InteropServices.ArrayWithOffset a, System.Runtime.InteropServices.ArrayWithOffset b) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Struct, Inherited=false)]
    public sealed partial class BestFitMappingAttribute : System.Attribute
    {
        public bool ThrowOnUnmappableChar;
        public BestFitMappingAttribute(bool BestFitMapping) { }
        public bool BestFitMapping { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class BStrWrapper
    {
        public BStrWrapper(object? value) { }
        public BStrWrapper(string? value) { }
        public string? WrappedObject { get { throw null; } }
    }
    public enum CallingConvention
    {
        Winapi = 1,
        Cdecl = 2,
        StdCall = 3,
        ThisCall = 4,
        FastCall = 5,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class, Inherited=false)]
    public sealed partial class ClassInterfaceAttribute : System.Attribute
    {
        public ClassInterfaceAttribute(short classInterfaceType) { }
        public ClassInterfaceAttribute(System.Runtime.InteropServices.ClassInterfaceType classInterfaceType) { }
        public System.Runtime.InteropServices.ClassInterfaceType Value { get { throw null; } }
    }
    public enum ClassInterfaceType
    {
        None = 0,
        AutoDispatch = 1,
        AutoDual = 2,
    }
    [System.CLSCompliantAttribute(false)]
    public readonly partial struct CLong : System.IEquatable<System.Runtime.InteropServices.CLong>
    {
        private readonly int _dummyPrimitive;
        public CLong(int value) { throw null; }
        public CLong(nint value) { throw null; }
        public nint Value { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? o) { throw null; }
        public bool Equals(System.Runtime.InteropServices.CLong other) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Interface, Inherited=false)]
    public sealed partial class CoClassAttribute : System.Attribute
    {
        public CoClassAttribute(System.Type coClass) { }
        public System.Type CoClass { get { throw null; } }
    }
    public static partial class CollectionsMarshal
    {
        public static System.Span<T> AsSpan<T>(System.Collections.Generic.List<T>? list) { throw null; }
        public static ref TValue GetValueRefOrNullRef<TKey, TValue>(System.Collections.Generic.Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull { throw null; }
        public static ref TValue? GetValueRefOrAddDefault<TKey, TValue>(System.Collections.Generic.Dictionary<TKey, TValue> dictionary, TKey key, out bool exists) where TKey : notnull { throw null; }
        public static void SetCount<T>(System.Collections.Generic.List<T> list, int count) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited=false)]
    public sealed partial class ComDefaultInterfaceAttribute : System.Attribute
    {
        public ComDefaultInterfaceAttribute(System.Type defaultInterface) { }
        public System.Type Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Interface, Inherited=false)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class ComEventInterfaceAttribute : System.Attribute
    {
        public ComEventInterfaceAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] System.Type SourceInterface, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] System.Type EventProvider) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public System.Type EventProvider { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public System.Type SourceInterface { get { throw null; } }
    }
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static partial class ComEventsHelper
    {
        public static void Combine(object rcw, System.Guid iid, int dispid, System.Delegate d) { }
        public static System.Delegate? Remove(object rcw, System.Guid iid, int dispid, System.Delegate d) { throw null; }
    }
    public partial class COMException : System.Runtime.InteropServices.ExternalException
    {
        public COMException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected COMException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public COMException(string? message) { }
        public COMException(string? message, System.Exception? inner) { }
        public COMException(string? message, int errorCode) { }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface, Inherited=false)]
    public sealed partial class ComImportAttribute : System.Attribute
    {
        public ComImportAttribute() { }
    }
    public enum ComInterfaceType
    {
        InterfaceIsDual = 0,
        InterfaceIsIUnknown = 1,
        InterfaceIsIDispatch = 2,
        InterfaceIsIInspectable = 3,
    }
    public enum ComMemberType
    {
        Method = 0,
        PropGet = 1,
        PropSet = 2,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited=true)]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class ComSourceInterfacesAttribute : System.Attribute
    {
        public ComSourceInterfacesAttribute(string sourceInterfaces) { }
        public ComSourceInterfacesAttribute(System.Type sourceInterface) { }
        public ComSourceInterfacesAttribute(System.Type sourceInterface1, System.Type sourceInterface2) { }
        public ComSourceInterfacesAttribute(System.Type sourceInterface1, System.Type sourceInterface2, System.Type sourceInterface3) { }
        public ComSourceInterfacesAttribute(System.Type sourceInterface1, System.Type sourceInterface2, System.Type sourceInterface3, System.Type sourceInterface4) { }
        public string Value { get { throw null; } }
    }
    [System.Runtime.Versioning.UnsupportedOSPlatform("android")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("ios")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("tvos")]
    [System.CLSCompliantAttribute(false)]
    public abstract class ComWrappers
    {
        public static unsafe bool TryGetComInstance(object obj, out System.IntPtr unknown) { throw null; }
        public static unsafe bool TryGetObject(System.IntPtr unknown, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out object? obj) { throw null; }
        public struct ComInterfaceEntry
        {
            public System.Guid IID;
            public System.IntPtr Vtable;
        }
        public struct ComInterfaceDispatch
        {
            public System.IntPtr Vtable;
            public unsafe static T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class { throw null; }
        }
        public System.IntPtr GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags) { throw null; }
        protected unsafe abstract ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count);
        public object GetOrCreateObjectForComInstance(System.IntPtr externalComObject, CreateObjectFlags flags) { throw null; }
        protected abstract object? CreateObject(System.IntPtr externalComObject, CreateObjectFlags flags);
        public object GetOrRegisterObjectForComInstance(System.IntPtr externalComObject, CreateObjectFlags flags, object wrapper) { throw null; }
        public object GetOrRegisterObjectForComInstance(System.IntPtr externalComObject, CreateObjectFlags flags, object wrapper, System.IntPtr inner) { throw null; }
        protected abstract void ReleaseObjects(System.Collections.IEnumerable objects);
        public static void RegisterForTrackerSupport(ComWrappers instance) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void RegisterForMarshalling(ComWrappers instance) { }
        public static void GetIUnknownImpl(out System.IntPtr fpQueryInterface, out System.IntPtr fpAddRef, out System.IntPtr fpRelease) { throw null; }
    }
    [System.FlagsAttribute]
    public enum CreateComInterfaceFlags
    {
        None = 0,
        CallerDefinedIUnknown = 1,
        TrackerSupport = 2,
    }
    [System.FlagsAttribute]
    public enum CreateObjectFlags
    {
        None = 0,
        TrackerObject = 1,
        UniqueInstance = 2,
        Aggregation = 4,
        Unwrap = 8,
    }
    [System.CLSCompliantAttribute(false)]
    public readonly partial struct CULong : System.IEquatable<System.Runtime.InteropServices.CULong>
    {
        private readonly int _dummyPrimitive;
        public CULong(uint value) { throw null; }
        public CULong(nuint value) { throw null; }
        public nuint Value { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? o) { throw null; }
        public bool Equals(System.Runtime.InteropServices.CULong other) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("CurrencyWrapper and support for marshalling to the VARIANT type may be unavailable in future releases.")]
    public sealed partial class CurrencyWrapper
    {
        public CurrencyWrapper(decimal obj) { }
        public CurrencyWrapper(object obj) { }
        public decimal WrappedObject { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum CustomQueryInterfaceMode
    {
        Ignore = 0,
        Allow = 1,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum CustomQueryInterfaceResult
    {
        Handled = 0,
        NotHandled = 1,
        Failed = 2,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Module, Inherited=false)]
    public sealed partial class DefaultCharSetAttribute : System.Attribute
    {
        public DefaultCharSetAttribute(System.Runtime.InteropServices.CharSet charSet) { }
        public System.Runtime.InteropServices.CharSet CharSet { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Method, AllowMultiple=false)]
    public sealed partial class DefaultDllImportSearchPathsAttribute : System.Attribute
    {
        public DefaultDllImportSearchPathsAttribute(System.Runtime.InteropServices.DllImportSearchPath paths) { }
        public System.Runtime.InteropServices.DllImportSearchPath Paths { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter)]
    public sealed partial class DefaultParameterValueAttribute : System.Attribute
    {
        public DefaultParameterValueAttribute(object? value) { }
        public object? Value { get { throw null; } }
    }
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class DispatchWrapper
    {
        public DispatchWrapper(object? obj) { }
        public object? WrappedObject { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Event | System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Property, Inherited=false)]
    public sealed partial class DispIdAttribute : System.Attribute
    {
        public DispIdAttribute(int dispId) { }
        public int Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false)]
    public sealed partial class DllImportAttribute : System.Attribute
    {
        public bool BestFitMapping;
        public System.Runtime.InteropServices.CallingConvention CallingConvention;
        public System.Runtime.InteropServices.CharSet CharSet;
        public string? EntryPoint;
        public bool ExactSpelling;
        public bool PreserveSig;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
        public DllImportAttribute(string dllName) { }
        public string Value { get { throw null; } }
    }
    public delegate System.IntPtr DllImportResolver(string libraryName, System.Reflection.Assembly assembly, System.Runtime.InteropServices.DllImportSearchPath? searchPath);
    [System.FlagsAttribute]
    public enum DllImportSearchPath
    {
        LegacyBehavior = 0,
        AssemblyDirectory = 2,
        UseDllDirectoryForDependencies = 256,
        ApplicationDirectory = 512,
        UserDirectories = 1024,
        System32 = 2048,
        SafeDirectories = 4096,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Interface, AllowMultiple=false, Inherited=false)]
    public sealed class DynamicInterfaceCastableImplementationAttribute : Attribute
    {
        public DynamicInterfaceCastableImplementationAttribute() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class ErrorWrapper
    {
        public ErrorWrapper(System.Exception e) { }
        public ErrorWrapper(int errorCode) { }
        public ErrorWrapper(object errorCode) { }
        public int ErrorCode { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Delegate | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Struct, Inherited=false)]
    public sealed partial class GuidAttribute : System.Attribute
    {
        public GuidAttribute(string guid) { }
        public string Value { get { throw null; } }
    }
    public readonly partial struct HandleRef
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public HandleRef(object? wrapper, System.IntPtr handle) { throw null; }
        public System.IntPtr Handle { get { throw null; } }
        public object? Wrapper { get { throw null; } }
        public static explicit operator System.IntPtr (System.Runtime.InteropServices.HandleRef value) { throw null; }
        public static System.IntPtr ToIntPtr(System.Runtime.InteropServices.HandleRef value) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial interface ICustomAdapter
    {
        object GetUnderlyingObject();
    }
    public partial interface ICustomFactory
    {
        System.MarshalByRefObject CreateInstance(System.Type serverType);
    }
    public partial interface ICustomMarshaler
    {
        void CleanUpManagedData(object ManagedObj);
        void CleanUpNativeData(System.IntPtr pNativeData);
        int GetNativeDataSize();
        System.IntPtr MarshalManagedToNative(object ManagedObj);
        object MarshalNativeToManaged(System.IntPtr pNativeData);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial interface ICustomQueryInterface
    {
        System.Runtime.InteropServices.CustomQueryInterfaceResult GetInterface(ref System.Guid iid, out System.IntPtr ppv);
    }
    public partial interface IDynamicInterfaceCastable
    {
        bool IsInterfaceImplemented(System.RuntimeTypeHandle interfaceType, bool throwIfNotImplemented);
        System.RuntimeTypeHandle GetInterfaceImplementation(System.RuntimeTypeHandle interfaceType);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Interface, Inherited=false)]
    public sealed partial class InterfaceTypeAttribute : System.Attribute
    {
        public InterfaceTypeAttribute(short interfaceType) { }
        public InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType interfaceType) { }
        public System.Runtime.InteropServices.ComInterfaceType Value { get { throw null; } }
    }
    public partial class InvalidComObjectException : System.SystemException
    {
        public InvalidComObjectException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected InvalidComObjectException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidComObjectException(string? message) { }
        public InvalidComObjectException(string? message, System.Exception? inner) { }
    }
    public partial class InvalidOleVariantTypeException : System.SystemException
    {
        public InvalidOleVariantTypeException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected InvalidOleVariantTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidOleVariantTypeException(string? message) { }
        public InvalidOleVariantTypeException(string? message, System.Exception? inner) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false)]
    public sealed partial class LCIDConversionAttribute : System.Attribute
    {
        public LCIDConversionAttribute(int lcid) { }
        public int Value { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple = false, Inherited=false)]
    public sealed partial class LibraryImportAttribute : System.Attribute
    {
        public LibraryImportAttribute(string libraryName) { }
        public string LibraryName { get { throw null; } }
        public string? EntryPoint { get { throw null; } set { } }
        public bool SetLastError { get { throw null; } set { }}
        public System.Runtime.InteropServices.StringMarshalling StringMarshalling { get { throw null; } set { } }
        public System.Type? StringMarshallingCustomType { get { throw null; } set { } }
    }
    public static partial class Marshal
    {
        public static readonly int SystemDefaultCharSize;
        public static readonly int SystemMaxDBCSCharSize;
        public static int AddRef(System.IntPtr pUnk) { throw null; }
        public static System.IntPtr AllocCoTaskMem(int cb) { throw null; }
        public static System.IntPtr AllocHGlobal(int cb) { throw null; }
        public static System.IntPtr AllocHGlobal(System.IntPtr cb) { throw null; }
        public static bool AreComObjectsAvailableForCleanup() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static object BindToMoniker(string monikerName) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void ChangeWrapperHandleStrength(object otp, bool fIsWeak) { }
        public static void CleanupUnusedObjectsInCurrentContext() { }
        public static void Copy(byte[] source, int startIndex, System.IntPtr destination, int length) { }
        public static void Copy(char[] source, int startIndex, System.IntPtr destination, int length) { }
        public static void Copy(double[] source, int startIndex, System.IntPtr destination, int length) { }
        public static void Copy(short[] source, int startIndex, System.IntPtr destination, int length) { }
        public static void Copy(int[] source, int startIndex, System.IntPtr destination, int length) { }
        public static void Copy(long[] source, int startIndex, System.IntPtr destination, int length) { }
        public static void Copy(System.IntPtr source, byte[] destination, int startIndex, int length) { }
        public static void Copy(System.IntPtr source, char[] destination, int startIndex, int length) { }
        public static void Copy(System.IntPtr source, double[] destination, int startIndex, int length) { }
        public static void Copy(System.IntPtr source, short[] destination, int startIndex, int length) { }
        public static void Copy(System.IntPtr source, int[] destination, int startIndex, int length) { }
        public static void Copy(System.IntPtr source, long[] destination, int startIndex, int length) { }
        public static void Copy(System.IntPtr source, System.IntPtr[] destination, int startIndex, int length) { }
        public static void Copy(System.IntPtr source, float[] destination, int startIndex, int length) { }
        public static void Copy(System.IntPtr[] source, int startIndex, System.IntPtr destination, int length) { }
        public static void Copy(float[] source, int startIndex, System.IntPtr destination, int length) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.IntPtr CreateAggregatedObject(System.IntPtr pOuter, object o) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.IntPtr CreateAggregatedObject<T>(System.IntPtr pOuter, T o) where T : notnull { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("o")]
        public static object? CreateWrapperOfType(object? o, System.Type t) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static TWrapper CreateWrapperOfType<T, TWrapper>(T? o) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available. Use the DestroyStructure<T> overload instead.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void DestroyStructure(System.IntPtr ptr, System.Type structuretype) { }
        public static void DestroyStructure<T>(System.IntPtr ptr) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static int FinalReleaseComObject(object o) { throw null; }
        public static void FreeBSTR(System.IntPtr ptr) { }
        public static void FreeCoTaskMem(System.IntPtr ptr) { }
        public static void FreeHGlobal(System.IntPtr hglobal) { }
        public static System.Guid GenerateGuidForType(System.Type type) { throw null; }
        public static string? GenerateProgIdForType(System.Type type) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.IntPtr GetComInterfaceForObject(object o, System.Type T) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.IntPtr GetComInterfaceForObject(object o, System.Type T, System.Runtime.InteropServices.CustomQueryInterfaceMode mode) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.IntPtr GetComInterfaceForObject<T, TInterface>([System.Diagnostics.CodeAnalysis.DisallowNullAttribute] T o) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static object? GetComObjectData(object obj, object key) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the delegate might not be available. Use the GetDelegateForFunctionPointer<TDelegate> overload instead.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.Delegate GetDelegateForFunctionPointer(System.IntPtr ptr, System.Type t) { throw null; }
        public static TDelegate GetDelegateForFunctionPointer<TDelegate>(System.IntPtr ptr) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static int GetEndComSlot(System.Type t) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("GetExceptionCode() may be unavailable in future releases.")]
        public static int GetExceptionCode() { throw null; }
        public static System.Exception? GetExceptionForHR(int errorCode) { throw null; }
        public static System.Exception? GetExceptionForHR(int errorCode, System.IntPtr errorInfo) { throw null; }
        public static System.IntPtr GetExceptionPointers() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the delegate might not be available. Use the GetFunctionPointerForDelegate<TDelegate> overload instead.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.IntPtr GetFunctionPointerForDelegate(System.Delegate d) { throw null; }
        public static System.IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d) where TDelegate : notnull { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles("Windows only assigns HINSTANCE to assemblies loaded from disk. This API will return -1 for modules without a file on disk.")]
        public static System.IntPtr GetHINSTANCE(System.Reflection.Module m) { throw null; }
        public static int GetHRForException(System.Exception? e) { throw null; }
        public static int GetHRForLastWin32Error() { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.IntPtr GetIDispatchForObject(object o) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.IntPtr GetIUnknownForObject(object o) { throw null; }
        public static int GetLastPInvokeError() { throw null; }
        public static int GetLastSystemError() { throw null; }
        public static int GetLastWin32Error() { throw null; }
        public static string GetLastPInvokeErrorMessage() { throw null; }
        public static string GetPInvokeErrorMessage(int error) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void GetNativeVariantForObject(object? obj, System.IntPtr pDstNativeVariant) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void GetNativeVariantForObject<T>(T? obj, System.IntPtr pDstNativeVariant) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static object GetObjectForIUnknown(System.IntPtr pUnk) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static object? GetObjectForNativeVariant(System.IntPtr pSrcNativeVariant) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static T? GetObjectForNativeVariant<T>(System.IntPtr pSrcNativeVariant) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static object?[] GetObjectsForNativeVariants(System.IntPtr aSrcNativeVariant, int cVars) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static T[] GetObjectsForNativeVariants<T>(System.IntPtr aSrcNativeVariant, int cVars) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static int GetStartComSlot(System.Type t) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static object GetTypedObjectForIUnknown(System.IntPtr pUnk, System.Type t) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static System.Type? GetTypeFromCLSID(System.Guid clsid) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static string GetTypeInfoName(System.Runtime.InteropServices.ComTypes.ITypeInfo typeInfo) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static object GetUniqueObjectForIUnknown(System.IntPtr unknown) { throw null; }
        public static void InitHandle(SafeHandle safeHandle, IntPtr handle) { }
        public static bool IsComObject(object o) { throw null; }
        public static bool IsTypeVisibleFromCom(System.Type t) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.IntPtr OffsetOf(System.Type t, string fieldName) { throw null; }
        public static System.IntPtr OffsetOf<T>(string fieldName) { throw null; }
        public static void Prelink(System.Reflection.MethodInfo m) { }
        public static void PrelinkAll(System.Type c) { }
        public static string? PtrToStringAnsi(System.IntPtr ptr) { throw null; }
        public static string PtrToStringAnsi(System.IntPtr ptr, int len) { throw null; }
        public static string? PtrToStringAuto(System.IntPtr ptr) { throw null; }
        public static string? PtrToStringAuto(System.IntPtr ptr, int len) { throw null; }
        public static string PtrToStringBSTR(System.IntPtr ptr) { throw null; }
        public static string? PtrToStringUni(System.IntPtr ptr) { throw null; }
        public static string PtrToStringUni(System.IntPtr ptr, int len) { throw null; }
        public static string? PtrToStringUTF8(System.IntPtr ptr) { throw null; }
        public static string PtrToStringUTF8(System.IntPtr ptr, int byteLen) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void PtrToStructure(System.IntPtr ptr, object structure) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static object? PtrToStructure(System.IntPtr ptr, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors| System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type structureType) { throw null; }
        public static T? PtrToStructure<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]T>(System.IntPtr ptr) { throw null; }
        public static void PtrToStructure<T>(System.IntPtr ptr, [System.Diagnostics.CodeAnalysis.DisallowNullAttribute] T structure) { }
        public static int QueryInterface(System.IntPtr pUnk, ref System.Guid iid, out System.IntPtr ppv) { throw null; }
        public static byte ReadByte(System.IntPtr ptr) { throw null; }
        public static byte ReadByte(System.IntPtr ptr, int ofs) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("ReadByte(Object, Int32) may be unavailable in future releases.")]
        public static byte ReadByte(object ptr, int ofs) { throw null; }
        public static short ReadInt16(System.IntPtr ptr) { throw null; }
        public static short ReadInt16(System.IntPtr ptr, int ofs) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("ReadInt16(Object, Int32) may be unavailable in future releases.")]
        public static short ReadInt16(object ptr, int ofs) { throw null; }
        public static int ReadInt32(System.IntPtr ptr) { throw null; }
        public static int ReadInt32(System.IntPtr ptr, int ofs) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("ReadInt32(Object, Int32) may be unavailable in future releases.")]
        public static int ReadInt32(object ptr, int ofs) { throw null; }
        public static long ReadInt64(System.IntPtr ptr) { throw null; }
        public static long ReadInt64(System.IntPtr ptr, int ofs) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("ReadInt64(Object, Int32) may be unavailable in future releases.")]
        public static long ReadInt64(object ptr, int ofs) { throw null; }
        public static System.IntPtr ReadIntPtr(System.IntPtr ptr) { throw null; }
        public static System.IntPtr ReadIntPtr(System.IntPtr ptr, int ofs) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("ReadIntPtr(Object, Int32) may be unavailable in future releases.")]
        public static System.IntPtr ReadIntPtr(object ptr, int ofs) { throw null; }
        public static System.IntPtr ReAllocCoTaskMem(System.IntPtr pv, int cb) { throw null; }
        public static System.IntPtr ReAllocHGlobal(System.IntPtr pv, System.IntPtr cb) { throw null; }
        public static int Release(System.IntPtr pUnk) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static int ReleaseComObject(object o) { throw null; }
        public static System.IntPtr SecureStringToBSTR(System.Security.SecureString s) { throw null; }
        public static System.IntPtr SecureStringToCoTaskMemAnsi(System.Security.SecureString s) { throw null; }
        public static System.IntPtr SecureStringToCoTaskMemUnicode(System.Security.SecureString s) { throw null; }
        public static System.IntPtr SecureStringToGlobalAllocAnsi(System.Security.SecureString s) { throw null; }
        public static System.IntPtr SecureStringToGlobalAllocUnicode(System.Security.SecureString s) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool SetComObjectData(object obj, object key, object? data) { throw null; }
        public static void SetLastPInvokeError(int error) { throw null; }
        public static void SetLastSystemError(int error) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available. Use the SizeOf<T> overload instead.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static int SizeOf(object structure) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available. Use the SizeOf<T> overload instead.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static int SizeOf(System.Type t) { throw null; }
        public static int SizeOf<T>() { throw null; }
        public static int SizeOf<T>(T structure) { throw null; }
        public static System.IntPtr StringToBSTR(string? s) { throw null; }
        public static System.IntPtr StringToCoTaskMemAnsi(string? s) { throw null; }
        public static System.IntPtr StringToCoTaskMemAuto(string? s) { throw null; }
        public static System.IntPtr StringToCoTaskMemUni(string? s) { throw null; }
        public static System.IntPtr StringToCoTaskMemUTF8(string? s) { throw null; }
        public static System.IntPtr StringToHGlobalAnsi(string? s) { throw null; }
        public static System.IntPtr StringToHGlobalAuto(string? s) { throw null; }
        public static System.IntPtr StringToHGlobalUni(string? s) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available. Use the StructureToPtr<T> overload instead.")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static void StructureToPtr(object structure, System.IntPtr ptr, bool fDeleteOld) { }
        public static void StructureToPtr<T>([System.Diagnostics.CodeAnalysis.DisallowNullAttribute] T structure, System.IntPtr ptr, bool fDeleteOld) { }
        public static void ThrowExceptionForHR(int errorCode) { }
        public static void ThrowExceptionForHR(int errorCode, System.IntPtr errorInfo) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.IntPtr UnsafeAddrOfPinnedArrayElement(System.Array arr, int index) { throw null; }
        public static System.IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index) { throw null; }
        public static void WriteByte(System.IntPtr ptr, byte val) { }
        public static void WriteByte(System.IntPtr ptr, int ofs, byte val) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("WriteByte(Object, Int32, Byte) may be unavailable in future releases.")]
        public static void WriteByte(object ptr, int ofs, byte val) { }
        public static void WriteInt16(System.IntPtr ptr, char val) { }
        public static void WriteInt16(System.IntPtr ptr, short val) { }
        public static void WriteInt16(System.IntPtr ptr, int ofs, char val) { }
        public static void WriteInt16(System.IntPtr ptr, int ofs, short val) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("WriteInt16(Object, Int32, Char) may be unavailable in future releases.")]
        public static void WriteInt16(object ptr, int ofs, char val) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("WriteInt16(Object, Int32, Int16) may be unavailable in future releases.")]
        public static void WriteInt16(object ptr, int ofs, short val) { }
        public static void WriteInt32(System.IntPtr ptr, int val) { }
        public static void WriteInt32(System.IntPtr ptr, int ofs, int val) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("WriteInt32(Object, Int32, Int32) may be unavailable in future releases.")]
        public static void WriteInt32(object ptr, int ofs, int val) { }
        public static void WriteInt64(System.IntPtr ptr, int ofs, long val) { }
        public static void WriteInt64(System.IntPtr ptr, long val) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("WriteInt64(Object, Int32, Int64) may be unavailable in future releases.")]
        public static void WriteInt64(object ptr, int ofs, long val) { }
        public static void WriteIntPtr(System.IntPtr ptr, int ofs, System.IntPtr val) { }
        public static void WriteIntPtr(System.IntPtr ptr, System.IntPtr val) { }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Marshalling code for the object might not be available")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("WriteIntPtr(Object, Int32, IntPtr) may be unavailable in future releases.")]
        public static void WriteIntPtr(object ptr, int ofs, System.IntPtr val) { }
        public static void ZeroFreeBSTR(System.IntPtr s) { }
        public static void ZeroFreeCoTaskMemAnsi(System.IntPtr s) { }
        public static void ZeroFreeCoTaskMemUnicode(System.IntPtr s) { }
        public static void ZeroFreeCoTaskMemUTF8(System.IntPtr s) { }
        public static void ZeroFreeGlobalAllocAnsi(System.IntPtr s) { }
        public static void ZeroFreeGlobalAllocUnicode(System.IntPtr s) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.ReturnValue, Inherited=false)]
    public sealed partial class MarshalAsAttribute : System.Attribute
    {
        public System.Runtime.InteropServices.UnmanagedType ArraySubType;
        public int IidParameterIndex;
        public string? MarshalCookie;
        public string? MarshalType;
        public System.Type? MarshalTypeRef;
        public System.Runtime.InteropServices.VarEnum SafeArraySubType;
        public System.Type? SafeArrayUserDefinedSubType;
        public int SizeConst;
        public short SizeParamIndex;
        public MarshalAsAttribute(short unmanagedType) { }
        public MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType unmanagedType) { }
        public System.Runtime.InteropServices.UnmanagedType Value { get { throw null; } }
    }
    public partial class MarshalDirectiveException : System.SystemException
    {
        public MarshalDirectiveException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected MarshalDirectiveException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MarshalDirectiveException(string? message) { }
        public MarshalDirectiveException(string? message, System.Exception? inner) { }
    }
    public static partial class NativeLibrary
    {
        public static void Free(System.IntPtr handle) { }
        public static System.IntPtr GetMainProgramHandle() { throw null; }
        public static System.IntPtr GetExport(System.IntPtr handle, string name) { throw null; }
        public static System.IntPtr Load(string libraryPath) { throw null; }
        public static System.IntPtr Load(string libraryName, System.Reflection.Assembly assembly, System.Runtime.InteropServices.DllImportSearchPath? searchPath) { throw null; }
        public static void SetDllImportResolver(System.Reflection.Assembly assembly, System.Runtime.InteropServices.DllImportResolver resolver) { }
        public static bool TryGetExport(System.IntPtr handle, string name, out System.IntPtr address) { throw null; }
        public static bool TryLoad(string libraryPath, out System.IntPtr handle) { throw null; }
        public static bool TryLoad(string libraryName, System.Reflection.Assembly assembly, System.Runtime.InteropServices.DllImportSearchPath? searchPath, out System.IntPtr handle) { throw null; }
    }
    public static unsafe partial class NativeMemory
    {
        [System.CLSCompliantAttribute(false)]
        public static void* AlignedAlloc(nuint byteCount, nuint alignment) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void AlignedFree(void* ptr) { }
        [System.CLSCompliantAttribute(false)]
        public static void* AlignedRealloc(void* ptr, nuint byteCount, nuint alignment) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void* Alloc(nuint byteCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void* Alloc(nuint elementCount, nuint elementSize) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void* AllocZeroed(nuint byteCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void* AllocZeroed(nuint elementCount, nuint elementSize) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void Free(void* ptr) { }
        [System.CLSCompliantAttribute(false)]
        public static void* Realloc(void* ptr, nuint byteCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void Clear(void* ptr, nuint byteCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void Copy(void* source, void* destination, nuint byteCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void Fill(void* ptr, nuint byteCount, byte value) { throw null; }
    }
    public readonly partial struct NFloat : System.IComparable, System.IComparable<System.Runtime.InteropServices.NFloat>, System.IEquatable<System.Runtime.InteropServices.NFloat>, System.IFormattable, System.IParsable<System.Runtime.InteropServices.NFloat>, System.ISpanFormattable, System.ISpanParsable<System.Runtime.InteropServices.NFloat>, System.Numerics.IAdditionOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.IAdditiveIdentity<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.IBinaryFloatingPointIeee754<System.Runtime.InteropServices.NFloat>, System.Numerics.IBinaryNumber<System.Runtime.InteropServices.NFloat>, System.Numerics.IBitwiseOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.IComparisonOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, bool>, System.Numerics.IDecrementOperators<System.Runtime.InteropServices.NFloat>, System.Numerics.IDivisionOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.IEqualityOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, bool>, System.Numerics.IExponentialFunctions<System.Runtime.InteropServices.NFloat>, System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>, System.Numerics.IFloatingPointConstants<System.Runtime.InteropServices.NFloat>, System.Numerics.IFloatingPointIeee754<System.Runtime.InteropServices.NFloat>, System.Numerics.IHyperbolicFunctions<System.Runtime.InteropServices.NFloat>, System.Numerics.IIncrementOperators<System.Runtime.InteropServices.NFloat>, System.Numerics.ILogarithmicFunctions<System.Runtime.InteropServices.NFloat>, System.Numerics.IMinMaxValue<System.Runtime.InteropServices.NFloat>, System.Numerics.IModulusOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.IMultiplicativeIdentity<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.IMultiplyOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.INumber<System.Runtime.InteropServices.NFloat>, System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>, System.Numerics.IPowerFunctions<System.Runtime.InteropServices.NFloat>, System.Numerics.IRootFunctions<System.Runtime.InteropServices.NFloat>, System.Numerics.ISignedNumber<System.Runtime.InteropServices.NFloat>, System.Numerics.ISubtractionOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.ITrigonometricFunctions<System.Runtime.InteropServices.NFloat>, System.Numerics.IUnaryNegationOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.Numerics.IUnaryPlusOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>, System.IUtf8SpanFormattable
    {
        private readonly int _dummyPrimitive;
        public NFloat(double value) { throw null; }
        public NFloat(float value) { throw null; }
        public static System.Runtime.InteropServices.NFloat E { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat Epsilon { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat MaxValue { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat MinValue { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat NaN { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat NegativeInfinity { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat NegativeZero { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat Pi { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat PositiveInfinity { get { throw null; } }
        public static int Size { get { throw null; } }
        static System.Runtime.InteropServices.NFloat System.Numerics.IAdditiveIdentity<System.Runtime.InteropServices.NFloat,System.Runtime.InteropServices.NFloat>.AdditiveIdentity { get { throw null; } }
        static System.Runtime.InteropServices.NFloat System.Numerics.IBinaryNumber<System.Runtime.InteropServices.NFloat>.AllBitsSet { get { throw null; } }
        static System.Runtime.InteropServices.NFloat System.Numerics.IMultiplicativeIdentity<System.Runtime.InteropServices.NFloat,System.Runtime.InteropServices.NFloat>.MultiplicativeIdentity { get { throw null; } }
        static System.Runtime.InteropServices.NFloat System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.One { get { throw null; } }
        static int System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.Radix { get { throw null; } }
        static System.Runtime.InteropServices.NFloat System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.Zero { get { throw null; } }
        static System.Runtime.InteropServices.NFloat System.Numerics.ISignedNumber<System.Runtime.InteropServices.NFloat>.NegativeOne { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat Tau { get { throw null; } }
        public double Value { get { throw null; } }
        public static System.Runtime.InteropServices.NFloat Abs(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat Acos(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Acosh(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat AcosPi(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Asin(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Asinh(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat AsinPi(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Atan(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Atan2(System.Runtime.InteropServices.NFloat y, System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Atan2Pi(System.Runtime.InteropServices.NFloat y, System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Atanh(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat AtanPi(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat BitDecrement(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat BitIncrement(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Cbrt(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Ceiling(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Clamp(System.Runtime.InteropServices.NFloat value, System.Runtime.InteropServices.NFloat min, System.Runtime.InteropServices.NFloat max) { throw null; }
        public int CompareTo(object? obj) { throw null; }
        public int CompareTo(System.Runtime.InteropServices.NFloat other) { throw null; }
        public static System.Runtime.InteropServices.NFloat CopySign(System.Runtime.InteropServices.NFloat value, System.Runtime.InteropServices.NFloat sign) { throw null; }
        public static System.Runtime.InteropServices.NFloat Cos(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Cosh(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat CosPi(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat CreateChecked<TOther>(TOther value) where TOther : System.Numerics.INumberBase<TOther> { throw null; }
        public static System.Runtime.InteropServices.NFloat CreateSaturating<TOther>(TOther value) where TOther : System.Numerics.INumberBase<TOther> { throw null; }
        public static System.Runtime.InteropServices.NFloat CreateTruncating<TOther>(TOther value) where TOther : System.Numerics.INumberBase<TOther> { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Runtime.InteropServices.NFloat other) { throw null; }
        public static System.Runtime.InteropServices.NFloat Exp(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Exp10(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Exp10M1(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Exp2(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Exp2M1(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat ExpM1(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Floor(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat FusedMultiplyAdd(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right, System.Runtime.InteropServices.NFloat addend) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Runtime.InteropServices.NFloat Hypot(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat Ieee754Remainder(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static int ILogB(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static bool IsEvenInteger(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsFinite(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsInfinity(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsInteger(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsNaN(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsNegative(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsNegativeInfinity(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsNormal(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsOddInteger(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsPositive(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsPositiveInfinity(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsPow2(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsRealNumber(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool IsSubnormal(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat Lerp(System.Runtime.InteropServices.NFloat value1, System.Runtime.InteropServices.NFloat value2, System.Runtime.InteropServices.NFloat amount) { throw null; }
        public static System.Runtime.InteropServices.NFloat Log(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Log(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat newBase) { throw null; }
        public static System.Runtime.InteropServices.NFloat Log10(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Log10P1(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Log2(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat Log2P1(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat LogP1(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Max(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat MaxMagnitude(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat MaxMagnitudeNumber(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat MaxNumber(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat Min(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat MinMagnitude(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat MinMagnitudeNumber(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat MinNumber(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator +(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static explicit operator checked byte (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator checked char (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator checked short (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator checked int (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator checked long (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator checked System.Int128 (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator checked nint (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator checked sbyte (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator checked ushort (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator checked uint (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator checked ulong (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator checked System.UInt128 (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator checked nuint (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator --(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator /(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static bool operator ==(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static explicit operator System.Runtime.InteropServices.NFloat (decimal value) { throw null; }
        public static explicit operator System.Runtime.InteropServices.NFloat (double value) { throw null; }
        public static explicit operator System.Runtime.InteropServices.NFloat (System.Int128 value) { throw null; }
        public static explicit operator byte (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator char (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator decimal (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator System.Half (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator System.Int128 (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator short (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator int (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator long (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator nint (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte (System.Runtime.InteropServices.NFloat value) { throw null; }
        public static explicit operator float (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.UInt128 (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator nuint (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Runtime.InteropServices.NFloat (System.UInt128 value) { throw null; }
        public static bool operator >(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static bool operator >=(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static implicit operator System.Runtime.InteropServices.NFloat (byte value) { throw null; }
        public static implicit operator System.Runtime.InteropServices.NFloat (char value) { throw null; }
        public static implicit operator System.Runtime.InteropServices.NFloat (short value) { throw null; }
        public static implicit operator System.Runtime.InteropServices.NFloat (int value) { throw null; }
        public static implicit operator System.Runtime.InteropServices.NFloat (long value) { throw null; }
        public static implicit operator System.Runtime.InteropServices.NFloat (nint value) { throw null; }
        public static implicit operator System.Runtime.InteropServices.NFloat (System.Half value) { throw null; }
        public static implicit operator double (System.Runtime.InteropServices.NFloat value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Runtime.InteropServices.NFloat (sbyte value) { throw null; }
        public static implicit operator System.Runtime.InteropServices.NFloat (float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Runtime.InteropServices.NFloat (ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Runtime.InteropServices.NFloat (uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Runtime.InteropServices.NFloat (ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Runtime.InteropServices.NFloat (nuint value) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator ++(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static bool operator !=(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static bool operator <(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static bool operator <=(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator %(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator *(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator -(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator -(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat operator +(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowLeadingWhite | System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowTrailingWhite, System.IFormatProvider? provider = null) { throw null; }
        public static System.Runtime.InteropServices.NFloat Parse(System.ReadOnlySpan<char> s, System.IFormatProvider? provider) { throw null; }
        public static System.Runtime.InteropServices.NFloat Parse(string s) { throw null; }
        public static System.Runtime.InteropServices.NFloat Parse(string s, System.Globalization.NumberStyles style) { throw null; }
        public static System.Runtime.InteropServices.NFloat Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider? provider) { throw null; }
        public static System.Runtime.InteropServices.NFloat Parse(string s, System.IFormatProvider? provider) { throw null; }
        public static System.Runtime.InteropServices.NFloat Pow(System.Runtime.InteropServices.NFloat x, System.Runtime.InteropServices.NFloat y) { throw null; }
        public static System.Runtime.InteropServices.NFloat ReciprocalEstimate(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat ReciprocalSqrtEstimate(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat RootN(System.Runtime.InteropServices.NFloat x, int n) { throw null; }
        public static System.Runtime.InteropServices.NFloat Round(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Round(System.Runtime.InteropServices.NFloat x, int digits) { throw null; }
        public static System.Runtime.InteropServices.NFloat Round(System.Runtime.InteropServices.NFloat x, int digits, System.MidpointRounding mode) { throw null; }
        public static System.Runtime.InteropServices.NFloat Round(System.Runtime.InteropServices.NFloat x, System.MidpointRounding mode) { throw null; }
        public static System.Runtime.InteropServices.NFloat ScaleB(System.Runtime.InteropServices.NFloat x, int n) { throw null; }
        public static int Sign(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat Sin(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static (System.Runtime.InteropServices.NFloat Sin, System.Runtime.InteropServices.NFloat Cos) SinCos(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static (System.Runtime.InteropServices.NFloat SinPi, System.Runtime.InteropServices.NFloat CosPi) SinCosPi(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Sinh(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat SinPi(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Sqrt(System.Runtime.InteropServices.NFloat x) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IAdditionOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator checked +(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IBitwiseOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator &(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IBitwiseOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator |(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IBitwiseOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator ^(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IBitwiseOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator ~(System.Runtime.InteropServices.NFloat value) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IDecrementOperators<System.Runtime.InteropServices.NFloat>.operator checked --(System.Runtime.InteropServices.NFloat value) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IDivisionOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator checked /(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        int System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>.GetExponentByteCount() { throw null; }
        int System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>.GetExponentShortestBitLength() { throw null; }
        int System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>.GetSignificandBitLength() { throw null; }
        int System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>.GetSignificandByteCount() { throw null; }
        bool System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>.TryWriteExponentBigEndian(System.Span<byte> destination, out int bytesWritten) { throw null; }
        bool System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>.TryWriteExponentLittleEndian(System.Span<byte> destination, out int bytesWritten) { throw null; }
        bool System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>.TryWriteSignificandBigEndian(System.Span<byte> destination, out int bytesWritten) { throw null; }
        bool System.Numerics.IFloatingPoint<System.Runtime.InteropServices.NFloat>.TryWriteSignificandLittleEndian(System.Span<byte> destination, out int bytesWritten) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IIncrementOperators<System.Runtime.InteropServices.NFloat>.operator checked ++(System.Runtime.InteropServices.NFloat value) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IMultiplyOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator checked *(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.IsCanonical(System.Runtime.InteropServices.NFloat value) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.IsComplexNumber(System.Runtime.InteropServices.NFloat value) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.IsImaginaryNumber(System.Runtime.InteropServices.NFloat value) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.IsZero(System.Runtime.InteropServices.NFloat value) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.TryConvertFromChecked<TOther>(TOther value, out System.Runtime.InteropServices.NFloat result) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.TryConvertFromSaturating<TOther>(TOther value, out System.Runtime.InteropServices.NFloat result) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.TryConvertFromTruncating<TOther>(TOther value, out System.Runtime.InteropServices.NFloat result) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.TryConvertToChecked<TOther>(System.Runtime.InteropServices.NFloat value, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TOther result) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.TryConvertToSaturating<TOther>(System.Runtime.InteropServices.NFloat value, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TOther result) { throw null; }
        static bool System.Numerics.INumberBase<System.Runtime.InteropServices.NFloat>.TryConvertToTruncating<TOther>(System.Runtime.InteropServices.NFloat value, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TOther result) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.ISubtractionOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator checked -(System.Runtime.InteropServices.NFloat left, System.Runtime.InteropServices.NFloat right) { throw null; }
        static System.Runtime.InteropServices.NFloat System.Numerics.IUnaryNegationOperators<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>.operator checked -(System.Runtime.InteropServices.NFloat value) { throw null; }
        public static System.Runtime.InteropServices.NFloat Tan(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat Tanh(System.Runtime.InteropServices.NFloat x) { throw null; }
        public static System.Runtime.InteropServices.NFloat TanPi(System.Runtime.InteropServices.NFloat x) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider? provider) { throw null; }
        public string ToString([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] string? format) { throw null; }
        public string ToString([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] string? format, System.IFormatProvider? provider) { throw null; }
        public static System.Runtime.InteropServices.NFloat Truncate(System.Runtime.InteropServices.NFloat x) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] System.ReadOnlySpan<char> format = default(System.ReadOnlySpan<char>), System.IFormatProvider? provider = null) { throw null; }
        public bool TryFormat(System.Span<byte> utf8Destination, out int bytesWritten, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] System.ReadOnlySpan<char> format = default(System.ReadOnlySpan<char>), System.IFormatProvider? provider = null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider? provider, out System.Runtime.InteropServices.NFloat result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.IFormatProvider? provider, out System.Runtime.InteropServices.NFloat result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, out System.Runtime.InteropServices.NFloat result) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? s, System.Globalization.NumberStyles style, System.IFormatProvider? provider, out System.Runtime.InteropServices.NFloat result) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? s, System.IFormatProvider? provider, out System.Runtime.InteropServices.NFloat result) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? s, out System.Runtime.InteropServices.NFloat result) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter, Inherited=false)]
    public sealed partial class OptionalAttribute : System.Attribute
    {
        public OptionalAttribute() { }
    }
    public enum PosixSignal
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        SIGTSTP = -10,
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        SIGTTOU = -9,
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        SIGTTIN = -8,
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        SIGWINCH = -7,
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        SIGCONT = -6,
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        SIGCHLD = -5,
        SIGTERM = -4,
        SIGQUIT = -3,
        SIGINT = -2,
        SIGHUP = -1,
    }
    public sealed partial class PosixSignalContext
    {
        public PosixSignalContext(System.Runtime.InteropServices.PosixSignal signal) { }
        public bool Cancel { get { throw null; } set { } }
        public System.Runtime.InteropServices.PosixSignal Signal { get { throw null; } }
    }
    public sealed partial class PosixSignalRegistration : System.IDisposable
    {
        internal PosixSignalRegistration() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Runtime.InteropServices.PosixSignalRegistration Create(System.Runtime.InteropServices.PosixSignal signal, System.Action<System.Runtime.InteropServices.PosixSignalContext> handler) { throw null; }
        public void Dispose() { }
        ~PosixSignalRegistration() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false)]
    public sealed partial class PreserveSigAttribute : System.Attribute
    {
        public PreserveSigAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited=false)]
    public sealed partial class ProgIdAttribute : System.Attribute
    {
        public ProgIdAttribute(string progId) { }
        public string Value { get { throw null; } }
    }
    public partial class SafeArrayRankMismatchException : System.SystemException
    {
        public SafeArrayRankMismatchException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected SafeArrayRankMismatchException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SafeArrayRankMismatchException(string? message) { }
        public SafeArrayRankMismatchException(string? message, System.Exception? inner) { }
    }
    public partial class SafeArrayTypeMismatchException : System.SystemException
    {
        public SafeArrayTypeMismatchException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected SafeArrayTypeMismatchException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SafeArrayTypeMismatchException(string? message) { }
        public SafeArrayTypeMismatchException(string? message, System.Exception? inner) { }
    }
    public partial class SEHException : System.Runtime.InteropServices.ExternalException
    {
        public SEHException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected SEHException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SEHException(string? message) { }
        public SEHException(string? message, System.Exception? inner) { }
        public virtual bool CanResume() { throw null; }
    }
    public partial class StandardOleMarshalObject : System.MarshalByRefObject
    {
        protected StandardOleMarshalObject() { }
    }
    public enum StringMarshalling
    {
        Custom = 0,
        Utf8 = 1,
        Utf16 = 2,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Delegate | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Struct, AllowMultiple=false, Inherited=false)]
    public sealed partial class TypeIdentifierAttribute : System.Attribute
    {
        public TypeIdentifierAttribute() { }
        public TypeIdentifierAttribute(string? scope, string? identifier) { }
        public string? Identifier { get { throw null; } }
        public string? Scope { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class UnknownWrapper
    {
        public UnknownWrapper(object? obj) { }
        public object? WrappedObject { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class UnmanagedCallConvAttribute : System.Attribute
    {
        public UnmanagedCallConvAttribute() { }
        public System.Type[]? CallConvs;
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : System.Attribute
    {
        public UnmanagedCallersOnlyAttribute() { }
        public System.Type[]? CallConvs;
        public string? EntryPoint;
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Delegate, AllowMultiple=false, Inherited=false)]
    public sealed partial class UnmanagedFunctionPointerAttribute : System.Attribute
    {
        public bool BestFitMapping;
        public System.Runtime.InteropServices.CharSet CharSet;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
        public UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention callingConvention) { }
        public System.Runtime.InteropServices.CallingConvention CallingConvention { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum VarEnum
    {
        VT_EMPTY = 0,
        VT_NULL = 1,
        VT_I2 = 2,
        VT_I4 = 3,
        VT_R4 = 4,
        VT_R8 = 5,
        VT_CY = 6,
        VT_DATE = 7,
        VT_BSTR = 8,
        VT_DISPATCH = 9,
        VT_ERROR = 10,
        VT_BOOL = 11,
        VT_VARIANT = 12,
        VT_UNKNOWN = 13,
        VT_DECIMAL = 14,
        VT_I1 = 16,
        VT_UI1 = 17,
        VT_UI2 = 18,
        VT_UI4 = 19,
        VT_I8 = 20,
        VT_UI8 = 21,
        VT_INT = 22,
        VT_UINT = 23,
        VT_VOID = 24,
        VT_HRESULT = 25,
        VT_PTR = 26,
        VT_SAFEARRAY = 27,
        VT_CARRAY = 28,
        VT_USERDEFINED = 29,
        VT_LPSTR = 30,
        VT_LPWSTR = 31,
        VT_RECORD = 36,
        VT_FILETIME = 64,
        VT_BLOB = 65,
        VT_STREAM = 66,
        VT_STORAGE = 67,
        VT_STREAMED_OBJECT = 68,
        VT_STORED_OBJECT = 69,
        VT_BLOB_OBJECT = 70,
        VT_CF = 71,
        VT_CLSID = 72,
        VT_VECTOR = 4096,
        VT_ARRAY = 8192,
        VT_BYREF = 16384,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public sealed partial class VariantWrapper
    {
        public VariantWrapper(object? obj) { }
        public object? WrappedObject { get { throw null; } }
    }
}
namespace System.Runtime.InteropServices.ComTypes
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public partial struct BINDPTR
    {
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public System.IntPtr lpfuncdesc;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public System.IntPtr lptcomp;
        [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
        public System.IntPtr lpvardesc;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct BIND_OPTS
    {
        public int cbStruct;
        public int dwTickCountDeadline;
        public int grfFlags;
        public int grfMode;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum CALLCONV
    {
        CC_CDECL = 1,
        CC_MSCPASCAL = 2,
        CC_PASCAL = 2,
        CC_MACPASCAL = 3,
        CC_STDCALL = 4,
        CC_RESERVED = 5,
        CC_SYSCALL = 6,
        CC_MPWCDECL = 7,
        CC_MPWPASCAL = 8,
        CC_MAX = 9,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CONNECTDATA
    {
        public int dwCookie;
        public object pUnk;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum DESCKIND
    {
        DESCKIND_NONE = 0,
        DESCKIND_FUNCDESC = 1,
        DESCKIND_VARDESC = 2,
        DESCKIND_TYPECOMP = 3,
        DESCKIND_IMPLICITAPPOBJ = 4,
        DESCKIND_MAX = 5,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct DISPPARAMS
    {
        public int cArgs;
        public int cNamedArgs;
        public System.IntPtr rgdispidNamedArgs;
        public System.IntPtr rgvarg;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ELEMDESC
    {
        public System.Runtime.InteropServices.ComTypes.ELEMDESC.DESCUNION desc;
        public System.Runtime.InteropServices.ComTypes.TYPEDESC tdesc;
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
        public partial struct DESCUNION
        {
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public System.Runtime.InteropServices.ComTypes.IDLDESC idldesc;
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public System.Runtime.InteropServices.ComTypes.PARAMDESC paramdesc;
        }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct EXCEPINFO
    {
        public string bstrDescription;
        public string bstrHelpFile;
        public string bstrSource;
        public int dwHelpContext;
        public System.IntPtr pfnDeferredFillIn;
        public System.IntPtr pvReserved;
        public int scode;
        public short wCode;
        public short wReserved;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FILETIME
    {
        public int dwHighDateTime;
        public int dwLowDateTime;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct FUNCDESC
    {
        public System.Runtime.InteropServices.ComTypes.CALLCONV callconv;
        public short cParams;
        public short cParamsOpt;
        public short cScodes;
        public System.Runtime.InteropServices.ComTypes.ELEMDESC elemdescFunc;
        public System.Runtime.InteropServices.ComTypes.FUNCKIND funckind;
        public System.Runtime.InteropServices.ComTypes.INVOKEKIND invkind;
        public System.IntPtr lprgelemdescParam;
        public System.IntPtr lprgscode;
        public int memid;
        public short oVft;
        public short wFuncFlags;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum FUNCFLAGS : short
    {
        FUNCFLAG_FRESTRICTED = (short)1,
        FUNCFLAG_FSOURCE = (short)2,
        FUNCFLAG_FBINDABLE = (short)4,
        FUNCFLAG_FREQUESTEDIT = (short)8,
        FUNCFLAG_FDISPLAYBIND = (short)16,
        FUNCFLAG_FDEFAULTBIND = (short)32,
        FUNCFLAG_FHIDDEN = (short)64,
        FUNCFLAG_FUSESGETLASTERROR = (short)128,
        FUNCFLAG_FDEFAULTCOLLELEM = (short)256,
        FUNCFLAG_FUIDEFAULT = (short)512,
        FUNCFLAG_FNONBROWSABLE = (short)1024,
        FUNCFLAG_FREPLACEABLE = (short)2048,
        FUNCFLAG_FIMMEDIATEBIND = (short)4096,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum FUNCKIND
    {
        FUNC_VIRTUAL = 0,
        FUNC_PUREVIRTUAL = 1,
        FUNC_NONVIRTUAL = 2,
        FUNC_STATIC = 3,
        FUNC_DISPATCH = 4,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IBindCtx
    {
        void EnumObjectParam(out System.Runtime.InteropServices.ComTypes.IEnumString? ppenum);
        void GetBindOptions(ref System.Runtime.InteropServices.ComTypes.BIND_OPTS pbindopts);
        void GetObjectParam(string pszKey, out object? ppunk);
        void GetRunningObjectTable(out System.Runtime.InteropServices.ComTypes.IRunningObjectTable? pprot);
        void RegisterObjectBound(object punk);
        void RegisterObjectParam(string pszKey, object punk);
        void ReleaseBoundObjects();
        void RevokeObjectBound(object punk);
        int RevokeObjectParam(string pszKey);
        void SetBindOptions(ref System.Runtime.InteropServices.ComTypes.BIND_OPTS pbindopts);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IConnectionPoint
    {
        void Advise(object pUnkSink, out int pdwCookie);
        void EnumConnections(out System.Runtime.InteropServices.ComTypes.IEnumConnections ppEnum);
        void GetConnectionInterface(out System.Guid pIID);
        void GetConnectionPointContainer(out System.Runtime.InteropServices.ComTypes.IConnectionPointContainer ppCPC);
        void Unadvise(int dwCookie);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IConnectionPointContainer
    {
        void EnumConnectionPoints(out System.Runtime.InteropServices.ComTypes.IEnumConnectionPoints ppEnum);
        void FindConnectionPoint(ref System.Guid riid, out System.Runtime.InteropServices.ComTypes.IConnectionPoint? ppCP);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct IDLDESC
    {
        public System.IntPtr dwReserved;
        public System.Runtime.InteropServices.ComTypes.IDLFLAG wIDLFlags;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum IDLFLAG : short
    {
        IDLFLAG_NONE = (short)0,
        IDLFLAG_FIN = (short)1,
        IDLFLAG_FOUT = (short)2,
        IDLFLAG_FLCID = (short)4,
        IDLFLAG_FRETVAL = (short)8,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IEnumConnectionPoints
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumConnectionPoints ppenum);
        int Next(int celt, System.Runtime.InteropServices.ComTypes.IConnectionPoint[] rgelt, System.IntPtr pceltFetched);
        void Reset();
        int Skip(int celt);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IEnumConnections
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumConnections ppenum);
        int Next(int celt, System.Runtime.InteropServices.ComTypes.CONNECTDATA[] rgelt, System.IntPtr pceltFetched);
        void Reset();
        int Skip(int celt);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IEnumMoniker
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumMoniker ppenum);
        int Next(int celt, System.Runtime.InteropServices.ComTypes.IMoniker[] rgelt, System.IntPtr pceltFetched);
        void Reset();
        int Skip(int celt);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IEnumString
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IEnumString ppenum);
        int Next(int celt, string[] rgelt, System.IntPtr pceltFetched);
        void Reset();
        int Skip(int celt);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IEnumVARIANT
    {
        System.Runtime.InteropServices.ComTypes.IEnumVARIANT Clone();
        int Next(int celt, object?[] rgVar, System.IntPtr pceltFetched);
        int Reset();
        int Skip(int celt);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IMoniker
    {
        void BindToObject(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker? pmkToLeft, ref System.Guid riidResult, out object ppvResult);
        void BindToStorage(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker? pmkToLeft, ref System.Guid riid, out object ppvObj);
        void CommonPrefixWith(System.Runtime.InteropServices.ComTypes.IMoniker pmkOther, out System.Runtime.InteropServices.ComTypes.IMoniker? ppmkPrefix);
        void ComposeWith(System.Runtime.InteropServices.ComTypes.IMoniker pmkRight, bool fOnlyIfNotGeneric, out System.Runtime.InteropServices.ComTypes.IMoniker? ppmkComposite);
        void Enum(bool fForward, out System.Runtime.InteropServices.ComTypes.IEnumMoniker? ppenumMoniker);
        void GetClassID(out System.Guid pClassID);
        void GetDisplayName(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker? pmkToLeft, out string ppszDisplayName);
        void GetSizeMax(out long pcbSize);
        void GetTimeOfLastChange(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker? pmkToLeft, out System.Runtime.InteropServices.ComTypes.FILETIME pFileTime);
        void Hash(out int pdwHash);
        void Inverse(out System.Runtime.InteropServices.ComTypes.IMoniker ppmk);
        int IsDirty();
        int IsEqual(System.Runtime.InteropServices.ComTypes.IMoniker pmkOtherMoniker);
        int IsRunning(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker? pmkToLeft, System.Runtime.InteropServices.ComTypes.IMoniker? pmkNewlyRunning);
        int IsSystemMoniker(out int pdwMksys);
        void Load(System.Runtime.InteropServices.ComTypes.IStream pStm);
        void ParseDisplayName(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, System.Runtime.InteropServices.ComTypes.IMoniker pmkToLeft, string pszDisplayName, out int pchEaten, out System.Runtime.InteropServices.ComTypes.IMoniker ppmkOut);
        void Reduce(System.Runtime.InteropServices.ComTypes.IBindCtx pbc, int dwReduceHowFar, ref System.Runtime.InteropServices.ComTypes.IMoniker? ppmkToLeft, out System.Runtime.InteropServices.ComTypes.IMoniker? ppmkReduced);
        void RelativePathTo(System.Runtime.InteropServices.ComTypes.IMoniker pmkOther, out System.Runtime.InteropServices.ComTypes.IMoniker? ppmkRelPath);
        void Save(System.Runtime.InteropServices.ComTypes.IStream pStm, bool fClearDirty);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum IMPLTYPEFLAGS
    {
        IMPLTYPEFLAG_FDEFAULT = 1,
        IMPLTYPEFLAG_FSOURCE = 2,
        IMPLTYPEFLAG_FRESTRICTED = 4,
        IMPLTYPEFLAG_FDEFAULTVTABLE = 8,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum INVOKEKIND
    {
        INVOKE_FUNC = 1,
        INVOKE_PROPERTYGET = 2,
        INVOKE_PROPERTYPUT = 4,
        INVOKE_PROPERTYPUTREF = 8,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IPersistFile
    {
        void GetClassID(out System.Guid pClassID);
        void GetCurFile(out string ppszFileName);
        int IsDirty();
        void Load(string pszFileName, int dwMode);
        void Save(string? pszFileName, bool fRemember);
        void SaveCompleted(string pszFileName);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IRunningObjectTable
    {
        void EnumRunning(out System.Runtime.InteropServices.ComTypes.IEnumMoniker ppenumMoniker);
        int GetObject(System.Runtime.InteropServices.ComTypes.IMoniker pmkObjectName, out object ppunkObject);
        int GetTimeOfLastChange(System.Runtime.InteropServices.ComTypes.IMoniker pmkObjectName, out System.Runtime.InteropServices.ComTypes.FILETIME pfiletime);
        int IsRunning(System.Runtime.InteropServices.ComTypes.IMoniker pmkObjectName);
        void NoteChangeTime(int dwRegister, ref System.Runtime.InteropServices.ComTypes.FILETIME pfiletime);
        int Register(int grfFlags, object punkObject, System.Runtime.InteropServices.ComTypes.IMoniker pmkObjectName);
        void Revoke(int dwRegister);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IStream
    {
        void Clone(out System.Runtime.InteropServices.ComTypes.IStream ppstm);
        void Commit(int grfCommitFlags);
        void CopyTo(System.Runtime.InteropServices.ComTypes.IStream pstm, long cb, System.IntPtr pcbRead, System.IntPtr pcbWritten);
        void LockRegion(long libOffset, long cb, int dwLockType);
        void Read(byte[] pv, int cb, System.IntPtr pcbRead);
        void Revert();
        void Seek(long dlibMove, int dwOrigin, System.IntPtr plibNewPosition);
        void SetSize(long libNewSize);
        void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag);
        void UnlockRegion(long libOffset, long cb, int dwLockType);
        void Write(byte[] pv, int cb, System.IntPtr pcbWritten);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITypeComp
    {
        void Bind(string szName, int lHashVal, short wFlags, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTInfo, out System.Runtime.InteropServices.ComTypes.DESCKIND pDescKind, out System.Runtime.InteropServices.ComTypes.BINDPTR pBindPtr);
        void BindType(string szName, int lHashVal, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTInfo, out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITypeInfo
    {
        void AddressOfMember(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, out System.IntPtr ppv);
        void CreateInstance(object? pUnkOuter, ref System.Guid riid, out object ppvObj);
        void GetContainingTypeLib(out System.Runtime.InteropServices.ComTypes.ITypeLib ppTLB, out int pIndex);
        void GetDllEntry(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, System.IntPtr pBstrDllName, System.IntPtr pBstrName, System.IntPtr pwOrdinal);
        void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetFuncDesc(int index, out System.IntPtr ppFuncDesc);
        void GetIDsOfNames(string[] rgszNames, int cNames, int[] pMemId);
        void GetImplTypeFlags(int index, out System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS pImplTypeFlags);
        void GetMops(int memid, out string? pBstrMops);
        void GetNames(int memid, string[] rgBstrNames, int cMaxNames, out int pcNames);
        void GetRefTypeInfo(int hRef, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTI);
        void GetRefTypeOfImplType(int index, out int href);
        void GetTypeAttr(out System.IntPtr ppTypeAttr);
        void GetTypeComp(out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
        void GetVarDesc(int index, out System.IntPtr ppVarDesc);
        void Invoke(object pvInstance, int memid, short wFlags, ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams, System.IntPtr pVarResult, System.IntPtr pExcepInfo, out int puArgErr);
        void ReleaseFuncDesc(System.IntPtr pFuncDesc);
        void ReleaseTypeAttr(System.IntPtr pTypeAttr);
        void ReleaseVarDesc(System.IntPtr pVarDesc);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITypeInfo2 : System.Runtime.InteropServices.ComTypes.ITypeInfo
    {
        new void AddressOfMember(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, out System.IntPtr ppv);
        new void CreateInstance(object? pUnkOuter, ref System.Guid riid, out object ppvObj);
        void GetAllCustData(System.IntPtr pCustData);
        void GetAllFuncCustData(int index, System.IntPtr pCustData);
        void GetAllImplTypeCustData(int index, System.IntPtr pCustData);
        void GetAllParamCustData(int indexFunc, int indexParam, System.IntPtr pCustData);
        void GetAllVarCustData(int index, System.IntPtr pCustData);
        new void GetContainingTypeLib(out System.Runtime.InteropServices.ComTypes.ITypeLib ppTLB, out int pIndex);
        void GetCustData(ref System.Guid guid, out object pVarVal);
        new void GetDllEntry(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, System.IntPtr pBstrDllName, System.IntPtr pBstrName, System.IntPtr pwOrdinal);
        new void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetDocumentation2(int memid, out string pbstrHelpString, out int pdwHelpStringContext, out string pbstrHelpStringDll);
        void GetFuncCustData(int index, ref System.Guid guid, out object pVarVal);
        new void GetFuncDesc(int index, out System.IntPtr ppFuncDesc);
        void GetFuncIndexOfMemId(int memid, System.Runtime.InteropServices.ComTypes.INVOKEKIND invKind, out int pFuncIndex);
        new void GetIDsOfNames(string[] rgszNames, int cNames, int[] pMemId);
        void GetImplTypeCustData(int index, ref System.Guid guid, out object pVarVal);
        new void GetImplTypeFlags(int index, out System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS pImplTypeFlags);
        new void GetMops(int memid, out string? pBstrMops);
        new void GetNames(int memid, string[] rgBstrNames, int cMaxNames, out int pcNames);
        void GetParamCustData(int indexFunc, int indexParam, ref System.Guid guid, out object pVarVal);
        new void GetRefTypeInfo(int hRef, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTI);
        new void GetRefTypeOfImplType(int index, out int href);
        new void GetTypeAttr(out System.IntPtr ppTypeAttr);
        new void GetTypeComp(out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
        void GetTypeFlags(out int pTypeFlags);
        void GetTypeKind(out System.Runtime.InteropServices.ComTypes.TYPEKIND pTypeKind);
        void GetVarCustData(int index, ref System.Guid guid, out object pVarVal);
        new void GetVarDesc(int index, out System.IntPtr ppVarDesc);
        void GetVarIndexOfMemId(int memid, out int pVarIndex);
        new void Invoke(object pvInstance, int memid, short wFlags, ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams, System.IntPtr pVarResult, System.IntPtr pExcepInfo, out int puArgErr);
        new void ReleaseFuncDesc(System.IntPtr pFuncDesc);
        new void ReleaseTypeAttr(System.IntPtr pTypeAttr);
        new void ReleaseVarDesc(System.IntPtr pVarDesc);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITypeLib
    {
        void FindName(string szNameBuf, int lHashVal, System.Runtime.InteropServices.ComTypes.ITypeInfo[] ppTInfo, int[] rgMemId, ref short pcFound);
        void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetLibAttr(out System.IntPtr ppTLibAttr);
        void GetTypeComp(out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
        void GetTypeInfo(int index, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTI);
        int GetTypeInfoCount();
        void GetTypeInfoOfGuid(ref System.Guid guid, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTInfo);
        void GetTypeInfoType(int index, out System.Runtime.InteropServices.ComTypes.TYPEKIND pTKind);
        bool IsName(string szNameBuf, int lHashVal);
        void ReleaseTLibAttr(System.IntPtr pTLibAttr);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ITypeLib2 : System.Runtime.InteropServices.ComTypes.ITypeLib
    {
        new void FindName(string szNameBuf, int lHashVal, System.Runtime.InteropServices.ComTypes.ITypeInfo[] ppTInfo, int[] rgMemId, ref short pcFound);
        void GetAllCustData(System.IntPtr pCustData);
        void GetCustData(ref System.Guid guid, out object pVarVal);
        new void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        void GetDocumentation2(int index, out string pbstrHelpString, out int pdwHelpStringContext, out string pbstrHelpStringDll);
        new void GetLibAttr(out System.IntPtr ppTLibAttr);
        void GetLibStatistics(System.IntPtr pcUniqueNames, out int pcchUniqueNames);
        new void GetTypeComp(out System.Runtime.InteropServices.ComTypes.ITypeComp ppTComp);
        new void GetTypeInfo(int index, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTI);
        new int GetTypeInfoCount();
        new void GetTypeInfoOfGuid(ref System.Guid guid, out System.Runtime.InteropServices.ComTypes.ITypeInfo ppTInfo);
        new void GetTypeInfoType(int index, out System.Runtime.InteropServices.ComTypes.TYPEKIND pTKind);
        new bool IsName(string szNameBuf, int lHashVal);
        new void ReleaseTLibAttr(System.IntPtr pTLibAttr);
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum LIBFLAGS : short
    {
        LIBFLAG_FRESTRICTED = (short)1,
        LIBFLAG_FCONTROL = (short)2,
        LIBFLAG_FHIDDEN = (short)4,
        LIBFLAG_FHASDISKIMAGE = (short)8,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct PARAMDESC
    {
        public System.IntPtr lpVarValue;
        public System.Runtime.InteropServices.ComTypes.PARAMFLAG wParamFlags;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum PARAMFLAG : short
    {
        PARAMFLAG_NONE = (short)0,
        PARAMFLAG_FIN = (short)1,
        PARAMFLAG_FOUT = (short)2,
        PARAMFLAG_FLCID = (short)4,
        PARAMFLAG_FRETVAL = (short)8,
        PARAMFLAG_FOPT = (short)16,
        PARAMFLAG_FHASDEFAULT = (short)32,
        PARAMFLAG_FHASCUSTDATA = (short)64,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct STATSTG
    {
        public System.Runtime.InteropServices.ComTypes.FILETIME atime;
        public long cbSize;
        public System.Guid clsid;
        public System.Runtime.InteropServices.ComTypes.FILETIME ctime;
        public int grfLocksSupported;
        public int grfMode;
        public int grfStateBits;
        public System.Runtime.InteropServices.ComTypes.FILETIME mtime;
        public string pwcsName;
        public int reserved;
        public int type;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum SYSKIND
    {
        SYS_WIN16 = 0,
        SYS_WIN32 = 1,
        SYS_MAC = 2,
        SYS_WIN64 = 3,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TYPEATTR
    {
        public short cbAlignment;
        public int cbSizeInstance;
        public short cbSizeVft;
        public short cFuncs;
        public short cImplTypes;
        public short cVars;
        public int dwReserved;
        public System.Guid guid;
        public System.Runtime.InteropServices.ComTypes.IDLDESC idldescType;
        public int lcid;
        public System.IntPtr lpstrSchema;
        public const int MEMBER_ID_NIL = -1;
        public int memidConstructor;
        public int memidDestructor;
        public System.Runtime.InteropServices.ComTypes.TYPEDESC tdescAlias;
        public System.Runtime.InteropServices.ComTypes.TYPEKIND typekind;
        public short wMajorVerNum;
        public short wMinorVerNum;
        public System.Runtime.InteropServices.ComTypes.TYPEFLAGS wTypeFlags;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TYPEDESC
    {
        public System.IntPtr lpValue;
        public short vt;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum TYPEFLAGS : short
    {
        TYPEFLAG_FAPPOBJECT = (short)1,
        TYPEFLAG_FCANCREATE = (short)2,
        TYPEFLAG_FLICENSED = (short)4,
        TYPEFLAG_FPREDECLID = (short)8,
        TYPEFLAG_FHIDDEN = (short)16,
        TYPEFLAG_FCONTROL = (short)32,
        TYPEFLAG_FDUAL = (short)64,
        TYPEFLAG_FNONEXTENSIBLE = (short)128,
        TYPEFLAG_FOLEAUTOMATION = (short)256,
        TYPEFLAG_FRESTRICTED = (short)512,
        TYPEFLAG_FAGGREGATABLE = (short)1024,
        TYPEFLAG_FREPLACEABLE = (short)2048,
        TYPEFLAG_FDISPATCHABLE = (short)4096,
        TYPEFLAG_FREVERSEBIND = (short)8192,
        TYPEFLAG_FPROXY = (short)16384,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum TYPEKIND
    {
        TKIND_ENUM = 0,
        TKIND_RECORD = 1,
        TKIND_MODULE = 2,
        TKIND_INTERFACE = 3,
        TKIND_DISPATCH = 4,
        TKIND_COCLASS = 5,
        TKIND_ALIAS = 6,
        TKIND_UNION = 7,
        TKIND_MAX = 8,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct TYPELIBATTR
    {
        public System.Guid guid;
        public int lcid;
        public System.Runtime.InteropServices.ComTypes.SYSKIND syskind;
        public System.Runtime.InteropServices.ComTypes.LIBFLAGS wLibFlags;
        public short wMajorVerNum;
        public short wMinorVerNum;
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct VARDESC
    {
        public System.Runtime.InteropServices.ComTypes.VARDESC.DESCUNION desc;
        public System.Runtime.InteropServices.ComTypes.ELEMDESC elemdescVar;
        public string lpstrSchema;
        public int memid;
        public System.Runtime.InteropServices.ComTypes.VARKIND varkind;
        public short wVarFlags;
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Explicit)]
        public partial struct DESCUNION
        {
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public System.IntPtr lpvarValue;
            [System.Runtime.InteropServices.FieldOffsetAttribute(0)]
            public int oInst;
        }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.FlagsAttribute]
    public enum VARFLAGS : short
    {
        VARFLAG_FREADONLY = (short)1,
        VARFLAG_FSOURCE = (short)2,
        VARFLAG_FBINDABLE = (short)4,
        VARFLAG_FREQUESTEDIT = (short)8,
        VARFLAG_FDISPLAYBIND = (short)16,
        VARFLAG_FDEFAULTBIND = (short)32,
        VARFLAG_FHIDDEN = (short)64,
        VARFLAG_FRESTRICTED = (short)128,
        VARFLAG_FDEFAULTCOLLELEM = (short)256,
        VARFLAG_FUIDEFAULT = (short)512,
        VARFLAG_FNONBROWSABLE = (short)1024,
        VARFLAG_FREPLACEABLE = (short)2048,
        VARFLAG_FIMMEDIATEBIND = (short)4096,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public enum VARKIND
    {
        VAR_PERINSTANCE = 0,
        VAR_STATIC = 1,
        VAR_CONST = 2,
        VAR_DISPATCH = 3,
    }
}
namespace System.Runtime.InteropServices.ObjectiveC
{
    [System.Runtime.Versioning.SupportedOSPlatformAttribute("macos")]
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class ObjectiveCTrackedTypeAttribute : System.Attribute
    {
        public ObjectiveCTrackedTypeAttribute() { }
    }

    [System.Runtime.Versioning.SupportedOSPlatformAttribute("macos")]
    [System.CLSCompliantAttribute(false)]
    public static class ObjectiveCMarshal
    {
        public unsafe delegate delegate* unmanaged<System.IntPtr, void> UnhandledExceptionPropagationHandler(
            System.Exception exception,
            System.RuntimeMethodHandle lastMethod,
            out System.IntPtr context);
        public static unsafe void Initialize(
            delegate* unmanaged<void> beginEndCallback,
            delegate* unmanaged<System.IntPtr, int> isReferencedCallback,
            delegate* unmanaged<System.IntPtr, void> trackedObjectEnteredFinalization,
            UnhandledExceptionPropagationHandler unhandledExceptionPropagationHandler) => throw null;
        public static GCHandle CreateReferenceTrackingHandle(
            object obj,
            out System.Span<System.IntPtr> taggedMemory) => throw null;
        public enum MessageSendFunction
        {
            MsgSend,
            MsgSendFpret,
            MsgSendStret,
            MsgSendSuper,
            MsgSendSuperStret,
        }
        public static void SetMessageSendCallback(MessageSendFunction msgSendFunction, System.IntPtr func) => throw null;
        public static void SetMessageSendPendingException(Exception? exception) => throw null;
    }
}
namespace System.Runtime.InteropServices.Marshalling
{
    [System.CLSCompliant(false)]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(string),
        System.Runtime.InteropServices.Marshalling.MarshalMode.Default,
        typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(string),
        System.Runtime.InteropServices.Marshalling.MarshalMode.ManagedToUnmanagedIn,
        typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller.ManagedToUnmanagedIn))]
    public static unsafe class AnsiStringMarshaller
    {
        public static byte* ConvertToUnmanaged(string? managed) { throw null; }
        public static string? ConvertToManaged(byte* unmanaged) { throw null; }
        public static void Free(byte* unmanaged) { throw null; }

        public ref struct ManagedToUnmanagedIn
        {
            public static int BufferSize { get { throw null; } }
            public void FromManaged(string? managed, System.Span<byte> buffer) { throw null; }
            public byte* ToUnmanaged()  { throw null; }
            public void Free() { throw null; }
        }
    }

    [System.CLSCompliant(false)]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(CustomMarshallerAttribute.GenericPlaceholder[]),
        System.Runtime.InteropServices.Marshalling.MarshalMode.Default,
        typeof(System.Runtime.InteropServices.Marshalling.ArrayMarshaller<,>))]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(CustomMarshallerAttribute.GenericPlaceholder[]),
        System.Runtime.InteropServices.Marshalling.MarshalMode.ManagedToUnmanagedIn,
        typeof(System.Runtime.InteropServices.Marshalling.ArrayMarshaller<,>.ManagedToUnmanagedIn))]
    [System.Runtime.InteropServices.Marshalling.ContiguousCollectionMarshaller]
    public static unsafe class ArrayMarshaller<T, TUnmanagedElement>
        where TUnmanagedElement : unmanaged
    {
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(T[]? managed, out int numElements) { throw null; }
        public static System.ReadOnlySpan<T> GetManagedValuesSource(T[]? managed) { throw null; }
        public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements) { throw null; }
        public static T[]? AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int numElements) { throw null; }
        public static System.Span<T> GetManagedValuesDestination(T[]? managed) { throw null; }
        public static System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanagedValue, int numElements) { throw null; }
        public static void Free(TUnmanagedElement* unmanaged) { }

        public unsafe ref struct ManagedToUnmanagedIn
        {
            private object _dummy;
            private int _dummyPrimitive;
            public static int BufferSize { get { throw null; } }
            public void FromManaged(T[]? array, System.Span<TUnmanagedElement> buffer) { }
            public System.ReadOnlySpan<T> GetManagedValuesSource() { throw null; }
            public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() { throw null; }
            public ref TUnmanagedElement GetPinnableReference() { throw null; }
            public static ref T GetPinnableReference(T[]? array) { throw null; }
            public TUnmanagedElement* ToUnmanaged() { throw null; }
            public void Free() { }
        }
    }
    [System.CLSCompliant(false)]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(string),
        System.Runtime.InteropServices.Marshalling.MarshalMode.Default,
        typeof(System.Runtime.InteropServices.Marshalling.BStrStringMarshaller))]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(string),
        System.Runtime.InteropServices.Marshalling.MarshalMode.ManagedToUnmanagedIn,
        typeof(System.Runtime.InteropServices.Marshalling.BStrStringMarshaller.ManagedToUnmanagedIn))]
    public static unsafe class BStrStringMarshaller
    {
        public static ushort* ConvertToUnmanaged(string? managed) { throw null; }
        public static string? ConvertToManaged(ushort* unmanaged) { throw null; }
        public static void Free(ushort* unmanaged) { throw null; }

        public ref struct ManagedToUnmanagedIn
        {
            public static int BufferSize { get { throw null; } }
            public void FromManaged(string? managed, System.Span<byte> buffer) { throw null; }
            public ushort* ToUnmanaged()  { throw null; }
            public void Free() { throw null; }
        }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter | System.AttributeTargets.ReturnValue, AllowMultiple = true)]
    public sealed partial class MarshalUsingAttribute : System.Attribute
    {
        public MarshalUsingAttribute() { }
        public MarshalUsingAttribute(System.Type nativeType) { }
        public System.Type? NativeType { get { throw null; } }
        public string CountElementName { get { throw null; } set { } }
        public int ConstantElementCount { get { throw null; } set { } }
        public int ElementIndirectionDepth { get { throw null; } set { } }
        public const string ReturnsCountValue = "return-value";
    }
    [System.CLSCompliant(false)]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(CustomMarshallerAttribute.GenericPlaceholder*[]),
        System.Runtime.InteropServices.Marshalling.MarshalMode.Default,
        typeof(System.Runtime.InteropServices.Marshalling.PointerArrayMarshaller<,>))]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(CustomMarshallerAttribute.GenericPlaceholder*[]),
        System.Runtime.InteropServices.Marshalling.MarshalMode.ManagedToUnmanagedIn,
        typeof(System.Runtime.InteropServices.Marshalling.PointerArrayMarshaller<,>.ManagedToUnmanagedIn))]
    [System.Runtime.InteropServices.Marshalling.ContiguousCollectionMarshaller]
    public static unsafe class PointerArrayMarshaller<T, TUnmanagedElement>
        where T : unmanaged
        where TUnmanagedElement : unmanaged
    {
        public static TUnmanagedElement* AllocateContainerForUnmanagedElements(T*[]? managed, out int numElements) { throw null; }
        public static System.ReadOnlySpan<IntPtr> GetManagedValuesSource(T*[]? managed) { throw null; }
        public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements) { throw null; }
        public static T*[]? AllocateContainerForManagedElements(TUnmanagedElement* unmanaged, int numElements) { throw null; }
        public static System.Span<IntPtr> GetManagedValuesDestination(T*[]? managed) { throw null; }
        public static System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TUnmanagedElement* unmanagedValue, int numElements) { throw null; }
        public static void Free(TUnmanagedElement* unmanaged) { }

        public unsafe ref struct ManagedToUnmanagedIn
        {
            private object _dummy;
            private int _dummyPrimitive;
            public static int BufferSize { get { throw null; } }
            public void FromManaged(T*[]? array, System.Span<TUnmanagedElement> buffer) { }
            public System.ReadOnlySpan<IntPtr> GetManagedValuesSource() { throw null; }
            public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() { throw null; }
            public ref TUnmanagedElement GetPinnableReference() { throw null; }
            public static ref byte GetPinnableReference(T*[]? array) { throw null; }
            public TUnmanagedElement* ToUnmanaged() { throw null; }
            public void Free() { }
        }
    }
    [System.CLSCompliant(false)]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(string),
        System.Runtime.InteropServices.Marshalling.MarshalMode.Default,
        typeof(System.Runtime.InteropServices.Marshalling.Utf8StringMarshaller))]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(string),
        System.Runtime.InteropServices.Marshalling.MarshalMode.ManagedToUnmanagedIn,
        typeof(System.Runtime.InteropServices.Marshalling.Utf8StringMarshaller.ManagedToUnmanagedIn))]
    public static unsafe class Utf8StringMarshaller
    {
        public static byte* ConvertToUnmanaged(string? managed) { throw null; }
        public static string? ConvertToManaged(byte* unmanaged) { throw null; }
        public static void Free(byte* unmanaged) { throw null; }

        public ref struct ManagedToUnmanagedIn
        {
            public static int BufferSize { get { throw null; } }
            public void FromManaged(string? managed, System.Span<byte> buffer) { throw null; }
            public byte* ToUnmanaged()  { throw null; }
            public void Free() { throw null; }
        }
    }
    [System.CLSCompliant(false)]
    [System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute(typeof(string),
        System.Runtime.InteropServices.Marshalling.MarshalMode.Default,
        typeof(System.Runtime.InteropServices.Marshalling.Utf16StringMarshaller))]
    public static unsafe class Utf16StringMarshaller
    {
        public static ushort* ConvertToUnmanaged(string? managed) { throw null; }
        public static string? ConvertToManaged(ushort* unmanaged) { throw null; }
        public static void Free(ushort* unmanaged) { throw null; }
        public static ref readonly char GetPinnableReference(string? str) { throw null; }
    }
}
namespace System.Security
{
    public sealed partial class SecureString : System.IDisposable
    {
        public SecureString() { }
        [System.CLSCompliantAttribute(false)]
        public unsafe SecureString(char* value, int length) { }
        public int Length { get { throw null; } }
        public void AppendChar(char c) { }
        public void Clear() { }
        public System.Security.SecureString Copy() { throw null; }
        public void Dispose() { }
        public void InsertAt(int index, char c) { }
        public bool IsReadOnly() { throw null; }
        public void MakeReadOnly() { }
        public void RemoveAt(int index) { }
        public void SetAt(int index, char c) { }
    }
}
