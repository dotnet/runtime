// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    public sealed partial class DataMisalignedException : System.Exception
    {
        public DataMisalignedException() { }
        public DataMisalignedException(string message) { }
        public DataMisalignedException(string message, System.Exception innerException) { }
    }
    public partial class DllNotFoundException : System.TypeLoadException
    {
        public DllNotFoundException() { }
        public DllNotFoundException(string message) { }
        public DllNotFoundException(string message, System.Exception inner) { }
    }
}
namespace System.Runtime.InteropServices
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ArrayWithOffset
    {
        public ArrayWithOffset(object array, int offset) { throw new System.NotImplementedException(); }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Runtime.InteropServices.ArrayWithOffset obj) { return default(bool); }
        public object GetArray() { return default(object); }
        public override int GetHashCode() { return default(int); }
        public int GetOffset() { return default(int); }
        public static bool operator ==(System.Runtime.InteropServices.ArrayWithOffset a, System.Runtime.InteropServices.ArrayWithOffset b) { return default(bool); }
        public static bool operator !=(System.Runtime.InteropServices.ArrayWithOffset a, System.Runtime.InteropServices.ArrayWithOffset b) { return default(bool); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1037), Inherited = false)]
    public sealed partial class BestFitMappingAttribute : System.Attribute
    {
        public bool ThrowOnUnmappableChar;
        public BestFitMappingAttribute(bool BestFitMapping) { }
        public bool BestFitMapping { get { return default(bool); } }
    }
    public enum CallingConvention
    {
        Cdecl = 2,
        StdCall = 3,
        ThisCall = 4,
        Winapi = 1,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2), Inherited = false)]
    public sealed partial class DefaultCharSetAttribute : System.Attribute
    {
        public DefaultCharSetAttribute(System.Runtime.InteropServices.CharSet charSet) { }
        public System.Runtime.InteropServices.CharSet CharSet { get { return default(System.Runtime.InteropServices.CharSet); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048))]
    public sealed partial class DefaultParameterValueAttribute : System.Attribute
    {
        public DefaultParameterValueAttribute(object value) { }
        public object Value { get { return default(object); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited = false)]
    public sealed partial class DllImportAttribute : System.Attribute
    {
        public bool BestFitMapping;
        public System.Runtime.InteropServices.CallingConvention CallingConvention;
        public System.Runtime.InteropServices.CharSet CharSet;
        public string EntryPoint;
        public bool ExactSpelling;
        public bool PreserveSig;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
        public DllImportAttribute(string dllName) { }
        public string Value { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(5149), Inherited = false)]
    public sealed partial class GuidAttribute : System.Attribute
    {
        public GuidAttribute(string guid) { }
        public string Value { get { return default(string); } }
    }
    public sealed partial class HandleCollector
    {
        public HandleCollector(string name, int initialThreshold) { }
        public HandleCollector(string name, int initialThreshold, int maximumThreshold) { }
        public int Count { get { return default(int); } }
        public int InitialThreshold { get { return default(int); } }
        public int MaximumThreshold { get { return default(int); } }
        public string Name { get { return default(string); } }
        public void Add() { }
        public void Remove() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited = false)]
    public sealed partial class InAttribute : System.Attribute
    {
        public InAttribute() { }
    }
    public static partial class PInvokeMarshal
    {
        public static readonly int SystemDefaultCharSize;
        public static readonly int SystemMaxDBCSCharSize;
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr AllocateMemory(int sizeInBytes) { return default(System.IntPtr); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("DestroyStructure(IntPtr, Type) may be unavailable in future releases. Instead, use DestroyStructure<T>(IntPtr). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296520")]
        [System.Security.SecurityCriticalAttribute]
        public static void DestroyStructure(System.IntPtr ptr, System.Type structureType) { }
        [System.Security.SecurityCriticalAttribute]
        public static void DestroyStructure<T>(System.IntPtr ptr) { }
        [System.Security.SecurityCriticalAttribute]
        public static void FreeMemory(System.IntPtr ptr) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("GetDelegateForFunctionPointer(IntPtr, Type) may be unavailable in future releases. Instead, use GetDelegateForFunctionPointer<T>(IntPtr). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296521")]
        [System.Security.SecurityCriticalAttribute]
        public static System.Delegate GetDelegateForFunctionPointer(System.IntPtr ptr, System.Type delegateType) { return default(System.Delegate); }
        [System.Security.SecurityCriticalAttribute]
        public static TDelegate GetDelegateForFunctionPointer<TDelegate>(System.IntPtr ptr) { return default(TDelegate); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("GetFunctionPointerForDelegate(Delegate) may be unavailable in future releases. Instead, use GetFunctionPointerForDelegate<T>(T). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296522")]
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetFunctionPointerForDelegate(System.Delegate d) { return default(System.IntPtr); }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d) { return default(System.IntPtr); }
        [System.Security.SecurityCriticalAttribute]
        public static int GetLastError() { return default(int); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("OffsetOf(Type, string) may be unavailable in future releases. Instead, use OffsetOf<T>(string). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296511")]
        public static System.IntPtr OffsetOf(System.Type type, string fieldName) { return default(System.IntPtr); }
        public static System.IntPtr OffsetOf<T>(string fieldName) { return default(System.IntPtr); }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringAnsi(System.IntPtr ptr) { return default(string); }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringAnsi(System.IntPtr ptr, int len) { return default(string); }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringUTF16(System.IntPtr ptr) { return default(string); }
        [System.Security.SecurityCriticalAttribute]
        public static string PtrToStringUTF16(System.IntPtr ptr, int len) { return default(string); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("PtrToStructure(IntPtr, Object) may be unavailable in future releases. Instead, use PtrToStructure<T>(IntPtr). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296512")]
        [System.Security.SecurityCriticalAttribute]
        public static void PtrToStructure(System.IntPtr ptr, object structure) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("PtrToStructure(IntPtr, Type) may be unavailable in future releases. Instead, use PtrToStructure<T>(IntPtr). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296513")]
        [System.Security.SecurityCriticalAttribute]
        public static object PtrToStructure(System.IntPtr ptr, System.Type structureType) { return default(object); }
        [System.Security.SecurityCriticalAttribute]
        public static T PtrToStructure<T>(System.IntPtr ptr) { return default(T); }
        [System.Security.SecurityCriticalAttribute]
        public static void PtrToStructure<T>(System.IntPtr ptr, T structure) { }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr ReallocateMemory(System.IntPtr ptr, int sizeInBytes) { return default(System.IntPtr); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("SizeOf(Object) may be unavailable in future releases. Instead, use SizeOf<T>(). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296514")]
        public static int SizeOf(object structure) { return default(int); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("SizeOf(Type) may be unavailable in future releases. Instead, use SizeOf<T>(). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296515")]
        public static int SizeOf(System.Type type) { return default(int); }
        public static int SizeOf<T>() { return default(int); }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToAllocatedMemoryAnsi(string s) { return default(System.IntPtr); }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr StringToAllocatedMemoryUTF16(string s) { return default(System.IntPtr); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("StructureToPtr(Object, IntPtr, Boolean) may be unavailable in future releases. Instead, use StructureToPtr<T>(T, IntPtr, Boolean). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296516")]
        [System.Security.SecurityCriticalAttribute]
        public static void StructureToPtr(object structure, System.IntPtr ptr, bool fDeleteOld) { }
        [System.Security.SecurityCriticalAttribute]
        public static void StructureToPtr<T>(T structure, System.IntPtr ptr, bool fDeleteOld) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("UnsafeAddrOfPinnedArrayElement(Array, Int32) may be unavailable in future releases. Instead, use UnsafeAddrOfPinnedArrayElement<T>(T[], Int32). For more info, go to http://go.microsoft.com/fwlink/?LinkID=296517")]
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr UnsafeAddrOfPinnedArrayElement(System.Array arr, int index) { return default(System.IntPtr); }
        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index) { return default(System.IntPtr); }
        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeMemoryAnsi(System.IntPtr s) { }
        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeMemoryUTF16(System.IntPtr s) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10496), Inherited = false)]
    public sealed partial class MarshalAsAttribute : System.Attribute
    {
        public System.Runtime.InteropServices.UnmanagedType ArraySubType;
        public int IidParameterIndex;
        public string MarshalCookie;
        public string MarshalType;
        public System.Type MarshalTypeRef;
        public System.Runtime.InteropServices.VarEnum SafeArraySubType;
        public System.Type SafeArrayUserDefinedSubType;
        public int SizeConst;
        public short SizeParamIndex;
        public MarshalAsAttribute(short unmanagedType) { }
        public MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType unmanagedType) { }
        public System.Runtime.InteropServices.UnmanagedType Value { get { return default(System.Runtime.InteropServices.UnmanagedType); } }
    }
    public partial class MarshalDirectiveException : System.Exception
    {
        public MarshalDirectiveException() { }
        public MarshalDirectiveException(string message) { }
        public MarshalDirectiveException(string message, System.Exception inner) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited = false)]
    public sealed partial class OptionalAttribute : System.Attribute
    {
        public OptionalAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited = false)]
    public sealed partial class PreserveSigAttribute : System.Attribute
    {
        public PreserveSigAttribute() { }
    }
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class SafeBuffer : System.Runtime.InteropServices.SafeHandle
    {
        protected SafeBuffer(bool ownsHandle) : base(default(System.IntPtr), default(bool)) { }
        // Added because SafeHandleZeroOrMinusOneIsInvalid is removed
        public override bool IsInvalid { get { return default(bool); } }
        [System.CLSCompliantAttribute(false)]
        public ulong ByteLength { get { return default(ulong); } }
        [System.CLSCompliantAttribute(false)]
        public unsafe void AcquirePointer(ref byte* pointer) { }
        [System.CLSCompliantAttribute(false)]
        public void Initialize(uint numElements, uint sizeOfEachElement) { }
        [System.CLSCompliantAttribute(false)]
        public void Initialize(ulong numBytes) { }
        [System.CLSCompliantAttribute(false)]
        public void Initialize<T>(uint numElements) where T : struct { }
        [System.CLSCompliantAttribute(false)]
        public T Read<T>(ulong byteOffset) where T : struct { return default(T); }
        [System.CLSCompliantAttribute(false)]
        public void ReadArray<T>(ulong byteOffset, T[] array, int index, int count) where T : struct { }
        public void ReleasePointer() { }
        [System.CLSCompliantAttribute(false)]
        public void Write<T>(ulong byteOffset, T value) where T : struct { }
        [System.CLSCompliantAttribute(false)]
        public void WriteArray<T>(ulong byteOffset, T[] array, int index, int count) where T : struct { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4096), AllowMultiple = false, Inherited = false)]
    public sealed partial class UnmanagedFunctionPointerAttribute : System.Attribute
    {
        public bool BestFitMapping;
        public System.Runtime.InteropServices.CharSet CharSet;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
        public UnmanagedFunctionPointerAttribute(System.Runtime.InteropServices.CallingConvention callingConvention) { }
        public System.Runtime.InteropServices.CallingConvention CallingConvention { get { return default(System.Runtime.InteropServices.CallingConvention); } }
    }
    public enum UnmanagedType
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Marshalling as AnsiBStr may be unavailable in future releases.")]
        AnsiBStr = 35,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Marshalling arbitrary types may be unavailable in future releases. Please specify the type you wish to marshal as.")]
        AsAny = 40,
        Bool = 2,
        BStr = 19,
        ByValArray = 30,
        ByValTStr = 23,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Marshalling as Currency may be unavailable in future releases.")]
        Currency = 15,
        Error = 45,
        FunctionPtr = 38,
        HString = 47,
        I1 = 3,
        I2 = 5,
        I4 = 7,
        I8 = 9,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Marshalling as IDispatch may be unavailable in future releases.")]
        IDispatch = 26,
        IInspectable = 46,
        Interface = 28,
        IUnknown = 25,
        LPArray = 42,
        LPStr = 20,
        LPStruct = 43,
        LPTStr = 22,
        LPWStr = 21,
        R4 = 11,
        R8 = 12,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Marshalling as SafeArray may be unavailable in future releases.")]
        SafeArray = 29,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Applying UnmanagedType.Struct is unnecessary when marshalling a struct. Support for UnmanagedType.Struct when marshalling a reference type may be unavailable in future releases.")]
        Struct = 27,
        SysInt = 31,
        SysUInt = 32,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Marshalling as TBstr may be unavailable in future releases.")]
        TBStr = 36,
        U1 = 4,
        U2 = 6,
        U4 = 8,
        U8 = 10,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Marshalling as VariantBool may be unavailable in future releases.")]
        VariantBool = 37,
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Marshalling as VBByRefString may be unavailable in future releases.")]
        VBByRefStr = 34,
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.ObsoleteAttribute("Marshalling VARIANTs may be unavailable in future releases.")]
    public enum VarEnum
    {
        VT_ARRAY = 8192,
        VT_BLOB = 65,
        VT_BLOB_OBJECT = 70,
        VT_BOOL = 11,
        VT_BSTR = 8,
        VT_BYREF = 16384,
        VT_CARRAY = 28,
        VT_CF = 71,
        VT_CLSID = 72,
        VT_CY = 6,
        VT_DATE = 7,
        VT_DECIMAL = 14,
        VT_DISPATCH = 9,
        VT_EMPTY = 0,
        VT_ERROR = 10,
        VT_FILETIME = 64,
        VT_HRESULT = 25,
        VT_I1 = 16,
        VT_I2 = 2,
        VT_I4 = 3,
        VT_I8 = 20,
        VT_INT = 22,
        VT_LPSTR = 30,
        VT_LPWSTR = 31,
        VT_NULL = 1,
        VT_PTR = 26,
        VT_R4 = 4,
        VT_R8 = 5,
        VT_RECORD = 36,
        VT_SAFEARRAY = 27,
        VT_STORAGE = 67,
        VT_STORED_OBJECT = 69,
        VT_STREAM = 66,
        VT_STREAMED_OBJECT = 68,
        VT_UI1 = 17,
        VT_UI2 = 18,
        VT_UI4 = 19,
        VT_UI8 = 21,
        VT_UINT = 23,
        VT_UNKNOWN = 13,
        VT_USERDEFINED = 29,
        VT_VARIANT = 12,
        VT_VECTOR = 4096,
        VT_VOID = 24,
    }
}
