// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static unsafe partial class JavaScriptImports
    {
#if MONO
        private const string TargetPrefix = "INTERNAL.";
#else
        private const string TargetPrefix = "INTERNAL.TARGET.";
#endif
        [JSImport(TargetPrefix + "hasProperty")]
        public static partial bool HasProperty(JSObject self, string propertyName);
        [JSImport(TargetPrefix + "getTypeOfProperty")]
        public static partial string GetTypeOfProperty(JSObject self, string propertyName);
        [JSImport(TargetPrefix + "getProperty")]
        public static partial bool GetPropertyAsBoolean(JSObject self, string propertyName);
        [JSImport(TargetPrefix + "getProperty")]
        public static partial int GetPropertyAsInt32(JSObject self, string propertyName);
        [JSImport(TargetPrefix + "getProperty")]
        public static partial double GetPropertyAsDouble(JSObject self, string propertyName);
        [JSImport(TargetPrefix + "getProperty")]
        public static partial string GetPropertyAsString(JSObject self, string propertyName);
        [JSImport(TargetPrefix + "getProperty")]
        public static partial JSObject GetPropertyAsJSObject(JSObject self, string propertyName);
        [JSImport(TargetPrefix + "getProperty")]
        public static partial byte[] GetPropertyAsByteArray(JSObject self, string propertyName);

        [JSImport(TargetPrefix + "setProperty")]
        public static partial void SetPropertyBool(JSObject self, string propertyName, bool value);
        [JSImport(TargetPrefix + "setProperty")]
        public static partial void SetPropertyInt(JSObject self, string propertyName, int value);
        [JSImport(TargetPrefix + "setProperty")]
        public static partial void SetPropertyDouble(JSObject self, string propertyName, double value);
        [JSImport(TargetPrefix + "setProperty")]
        public static partial void SetPropertyString(JSObject self, string propertyName, string value);
        [JSImport(TargetPrefix + "setProperty")]
        public static partial void SetPropertyJSObject(JSObject self, string propertyName, JSObject value);
        [JSImport(TargetPrefix + "setProperty")]
        public static partial void SetPropertyBytes(JSObject self, string propertyName, byte[] value);

        [JSImport(TargetPrefix + "getGlobalThis")]
        public static partial JSObject GetGlobalThis();
        [JSImport(TargetPrefix + "getDotnetInstance")]
        public static partial JSObject GetDotnetInstance();
        [JSImport(TargetPrefix + "dynamicImport")]
        public static partial Task<JSObject> DynamicImport(string moduleName, string moduleUrl);

        [JSImport(TargetPrefix + "bindCsFunction")]
        public static partial void BindCSFunction(IntPtr methodHandle, string assemblyName, string namespaceName, string shortClassName, string methodName, int signatureHash, IntPtr signature);

#if DEBUG
        [JSImport("globalThis.console.log")]
        [return: JSMarshalAs<JSType.DiscardNoWait>] // this means that the message will arrive out of order, especially across threads.
        public static partial void Log([JSMarshalAs<JSType.String>] string message);
#endif
    }
}
