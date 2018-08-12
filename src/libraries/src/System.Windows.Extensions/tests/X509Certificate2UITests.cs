// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class X509Certificate2UITests
    {
        [Fact]
        public static void SelectFromCollection_InvalidInput()
        { 
            Assert.Throws<ArgumentNullException>("certificates", () => X509Certificate2UI.SelectFromCollection(null, string.Empty, string.Empty, X509SelectionFlag.SingleSelection));
            Assert.Throws<ArgumentException>(() => X509Certificate2UI.SelectFromCollection(new X509Certificate2Collection(), string.Empty, string.Empty, (X509SelectionFlag)2));
        }

        [Fact]
        public static void SelectFromCollection_InvalidInput_WithHwnd()
        {
            Assert.Throws<ArgumentNullException>("certificates", () => X509Certificate2UI.SelectFromCollection(null, string.Empty, string.Empty, X509SelectionFlag.SingleSelection, IntPtr.Zero));
            Assert.Throws<ArgumentException>(() => X509Certificate2UI.SelectFromCollection(new X509Certificate2Collection(), string.Empty, string.Empty, (X509SelectionFlag)2, IntPtr.Zero));
        }

        [Fact]
        public static void DisplayCertificate_InvalidInput()
        {
            Assert.Throws<ArgumentNullException>("certificate", () => X509Certificate2UI.DisplayCertificate(null));
            Assert.Throws<ArgumentNullException>("certificate", () => X509Certificate2UI.DisplayCertificate(null, IntPtr.Zero));
        }
    }
}
