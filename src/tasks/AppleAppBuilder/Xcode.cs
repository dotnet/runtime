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

    /// Strip local symbols and debug information, and extract it in XcodeProjectPath directory
    /// </summary>
    public bool StripSymbolTable { get; set; }

    public override bool Execute()
    {
        Xcode project = new Xcode(Log, TargetOS, Arch);
        string appDir = project.BuildAppBundle(XcodeProjectPath, Optimized, DevTeamProvisioning);

        string appPath = Xcode.GetAppPath(appDir, XcodeProjectPath);
        string newAppPath = Xcode.GetAppPath(DestinationFolder!, XcodeProjectPath);
        Directory.Move(appPath, newAppPath);

        if (StripSymbolTable)
        {
            project.StripApp(XcodeProjectPath, newAppPath);
        }

        project.LogAppSize(newAppPath);

        return true;
    }
}

internal sealed class Xcode
{
    private string RuntimeIdentifier { get; set; }
    private string Target { get; set; }
    private string XcodeArch { get; set; }
    private TaskLoggingHelper Logger { get; set; }

    public Xcode(TaskLoggingHelper logger, string runtimeIdentifier)
    {
        Logger = logger;

        string[] runtimeIds = runtimeIdentifier.Split('-');

        if (runtimeIds.Length != 2)
        {
            throw new ArgumentException("A valid runtime identifier was not specified (os-arch)");
        }

        RuntimeIdentifier = runtimeIdentifier;
        Target = runtimeIds[0];
        XcodeArch = SetArch(runtimeIds[1]);
    }

    public Xcode(TaskLoggingHelper logger, string target, string arch)
    {
        Logger = logger;
        Target = target;
        RuntimeIdentifier = $"{Target}-{arch}";
        XcodeArch = SetArch(arch);
    }

    private static string SetArch(string arch)
    {
        return arch switch {
            "x64" => "x86_64",
            "arm" => "armv7",
            _ => arch
        };
    }

    public string GenerateXCode(
        string projectName,
        string entryPointLib,
        IEnumerable<string> asmFiles,
        IEnumerable<string> asmDataFiles,
        IEnumerable<string> asmLinkFiles,
        IEnumerable<string> extraLinkerArgs,
        IEnumerable<string> excludes,
        string workspace,
        string binDir,
        string monoInclude,
        bool preferDylibs,
        bool useConsoleUiTemplate,
        bool forceAOT,
        bool forceInterpreter,
        bool invariantGlobalization,
        bool hybridGlobalization,
        bool optimized,
        bool enableRuntimeLogging,
        bool enableAppSandbox,
        string? diagnosticPorts,
        IEnumerable<string> runtimeComponents,
        string? nativeMainSource = null,
        bool useNativeAOTRuntime = false,
        bool isLibraryMode = false)
    {
        var cmakeDirectoryPath = GenerateCMake(projectName, entryPointLib, asmFiles, asmDataFiles, asmLinkFiles, extraLinkerArgs, excludes, workspace, binDir, monoInclude, preferDylibs, useConsoleUiTemplate, forceAOT, forceInterpreter, invariantGlobalization, hybridGlobalization, optimized, enableRuntimeLogging, enableAppSandbox, diagnosticPorts, runtimeComponents, nativeMainSource, useNativeAOTRuntime, isLibraryMode);
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
        var deployTarget = (Target == TargetNames.MacCatalyst) ? " -DCMAKE_OSX_ARCHITECTURES=" + XcodeArch : " -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0";
        var cmakeArgs = new StringBuilder();
        cmakeArgs
            .Append("-S.")
            .Append(" -B").Append(projectName)
            .Append(" -GXcode")
            .Append(" -DTARGETS_APPLE_MOBILE=1")
            .Append(" -DCMAKE_SYSTEM_NAME=").Append(targetName)
            .Append(deployTarget);

        Utils.RunProcess(Logger, "cmake", cmakeArgs.ToString(), workingDir: cmakeDirectoryPath);
    }

