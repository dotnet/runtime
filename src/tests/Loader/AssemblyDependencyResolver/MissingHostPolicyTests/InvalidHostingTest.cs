// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Runtime.Loader;
using Xunit;

namespace AssemblyDependencyResolverTests
{
    public class InvalidHostingTest
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                string assemblyLocation = typeof(InvalidHostingTest).Assembly.Location;
                string testBasePath = Path.GetDirectoryName(assemblyLocation);
                string componentDirectory = Path.Combine(testBasePath, $"InvalidHostingComponent_{Guid.NewGuid().ToString().Substring(0, 8)}");
                Directory.CreateDirectory(componentDirectory);
                string componentAssemblyPath = Path.Combine(componentDirectory, "InvalidHostingComponent.dll");
                File.WriteAllText(componentAssemblyPath, "Mock assembly");

                object innerException = Assert.Throws<InvalidOperationException>(() =>
                {
                    AssemblyDependencyResolver resolver = new AssemblyDependencyResolver(
                        Path.Combine(testBasePath, componentAssemblyPath));
                }).InnerException;

                Assert.IsType<DllNotFoundException>(innerException);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 101;
            }
            return 100;
        }
    }
}
