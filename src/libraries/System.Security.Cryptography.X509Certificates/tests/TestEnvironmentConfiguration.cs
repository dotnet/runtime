// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal static partial class TestEnvironmentConfiguration
    {
        internal static bool CanModifyStores { get; }

        internal static bool RunManualTests { get; } =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CRYPTOGRAPHY_MANUAL_TESTS"));

        static TestEnvironmentConfiguration()
        {
            bool canModifyStores = true;
            DetermineCanModifyStores(ref canModifyStores);
            CanModifyStores = canModifyStores;
        }

        static partial void DetermineCanModifyStores(ref bool canModify);
    }
}
