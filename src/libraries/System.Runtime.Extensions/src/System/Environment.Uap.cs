// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System
{
    public static partial class Environment
    {
        public static int ExitCode
        {
            get { return 0; }
            set { throw new PlatformNotSupportedException(); }
        }

        private static string ExpandEnvironmentVariablesCore(string name) => name;

        private static string GetEnvironmentVariableCore(string variable) => string.Empty;

        private static string GetEnvironmentVariableCore(string variable, EnvironmentVariableTarget target) => string.Empty;

        private static IDictionary GetEnvironmentVariablesCore() => new LowLevelDictionary<string, string>();

        private static IDictionary GetEnvironmentVariablesCore(EnvironmentVariableTarget target) => new LowLevelDictionary<string, string>();

        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option)
        {
            return string.Empty;
        }

        private static bool Is64BitOperatingSystemWhen32BitProcess => false;

        public static string MachineName { get { throw new PlatformNotSupportedException(); } }

        private static Lazy<OperatingSystem> s_osVersion = new Lazy<OperatingSystem>(() =>
        {
            // GetVersionExW isn't available.  We could throw a PlatformNotSupportedException, but we can
            // at least hand back Win32NT to highlight that we're on Windows rather than Unix.
            return new OperatingSystem(PlatformID.Win32NT, new Version(0, 0));
        });

        public static int ProcessorCount => ProcessorCountFromSystemInfo;

        private static void SetEnvironmentVariableCore(string variable, string value)
        {
            throw new PlatformNotSupportedException();
        }

        private static void SetEnvironmentVariableCore(string variable, string value, EnvironmentVariableTarget target)
        {
            throw new PlatformNotSupportedException();
        }

        public static string SystemDirectory { get { throw new PlatformNotSupportedException(); } }

        public static string UserName { get { throw new PlatformNotSupportedException(); } }

        public static string UserDomainName { get { throw new PlatformNotSupportedException(); } }


        public static string GetEnvironmentVariable(string variable)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            return GetEnvironmentVariableCore(variable);
        }

        public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            ValidateTarget(target);

            return GetEnvironmentVariableCore(variable, target);
        }

        public static IDictionary GetEnvironmentVariables()
        {
            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            return GetEnvironmentVariablesCore();
        }

        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            ValidateTarget(target);

            return GetEnvironmentVariablesCore(target);
        }

        public static void SetEnvironmentVariable(string variable, string value)
        {
            ValidateVariableAndValue(variable, ref value);

            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            SetEnvironmentVariableCore(variable, value);
        }

        public static void SetEnvironmentVariable(string variable, string value, EnvironmentVariableTarget target)
        {
            ValidateVariableAndValue(variable, ref value);
            ValidateTarget(target);

            SetEnvironmentVariableCore(variable, value, target);
        }

        private static void ValidateVariableAndValue(string variable, ref string value)
        {
            const int MaxEnvVariableValueLength = 32767;

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }
            if (variable.Length == 0)
            {
                throw new ArgumentException(SR.Argument_StringZeroLength, nameof(variable));
            }
            if (variable[0] == '\0')
            {
                throw new ArgumentException(SR.Argument_StringFirstCharIsZero, nameof(variable));
            }
            if (variable.Length >= MaxEnvVariableValueLength)
            {
                throw new ArgumentException(SR.Argument_LongEnvVarValue, nameof(variable));
            }
            if (variable.IndexOf('=') != -1)
            {
                throw new ArgumentException(SR.Argument_IllegalEnvVarName, nameof(variable));
            }

            if (string.IsNullOrEmpty(value) || value[0] == '\0')
            {
                // Explicitly null out value if it's empty
                value = null;
            }
            else if (value.Length >= MaxEnvVariableValueLength)
            {
                throw new ArgumentException(SR.Argument_LongEnvVarValue, nameof(value));
            }
        }

        private static void ValidateTarget(EnvironmentVariableTarget target)
        {
            if (target != EnvironmentVariableTarget.Process &&
                target != EnvironmentVariableTarget.Machine &&
                target != EnvironmentVariableTarget.User)
            {
                throw new ArgumentOutOfRangeException(nameof(target), target, SR.Format(SR.Arg_EnumIllegalVal, target));
            }
        }
    }
}
