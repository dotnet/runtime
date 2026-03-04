// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        internal static int MaxReferencesPerSignedInfo { get; } = InitializeMaxReferencesPerSignedInfo();

        private static int InitializeMaxReferencesPerSignedInfo()
        {
            const int DefaultMaxReferencesPerSignedInfo = 100;
            object? data = AppContext.GetData("System.Security.Cryptography.MaxReferencesPerSignedInfo");

            if (data is null)
            {
                return DefaultMaxReferencesPerSignedInfo;
            }

            try
            {
                return Convert.ToInt32(data, CultureInfo.InvariantCulture);
            }
            catch
            {
                return DefaultMaxReferencesPerSignedInfo;
            }
        }
    }
}
