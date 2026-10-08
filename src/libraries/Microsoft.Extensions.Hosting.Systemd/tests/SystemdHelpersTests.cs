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
        public void IsSystemdServiceReturnsFalseWhenSystemdExecPidDoesNotMatchOutsideSystemdSession()
        {
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                // When SYSTEMD_EXEC_PID is set but doesn't match the current PID, the code
                // falls through to legacy detection. Outside a real systemd session
                // (not PID 1, parent is not named "systemd"), the legacy path returns false.
                // Note: the fall-through itself cannot be directly observed in a unit test
                // without being PID 1 or having a parent named "systemd".
                int nonMatchingPid = int.MaxValue; // No real process will ever have this PID.
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", nonMatchingPid.ToString(CultureInfo.InvariantCulture));
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", null);
                Environment.SetEnvironmentVariable("LISTEN_PID", null);

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

        [ConditionalFact(typeof(SystemdHelpersTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void IsSystemdServiceCachesFirstNegativeEvaluation()
        {
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", null);
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", null);
                Environment.SetEnvironmentVariable("LISTEN_PID", null);

                var firstEvaluation = SystemdHelpers.IsSystemdService();

                string processId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", processId);

                var secondEvaluation = SystemdHelpers.IsSystemdService();
                Assert.False(firstEvaluation);
                Assert.False(secondEvaluation);
            });
        }

        [ConditionalFact(typeof(SystemdHelpersTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void IsSystemdServiceReturnsFalseWhenSystemdExecPidDoesNotMatchAndNotifySocketIsSet()
        {
            // Child process scenario: SYSTEMD_EXEC_PID is set but doesn't match the current PID,
            // while NOTIFY_SOCKET is inherited from the parent service.
            // The mismatch falls through to legacy detection (not PID 1, parent not named "systemd"),
            // which returns false. NOTIFY_SOCKET is only considered in the PID 1 container path.
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                int nonMatchingPid = int.MaxValue;
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", nonMatchingPid.ToString(CultureInfo.InvariantCulture));
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", "/run/systemd/notify");
                Environment.SetEnvironmentVariable("LISTEN_PID", null);

                Assert.False(SystemdHelpers.IsSystemdService());
            });
        }

        [ConditionalFact(typeof(SystemdHelpersTests), nameof(IsRemoteExecutorSupportedOnLinux))]
        public void IsSystemdServiceReturnsFalseWhenNotifySocketIsEmpty()
        {
            using var _ = RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable("SYSTEMD_EXEC_PID", null);
                Environment.SetEnvironmentVariable("NOTIFY_SOCKET", "");
                Environment.SetEnvironmentVariable("LISTEN_PID", null);

                Assert.False(SystemdHelpers.IsSystemdService());
            });
        }
    }
}
