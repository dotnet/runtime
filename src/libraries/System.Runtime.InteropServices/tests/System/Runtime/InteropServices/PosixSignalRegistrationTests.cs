// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public partial class PosixSignalRegistrationTests
    {
        private static TimeSpan SuccessTimeout => TimeSpan.FromSeconds(30);

        [Fact]
        public void Create_NullHandler_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("handler", () => PosixSignalRegistration.Create(PosixSignal.SIGCONT, null));
        }

        [Theory]
        [MemberData(nameof(UnsupportedSignals))]
        public void Create_InvalidSignal_Throws(PosixSignal signal)
        {
            Assert.Throws<PlatformNotSupportedException>(() => PosixSignalRegistration.Create(signal, ctx => { }));
        }

        [Theory]
        [MemberData(nameof(UninstallableSignals))]
        public void Create_UninstallableSignal_Throws(PosixSignal signal)
        {
            Assert.Throws<IOException>(() => PosixSignalRegistration.Create(signal, ctx => { }));
        }

        [Theory]
        [MemberData(nameof(SupportedSignals))]
        public void Create_ValidSignal_Success(PosixSignal signal)
        {
            PosixSignalRegistration.Create(signal, ctx => { }).Dispose();
        }

        [Theory]
        [MemberData(nameof(SupportedSignals))]
        public void Dispose_Idempotent(PosixSignal signal)
        {
            PosixSignalRegistration registration = PosixSignalRegistration.Create(signal, ctx => { });
            registration.Dispose();
            registration.Dispose();
        }

        [Fact]
        public void Create_RegisterForMultipleSignalsMultipletimes_Success()
        {
            var registrations = new List<PosixSignalRegistration>();
            for (int i = 0; i < 3; i++)
            {
                foreach (object[] signal in SupportedSignals())
                {
                    registrations.Add(PosixSignalRegistration.Create((PosixSignal)signal[0], _ => { }));
                }

                foreach (object[] signal in SupportedSignals())
                {
                    registrations.Add(PosixSignalRegistration.Create((PosixSignal)signal[0], _ => { }));
                }

                foreach (PosixSignalRegistration registration in registrations)
                {
                    registration.Dispose();
                }
            }
        }
    }
}
