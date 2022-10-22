// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace Microsoft.DotNet.CoreSetup.Test
{
    /// <summary>
    /// Helper class for creating, modifying and cleaning up shared frameworks
    /// </summary>
    public static class SharedFramework
    {
        private static readonly Mutex id_mutex = new Mutex();

        // MultilevelDirectory is %TEST_ARTIFACTS%\dotnetMultilevelSharedFxLookup\id.
        // We must locate the first non existing id.
        public static string CalculateUniqueTestDirectory(string baseDir)
        {
            id_mutex.WaitOne();

            int count = 0;
            string dir;

            do
            {
                dir = Path.Combine(baseDir, count.ToString());
                count++;
            } while (Directory.Exists(dir));

            id_mutex.ReleaseMutex();

            return dir;
        }

        // This method adds a list of new framework version folders in the specified
        // sharedFxBaseDir. The files are copied from the _buildSharedFxDir.
        // Remarks:
        // - If the sharedFxBaseDir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        // - If a specified version folder already exists, then it is deleted and replaced
        //   with the contents of the _builtSharedFxDir.
        public static void AddAvailableSharedFxVersions(string sharedFxDir, string sharedFxBaseDir, params string[] availableVersions)
        {
            DirectoryInfo sharedFxBaseDirInfo = new DirectoryInfo(sharedFxBaseDir);

            if (!sharedFxBaseDirInfo.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            foreach (string version in availableVersions)
            {
                string newSharedFxDir = Path.Combine(sharedFxBaseDir, version);
                CopyDirectory(sharedFxDir, newSharedFxDir);
            }
        }

        // This method adds a list of new framework version folders in the specified
        // sharedFxUberBaseDir. A runtimeconfig file is created that references
        // Microsoft.NETCore.App version=sharedFxBaseVersion
        public static void AddAvailableSharedUberFxVersions(string sharedFxDir, string sharedUberFxBaseDir, string sharedFxBaseVersion, params string[] availableUberVersions)
        {
            DirectoryInfo sharedFxUberBaseDirInfo = new DirectoryInfo(sharedUberFxBaseDir);

            if (!sharedFxUberBaseDirInfo.Exists)
            {
                sharedFxUberBaseDirInfo.Create();
            }

            foreach (string version in availableUberVersions)
            {
                string newSharedFxDir = Path.Combine(sharedUberFxBaseDir, version);
                CopyDirectory(sharedFxDir, newSharedFxDir);

                string runtimeBaseConfig = Path.Combine(newSharedFxDir, "Microsoft.UberFramework.runtimeconfig.json");
                SharedFramework.SetRuntimeConfigJson(runtimeBaseConfig, sharedFxBaseVersion, null);
            }
        }

        // Generated json file:
        /*
         * {
         *   "runtimeOptions": {
         *     "framework": {
         *       "name": "Microsoft.NETCore.App",
         *       "version": {version}
         *     },
         *     "rollForwardOnNoCandidateFx": {rollFwdOnNoCandidateFx} <-- only if rollFwdOnNoCandidateFx is defined
         *   }
         * }
        */
        public static void SetRuntimeConfigJson(string destFile, string version, int? rollFwdOnNoCandidateFx = null, bool? useUberFramework = false, JsonArray frameworks = null)
        {
            string name = useUberFramework.HasValue && useUberFramework.Value ? "Microsoft.UberFramework" : "Microsoft.NETCore.App";

            JsonObject runtimeOptions = new JsonObject
            {
                ["framework"] = new JsonObject
                {
                    ["name"] = name,
                    ["version"] = version
                }
            };

            if (rollFwdOnNoCandidateFx.HasValue)
            {
                runtimeOptions.Add("rollForwardOnNoCandidateFx", rollFwdOnNoCandidateFx);
            }

            if (frameworks != null)
            {
                runtimeOptions.Add("frameworks", frameworks);
            }

            FileInfo file = new FileInfo(destFile);
            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            JsonObject json = new JsonObject();
            json.Add("runtimeOptions", runtimeOptions);
            File.WriteAllText(destFile, json.ToString());
        }

        // CopyDirectory recursively copies a directory
        // Remarks:
        // - If the dest dir does not exist, then it is created.
        // - If the dest dir exists, then it is substituted with the new one
        //   (original files and subfolders are deleted).
        // - If the src dir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        public static void CopyDirectory(string srcDir, string dstDir)
        {
            DirectoryInfo srcDirInfo = new DirectoryInfo(srcDir);

            if (!srcDirInfo.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            DirectoryInfo dstDirInfo = new DirectoryInfo(dstDir);

            if (dstDirInfo.Exists)
            {
                dstDirInfo.Delete(true);
            }

            dstDirInfo.Create();

            foreach (FileInfo fileInfo in srcDirInfo.GetFiles())
            {
                string newFile = Path.Combine(dstDir, fileInfo.Name);
                fileInfo.CopyTo(newFile);
            }

            foreach (DirectoryInfo subdirInfo in srcDirInfo.GetDirectories())
            {
                string newDir = Path.Combine(dstDir, subdirInfo.Name);
                CopyDirectory(subdirInfo.FullName, newDir);
            }
        }

        public static void CreateUberFrameworkArtifacts(string builtSharedFxDir, string builtSharedUberFxDir, string assemblyVersion = null, string fileVersion = null)
        {
            DirectoryInfo dir = new DirectoryInfo(builtSharedUberFxDir);
            if (dir.Exists)
            {
                dir.Delete(true);
            }

            dir.Create();

            JsonObject versionInfo = new JsonObject();
            if (assemblyVersion != null)
            {
                versionInfo.Add("assemblyVersion", (JsonNode)assemblyVersion);
            }

            if (fileVersion != null)
            {
                versionInfo.Add("fileVersion", (JsonNode)fileVersion);
            }

            JsonObject depsjson = CreateDepsJson("UberFx", "System.Collections.Immutable/1.0.0", "System.Collections.Immutable", versionInfo);
            string depsFile = Path.Combine(builtSharedUberFxDir, "Microsoft.UberFramework.deps.json");
            File.WriteAllText(depsFile, depsjson.ToString());

            // Copy the test assembly
            string fileSource = Path.Combine(builtSharedFxDir, "System.Collections.Immutable.dll");
            string fileDest = Path.Combine(builtSharedUberFxDir, "System.Collections.Immutable.dll");
            File.Copy(fileSource, fileDest);
        }

        public static JsonObject CreateDepsJson(string fxName, string testPackage, string testAssembly, JsonObject versionInfo = null)
        {
            // Create the deps.json. Generated file (example)
            /*
                {
                  "runtimeTarget": {
                    "name": "UberFx"
                  },
                  "targets": {
                    "UberFx": {
                      "System.Collections.Immutable/1.0.0": {
                        "dependencies": {}
                        "runtime": {
                          "System.Collections.Immutable.dll": {}
                        }
                      }
                    }
                  },
                  "libraries": {
                    "System.Collections.Immutable/1.0.0": {
                      "type": "assemblyreference",
                      "serviceable": false,
                      "sha512": ""
                    }
                  }
                }
             */

            if (versionInfo == null)
            {
                versionInfo = new JsonObject();
            }

            JsonObject depsjson = new JsonObject
            {
                ["runtimeTarget"] = new JsonObject
                {
                    ["name"] = fxName
                },
                ["targets"] = new JsonObject
                {
                    [fxName] = new JsonObject
                    {
                        [testPackage] = new JsonObject
                        {
                            ["dependencies"] = new JsonObject(),
                            ["runtime"] = new JsonObject
                            {
                                [testAssembly + ".dll"] = versionInfo
                            }
                        }
                    }
                },
                ["libraries"] = new JsonObject
                {
                    [testPackage] = new JsonObject
                    {
                        ["type"] = "assemblyreference",
                        ["serviceable"] = false,
                        ["sha512"] = ""
                    }
                }
            };

            return depsjson;
        }

        public static void AddReferenceToDepsJson(
            string jsonFile, 
            string fxNameWithVersion, 
            string testPackage, 
            string testPackageVersion, 
            JsonObject testAssemblyVersionInfo = null,
            string testAssembly = null)
        {
            JsonObject depsjson = (JsonObject)JsonObject.Parse(File.ReadAllText(jsonFile));

            string testPackageWithVersion = testPackage + "/" + testPackageVersion;
            testAssembly = testAssembly ?? (testPackage + ".dll");

            JsonObject targetsValue = (JsonObject)depsjson["targets"].AsObject().First().Value;

            JsonObject packageDependencies = (JsonObject)targetsValue[fxNameWithVersion]["dependencies"];
            packageDependencies.Add(testPackage, (JsonNode)testPackageVersion);

            if (testAssemblyVersionInfo == null)
            {
                testAssemblyVersionInfo = new JsonObject();
            }

            targetsValue.Add(testPackageWithVersion, new JsonObject
            {
                ["runtime"] = new JsonObject
                {
                    [testAssembly] = testAssemblyVersionInfo
                }
            });

            JsonObject libraries = (JsonObject)depsjson["libraries"];
            libraries.Add(testPackageWithVersion, new JsonObject
            {
                ["type"] = "assemblyreference",
                ["serviceable"] = false,
                ["sha512"] = ""
            });

            File.WriteAllText(jsonFile, depsjson.ToString());
        }
    }
}
