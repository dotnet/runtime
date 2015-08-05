// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class HandleTests
    {
        [Fact]
        public static void HandleDuplication()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                SafeNCryptKeyHandle keyHandle1 = key.Handle;
                SafeNCryptKeyHandle keyHandle2 = key.Handle;
                Assert.NotSame(keyHandle1, keyHandle2);

                keyHandle1.Dispose();
                keyHandle2.Dispose();

                // Make sure that disposing the spawned off handles didn't dispose the original. Set and get a custom property to ensure
                // the original is still in good condition.
                string propertyName = "Are you alive";
                bool hasProperty = key.HasProperty(propertyName, CngPropertyOptions.CustomProperty);
                Assert.False(hasProperty);

                byte[] propertyValue = { 1, 2, 3 };
                CngProperty property = new CngProperty(propertyName, propertyValue, CngPropertyOptions.CustomProperty);
                key.SetProperty(property);

                byte[] actualValue = key.GetProperty(propertyName, CngPropertyOptions.CustomProperty).GetValue();
                Assert.Equal<byte>(propertyValue, actualValue);
            }
        }
    }
}

