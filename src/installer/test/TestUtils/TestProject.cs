using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestProject
    {
        private string _projectDirectory;
        private string _projectName;
        private string _outputDirectory;
        private string _exeExtension;
        private string _sharedLibraryExtension;
        private string _sharedLibraryPrefix;

        private string _projectJson;
        private string _projectLockJson;
        private string _runtimeConfigJson;
        private string _depsJson;
        private string _appDll;
        private string _appExe;
        private string _hostPolicyDll;
        private string _hostFxrDll;

        public string ProjectDirectory => _projectDirectory;
        public string ProjectName => _projectName;

        public string OutputDirectory { get { return _outputDirectory; } set { _outputDirectory = value; } }
        public string ExeExtension { get { return _exeExtension; } set { _exeExtension = value; } }
        public string ProjectJson { get { return _projectJson; } set { _projectJson = value; } }
        public string ProjectLockJson { get { return _projectLockJson; } set { _projectLockJson = value; } }
        public string RuntimeConfigJson { get { return _runtimeConfigJson; } set { _runtimeConfigJson = value; } }
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
            string outputDirectory = null)
        {
            _projectDirectory = projectDirectory;
            _exeExtension = exeExtension;
            _sharedLibraryExtension = sharedLibraryExtension;
            _sharedLibraryPrefix = sharedLibraryPrefix;
            _projectName = Path.GetFileName(_projectDirectory);
            _projectJson = Path.Combine(_projectDirectory, $"{_projectName}.csproj");
            _projectLockJson = Path.Combine(_projectDirectory, "obj", "project.assets.json");

            _outputDirectory = outputDirectory ?? Path.Combine(_projectDirectory, "bin");
            if (Directory.Exists(_outputDirectory))
            {
                LoadOutputFiles();
            }
        }

        public void CopyProjectFiles(string directory)
        {
            CopyRecursive(_projectDirectory, directory, overwrite: true);
        }

        public void LoadOutputFiles()
        {
            _appDll = Path.Combine(_outputDirectory, $"{_projectName}.dll");
            _appExe = Path.Combine(_outputDirectory, $"{_projectName}{_exeExtension}");
            _depsJson = Path.Combine(_outputDirectory, $"{_projectName}.deps.json");
            _runtimeConfigJson = Path.Combine(_outputDirectory, $"{_projectName}.runtimeconfig.json");
            _hostPolicyDll = Path.Combine(_outputDirectory, $"{_sharedLibraryPrefix}hostpolicy{_sharedLibraryExtension}");
            _hostFxrDll = Path.Combine(_outputDirectory, $"{_sharedLibraryPrefix}hostfxr{_sharedLibraryExtension}");
        }

        public bool IsRestored()
        {
            if (string.IsNullOrEmpty(_projectLockJson))
            {
                return false;
            }

            return File.Exists(_projectLockJson);
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
    }
}
