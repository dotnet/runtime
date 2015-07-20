// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

namespace InterProcessCommunication.Tests
{
    /// <summary>Base class used for all inter-process communication tests.</summary>
    public abstract class IpcTestBase : TemporaryFilesCleanupTestBase
    {
        /// <summary>The CoreCLR host used to host the test console app.</summary>
        private const string HostRunner = "corerun";
        /// <summary>The name of the test console app.</summary>
        private const string TestConsoleApp = "InterProcessCommunication.TestConsoleApp.exe";

        /// <summary>A timeout (milliseconds) after which a wait on a remote operation should be considered a failure.</summary>
        internal const int FailWaitTimeoutMilliseconds = 30 * 1000;
        /// <summary>The exist code returned when the test process exists successfully.</summary>
        internal const int SuccessExitCode = 42;

        /// <summary>Invokes the public, static method from this assembly in another process using the specified arguments.</summary>
        /// <param name="publicStaticMethodName">The name of the method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        internal RemoteInvokeHandle RemoteInvoke(string publicStaticMethodName, params string[] args)
        {
            return RemoteInvoke(GetType().GetTypeInfo().GetDeclaredMethod(publicStaticMethodName), args);
        }

        /// <summary>Invokes the public, static method from this assembly in another process using the specified arguments.</summary>
        /// <param name="publicStaticMethod">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        internal static RemoteInvokeHandle RemoteInvoke(MethodInfo publicStaticMethod, params string[] args)
        {
            // Verify the specified method is public and static, that it returns an int (the exit code),
            // and that if it accepts any arguments, they're all strings.
            Assert.True(publicStaticMethod.IsStatic);
            Assert.True(publicStaticMethod.IsPublic);
            Assert.Equal(typeof(int), publicStaticMethod.ReturnType);
            Assert.All(publicStaticMethod.GetParameters(), pi => Assert.Equal(typeof(string), pi.ParameterType));

            // Make sure it's on a public type
            Type t = publicStaticMethod.DeclaringType;
            Assert.True(t.GetTypeInfo().IsPublic);

            // And make sure it's in this assembly.  This isn't critical, but it helps with deployment to know
            // that the method to invoke is available because we're already runningin this assembly.
            Assembly a = t.GetTypeInfo().Assembly;
            Assert.Equal(typeof(IpcTestBase).GetTypeInfo().Assembly, a);

            // Start the other process and return a wrapper for it to handle its lifetime and exit checking.
            ProcessStartInfo psi = new ProcessStartInfo(
                HostRunner,
                TestConsoleApp + " \"" + a.FullName + "\" " + t.FullName + " " + publicStaticMethod.Name + " " + string.Join(" ", args));
            psi.UseShellExecute = false;
            psi.CreateNoWindow = false;
            return new RemoteInvokeHandle(Process.Start(psi));
        }

        /// <summary>A cleanup handle to the Process created for the remote invocation.</summary>
        internal sealed class RemoteInvokeHandle : IDisposable
        {
            public RemoteInvokeHandle(Process process)
            {
                Process = process;
            }

            public Process Process { get; private set; }

            public void Dispose()
            {
                if (Process != null)
                {
                    // A bit unorthodox to do throwing operations in a Dispose, but by doing it here we avoid
                    // needing to do this in every derived test and keep each test much simpler.
                    try
                    {
                        Assert.True(Process.WaitForExit(FailWaitTimeoutMilliseconds));
                        Assert.Equal(SuccessExitCode, Process.ExitCode);
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
}
