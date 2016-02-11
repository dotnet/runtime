// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Runtime.InteropServices.WindowsRuntime
{
    [global::System.AttributeUsageAttribute((global::System.AttributeTargets)(1028), AllowMultiple = false, Inherited = false)]
    public sealed partial class DefaultInterfaceAttribute : global::System.Attribute
    {
        public DefaultInterfaceAttribute(global::System.Type defaultInterface) { }
        public global::System.Type DefaultInterface { get { return default(global::System.Type); } }
    }
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct EventRegistrationToken
    {
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken left, global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken right) { return default(bool); }
        public static bool operator !=(global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken left, global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken right) { return default(bool); }
    }
    public sealed partial class EventRegistrationTokenTable<T> where T : class
    {
        public EventRegistrationTokenTable() { }
        public T InvocationList { get { return default(T); } set { } }
        public global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken AddEventHandler(T handler) { return default(global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken); }
        public static global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T> refEventTable) { return default(global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T>); }
        public void RemoveEventHandler(T handler) { }
        public void RemoveEventHandler(global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken token) { }
    }
    public partial interface IActivationFactory
    {
        object ActivateInstance();
    }
    [global::System.AttributeUsageAttribute((global::System.AttributeTargets)(1028), Inherited = false, AllowMultiple = true)]
    public sealed partial class InterfaceImplementedInVersionAttribute : global::System.Attribute
    {
        public InterfaceImplementedInVersionAttribute(global::System.Type interfaceType, byte majorVersion, byte minorVersion, byte buildVersion, byte revisionVersion) { }
        public byte BuildVersion { get { return default(byte); } }
        public global::System.Type InterfaceType { get { return default(global::System.Type); } }
        public byte MajorVersion { get { return default(byte); } }
        public byte MinorVersion { get { return default(byte); } }
        public byte RevisionVersion { get { return default(byte); } }
    }
    [global::System.AttributeUsageAttribute((global::System.AttributeTargets)(2048), Inherited = false, AllowMultiple = false)]
    public sealed partial class ReadOnlyArrayAttribute : global::System.Attribute
    {
        public ReadOnlyArrayAttribute() { }
    }
    [global::System.AttributeUsageAttribute((global::System.AttributeTargets)(12288), AllowMultiple = false, Inherited = false)]
    public sealed partial class ReturnValueNameAttribute : global::System.Attribute
    {
        public ReturnValueNameAttribute(string name) { }
        public string Name { get { return default(string); } }
    }
    public static partial class WindowsRuntimeMarshal
    {
        [global::System.Security.SecurityCriticalAttribute]
        public static void AddEventHandler<T>(global::System.Func<T, global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> addMethod, global::System.Action<global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod, T handler) { }
        [global::System.Security.SecurityCriticalAttribute]
        public static void FreeHString(global::System.IntPtr ptr) { }
        [global::System.Security.SecurityCriticalAttribute]
        public static global::System.Runtime.InteropServices.WindowsRuntime.IActivationFactory GetActivationFactory(global::System.Type type) { return default(global::System.Runtime.InteropServices.WindowsRuntime.IActivationFactory); }
        [global::System.Security.SecurityCriticalAttribute]
        public static string PtrToStringHString(global::System.IntPtr ptr) { return default(string); }
        [global::System.Security.SecurityCriticalAttribute]
        public static void RemoveAllEventHandlers(global::System.Action<global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod) { }
        [global::System.Security.SecurityCriticalAttribute]
        public static void RemoveEventHandler<T>(global::System.Action<global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod, T handler) { }
        [global::System.Security.SecurityCriticalAttribute]
        public static global::System.IntPtr StringToHString(string s) { return default(global::System.IntPtr); }
    }
    [global::System.AttributeUsageAttribute((global::System.AttributeTargets)(2048), Inherited = false, AllowMultiple = false)]
    public sealed partial class WriteOnlyArrayAttribute : global::System.Attribute
    {
        public WriteOnlyArrayAttribute() { }
    }
}
