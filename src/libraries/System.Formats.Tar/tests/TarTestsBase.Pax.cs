// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase : FileCleanupTestBase
    {
        protected void SetRegularFile(PaxTarEntry regularFile)
        {
            SetCommonRegularFile(regularFile);
            SetPosixProperties(regularFile);
        }

        protected void SetDirectory(PaxTarEntry directory)
        {
            SetCommonDirectory(directory);
            SetPosixProperties(directory);
        }

        protected void SetHardLink(PaxTarEntry hardLink)
        {
            SetCommonHardLink(hardLink);
            SetPosixProperties(hardLink);
        }

        protected void SetSymbolicLink(PaxTarEntry symbolicLink)
        {
            SetCommonSymbolicLink(symbolicLink);
            SetPosixProperties(symbolicLink);
        }

        protected void SetCharacterDevice(PaxTarEntry characterDevice)
        {
            SetCharacterDeviceProperties(characterDevice);
        }

        protected void SetBlockDevice(PaxTarEntry blockDevice)
        {
            SetBlockDeviceProperties(blockDevice);
        }

        protected void SetFifo(PaxTarEntry fifo)
        {
            SetFifoProperties(fifo);
        }

        protected void VerifyRegularFile(PaxTarEntry regularFile, bool isWritable)
        {
            VerifyPosixRegularFile(regularFile, isWritable);
        }

        protected void VerifyDirectory(PaxTarEntry directory)
        {
            VerifyPosixDirectory(directory);
        }

        protected void VerifyHardLink(PaxTarEntry hardLink)
        {
            VerifyPosixHardLink(hardLink);
        }

        protected void VerifySymbolicLink(PaxTarEntry symbolicLink)
        {
            VerifyPosixSymbolicLink(symbolicLink);
        }

        protected void VerifyCharacterDevice(PaxTarEntry characterDevice)
        {
            VerifyPosixCharacterDevice(characterDevice);
        }

        protected void VerifyBlockDevice(PaxTarEntry blockDevice)
        {
            VerifyPosixBlockDevice(blockDevice);
        }

        protected void VerifyFifo(PaxTarEntry fifo)
        {
            VerifyPosixFifo(fifo);
        }

        protected DateTimeOffset ConvertDoubleToDateTimeOffset(double value)
        {
            return new DateTimeOffset((long)(value * TimeSpan.TicksPerSecond) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);
        }

        protected double ConvertDateTimeOffsetToDouble(DateTimeOffset value)
        {
            return ((double)(value.UtcDateTime - DateTime.UnixEpoch).Ticks)/TimeSpan.TicksPerSecond;
        }

        protected void VerifyExtendedAttributeTimestamp(PaxTarEntry entry, string name, DateTimeOffset expected = default)
        {
            Assert.Contains(name, entry.ExtendedAttributes);

            // As regular header fields, timestamps are saved as integer numbers that fit in 12 bytes
            // But as extended attributes, they should always be saved as doubles with decimal precision
            Assert.Contains(".", entry.ExtendedAttributes[name]);

            Assert.True(double.TryParse(entry.ExtendedAttributes[name], NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleTime)); // Force the parsing to use '.' as decimal separator
            DateTimeOffset timestamp = ConvertDoubleToDateTimeOffset(doubleTime);

            if (expected != default)
            {
                Assert.Equal(expected, timestamp);
            }
            else
            {
                Assert.True(timestamp > DateTimeOffset.UnixEpoch);
            }
        }
    }
}
