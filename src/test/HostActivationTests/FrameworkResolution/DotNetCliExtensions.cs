// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    internal static class DotNetCliExtensions
    {
        public static DotNetCliCustomizer Customize(this DotNetCli dotnet)
        {
            return new DotNetCliCustomizer(dotnet);
        }

        internal interface ITestFileBackup
        {
            void Backup(string path);
        }

        internal class DotNetCliCustomizer : IDisposable, ITestFileBackup
        {
            private readonly DotNetCli _dotnet;
            private readonly string _basePath;
            private readonly string _backupPath;

            public DotNetCliCustomizer(DotNetCli dotnet)
            {
                _dotnet = dotnet;
                _basePath = Path.GetFullPath(dotnet.BinPath);
                _backupPath = Path.Combine(_basePath, ".test.backup");

                if (Directory.Exists(_backupPath))
                {
                    throw new Exception($"The backup directory already exists. Please make sure that all customizers are correctly disposed.");
                }
            }

            void ITestFileBackup.Backup(string path)
            {
                path = Path.GetFullPath(path);
                if (!path.StartsWith(_basePath))
                {
                    throw new Exception($"Trying to backup file {path} which is outside of the backup root {_basePath}.");
                }

                if (!Directory.Exists(_backupPath))
                {
                    Directory.CreateDirectory(_backupPath);
                }

                string backupFile = Path.Combine(_backupPath, path.Substring(_basePath.Length + 1));
                string containingDirectory = Path.GetDirectoryName(backupFile);
                if (!Directory.Exists(containingDirectory))
                {
                    Directory.CreateDirectory(containingDirectory);
                    if (!File.Exists(backupFile))
                    {
                        File.Copy(path, backupFile);
                    }
                }
            }

            public void Dispose()
            {
                if (Directory.Exists(_backupPath))
                {
                    CopyOverDirectory(_backupPath, _dotnet.BinPath);

                    Directory.Delete(_backupPath, recursive: true);
                }
            }

            private static void CopyOverDirectory(string source, string destination)
            {
                foreach (string directory in Directory.GetDirectories(source))
                {
                    CopyOverDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
                }

                foreach (string file in Directory.GetFiles(source))
                {
                    File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
                }
            }

            public DotNetFramework Framework(string name, string version = null)
            {
                string path = Path.Combine(_dotnet.BinPath, "shared", name);
                IEnumerable<string> versions =
                    Directory.Exists(path) ? Directory.GetDirectories(path) : Enumerable.Empty<string>();

                if (version == null)
                {
                    version = versions.FirstOrDefault();
                    if (versions.Skip(1).Any())
                    {
                        throw new Exception($"Multiple versions of framework {name} found, but no version selector specified.");
                    }
                }
                else
                {
                    version = versions.FirstOrDefault(v => v == version);
                }

                if (version == null)
                {
                    throw new Exception($"No framework {name}, version {version} found.");
                }

                return new DotNetFramework(this, Path.Combine(path, version));
            }
        }

        internal class DotNetFramework
        {
            private readonly ITestFileBackup _backup;
            private readonly string _path;
            private readonly string _name;

            internal DotNetFramework(ITestFileBackup backup, string path)
            {
                _backup = backup;
                _path = path;
                _name = Path.GetFileName(Path.GetDirectoryName(path));
            }

            public DotNetFramework RuntimeConfig(Action<RuntimeConfig> runtimeConfigCustomizer)
            {
                string runtimeConfigPath = Path.Combine(_path, _name + ".runtimeconfig.json");
                _backup.Backup(runtimeConfigPath);
                RuntimeConfig runtimeConfig = HostActivation.RuntimeConfig.FromFile(runtimeConfigPath);
                runtimeConfigCustomizer(runtimeConfig);
                runtimeConfig.Save();

                return this;
            }
        }
    }
}
