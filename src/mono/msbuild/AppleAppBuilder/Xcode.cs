// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Text;

internal class Xcode
{
    public static string Sysroot { get; } = Utils.RunProcess("xcrun", "--sdk iphoneos --show-sdk-path");

    public static string GenerateXCode(
        string projectName,
        string entryPointLib,
        string workspace,
        string binDir,
        string monoInclude,
        bool useConsoleUiTemplate = false,
        string? nativeMainSource = null)
    {
        // bundle everything as resources excluding native files
        string[] excludes = {".dylib", ".dll.o", ".dll.s", ".dwarf", ".m", ".h", ".a"};
        string[] resources = Directory.GetFiles(workspace)
            .Where(f => !excludes.Any(e => f.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
            .Concat(Directory.GetFiles(binDir, "*.aotdata"))
            .ToArray();

        if (string.IsNullOrEmpty(nativeMainSource))
        {
            // use built-in main.m (with default UI) if it's not set
            nativeMainSource = Path.Combine(binDir, "main.m");
            File.WriteAllText(nativeMainSource, Utils.GetEmbeddedResource(useConsoleUiTemplate ? "main-console.m" : "main-simple.m"));
        }
        else
        {
            string newMainPath = Path.Combine(binDir, "main.m");
            if (nativeMainSource != newMainPath)
            {
                File.Copy(nativeMainSource, Path.Combine(binDir, "main.m"), true);
                nativeMainSource = newMainPath;
            }
        }

        string cmakeLists = Utils.GetEmbeddedResource("CMakeLists.txt.template")
            .Replace("%ProjectName%", projectName)
            .Replace("%AppResources%", string.Join(Environment.NewLine, resources.Select(r => "    " + r)))
            .Replace("%MainSource%", nativeMainSource)
            .Replace("%MonoInclude%", monoInclude);

        string toLink = "";
        foreach (string lib in Directory.GetFiles(workspace, "*.a"))
        {
            // these libraries are pinvoked
            // -force_load will be removed once we enable direct-pinvokes for AOT
            toLink += $"    \"-force_load {lib}\"{Environment.NewLine}";
        }
        foreach (string lib in Directory.GetFiles(binDir, "*.dll.o"))
        {
            // these libraries are linked via modules.m
            toLink += $"    \"{lib}\"{Environment.NewLine}";
        }
        cmakeLists = cmakeLists.Replace("%NativeLibrariesToLink%", toLink);

        string plist = Utils.GetEmbeddedResource("Info.plist.template")
            .Replace("%BundleIdentifier%", projectName);

        File.WriteAllText(Path.Combine(binDir, "Info.plist.in"), plist);
        File.WriteAllText(Path.Combine(binDir, "CMakeLists.txt"), cmakeLists);

        var cmakeArgs = new StringBuilder();
        cmakeArgs
            .Append("-S.")
            .Append(" -B").Append(projectName)
            .Append(" -GXcode")
            .Append(" -DCMAKE_SYSTEM_NAME=iOS")
            .Append(" \"-DCMAKE_OSX_ARCHITECTURES=arm64;x86_64\"")
            .Append(" -DCMAKE_OSX_DEPLOYMENT_TARGET=10.1")
            .Append(" -DCMAKE_XCODE_ATTRIBUTE_ONLY_ACTIVE_ARCH=NO");

        File.WriteAllText(Path.Combine(binDir, "runtime.h"),
            Utils.GetEmbeddedResource("runtime.h"));

        // forward pinvokes to "__Internal"
        string dllMap = string.Join(Environment.NewLine, Directory.GetFiles(workspace, "*.a")
            .Select(f => $"    mono_dllmap_insert (NULL, \"{Path.GetFileNameWithoutExtension(f)}\", NULL, \"__Internal\", NULL);"));

        File.WriteAllText(Path.Combine(binDir, "runtime.m"),
            Utils.GetEmbeddedResource("runtime.m")
                .Replace("//%DllMap%", dllMap)
                .Replace("%EntryPointLibName%", Path.GetFileName(entryPointLib)));

        Utils.RunProcess("cmake", cmakeArgs.ToString(), workingDir: binDir);

        return Path.Combine(binDir, projectName, projectName + ".xcodeproj");
    }

    public static string BuildAppBundle(
        string xcodePrjPath, string architecture, bool optimized, string? devTeamProvisioning = null)
    {
        string sdk = "";
        var args = new StringBuilder();
        args.Append("ONLY_ACTIVE_ARCH=NO");
        if (architecture == "arm64")
        {
            sdk = "iphoneos";
            args.Append(" -arch arm64")
                .Append(" -sdk iphoneos")
                .Append(" -allowProvisioningUpdates")
                .Append(" DEVELOPMENT_TEAM=").Append(devTeamProvisioning);
        }
        else
        {
            sdk = "iphonesimulator";
            args.Append(" -arch x86_64")
                .Append(" -sdk iphonesimulator");
        }

        string config = optimized ? "Release" : "Debug";
        args.Append(" -configuration ").Append(config);

        Utils.RunProcess("xcodebuild", args.ToString(), workingDir: Path.GetDirectoryName(xcodePrjPath));
        return Path.Combine(Path.GetDirectoryName(xcodePrjPath)!, config + "-" + sdk,
            Path.GetFileNameWithoutExtension(xcodePrjPath) + ".app");
    }
}
