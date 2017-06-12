// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

#if uap
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
#endif // uap

namespace System.Diagnostics
{
    /// <summary>Base class used for all tests that need to spawn a remote process.</summary>
    public abstract class RemoteExecutorTestBase : FileCleanupTestBase
    {
        /// <summary>The name of the test console app.</summary>
        protected static readonly string TestConsoleApp = "RemoteExecutorConsoleApp.exe";
        /// <summary>The name, without an extension, of the host used to host the test console app.</summary>
        private static readonly string HostRunnerExecutableName = IsFullFramework ? "xunit.console" : IsNetNative ? "xunit.console.netcore" : "dotnet";
        /// <summary>The name, with an extension, of the host host used to host the test console app.</summary>
        protected static readonly string HostRunnerName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? HostRunnerExecutableName + ".exe" : HostRunnerExecutableName;
        /// <summary>The absolute path to the host runner executable.</summary>
        protected static string HostRunner => Process.GetCurrentProcess().MainModule.FileName;

        /// <summary>A timeout (milliseconds) after which a wait on a remote operation should be considered a failure.</summary>
        public const int FailWaitTimeoutMilliseconds = 60 * 1000;
        /// <summary>The exit code returned when the test process exits successfully.</summary>
        internal const int SuccessExitCode = 42;

        /// <summary>Determines if we're running on the .NET Framework (rather than .NET Core).</summary>
        internal static bool IsFullFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);

