// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

internal class Xcode
{
    private string SysRoot { get; set; }
    private string Target { get; set; }

    public Xcode(string target)
    {
        Target = target;
        SysRoot = (Target == TargetNames.iOS) ?
            Utils.RunProcess("xcrun", "--sdk iphoneos --show-sdk-path") :
            Utils.RunProcess("xcrun", "--sdk appletvos --show-sdk-path");
    }

    public string GenerateXCode(
        string projectName,
        string entryPointLib,
        IEnumerable<string> asmFiles,
        string workspace,
        string binDir,
        string monoInclude,
        bool preferDylibs,
        bool useConsoleUiTemplate,
        bool forceAOT,
        bool forceInterpreter,
        bool stripDebugSymbols,
        string? nativeMainSource = null)
    {
        // bundle everything as resources excluding native files
        var excludes = new List<string> { ".dll.o", ".dll.s", ".dwarf", ".m", ".h", ".a", ".bc", "libmonosgen-2.0.dylib" };
        if (stripDebugSymbols)
        {
            excludes.Add(".pdb");
        }

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


        string[] dylibs = Directory.GetFiles(workspace, "*.dylib");
        string toLink = "";
        foreach (string lib in Directory.GetFiles(workspace, "*.a"))
        {
            string libName = Path.GetFileNameWithoutExtension(lib);
            // libmono must always be statically linked, for other librarires we can use dylibs
            bool dylibExists = libName != "libmonosgen-2.0" && dylibs.Any(dylib => Path.GetFileName(dylib) == libName + ".dylib");

            if (!preferDylibs || !dylibExists)
            {
                // these libraries are pinvoked
                // -force_load will be removed once we enable direct-pinvokes for AOT
                toLink += $"    \"-force_load {lib}\"{Environment.NewLine}";
            }
        }

        string aotSources = "";
        foreach (string asm in asmFiles)
        {
            // these libraries are linked via modules.m
            var name = Path.GetFileNameWithoutExtension(asm);
            aotSources += $"add_library({name} OBJECT {asm}){Environment.NewLine}";
            toLink += $"    {name}{Environment.NewLine}";
        }

        string frameworks = "";
        if (Target == TargetNames.iOS)
        {
            frameworks = "\"-framework GSS\"";
        }

        cmakeLists = cmakeLists.Replace("%FrameworksToLink%", frameworks);
        cmakeLists = cmakeLists.Replace("%NativeLibrariesToLink%", toLink);
        cmakeLists = cmakeLists.Replace("%AotSources%", aotSources);
        cmakeLists = cmakeLists.Replace("%AotModulesSource%", string.IsNullOrEmpty(aotSources) ? "" : "modules.m");

        string defines = "";
        if (forceInterpreter)
        {
            defines = "add_definitions(-DFORCE_INTERPRETER=1)";
        }
        else if (forceAOT)
        {
            defines = "add_definitions(-DFORCE_AOT=1)";
        }
        cmakeLists = cmakeLists.Replace("%Defines%", defines);

        string plist = Utils.GetEmbeddedResource("Info.plist.template")
            .Replace("%BundleIdentifier%", projectName);

        File.WriteAllText(Path.Combine(binDir, "Info.plist"), plist);
        File.WriteAllText(Path.Combine(binDir, "CMakeLists.txt"), cmakeLists);

        var cmakeArgs = new StringBuilder();
        cmakeArgs
            .Append("-S.")
            .Append(" -B").Append(projectName)
            .Append(" -GXcode")
            .Append(" -DCMAKE_SYSTEM_NAME=" + Target.ToString())
            .Append(" -DCMAKE_OSX_DEPLOYMENT_TARGET=10.1");

        File.WriteAllText(Path.Combine(binDir, "runtime.h"),
            Utils.GetEmbeddedResource("runtime.h"));

        // forward pinvokes to "__Internal"
        var dllMap = new StringBuilder();
        foreach (string aFile in Directory.GetFiles(workspace, "*.a"))
        {
            string aFileName = Path.GetFileNameWithoutExtension(aFile);
            dllMap.AppendLine($"    mono_dllmap_insert (NULL, \"{aFileName}\", NULL, \"__Internal\", NULL);");

            // also register with or without "lib" prefix
            aFileName = aFileName.StartsWith("lib") ? aFileName.Remove(0, 3) : "lib" + aFileName;
            dllMap.AppendLine($"    mono_dllmap_insert (NULL, \"{aFileName}\", NULL, \"__Internal\", NULL);");
        }

        File.WriteAllText(Path.Combine(binDir, "runtime.m"),
            Utils.GetEmbeddedResource("runtime.m")
                .Replace("//%DllMap%", dllMap.ToString())
                .Replace("%EntryPointLibName%", Path.GetFileName(entryPointLib)));

        Utils.RunProcess("cmake", cmakeArgs.ToString(), workingDir: binDir);

        return Path.Combine(binDir, projectName, projectName + ".xcodeproj");
    }

    public string BuildAppBundle(
        string xcodePrjPath, string architecture, bool optimized, string? devTeamProvisioning = null)
    {
        string sdk = "";
        var args = new StringBuilder();
        args.Append("ONLY_ACTIVE_ARCH=YES");

        if (architecture == "arm64")
        {
            sdk = (Target == TargetNames.iOS) ? "iphoneos" : "appletvos";
            args.Append(" -arch arm64")
                .Append(" -sdk " + sdk);

            if (devTeamProvisioning == "-")
            {
                args.Append(" CODE_SIGN_IDENTITY=\"\"")
                    .Append(" CODE_SIGNING_REQUIRED=NO")
                    .Append(" CODE_SIGNING_ALLOWED=NO");
            }
            else
            {
                args.Append(" -allowProvisioningUpdates")
                    .Append(" DEVELOPMENT_TEAM=").Append(devTeamProvisioning);
            }
        }
        else
        {
            sdk = (Target == TargetNames.iOS) ? "iphonesimulator" : "appletvsimulator";
            args.Append(" -arch x86_64")
                .Append(" -sdk " + sdk);
        }

        string config = optimized ? "Release" : "Debug";
        args.Append(" -configuration ").Append(config);

        Utils.RunProcess("xcodebuild", args.ToString(), workingDir: Path.GetDirectoryName(xcodePrjPath));

        string appPath = Path.Combine(Path.GetDirectoryName(xcodePrjPath)!, config + "-" + sdk,
            Path.GetFileNameWithoutExtension(xcodePrjPath) + ".app");

        long appSize = new DirectoryInfo(appPath)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(file => file.Length);

        Utils.LogInfo($"\nAPP size: {(appSize / 1000_000.0):0.#} Mb.\n");

        return appPath;
    }
}
