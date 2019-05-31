// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public sealed partial class AppDomain : System.MarshalByRefObject
    {
        internal AppDomain() { }
        public string BaseDirectory { get { throw null; } }
        public static System.AppDomain CurrentDomain { get { throw null; } }
        public string DynamicDirectory { get { throw null; } }
        public string FriendlyName { get { throw null; } }
        public int Id { get { throw null; } }
        public bool IsFullyTrusted { get { throw null; } }
        public bool IsHomogenous { get { throw null; } }
        public static bool MonitoringIsEnabled { get { throw null; } set { } }
        public long MonitoringSurvivedMemorySize { get { throw null; } }
        public static long MonitoringSurvivedProcessMemorySize { get { throw null; } }
        public long MonitoringTotalAllocatedMemorySize { get { throw null; } }
        public System.TimeSpan MonitoringTotalProcessorTime { get { throw null; } }
        public System.Security.PermissionSet PermissionSet { get { throw null; } }
        public string RelativeSearchPath { get { throw null; } }
        public System.AppDomainSetup SetupInformation { get { throw null; } }
        public bool ShadowCopyFiles { get { throw null; } }
        public event System.AssemblyLoadEventHandler AssemblyLoad { add { } remove { } }
        public event System.ResolveEventHandler AssemblyResolve { add { } remove { } }
        public event System.EventHandler DomainUnload { add { } remove { } }
        public event System.EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> FirstChanceException { add { } remove { } }
        public event System.EventHandler ProcessExit { add { } remove { } }
        public event System.ResolveEventHandler ReflectionOnlyAssemblyResolve { add { } remove { } }
        public event System.ResolveEventHandler ResourceResolve { add { } remove { } }
        public event System.ResolveEventHandler TypeResolve { add { } remove { } }
        public event System.UnhandledExceptionEventHandler UnhandledException { add { } remove { } }
        [System.ObsoleteAttribute("AppDomain.AppendPrivatePath has been deprecated. Please investigate the use of AppDomainSetup.PrivateBinPath instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void AppendPrivatePath(string path) { }
        public string ApplyPolicy(string assemblyName) { throw null; }
        [System.ObsoleteAttribute("AppDomain.ClearPrivatePath has been deprecated. Please investigate the use of AppDomainSetup.PrivateBinPath instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void ClearPrivatePath() { }
        [System.ObsoleteAttribute("AppDomain.ClearShadowCopyPath has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyDirectories instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void ClearShadowCopyPath() { }
        public static System.AppDomain CreateDomain(string friendlyName) { throw null; }
        public System.Runtime.Remoting.ObjectHandle CreateInstance(string assemblyName, string typeName) { throw null; }
        public System.Runtime.Remoting.ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        public System.Runtime.Remoting.ObjectHandle CreateInstance(string assemblyName, string typeName, object[] activationAttributes) { throw null; }
        public object CreateInstanceAndUnwrap(string assemblyName, string typeName) { throw null; }
        public object CreateInstanceAndUnwrap(string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        public object CreateInstanceAndUnwrap(string assemblyName, string typeName, object[] activationAttributes) { throw null; }
        public System.Runtime.Remoting.ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName) { throw null; }
        public System.Runtime.Remoting.ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        public System.Runtime.Remoting.ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, object[] activationAttributes) { throw null; }
        public object CreateInstanceFromAndUnwrap(string assemblyFile, string typeName) { throw null; }
        public object CreateInstanceFromAndUnwrap(string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes) { throw null; }
        public object CreateInstanceFromAndUnwrap(string assemblyFile, string typeName, object[] activationAttributes) { throw null; }
        public int ExecuteAssembly(string assemblyFile) { throw null; }
        public int ExecuteAssembly(string assemblyFile, string[] args) { throw null; }
        public int ExecuteAssembly(string assemblyFile, string[] args, byte[] hashValue, System.Configuration.Assemblies.AssemblyHashAlgorithm hashAlgorithm) { throw null; }
        public int ExecuteAssemblyByName(System.Reflection.AssemblyName assemblyName, params string[] args) { throw null; }
        public int ExecuteAssemblyByName(string assemblyName) { throw null; }
        public int ExecuteAssemblyByName(string assemblyName, params string[] args) { throw null; }
        public System.Reflection.Assembly[] GetAssemblies() { throw null; }
        [System.ObsoleteAttribute("AppDomain.GetCurrentThreadId has been deprecated because it does not provide a stable Id when managed threads are running on fibers (aka lightweight threads). To get a stable identifier for a managed thread, use the ManagedThreadId property on Thread.  https://go.microsoft.com/fwlink/?linkid=14202", false)]
        public static int GetCurrentThreadId() { throw null; }
        public object GetData(string name) { throw null; }
        public bool? IsCompatibilitySwitchSet(string value) { throw null; }
        public bool IsDefaultAppDomain() { throw null; }
        public bool IsFinalizingForUnload() { throw null; }
        public System.Reflection.Assembly Load(byte[] rawAssembly) { throw null; }
        public System.Reflection.Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore) { throw null; }
        public System.Reflection.Assembly Load(System.Reflection.AssemblyName assemblyRef) { throw null; }
        public System.Reflection.Assembly Load(string assemblyString) { throw null; }
        public System.Reflection.Assembly[] ReflectionOnlyGetAssemblies() { throw null; }
        [System.ObsoleteAttribute("AppDomain.SetCachePath has been deprecated. Please investigate the use of AppDomainSetup.CachePath instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetCachePath(string path) { }
        public void SetData(string name, object data) { }
        [System.ObsoleteAttribute("AppDomain.SetDynamicBase has been deprecated. Please investigate the use of AppDomainSetup.DynamicBase instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetDynamicBase(string path) { }
        public void SetPrincipalPolicy(System.Security.Principal.PrincipalPolicy policy) { }
        [System.ObsoleteAttribute("AppDomain.SetShadowCopyFiles has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyFiles instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetShadowCopyFiles() { }
        [System.ObsoleteAttribute("AppDomain.SetShadowCopyPath has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyDirectories instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void SetShadowCopyPath(string path) { }
        public void SetThreadPrincipal(System.Security.Principal.IPrincipal principal) { }
        public override string ToString() { throw null; }
        public static void Unload(System.AppDomain domain) { }
    }
    public sealed partial class AppDomainSetup
    {
        internal AppDomainSetup() { }
        public string ApplicationBase { get { throw null; } }
        public string TargetFrameworkName { get { throw null; } }
    }
    public partial class AppDomainUnloadedException : System.SystemException
    {
        public AppDomainUnloadedException() { }
        protected AppDomainUnloadedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public AppDomainUnloadedException(string message) { }
        public AppDomainUnloadedException(string message, System.Exception innerException) { }
    }
    public sealed partial class ApplicationId
    {
        public ApplicationId(byte[] publicKeyToken, string name, System.Version version, string processorArchitecture, string culture) { }
        public string Culture { get { throw null; } }
        public string Name { get { throw null; } }
        public string ProcessorArchitecture { get { throw null; } }
        public byte[] PublicKeyToken { get { throw null; } }
        public System.Version Version { get { throw null; } }
        public System.ApplicationId Copy() { throw null; }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class AssemblyLoadEventArgs : System.EventArgs
    {
        public AssemblyLoadEventArgs(System.Reflection.Assembly loadedAssembly) { }
        public System.Reflection.Assembly LoadedAssembly { get { throw null; } }
    }
    public delegate void AssemblyLoadEventHandler(object sender, System.AssemblyLoadEventArgs args);
    [System.FlagsAttribute]
    public enum Base64FormattingOptions
    {
        None = 0,
        InsertLineBreaks = 1,
    }
    public static partial class BitConverter
    {
        public static readonly bool IsLittleEndian;
        public static long DoubleToInt64Bits(double value) { throw null; }
        public static byte[] GetBytes(bool value) { throw null; }
        public static byte[] GetBytes(char value) { throw null; }
        public static byte[] GetBytes(double value) { throw null; }
        public static byte[] GetBytes(short value) { throw null; }
        public static byte[] GetBytes(int value) { throw null; }
        public static byte[] GetBytes(long value) { throw null; }
        public static byte[] GetBytes(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(ulong value) { throw null; }
        public static float Int32BitsToSingle(int value) { throw null; }
        public static double Int64BitsToDouble(long value) { throw null; }
        public static int SingleToInt32Bits(float value) { throw null; }
        public static bool ToBoolean(byte[] value, int startIndex) { throw null; }
        public static bool ToBoolean(System.ReadOnlySpan<byte> value) { throw null; }
        public static char ToChar(byte[] value, int startIndex) { throw null; }
        public static char ToChar(System.ReadOnlySpan<byte> value) { throw null; }
        public static double ToDouble(byte[] value, int startIndex) { throw null; }
        public static double ToDouble(System.ReadOnlySpan<byte> value) { throw null; }
        public static short ToInt16(byte[] value, int startIndex) { throw null; }
        public static short ToInt16(System.ReadOnlySpan<byte> value) { throw null; }
        public static int ToInt32(byte[] value, int startIndex) { throw null; }
        public static int ToInt32(System.ReadOnlySpan<byte> value) { throw null; }
        public static long ToInt64(byte[] value, int startIndex) { throw null; }
        public static long ToInt64(System.ReadOnlySpan<byte> value) { throw null; }
        public static float ToSingle(byte[] value, int startIndex) { throw null; }
        public static float ToSingle(System.ReadOnlySpan<byte> value) { throw null; }
        public static string ToString(byte[] value) { throw null; }
        public static string ToString(byte[] value, int startIndex) { throw null; }
        public static string ToString(byte[] value, int startIndex, int length) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(byte[] value, int startIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(System.ReadOnlySpan<byte> value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(byte[] value, int startIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(System.ReadOnlySpan<byte> value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(byte[] value, int startIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(System.ReadOnlySpan<byte> value) { throw null; }
        public static bool TryWriteBytes(System.Span<byte> destination, bool value) { throw null; }
        public static bool TryWriteBytes(System.Span<byte> destination, char value) { throw null; }
        public static bool TryWriteBytes(System.Span<byte> destination, double value) { throw null; }
        public static bool TryWriteBytes(System.Span<byte> destination, short value) { throw null; }
        public static bool TryWriteBytes(System.Span<byte> destination, int value) { throw null; }
        public static bool TryWriteBytes(System.Span<byte> destination, long value) { throw null; }
        public static bool TryWriteBytes(System.Span<byte> destination, float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteBytes(System.Span<byte> destination, ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteBytes(System.Span<byte> destination, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteBytes(System.Span<byte> destination, ulong value) { throw null; }
    }
    public partial class CannotUnloadAppDomainException : System.SystemException
    {
        public CannotUnloadAppDomainException() { }
        protected CannotUnloadAppDomainException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CannotUnloadAppDomainException(string message) { }
        public CannotUnloadAppDomainException(string message, System.Exception innerException) { }
    }
    public abstract partial class ContextBoundObject : System.MarshalByRefObject
    {
        protected ContextBoundObject() { }
    }
    public partial class ContextMarshalException : System.SystemException
    {
        public ContextMarshalException() { }
        protected ContextMarshalException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ContextMarshalException(string message) { }
        public ContextMarshalException(string message, System.Exception inner) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field, Inherited=false)]
    public partial class ContextStaticAttribute : System.Attribute
    {
        public ContextStaticAttribute() { }
    }
    public static partial class Convert
    {
        public static readonly object DBNull;
        public static object ChangeType(object value, System.Type conversionType) { throw null; }
        public static object ChangeType(object value, System.Type conversionType, System.IFormatProvider provider) { throw null; }
        public static object ChangeType(object value, System.TypeCode typeCode) { throw null; }
        public static object ChangeType(object value, System.TypeCode typeCode, System.IFormatProvider provider) { throw null; }
        public static byte[] FromBase64CharArray(char[] inArray, int offset, int length) { throw null; }
        public static byte[] FromBase64String(string s) { throw null; }
        public static System.TypeCode GetTypeCode(object value) { throw null; }
        public static bool IsDBNull(object value) { throw null; }
        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut) { throw null; }
        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut, System.Base64FormattingOptions options) { throw null; }
        public static string ToBase64String(byte[] inArray) { throw null; }
        public static string ToBase64String(byte[] inArray, System.Base64FormattingOptions options) { throw null; }
        public static string ToBase64String(byte[] inArray, int offset, int length) { throw null; }
        public static string ToBase64String(byte[] inArray, int offset, int length, System.Base64FormattingOptions options) { throw null; }
        public static string ToBase64String(System.ReadOnlySpan<byte> bytes, System.Base64FormattingOptions options = System.Base64FormattingOptions.None) { throw null; }
        public static bool ToBoolean(bool value) { throw null; }
        public static bool ToBoolean(byte value) { throw null; }
        public static bool ToBoolean(char value) { throw null; }
        public static bool ToBoolean(System.DateTime value) { throw null; }
        public static bool ToBoolean(decimal value) { throw null; }
        public static bool ToBoolean(double value) { throw null; }
        public static bool ToBoolean(short value) { throw null; }
        public static bool ToBoolean(int value) { throw null; }
        public static bool ToBoolean(long value) { throw null; }
        public static bool ToBoolean(object value) { throw null; }
        public static bool ToBoolean(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(sbyte value) { throw null; }
        public static bool ToBoolean(float value) { throw null; }
        public static bool ToBoolean(string value) { throw null; }
        public static bool ToBoolean(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(ulong value) { throw null; }
        public static byte ToByte(bool value) { throw null; }
        public static byte ToByte(byte value) { throw null; }
        public static byte ToByte(char value) { throw null; }
        public static byte ToByte(System.DateTime value) { throw null; }
        public static byte ToByte(decimal value) { throw null; }
        public static byte ToByte(double value) { throw null; }
        public static byte ToByte(short value) { throw null; }
        public static byte ToByte(int value) { throw null; }
        public static byte ToByte(long value) { throw null; }
        public static byte ToByte(object value) { throw null; }
        public static byte ToByte(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(sbyte value) { throw null; }
        public static byte ToByte(float value) { throw null; }
        public static byte ToByte(string value) { throw null; }
        public static byte ToByte(string value, System.IFormatProvider provider) { throw null; }
        public static byte ToByte(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(ulong value) { throw null; }
        public static char ToChar(bool value) { throw null; }
        public static char ToChar(byte value) { throw null; }
        public static char ToChar(char value) { throw null; }
        public static char ToChar(System.DateTime value) { throw null; }
        public static char ToChar(decimal value) { throw null; }
        public static char ToChar(double value) { throw null; }
        public static char ToChar(short value) { throw null; }
        public static char ToChar(int value) { throw null; }
        public static char ToChar(long value) { throw null; }
        public static char ToChar(object value) { throw null; }
        public static char ToChar(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(sbyte value) { throw null; }
        public static char ToChar(float value) { throw null; }
        public static char ToChar(string value) { throw null; }
        public static char ToChar(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(ulong value) { throw null; }
        public static System.DateTime ToDateTime(bool value) { throw null; }
        public static System.DateTime ToDateTime(byte value) { throw null; }
        public static System.DateTime ToDateTime(char value) { throw null; }
        public static System.DateTime ToDateTime(System.DateTime value) { throw null; }
        public static System.DateTime ToDateTime(decimal value) { throw null; }
        public static System.DateTime ToDateTime(double value) { throw null; }
        public static System.DateTime ToDateTime(short value) { throw null; }
        public static System.DateTime ToDateTime(int value) { throw null; }
        public static System.DateTime ToDateTime(long value) { throw null; }
        public static System.DateTime ToDateTime(object value) { throw null; }
        public static System.DateTime ToDateTime(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.DateTime ToDateTime(sbyte value) { throw null; }
        public static System.DateTime ToDateTime(float value) { throw null; }
        public static System.DateTime ToDateTime(string value) { throw null; }
        public static System.DateTime ToDateTime(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.DateTime ToDateTime(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.DateTime ToDateTime(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.DateTime ToDateTime(ulong value) { throw null; }
        public static decimal ToDecimal(bool value) { throw null; }
        public static decimal ToDecimal(byte value) { throw null; }
        public static decimal ToDecimal(char value) { throw null; }
        public static decimal ToDecimal(System.DateTime value) { throw null; }
        public static decimal ToDecimal(decimal value) { throw null; }
        public static decimal ToDecimal(double value) { throw null; }
        public static decimal ToDecimal(short value) { throw null; }
        public static decimal ToDecimal(int value) { throw null; }
        public static decimal ToDecimal(long value) { throw null; }
        public static decimal ToDecimal(object value) { throw null; }
        public static decimal ToDecimal(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(sbyte value) { throw null; }
        public static decimal ToDecimal(float value) { throw null; }
        public static decimal ToDecimal(string value) { throw null; }
        public static decimal ToDecimal(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(ulong value) { throw null; }
        public static double ToDouble(bool value) { throw null; }
        public static double ToDouble(byte value) { throw null; }
        public static double ToDouble(char value) { throw null; }
        public static double ToDouble(System.DateTime value) { throw null; }
        public static double ToDouble(decimal value) { throw null; }
        public static double ToDouble(double value) { throw null; }
        public static double ToDouble(short value) { throw null; }
        public static double ToDouble(int value) { throw null; }
        public static double ToDouble(long value) { throw null; }
        public static double ToDouble(object value) { throw null; }
        public static double ToDouble(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(sbyte value) { throw null; }
        public static double ToDouble(float value) { throw null; }
        public static double ToDouble(string value) { throw null; }
        public static double ToDouble(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(ulong value) { throw null; }
        public static short ToInt16(bool value) { throw null; }
        public static short ToInt16(byte value) { throw null; }
        public static short ToInt16(char value) { throw null; }
        public static short ToInt16(System.DateTime value) { throw null; }
        public static short ToInt16(decimal value) { throw null; }
        public static short ToInt16(double value) { throw null; }
        public static short ToInt16(short value) { throw null; }
        public static short ToInt16(int value) { throw null; }
        public static short ToInt16(long value) { throw null; }
        public static short ToInt16(object value) { throw null; }
        public static short ToInt16(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(sbyte value) { throw null; }
        public static short ToInt16(float value) { throw null; }
        public static short ToInt16(string value) { throw null; }
        public static short ToInt16(string value, System.IFormatProvider provider) { throw null; }
        public static short ToInt16(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(ulong value) { throw null; }
        public static int ToInt32(bool value) { throw null; }
        public static int ToInt32(byte value) { throw null; }
        public static int ToInt32(char value) { throw null; }
        public static int ToInt32(System.DateTime value) { throw null; }
        public static int ToInt32(decimal value) { throw null; }
        public static int ToInt32(double value) { throw null; }
        public static int ToInt32(short value) { throw null; }
        public static int ToInt32(int value) { throw null; }
        public static int ToInt32(long value) { throw null; }
        public static int ToInt32(object value) { throw null; }
        public static int ToInt32(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(sbyte value) { throw null; }
        public static int ToInt32(float value) { throw null; }
        public static int ToInt32(string value) { throw null; }
        public static int ToInt32(string value, System.IFormatProvider provider) { throw null; }
        public static int ToInt32(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(ulong value) { throw null; }
        public static long ToInt64(bool value) { throw null; }
        public static long ToInt64(byte value) { throw null; }
        public static long ToInt64(char value) { throw null; }
        public static long ToInt64(System.DateTime value) { throw null; }
        public static long ToInt64(decimal value) { throw null; }
        public static long ToInt64(double value) { throw null; }
        public static long ToInt64(short value) { throw null; }
        public static long ToInt64(int value) { throw null; }
        public static long ToInt64(long value) { throw null; }
        public static long ToInt64(object value) { throw null; }
        public static long ToInt64(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(sbyte value) { throw null; }
        public static long ToInt64(float value) { throw null; }
        public static long ToInt64(string value) { throw null; }
        public static long ToInt64(string value, System.IFormatProvider provider) { throw null; }
        public static long ToInt64(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(bool value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(byte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(System.DateTime value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(double value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(short value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(ulong value) { throw null; }
        public static float ToSingle(bool value) { throw null; }
        public static float ToSingle(byte value) { throw null; }
        public static float ToSingle(char value) { throw null; }
        public static float ToSingle(System.DateTime value) { throw null; }
        public static float ToSingle(decimal value) { throw null; }
        public static float ToSingle(double value) { throw null; }
        public static float ToSingle(short value) { throw null; }
        public static float ToSingle(int value) { throw null; }
        public static float ToSingle(long value) { throw null; }
        public static float ToSingle(object value) { throw null; }
        public static float ToSingle(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(sbyte value) { throw null; }
        public static float ToSingle(float value) { throw null; }
        public static float ToSingle(string value) { throw null; }
        public static float ToSingle(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(ulong value) { throw null; }
        public static string ToString(bool value) { throw null; }
        public static string ToString(bool value, System.IFormatProvider provider) { throw null; }
        public static string ToString(byte value) { throw null; }
        public static string ToString(byte value, System.IFormatProvider provider) { throw null; }
        public static string ToString(byte value, int toBase) { throw null; }
        public static string ToString(char value) { throw null; }
        public static string ToString(char value, System.IFormatProvider provider) { throw null; }
        public static string ToString(System.DateTime value) { throw null; }
        public static string ToString(System.DateTime value, System.IFormatProvider provider) { throw null; }
        public static string ToString(decimal value) { throw null; }
        public static string ToString(decimal value, System.IFormatProvider provider) { throw null; }
        public static string ToString(double value) { throw null; }
        public static string ToString(double value, System.IFormatProvider provider) { throw null; }
        public static string ToString(short value) { throw null; }
        public static string ToString(short value, System.IFormatProvider provider) { throw null; }
        public static string ToString(short value, int toBase) { throw null; }
        public static string ToString(int value) { throw null; }
        public static string ToString(int value, System.IFormatProvider provider) { throw null; }
        public static string ToString(int value, int toBase) { throw null; }
        public static string ToString(long value) { throw null; }
        public static string ToString(long value, System.IFormatProvider provider) { throw null; }
        public static string ToString(long value, int toBase) { throw null; }
        public static string ToString(object value) { throw null; }
        public static string ToString(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(sbyte value, System.IFormatProvider provider) { throw null; }
        public static string ToString(float value) { throw null; }
        public static string ToString(float value, System.IFormatProvider provider) { throw null; }
        public static string ToString(string value) { throw null; }
        public static string ToString(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ushort value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(bool value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(byte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(System.DateTime value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(double value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(short value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(bool value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(byte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(System.DateTime value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(double value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(short value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(bool value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(byte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(char value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(System.DateTime value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(decimal value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(double value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(short value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(object value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value, System.IFormatProvider provider) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value, int fromBase) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(ulong value) { throw null; }
        public static bool TryFromBase64Chars(System.ReadOnlySpan<char> chars, System.Span<byte> bytes, out int bytesWritten) { throw null; }
        public static bool TryFromBase64String(string s, System.Span<byte> bytes, out int bytesWritten) { throw null; }
        public static bool TryToBase64Chars(System.ReadOnlySpan<byte> bytes, System.Span<char> chars, out int charsWritten, System.Base64FormattingOptions options = System.Base64FormattingOptions.None) { throw null; }
    }
    public static partial class Environment
    {
        public static string CommandLine { get { throw null; } }
        public static string CurrentDirectory { get { throw null; } set { } }
        public static int CurrentManagedThreadId { get { throw null; } }
        public static int ExitCode { get { throw null; } set { } }
        public static bool HasShutdownStarted { get { throw null; } }
        public static bool Is64BitOperatingSystem { get { throw null; } }
        public static bool Is64BitProcess { get { throw null; } }
        public static string MachineName { get { throw null; } }
        public static string NewLine { get { throw null; } }
        public static System.OperatingSystem OSVersion { get { throw null; } }
        public static int ProcessorCount { get { throw null; } }
        public static string StackTrace { get { throw null; } }
        public static string SystemDirectory { get { throw null; } }
        public static int SystemPageSize { get { throw null; } }
        public static int TickCount { get { throw null; } }
        public static long TickCount64 { get { throw null; } }
        public static string UserDomainName { get { throw null; } }
        public static bool UserInteractive { get { throw null; } }
        public static string UserName { get { throw null; } }
        public static System.Version Version { get { throw null; } }
        public static long WorkingSet { get { throw null; } }
        public static void Exit(int exitCode) { }
        public static string ExpandEnvironmentVariables(string name) { throw null; }
        public static void FailFast(string message) { }
        public static void FailFast(string message, System.Exception exception) { }
        public static string[] GetCommandLineArgs() { throw null; }
        public static string GetEnvironmentVariable(string variable) { throw null; }
        public static string GetEnvironmentVariable(string variable, System.EnvironmentVariableTarget target) { throw null; }
        public static System.Collections.IDictionary GetEnvironmentVariables() { throw null; }
        public static System.Collections.IDictionary GetEnvironmentVariables(System.EnvironmentVariableTarget target) { throw null; }
        public static string GetFolderPath(System.Environment.SpecialFolder folder) { throw null; }
        public static string GetFolderPath(System.Environment.SpecialFolder folder, System.Environment.SpecialFolderOption option) { throw null; }
        public static string[] GetLogicalDrives() { throw null; }
        public static void SetEnvironmentVariable(string variable, string value) { }
        public static void SetEnvironmentVariable(string variable, string value, System.EnvironmentVariableTarget target) { }
        public enum SpecialFolder
        {
            Desktop = 0,
            Programs = 2,
            MyDocuments = 5,
            Personal = 5,
            Favorites = 6,
            Startup = 7,
            Recent = 8,
            SendTo = 9,
            StartMenu = 11,
            MyMusic = 13,
            MyVideos = 14,
            DesktopDirectory = 16,
            MyComputer = 17,
            NetworkShortcuts = 19,
            Fonts = 20,
            Templates = 21,
            CommonStartMenu = 22,
            CommonPrograms = 23,
            CommonStartup = 24,
            CommonDesktopDirectory = 25,
            ApplicationData = 26,
            PrinterShortcuts = 27,
            LocalApplicationData = 28,
            InternetCache = 32,
            Cookies = 33,
            History = 34,
            CommonApplicationData = 35,
            Windows = 36,
            System = 37,
            ProgramFiles = 38,
            MyPictures = 39,
            UserProfile = 40,
            SystemX86 = 41,
            ProgramFilesX86 = 42,
            CommonProgramFiles = 43,
            CommonProgramFilesX86 = 44,
            CommonTemplates = 45,
            CommonDocuments = 46,
            CommonAdminTools = 47,
            AdminTools = 48,
            CommonMusic = 53,
            CommonPictures = 54,
            CommonVideos = 55,
            Resources = 56,
            LocalizedResources = 57,
            CommonOemLinks = 58,
            CDBurning = 59,
        }
        public enum SpecialFolderOption
        {
            None = 0,
            DoNotVerify = 16384,
            Create = 32768,
        }
    }
    public enum EnvironmentVariableTarget
    {
        Process = 0,
        User = 1,
        Machine = 2,
    }
    public enum LoaderOptimization
    {
        NotSpecified = 0,
        SingleDomain = 1,
        MultiDomain = 2,
        [System.ObsoleteAttribute("This method has been deprecated. Please use Assembly.Load() instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        DomainMask = 3,
        MultiDomainHost = 3,
        [System.ObsoleteAttribute("This method has been deprecated. Please use Assembly.Load() instead. https://go.microsoft.com/fwlink/?linkid=14202")]
        DisallowBindings = 4,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public sealed partial class LoaderOptimizationAttribute : System.Attribute
    {
        public LoaderOptimizationAttribute(byte value) { }
        public LoaderOptimizationAttribute(System.LoaderOptimization value) { }
        public System.LoaderOptimization Value { get { throw null; } }
    }
    public static partial class Math
    {
        public const double E = 2.718281828459045;
        public const double PI = 3.141592653589793;
        public static decimal Abs(decimal value) { throw null; }
        public static double Abs(double value) { throw null; }
        public static short Abs(short value) { throw null; }
        public static int Abs(int value) { throw null; }
        public static long Abs(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Abs(sbyte value) { throw null; }
        public static float Abs(float value) { throw null; }
        public static double Acos(double d) { throw null; }
        public static double Acosh(double d) { throw null; }
        public static double Asin(double d) { throw null; }
        public static double Asinh(double d) { throw null; }
        public static double Atan(double d) { throw null; }
        public static double Atan2(double y, double x) { throw null; }
        public static double Atanh(double d) { throw null; }
        public static long BigMul(int a, int b) { throw null; }
        public static double BitDecrement(double x) { throw null; }
        public static double BitIncrement(double x) { throw null; }
        public static double Cbrt(double d) { throw null; }
        public static decimal Ceiling(decimal d) { throw null; }
        public static double Ceiling(double a) { throw null; }
        public static byte Clamp(byte value, byte min, byte max) { throw null; }
        public static decimal Clamp(decimal value, decimal min, decimal max) { throw null; }
        public static double Clamp(double value, double min, double max) { throw null; }
        public static short Clamp(short value, short min, short max) { throw null; }
        public static int Clamp(int value, int min, int max) { throw null; }
        public static long Clamp(long value, long min, long max) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Clamp(sbyte value, sbyte min, sbyte max) { throw null; }
        public static float Clamp(float value, float min, float max) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort Clamp(ushort value, ushort min, ushort max) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Clamp(uint value, uint min, uint max) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Clamp(ulong value, ulong min, ulong max) { throw null; }
        public static double CopySign(double x, double y) { throw null; }
        public static double Cos(double d) { throw null; }
        public static double Cosh(double value) { throw null; }
        public static int DivRem(int a, int b, out int result) { throw null; }
        public static long DivRem(long a, long b, out long result) { throw null; }
        public static double Exp(double d) { throw null; }
        public static decimal Floor(decimal d) { throw null; }
        public static double Floor(double d) { throw null; }
        public static double FusedMultiplyAdd(double x, double y, double z) { throw null; }
        public static double IEEERemainder(double x, double y) { throw null; }
        public static int ILogB(double x) { throw null; }
        public static double Log(double d) { throw null; }
        public static double Log(double a, double newBase) { throw null; }
        public static double Log10(double d) { throw null; }
        public static double Log2(double x) { throw null; }
        public static byte Max(byte val1, byte val2) { throw null; }
        public static decimal Max(decimal val1, decimal val2) { throw null; }
        public static double Max(double val1, double val2) { throw null; }
        public static short Max(short val1, short val2) { throw null; }
        public static int Max(int val1, int val2) { throw null; }
        public static long Max(long val1, long val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Max(sbyte val1, sbyte val2) { throw null; }
        public static float Max(float val1, float val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort Max(ushort val1, ushort val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Max(uint val1, uint val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Max(ulong val1, ulong val2) { throw null; }
        public static double MaxMagnitude(double x, double y) { throw null; }
        public static byte Min(byte val1, byte val2) { throw null; }
        public static decimal Min(decimal val1, decimal val2) { throw null; }
        public static double Min(double val1, double val2) { throw null; }
        public static short Min(short val1, short val2) { throw null; }
        public static int Min(int val1, int val2) { throw null; }
        public static long Min(long val1, long val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Min(sbyte val1, sbyte val2) { throw null; }
        public static float Min(float val1, float val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort Min(ushort val1, ushort val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint Min(uint val1, uint val2) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong Min(ulong val1, ulong val2) { throw null; }
        public static double MinMagnitude(double x, double y) { throw null; }
        public static double Pow(double x, double y) { throw null; }
        public static decimal Round(decimal d) { throw null; }
        public static decimal Round(decimal d, int decimals) { throw null; }
        public static decimal Round(decimal d, int decimals, System.MidpointRounding mode) { throw null; }
        public static decimal Round(decimal d, System.MidpointRounding mode) { throw null; }
        public static double Round(double a) { throw null; }
        public static double Round(double value, int digits) { throw null; }
        public static double Round(double value, int digits, System.MidpointRounding mode) { throw null; }
        public static double Round(double value, System.MidpointRounding mode) { throw null; }
        public static double ScaleB(double x, int n) { throw null; }
        public static int Sign(decimal value) { throw null; }
        public static int Sign(double value) { throw null; }
        public static int Sign(short value) { throw null; }
        public static int Sign(int value) { throw null; }
        public static int Sign(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int Sign(sbyte value) { throw null; }
        public static int Sign(float value) { throw null; }
        public static double Sin(double a) { throw null; }
        public static double Sinh(double value) { throw null; }
        public static double Sqrt(double d) { throw null; }
        public static double Tan(double a) { throw null; }
        public static double Tanh(double value) { throw null; }
        public static decimal Truncate(decimal d) { throw null; }
        public static double Truncate(double d) { throw null; }
    }
    public static partial class MathF
    {
        public const float E = 2.7182817f;
        public const float PI = 3.1415927f;
        public static float Abs(float x) { throw null; }
        public static float Acos(float x) { throw null; }
        public static float Acosh(float x) { throw null; }
        public static float Asin(float x) { throw null; }
        public static float Asinh(float x) { throw null; }
        public static float Atan(float x) { throw null; }
        public static float Atan2(float y, float x) { throw null; }
        public static float Atanh(float x) { throw null; }
        public static float BitDecrement(float x) { throw null; }
        public static float BitIncrement(float x) { throw null; }
        public static float Cbrt(float x) { throw null; }
        public static float Ceiling(float x) { throw null; }
        public static float CopySign(float x, float y) { throw null; }
        public static float Cos(float x) { throw null; }
        public static float Cosh(float x) { throw null; }
        public static float Exp(float x) { throw null; }
        public static float Floor(float x) { throw null; }
        public static float FusedMultiplyAdd(float x, float y, float z) { throw null; }
        public static float IEEERemainder(float x, float y) { throw null; }
        public static int ILogB(float x) { throw null; }
        public static float Log(float x) { throw null; }
        public static float Log(float x, float y) { throw null; }
        public static float Log10(float x) { throw null; }
        public static float Log2(float x) { throw null; }
        public static float Max(float x, float y) { throw null; }
        public static float MaxMagnitude(float x, float y) { throw null; }
        public static float Min(float x, float y) { throw null; }
        public static float MinMagnitude(float x, float y) { throw null; }
        public static float Pow(float x, float y) { throw null; }
        public static float Round(float x) { throw null; }
        public static float Round(float x, int digits) { throw null; }
        public static float Round(float x, int digits, System.MidpointRounding mode) { throw null; }
        public static float Round(float x, System.MidpointRounding mode) { throw null; }
        public static float ScaleB(float x, int n) { throw null; }
        public static int Sign(float x) { throw null; }
        public static float Sin(float x) { throw null; }
        public static float Sinh(float x) { throw null; }
        public static float Sqrt(float x) { throw null; }
        public static float Tan(float x) { throw null; }
        public static float Tanh(float x) { throw null; }
        public static float Truncate(float x) { throw null; }
    }
    public sealed partial class OperatingSystem : System.ICloneable, System.Runtime.Serialization.ISerializable
    {
        public OperatingSystem(System.PlatformID platform, System.Version version) { }
        public System.PlatformID Platform { get { throw null; } }
        public string ServicePack { get { throw null; } }
        public System.Version Version { get { throw null; } }
        public string VersionString { get { throw null; } }
        public object Clone() { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    public enum PlatformID
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        Win32S = 0,
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        Win32Windows = 1,
        Win32NT = 2,
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        WinCE = 3,
        Unix = 4,
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        Xbox = 5,
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        MacOSX = 6,
    }
    public partial class Progress<T> : System.IProgress<T>
    {
        public Progress() { }
        public Progress(System.Action<T> handler) { }
        public event System.EventHandler<T> ProgressChanged { add { } remove { } }
        protected virtual void OnReport(T value) { }
        void System.IProgress<T>.Report(T value) { }
    }
    public partial class Random
    {
        public Random() { }
        public Random(int Seed) { }
        public virtual int Next() { throw null; }
        public virtual int Next(int maxValue) { throw null; }
        public virtual int Next(int minValue, int maxValue) { throw null; }
        public virtual void NextBytes(byte[] buffer) { }
        public virtual void NextBytes(System.Span<byte> buffer) { }
        public virtual double NextDouble() { throw null; }
        protected virtual double Sample() { throw null; }
    }
    public delegate System.Reflection.Assembly ResolveEventHandler(object sender, System.ResolveEventArgs args);
    public abstract partial class StringComparer : System.Collections.Generic.IComparer<string>, System.Collections.Generic.IEqualityComparer<string>, System.Collections.IComparer, System.Collections.IEqualityComparer
    {
        protected StringComparer() { }
        public static System.StringComparer CurrentCulture { get { throw null; } }
        public static System.StringComparer CurrentCultureIgnoreCase { get { throw null; } }
        public static System.StringComparer InvariantCulture { get { throw null; } }
        public static System.StringComparer InvariantCultureIgnoreCase { get { throw null; } }
        public static System.StringComparer Ordinal { get { throw null; } }
        public static System.StringComparer OrdinalIgnoreCase { get { throw null; } }
        public int Compare(object x, object y) { throw null; }
        public abstract int Compare(string x, string y);
        public static System.StringComparer Create(System.Globalization.CultureInfo culture, bool ignoreCase) { throw null; }
        public static System.StringComparer Create(System.Globalization.CultureInfo culture, System.Globalization.CompareOptions options) { throw null; }
        public new bool Equals(object x, object y) { throw null; }
        public abstract bool Equals(string x, string y);
        public static System.StringComparer FromComparison(System.StringComparison comparisonType) { throw null; }
        public int GetHashCode(object obj) { throw null; }
        public abstract int GetHashCode(string obj);
    }
    public static partial class StringNormalizationExtensions
    {
        public static bool IsNormalized(this string value) { throw null; }
        public static bool IsNormalized(this string value, System.Text.NormalizationForm normalizationForm) { throw null; }
        public static string Normalize(this string value) { throw null; }
        public static string Normalize(this string value, System.Text.NormalizationForm normalizationForm) { throw null; }
    }
    public partial class UriBuilder
    {
        public UriBuilder() { }
        public UriBuilder(string uri) { }
        public UriBuilder(string schemeName, string hostName) { }
        public UriBuilder(string scheme, string host, int portNumber) { }
        public UriBuilder(string scheme, string host, int port, string pathValue) { }
        public UriBuilder(string scheme, string host, int port, string path, string extraValue) { }
        public UriBuilder(System.Uri uri) { }
        public string Fragment { get { throw null; } set { } }
        public string Host { get { throw null; } set { } }
        public string Password { get { throw null; } set { } }
        public string Path { get { throw null; } set { } }
        public int Port { get { throw null; } set { } }
        public string Query { get { throw null; } set { } }
        public string Scheme { get { throw null; } set { } }
        public System.Uri Uri { get { throw null; } }
        public string UserName { get { throw null; } set { } }
        public override bool Equals(object rparam) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace System.CodeDom.Compiler
{
    public partial class IndentedTextWriter : System.IO.TextWriter
    {
        public const string DefaultTabString = "    ";
        public IndentedTextWriter(System.IO.TextWriter writer) { }
        public IndentedTextWriter(System.IO.TextWriter writer, string tabString) { }
        public override System.Text.Encoding Encoding { get { throw null; } }
        public int Indent { get { throw null; } set { } }
        public System.IO.TextWriter InnerWriter { get { throw null; } }
        public override string NewLine { get { throw null; } set { } }
        public override void Close() { }
        public override void Flush() { }
        protected virtual void OutputTabs() { }
        public override void Write(bool value) { }
        public override void Write(char value) { }
        public override void Write(char[] buffer) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(double value) { }
        public override void Write(int value) { }
        public override void Write(long value) { }
        public override void Write(object value) { }
        public override void Write(float value) { }
        public override void Write(string s) { }
        public override void Write(string format, object arg0) { }
        public override void Write(string format, object arg0, object arg1) { }
        public override void Write(string format, params object[] arg) { }
        public override void WriteLine() { }
        public override void WriteLine(bool value) { }
        public override void WriteLine(char value) { }
        public override void WriteLine(char[] buffer) { }
        public override void WriteLine(char[] buffer, int index, int count) { }
        public override void WriteLine(double value) { }
        public override void WriteLine(int value) { }
        public override void WriteLine(long value) { }
        public override void WriteLine(object value) { }
        public override void WriteLine(float value) { }
        public override void WriteLine(string s) { }
        public override void WriteLine(string format, object arg0) { }
        public override void WriteLine(string format, object arg0, object arg1) { }
        public override void WriteLine(string format, params object[] arg) { }
        [System.CLSCompliantAttribute(false)]
        public override void WriteLine(uint value) { }
        public void WriteLineNoTabs(string s) { }
    }
}
namespace System.Collections
{
    public partial class ArrayList : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.ICloneable
    {
        public ArrayList() { }
        public ArrayList(System.Collections.ICollection c) { }
        public ArrayList(int capacity) { }
        public virtual int Capacity { get { throw null; } set { } }
        public virtual int Count { get { throw null; } }
        public virtual bool IsFixedSize { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public virtual object this[int index] { get { throw null; } set { } }
        public virtual object SyncRoot { get { throw null; } }
        public static System.Collections.ArrayList Adapter(System.Collections.IList list) { throw null; }
        public virtual int Add(object value) { throw null; }
        public virtual void AddRange(System.Collections.ICollection c) { }
        public virtual int BinarySearch(int index, int count, object value, System.Collections.IComparer comparer) { throw null; }
        public virtual int BinarySearch(object value) { throw null; }
        public virtual int BinarySearch(object value, System.Collections.IComparer comparer) { throw null; }
        public virtual void Clear() { }
        public virtual object Clone() { throw null; }
        public virtual bool Contains(object item) { throw null; }
        public virtual void CopyTo(System.Array array) { }
        public virtual void CopyTo(System.Array array, int arrayIndex) { }
        public virtual void CopyTo(int index, System.Array array, int arrayIndex, int count) { }
        public static System.Collections.ArrayList FixedSize(System.Collections.ArrayList list) { throw null; }
        public static System.Collections.IList FixedSize(System.Collections.IList list) { throw null; }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual System.Collections.IEnumerator GetEnumerator(int index, int count) { throw null; }
        public virtual System.Collections.ArrayList GetRange(int index, int count) { throw null; }
        public virtual int IndexOf(object value) { throw null; }
        public virtual int IndexOf(object value, int startIndex) { throw null; }
        public virtual int IndexOf(object value, int startIndex, int count) { throw null; }
        public virtual void Insert(int index, object value) { }
        public virtual void InsertRange(int index, System.Collections.ICollection c) { }
        public virtual int LastIndexOf(object value) { throw null; }
        public virtual int LastIndexOf(object value, int startIndex) { throw null; }
        public virtual int LastIndexOf(object value, int startIndex, int count) { throw null; }
        public static System.Collections.ArrayList ReadOnly(System.Collections.ArrayList list) { throw null; }
        public static System.Collections.IList ReadOnly(System.Collections.IList list) { throw null; }
        public virtual void Remove(object obj) { }
        public virtual void RemoveAt(int index) { }
        public virtual void RemoveRange(int index, int count) { }
        public static System.Collections.ArrayList Repeat(object value, int count) { throw null; }
        public virtual void Reverse() { }
        public virtual void Reverse(int index, int count) { }
        public virtual void SetRange(int index, System.Collections.ICollection c) { }
        public virtual void Sort() { }
        public virtual void Sort(System.Collections.IComparer comparer) { }
        public virtual void Sort(int index, int count, System.Collections.IComparer comparer) { }
        public static System.Collections.ArrayList Synchronized(System.Collections.ArrayList list) { throw null; }
        public static System.Collections.IList Synchronized(System.Collections.IList list) { throw null; }
        public virtual object[] ToArray() { throw null; }
        public virtual System.Array ToArray(System.Type type) { throw null; }
        public virtual void TrimToSize() { }
    }
    public sealed partial class Comparer : System.Collections.IComparer, System.Runtime.Serialization.ISerializable
    {
        public static readonly System.Collections.Comparer Default;
        public static readonly System.Collections.Comparer DefaultInvariant;
        public Comparer(System.Globalization.CultureInfo culture) { }
        public int Compare(object a, object b) { throw null; }
        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class Hashtable : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.ICloneable, System.Runtime.Serialization.IDeserializationCallback, System.Runtime.Serialization.ISerializable
    {
        public Hashtable() { }
        public Hashtable(System.Collections.IDictionary d) { }
        public Hashtable(System.Collections.IDictionary d, System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use Hashtable(IDictionary, IEqualityComparer) instead.")]
        public Hashtable(System.Collections.IDictionary d, System.Collections.IHashCodeProvider hcp, System.Collections.IComparer comparer) { }
        public Hashtable(System.Collections.IDictionary d, float loadFactor) { }
        public Hashtable(System.Collections.IDictionary d, float loadFactor, System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use Hashtable(IDictionary, float, IEqualityComparer) instead.")]
        public Hashtable(System.Collections.IDictionary d, float loadFactor, System.Collections.IHashCodeProvider hcp, System.Collections.IComparer comparer) { }
        public Hashtable(System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use Hashtable(IEqualityComparer) instead.")]
        public Hashtable(System.Collections.IHashCodeProvider hcp, System.Collections.IComparer comparer) { }
        public Hashtable(int capacity) { }
        public Hashtable(int capacity, System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use Hashtable(int, IEqualityComparer) instead.")]
        public Hashtable(int capacity, System.Collections.IHashCodeProvider hcp, System.Collections.IComparer comparer) { }
        public Hashtable(int capacity, float loadFactor) { }
        public Hashtable(int capacity, float loadFactor, System.Collections.IEqualityComparer equalityComparer) { }
        [System.ObsoleteAttribute("Please use Hashtable(int, float, IEqualityComparer) instead.")]
        public Hashtable(int capacity, float loadFactor, System.Collections.IHashCodeProvider hcp, System.Collections.IComparer comparer) { }
        protected Hashtable(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.ObsoleteAttribute("Please use KeyComparer properties.")]
        protected System.Collections.IComparer comparer { get { throw null; } set { } }
        public virtual int Count { get { throw null; } }
        protected System.Collections.IEqualityComparer EqualityComparer { get { throw null; } }
        [System.ObsoleteAttribute("Please use EqualityComparer property.")]
        protected System.Collections.IHashCodeProvider hcp { get { throw null; } set { } }
        public virtual bool IsFixedSize { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public virtual object this[object key] { get { throw null; } set { } }
        public virtual System.Collections.ICollection Keys { get { throw null; } }
        public virtual object SyncRoot { get { throw null; } }
        public virtual System.Collections.ICollection Values { get { throw null; } }
        public virtual void Add(object key, object value) { }
        public virtual void Clear() { }
        public virtual object Clone() { throw null; }
        public virtual bool Contains(object key) { throw null; }
        public virtual bool ContainsKey(object key) { throw null; }
        public virtual bool ContainsValue(object value) { throw null; }
        public virtual void CopyTo(System.Array array, int arrayIndex) { }
        public virtual System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
        protected virtual int GetHash(object key) { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        protected virtual bool KeyEquals(object item, object key) { throw null; }
        public virtual void OnDeserialization(object sender) { }
        public virtual void Remove(object key) { }
        public static System.Collections.Hashtable Synchronized(System.Collections.Hashtable table) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.ObsoleteAttribute("Please use IEqualityComparer instead.")]
    public partial interface IHashCodeProvider
    {
        int GetHashCode(object obj);
    }
}
namespace System.Diagnostics
{
    public partial class Stopwatch
    {
        public static readonly long Frequency;
        public static readonly bool IsHighResolution;
        public Stopwatch() { }
        public System.TimeSpan Elapsed { get { throw null; } }
        public long ElapsedMilliseconds { get { throw null; } }
        public long ElapsedTicks { get { throw null; } }
        public bool IsRunning { get { throw null; } }
        public static long GetTimestamp() { throw null; }
        public void Reset() { }
        public void Restart() { }
        public void Start() { }
        public static System.Diagnostics.Stopwatch StartNew() { throw null; }
        public void Stop() { }
    }
}
namespace System.Globalization
{
    public static partial class GlobalizationExtensions
    {
        public static System.StringComparer GetStringComparer(this System.Globalization.CompareInfo compareInfo, System.Globalization.CompareOptions options) { throw null; }
    }
}
namespace System.IO
{
    public partial class BinaryReader : System.IDisposable
    {
        public BinaryReader(System.IO.Stream input) { }
        public BinaryReader(System.IO.Stream input, System.Text.Encoding encoding) { }
        public BinaryReader(System.IO.Stream input, System.Text.Encoding encoding, bool leaveOpen) { }
        public virtual System.IO.Stream BaseStream { get { throw null; } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected virtual void FillBuffer(int numBytes) { }
        public virtual int PeekChar() { throw null; }
        public virtual int Read() { throw null; }
        public virtual int Read(byte[] buffer, int index, int count) { throw null; }
        public virtual int Read(char[] buffer, int index, int count) { throw null; }
        public virtual int Read(System.Span<byte> buffer) { throw null; }
        public virtual int Read(System.Span<char> buffer) { throw null; }
        protected internal int Read7BitEncodedInt() { throw null; }
        public virtual bool ReadBoolean() { throw null; }
        public virtual byte ReadByte() { throw null; }
        public virtual byte[] ReadBytes(int count) { throw null; }
        public virtual char ReadChar() { throw null; }
        public virtual char[] ReadChars(int count) { throw null; }
        public virtual decimal ReadDecimal() { throw null; }
        public virtual double ReadDouble() { throw null; }
        public virtual short ReadInt16() { throw null; }
        public virtual int ReadInt32() { throw null; }
        public virtual long ReadInt64() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual sbyte ReadSByte() { throw null; }
        public virtual float ReadSingle() { throw null; }
        public virtual string ReadString() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual ushort ReadUInt16() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual uint ReadUInt32() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public virtual ulong ReadUInt64() { throw null; }
    }
    public partial class BinaryWriter : System.IAsyncDisposable, System.IDisposable
    {
        public static readonly System.IO.BinaryWriter Null;
        protected System.IO.Stream OutStream;
        protected BinaryWriter() { }
        public BinaryWriter(System.IO.Stream output) { }
        public BinaryWriter(System.IO.Stream output, System.Text.Encoding encoding) { }
        public BinaryWriter(System.IO.Stream output, System.Text.Encoding encoding, bool leaveOpen) { }
        public virtual System.IO.Stream BaseStream { get { throw null; } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public virtual void Flush() { }
        public virtual long Seek(int offset, System.IO.SeekOrigin origin) { throw null; }
        public virtual void Write(bool value) { }
        public virtual void Write(byte value) { }
        public virtual void Write(byte[] buffer) { }
        public virtual void Write(byte[] buffer, int index, int count) { }
        public virtual void Write(char ch) { }
        public virtual void Write(char[] chars) { }
        public virtual void Write(char[] chars, int index, int count) { }
        public virtual void Write(decimal value) { }
        public virtual void Write(double value) { }
        public virtual void Write(short value) { }
        public virtual void Write(int value) { }
        public virtual void Write(long value) { }
        public virtual void Write(System.ReadOnlySpan<byte> buffer) { }
        public virtual void Write(System.ReadOnlySpan<char> chars) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(sbyte value) { }
        public virtual void Write(float value) { }
        public virtual void Write(string value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ulong value) { }
        protected void Write7BitEncodedInt(int value) { }
    }
    public sealed partial class BufferedStream : System.IO.Stream
    {
        public BufferedStream(System.IO.Stream stream) { }
        public BufferedStream(System.IO.Stream stream, int bufferSize) { }
        public int BufferSize { get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public System.IO.Stream UnderlyingStream { get { throw null; } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override void CopyTo(System.IO.Stream destination, int bufferSize) { }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] array, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> destination) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] array, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> buffer) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
    }
    public partial class EndOfStreamException : System.IO.IOException
    {
        public EndOfStreamException() { }
        protected EndOfStreamException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public EndOfStreamException(string message) { }
        public EndOfStreamException(string message, System.Exception innerException) { }
    }
    public sealed partial class InvalidDataException : System.SystemException
    {
        public InvalidDataException() { }
        public InvalidDataException(string message) { }
        public InvalidDataException(string message, System.Exception innerException) { }
    }
    public partial class MemoryStream : System.IO.Stream
    {
        public MemoryStream() { }
        public MemoryStream(byte[] buffer) { }
        public MemoryStream(byte[] buffer, bool writable) { }
        public MemoryStream(byte[] buffer, int index, int count) { }
        public MemoryStream(byte[] buffer, int index, int count, bool writable) { }
        public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) { }
        public MemoryStream(int capacity) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public virtual int Capacity { get { throw null; } set { } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override void CopyTo(System.IO.Stream destination, int bufferSize) { }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual byte[] GetBuffer() { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override int Read(System.Span<byte> destination) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin loc) { throw null; }
        public override void SetLength(long value) { }
        public virtual byte[] ToArray() { throw null; }
        public virtual bool TryGetBuffer(out System.ArraySegment<byte> buffer) { throw null; }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> source) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteByte(byte value) { }
        public virtual void WriteTo(System.IO.Stream stream) { }
    }
    public static partial class Path
    {
        public static readonly char AltDirectorySeparatorChar;
        public static readonly char DirectorySeparatorChar;
        [System.ObsoleteAttribute("Please use GetInvalidPathChars or GetInvalidFileNameChars instead.")]
        public static readonly char[] InvalidPathChars;
        public static readonly char PathSeparator;
        public static readonly char VolumeSeparatorChar;
        public static string ChangeExtension(string path, string extension) { throw null; }
        public static string Combine(string path1, string path2) { throw null; }
        public static string Combine(string path1, string path2, string path3) { throw null; }
        public static string Combine(string path1, string path2, string path3, string path4) { throw null; }
        public static string Combine(params string[] paths) { throw null; }
        public static bool EndsInDirectorySeparator(System.ReadOnlySpan<char> path) { throw null; }
        public static bool EndsInDirectorySeparator(string path) { throw null; }
        public static System.ReadOnlySpan<char> GetDirectoryName(System.ReadOnlySpan<char> path) { throw null; }
        public static string GetDirectoryName(string path) { throw null; }
        public static System.ReadOnlySpan<char> GetExtension(System.ReadOnlySpan<char> path) { throw null; }
        public static string GetExtension(string path) { throw null; }
        public static System.ReadOnlySpan<char> GetFileName(System.ReadOnlySpan<char> path) { throw null; }
        public static string GetFileName(string path) { throw null; }
        public static System.ReadOnlySpan<char> GetFileNameWithoutExtension(System.ReadOnlySpan<char> path) { throw null; }
        public static string GetFileNameWithoutExtension(string path) { throw null; }
        public static string GetFullPath(string path) { throw null; }
        public static string GetFullPath(string path, string basePath) { throw null; }
        public static char[] GetInvalidFileNameChars() { throw null; }
        public static char[] GetInvalidPathChars() { throw null; }
        public static System.ReadOnlySpan<char> GetPathRoot(System.ReadOnlySpan<char> path) { throw null; }
        public static string GetPathRoot(string path) { throw null; }
        public static string GetRandomFileName() { throw null; }
        public static string GetRelativePath(string relativeTo, string path) { throw null; }
        public static string GetTempFileName() { throw null; }
        public static string GetTempPath() { throw null; }
        public static bool HasExtension(System.ReadOnlySpan<char> path) { throw null; }
        public static bool HasExtension(string path) { throw null; }
        public static bool IsPathFullyQualified(System.ReadOnlySpan<char> path) { throw null; }
        public static bool IsPathFullyQualified(string path) { throw null; }
        public static bool IsPathRooted(System.ReadOnlySpan<char> path) { throw null; }
        public static bool IsPathRooted(string path) { throw null; }
        public static string Join(System.ReadOnlySpan<char> path1, System.ReadOnlySpan<char> path2) { throw null; }
        public static string Join(System.ReadOnlySpan<char> path1, System.ReadOnlySpan<char> path2, System.ReadOnlySpan<char> path3) { throw null; }
        public static string Join(System.ReadOnlySpan<char> path1, System.ReadOnlySpan<char> path2, System.ReadOnlySpan<char> path3, System.ReadOnlySpan<char> path4) { throw null; }
        public static string Join(string path1, string path2) { throw null; }
        public static string Join(string path1, string path2, string path3) { throw null; }
        public static string Join(string path1, string path2, string path3, string path4) { throw null; }
        public static string Join(params string[] paths) { throw null; }
        public static bool TryJoin(System.ReadOnlySpan<char> path1, System.ReadOnlySpan<char> path2, System.ReadOnlySpan<char> path3, System.Span<char> destination, out int charsWritten) { throw null; }
        public static bool TryJoin(System.ReadOnlySpan<char> path1, System.ReadOnlySpan<char> path2, System.Span<char> destination, out int charsWritten) { throw null; }
        public static System.ReadOnlySpan<char> TrimEndingDirectorySeparator(System.ReadOnlySpan<char> path) { throw null; }
        public static string TrimEndingDirectorySeparator(string path) { throw null; }
    }
    public partial class StreamReader : System.IO.TextReader
    {
        public static readonly new System.IO.StreamReader Null;
        public StreamReader(System.IO.Stream stream) { }
        public StreamReader(System.IO.Stream stream, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) { }
        public StreamReader(System.IO.Stream stream, System.Text.Encoding encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false) { }
        public StreamReader(string path) { }
        public StreamReader(string path, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(string path, System.Text.Encoding encoding) { }
        public StreamReader(string path, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks) { }
        public StreamReader(string path, System.Text.Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize) { }
        public virtual System.IO.Stream BaseStream { get { throw null; } }
        public virtual System.Text.Encoding CurrentEncoding { get { throw null; } }
        public bool EndOfStream { get { throw null; } }
        public override void Close() { }
        public void DiscardBufferedData() { }
        protected override void Dispose(bool disposing) { }
        public override int Peek() { throw null; }
        public override int Read() { throw null; }
        public override int Read(char[] buffer, int index, int count) { throw null; }
        public override int Read(System.Span<char> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadBlock(char[] buffer, int index, int count) { throw null; }
        public override int ReadBlock(System.Span<char> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadBlockAsync(System.Memory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override string ReadLine() { throw null; }
        public override System.Threading.Tasks.Task<string> ReadLineAsync() { throw null; }
        public override string ReadToEnd() { throw null; }
        public override System.Threading.Tasks.Task<string> ReadToEndAsync() { throw null; }
    }
    public partial class StreamWriter : System.IO.TextWriter
    {
        public static readonly new System.IO.StreamWriter Null;
        public StreamWriter(System.IO.Stream stream) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding, int bufferSize) { }
        public StreamWriter(System.IO.Stream stream, System.Text.Encoding encoding = null, int bufferSize = -1, bool leaveOpen = false) { }
        public StreamWriter(string path) { }
        public StreamWriter(string path, bool append) { }
        public StreamWriter(string path, bool append, System.Text.Encoding encoding) { }
        public StreamWriter(string path, bool append, System.Text.Encoding encoding, int bufferSize) { }
        public virtual bool AutoFlush { get { throw null; } set { } }
        public virtual System.IO.Stream BaseStream { get { throw null; } }
        public override System.Text.Encoding Encoding { get { throw null; } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync() { throw null; }
        public override void Write(char value) { }
        public override void Write(char[] buffer) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(System.ReadOnlySpan<char> buffer) { }
        public override void Write(string value) { }
        public override void Write(string format, object arg0) { }
        public override void Write(string format, object arg0, object arg1) { }
        public override void Write(string format, object arg0, object arg1, object arg2) { }
        public override void Write(string format, params object[] arg) { }
        public override System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
        public override void WriteLine(System.ReadOnlySpan<char> buffer) { }
        public override void WriteLine(string value) { }
        public override void WriteLine(string format, object arg0) { }
        public override void WriteLine(string format, object arg0, object arg1) { }
        public override void WriteLine(string format, object arg0, object arg1, object arg2) { }
        public override void WriteLine(string format, params object[] arg) { }
        public override System.Threading.Tasks.Task WriteLineAsync() { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(System.ReadOnlyMemory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(string value) { throw null; }
    }
    public partial class StringReader : System.IO.TextReader
    {
        public StringReader(string s) { }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        public override int Peek() { throw null; }
        public override int Read() { throw null; }
        public override int Read(char[] buffer, int index, int count) { throw null; }
        public override int Read(System.Span<char> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override int ReadBlock(System.Span<char> buffer) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadBlockAsync(System.Memory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override string ReadLine() { throw null; }
        public override System.Threading.Tasks.Task<string> ReadLineAsync() { throw null; }
        public override string ReadToEnd() { throw null; }
        public override System.Threading.Tasks.Task<string> ReadToEndAsync() { throw null; }
    }
    public partial class StringWriter : System.IO.TextWriter
    {
        public StringWriter() { }
        public StringWriter(System.IFormatProvider formatProvider) { }
        public StringWriter(System.Text.StringBuilder sb) { }
        public StringWriter(System.Text.StringBuilder sb, System.IFormatProvider formatProvider) { }
        public override System.Text.Encoding Encoding { get { throw null; } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.Task FlushAsync() { throw null; }
        public virtual System.Text.StringBuilder GetStringBuilder() { throw null; }
        public override string ToString() { throw null; }
        public override void Write(char value) { }
        public override void Write(char[] buffer, int index, int count) { }
        public override void Write(System.ReadOnlySpan<char> buffer) { }
        public override void Write(string value) { }
        public override void Write(System.Text.StringBuilder value) { }
        public override System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
        public override System.Threading.Tasks.Task WriteAsync(System.Text.StringBuilder value, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override void WriteLine(System.ReadOnlySpan<char> buffer) { }
        public override void WriteLine(System.Text.StringBuilder value) { }
        public override System.Threading.Tasks.Task WriteLineAsync(char value) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(System.ReadOnlyMemory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(string value) { throw null; }
        public override System.Threading.Tasks.Task WriteLineAsync(System.Text.StringBuilder value, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public abstract partial class TextReader : System.MarshalByRefObject, System.IDisposable
    {
        public static readonly System.IO.TextReader Null;
        protected TextReader() { }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual int Peek() { throw null; }
        public virtual int Read() { throw null; }
        public virtual int Read(char[] buffer, int index, int count) { throw null; }
        public virtual int Read(System.Span<char> buffer) { throw null; }
        public virtual System.Threading.Tasks.Task<int> ReadAsync(char[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual int ReadBlock(char[] buffer, int index, int count) { throw null; }
        public virtual int ReadBlock(System.Span<char> buffer) { throw null; }
        public virtual System.Threading.Tasks.Task<int> ReadBlockAsync(char[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.ValueTask<int> ReadBlockAsync(System.Memory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual string ReadLine() { throw null; }
        public virtual System.Threading.Tasks.Task<string> ReadLineAsync() { throw null; }
        public virtual string ReadToEnd() { throw null; }
        public virtual System.Threading.Tasks.Task<string> ReadToEndAsync() { throw null; }
        public static System.IO.TextReader Synchronized(System.IO.TextReader reader) { throw null; }
    }
    public abstract partial class TextWriter : System.MarshalByRefObject, System.IAsyncDisposable, System.IDisposable
    {
        protected char[] CoreNewLine;
        public static readonly System.IO.TextWriter Null;
        protected TextWriter() { }
        protected TextWriter(System.IFormatProvider formatProvider) { }
        public abstract System.Text.Encoding Encoding { get; }
        public virtual System.IFormatProvider FormatProvider { get { throw null; } }
        public virtual string NewLine { get { throw null; } set { } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public virtual void Flush() { }
        public virtual System.Threading.Tasks.Task FlushAsync() { throw null; }
        public static System.IO.TextWriter Synchronized(System.IO.TextWriter writer) { throw null; }
        public virtual void Write(bool value) { }
        public virtual void Write(char value) { }
        public virtual void Write(char[] buffer) { }
        public virtual void Write(char[] buffer, int index, int count) { }
        public virtual void Write(decimal value) { }
        public virtual void Write(double value) { }
        public virtual void Write(int value) { }
        public virtual void Write(long value) { }
        public virtual void Write(object value) { }
        public virtual void Write(System.ReadOnlySpan<char> buffer) { }
        public virtual void Write(float value) { }
        public virtual void Write(string value) { }
        public virtual void Write(string format, object arg0) { }
        public virtual void Write(string format, object arg0, object arg1) { }
        public virtual void Write(string format, object arg0, object arg1, object arg2) { }
        public virtual void Write(string format, params object[] arg) { }
        public virtual void Write(System.Text.StringBuilder value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void Write(ulong value) { }
        public virtual System.Threading.Tasks.Task WriteAsync(char value) { throw null; }
        public System.Threading.Tasks.Task WriteAsync(char[] buffer) { throw null; }
        public virtual System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task WriteAsync(string value) { throw null; }
        public virtual System.Threading.Tasks.Task WriteAsync(System.Text.StringBuilder value, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual void WriteLine() { }
        public virtual void WriteLine(bool value) { }
        public virtual void WriteLine(char value) { }
        public virtual void WriteLine(char[] buffer) { }
        public virtual void WriteLine(char[] buffer, int index, int count) { }
        public virtual void WriteLine(decimal value) { }
        public virtual void WriteLine(double value) { }
        public virtual void WriteLine(int value) { }
        public virtual void WriteLine(long value) { }
        public virtual void WriteLine(object value) { }
        public virtual void WriteLine(System.ReadOnlySpan<char> buffer) { }
        public virtual void WriteLine(float value) { }
        public virtual void WriteLine(string value) { }
        public virtual void WriteLine(string format, object arg0) { }
        public virtual void WriteLine(string format, object arg0, object arg1) { }
        public virtual void WriteLine(string format, object arg0, object arg1, object arg2) { }
        public virtual void WriteLine(string format, params object[] arg) { }
        public virtual void WriteLine(System.Text.StringBuilder value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void WriteLine(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public virtual void WriteLine(ulong value) { }
        public virtual System.Threading.Tasks.Task WriteLineAsync() { throw null; }
        public virtual System.Threading.Tasks.Task WriteLineAsync(char value) { throw null; }
        public System.Threading.Tasks.Task WriteLineAsync(char[] buffer) { throw null; }
        public virtual System.Threading.Tasks.Task WriteLineAsync(char[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.Task WriteLineAsync(System.ReadOnlyMemory<char> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task WriteLineAsync(string value) { throw null; }
        public virtual System.Threading.Tasks.Task WriteLineAsync(System.Text.StringBuilder value, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
namespace System.Net
{
    public static partial class WebUtility
    {
        public static string HtmlDecode(string value) { throw null; }
        public static void HtmlDecode(string value, System.IO.TextWriter output) { }
        public static string HtmlEncode(string value) { throw null; }
        public static void HtmlEncode(string value, System.IO.TextWriter output) { }
        public static string UrlDecode(string encodedValue) { throw null; }
        public static byte[] UrlDecodeToBytes(byte[] encodedValue, int offset, int count) { throw null; }
        public static string UrlEncode(string value) { throw null; }
        public static byte[] UrlEncodeToBytes(byte[] value, int offset, int count) { throw null; }
    }
}
namespace System.Numerics
{
    public static partial class BitOperations
    {
        [System.CLSCompliantAttribute(false)]
        public static int LeadingZeroCount(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int LeadingZeroCount(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int Log2(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int Log2(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int PopCount(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int PopCount(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint RotateLeft(uint value, int offset) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong RotateLeft(ulong value, int offset) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint RotateRight(uint value, int offset) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong RotateRight(ulong value, int offset) { throw null; }
        public static int TrailingZeroCount(int value) { throw null; }
        public static int TrailingZeroCount(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int TrailingZeroCount(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int TrailingZeroCount(ulong value) { throw null; }
    }
}
namespace System.Reflection
{
    public partial class AssemblyNameProxy : System.MarshalByRefObject
    {
        public AssemblyNameProxy() { }
        public System.Reflection.AssemblyName GetAssemblyName(string assemblyFile) { throw null; }
    }
}
namespace System.Runtime
{
    public static partial class ProfileOptimization
    {
        public static void SetProfileRoot(string directoryPath) { }
        public static void StartProfile(string profile) { }
    }
}
namespace System.Runtime.CompilerServices
{
    public sealed partial class SwitchExpressionException : System.InvalidOperationException
    {
        public SwitchExpressionException() { }
        public SwitchExpressionException(System.Exception innerException) { }
        public SwitchExpressionException(object unmatchedValue) { }
        public SwitchExpressionException(string message) { }
        public SwitchExpressionException(string message, System.Exception innerException) { }
        public override string Message { get { throw null; } }
        public object UnmatchedValue { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
namespace System.Runtime.Versioning
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Delegate | System.AttributeTargets.Enum | System.AttributeTargets.Event | System.AttributeTargets.Interface | System.AttributeTargets.Method | System.AttributeTargets.Module | System.AttributeTargets.Property | System.AttributeTargets.Struct, AllowMultiple=false, Inherited=false)]
    public sealed partial class ComponentGuaranteesAttribute : System.Attribute
    {
        public ComponentGuaranteesAttribute(System.Runtime.Versioning.ComponentGuaranteesOptions guarantees) { }
        public System.Runtime.Versioning.ComponentGuaranteesOptions Guarantees { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum ComponentGuaranteesOptions
    {
        None = 0,
        Exchange = 1,
        Stable = 2,
        SideBySide = 4,
    }
    public sealed partial class FrameworkName : System.IEquatable<System.Runtime.Versioning.FrameworkName>
    {
        public FrameworkName(string frameworkName) { }
        public FrameworkName(string identifier, System.Version version) { }
        public FrameworkName(string identifier, System.Version version, string profile) { }
        public string FullName { get { throw null; } }
        public string Identifier { get { throw null; } }
        public string Profile { get { throw null; } }
        public System.Version Version { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.Runtime.Versioning.FrameworkName other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Runtime.Versioning.FrameworkName left, System.Runtime.Versioning.FrameworkName right) { throw null; }
        public static bool operator !=(System.Runtime.Versioning.FrameworkName left, System.Runtime.Versioning.FrameworkName right) { throw null; }
        public override string ToString() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Property, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("RESOURCE_ANNOTATION_WORK")]
    public sealed partial class ResourceConsumptionAttribute : System.Attribute
    {
        public ResourceConsumptionAttribute(System.Runtime.Versioning.ResourceScope resourceScope) { }
        public ResourceConsumptionAttribute(System.Runtime.Versioning.ResourceScope resourceScope, System.Runtime.Versioning.ResourceScope consumptionScope) { }
        public System.Runtime.Versioning.ResourceScope ConsumptionScope { get { throw null; } }
        public System.Runtime.Versioning.ResourceScope ResourceScope { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Constructor | System.AttributeTargets.Field | System.AttributeTargets.Method | System.AttributeTargets.Property, Inherited=false)]
    [System.Diagnostics.ConditionalAttribute("RESOURCE_ANNOTATION_WORK")]
    public sealed partial class ResourceExposureAttribute : System.Attribute
    {
        public ResourceExposureAttribute(System.Runtime.Versioning.ResourceScope exposureLevel) { }
        public System.Runtime.Versioning.ResourceScope ResourceExposureLevel { get { throw null; } }
    }
    [System.FlagsAttribute]
    public enum ResourceScope
    {
        None = 0,
        Machine = 1,
        Process = 2,
        AppDomain = 4,
        Library = 8,
        Private = 16,
        Assembly = 32,
    }
    public static partial class VersioningHelper
    {
        public static string MakeVersionSafeName(string name, System.Runtime.Versioning.ResourceScope from, System.Runtime.Versioning.ResourceScope to) { throw null; }
        public static string MakeVersionSafeName(string name, System.Runtime.Versioning.ResourceScope from, System.Runtime.Versioning.ResourceScope to, System.Type type) { throw null; }
    }
}
namespace System.Security
{
    public partial interface IPermission : System.Security.ISecurityEncodable
    {
        System.Security.IPermission Copy();
        void Demand();
        System.Security.IPermission Intersect(System.Security.IPermission target);
        bool IsSubsetOf(System.Security.IPermission target);
        System.Security.IPermission Union(System.Security.IPermission target);
    }
    public partial interface ISecurityEncodable
    {
        void FromXml(System.Security.SecurityElement e);
        System.Security.SecurityElement ToXml();
    }
    public partial interface IStackWalk
    {
        void Assert();
        void Demand();
        void Deny();
        void PermitOnly();
    }
    public partial class PermissionSet : System.Collections.ICollection, System.Collections.IEnumerable, System.Runtime.Serialization.IDeserializationCallback, System.Security.ISecurityEncodable, System.Security.IStackWalk
    {
        public PermissionSet(System.Security.Permissions.PermissionState state) { }
        public PermissionSet(System.Security.PermissionSet permSet) { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual bool IsSynchronized { get { throw null; } }
        public virtual object SyncRoot { get { throw null; } }
        public System.Security.IPermission AddPermission(System.Security.IPermission perm) { throw null; }
        protected virtual System.Security.IPermission AddPermissionImpl(System.Security.IPermission perm) { throw null; }
        public void Assert() { }
        public bool ContainsNonCodeAccessPermissions() { throw null; }
        [System.ObsoleteAttribute]
        public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat) { throw null; }
        public virtual System.Security.PermissionSet Copy() { throw null; }
        public virtual void CopyTo(System.Array array, int index) { }
        public void Demand() { }
        [System.ObsoleteAttribute]
        public void Deny() { }
        public override bool Equals(object o) { throw null; }
        public virtual void FromXml(System.Security.SecurityElement et) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        protected virtual System.Collections.IEnumerator GetEnumeratorImpl() { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Security.IPermission GetPermission(System.Type permClass) { throw null; }
        protected virtual System.Security.IPermission GetPermissionImpl(System.Type permClass) { throw null; }
        public System.Security.PermissionSet Intersect(System.Security.PermissionSet other) { throw null; }
        public bool IsEmpty() { throw null; }
        public bool IsSubsetOf(System.Security.PermissionSet target) { throw null; }
        public bool IsUnrestricted() { throw null; }
        public void PermitOnly() { }
        public System.Security.IPermission RemovePermission(System.Type permClass) { throw null; }
        protected virtual System.Security.IPermission RemovePermissionImpl(System.Type permClass) { throw null; }
        public static void RevertAssert() { }
        public System.Security.IPermission SetPermission(System.Security.IPermission perm) { throw null; }
        protected virtual System.Security.IPermission SetPermissionImpl(System.Security.IPermission perm) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        public override string ToString() { throw null; }
        public virtual System.Security.SecurityElement ToXml() { throw null; }
        public System.Security.PermissionSet Union(System.Security.PermissionSet other) { throw null; }
    }
    public sealed partial class SecurityElement
    {
        public SecurityElement(string tag) { }
        public SecurityElement(string tag, string text) { }
        public System.Collections.Hashtable Attributes { get { throw null; } set { } }
        public System.Collections.ArrayList Children { get { throw null; } set { } }
        public string Tag { get { throw null; } set { } }
        public string Text { get { throw null; } set { } }
        public void AddAttribute(string name, string value) { }
        public void AddChild(System.Security.SecurityElement child) { }
        public string Attribute(string name) { throw null; }
        public System.Security.SecurityElement Copy() { throw null; }
        public bool Equal(System.Security.SecurityElement other) { throw null; }
        public static string Escape(string str) { throw null; }
        public static System.Security.SecurityElement FromString(string xml) { throw null; }
        public static bool IsValidAttributeName(string name) { throw null; }
        public static bool IsValidAttributeValue(string value) { throw null; }
        public static bool IsValidTag(string tag) { throw null; }
        public static bool IsValidText(string text) { throw null; }
        public System.Security.SecurityElement SearchForChildByTag(string tag) { throw null; }
        public string SearchForTextOfTag(string tag) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace System.Security.Permissions
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Struct, AllowMultiple=true, Inherited=false)]
    public abstract partial class CodeAccessSecurityAttribute : System.Security.Permissions.SecurityAttribute
    {
        protected CodeAccessSecurityAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
    }
    public enum PermissionState
    {
        None = 0,
        Unrestricted = 1,
    }
    public enum SecurityAction
    {
        Demand = 2,
        Assert = 3,
        [System.ObsoleteAttribute("Deny is obsolete and will be removed in a future release of the .NET Framework. See https://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        Deny = 4,
        PermitOnly = 5,
        LinkDemand = 6,
        InheritanceDemand = 7,
        [System.ObsoleteAttribute("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See https://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestMinimum = 8,
        [System.ObsoleteAttribute("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See https://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestOptional = 9,
        [System.ObsoleteAttribute("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See https://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        RequestRefuse = 10,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Struct, AllowMultiple=true, Inherited=false)]
    public abstract partial class SecurityAttribute : System.Attribute
    {
        protected SecurityAttribute(System.Security.Permissions.SecurityAction action) { }
        public System.Security.Permissions.SecurityAction Action { get { throw null; } set { } }
        public bool Unrestricted { get { throw null; } set { } }
        public abstract System.Security.IPermission CreatePermission();
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Struct, AllowMultiple=true, Inherited=false)]
    public sealed partial class SecurityPermissionAttribute : System.Security.Permissions.CodeAccessSecurityAttribute
    {
        public SecurityPermissionAttribute(System.Security.Permissions.SecurityAction action) : base (default(System.Security.Permissions.SecurityAction)) { }
        public bool Assertion { get { throw null; } set { } }
        public bool BindingRedirects { get { throw null; } set { } }
        public bool ControlAppDomain { get { throw null; } set { } }
        public bool ControlDomainPolicy { get { throw null; } set { } }
        public bool ControlEvidence { get { throw null; } set { } }
        public bool ControlPolicy { get { throw null; } set { } }
        public bool ControlPrincipal { get { throw null; } set { } }
        public bool ControlThread { get { throw null; } set { } }
        public bool Execution { get { throw null; } set { } }
        public System.Security.Permissions.SecurityPermissionFlag Flags { get { throw null; } set { } }
        public bool Infrastructure { get { throw null; } set { } }
        public bool RemotingConfiguration { get { throw null; } set { } }
        public bool SerializationFormatter { get { throw null; } set { } }
        public bool SkipVerification { get { throw null; } set { } }
        public bool UnmanagedCode { get { throw null; } set { } }
        public override System.Security.IPermission CreatePermission() { throw null; }
    }
    [System.FlagsAttribute]
    public enum SecurityPermissionFlag
    {
        NoFlags = 0,
        Assertion = 1,
        UnmanagedCode = 2,
        SkipVerification = 4,
        Execution = 8,
        ControlThread = 16,
        ControlEvidence = 32,
        ControlPolicy = 64,
        SerializationFormatter = 128,
        ControlDomainPolicy = 256,
        ControlPrincipal = 512,
        ControlAppDomain = 1024,
        RemotingConfiguration = 2048,
        Infrastructure = 4096,
        BindingRedirects = 8192,
        AllFlags = 16383,
    }
}
