// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestProject : TestArtifact
    {
        public string ProjectDirectory { get => Location; }
        public string ProjectName { get => Name; }

        public string AssemblyName { get; private set; }
        public string OutputDirectory { get; set; }
        public string ProjectFile { get; private set; }
        public string ProjectAssetsJson { get; private set; }
        public string RuntimeConfigJson { get => BuiltApp?.RuntimeConfigJson; }
        public string RuntimeDevConfigJson { get => BuiltApp?.RuntimeDevConfigJson; }
        public string DepsJson { get => BuiltApp?.DepsJson; }
        public string AppDll { get => BuiltApp?.AppDll; }
        public string AppExe { get => BuiltApp?.AppExe; }
        public string HostPolicyDll { get => BuiltApp?.HostPolicyDll; }
        public string HostFxrDll { get => BuiltApp?.HostFxrDll; }
        public string CoreClrDll { get => BuiltApp?.CoreClrDll; }

        public TestApp BuiltApp { get; private set; }

        public TestProject(
            string projectDirectory,
            string outputDirectory = null,
            string assemblyName = null)
            : base(projectDirectory)
        {
            Initialize(outputDirectory, assemblyName);
        }

        public TestProject(TestProject source)
            : base(source)
        {
            Initialize(null, source.AssemblyName);
        }

        public TestProject Copy()
        {
            return new TestProject(this);
        }

        private void Initialize(string outputDirectory, string assemblyName)
        {
            AssemblyName = assemblyName ?? ProjectName;
            ProjectFile = Path.Combine(ProjectDirectory, $"{ProjectName}.csproj");
            ProjectAssetsJson = Path.Combine(ProjectDirectory, "obj", "project.assets.json");

            OutputDirectory = outputDirectory ?? Path.Combine(ProjectDirectory, "bin");
            if (Directory.Exists(OutputDirectory))
            {
                LoadOutputFiles();
            }
        }

        public void LoadOutputFiles()
        {
            BuiltApp = new TestApp(OutputDirectory, AssemblyName);
        }

        public bool IsRestored()
        {
            if (string.IsNullOrEmpty(ProjectAssetsJson))
            {
                return false;
            }

            return File.Exists(ProjectAssetsJson);
        }
    }
}
