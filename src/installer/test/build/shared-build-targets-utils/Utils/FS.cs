using System.IO;
using System.Runtime.InteropServices;
using System;

using Microsoft.DotNet.Cli.Build.Framework;

using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public static class FS
    {
        public static void Mkdirp(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void Rm(string file)
        {
            if(File.Exists(file))
            {
                File.Delete(file);
            }
        }

        public static void Rmdir(string dir)
        {
            if(Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        public static void RmFilesInDirRecursive(string dir, string filePattern)
        {
            var files = Directory.EnumerateFiles(dir, filePattern, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                FS.Rm(file);
            }
        }

        public static void Chmod(string file, string mode, bool recursive = false)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (recursive)
                {
                    Command.Create("chmod", "-R", mode, file).Execute().EnsureSuccessful();
                }
                else
                {
                    Command.Create("chmod", mode, file).Execute().EnsureSuccessful();
                }
            }
        }

        public static void ChmodAll(string searchDir, string pattern, string mode)
        {
            Exec("find", searchDir, "-type", "f", "-name", pattern, "-exec", "chmod", mode, "{}", ";");
        }

        public static void FixModeFlags(string dir)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Managed code doesn't need 'x'
                ChmodAll(dir, "*.dll", "644");
                ChmodAll(dir, "*.exe", "644");

                // Generally, dylibs and sos have 'x' (no idea if it's required ;))
                // (No need to condition this on OS since there shouldn't be any dylibs on Linux,
                // but even if they are we may as well set their mode flags :))
                ChmodAll(dir, "*.dylib", "755");
                ChmodAll(dir, "*.so", "755");

                // Executables (those without dots) are executable :)
                Exec("find", dir, "-type", "f", "!", "-name", "*.*", "-exec", "chmod", "755", "{}", ";");
            }
        }

        public static void CopyRecursive(string sourceDirectory, string destinationDirectory, bool overwrite = false)
        {
            Mkdirp(destinationDirectory);

            foreach(var dir in Directory.EnumerateDirectories(sourceDirectory))
            {
                CopyRecursive(dir, Path.Combine(destinationDirectory, Path.GetFileName(dir)), overwrite);
            }

            foreach(var file in Directory.EnumerateFiles(sourceDirectory))
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

        public static void CleanBinObj(BuildTargetContext c, string dir)
        {
            dir = dir ?? c.BuildContext.BuildDirectory;
            foreach(var candidate in Directory.EnumerateDirectories(dir))
            {
                if (string.Equals(Path.GetFileName(candidate), "bin") ||
                    string.Equals(Path.GetFileName(candidate), "Bin") ||
                    string.Equals(Path.GetFileName(candidate), "obj"))
                {
                    Utils.DeleteDirectory(candidate);
                }
                else
                {
                    CleanBinObj(c, candidate);
                }
            }
        }
    }
}
