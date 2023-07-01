// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Internal.DeveloperExperience;
using System.Runtime;

namespace System
{
    public static partial class Environment
    {
        public static int CurrentManagedThreadId => ManagedThreadId.Current;

        private static int s_latchedExitCode;

        public static int ExitCode
        {
            get => s_latchedExitCode;
            set => s_latchedExitCode = value;
        }

        [DoesNotReturn]
        public static void Exit(int exitCode)
        {
            s_latchedExitCode = exitCode;
            ShutdownCore();
            ExitRaw();
        }

        // Note: The CLR's Watson bucketization code looks at the caller of the FCALL method
        // to assign blame for crashes.  Don't mess with this, such as by making it call
        // another managed helper method, unless you consult with some CLR Watson experts.
        [DoesNotReturn]
        public static void FailFast(string message) =>
            RuntimeExceptionHelpers.FailFast(message);

        [DoesNotReturn]
        public static void FailFast(string message, Exception exception) =>
            RuntimeExceptionHelpers.FailFast(message, exception);

        internal static void FailFast(string message, Exception exception, string _ /*errorSource*/)
        {
            // TODO: errorSource originates from CoreCLR (See: https://github.com/dotnet/coreclr/pull/15895)
            // For now, we ignore errorSource but we should distinguish the way FailFast prints exception message using errorSource
            bool result = DeveloperExperience.Default.OnContractFailure(exception.StackTrace, ContractFailureKind.Assert, message, null, null, null);
            if (!result)
            {
                RuntimeExceptionHelpers.FailFast(message, exception);
            }
        }

        private static int GetProcessorCount() => Runtime.RuntimeImports.RhGetProcessCpuCount();

        internal static void ShutdownCore()
        {
#if !TARGET_BROWSER // WASMTODO Be careful what happens here as if the code has called emscripten_set_main_loop then the main loop method will normally be called repeatedly after this method
            AppContext.OnProcessExit();
#endif
        }

        public static int TickCount => (int)TickCount64;
    }
}
