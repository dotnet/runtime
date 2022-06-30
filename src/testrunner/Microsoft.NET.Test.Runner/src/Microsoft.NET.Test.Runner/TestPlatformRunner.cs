// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.NET.Test.Runner.Console;
using Microsoft.NET.Test.Runner.ExtensionsManager;
using Microsoft.NET.Test.Runner.JsonRpc.Jsonite;

namespace Microsoft.NET.Test.Runner
{
    public class TestPlatformRunner
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public static int Main(string[] args)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            IConsole console = new SystemConsole();
            IExtensionsManager extensionsManager = new DefaultExtensionsManager(new DefaultInprocessExtensionsRegistration());
            ITestOrchestrator runTestsOrchestrator = new RunTestsOrchestrator(extensionsManager, console, new Jsonite_JsonRpcMessageSerializer());
            Splash(console);

            Task.WaitAll(runTestsOrchestrator.Start());
            return 0;
        }

        private static void Splash(IConsole console)
        {
            console.WriteLine(UserMessages.Banner);
            console.WriteLine($"Framework '{RuntimeInformation.FrameworkDescription}' ({RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}) '{RuntimeInformation.OSDescription}'");
        }
    }
}
