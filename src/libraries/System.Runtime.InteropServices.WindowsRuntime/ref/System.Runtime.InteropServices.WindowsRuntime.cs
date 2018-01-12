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
        public global::System.Type DefaultInterface { get { throw null; } }
    }
    public partial struct EventRegistrationToken
    {
        private int _dummy;
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken left, global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken right) { throw null; }
        public static bool operator !=(global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken left, global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken right) { throw null; }
    }
    public sealed partial class EventRegistrationTokenTable<T> where T : class
    {
        public EventRegistrationTokenTable() { }
        public T InvocationList { get { throw null; } set { } }
        public global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken AddEventHandler(T handler) { throw null; }
        public static global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T> refEventTable) { throw null; }
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
        public byte BuildVersion { get { throw null; } }
        public global::System.Type InterfaceType { get { throw null; } }
        public byte MajorVersion { get { throw null; } }
        public byte MinorVersion { get { throw null; } }
        public byte RevisionVersion { get { throw null; } }
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
        public string Name { get { throw null; } }
    }
    public static partial class WindowsRuntimeMarshal
    {
        public static void AddEventHandler<T>(global::System.Func<T, global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> addMethod, global::System.Action<global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod, T handler) { }
        public static void FreeHString(global::System.IntPtr ptr) { }
        public static global::System.Runtime.InteropServices.WindowsRuntime.IActivationFactory GetActivationFactory(global::System.Type type) { throw null; }
        public static string PtrToStringHString(global::System.IntPtr ptr) { throw null; }
        public static void RemoveAllEventHandlers(global::System.Action<global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod) { }
        public static void RemoveEventHandler<T>(global::System.Action<global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken> removeMethod, T handler) { }
        public static global::System.IntPtr StringToHString(string s) { throw null; }
    }
    [global::System.AttributeUsageAttribute((global::System.AttributeTargets)(2048), Inherited = false, AllowMultiple = false)]
    public sealed partial class WriteOnlyArrayAttribute : global::System.Attribute
    {
        public WriteOnlyArrayAttribute() { }
    }
}
