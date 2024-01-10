// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        internal const long DefaultPkcs12UnspecifiedPasswordIterationLimit = 600_000;

        internal static long Pkcs12UnspecifiedPasswordIterationLimit { get; } = InitializePkcs12UnspecifiedPasswordIterationLimit();

        internal static bool X509ChainBuildThrowOnInternalError { get; } = InitializeX509ChainBuildThrowOnInternalError();

        private static long InitializePkcs12UnspecifiedPasswordIterationLimit()
        {
            object? data = AppContext.GetData("System.Security.Cryptography.Pkcs12UnspecifiedPasswordIterationLimit");

            if (data is null)
            {
                return DefaultPkcs12UnspecifiedPasswordIterationLimit;
            }

            try
            {
                return Convert.ToInt64(data);
            }
            catch
            {
                return DefaultPkcs12UnspecifiedPasswordIterationLimit;
            }
        }

        private static bool InitializeX509ChainBuildThrowOnInternalError()
        {
            // n.b. the switch defaults to TRUE if it has not been explicitly set.
            return AppContext.TryGetSwitch("System.Security.Cryptography.ThrowOnX509ChainBuildInternalError", out bool isEnabled)
                ? isEnabled : true;
        }
    }
}
