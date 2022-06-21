﻿// Licensed to the .NET Foundation under one or more agreements.
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

        private DateTimeOffset GetDateTimeOffsetFromSecondsSinceEpoch(double secondsSinceUnixEpoch) =>
            new DateTimeOffset((long)(secondsSinceUnixEpoch * TimeSpan.TicksPerSecond) + DateTime.UnixEpoch.Ticks, TimeSpan.Zero);

        private double GetSecondsSinceEpochFromDateTimeOffset(DateTimeOffset value) =>
            ((double)(value.UtcDateTime - DateTime.UnixEpoch).Ticks) / TimeSpan.TicksPerSecond;

        protected DateTimeOffset GetDateTimeOffsetFromTimestampString(IReadOnlyDictionary<string, string> ea, string fieldName)
        {
            Assert.True(ea.TryGetValue(fieldName, out string value), $"Extended attributes did not contain field '{fieldName}'");

            // As regular header fields, timestamps are saved as integer numbers that fit in 12 bytes
            // But as extended attributes, they should always be saved as doubles with decimal precision
            Assert.Contains(".", value);

            Assert.True(double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double secondsSinceEpoch), $"Extended attributes field '{fieldName}' is not a valid double.");
            return GetDateTimeOffsetFromSecondsSinceEpoch(secondsSinceEpoch);
        }

        protected string GetTimestampStringFromDateTimeOffset(DateTimeOffset timestamp)
        {
            double secondsSinceEpoch = GetSecondsSinceEpochFromDateTimeOffset(timestamp);
            return secondsSinceEpoch.ToString("F9", CultureInfo.InvariantCulture);
        }

        protected void VerifyExtendedAttributeTimestamp(PaxTarEntry paxEntry, string fieldName, DateTimeOffset minimumTime)
        {
            DateTimeOffset converted = GetDateTimeOffsetFromTimestampString(paxEntry.ExtendedAttributes, fieldName);
            AssertExtensions.GreaterThanOrEqualTo(converted, minimumTime);
        }
    }
}
