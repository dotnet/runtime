// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Assert = Xunit.Assert;

namespace BinderTracingTests
{
    class BinderTracingTest
    {
        public static void PlatformAssembly_DefaultALC()
        {
            Console.WriteLine($"Running {nameof(PlatformAssembly_DefaultALC)}...");
            using (var listener = new BinderEventListener())
            {
                string assemblyName = "System.Xml";
                Assembly asm = Assembly.Load(assemblyName);

                BindOperation[] binds = listener.WaitAndGetEventsForAssembly(assemblyName);
                Assert.True(binds.Length == 1, $"Bind count for {assemblyName} - expected: 1, actual: {binds.Length}");
                BindOperation bind = binds[0];
                Assert.True(bind.Success, $"Expected bind for {assemblyName} to succeed");
            }
        }

        public static void NonExistentAssembly_DefaultALC()
        {
            Console.WriteLine($"Running {nameof(NonExistentAssembly_DefaultALC)}...");
            using (var listener = new BinderEventListener())
            {
                string assemblyName = "DoesNotExist";
                try
                {
                    Assembly.Load(assemblyName);
                }
                catch { }

                BindOperation[] binds = listener.WaitAndGetEventsForAssembly(assemblyName);
                Assert.True(binds.Length == 1, $"Bind event count for {assemblyName} - expected: 1, actual: {binds.Length}");
                BindOperation bind = binds[0];
                Assert.False(bind.Success, $"Expected bind for {assemblyName} to fail");
            }
        }

        public static int Main(string[] unused)
        {
            try
            {
                PlatformAssembly_DefaultALC();
                NonExistentAssembly_DefaultALC();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
