// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestApp : TestArtifact
    {
        public string AppDll { get; private set; }
        public string AppExe { get; private set; }
        public string DepsJson { get; private set; }
        public string RuntimeConfigJson { get; private set; }
        public string RuntimeDevConfigJson { get; private set; }
        public string HostPolicyDll { get; private set; }
        public string HostFxrDll { get; private set; }
        public string CoreClrDll { get; private set; }

        public string AssemblyName { get; }

        public TestApp(string basePath, string assemblyName = null)
            : base(basePath)
        {
            AssemblyName = assemblyName ?? Name;
            LoadAssets();
        }

        public TestApp(TestApp source)
            : base(source)
        {
            AssemblyName = source.AssemblyName;
            LoadAssets();
        }

        public static TestApp CreateEmpty(string name)
        {
            string location = GetNewTestArtifactPath(name);
            return new TestApp(location);
        }

        public TestApp Copy()
        {
            return new TestApp(this);
        }

        private void LoadAssets()
        {
            AppDll = Path.Combine(Location, $"{AssemblyName}.dll");
            AppExe = Path.Combine(Location, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform(AssemblyName));
            DepsJson = Path.Combine(Location, $"{AssemblyName}.deps.json");
            RuntimeConfigJson = Path.Combine(Location, $"{AssemblyName}.runtimeconfig.json");
            RuntimeDevConfigJson = Path.Combine(Location, $"{AssemblyName}.runtimeconfig.dev.json");
            HostPolicyDll = Path.Combine(Location, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy"));
            HostFxrDll = Path.Combine(Location, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr"));
            CoreClrDll = Path.Combine(Location, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("coreclr"));
        }
    }
}
