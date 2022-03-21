// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class PosixSignalRegistrationTests
    {
        public static IEnumerable<object[]> UninstallableSignals() => Enumerable.Empty<object[]>();

        public static IEnumerable<object[]> SupportedSignals() => SupportedPosixSignals.Select(p => new object[] { p });

        public static IEnumerable<object[]> UnsupportedSignals()
        {
            foreach (PosixSignal signal in Enum.GetValues<PosixSignal>().Except(SupportedPosixSignals))
            {
                yield return new object[] { signal };
            }

            yield return new object[] { 0 };
            yield return new object[] { -1000 };
            yield return new object[] { 1000 };
        }

        private static IEnumerable<PosixSignal> SupportedPosixSignals => new[] { PosixSignal.SIGINT, PosixSignal.SIGQUIT, PosixSignal.SIGTERM, PosixSignal.SIGHUP };

        [Fact]
        public void ExternalConsoleManipulation_RegistrationRemoved_UnregisterSucceeds()
        {
            RemoteExecutor.Invoke(() =>
            {
                PosixSignalRegistration r = PosixSignalRegistration.Create(PosixSignal.SIGINT, _ => { });
                FreeConsole();
                AllocConsole();
                r.Dispose(); // validate this doesn't throw even though the use of Free/AllocConsole likely removed our registration
            }).Dispose();
        }

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
