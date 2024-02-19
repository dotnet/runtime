// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static unsafe partial class JavaScriptImports
    {
        [JSImport("INTERNAL.has_property")]
        public static partial bool HasProperty(JSObject self, string propertyName);
        [JSImport("INTERNAL.get_typeof_property")]
        public static partial string GetTypeOfProperty(JSObject self, string propertyName);
        [JSImport("INTERNAL.get_property")]
        public static partial bool GetPropertyAsBoolean(JSObject self, string propertyName);
        [JSImport("INTERNAL.get_property")]
        public static partial int GetPropertyAsInt32(JSObject self, string propertyName);
        [JSImport("INTERNAL.get_property")]
        public static partial double GetPropertyAsDouble(JSObject self, string propertyName);
        [JSImport("INTERNAL.get_property")]
        public static partial string GetPropertyAsString(JSObject self, string propertyName);
        [JSImport("INTERNAL.get_property")]
        public static partial JSObject GetPropertyAsJSObject(JSObject self, string propertyName);
        [JSImport("INTERNAL.get_property")]
        public static partial byte[] GetPropertyAsByteArray(JSObject self, string propertyName);

        [JSImport("INTERNAL.set_property")]
        public static partial void SetPropertyBool(JSObject self, string propertyName, bool value);
        [JSImport("INTERNAL.set_property")]
        public static partial void SetPropertyInt(JSObject self, string propertyName, int value);
        [JSImport("INTERNAL.set_property")]
        public static partial void SetPropertyDouble(JSObject self, string propertyName, double value);
        [JSImport("INTERNAL.set_property")]
        public static partial void SetPropertyString(JSObject self, string propertyName, string value);
        [JSImport("INTERNAL.set_property")]
        public static partial void SetPropertyJSObject(JSObject self, string propertyName, JSObject value);
        [JSImport("INTERNAL.set_property")]
        public static partial void SetPropertyBytes(JSObject self, string propertyName, byte[] value);

        [JSImport("INTERNAL.get_global_this")]
        public static partial JSObject GetGlobalThis();
        [JSImport("INTERNAL.get_dotnet_instance")]
        public static partial JSObject GetDotnetInstance();
        [JSImport("INTERNAL.dynamic_import")]
        // TODO: the continuation should be running on deputy or TP in MT
        public static partial Task<JSObject> DynamicImport(string moduleName, string moduleUrl);

        [JSImport("INTERNAL.mono_wasm_bind_cs_function")]
        public static partial void BindCSFunction(IntPtr monoMethod, string assemblyName, string namespaceName, string shortClassName, string methodName, int signatureHash, IntPtr signature);

#if FEATURE_WASM_MANAGED_THREADS
        [JSImport("INTERNAL.thread_available")]
        // TODO: the continuation should be running on deputy or TP in MT
        public static partial Task ThreadAvailable();
#endif

#if DEBUG
        [JSImport("globalThis.console.log")]
        [return: JSMarshalAs<JSType.OneWay>]
        public static partial void Log([JSMarshalAs<JSType.String>] string message);
#endif
    }
}
