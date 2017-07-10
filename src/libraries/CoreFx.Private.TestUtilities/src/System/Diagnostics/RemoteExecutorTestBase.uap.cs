// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.AppService;

namespace System.Diagnostics
{
    /// <summary>Base class used for all tests that need to spawn a remote process.</summary>
    public abstract partial class RemoteExecutorTestBase : FileCleanupTestBase
    {
        protected static readonly string HostRunnerName = "xunit.runner.uap.exe";
        protected static readonly string HostRunner = "xunit.runner.uap";

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="options"><see cref="System.Diagnostics.RemoteInvokeOptions"/> The options to execute the remote process.</param>
        /// <param name="pasteArguments">Unused in UAP.</param>
        private static RemoteInvokeHandle RemoteInvoke(MethodInfo method, string[] args, RemoteInvokeOptions options, bool pasteArguments = false)
        {
            options = options ?? new RemoteInvokeOptions();

            // Verify the specified method is and that it returns an int (the exit code),
            // and that if it accepts any arguments, they're all strings.
            Assert.True(method.ReturnType == typeof(int) || method.ReturnType == typeof(Task<int>));
            Assert.All(method.GetParameters(), pi => Assert.Equal(typeof(string), pi.ParameterType));

            // Perform the remote invoke in a background thread so as not to block the caller. The background thread will
            // perform the remote invocation synchronously.
            Action waitForRemoteInvokeThread;
            Thread remoteInvokeThread = ThreadTestHelpers.CreateGuardedThread(out waitForRemoteInvokeThread, () =>
            {
                // And make sure it's in this assembly.  This isn't critical, but it helps with deployment to know
                // that the method to invoke is available because we're already running in this assembly.
                Type t = method.DeclaringType;
                Assembly a = t.GetTypeInfo().Assembly;

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
                    int res = (int)response.Message["Results"];
                    Assert.True(res == options.ExpectedExitCode, (string)response.Message["Log"] + Environment.NewLine + $"Returned Error code: {res}");
                }
            });
            remoteInvokeThread.IsBackground = true;
            remoteInvokeThread.Start();

            return new RemoteInvokeHandle(remoteInvokeThread, waitForRemoteInvokeThread, options);
        }

        /// <summary>A cleanup handle to the Process created for the remote invocation.</summary>
        public sealed class RemoteInvokeHandle : IDisposable
        {
            public RemoteInvokeHandle(Thread remoteInvokeThread, Action waitForRemoteInvokeThread, RemoteInvokeOptions options)
            {
                RemoteInvokeThread = remoteInvokeThread;
                WaitForRemoteInvokeThread = waitForRemoteInvokeThread;
                Options = options;
            }

            public Process Process { get { throw null; } } // it's in the contract :(
            private Thread RemoteInvokeThread { get; set; }
            private Action WaitForRemoteInvokeThread { get; set; }
            public RemoteInvokeOptions Options { get; private set; }

            public void Dispose()
            {
                // A bit unorthodox to do throwing operations in a Dispose, but by doing it here we avoid
                // needing to do this in every derived test and keep each test much simpler.

                // Wait for the background thread to complete the synchronous invocation. We don't use WaitForRemoteWaitThread()
                // directly so that the timeout can be controlled.
                Assert.True(RemoteInvokeThread.Join(Options.TimeOut));

                // Throw if there was a remote exception.
                WaitForRemoteInvokeThread();
            }
        }
    }
}