    public string GenerateCMake(
        string projectName,
        string entryPointLib,
        IEnumerable<string> asmFiles,
        IEnumerable<string> asmDataFiles,
        IEnumerable<string> asmLinkFiles,
        IEnumerable<string> extraLinkerArgs,
        IEnumerable<string> excludes,
        string workspace,
        string binDir,
        string monoInclude,
        bool preferDylibs,
        bool useConsoleUiTemplate,
        bool forceAOT,
        bool forceInterpreter,
        bool invariantGlobalization,
        bool hybridGlobalization,
        bool optimized,
        bool enableRuntimeLogging,
        bool enableAppSandbox,
        string? diagnosticPorts,
        IEnumerable<string> runtimeComponents,
        string? nativeMainSource = null,
        bool useNativeAOTRuntime = false,
        bool isLibraryMode = false)
    {
        // bundle everything as resources excluding native files
        var predefinedExcludes = new List<string> { ".dll.o", ".dll.s", ".dwarf", ".m", ".h", ".a", ".bc", "libmonosgen-2.0.dylib", "libcoreclr.dylib", "icudt*" };

        // TODO: All of these exclusions shouldn't be needed once we carefully construct the publish folder on Helix
        if (useNativeAOTRuntime)
        {
            predefinedExcludes.Add(".dll");
            predefinedExcludes.Add(".pdb");
            predefinedExcludes.Add(".json");
            predefinedExcludes.Add(".txt");
            predefinedExcludes.Add(".bin");
            predefinedExcludes.Add(".dSYM");
        }

        predefinedExcludes = predefinedExcludes.Concat(excludes).ToList();
        if (!preferDylibs)
        {
            predefinedExcludes.Add(".dylib");
        }
        if (optimized)
        {
            predefinedExcludes.Add(".pdb");
        }

        string[] resources = Directory.GetFileSystemEntries(workspace, "", SearchOption.TopDirectoryOnly)
            .Where(f => !predefinedExcludes.Any(e => (!e.EndsWith('*') && f.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)) || (e.EndsWith('*') && Path.GetFileName(f).StartsWith(e.TrimEnd('*'), StringComparison.InvariantCultureIgnoreCase) &&
            !(!hybridGlobalization && Path.GetFileName(f) == "icudt.dat"))))
            .ToArray();