        internal static bool IsNetNative => RuntimeInformation.FrameworkDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase);

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<int> method, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<Task<int>> method,
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, int> method, 
            string arg, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, int> method, 
            string arg1, string arg2, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg1, arg2 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, string, int> method, 
            string arg1, string arg2, string arg3, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg1, arg2, arg3 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, string, string, int> method, 
            string arg1, string arg2, string arg3, string arg4, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="arg5">The fifth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvoke(
            Func<string, string, string, string, string, int> method, 
            string arg1, string arg2, string arg3, string arg4, string arg5, 
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4, arg5 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        internal static RemoteInvokeHandle RemoteInvokeRaw(Delegate method, string unparsedArg,
            RemoteInvokeOptions options = null)
        {
            return RemoteInvoke(GetMethodInfo(method), new[] { unparsedArg }, options);
        }

#if uap
        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="start">true if this function should Start the Process; false if that responsibility is left up to the caller.</param>
        /// <param name="psi">The ProcessStartInfo to use, or null for a default.</param>
        private static RemoteInvokeHandle RemoteInvoke(MethodInfo method, string[] args, RemoteInvokeOptions options)
        {
            options = options ?? new RemoteInvokeOptions();

            // Verify the specified method is and that it returns an int (the exit code),
            // and that if it accepts any arguments, they're all strings.
            Assert.True(method.ReturnType == typeof(int) || method.ReturnType == typeof(Task<int>));
            Assert.All(method.GetParameters(), pi => Assert.Equal(typeof(string), pi.ParameterType));

            // And make sure it's in this assembly.  This isn't critical, but it helps with deployment to know
            // that the method to invoke is available because we're already running in this assembly.
            Type t = method.DeclaringType;
            Assembly a = t.GetTypeInfo().Assembly;
            Assert.Equal(typeof(RemoteExecutorTestBase).GetTypeInfo().Assembly, a);

            using (AppServiceConnection remoteExecutionService = new AppServiceConnection())
            {
                // Here, we use the app service name defined in the app service provider's Package.appxmanifest file in the <Extension> section.
                remoteExecutionService.AppServiceName = "com.microsoft.corefxuaptests";
                remoteExecutionService.PackageFamilyName = Package.Current.Id.FamilyName;

                AppServiceConnectionStatus status = remoteExecutionService.OpenAsync().GetAwaiter().GetResult();
                if (status != AppServiceConnectionStatus.Success)
                {
                    throw new IOException($"RemoteInvoke cannot open the remote service. Open Service Status: {status}");
                }

                ValueSet message = new ValueSet();

                message.Add("AssemblyName", a.FullName);
                message.Add("TypeName", t.FullName);
                message.Add("MethodName", method.Name);

                int i = 0;
                foreach (string arg in args)
                {
                    message.Add("Arg" + i, arg);
                    i++;
                }

                AppServiceResponse response = remoteExecutionService.SendMessageAsync(message).GetAwaiter().GetResult();

                Assert.True(response.Status == AppServiceResponseStatus.Success, $"response.Status = {response.Status}");
                int res = (int) response.Message["Results"];
                Assert.True(res == SuccessExitCode, (string) response.Message["Log"] + Environment.NewLine + $"Returned Error code: {res}");
            }

            // RemoteInvokeHandle is not really needed in the UAP scenario but we use it just to have consistent interface as non UAP
            return new RemoteInvokeHandle(null, options);
        }
#else

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="start">true if this function should Start the Process; false if that responsibility is left up to the caller.</param>
        /// <param name="psi">The ProcessStartInfo to use, or null for a default.</param>
        private static RemoteInvokeHandle RemoteInvoke(MethodInfo method, string[] args, RemoteInvokeOptions options)
        {
            options = options ?? new RemoteInvokeOptions();

            // Verify the specified method is and that it returns an int (the exit code),
            // and that if it accepts any arguments, they're all strings.
            Assert.True(method.ReturnType == typeof(int) || method.ReturnType == typeof(Task<int>));
            Assert.All(method.GetParameters(), pi => Assert.Equal(typeof(string), pi.ParameterType));

            // And make sure it's in this assembly.  This isn't critical, but it helps with deployment to know
            // that the method to invoke is available because we're already running in this assembly.
            Type t = method.DeclaringType;
            Assembly a = t.GetTypeInfo().Assembly;
            Assert.Equal(typeof(RemoteExecutorTestBase).GetTypeInfo().Assembly, a);

            // Start the other process and return a wrapper for it to handle its lifetime and exit checking.
            var psi = options.StartInfo;
            psi.UseShellExecute = false;

            if (!options.EnableProfiling)
            {
                // Profilers / code coverage tools doing coverage of the test process set environment
                // variables to tell the targeted process what profiler to load.  We don't want the child process 
                // to be profiled / have code coverage, so we remove these environment variables for that process 
                // before it's started.
                psi.Environment.Remove("Cor_Profiler");
                psi.Environment.Remove("Cor_Enable_Profiling");
                psi.Environment.Remove("CoreClr_Profiler");
                psi.Environment.Remove("CoreClr_Enable_Profiling");
            }

            // If we need the host (if it exists), use it, otherwise target the console app directly.
            string testConsoleAppArgs = "\"" + a.FullName + "\" " + t.FullName + " " + method.Name + " " + string.Join(" ", args);

            if (!File.Exists(TestConsoleApp))
                throw new IOException("RemoteExecutorConsoleApp test app isn't present in the test runtime directory.");

            if (IsFullFramework || IsNetNative)
            {
                psi.FileName = TestConsoleApp;
                psi.Arguments = testConsoleAppArgs;
            }
            else
            {
                psi.FileName = HostRunner;
                psi.Arguments = TestConsoleApp + " " + testConsoleAppArgs;
            }

            // Return the handle to the process, which may or not be started
            return new RemoteInvokeHandle(options.Start ?
                Process.Start(psi) :
                new Process() { StartInfo = psi }, options);
        }
#endif

        private static MethodInfo GetMethodInfo(Delegate d)
        {
            // RemoteInvoke doesn't support marshaling state on classes associated with
            // the delegate supplied (often a display class of a lambda).  If such fields
            // are used, odd errors result, e.g. NullReferenceExceptions during the remote
            // execution.  Try to ward off the common cases by proactively failing early
            // if it looks like such fields are needed.
            if (d.Target != null)
            {
                // The only fields on the type should be compiler-defined (any fields of the compiler's own
                // making generally include '<' and '>', as those are invalid in C# source).  Note that this logic
                // may need to be revised in the future as the compiler changes, as this relies on the specifics of
                // actually how the compiler handles lifted fields for lambdas.
                Type targetType = d.Target.GetType();
                Assert.All(
                    targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                    fi => Assert.True(fi.Name.IndexOf('<') != -1, $"Field marshaling is not supported by {nameof(RemoteInvoke)}: {fi.Name}"));
            }

            return d.GetMethodInfo();
        }

        /// <summary>A cleanup handle to the Process created for the remote invocation.</summary>
        internal sealed class RemoteInvokeHandle : IDisposable
        {
            public RemoteInvokeHandle(Process process, RemoteInvokeOptions options)
            {
                Process = process;
                Options = options;
            }

            public Process Process { get; private set; }
            public RemoteInvokeOptions Options { get; private set; }

            public void Dispose()
            {
                if (Process != null)
                {
                    // A bit unorthodox to do throwing operations in a Dispose, but by doing it here we avoid
                    // needing to do this in every derived test and keep each test much simpler.
                    try
                    {
                        Assert.True(Process.WaitForExit(Options.TimeOut),
                            $"Timed out after {Options.TimeOut}ms waiting for remote process {Process.Id}");

                        if (Options.CheckExitCode)
                        {
                            Assert.Equal(SuccessExitCode, Process.ExitCode);
                        }
                    }
                    finally
                    {
                        // Cleanup
                        try { Process.Kill(); }
                        catch { } // ignore all cleanup errors

                        Process.Dispose();
                        Process = null;
                    }
                }
            }
        }
    }

    /// <summary>Options used with RemoteInvoke.</summary>
    internal sealed class RemoteInvokeOptions
    {
        public bool Start { get; set; } = true;
        public ProcessStartInfo StartInfo { get; set; } = new ProcessStartInfo();
        public bool EnableProfiling { get; set; } = true;
        public bool CheckExitCode {get; set; } = true;

        public int TimeOut {get; set; } = RemoteExecutorTestBase.FailWaitTimeoutMilliseconds;
    }
}
