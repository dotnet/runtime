// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32
{
    public static partial class Registry
    {
        public static readonly Microsoft.Win32.RegistryKey ClassesRoot;
        public static readonly Microsoft.Win32.RegistryKey CurrentConfig;
        public static readonly Microsoft.Win32.RegistryKey CurrentUser;
        public static readonly Microsoft.Win32.RegistryKey LocalMachine;
        public static readonly Microsoft.Win32.RegistryKey PerformanceData;
        public static readonly Microsoft.Win32.RegistryKey Users;
        public static object GetValue(string keyName, string valueName, object defaultValue) { return default(object); }
        public static void SetValue(string keyName, string valueName, object value) { }
        public static void SetValue(string keyName, string valueName, object value, Microsoft.Win32.RegistryValueKind valueKind) { }
    }
    public enum RegistryHive
    {
        ClassesRoot = -2147483648,
        CurrentConfig = -2147483643,
        CurrentUser = -2147483647,
        LocalMachine = -2147483646,
        PerformanceData = -2147483644,
        Users = -2147483645,
    }
    public sealed partial class RegistryKey : System.IDisposable
    {
        internal RegistryKey() { }
        public Microsoft.Win32.SafeHandles.SafeRegistryHandle Handle {[System.Security.SecurityCriticalAttribute]get { return default(Microsoft.Win32.SafeHandles.SafeRegistryHandle); } }
        public string Name { get { return default(string); } }
        public int SubKeyCount { get { return default(int); } }
        public int ValueCount { get { return default(int); } }
        public Microsoft.Win32.RegistryView View { get { return default(Microsoft.Win32.RegistryView); } }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey) { return default(Microsoft.Win32.RegistryKey); }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey, bool writable) { return default(Microsoft.Win32.RegistryKey); }
        public Microsoft.Win32.RegistryKey CreateSubKey(string subkey, bool writable, Microsoft.Win32.RegistryOptions options) { return default(Microsoft.Win32.RegistryKey); }
        public void DeleteSubKey(string subkey) { }
        public void DeleteSubKey(string subkey, bool throwOnMissingSubKey) { }
        public void DeleteSubKeyTree(string subkey) { }
        public void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey) { }
        public void DeleteValue(string name) { }
        public void DeleteValue(string name, bool throwOnMissingValue) { }
        public void Dispose() { }
        public void Flush() { }
        [System.Security.SecurityCriticalAttribute]
        public static Microsoft.Win32.RegistryKey FromHandle(Microsoft.Win32.SafeHandles.SafeRegistryHandle handle) { return default(Microsoft.Win32.RegistryKey); }
        [System.Security.SecurityCriticalAttribute]
        public static Microsoft.Win32.RegistryKey FromHandle(Microsoft.Win32.SafeHandles.SafeRegistryHandle handle, Microsoft.Win32.RegistryView view) { return default(Microsoft.Win32.RegistryKey); }
        public string[] GetSubKeyNames() { return default(string[]); }
        public object GetValue(string name) { return default(object); }
        public object GetValue(string name, object defaultValue) { return default(object); }
        public object GetValue(string name, object defaultValue, Microsoft.Win32.RegistryValueOptions options) { return default(object); }
        public Microsoft.Win32.RegistryValueKind GetValueKind(string name) { return default(Microsoft.Win32.RegistryValueKind); }
        public string[] GetValueNames() { return default(string[]); }
        public static Microsoft.Win32.RegistryKey OpenBaseKey(Microsoft.Win32.RegistryHive hKey, Microsoft.Win32.RegistryView view) { return default(Microsoft.Win32.RegistryKey); }
        public Microsoft.Win32.RegistryKey OpenSubKey(string name) { return default(Microsoft.Win32.RegistryKey); }
        public Microsoft.Win32.RegistryKey OpenSubKey(string name, bool writable) { return default(Microsoft.Win32.RegistryKey); }
        public Microsoft.Win32.RegistryKey OpenSubKey(string name, System.Security.AccessControl.RegistryRights rights) { return default(Microsoft.Win32.RegistryKey); }
        public void SetValue(string name, object value) { }
        public void SetValue(string name, object value, Microsoft.Win32.RegistryValueKind valueKind) { }
        public override string ToString() { return default(string); }
    }
    [System.FlagsAttribute]
    public enum RegistryOptions
    {
        None = 0,
        Volatile = 1,
    }
    public enum RegistryValueKind
    {
        Binary = 3,
        DWord = 4,
        ExpandString = 2,
        MultiString = 7,
        None = -1,
        QWord = 11,
        String = 1,
        Unknown = 0,
    }
    [System.FlagsAttribute]
    public enum RegistryValueOptions
    {
        DoNotExpandEnvironmentNames = 1,
        None = 0,
    }
    public enum RegistryView
    {
        Default = 0,
        Registry32 = 512,
        Registry64 = 256,
    }
}
namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class SafeRegistryHandle : System.Runtime.InteropServices.SafeHandle
    {
        [System.Security.SecurityCriticalAttribute]
        public SafeRegistryHandle(System.IntPtr preexistingHandle, bool ownsHandle) : base(default(System.IntPtr), default(bool)) { }
        [System.Security.SecurityCriticalAttribute]
        protected override bool ReleaseHandle() { return default(bool); }
    }
}
namespace System.Security.AccessControl
{
    [System.FlagsAttribute]
    public enum RegistryRights
    {
        ChangePermissions = 262144,
        CreateLink = 32,
        CreateSubKey = 4,
        Delete = 65536,
        EnumerateSubKeys = 8,
        ExecuteKey = 131097,
        FullControl = 983103,
        Notify = 16,
        QueryValues = 1,
        ReadKey = 131097,
        ReadPermissions = 131072,
        SetValue = 2,
        TakeOwnership = 524288,
        WriteKey = 131078,
    }
}
