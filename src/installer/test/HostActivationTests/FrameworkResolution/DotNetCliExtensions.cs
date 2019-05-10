// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public static class DotNetCliExtensions
    {
        public static DotNetCliCustomizer Customize(this DotNetCli dotnet)
        {
            return new DotNetCliCustomizer(dotnet);
        }

        public class DotNetCliCustomizer : IDisposable
        {
            private readonly DotNetCli _dotnet;
            private readonly TestFileBackup _backup;

            public DotNetCliCustomizer(DotNetCli dotnet)
            {
                _dotnet = dotnet;
                _backup = new TestFileBackup(dotnet.BinPath);
            }

            public void Dispose()
            {
                _backup.Dispose();
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

                return new DotNetFramework(_backup, Path.Combine(path, version));
            }
        }

        public class DotNetFramework
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
