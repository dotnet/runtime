// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    internal static class HostContextResultExtensions
    {
        public static AndConstraint<CommandResultAssertions> ExecuteAssemblyMock(this CommandResultAssertions assertion, string appPath, string[] appArgs)
        {
            var constraint = assertion.HaveStdOutContaining("mock coreclr_initialize() called")
                .And.HaveStdOutContaining("mock coreclr_execute_assembly() called")
                .And.HaveStdOutContaining($"mock managedAssemblyPath:{appPath}")
                .And.HaveStdOutContaining($"mock argc:{appArgs.Length}")
                .And.HaveStdOutContaining("mock coreclr_shutdown_2() called");

            for (int i = 0; i < appArgs.Length; ++i)
            {
                constraint = constraint.And.HaveStdOutContaining($"mock argv[{i}] = {appArgs[i]}");
            }

            return constraint;
        }

        public static AndConstraint<CommandResultAssertions> CreateDelegateMock(this CommandResultAssertions assertion)
        {
            return assertion.HaveStdOutContaining("mock coreclr_initialize() called")
                .And.HaveStdOutContaining("mock coreclr_create_delegate() called");
        }

        public static AndConstraint<CommandResultAssertions> CreateDelegateMock_COM(this CommandResultAssertions assertion)
        {
            return assertion.CreateDelegateMock()
                .And.HaveStdOutContaining("mock entryPointAssemblyName:System.Private.CoreLib")
                .And.HaveStdOutContaining("mock entryPointTypeName:Internal.Runtime.InteropServices.ComActivator")
                .And.HaveStdOutContaining("mock entryPointMethodName:GetClassFactoryForTypeInternal");
        }

        public static AndConstraint<CommandResultAssertions> CreateDelegateMock_InMemoryAssembly(this CommandResultAssertions assertion)
        {
            return assertion.CreateDelegateMock()
                .And.HaveStdOutContaining("mock entryPointAssemblyName:System.Private.CoreLib")
                .And.HaveStdOutContaining("mock entryPointTypeName:Internal.Runtime.InteropServices.InMemoryAssemblyLoader")
                .And.HaveStdOutContaining("mock entryPointMethodName:LoadInMemoryAssembly");
        }

        public static AndConstraint<CommandResultAssertions> HavePropertyMock(this CommandResultAssertions assertion, string name, string value)
        {
            return assertion.HaveStdOutContaining($"mock property[{name}] = {value}");
        }

        public static AndConstraint<CommandResultAssertions> NotHavePropertyMock(this CommandResultAssertions assertion, string name)
        {
            return assertion.NotHaveStdOutContaining($"mock property[{name}]");
        }

        public static AndConstraint<CommandResultAssertions> InitializeContextForApp(this CommandResultAssertions assertion, string path)
        {
            return assertion.HaveStdErrContaining($"Initialized context for app: {path}");
        }

        public static AndConstraint<CommandResultAssertions> InitializeContextForConfig(this CommandResultAssertions assertion, string path)
        {
            return assertion.HaveStdErrContaining($"Initialized context for config: {path}");
        }

        public static AndConstraint<CommandResultAssertions> InitializeSecondaryContext(this CommandResultAssertions assertion, string path, int statusCode)
        {
            return assertion.HaveStdErrContaining($"Initialized secondary context for config: {path}")
                .And.HaveStdOutContaining($"hostfxr_initialize_for_runtime_config succeeded: 0x{statusCode.ToString("x")}");
        }

        public static AndConstraint<CommandResultAssertions> FailToInitializeContextForConfig(this CommandResultAssertions assertion, int errorCode)
        {
            return assertion.HaveStdOutContaining($"hostfxr_initialize_for_runtime_config failed: 0x{errorCode.ToString("x")}");
        }

        public static AndConstraint<CommandResultAssertions> GetRuntimePropertyValue(this CommandResultAssertions assertion, string prefix, string name, string value)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_property_value succeeded for property: {name}={value}");
        }

        public static AndConstraint<CommandResultAssertions> FailToGetRuntimePropertyValue(this CommandResultAssertions assertion, string prefix, string name, int errorCode)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_property_value failed for property: {name} - 0x{errorCode.ToString("x")}");
        }

        public static AndConstraint<CommandResultAssertions> SetRuntimePropertyValue(this CommandResultAssertions assertion, string prefix, string name)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_set_runtime_property_value succeeded for property: {name}");
        }

        public static AndConstraint<CommandResultAssertions> FailToSetRuntimePropertyValue(this CommandResultAssertions assertion, string prefix, string name, int errorCode)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_set_runtime_property_value failed for property: {name} - 0x{errorCode.ToString("x")}");
        }

        public static AndConstraint<CommandResultAssertions> GetRuntimePropertiesIncludes(this CommandResultAssertions assertion, string prefix, string name, string value)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties succeeded")
                .And.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties: {name}={value}");
        }

        public static AndConstraint<CommandResultAssertions> GetRuntimePropertiesExcludes(this CommandResultAssertions assertion, string prefix, string name)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties succeeded")
                .And.NotHaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties: {name}");
        }

        public static AndConstraint<CommandResultAssertions> FailToGetRuntimeProperties(this CommandResultAssertions assertion, string prefix, int errorCode)
        {
            return assertion.HaveStdOutContaining($"{prefix}hostfxr_get_runtime_properties failed - 0x{errorCode.ToString("x")}");
        }
    }
}
