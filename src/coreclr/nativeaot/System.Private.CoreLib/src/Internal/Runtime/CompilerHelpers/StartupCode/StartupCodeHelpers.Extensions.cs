// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using System.Threading;

using Internal.Runtime.Augments;

using Debug = Internal.Runtime.CompilerHelpers.StartupDebug;

namespace Internal.Runtime.CompilerHelpers
{
    internal partial class StartupCodeHelpers
    {
        /// <summary>
        /// Return the registered logical modules; optionally copy them into an array.
        /// </summary>
        internal static ReadOnlySpan<TypeManagerHandle> GetLoadedModules()
            => s_modules.AsSpan(0, s_moduleCount);

        internal static unsafe void InitializeCommandLineArgsW(int argc, char** argv)
        {
            string[] args = new string[argc];
            for (int i = 0; i < argc; ++i)
            {
                args[i] = new string(argv[i]);
            }
            Environment.s_commandLineArgs = args;
        }

        internal static unsafe void InitializeCommandLineArgs(int argc, sbyte** argv)
        {
            string[] args = new string[argc];
            for (int i = 0; i < argc; ++i)
            {
                args[i] = new string(argv[i]);
            }
            Environment.s_commandLineArgs = args;
        }

        private static string[] GetMainMethodArguments()
        {
            // Environment.s_commandLineArgs includes the executable name, Main() arguments do not.
            string[]? args = Environment.s_commandLineArgs;

            // Environment.s_commandLineArgs is expected to initialized during startup.
            Debug.Assert(args != null);
            Debug.Assert(args.Length > 0);

            string[] mainArgs = new string[args.Length - 1];
            Array.Copy(args, 1, mainArgs, 0, mainArgs.Length);

            return mainArgs;
        }

        private static void SetLatchedExitCode(int exitCode)
        {
            Environment.ExitCode = exitCode;
        }

        // Shuts down the class library and returns the process exit code.
        private static int Shutdown()
        {
            Thread.WaitForForegroundThreads();

            Environment.ShutdownCore();

            return Environment.ExitCode;
        }

#if TARGET_WINDOWS
        [SupportedOSPlatform("windows")]
        private static void InitializeApartmentState(ApartmentState state)
        {
            Thread.CurrentThread.SetApartmentState(state);
        }
#endif
    }
}