        if (string.IsNullOrEmpty(nativeMainSource))
        {
            // use built-in main.m (with default UI) if it's not set
            nativeMainSource = Path.Combine(binDir, "main.m");
            File.WriteAllText(nativeMainSource, Utils.GetEmbeddedResource((useConsoleUiTemplate || isLibraryMode) ? "main-console.m" : "main-simple.m"));
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

        string appResources = string.Join(Environment.NewLine, asmDataFiles.Select(r => "    " + r));
        appResources += string.Join(Environment.NewLine, resources.Where(r => !r.EndsWith("-llvm.o")).Select(r => "    " + Path.GetRelativePath(binDir, r)));

        string cmakeTemplateName = (isLibraryMode) ? "CMakeLists-librarymode.txt.template" : "CMakeLists.txt.template";
        string cmakeLists = Utils.GetEmbeddedResource(cmakeTemplateName)
            .Replace("%UseNativeAOTRuntime%", useNativeAOTRuntime ? "TRUE" : "FALSE")
            .Replace("%ProjectName%", projectName)
            .Replace("%AppResources%", appResources)
            .Replace("%MainSource%", nativeMainSource)
            .Replace("%MonoInclude%", monoInclude)
            .Replace("%HardenedRuntime%", hardenedRuntime ? "TRUE" : "FALSE");

        string toLink = "";
        string aotSources = "";
        string aotList = "";

        if (isLibraryMode)
        {
            string libraryPath;
            // TODO: unify MonoAOT and NativeAOT library paths
            // Current differences:
            // - NativeAOT produces {ProjectName}.dylib, while MonoAOT produces lib{ProjectName}.dylib
            // - NativeAOT places the library in the 'workspace' location ie 'publish' folder, while MonoAOT places it in 'binDir' ie 'AppBundle'
            if (useNativeAOTRuntime)
            {
                libraryPath = Path.Combine(workspace, $"{projectName}.dylib");
            }
            else
            {
                libraryPath = Path.Combine(binDir, $"lib{projectName}.dylib");
            }

            if (!File.Exists(libraryPath))
            {
                throw new Exception($"Library not found at: {libraryPath} when building in the library mode.");
            }

            cmakeLists = cmakeLists.Replace("%DYLIB_PATH%", libraryPath);

            // pass the shared library to the linker for dynamic linking
            if (useNativeAOTRuntime)
                toLink += $"    {libraryPath}{Environment.NewLine}";
        }
        else
        {
            string[] allComponentLibs = Directory.GetFiles(workspace, "libmono-component-*-static.a");
            string[] staticComponentStubLibs = Directory.GetFiles(workspace, "libmono-component-*-stub-static.a");

            // by default, component stubs will be linked and depending on how mono runtime has been build,
            // stubs can disable or dynamic load components.
            foreach (string staticComponentStubLib in staticComponentStubLibs)
            {
                string componentLibToLink = staticComponentStubLib;
                foreach (string runtimeComponent in runtimeComponents)
                {
                    if (componentLibToLink.Contains(runtimeComponent, StringComparison.OrdinalIgnoreCase))
                    {
                        // static link component.
                        componentLibToLink = componentLibToLink.Replace("-stub-static.a", "-static.a", StringComparison.OrdinalIgnoreCase);
                        break;
                    }
                }

                // if lib doesn't exist (primarily due to runtime build without static lib support), fallback linking stub lib.
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
                if((libName.StartsWith("libSystem.Globalization", StringComparison.OrdinalIgnoreCase) ||
                    libName.StartsWith("libicudata", StringComparison.OrdinalIgnoreCase) ||
                    libName.StartsWith("libicui18n", StringComparison.OrdinalIgnoreCase) ||
                    libName.StartsWith("libicuuc", StringComparison.OrdinalIgnoreCase)) && hybridGlobalization)
                    continue;
                else if (libName.StartsWith("libSystem.HybridGlobalization", StringComparison.OrdinalIgnoreCase) && !hybridGlobalization)
                    continue;
                // libmono must always be statically linked, for other librarires we can use dylibs
                bool dylibExists = libName != "libmonosgen-2.0" && dylibs.Any(dylib => Path.GetFileName(dylib) == libName + ".dylib");

                if (useNativeAOTRuntime)
                {
                    // link NativeAOT framework libs without '-force_load'
                    toLink += $"    {lib}{Environment.NewLine}";
                }
                else if (forceAOT || !(preferDylibs && dylibExists))
                {
                    // these libraries are pinvoked
                    // -force_load will be removed once we enable direct-pinvokes for AOT
                    toLink += $"    \"-force_load {lib}\"{Environment.NewLine}";
                }
            }

            foreach (string asm in asmFiles)
            {
                // these libraries are linked via modules.m
                var name = Path.GetFileNameWithoutExtension(asm);
                aotSources += $"add_library({projectName}_{name} OBJECT {asm}){Environment.NewLine}";
                toLink += $"    {projectName}_{name}{Environment.NewLine}";
                aotList += $" {projectName}_{name}";
            }
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

        string appLinkLibraries = $"    {frameworks}{Environment.NewLine}";
        string extraLinkerArgsConcatEscapeQuotes = string.Join('\n', extraLinkerArgs).Replace("\"", "\\\"");
        string extraLinkerArgsConcat = $"\"{extraLinkerArgsConcatEscapeQuotes}\"";

        cmakeLists = cmakeLists.Replace("%NativeLibrariesToLink%", toLink);
        cmakeLists = cmakeLists.Replace("%APP_LINK_LIBRARIES%", appLinkLibraries);
        cmakeLists = cmakeLists.Replace("%EXTRA_LINKER_ARGS%", extraLinkerArgsConcat);
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

        if (hybridGlobalization && !invariantGlobalization)
        {
            defines.AppendLine("add_definitions(-DHYBRID_GLOBALIZATION=1)");
        }

        if (enableRuntimeLogging)
        {
            defines.AppendLine("add_definitions(-DENABLE_RUNTIME_LOGGING=1)");
        }

        if (!string.IsNullOrEmpty(diagnosticPorts))
        {
            defines.AppendLine($"\nadd_definitions(-DDIAGNOSTIC_PORTS=\"{diagnosticPorts}\")");
        }

        if (useNativeAOTRuntime)
        {
            defines.AppendLine("add_definitions(-DUSE_NATIVE_AOT=1)");
        }

        if (isLibraryMode)
        {
            defines.AppendLine("add_definitions(-DUSE_LIBRARY_MODE=1)");
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

        if (isLibraryMode)
        {
            File.WriteAllText(Path.Combine(binDir, "runtime-librarymode.h"), Utils.GetEmbeddedResource("runtime-librarymode.h"));
            File.WriteAllText(Path.Combine(binDir, "runtime-librarymode.m"), Utils.GetEmbeddedResource("runtime-librarymode.m"));
        }
        else if (!useNativeAOTRuntime)
        {
            File.WriteAllText(Path.Combine(binDir, "runtime.h"),
                Utils.GetEmbeddedResource("runtime.h"));

            // lookup statically linked libraries via dlsym(), see handle_pinvoke_override() in runtime.m
            var pinvokeOverrides = new StringBuilder();
            foreach (string aFile in Directory.GetFiles(workspace, "*.a"))
            {
                string aFileName = Path.GetFileNameWithoutExtension(aFile);
                if((aFileName.StartsWith("libSystem.Globalization", StringComparison.OrdinalIgnoreCase) ||
                    aFileName.StartsWith("libicudata", StringComparison.OrdinalIgnoreCase) ||
                    aFileName.StartsWith("libicui18n", StringComparison.OrdinalIgnoreCase) ||
                    aFileName.StartsWith("libicuuc", StringComparison.OrdinalIgnoreCase)) && hybridGlobalization)
                    continue;
                else if (aFileName.StartsWith("libSystem.HybridGlobalization", StringComparison.OrdinalIgnoreCase) && !hybridGlobalization)
                    continue;
                pinvokeOverrides.AppendLine($"        \"{aFileName}\",");

                // also register with or without "lib" prefix
                aFileName = aFileName.StartsWith("lib") ? aFileName.Remove(0, 3) : "lib" + aFileName;
                pinvokeOverrides.AppendLine($"        \"{aFileName}\",");
            }
            if (hybridGlobalization)
            {
                pinvokeOverrides.AppendLine($"        \"System.HybridGlobalization.Native\",");
            }
            else
            {
                pinvokeOverrides.AppendLine($"        \"System.Globalization.Native\",");
            }

            File.WriteAllText(Path.Combine(binDir, "runtime.m"),
                Utils.GetEmbeddedResource("runtime.m")
                    .Replace("//%PInvokeOverrideLibraries%", pinvokeOverrides.ToString())
                    .Replace("//%APPLE_RUNTIME_IDENTIFIER%", RuntimeIdentifier)
                    .Replace("%EntryPointLibName%", Path.GetFileName(entryPointLib)));
        }

        File.WriteAllText(Path.Combine(binDir, "util.h"), Utils.GetEmbeddedResource("util.h"));
        File.WriteAllText(Path.Combine(binDir, "util.m"), Utils.GetEmbeddedResource("util.m"));

        return binDir;
    }

    public string BuildAppBundle(
        string xcodePrjPath, bool optimized, string? devTeamProvisioning = null)
    {
        string sdk;
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

        string appDirectory = Path.Combine(Path.GetDirectoryName(xcodePrjPath)!, config + "-" + sdk);
        if (!Directory.Exists(appDirectory))
        {
            // cmake 3.25.0 seems to have changed the output directory for MacCatalyst, move it back to the old format
            string appDirectoryWithoutSdk = Path.Combine(Path.GetDirectoryName(xcodePrjPath)!, config);
            Directory.Move(appDirectoryWithoutSdk, appDirectory);
        }

        return appDirectory;
    }

    public void LogAppSize(string appPath)
    {
        long appSize = new DirectoryInfo(appPath)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(file => file.Length);

        Logger.LogMessage(MessageImportance.High, $"\nAPP size: {(appSize / 1000_000.0):0.#} Mb.\n");
    }

    public void StripApp(string xcodePrjPath, string appPath)
    {
        string filename = Path.GetFileNameWithoutExtension(appPath);
        Utils.RunProcess(Logger, "dsymutil", $"{appPath}/{filename} -o {Path.GetDirectoryName(xcodePrjPath)}/{filename}.dSYM", workingDir: Path.GetDirectoryName(appPath));
        Utils.RunProcess(Logger, "strip", $"-no_code_signature_warning -x {appPath}/{filename}", workingDir: Path.GetDirectoryName(appPath));
    }

    public static string GetAppPath(string appDirectory, string xcodePrjPath)
    {
        return Path.Combine(appDirectory, Path.GetFileNameWithoutExtension(xcodePrjPath) + ".app");
    }
}
