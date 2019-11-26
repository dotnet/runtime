// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Skips a test if the OS is the given type (Windows) and the OS version is greater than specified.
    /// E.g. Specifying Window 8 skips on Win 10, but not on Linux. Combine with OSSkipConditionAttribute as needed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class MaximumOSVersionAttribute : Attribute, ITestCondition
    {
        private readonly OperatingSystems _targetOS;
        private readonly Version _maxVersion;
        private readonly OperatingSystems _currentOS;
        private readonly Version _currentVersion;
        private readonly bool _skip;

        public MaximumOSVersionAttribute(OperatingSystems operatingSystem, string maxVersion) :
            this(operatingSystem, Version.Parse(maxVersion), GetCurrentOS(), GetCurrentOSVersion())
        {
        }

        // to enable unit testing
        internal MaximumOSVersionAttribute(OperatingSystems targetOS, Version maxVersion, OperatingSystems currentOS, Version currentVersion)
        {
            if (targetOS != OperatingSystems.Windows)
            {
                throw new NotImplementedException("Max version support is only implemented for Windows.");
            }
            _targetOS = targetOS;
            _maxVersion = maxVersion;
            _currentOS = currentOS;
            _currentVersion = currentVersion;

            // Do not skip other OS's, Use OSSkipConditionAttribute or a separate MaximumOsVersionAttribute for that.
            _skip = _targetOS == _currentOS && _maxVersion < _currentVersion;
            SkipReason = $"This test requires {_targetOS} {_maxVersion} or earlier.";
        }

        // Since a test would be executed only if 'IsMet' is true, return false if we want to skip
        public bool IsMet => !_skip;

        public string SkipReason { get; set; }

        private static OperatingSystems GetCurrentOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OperatingSystems.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OperatingSystems.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OperatingSystems.MacOSX;
            }
            throw new PlatformNotSupportedException();
        }

        static private Version GetCurrentOSVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.OSVersion.Version;
            }
            else
            {
                // Not implemented, but this will still be called before the OS check happens so don't throw.
                return new Version(0, 0);
            }
        }
    }
}
