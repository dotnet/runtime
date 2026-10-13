// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
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

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public static void Handle_ConcurrentFirstAccess()
        {
            const int IterationCount = 10;
            const int ThreadCount = 8;
            TimeSpan timeout = TimeSpan.FromSeconds(30);

            for (int iteration = 0; iteration < IterationCount; iteration++)
            {
                using CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256);
                using Barrier barrier = new Barrier(ThreadCount);
                var handles = new SafeNCryptKeyHandle?[ThreadCount];
                var exceptions = new Exception?[ThreadCount];
                var threads = new Thread[ThreadCount];

                for (int i = 0; i < threads.Length; i++)
                {
                    int index = i;
                    threads[i] = new Thread(() =>
                    {
                        try
                        {
                            Assert.True(barrier.SignalAndWait(timeout), "Timed out waiting for concurrent handle access.");
                            handles[index] = key.Handle;
                        }
                        catch (Exception e)
                        {
                            exceptions[index] = e;
                        }
                    });
                    threads[i].IsBackground = true;
                    threads[i].Start();
                }

                foreach (Thread thread in threads)
                {
                    Assert.True(thread.Join(timeout), "Timed out waiting for handle access thread.");
                }

                Assert.All(exceptions, Assert.Null);
                Assert.All(handles, Assert.NotNull);

                key.Dispose();

                for (int i = 0; i < handles.Length - 1; i++)
                {
                    handles[i]!.Dispose();
                }

                using SafeNCryptKeyHandle remainingHandle = handles[^1]!;
                using CngKey remainingKey = CngKey.Open(
                    remainingHandle,
                    CngKeyHandleOpenOptions.EphemeralKey);
                Assert.Equal(CngAlgorithm.ECDsaP256, remainingKey.Algorithm);
            }
        }
    }
}
