using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestProject : IDisposable
    {
        private string _projectDirectory;
        private string _projectName;
        private string _outputDirectory;
        private string _exeExtension;
        private string _sharedLibraryExtension;
        private string _sharedLibraryPrefix;

        private string _projectFile;
        private string _projectAssetsJson;
        private string _runtimeConfigJson;
        private string _runtimeDevConfigJson;
        private string _depsJson;
        private string _appDll;
        private string _appExe;
        private string _hostPolicyDll;
        private string _hostFxrDll;
        private string _assemblyName;

        public string ProjectDirectory => _projectDirectory;
        public string ProjectName => _projectName;

        public string OutputDirectory { get { return _outputDirectory; } set { _outputDirectory = value; } }
        public string ExeExtension { get { return _exeExtension; } set { _exeExtension = value; } }
        public string AssemblyName { get { return _assemblyName; } set { _assemblyName = value; } }
        public string ProjectFile { get { return _projectFile; } set { _projectFile = value; } }
        public string ProjectAssetsJson { get { return _projectAssetsJson; } set { _projectAssetsJson = value; } }
        public string RuntimeConfigJson { get { return _runtimeConfigJson; } set { _runtimeConfigJson = value; } }
        public string RuntimeDevConfigJson { get { return _runtimeDevConfigJson; } set { _runtimeDevConfigJson = value; } }
        public string DepsJson { get { return _depsJson; } set { _depsJson = value; } }
        public string AppDll { get { return _appDll; } set { _appDll = value; } }
        public string AppExe { get { return _appExe; } set { _appExe = value; } }
        public string HostPolicyDll { get { return _hostPolicyDll; } set { _hostPolicyDll = value; } }
        public string HostFxrDll { get { return _hostFxrDll; } set { _hostFxrDll = value; } }

        public TestProject(
            string projectDirectory,
            string exeExtension,
            string sharedLibraryExtension,
            string sharedLibraryPrefix,
            string outputDirectory = null,
            string assemblyName = null)
        {
            _projectDirectory = projectDirectory;
            _exeExtension = exeExtension;
            _sharedLibraryExtension = sharedLibraryExtension;
            _sharedLibraryPrefix = sharedLibraryPrefix;
            _projectName = Path.GetFileName(_projectDirectory);
            _assemblyName = assemblyName ?? _projectName;
            _projectFile = Path.Combine(_projectDirectory, $"{_projectName}.csproj");
            _projectAssetsJson = Path.Combine(_projectDirectory, "obj", "project.assets.json");

            _outputDirectory = outputDirectory ?? Path.Combine(_projectDirectory, "bin");
            if (Directory.Exists(_outputDirectory))
            {
                LoadOutputFiles();
            }
        }

        public void Dispose()
        {
            if (!PreserveTestRuns())
            {
                Directory.Delete(_projectDirectory, true);
            }
        }

        public void CopyProjectFiles(string directory)
        {
            CopyRecursive(_projectDirectory, directory, overwrite: true);
        }

        public void LoadOutputFiles()
        {
            _appDll = Path.Combine(_outputDirectory, $"{_assemblyName}.dll");
            _appExe = Path.Combine(_outputDirectory, $"{_assemblyName}{_exeExtension}");
            _depsJson = Path.Combine(_outputDirectory, $"{_assemblyName}.deps.json");
            _runtimeConfigJson = Path.Combine(_outputDirectory, $"{_assemblyName}.runtimeconfig.json");
            _runtimeDevConfigJson = Path.Combine(_outputDirectory, $"{_assemblyName}.runtimeconfig.dev.json");
            _hostPolicyDll = Path.Combine(_outputDirectory, $"{_sharedLibraryPrefix}hostpolicy{_sharedLibraryExtension}");
            _hostFxrDll = Path.Combine(_outputDirectory, $"{_sharedLibraryPrefix}hostfxr{_sharedLibraryExtension}");
        }

        public bool IsRestored()
        {
            if (string.IsNullOrEmpty(_projectAssetsJson))
            {
                return false;
            }

            return File.Exists(_projectAssetsJson);
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
