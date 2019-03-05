using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestProject : IDisposable
    {
        public string ProjectDirectory { get; }
        public string ProjectName { get; }

        public string OutputDirectory { get; set; }
        public string AssemblyName { get; set; }
        public string ProjectFile { get; set; }
        public string ProjectAssetsJson { get; set; }
        public string RuntimeConfigJson { get; set; }
        public string RuntimeDevConfigJson { get; set; }
        public string DepsJson { get; set; }
        public string AppDll { get; set; }
        public string AppExe { get; set; }
        public string HostPolicyDll { get; set; }
        public string HostFxrDll { get; set; }

        public TestProject(
            string projectDirectory,
            string outputDirectory = null,
            string assemblyName = null)
        {
            ProjectDirectory = projectDirectory;
            ProjectName = Path.GetFileName(ProjectDirectory);
            AssemblyName = assemblyName ?? ProjectName;
            ProjectFile = Path.Combine(ProjectDirectory, $"{ProjectName}.csproj");
            ProjectAssetsJson = Path.Combine(ProjectDirectory, "obj", "project.assets.json");

            OutputDirectory = outputDirectory ?? Path.Combine(ProjectDirectory, "bin");
            if (Directory.Exists(OutputDirectory))
            {
                LoadOutputFiles();
            }
        }

        public void Dispose()
        {
            if (!PreserveTestRuns())
            {
                Directory.Delete(ProjectDirectory, true);
            }
        }

        public void CopyProjectFiles(string directory)
        {
            CopyRecursive(ProjectDirectory, directory, overwrite: true);
        }

        public void LoadOutputFiles()
        {
            AppDll = Path.Combine(OutputDirectory, $"{AssemblyName}.dll");
            AppExe = Path.Combine(OutputDirectory, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform(AssemblyName));
            DepsJson = Path.Combine(OutputDirectory, $"{AssemblyName}.deps.json");
            RuntimeConfigJson = Path.Combine(OutputDirectory, $"{AssemblyName}.runtimeconfig.json");
            RuntimeDevConfigJson = Path.Combine(OutputDirectory, $"{AssemblyName}.runtimeconfig.dev.json");
            HostPolicyDll = Path.Combine(OutputDirectory, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy"));
            HostFxrDll = Path.Combine(OutputDirectory, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr"));
        }

        public bool IsRestored()
        {
            if (string.IsNullOrEmpty(ProjectAssetsJson))
            {
                return false;
            }

            return File.Exists(ProjectAssetsJson);
        }

        private void CopyRecursive(string sourceDirectory, string destinationDirectory, bool overwrite = false)
        {
            if ( ! Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            foreach (var dir in Directory.EnumerateDirectories(sourceDirectory))
            {
                CopyRecursive(dir, Path.Combine(destinationDirectory, Path.GetFileName(dir)), overwrite);
            }

            foreach (var file in Directory.EnumerateFiles(sourceDirectory))
            {
                var dest = Path.Combine(destinationDirectory, Path.GetFileName(file));
                if (!File.Exists(dest) || overwrite)
                {
                    // We say overwrite true, because we only get here if the file didn't exist (thus it doesn't matter) or we
                    // wanted to overwrite :)
                    File.Copy(file, dest, overwrite: true);
                }
            }
        }

        public static bool PreserveTestRuns()
        {
            return Environment.GetEnvironmentVariable("PRESERVE_TEST_RUNS") == "1";
        }
    }
}
