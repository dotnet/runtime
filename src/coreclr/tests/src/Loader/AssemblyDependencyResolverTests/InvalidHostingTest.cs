// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Runtime.Loader;
using Xunit;

namespace AssemblyDependencyResolverTests
{
    class InvalidHostingTest : TestBase
    {
        private string _componentDirectory;
        private string _componentAssemblyPath;
        private string _officialHostPolicyPath;
        private string _localHostPolicyPath;
        private string _renamedHostPolicyPath;

        protected override void Initialize()
        {
            // Make sure there's no hostpolicy available
            _officialHostPolicyPath = HostPolicyMock.DeleteExistingHostpolicy(CoreRoot);
            string hostPolicyFileName = XPlatformUtils.GetStandardNativeLibraryFileName("hostpolicy");
            _localHostPolicyPath = Path.Combine(TestBasePath, hostPolicyFileName);
            _renamedHostPolicyPath = Path.Combine(TestBasePath, hostPolicyFileName + "_renamed");
            if (File.Exists(_renamedHostPolicyPath))
            {
                File.Delete(_renamedHostPolicyPath);
            }
            File.Move(_localHostPolicyPath, _renamedHostPolicyPath);

            _componentDirectory = Path.Combine(TestBasePath, $"InvalidHostingComponent_{Guid.NewGuid().ToString().Substring(0, 8)}");
            Directory.CreateDirectory(_componentDirectory);
            _componentAssemblyPath = Path.Combine(_componentDirectory, "InvalidHostingComponent.dll");
            File.WriteAllText(_componentAssemblyPath, "Mock assembly");
        }

        protected override void Cleanup()
        {
            if (Directory.Exists(_componentDirectory))
            {
                Directory.Delete(_componentDirectory, recursive: true);
            }

            if (File.Exists(_renamedHostPolicyPath))
            {
                File.Move(_renamedHostPolicyPath, _localHostPolicyPath);
            }
        }

        public void TestMissingHostPolicy()
        {
            object innerException = Assert.Throws<InvalidOperationException>(() =>
            {
                AssemblyDependencyResolver resolver = new AssemblyDependencyResolver(
                    Path.Combine(TestBasePath, _componentAssemblyPath));
            }).InnerException;

            Assert.IsType<DllNotFoundException>(innerException);
        }

        // Note: No good way to test the missing entry point case where hostpolicy.dll
        // exists, but it doesn't have the right entry points.
        // Loading a "wrong" hostpolicy.dll into the process is non-revertable operation
        // so we would not be able to run other tests along side this one.
        // Having a standalone .exe just for that one test is not worth it.
    }
}
