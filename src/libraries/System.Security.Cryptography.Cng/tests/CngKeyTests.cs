// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CngKeyTests
    {
        [Theory]
        [MemberData(nameof(AllPublicProperties))]
        public static void AllProperties_ThrowOnDisposal(string propertyName)
        {
            // Create a dummy key. Use a fast-ish algorithm.
            // If we can't query the property even on a fresh key, exclude it from the tests.

            PropertyInfo propInfo = typeof(CngKey).GetProperty(propertyName);
            CngKey theKey = CngKey.Create(CngAlgorithm.ECDsaP256);

            try
            {
                propInfo.GetValue(theKey);
            }
            catch
            {
                // Property getter threw an exception. It's nonsensical for us to query
                // whether this same getter throws ObjectDisposedException once the object
                // is disposed. So we'll just mark this test as success.

                return;
            }

            // We've queried the property. Now dispose the object and query the property again.
            // We should see an ObjectDisposedException.

            theKey.Dispose();
            Assert.ThrowsAny<ObjectDisposedException>(() =>
                propInfo.GetValue(theKey, BindingFlags.DoNotWrapExceptions, null, null, null));
        }

        public static IEnumerable<object[]> AllPublicProperties()
        {
            foreach (PropertyInfo pi in typeof(CngKey).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (pi.GetMethod is not null && !typeof(SafeHandle).IsAssignableFrom(pi.PropertyType))
                {
                    yield return new[] { pi.Name };
                }
            }
        }
    }
}
