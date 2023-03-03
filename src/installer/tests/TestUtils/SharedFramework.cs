// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
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
