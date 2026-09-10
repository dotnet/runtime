// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Threading;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        internal static readonly ReaderWriterLockSlim s_processStartLock = new ReaderWriterLockSlim();
        internal static int s_cachedSerializationSwitch;

        internal static bool PlatformSupportsProcessStartAndKill
            => !((OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()) || OperatingSystem.IsTvOS());

        internal static bool PlatformSupportsConsole
            => !(OperatingSystem.IsAndroid() || OperatingSystem.IsMacCatalyst());

        internal static Win32Exception CreateExceptionForErrorStartingProcess(string errorMessage, int errorCode, string fileName, string? workingDirectory)
        {
            string directoryForException = string.IsNullOrEmpty(workingDirectory) ? Directory.GetCurrentDirectory() : workingDirectory;
            string msg = SR.Format(SR.ErrorStartingProcess, fileName, directoryForException, errorMessage);
            return new Win32Exception(errorCode, msg);
        }

        internal static int ToTimeoutMilliseconds(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;

            ArgumentOutOfRangeException.ThrowIfLessThan(totalMilliseconds, -1, nameof(timeout));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(totalMilliseconds, int.MaxValue, nameof(timeout));

            return (int)totalMilliseconds;
        }
    }
}
