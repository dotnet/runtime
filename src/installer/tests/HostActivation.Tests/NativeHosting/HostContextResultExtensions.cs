// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    internal static class HostContextResultExtensions
    {
        public static CommandResultAssertions ExecuteSelfContained(this CommandResultAssertions assertion, bool selfContained)
        {
            return assertion.HaveStdErrContaining($"Executing as a {(selfContained ? "self-contained" : "framework-dependent")} app");
        }

        public static CommandResultAssertions ExecuteAssemblyMock(this CommandResultAssertions assertion, string appPath, string[] appArgs)
        {
            var constraint = assertion.HaveStdOutContaining("mock coreclr_initialize() called")
                .HaveStdOutContaining("mock coreclr_execute_assembly() called")
                .HaveStdOutContaining($"mock managedAssemblyPath:{appPath}")
                .HaveStdOutContaining($"mock argc:{appArgs.Length}")
                .HaveStdOutContaining("mock coreclr_shutdown_2() called");

            for (int i = 0; i < appArgs.Length; ++i)
            {
                constraint = constraint.HaveStdOutContaining($"mock argv[{i}] = {appArgs[i]}");
            }

            return constraint;
        }

        public static CommandResultAssertions CreateDelegateMock(this CommandResultAssertions assertion)
        {
            return assertion.HaveStdOutContaining("mock coreclr_initialize() called")
                .HaveStdOutContaining("mock coreclr_create_delegate() called");
        }

        public static CommandResultAssertions CreateDelegateMock_COM(this CommandResultAssertions assertion)
        {
            return assertion.CreateDelegateMock()
                .HaveStdOutContaining("mock entryPointAssemblyName:System.Private.CoreLib")
                .HaveStdOutContaining("mock entryPointTypeName:Internal.Runtime.InteropServices.ComActivator")
                .HaveStdOutContaining("mock entryPointMethodName:GetClassFactoryForTypeInternal");
        }

        public static CommandResultAssertions CreateDelegateMock_InMemoryAssembly(this CommandResultAssertions assertion)
        {
            return assertion.CreateDelegateMock()
                .HaveStdOutContaining("mock entryPointAssemblyName:System.Private.CoreLib")
                .HaveStdOutContaining("mock entryPointTypeName:Internal.Runtime.InteropServices.InMemoryAssemblyLoader")
                .HaveStdOutContaining("mock entryPointMethodName:LoadInMemoryAssembly");
        }

        public static CommandResultAssertions HavePropertyMock(this CommandResultAssertions assertion, string name, string value)
        {
            return assertion.HaveStdOutContaining($"mock property[{name}] = {value}");
        }

        public static CommandResultAssertions NotHavePropertyMock(this CommandResultAssertions assertion, string name)
        {
            return assertion.NotHaveStdOutContaining($"mock property[{name}]");
        }

        public static CommandResultAssertions InitializeContextForApp(this CommandResultAssertions assertion, string path)
        {
            return assertion.HaveStdErrContaining($"Initialized context for app: {path}");
        }

        public static CommandResultAssertions InitializeContextForConfig(this CommandResultAssertions assertion, string path)
        {
            return assertion.HaveStdErrContaining($"Initialized context for config: {path}");
        }

        public static CommandResultAssertions InitializeSecondaryContext(this CommandResultAssertions assertion, string path, int statusCode)
        {
            return assertion.HaveStdErrContaining($"Initialized secondary context for config: {path}")
                .HaveStdOutContaining($"hostfxr_initialize_for_runtime_config succeeded: 0x{statusCode.ToString("x")}");
        }

        public static CommandResultAssertions FailToInitializeContextForConfig(this CommandResultAssertions assertion, int errorCode)
        {
            return assertion.HaveStdOutContaining($"hostfxr_initialize_for_runtime_config failed: 0x{errorCode.ToString("x")}");
        }

        public static CommandResultAssertions GetRuntimePropertyValue(this CommandResultAssertions assertion, string prefix, string name, string value)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_property_value succeeded for property: {name}={value}");
        }

        public static CommandResultAssertions FailToGetRuntimePropertyValue(this CommandResultAssertions assertion, string prefix, string name, int errorCode)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_property_value failed for property: {name} - 0x{errorCode.ToString("x")}");
        }

        public static CommandResultAssertions SetRuntimePropertyValue(this CommandResultAssertions assertion, string prefix, string name)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_set_runtime_property_value succeeded for property: {name}");
        }

        public static CommandResultAssertions FailToSetRuntimePropertyValue(this CommandResultAssertions assertion, string prefix, string name, int errorCode)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_set_runtime_property_value failed for property: {name} - 0x{errorCode.ToString("x")}");
        }

        public static CommandResultAssertions GetRuntimePropertiesIncludes(this CommandResultAssertions assertion, string prefix, string name, string value)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties succeeded")
                .HaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties: {name}={value}");
        }

        public static CommandResultAssertions GetRuntimePropertiesExcludes(this CommandResultAssertions assertion, string prefix, string name)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties succeeded")
                .NotHaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties: {name}");
        }

        public static CommandResultAssertions FailToGetRuntimeProperties(this CommandResultAssertions assertion, string prefix, int errorCode)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties failed - 0x{errorCode.ToString("x")}");
        }
    }
}
