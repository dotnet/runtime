// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    /// <summary>
    /// Helper class for creating, modifying and cleaning up shared frameworks
    /// </summary>
    internal static class SharedFramework
    {
        // MultilevelDirectory is %TEST_ARTIFACTS%\dotnetMultilevelSharedFxLookup\id.
        // We must locate the first non existing id.
        public static string CalculateUniqueTestDirectory(string baseDir)
        {
            int count = 0;
            string dir;

            do
            {
                dir = Path.Combine(baseDir, count.ToString());
                count++;
            } while (Directory.Exists(dir));

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
        public static void AddAvailableSharedUberFxVersions(string sharedFxDir, string sharedUberFxBaseDir, string sharedFxBaseVersion, string testConfigPropertyValue = null, params string[] availableUberVersions)
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
                SharedFramework.SetRuntimeConfigJson(runtimeBaseConfig, sharedFxBaseVersion, null, testConfigPropertyValue);
            }
        }

        // This method removes a list of framework version folders from the specified
        // sharedFxBaseDir.
        // Remarks:
        // - If the sharedFxBaseDir does not exist, then a DirectoryNotFoundException
        //   is thrown.
        // - If a specified version folder does not exist, then a DirectoryNotFoundException
        //   is thrown.
        public static void DeleteAvailableSharedFxVersions(string sharedFxBaseDir, params string[] availableVersions)
        {
            DirectoryInfo sharedFxBaseDirInfo = new DirectoryInfo(sharedFxBaseDir);

            if (!sharedFxBaseDirInfo.Exists)
            {
                throw new DirectoryNotFoundException();
            }

            foreach (string version in availableVersions)
            {
                string sharedFxDir = Path.Combine(sharedFxBaseDir, version);
                if (!Directory.Exists(sharedFxDir))
                {
                    throw new DirectoryNotFoundException();
                }
                Directory.Delete(sharedFxDir, true);
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
        public static void SetRuntimeConfigJson(string destFile, string version, int? rollFwdOnNoCandidateFx = null, string testConfigPropertyValue = null, bool? useUberFramework = false, JArray additionalFrameworks = null)
        {
            string name = useUberFramework.HasValue && useUberFramework.Value ? "Microsoft.UberFramework" : "Microsoft.NETCore.App";

            JObject runtimeOptions = new JObject(
                new JProperty("framework",
                    new JObject(
                        new JProperty("name", name),
                        new JProperty("version", version)
                    )
                )
            );

            if (rollFwdOnNoCandidateFx.HasValue)
            {
                runtimeOptions.Add("rollForwardOnNoCandidateFx", rollFwdOnNoCandidateFx);
            }

            if (testConfigPropertyValue != null)
            {
                runtimeOptions.Add(
                    new JProperty("configProperties",
                        new JObject(
                            new JProperty("TestProperty", testConfigPropertyValue)
                        )
                    )
                );
            }

            if (additionalFrameworks != null)
            {
                runtimeOptions.Add("additionalFrameworks", additionalFrameworks);
            }

            FileInfo file = new FileInfo(destFile);
            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            JObject json = new JObject();
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

            JObject versionInfo = new JObject();
            if (assemblyVersion != null)
            {
                versionInfo.Add(new JProperty("assemblyVersion", assemblyVersion));
            }

            if (fileVersion != null)
            {
                versionInfo.Add(new JProperty("fileVersion", fileVersion));
            }

            JObject depsjson = CreateDepsJson("UberFx", "System.Collections.Immutable/1.0.0", "System.Collections.Immutable", versionInfo);
            string depsFile = Path.Combine(builtSharedUberFxDir, "Microsoft.UberFramework.deps.json");
            File.WriteAllText(depsFile, depsjson.ToString());

            // Copy the test assembly
            string fileSource = Path.Combine(builtSharedFxDir, "System.Collections.Immutable.dll");
            string fileDest = Path.Combine(builtSharedUberFxDir, "System.Collections.Immutable.dll");
            File.Copy(fileSource, fileDest);
        }

        public static JObject CreateDepsJson(string fxName, string testPackage, string testAssembly, JObject versionInfo = null)
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
                versionInfo = new JObject();
            }

            JObject depsjson = new JObject(
                new JProperty("runtimeTarget",
                    new JObject(
                        new JProperty("name", fxName)
                    )
                ),
                new JProperty("targets",
                    new JObject(
                      new JProperty(fxName,
                          new JObject(
                              new JProperty(testPackage,
                                  new JObject(
                                      new JProperty("dependencies",
                                        new JObject()
                                      ),
                                      new JProperty("runtime",
                                          new JObject(
                                              new JProperty(testAssembly + ".dll",
                                                  versionInfo
                                              )
                                          )
                                      )
                                  )
                              )
                          )
                      )
                  )
              ),
                  new JProperty("libraries",
                      new JObject(
                          new JProperty(testPackage,
                            new JObject(
                                new JProperty("type", "assemblyreference"),
                                new JProperty("serviceable", false),
                                new JProperty("sha512", "")
                            )
                        )
                    )
                )
            );

            return depsjson;
        }

        public static void AddReferenceToDepsJson(string jsonFile, string fxNamewWithVersion, string testPackage, string testPackageVersion, JObject testAssemblyVersionInfo = null)
        {
            JObject depsjson = JObject.Parse(File.ReadAllText(jsonFile));

            string testPackageWithVersion = testPackage + "/" + testPackageVersion;
            string testAssembly = testPackage + ".dll";

            JProperty targetsProperty = (JProperty)depsjson["targets"].First;
            JObject targetsValue = (JObject)targetsProperty.Value;

            var assembly = new JProperty(testPackage, testPackageVersion);
            JObject packageDependencies = (JObject)targetsValue[fxNamewWithVersion]["dependencies"];
            packageDependencies.Add(assembly);

            if (testAssemblyVersionInfo == null)
            {
                testAssemblyVersionInfo = new JObject();
            }

            var package = new JProperty(testPackageWithVersion,
                new JObject(
                    new JProperty("runtime",
                        new JObject(
                            new JProperty(testAssembly,
                                new JObject(
                                    testAssemblyVersionInfo
                                )
                            )
                        )
                    )
                )
            );

            targetsValue.Add(package);

            var library = new JProperty(testPackageWithVersion,
                new JObject(
                    new JProperty("type", "assemblyreference"),
                    new JProperty("serviceable", false),
                    new JProperty("sha512", "")
                )
            );

            JObject libraries = (JObject)depsjson["libraries"];
            libraries.Add(library);

            File.WriteAllText(jsonFile, depsjson.ToString());
        }
    }
}
