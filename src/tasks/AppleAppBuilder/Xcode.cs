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
        bool preferDylibs,
        bool useConsoleUiTemplate,
        bool invariantGlobalization,
        bool hybridGlobalization,
        bool optimized,
        bool enableRuntimeLogging,
        bool enableAppSandbox,
        string? diagnosticPorts,
        IEnumerable<string> environmentVariables,
        string? nativeMainSource = null,
        bool useNativeAotRuntime = false,
        bool isLibraryMode = false,
        string? runtimeConfigFile = null)
    {
        var cmakeDirectoryPath = GenerateCMake(projectName, entryPointLib, asmFiles, asmDataFiles, asmLinkFiles, extraLinkerArgs, excludes, workspace, binDir, preferDylibs, useConsoleUiTemplate, invariantGlobalization, hybridGlobalization, optimized, enableRuntimeLogging, enableAppSandbox, diagnosticPorts, environmentVariables, nativeMainSource, useNativeAotRuntime, isLibraryMode, runtimeConfigFile);
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
        var cmakeArgs = new StringBuilder();
        cmakeArgs
            .Append("-S.")
            .Append(" -B").Append(projectName)
            .Append(" -GXcode")
            .Append(" -DTARGETS_APPLE_MOBILE=1")
            .Append(" -DCMAKE_SYSTEM_NAME=").Append(targetName);

        if (Target == TargetNames.MacCatalyst)
        {
            // min deploy target version is passed later when invoking xcodebuild
            cmakeArgs.Append(" -DCMAKE_OSX_ARCHITECTURES=" + XcodeArch);
        }
        else
        {
            // arch is passed later when invoking xcodebuild
            cmakeArgs.Append(" -DCMAKE_OSX_DEPLOYMENT_TARGET=15.0");
        }

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
        bool preferDylibs,
        bool useConsoleUiTemplate,
        bool invariantGlobalization,
        bool hybridGlobalization,
        bool optimized,
        bool enableRuntimeLogging,
        bool enableAppSandbox,
        string? diagnosticPorts,
        IEnumerable<string> environmentVariables,
        string? nativeMainSource = null,
        bool useNativeAotRuntime = false,
        bool isLibraryMode = false,
        string? runtimeConfigFile = null)
    {
        // bundle everything as resources excluding native files
        var predefinedExcludes = new List<string> { ".dll.o", ".dll.s", ".dwarf", ".m", ".h", ".a", ".bc", "icudt*" };

        // TODO: All of these exclusions shouldn't be needed once we carefully construct the publish folder on Helix
        if (useNativeAotRuntime)
        {
            predefinedExcludes.Add(".dll");
            predefinedExcludes.Add(".pdb");
            predefinedExcludes.Add(".deps.json");
            predefinedExcludes.Add(".runtimeconfig.json");
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
        if (useNativeAotRuntime)
        {
            predefinedExcludes.Add("libcoreclr.dylib");
        }
        else
        {
            // Interpreter is statically linked into the runtime already
            predefinedExcludes.Add("libclrjit.dylib");
            predefinedExcludes.Add("libclrinterpreter.dylib");
        }

        string[] resources = Directory.GetFileSystemEntries(workspace, "", SearchOption.TopDirectoryOnly)
            .Where(f => !predefinedExcludes.Any(e => (!e.EndsWith('*') && f.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)) || (e.EndsWith('*') && Path.GetFileName(f).StartsWith(e.TrimEnd('*'), StringComparison.InvariantCultureIgnoreCase))))
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
        if (Target == TargetNames.MacCatalyst)
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
            .Replace("%UseNativeAOTRuntime%", useNativeAotRuntime ? "TRUE" : "FALSE")
            .Replace("%ProjectName%", projectName)
            .Replace("%AppResources%", appResources)
            .Replace("%MainSource%", nativeMainSource)
            .Replace("%HardenedRuntime%", hardenedRuntime ? "TRUE" : "FALSE");

        string toLink = "";
        string aotSources = "";
        string aotList = "";

        if (isLibraryMode)
        {
            string libraryPath = Path.Combine(workspace, $"{projectName}.dylib");

            if (!File.Exists(libraryPath))
            {
                throw new Exception($"Library not found at: {libraryPath} when building in the library mode.");
            }

            cmakeLists = cmakeLists.Replace("%DYLIB_PATH%", libraryPath);

            // pass the shared library to the linker for dynamic linking
            if (useNativeAotRuntime)
                toLink += $"    {libraryPath}{Environment.NewLine}";
        }
        else
        {
            if (!useNativeAotRuntime)
            {
                string[] dylibsPatterns = new string[] {
                    "libcoreclr.dylib",
                    "libbrotli*.dylib",
                    "libSystem*.dylib"
                };
                string[] dylibs = dylibsPatterns.SelectMany(pattern => Directory.GetFiles(workspace, pattern)).ToArray();
                foreach (string lib in dylibs)
                {
                    toLink += $"    \"-force_load {lib}\"{Environment.NewLine}";
                }
            }
            else
            {
                // Sort the static libraries so the brotli libraries are linked after the compression native libraries.
                foreach (string lib in Directory.GetFiles(workspace, "*.a").OrderBy(libName => libName.Contains("brotli") ? 1 : 0))
                {
                    toLink += $"    {lib}{Environment.NewLine}";
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
        if (!useNativeAotRuntime)
        {
            cmakeLists = cmakeLists.Replace("%AotModulesSource%", "coreclrhost.h");
        }
        else
        {
            cmakeLists = cmakeLists.Replace("%AotModulesSource%", string.IsNullOrEmpty(aotSources) ? "" : "modules.m");
        }

        var defines = new StringBuilder();

        if (invariantGlobalization)
        {
            defines.AppendLine("add_definitions(-DINVARIANT_GLOBALIZATION=1)");
        }

        if (hybridGlobalization)
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

        if (useNativeAotRuntime)
        {
            defines.AppendLine("add_definitions(-DUSE_NATIVE_AOT=1)");
        }
        else
        {
            defines.AppendLine("add_definitions(-DCORECLR=1)");
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

        string envVariables = string.Empty;
        foreach (var item in environmentVariables)
        {
            var split = item.Split('=');
            if (split.Length == 2)
            {
                envVariables += $"\t\tsetenv (\"{split[0].Trim()}\", \"{split[1].Trim()}\", true);\n";
            }
        }

        if (needEntitlements)
        {
            var ent = new StringBuilder();
            foreach ((var key, var value) in entitlements)
            {
                ent.AppendLine($"<key>{key}</key>");
                ent.AppendLine(value);
            }
            string entitlementsTemplate = Utils.GetEmbeddedResource("app.entitlements.template");
            File.WriteAllText(Path.Combine(binDir, "app.entitlements"), entitlementsTemplate.Replace("%Entitlements%", ent.ToString()));
        }

        if (isLibraryMode)
        {
            File.WriteAllText(Path.Combine(binDir, "runtime-librarymode.h"), Utils.GetEmbeddedResource("runtime-librarymode.h"));
            File.WriteAllText(Path.Combine(binDir, "runtime-librarymode.m"), Utils.GetEmbeddedResource("runtime-librarymode.m"));
        }
        else if (!useNativeAotRuntime)
        {
            File.WriteAllText(Path.Combine(binDir, "runtime.h"),
                Utils.GetEmbeddedResource("runtime.h"));
            File.WriteAllText(Path.Combine(binDir, "coreclrhost.h"),
                Utils.GetEmbeddedResource("coreclrhost.h"));
            File.WriteAllText(Path.Combine(binDir, "host_runtime_contract.h"),
                Utils.GetEmbeddedResource("host_runtime_contract.h"));

            var (appContextKeys, appContextValues, propertyCount) = RenderCoreClrRuntimeConfigProperties(runtimeConfigFile);

            // NOTE: Library mode is not supported yet
            File.WriteAllText(Path.Combine(binDir, "runtime.m"),
                Utils.GetEmbeddedResource("runtime-coreclr.m")
                    .Replace("//%APPLE_RUNTIME_IDENTIFIER%", RuntimeIdentifier)
                    .Replace("%EntryPointLibName%", Path.GetFileName(entryPointLib))
                    .Replace("%EnvVariables%", envVariables)
                    .Replace("%AppContextPropertyCount%", propertyCount.ToString())
                    .Replace("%AppContextKeys%", appContextKeys)
                    .Replace("%AppContextValues%", appContextValues));
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
                        .Append(" IPHONEOS_DEPLOYMENT_TARGET=17.0");
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
                        .Append(" IPHONEOS_DEPLOYMENT_TARGET=17.0");
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

    private static (string appContextKeys, string appContextValues, int propertyCount) RenderCoreClrRuntimeConfigProperties(string? runtimeConfigFile)
    {
        Dictionary<string, string> configProperties = RuntimeConfigProperties.Read(
            runtimeConfigFile,
            "RUNTIME_IDENTIFIER",
            "APP_CONTEXT_BASE_DIRECTORY",
            "TRUSTED_PLATFORM_ASSEMBLIES",
            "HOST_RUNTIME_CONTRACT");

        // Hardcoded properties count: RUNTIME_IDENTIFIER, APP_CONTEXT_BASE_DIRECTORY, TRUSTED_PLATFORM_ASSEMBLIES, HOST_RUNTIME_CONTRACT = 4
        const int hardcodedProperties = 4;

        var appContextKeys = new StringBuilder();
        var appContextValues = new StringBuilder();

        int i = 0;
        foreach ((string key, string value) in configProperties.OrderBy(property => property.Key, StringComparer.Ordinal))
        {
            appContextKeys.AppendLine($"    appctx_keys[{i + hardcodedProperties}] = {RuntimeConfigProperties.ToCStringLiteral(key)};");
            appContextValues.AppendLine($"    appctx_values[{i + hardcodedProperties}] = {RuntimeConfigProperties.ToCStringLiteral(value)};");
            i++;
        }

        int propertyCount = hardcodedProperties + configProperties.Count;

        return (appContextKeys.ToString().TrimEnd(), appContextValues.ToString().TrimEnd(), propertyCount);
    }

}
