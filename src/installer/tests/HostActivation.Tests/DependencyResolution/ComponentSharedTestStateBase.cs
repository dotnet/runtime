// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class ComponentSharedTestStateBase : SharedTestStateBase
    {
        private const string resolve_component_dependencies = "resolve_component_dependencies";
        private const string run_app_and_resolve = "run_app_and_resolve";
        private const string run_app_and_resolve_multithreaded = "run_app_and_resolve_multithreaded";

        public DotNetCli DotNetWithNetCoreApp { get; }

        public TestApp FrameworkReferenceApp { get; }

        public string NativeHostPath { get => _nativeHostingState.NativeHostPath; }

        private readonly NativeHosting.SharedTestStateBase _nativeHostingState;

        public ComponentSharedTestStateBase()
        {
            var dotNetBuilder = DotNet("WithNetCoreApp")
                .AddMicrosoftNETCoreAppFrameworkMockCoreClr("4.0.0", builder => CustomizeDotNetWithNetCoreAppMicrosoftNETCoreApp(builder));
            CustomizeDotNetWithNetCoreApp(dotNetBuilder);
            DotNetWithNetCoreApp = dotNetBuilder.Build();

            FrameworkReferenceApp = CreateTestFrameworkReferenceApp();

            _nativeHostingState = new NativeHosting.SharedTestStateBase();
        }

        protected virtual TestApp CreateTestFrameworkReferenceApp() => CreateFrameworkReferenceApp(Constants.MicrosoftNETCoreApp, "4.0.0");

        protected virtual void CustomizeDotNetWithNetCoreAppMicrosoftNETCoreApp(NetCoreAppBuilder builder)
        {
        }

        protected virtual void CustomizeDotNetWithNetCoreApp(DotNetBuilder builder)
        {
        }

        public CommandResult RunComponentResolutionTest(TestApp component, Action<Command> commandCustomizer = null, [CallerMemberName] string caller = "")
        {
            return RunComponentResolutionTest(component.AppDll, FrameworkReferenceApp, DotNetWithNetCoreApp.GreatestVersionHostFxrPath, commandCustomizer, caller);
        }

        public CommandResult RunComponentResolutionTest(string componentPath, TestApp hostApp, string hostFxrFolder, Action<Command> commandCustomizer = null, [CallerMemberName] string caller = "")
        {
            string[] args =
            {
                resolve_component_dependencies,
                run_app_and_resolve,
                Path.Combine(hostFxrFolder, Binaries.HostFxr.FileName),
                hostApp.AppDll,
                componentPath
            };

            Command command = Command.Create(NativeHostPath, args)
                .EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false);
            commandCustomizer?.Invoke(command);

            return command.Execute(caller)
                .StdErrAfter("corehost_resolve_component_dependencies = {");
        }

        public CommandResult RunComponentResolutionMultiThreadedTest(TestApp componentOne, TestApp componentTwo, [CallerMemberName] string caller = "")
        {
            return RunComponentResolutionMultiThreadedTest(componentOne.AppDll, componentTwo.AppDll, FrameworkReferenceApp, DotNetWithNetCoreApp.GreatestVersionHostFxrPath, caller);
        }

        public CommandResult RunComponentResolutionMultiThreadedTest(string componentOnePath, string componentTwoPath, TestApp hostApp, string hostFxrFolder, [CallerMemberName] string caller = "")
        {
            string[] args =
            {
                resolve_component_dependencies,
                run_app_and_resolve_multithreaded,
                Path.Combine(hostFxrFolder, Binaries.HostFxr.FileName),
                hostApp.AppDll,
                componentOnePath,
                componentTwoPath
            };

            return Command.Create(NativeHostPath, args)
                .EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false)
                .Execute(caller);
        }

        protected override void Dispose(bool disposing)
        {
            FrameworkReferenceApp.Dispose();
            _nativeHostingState.Dispose();
            base.Dispose(disposing);
        }
    }
}
