// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

internal class Xcode
{
    private string RuntimeIdentifier { get; set; }
    private string SysRoot { get; set; }
    private string Target { get; set; }
    private string XcodeArch { get; set; }

    public Xcode(string target, string arch)
    {
        Target = target;
        XcodeArch = (arch == "x64") ? "x86_64" : arch;
        switch (Target)
        {
            case TargetNames.iOS:
                SysRoot = Utils.RunProcess("xcrun", "--sdk iphoneos --show-sdk-path");
                break;
            case TargetNames.iOSsim:
                SysRoot = Utils.RunProcess("xcrun", "--sdk iphonesimulator --show-sdk-path");
                break;
            case TargetNames.tvOS:
                SysRoot = Utils.RunProcess("xcrun", "--sdk appletvos --show-sdk-path");
                break;
            case TargetNames.tvOSsim:
                SysRoot = Utils.RunProcess("xcrun", "--sdk appletvsimulator --show-sdk-path");
                break;
            default:
                SysRoot = Utils.RunProcess("xcrun", "--sdk macosx --show-sdk-path");
                break;
        }

        RuntimeIdentifier = $"{Target}-{arch}";
    }

    public bool EnableRuntimeLogging { get; set; }
    public string? DiagnosticPorts { get; set; } = ""!;

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
        bool invariantGlobalization,
        bool stripDebugSymbols,
        string? runtimeComponents=null,
        string? nativeMainSource = null)
    {
        // bundle everything as resources excluding native files
        var excludes = new List<string> { ".dll.o", ".dll.s", ".dwarf", ".m", ".h", ".a", ".bc", "libmonosgen-2.0.dylib", "libcoreclr.dylib" };
        if (stripDebugSymbols)
        {
            excludes.Add(".pdb");
        }

        string[] resources = Directory.GetFileSystemEntries(workspace, "", SearchOption.TopDirectoryOnly)
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

        var entitlements = new List<KeyValuePair<string, string>>();

        bool hardenedRuntime = false;
        if (Target == TargetNames.MacCatalyst && !forceAOT) {
            hardenedRuntime = true;

            /* for mmmap MAP_JIT */
            entitlements.Add (KeyValuePair.Create ("com.apple.security.cs.allow-jit", "<true/>"));
            /* for loading unsigned dylibs like libicu from outside the bundle or libSystem.Native.dylib from inside */
            entitlements.Add (KeyValuePair.Create ("com.apple.security.cs.disable-library-validation", "<true/>"));
        }

        string cmakeLists = Utils.GetEmbeddedResource("CMakeLists.txt.template")
            .Replace("%ProjectName%", projectName)
            .Replace("%AppResources%", string.Join(Environment.NewLine, resources.Select(r => "    " + Path.GetRelativePath(binDir, r))))
            .Replace("%MainSource%", nativeMainSource)
            .Replace("%MonoInclude%", monoInclude)
            .Replace("%HardenedRuntime%", hardenedRuntime ? "TRUE" : "FALSE");

        string toLink = "";

        string[] allComponentLibs = Directory.GetFiles(workspace, "libmono-component-*-static.a");
        string[] staticComponentStubLibs = Directory.GetFiles(workspace, "libmono-component-*-stub-static.a");
        bool staticLinkAllComponents = false;
        string[] staticLinkedComponents = Array.Empty<string>();

        if (!string.IsNullOrEmpty(runtimeComponents) && runtimeComponents.Equals("*", StringComparison.OrdinalIgnoreCase))
            staticLinkAllComponents = true;
        else if (!string.IsNullOrEmpty(runtimeComponents))
            staticLinkedComponents = runtimeComponents.Split(";");

        // by default, component stubs will be linked and depending on how mono runtime has been build,
        // stubs can disable or dynamic load components.
        foreach (string staticComponentStubLib in staticComponentStubLibs)
        {
            string componentLibToLink = staticComponentStubLib;
            if (staticLinkAllComponents)
            {
                // static link component.
                componentLibToLink = componentLibToLink.Replace("-stub-static.a", "-static.a", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                foreach (string staticLinkedComponent in staticLinkedComponents)
                {
                    if (componentLibToLink.Contains(staticLinkedComponent, StringComparison.OrdinalIgnoreCase))
                    {
                        // static link component.
                        componentLibToLink = componentLibToLink.Replace("-stub-static.a", "-static.a", StringComparison.OrdinalIgnoreCase);
                        break;
                    }
                }
            }

            // if lib doesn't exist (primarly due to runtime build without static lib support), fallback linking stub lib.
            if (!File.Exists(componentLibToLink))
            {
                Utils.LogInfo($"\nCouldn't find static component library: {componentLibToLink}, linking static component stub library: {staticComponentStubLib}.\n");
                componentLibToLink = staticComponentStubLib;
            }

            toLink += $"    \"-force_load {componentLibToLink}\"{Environment.NewLine}";
        }

        string[] dylibs = Directory.GetFiles(workspace, "*.dylib");
        foreach (string lib in Directory.GetFiles(workspace, "*.a"))
        {
            // all component libs already added to linker.
            if (allComponentLibs.Any(lib.Contains))
                continue;

            string libName = Path.GetFileNameWithoutExtension(lib);
            // libmono must always be statically linked, for other librarires we can use dylibs
            bool dylibExists = libName != "libmonosgen-2.0" && dylibs.Any(dylib => Path.GetFileName(dylib) == libName + ".dylib");

            if (forceAOT || !(preferDylibs && dylibExists))
            {
                // these libraries are pinvoked
                // -force_load will be removed once we enable direct-pinvokes for AOT
                toLink += $"    \"-force_load {lib}\"{Environment.NewLine}";
            }
        }

        string aotSources = "";
        string aotList = "";
        foreach (string asm in asmFiles)
        {
            // these libraries are linked via modules.m
            var name = Path.GetFileNameWithoutExtension(asm);
            aotSources += $"add_library({name} OBJECT {asm}){Environment.NewLine}";
            toLink += $"    {name}{Environment.NewLine}";
            aotList += $" {name}";
        }

        string frameworks = "";
        if ((Target == TargetNames.iOS) || (Target == TargetNames.iOSsim) || (Target == TargetNames.MacCatalyst))
        {
            frameworks = "\"-framework GSS\"";
        }

        cmakeLists = cmakeLists.Replace("%FrameworksToLink%", frameworks);
        cmakeLists = cmakeLists.Replace("%NativeLibrariesToLink%", toLink);
        cmakeLists = cmakeLists.Replace("%AotSources%", aotSources);
        cmakeLists = cmakeLists.Replace("%AotTargetsList%", aotList);
        cmakeLists = cmakeLists.Replace("%AotModulesSource%", string.IsNullOrEmpty(aotSources) ? "" : "modules.m");

        var defines = new StringBuilder();
        if (forceInterpreter)
        {
            defines.AppendLine("add_definitions(-DFORCE_INTERPRETER=1)");
        }
        else if (forceAOT)
        {
            defines.AppendLine("add_definitions(-DFORCE_AOT=1)");
        }

        if (invariantGlobalization)
        {
            defines.AppendLine("add_definitions(-DINVARIANT_GLOBALIZATION=1)");
        }

        if (EnableRuntimeLogging)
        {
            defines.AppendLine("add_definitions(-DENABLE_RUNTIME_LOGGING=1)");
        }

        if (!string.IsNullOrEmpty(DiagnosticPorts))
        {
            defines.AppendLine("\nadd_definitions(-DDIAGNOSTIC_PORTS=\"" + DiagnosticPorts + "\")");
        }

        cmakeLists = cmakeLists.Replace("%Defines%", defines.ToString());

        string plist = Utils.GetEmbeddedResource("Info.plist.template")
            .Replace("%BundleIdentifier%", projectName);

        File.WriteAllText(Path.Combine(binDir, "Info.plist"), plist);

        var needEntitlements = entitlements.Count != 0;
        cmakeLists = cmakeLists.Replace("%HardenedRuntimeUseEntitlementsFile%",
                                        needEntitlements ? "TRUE" : "FALSE");

        File.WriteAllText(Path.Combine(binDir, "CMakeLists.txt"), cmakeLists);

        if (needEntitlements) {
            var ent = new StringBuilder();
            foreach ((var key, var value) in entitlements) {
                ent.AppendLine ($"<key>{key}</key>");
                ent.AppendLine (value);
            }
            string entitlementsTemplate = Utils.GetEmbeddedResource("app.entitlements.template");
            File.WriteAllText(Path.Combine(binDir, "app.entitlements"), entitlementsTemplate.Replace("%Entitlements%", ent.ToString()));
        }

        string targetName;
        switch (Target)
        {
            case TargetNames.MacCatalyst:
                targetName = "Darwin";
                break;
            case TargetNames.iOS:
            case TargetNames.iOSsim:
                targetName = "iOS";
                break;
            case TargetNames.tvOS:
            case TargetNames.tvOSsim:
                targetName = "tvOS";
                break;
            default:
                targetName = Target.ToString();
                break;
        }
        var deployTarget = (Target == TargetNames.MacCatalyst) ? " -DCMAKE_OSX_ARCHITECTURES=" + XcodeArch : " -DCMAKE_OSX_DEPLOYMENT_TARGET=10.1";
        var cmakeArgs = new StringBuilder();
        cmakeArgs
            .Append("-S.")
            .Append(" -B").Append(projectName)
            .Append(" -GXcode")
            .Append(" -DCMAKE_SYSTEM_NAME=" + targetName)
            .Append(deployTarget);

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

        dllMap.AppendLine($"    mono_dllmap_insert (NULL, \"System.Globalization.Native\", NULL, \"__Internal\", NULL);");

        File.WriteAllText(Path.Combine(binDir, "runtime.m"),
            Utils.GetEmbeddedResource("runtime.m")
                .Replace("//%DllMap%", dllMap.ToString())
                .Replace("//%APPLE_RUNTIME_IDENTIFIER%", RuntimeIdentifier)
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

        if (devTeamProvisioning == "-")
        {
            args.Append(" CODE_SIGN_IDENTITY=\"\"")
                .Append(" CODE_SIGNING_REQUIRED=NO")
                .Append(" CODE_SIGNING_ALLOWED=NO");
        }
        else if (string.Equals(devTeamProvisioning, "adhoc",  StringComparison.OrdinalIgnoreCase))
        {
            args.Append(" CODE_SIGN_IDENTITY=\"-\"");
        }
        else
        {
            args.Append(" -allowProvisioningUpdates")
                .Append(" DEVELOPMENT_TEAM=").Append(devTeamProvisioning);
        }


        if (architecture == "arm64")
        {
            switch (Target)
            {
                case TargetNames.iOS:
                    sdk = "iphoneos";
                    args.Append(" -arch arm64")
                        .Append(" -sdk " + sdk);
                    break;
                case TargetNames.iOSsim:
                    sdk = "iphonesimulator";
                    args.Append(" -arch arm64")
                        .Append(" -sdk " + sdk);
                    break;
                case TargetNames.tvOS:
                    sdk = "appletvos";
                    args.Append(" -arch arm64")
                        .Append(" -sdk " + sdk);
                    break;
                case TargetNames.tvOSsim:
                    sdk = "appletvsimulator";
                    args.Append(" -arch arm64")
                        .Append(" -sdk " + sdk);
                    break;
                default:
                    sdk = "maccatalyst";
                    args.Append(" -scheme \"" + Path.GetFileNameWithoutExtension(xcodePrjPath) + "\"")
                        .Append(" -destination \"generic/platform=macOS,name=Any Mac,variant=Mac Catalyst\"")
                        .Append(" -UseModernBuildSystem=YES")
                        .Append(" -archivePath \"" + Path.GetDirectoryName(xcodePrjPath) + "\"")
                        .Append(" -derivedDataPath \"" + Path.GetDirectoryName(xcodePrjPath) + "\"")
                        .Append(" IPHONEOS_DEPLOYMENT_TARGET=14.2");
                    break;
            }
        }
        else
        {
            switch (Target)
            {
                case TargetNames.iOSsim:
                    sdk = "iphonesimulator";
                    args.Append(" -arch x86_64")
                        .Append(" -sdk " + sdk);
                    break;
                case TargetNames.tvOSsim:
                    sdk = "appletvsimulator";
                    args.Append(" -arch x86_64")
                        .Append(" -sdk " + sdk);
                    break;
                default:
                    sdk = "maccatalyst";
                    args.Append(" -scheme \"" + Path.GetFileNameWithoutExtension(xcodePrjPath) + "\"")
                        .Append(" -destination \"generic/platform=macOS,name=Any Mac,variant=Mac Catalyst\"")
                        .Append(" -UseModernBuildSystem=YES")
                        .Append(" -archivePath \"" + Path.GetDirectoryName(xcodePrjPath) + "\"")
                        .Append(" -derivedDataPath \"" + Path.GetDirectoryName(xcodePrjPath) + "\"")
                        .Append(" IPHONEOS_DEPLOYMENT_TARGET=13.5");
                    break;
            }
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
