// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

        private DateTimeOffset GetDateTimeOffsetFromSecondsSinceEpoch(decimal secondsSinceUnixEpoch) =>
            new DateTimeOffset((long)(secondsSinceUnixEpoch * TimeSpan.TicksPerSecond) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);

        private decimal GetSecondsSinceEpochFromDateTimeOffset(DateTimeOffset value) =>
            ((decimal)(value.UtcDateTime - DateTime.UnixEpoch).Ticks) / TimeSpan.TicksPerSecond;

        protected DateTimeOffset GetDateTimeOffsetFromTimestampString(IReadOnlyDictionary<string, string> ea, string fieldName)
        {
            Assert.Contains(fieldName, ea);
            return GetDateTimeOffsetFromTimestampString(ea[fieldName]);
        }

        protected DateTimeOffset GetDateTimeOffsetFromTimestampString(string strNumber)
        {
            Assert.True(decimal.TryParse(strNumber, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal secondsSinceEpoch));
            return GetDateTimeOffsetFromSecondsSinceEpoch(secondsSinceEpoch);
        }

        protected string GetTimestampStringFromDateTimeOffset(DateTimeOffset timestamp)
        {
            decimal secondsSinceEpoch = GetSecondsSinceEpochFromDateTimeOffset(timestamp);
            return secondsSinceEpoch.ToString("G", CultureInfo.InvariantCulture);
        }

        protected void VerifyExtendedAttributeTimestamp(PaxTarEntry paxEntry, string fieldName, DateTimeOffset minimumTime)
        {
            DateTimeOffset converted = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, fieldName);
            AssertExtensions.GreaterThanOrEqualTo(converted, minimumTime);
        }

        protected void VerifyExtendedAttributeTimestamps(PaxTarEntry pax)
        {
            Assert.NotNull(pax.ExtendedAttributes);
            AssertExtensions.GreaterThanOrEqualTo(pax.ExtendedAttributes.Count, 3); // Expect to at least collect mtime, ctime and atime

            VerifyExtendedAttributeTimestamp(pax, PaxEaMTime, MinimumTime);
            VerifyExtendedAttributeTimestamp(pax, PaxEaATime, MinimumTime);
            VerifyExtendedAttributeTimestamp(pax, PaxEaCTime, MinimumTime);
        }
    }
}
