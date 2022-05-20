// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class XcodeCreateProject : Task
{
    private string targetOS = TargetNames.iOS;

    /// <summary>
    /// The Apple OS we are targeting (ios, tvos, iossimulator, tvossimulator)
    /// </summary>
    [Required]
    public string TargetOS
    {
        get
        {
            return targetOS;
        }

        set
        {
            targetOS = value.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Target arch, can be "arm64", "arm" or "x64" at the moment
    /// </summary>
    [Required]
    public string Arch { get; set; } = ""!;

    /// <summary>
    /// Path to the directory with the CMakeLists.txt to create a project for.
    /// </summary>
    [Required]
    public string CMakeListsDirectory { get; set; } = ""!;

    /// <summary>
    /// Name of the generated project.
    /// </summary>
    [Required]
    public string ProjectName { get; set; } = ""!;

    public override bool Execute()
    {
        new Xcode(Log, TargetOS, Arch).CreateXcodeProject(ProjectName, CMakeListsDirectory);

        return true;
    }
}

public class XcodeBuildApp : Task
{
    private string targetOS = TargetNames.iOS;

    /// <summary>
    /// The Apple OS we are targeting (ios, tvos, iossimulator, tvossimulator)
    /// </summary>
    [Required]
    public string TargetOS
    {
        get
        {
            return targetOS;
        }

        set
        {
            targetOS = value.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Target arch, can be "arm64", "arm" or "x64" at the moment
    /// </summary>
    [Required]
    public string Arch { get; set; } = ""!;

    /// <summary>
    /// Path to the .xcodeproj file
    /// </summary>
    [Required]
    public string XcodeProjectPath { get; set; } = ""!;

    /// <summary>
    /// DEVELOPER_TEAM provisioning, needed for arm64 builds.
    /// </summary>
    public string? DevTeamProvisioning { get; set; }

    /// <summary>
    /// Produce optimized binaries and use 'Release' config in xcode
    /// </summary>
    public bool Optimized { get; set; }

    /// Path to the directory where the .app should be created
    /// </summary>
    public string? DestinationFolder { get; set; }

    public override bool Execute()
    {
        new Xcode(Log, TargetOS, Arch).BuildAppBundle(XcodeProjectPath, Optimized, DevTeamProvisioning, DestinationFolder);

        return true;
    }
}

internal sealed class Xcode
{
    private string RuntimeIdentifier { get; set; }
    private string Target { get; set; }
    private string XcodeArch { get; set; }
    private TaskLoggingHelper Logger { get; set; }

    public Xcode(TaskLoggingHelper logger, string target, string arch)
    {
        Logger = logger;
        Target = target;
        RuntimeIdentifier = $"{Target}-{arch}";
        XcodeArch = arch switch {
            "x64" => "x86_64",
            "arm" => "armv7",
            _ => arch
        };
    }

    public string GenerateXCode(
        string projectName,
        string entryPointLib,
        IEnumerable<string> asmFiles,
        IEnumerable<string> asmLinkFiles,
        string workspace,
        string binDir,
        string monoInclude,
        bool preferDylibs,
        bool useConsoleUiTemplate,
        bool forceAOT,
        bool forceInterpreter,
        bool invariantGlobalization,
        bool optimized,
        bool enableRuntimeLogging,
        bool enableAppSandbox,
        string? diagnosticPorts,
        string? runtimeComponents=null,
        string? nativeMainSource = null)
    {
        var cmakeDirectoryPath = GenerateCMake(projectName, entryPointLib, asmFiles, asmLinkFiles, workspace, binDir, monoInclude, preferDylibs, useConsoleUiTemplate, forceAOT, forceInterpreter, invariantGlobalization, optimized, enableRuntimeLogging, enableAppSandbox, diagnosticPorts, runtimeComponents, nativeMainSource);
        CreateXcodeProject(projectName, cmakeDirectoryPath);
        return Path.Combine(binDir, projectName, projectName + ".xcodeproj");
    }

    public void CreateXcodeProject(string projectName, string cmakeDirectoryPath)
    {
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
            .Append(" -DCMAKE_SYSTEM_NAME=").Append(targetName)
            .Append(deployTarget);

        Utils.RunProcess(Logger, "cmake", cmakeArgs.ToString(), workingDir: cmakeDirectoryPath);
    }

    public string GenerateCMake(
        string projectName,
        string entryPointLib,
        IEnumerable<string> asmFiles,
        IEnumerable<string> asmLinkFiles,
        string workspace,
        string binDir,
        string monoInclude,
        bool preferDylibs,
        bool useConsoleUiTemplate,
        bool forceAOT,
        bool forceInterpreter,
        bool invariantGlobalization,
        bool optimized,
        bool enableRuntimeLogging,
        bool enableAppSandbox,
        string? diagnosticPorts,
        string? runtimeComponents=null,
        string? nativeMainSource = null)
    {
        // bundle everything as resources excluding native files
        var excludes = new List<string> { ".dll.o", ".dll.s", ".dwarf", ".m", ".h", ".a", ".bc", "libmonosgen-2.0.dylib", "libcoreclr.dylib" };
        if (optimized)
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
        if (Target == TargetNames.MacCatalyst && !forceAOT)
        {
            hardenedRuntime = true;

            /* for mmmap MAP_JIT */
            entitlements.Add (KeyValuePair.Create ("com.apple.security.cs.allow-jit", "<true/>"));
            /* for loading unsigned dylibs like libicu from outside the bundle or libSystem.Native.dylib from inside */
            entitlements.Add (KeyValuePair.Create ("com.apple.security.cs.disable-library-validation", "<true/>"));
        }

        if (enableAppSandbox)
        {
            hardenedRuntime = true;
            entitlements.Add (KeyValuePair.Create ("com.apple.security.app-sandbox", "<true/>"));

            // the networking entitlement is necessary to enable communication between the test app and xharness
            entitlements.Add (KeyValuePair.Create ("com.apple.security.network.client", "<true/>"));
        }

        string cmakeLists = Utils.GetEmbeddedResource("CMakeLists.txt.template")
            .Replace("%ProjectName%", projectName)
            .Replace("%AppResources%", string.Join(Environment.NewLine, resources.Where(r => !r.EndsWith("-llvm.o")).Select(r => "    " + Path.GetRelativePath(binDir, r))))
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
                Logger.LogMessage(MessageImportance.High, $"\nCouldn't find static component library: {componentLibToLink}, linking static component stub library: {staticComponentStubLib}.\n");
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
            aotSources += $"add_library({projectName}_{name} OBJECT {asm}){Environment.NewLine}";
            toLink += $"    {projectName}_{name}{Environment.NewLine}";
            aotList += $" {projectName}_{name}";
        }

        foreach (string asmLinkFile in asmLinkFiles)
        {
            toLink += $"    {asmLinkFile}{Environment.NewLine}";
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

        if (forceAOT)
        {
            defines.AppendLine("add_definitions(-DFORCE_AOT=1)");
        }

        if (invariantGlobalization)
        {
            defines.AppendLine("add_definitions(-DINVARIANT_GLOBALIZATION=1)");
        }

        if (enableRuntimeLogging)
        {
            defines.AppendLine("add_definitions(-DENABLE_RUNTIME_LOGGING=1)");
        }

        if (!string.IsNullOrEmpty(diagnosticPorts))
        {
            defines.AppendLine($"\nadd_definitions(-DDIAGNOSTIC_PORTS=\"{diagnosticPorts}\")");
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

        return binDir;
    }

    public string BuildAppBundle(
        string xcodePrjPath, bool optimized, string? devTeamProvisioning = null, string? destination = null)
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

        if (XcodeArch == "arm64" || XcodeArch == "armv7")
        {
            switch (Target)
            {
                case TargetNames.iOS:
                    sdk = "iphoneos";
                    args.Append(" -arch " + XcodeArch)
                        .Append(" -sdk ").Append(sdk);
                    break;
                case TargetNames.iOSsim:
                    sdk = "iphonesimulator";
                    args.Append(" -arch " + XcodeArch)
                        .Append(" -sdk ").Append(sdk);
                    break;
                case TargetNames.tvOS:
                    sdk = "appletvos";
                    args.Append(" -arch " + XcodeArch)
                        .Append(" -sdk ").Append(sdk);
                    break;
                case TargetNames.tvOSsim:
                    sdk = "appletvsimulator";
                    args.Append(" -arch " + XcodeArch)
                        .Append(" -sdk ").Append(sdk);
                    break;
                default:
                    sdk = "maccatalyst";
                    args.Append(" -scheme \"").Append(Path.GetFileNameWithoutExtension(xcodePrjPath)).Append('"')
                        .Append(" -destination \"generic/platform=macOS,name=Any Mac,variant=Mac Catalyst\"")
                        .Append(" -UseModernBuildSystem=YES")
                        .Append(" -archivePath \"").Append(Path.GetDirectoryName(xcodePrjPath)).Append('"')
                        .Append(" -derivedDataPath \"").Append(Path.GetDirectoryName(xcodePrjPath)).Append('"')
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
                    args.Append(" -arch " + XcodeArch)
                        .Append(" -sdk ").Append(sdk);
                    break;
                case TargetNames.tvOSsim:
                    sdk = "appletvsimulator";
                    args.Append(" -arch " + XcodeArch)
                        .Append(" -sdk ").Append(sdk);
                    break;
                default:
                    sdk = "maccatalyst";
                    args.Append(" -scheme \"").Append(Path.GetFileNameWithoutExtension(xcodePrjPath)).Append('"')
                        .Append(" -destination \"generic/platform=macOS,name=Any Mac,variant=Mac Catalyst\"")
                        .Append(" -UseModernBuildSystem=YES")
                        .Append(" -archivePath \"").Append(Path.GetDirectoryName(xcodePrjPath)).Append('"')
                        .Append(" -derivedDataPath \"").Append(Path.GetDirectoryName(xcodePrjPath)).Append('"')
                        .Append(" IPHONEOS_DEPLOYMENT_TARGET=13.5");
                    break;
            }
        }

        string config = optimized ? "Release" : "Debug";
        args.Append(" -configuration ").Append(config);

        Utils.RunProcess(Logger, "xcodebuild", args.ToString(), workingDir: Path.GetDirectoryName(xcodePrjPath));

        string appPath = Path.Combine(Path.GetDirectoryName(xcodePrjPath)!, config + "-" + sdk,
            Path.GetFileNameWithoutExtension(xcodePrjPath) + ".app");

        if (destination != null)
        {
            var newAppPath = Path.Combine(destination, Path.GetFileNameWithoutExtension(xcodePrjPath) + ".app");
            Directory.Move(appPath, newAppPath);
            appPath = newAppPath;
        }

        long appSize = new DirectoryInfo(appPath)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(file => file.Length);

        Logger.LogMessage(MessageImportance.High, $"\nAPP size: {(appSize / 1000_000.0):0.#} Mb.\n");

        return appPath;
    }
}
