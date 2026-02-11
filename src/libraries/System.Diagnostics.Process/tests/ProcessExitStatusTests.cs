// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessExitStatusTests
    {
        [Fact]
        public void Constructor_WithExitCodeOnly_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(0, false);

            Assert.Equal(0, status.ExitCode);
            Assert.False(status.Canceled);
            Assert.Null(status.Signal);
        }

        [Fact]
        public void Constructor_WithNonZeroExitCode_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(42, false);

            Assert.Equal(42, status.ExitCode);
            Assert.False(status.Canceled);
            Assert.Null(status.Signal);
        }

        [Fact]
        public void Constructor_WithCanceled_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(1, true);

            Assert.Equal(1, status.ExitCode);
            Assert.True(status.Canceled);
            Assert.Null(status.Signal);
        }

        [Fact]
        public void Constructor_WithSignal_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(137, false, PosixSignal.SIGTERM);

            Assert.Equal(137, status.ExitCode);
            Assert.False(status.Canceled);
            Assert.Equal(PosixSignal.SIGTERM, status.Signal);
        }

        [Fact]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            ProcessExitStatus status = new ProcessExitStatus(130, true, PosixSignal.SIGINT);

            Assert.Equal(130, status.ExitCode);
            Assert.True(status.Canceled);
            Assert.Equal(PosixSignal.SIGINT, status.Signal);
        }

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            ProcessExitStatus status1 = new ProcessExitStatus(0, false);
            ProcessExitStatus status2 = new ProcessExitStatus(0, false);

            Assert.True(status1.Equals(status2));
            Assert.True(status1 == status2);
            Assert.False(status1 != status2);
        }

        [Fact]
        public void Equals_DifferentExitCode_ReturnsFalse()
        {
            ProcessExitStatus status1 = new ProcessExitStatus(0, false);
            ProcessExitStatus status2 = new ProcessExitStatus(1, false);

            Assert.False(status1.Equals(status2));
            Assert.False(status1 == status2);
            Assert.True(status1 != status2);
        }

        [Fact]
        public void Equals_DifferentCanceled_ReturnsFalse()
        {
            ProcessExitStatus status1 = new ProcessExitStatus(0, false);
            ProcessExitStatus status2 = new ProcessExitStatus(0, true);

            Assert.False(status1.Equals(status2));
            Assert.False(status1 == status2);
            Assert.True(status1 != status2);
        }

        [Fact]
        public void Equals_DifferentSignal_ReturnsFalse()
        {
            ProcessExitStatus status1 = new ProcessExitStatus(130, false, PosixSignal.SIGINT);
            ProcessExitStatus status2 = new ProcessExitStatus(130, false, PosixSignal.SIGTERM);

            Assert.False(status1.Equals(status2));
            Assert.False(status1 == status2);
            Assert.True(status1 != status2);
        }

        [Fact]
        public void Equals_OneWithSignalOneWithout_ReturnsFalse()
        {
            ProcessExitStatus status1 = new ProcessExitStatus(0, false);
            ProcessExitStatus status2 = new ProcessExitStatus(0, false, PosixSignal.SIGINT);

            Assert.False(status1.Equals(status2));
            Assert.False(status1 == status2);
            Assert.True(status1 != status2);
        }

        [Fact]
        public void Equals_WithObject_SameValues_ReturnsTrue()
        {
            ProcessExitStatus status1 = new ProcessExitStatus(0, false);
            object status2 = new ProcessExitStatus(0, false);

            Assert.True(status1.Equals(status2));
        }

        [Fact]
        public void Equals_WithObject_DifferentType_ReturnsFalse()
        {
            ProcessExitStatus status = new ProcessExitStatus(0, false);
            object other = new object();

            Assert.False(status.Equals(other));
        }

        [Fact]
        public void Equals_WithObject_Null_ReturnsFalse()
        {
            ProcessExitStatus status = new ProcessExitStatus(0, false);

            Assert.False(status.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCode()
        {
            ProcessExitStatus status1 = new ProcessExitStatus(42, true, PosixSignal.SIGTERM);
            ProcessExitStatus status2 = new ProcessExitStatus(42, true, PosixSignal.SIGTERM);

            Assert.Equal(status1.GetHashCode(), status2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCode()
        {
            ProcessExitStatus status1 = new ProcessExitStatus(0, false);
            ProcessExitStatus status2 = new ProcessExitStatus(1, false);

            // While not guaranteed, hash codes for different values should generally be different
            // This is a probabilistic test - if it fails occasionally, that's acceptable
            Assert.NotEqual(status1.GetHashCode(), status2.GetHashCode());
        }
    }
}
