// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Hosting.Systemd;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class SystemdHelpersTests
    {
        public static bool IsRemoteExecutorSupportedOnLinux => PlatformDetection.IsLinux && RemoteExecutor.IsSupported;

        [ConditionalFact(typeof(SystemdHelpersTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void IsSystemdServiceReturnsTrueWhenSystemdExecPidMatchesCurrentProcessId()
        {
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                string processId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", processId);

                Assert.True(SystemdHelpers.IsSystemdService());
            });
        }

        [ConditionalFact(typeof(SystemdHelpersTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void IsSystemdServiceReturnsFalseWhenSystemdExecPidDoesNotMatchCurrentProcessId()
        {
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                string processId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
                int nonMatchingPid = int.MaxValue; // No real process will ever have this PID.

                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", nonMatchingPid.ToString(CultureInfo.InvariantCulture));

                Assert.False(SystemdHelpers.IsSystemdService());
            });
        }

        [ConditionalFact(typeof(SystemdHelpersTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void IsSystemdServiceReturnsFalseWhenSystemdExecPidIsAbsent()
        {
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                // When SYSTEMD_EXEC_PID is absent the code skips the v248+ path entirely
                // and falls through to the legacy detection. Outside a real systemd session
                // (not PID 1, parent is not systemd), the result must be false.
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", null);
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", null);
                Environment.SetEnvironmentVariable("LISTEN_PID", null);

                Assert.False(SystemdHelpers.IsSystemdService());
            });
        }

        [ConditionalFact(typeof(SystemdHelpersTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void IsSystemdServiceReturnsFalseWhenSystemdExecPidIsMalformed()
        {
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                // A malformed SYSTEMD_EXEC_PID must not be trusted: the logic falls through
                // to the legacy /proc-based detection, which won't match outside a real systemd session.
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", "not-a-pid");
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", null);
                Environment.SetEnvironmentVariable("LISTEN_PID", null);

                Assert.False(SystemdHelpers.IsSystemdService());
            });
        }

        [ConditionalFact(typeof(SystemdHelpersTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void IsSystemdServiceCachesFirstEvaluation()
        {
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                string processId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
                int nonMatchingPid = int.MaxValue; // No real process will ever have this PID.

                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", processId);
                bool firstEvaluation = SystemdHelpers.IsSystemdService();

                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", nonMatchingPid.ToString(CultureInfo.InvariantCulture));
                bool secondEvaluation = SystemdHelpers.IsSystemdService();

                Assert.True(firstEvaluation);
                Assert.True(secondEvaluation);
            });
        }
    }
}
