// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics
{
    /// <summary>Base class used for all tests that need to spawn a remote process.</summary>
    public abstract partial class RemoteExecutorTestBase : FileCleanupTestBase
    {
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

            psi.FileName = HostRunner;
            psi.Arguments = ExtraParameter + testConsoleAppArgs;

            // Return the handle to the process, which may or not be started
            return new RemoteInvokeHandle(options.Start ?
                Process.Start(psi) :
                new Process() { StartInfo = psi }, options);
        }
    }
}
