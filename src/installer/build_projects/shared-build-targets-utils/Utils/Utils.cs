using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public static class Utils
    {

        public static bool IsCrossArchRID(string rid) {
        if (!String.IsNullOrEmpty(rid))
        {
            return (String.Compare(rid, "win8-arm", true) == 0) 
                    || (String.Compare(rid, "win-arm", true) == 0)
                    || (String.Compare(rid, "win-arm64", true) == 0)
                    || (String.Compare(rid, "win10-arm64", true) == 0)
                    || (String.Compare(rid, "linux-arm", true) == 0)
                    || (String.Compare(rid, "ubuntu.14.04-arm", true) == 0)
                    || (String.Compare(rid, "ubuntu.16.04-arm", true) == 0)
                    || (rid.EndsWith("-armel"));
        }
        return false;
        }
        public static bool IsPortableRID(string rid, out string portablePlatformID)
        {
            bool fIsPortable = false;
            portablePlatformID = null;

            Dictionary<string, string> portablePlatformIDList = new Dictionary<string, string>()
             {
                 { "linux-", "linux" },
                 { "win-", "win" },
                 { "osx-", "osx" }
             };

            foreach(var platformRID in portablePlatformIDList)
            {
                if (rid.StartsWith(platformRID.Key))
                {
                    portablePlatformID = platformRID.Value;
                    fIsPortable = true;
                    break;
                }
            }
            
            return fIsPortable;
        }

        public static void CleanNuGetTempCache()
        {
            // Clean NuGet Temp Cache on Linux (seeing some issues on Linux)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Directory.Exists("/tmp/NuGet"))
            {
                Directory.Delete("/tmp/NuGet", recursive: true);
            }
        }

        public static string GetOSName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        // Generate a Version 5 (SHA1 Name Based) Guid from a name.
        public static Guid GenerateGuidFromName(string name)
        {
            // Any fixed GUID will do for a namespace.
            Guid namespaceId = new Guid("28F1468D-672B-489A-8E0C-7C5B3030630C");

            using (SHA1 hasher = SHA1.Create())
            {
                var nameBytes = System.Text.Encoding.UTF8.GetBytes(name ?? string.Empty);
                var namespaceBytes = namespaceId.ToByteArray();

                SwapGuidByteOrder(namespaceBytes);

                var streamToHash = new byte[namespaceBytes.Length + nameBytes.Length];

                Array.Copy(namespaceBytes, streamToHash, namespaceBytes.Length);
                Array.Copy(nameBytes, 0, streamToHash, namespaceBytes.Length, nameBytes.Length);

                var hashResult = hasher.ComputeHash(streamToHash);

                var res = new byte[16];

                Array.Copy(hashResult, res, res.Length);

                unchecked { res[6] = (byte)(0x50 | (res[6] & 0x0F)); }
                unchecked { res[8] = (byte)(0x40 | (res[8] & 0x3F)); }

                SwapGuidByteOrder(res);

                return new Guid(res);
            }
        }

        // Do a byte order swap, .NET GUIDs store multi byte components in little
        // endian.
        private static void SwapGuidByteOrder(byte[] b)
        {
            Swap(b, 0, 3);
            Swap(b, 1, 2);
            Swap(b, 5, 6);
            Swap(b, 7, 8);
        }

        private static void Swap(byte[] b, int x, int y)
        {
            byte t = b[x];
            b[x] = b[y];
            b[y] = t;
        }

        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                var retry = 5;
                while (retry >= 0)
                {
                    try
                    {
                        Directory.Delete(path, true);
                        return;
                    }
                    catch (IOException)
                    {
                        if (retry == 0)
                        {
                            throw;
                        }
                        System.Threading.Thread.Sleep(200);
                        retry--;
                    }
                }
            }
        }

        public static void CopyDirectoryRecursively(string path, string destination, bool keepParentDir = false)
        {
            if (keepParentDir)
            {
                path = path.TrimEnd(Path.DirectorySeparatorChar);
                destination = Path.Combine(destination, Path.GetFileName(path));
                Directory.CreateDirectory(destination);
            }

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                string destFile = file.Replace(path, destination);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile, true);
            }
        }

        public static string GetVersionFileContent(string commitHash, string version)
        {
            return $@"{commitHash}{Environment.NewLine}{version}{Environment.NewLine}";
        }

        public static string GetSharedFrameworkVersionFileContent(BuildTargetContext c)
        {
            string SharedFrameworkNugetVersion = c.BuildContext.Get<string>("SharedFrameworkNugetVersion");
            return $@"{c.BuildContext["CommitHash"]}{Environment.NewLine}{SharedFrameworkNugetVersion}{Environment.NewLine}";
        }
    }
}
