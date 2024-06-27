// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        internal const int DefaultPkcs12UnspecifiedPasswordIterationLimit = 600_000;

        internal static long Pkcs12UnspecifiedPasswordIterationLimit { get; } = InitializePkcs12UnspecifiedPasswordIterationLimit();

        private static long InitializePkcs12UnspecifiedPasswordIterationLimit()
        {
            object? data = AppContext.GetData("System.Security.Cryptography.Pkcs12UnspecifiedPasswordIterationLimit");

            if (data is null)
            {
                return DefaultPkcs12UnspecifiedPasswordIterationLimit;
            }

            try
            {
                return Convert.ToInt64(data, CultureInfo.InvariantCulture);
            }
            catch
            {
                return DefaultPkcs12UnspecifiedPasswordIterationLimit;
            }
        }
    }
}
