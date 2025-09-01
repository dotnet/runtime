// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Android.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public enum RuntimeFlavorEnum
{
    Mono,
    CoreCLR,
    NativeAOT
}

public partial class ApkBuilder
{
    private const string DefaultMinApiLevel = "21";
    private const string DefaultTargetApiLevel = "31";

    public string? ProjectName { get; set; }
    public string? AppDir { get; set; }
    public string? AndroidNdk { get; set; }
    public string? AndroidSdk { get; set; }
    public string? MinApiLevel { get; set; }
    public string? TargetApiLevel { get; set; }
    public string? BuildApiLevel { get; set; }
    public string? BuildToolsVersion { get; set; }
    public string OutputDir { get; set; } = ""!;
    public bool StripDebugSymbols { get; set; }
    public string? NativeMainSource { get; set; }
    public string? KeyStorePath { get; set; }
    public bool ForceInterpreter { get; set; }
    public bool ForceAOT { get; set; }
    public bool ForceFullAOT { get; set; }
    public ITaskItem[] EnvironmentVariables { get; set; } = Array.Empty<ITaskItem>();
    public bool EnableRuntimeLogging { get; set; }
    public bool StaticLinkedRuntime { get; set; }
    public string[] RuntimeComponents { get; set; } = Array.Empty<string>();
    public string? DiagnosticPorts { get; set; }
    public bool IsLibraryMode { get; set; }
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();
    public ITaskItem[] ExtraLinkerArguments { get; set; } = Array.Empty<ITaskItem>();
    public string[] NativeDependencies { get; set; } = Array.Empty<string>();
    public string RuntimeFlavor { get; set; } = nameof(RuntimeFlavorEnum.Mono);

    private RuntimeFlavorEnum parsedRuntimeFlavor;
    private bool IsMono => parsedRuntimeFlavor == RuntimeFlavorEnum.Mono;
    private bool IsCoreCLR => parsedRuntimeFlavor == RuntimeFlavorEnum.CoreCLR;
    private bool IsNativeAOT => parsedRuntimeFlavor == RuntimeFlavorEnum.NativeAOT;

    private TaskLoggingHelper logger;

    public ApkBuilder(TaskLoggingHelper logger)
    {
        this.logger = logger;
    }

    public (string apk, string packageId) BuildApk(
        string runtimeIdentifier,
        string mainLibraryFileName,
        string[] runtimeHeaders)
    {
        if (!Enum.TryParse(RuntimeFlavor, true, out parsedRuntimeFlavor))
        {
            throw new ArgumentException($"Unknown RuntimeFlavor value: {RuntimeFlavor}. '{nameof(RuntimeFlavor)}' must be one of: {string.Join(",", Enum.GetNames(typeof(RuntimeFlavorEnum)))}");
        }

        if (string.IsNullOrEmpty(AppDir) || !Directory.Exists(AppDir))
        {
            throw new ArgumentException($"AppDir='{AppDir}' is empty or doesn't exist");
        }

        if (!string.IsNullOrEmpty(mainLibraryFileName) && !File.Exists(Path.Combine(AppDir, mainLibraryFileName)))
        {
            throw new ArgumentException($"MainLibraryFileName='{mainLibraryFileName}' was not found in AppDir='{AppDir}'");
        }

        if (string.IsNullOrEmpty(runtimeIdentifier))
        {
            throw new ArgumentException("RuntimeIdentifier should not be empty and should contain a valid android RID");
        }

        if (!string.IsNullOrEmpty(ProjectName) && ProjectName.Contains(' '))
        {
            throw new ArgumentException($"ProjectName='{ProjectName}' should not not contain spaces.");
        }

        if (string.IsNullOrEmpty(AndroidSdk))
        {
            AndroidSdk = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        }

        if (string.IsNullOrEmpty(AndroidNdk))
        {
            AndroidNdk = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");
        }

        if (string.IsNullOrEmpty(AndroidSdk) || !Directory.Exists(AndroidSdk))
        {
            throw new ArgumentException($"Android SDK='{AndroidSdk}' was not found or empty (can be set via ANDROID_SDK_ROOT envvar).");
        }

        if (string.IsNullOrEmpty(AndroidNdk) || !Directory.Exists(AndroidNdk))
        {
            throw new ArgumentException($"Android NDK='{AndroidNdk}' was not found or empty (can be set via ANDROID_NDK_ROOT envvar).");
        }

        if (ForceInterpreter && ForceAOT)
        {
            throw new InvalidOperationException("Interpreter and AOT cannot be enabled at the same time");
        }

        if (IsMono && !string.IsNullOrEmpty(DiagnosticPorts) && !Array.Exists(RuntimeComponents, runtimeComponent => string.Equals(runtimeComponent, "diagnostics_tracing", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Using DiagnosticPorts targeting Mono requires diagnostics_tracing runtime component, which was not included in 'RuntimeComponents' item group. @RuntimeComponents: '{string.Join(", ", RuntimeComponents)}'");
        }

        AndroidSdkHelper androidSdkHelper = new AndroidSdkHelper(AndroidSdk, BuildApiLevel, BuildToolsVersion);

        if (string.IsNullOrEmpty(MinApiLevel))
            MinApiLevel = DefaultMinApiLevel;

        if (string.IsNullOrEmpty(TargetApiLevel))
            TargetApiLevel = DefaultTargetApiLevel;

        // make sure BuildApiLevel >= MinApiLevel and BuildApiLevel >= TargetApiLevel
        // only if these api levels are not "preview" (not integers)
        if (int.TryParse(androidSdkHelper.BuildApiLevel, out int intApi))
        {
            if (int.TryParse(MinApiLevel, out int intMinApi) && intApi < intMinApi)
            {
                throw new ArgumentException($"BuildApiLevel={androidSdkHelper.BuildApiLevel} < MinApiLevel={MinApiLevel}. " +
                    "Make sure you've downloaded some recent build-tools in Android SDK");
            }

            if (int.TryParse(TargetApiLevel, out int intTargetApi) && intApi < intTargetApi)
            {
                throw new ArgumentException($"BuildApiLevel={androidSdkHelper.BuildApiLevel} < TargetApiLevel={TargetApiLevel}. " +
                    "Make sure you've downloaded some recent build-tools in Android SDK");
            }
        }

        if (!Enum.TryParse(RuntimeFlavor, true, out parsedRuntimeFlavor))
        {
            throw new ArgumentException($"Unknown RuntimeFlavor value: {RuntimeFlavor}. '{nameof(RuntimeFlavor)}' must be one of: {string.Join(",", Enum.GetNames(typeof(RuntimeFlavorEnum)))}");
        }

        var assemblerFiles = new StringBuilder();
        var assemblerFilesToLink = new StringBuilder();
        var aotLibraryFiles = new List<string>();

        if (!(IsLibraryMode || IsNativeAOT))
        {
            foreach (ITaskItem file in Assemblies)
            {
                // use AOT files if available
                var obj = file.GetMetadata("AssemblerFile");
                var llvmObj = file.GetMetadata("LlvmObjectFile");
                var lib = file.GetMetadata("LibraryFile");

                if (!string.IsNullOrEmpty(obj))
                {
                    var name = Path.GetFileNameWithoutExtension(obj);
                    assemblerFiles.AppendLine($"add_library({name} OBJECT {obj})");
                    assemblerFilesToLink.AppendLine($"    {name}");
                }

                if (!string.IsNullOrEmpty(llvmObj))
                {
                    var name = Path.GetFileNameWithoutExtension(llvmObj);
                    assemblerFilesToLink.AppendLine($"    {llvmObj}");
                }

                if (!string.IsNullOrEmpty(lib))
                {
                    aotLibraryFiles.Add(lib);
                }
            }

            if (ForceAOT && assemblerFiles.Length == 0 && aotLibraryFiles.Count == 0)
            {
                throw new InvalidOperationException("Need list of AOT files.");
            }
        }

        Directory.CreateDirectory(OutputDir);
        Directory.CreateDirectory(Path.Combine(OutputDir, "bin"));
        Directory.CreateDirectory(Path.Combine(OutputDir, "obj"));
        Directory.CreateDirectory(Path.Combine(OutputDir, "assets-tozip"));
        Directory.CreateDirectory(Path.Combine(OutputDir, "assets"));

        var extensionsToIgnore = new List<string> { ".so", ".a", ".dex", ".jar" };
        extensionsToIgnore.Add(".pdb");
        extensionsToIgnore.Add(".dbg");

        // Copy sourceDir to OutputDir/assets-tozip (ignore native files)
        // these files then will be zipped and copied to apk/assets/assets.zip
        var assetsToZipDirectory = Path.Combine(OutputDir, "assets-tozip");

        Utils.DirectoryCopy(AppDir, assetsToZipDirectory, file =>
        {
            string fileName = Path.GetFileName(file);
            string extension = Path.GetExtension(file);

            if (extensionsToIgnore.Contains(extension))
            {
                // ignore native files, those go to lib/%abi%
                // also, aapt is not happy about zip files
                return false;
            }
            if (fileName.StartsWith('.'))
            {
                // aapt complains on such files
                return false;
            }
            return true;
        });

        // add AOT .so libraries
        foreach (var aotlib in aotLibraryFiles)
        {
            File.Copy(aotlib, Path.Combine(assetsToZipDirectory, Path.GetFileName(aotlib)));
        }

        string androidJar = Path.Combine(AndroidSdk, "platforms", "android-" + androidSdkHelper.BuildApiLevel, "android.jar");
        string androidToolchain = Path.Combine(AndroidNdk, "build", "cmake", "android.toolchain.cmake");
        string javac = "javac";

        ZipFile.CreateFromDirectory(assetsToZipDirectory, Path.Combine(OutputDir, "assets", "assets.zip"), CompressionLevel.SmallestSize, includeBaseDirectory: false);
        Directory.Delete(assetsToZipDirectory, true);

        if (!File.Exists(androidJar))
            throw new ArgumentException($"API level={androidSdkHelper.BuildApiLevel} is not downloaded in Android SDK");

        // 1. Build libmonodroid.so via cmake

        string nativeLibraries = "";
        if (IsLibraryMode)
        {
            nativeLibraries = string.Join("\n    ", NativeDependencies.Select(dep => dep));
        }
        else if (!IsNativeAOT)
        {
            string runtimeLib = "";
            if (StaticLinkedRuntime && IsMono)
            {
                runtimeLib = Path.Combine(AppDir, "libmonosgen-2.0.a");
            }
            else if (IsMono)
            {
                runtimeLib = Path.Combine(AppDir, "libmonosgen-2.0.so");
            }
            else if (StaticLinkedRuntime && IsCoreCLR)
            {
                runtimeLib = Path.Combine(AppDir, "libcoreclr_static.a");
            }
            else if (IsCoreCLR)
            {
                runtimeLib = Path.Combine(AppDir, "libcoreclr.so");
            }

            if (!File.Exists(runtimeLib))
            {
                throw new ArgumentException($"{runtimeLib} was not found");
            }
            else
            {
                nativeLibraries += $"{runtimeLib}{Environment.NewLine}";
            }

            if (StaticLinkedRuntime && IsMono)
            {
                string[] staticComponentStubLibs = Directory.GetFiles(AppDir, "libmono-component-*-stub-static.a");

                // by default, component stubs will be linked and depending on how mono runtime has been build,
                // stubs can disable or dynamic load components.
                foreach (string staticComponentStubLib in staticComponentStubLibs)
                {
                    string componentLibToLink = staticComponentStubLib;
                    foreach (string runtimeComponent in RuntimeComponents)
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
                        logger.LogMessage(MessageImportance.High, $"\nCouldn't find static component library: {componentLibToLink}, linking static component stub library: {staticComponentStubLib}.\n");
                        componentLibToLink = staticComponentStubLib;
                    }

                    nativeLibraries += $"    {componentLibToLink}{Environment.NewLine}";
                }

                // There's a circular dependency between static mono runtime lib and static component libraries.
                // Adding mono runtime lib before and after component libs will resolve issues with undefined symbols
                // due to circular dependency.
                nativeLibraries += $"    {runtimeLib}{Environment.NewLine}";
            }

            if (StaticLinkedRuntime && IsCoreCLR)
            {
                string[] staticMonoStubs = Directory.GetFiles(AppDir, "libmono*.a");
                string[] staticLibs = Directory.GetFiles(AppDir, "*.a")
                    .Where(lib => !Path.GetFileName(lib).Equals("libcoreclr_static.a", StringComparison.OrdinalIgnoreCase) &&
                                  !staticMonoStubs.Contains(lib, StringComparer.OrdinalIgnoreCase))
                    .ToArray();

                foreach (string lib in staticLibs)
                {
                    nativeLibraries += $"    {lib}{Environment.NewLine}";
                }

                nativeLibraries += $"    libc++abi.a{Environment.NewLine}";
                nativeLibraries += $"    libc++_static.a{Environment.NewLine}";
            }
        }
        string abi;
        if (IsNativeAOT)
        {
            abi = AndroidProject.DetermineAbi(runtimeIdentifier);
        }
        else
        {
            StringBuilder extraLinkerArgs = new StringBuilder();
            foreach (ITaskItem item in ExtraLinkerArguments)
            {
                extraLinkerArgs.AppendLine($"    \"{item.ItemSpec}\"");
            }

            if (StaticLinkedRuntime && IsCoreCLR)
            {
                // Ensure global symbol references in the shared library are resolved to definitions in
                // the same shared library. For the static linked runtime specifically, we need this for
                // global functions in assembly for the linker to treat relative offsets to them as constant
                extraLinkerArgs.AppendLine($"    \"-Wl,-Bsymbolic\"");
            }

            nativeLibraries += assemblerFilesToLink.ToString();

            string aotSources = assemblerFiles.ToString();
            string monodroidSource = IsCoreCLR ?
                "monodroid-coreclr.c" : (IsLibraryMode) ? "monodroid-librarymode.c" : "monodroid.c";
            string runtimeInclude = string.Join(" ", runtimeHeaders.Select(h => $"\"{NormalizePathToUnix(h)}\""));

            string cmakeLists = Utils.GetEmbeddedResource("CMakeLists-android.txt")
                .Replace("%RuntimeInclude%", runtimeInclude)
                .Replace("%NativeLibrariesToLink%", NormalizePathToUnix(nativeLibraries))
                .Replace("%MONODROID_SOURCE%", monodroidSource)
                .Replace("%AotSources%", NormalizePathToUnix(aotSources))
                .Replace("%AotModulesSource%", string.IsNullOrEmpty(aotSources) ? "" : "modules.c")
                .Replace("%APP_LINKER_ARGS%", extraLinkerArgs.ToString());

            var defines = new StringBuilder();
            if (ForceInterpreter)
            {
                defines.AppendLine("add_definitions(-DFORCE_INTERPRETER=1)");
            }
            else if (ForceAOT)
            {
                defines.AppendLine("add_definitions(-DFORCE_AOT=1)");
                if (aotLibraryFiles.Count == 0)
                {
                    defines.AppendLine("add_definitions(-DSTATIC_AOT=1)");
                }
            }

            if (ForceFullAOT)
            {
                defines.AppendLine("add_definitions(-DFULL_AOT=1)");
            }

            if (!string.IsNullOrEmpty(DiagnosticPorts))
            {
                defines.AppendLine("add_definitions(-DDIAGNOSTIC_PORTS=\"" + DiagnosticPorts + "\")");
            }

            cmakeLists = cmakeLists.Replace("%Defines%", defines.ToString());

            File.WriteAllText(Path.Combine(OutputDir, "CMakeLists.txt"), cmakeLists);

            string monodroidContent = Utils.GetEmbeddedResource(monodroidSource);
            if (IsCoreCLR)
            {
                monodroidContent = RenderMonodroidCoreClrTemplate(monodroidContent);
            }
            File.WriteAllText(Path.Combine(OutputDir, monodroidSource), monodroidContent);

            AndroidProject project = new AndroidProject("monodroid", runtimeIdentifier, AndroidNdk, logger);
            project.GenerateCMake(OutputDir, MinApiLevel, StripDebugSymbols);
            project.BuildCMake(OutputDir, StripDebugSymbols);
            abi = project.Abi;

            // TODO: https://github.com/dotnet/runtime/issues/115717
        }

        // 2. Compile Java files

        string javaSrcFolder = Path.Combine(OutputDir, "src", "net", "dot");
        Directory.CreateDirectory(javaSrcFolder);

        string javaActivityPath = Path.Combine(javaSrcFolder, "MainActivity.java");
        string monoRunnerPath = Path.Combine(javaSrcFolder, "MonoRunner.java");

        Regex checkNumerics = DotNumberRegex();
        if (!string.IsNullOrEmpty(ProjectName) && checkNumerics.IsMatch(ProjectName))
            ProjectName = checkNumerics.Replace(ProjectName, @"_$1");

        if (!string.IsNullOrEmpty(ProjectName) && ProjectName.Contains('-'))
            ProjectName = ProjectName.Replace("-", "_");

        string packageId = $"net.dot.{ProjectName}";

        File.WriteAllText(javaActivityPath,
            Utils.GetEmbeddedResource("MainActivity.java")
                .Replace("%EntryPointLibName%", Path.GetFileName(mainLibraryFileName)));

        if (!string.IsNullOrEmpty(NativeMainSource))
            File.Copy(NativeMainSource, javaActivityPath, true);

        string envVariables = "";
        foreach (ITaskItem item in EnvironmentVariables)
        {
            string name = item.ItemSpec;
            string value = item.GetMetadata("Value");
            envVariables += $"\t\tsetEnv(\"{name}\", \"{value}\");\n";
        }

        string jniLibraryName;
        if (IsLibraryMode || IsNativeAOT)
            jniLibraryName = ProjectName!;
        else if (StaticLinkedRuntime && IsCoreCLR)
            jniLibraryName = "monodroid";
        else
            jniLibraryName = "System.Security.Cryptography.Native.Android";

        List<string> librariesToLoad = [jniLibraryName];
        if (!IsNativeAOT)
            librariesToLoad.Add("monodroid");

        string monoRunner = Utils.GetEmbeddedResource("MonoRunner.java")
            .Replace("%EntryPointLibName%", Path.GetFileName(mainLibraryFileName))
            .Replace("%LoadLibraryStatements%",
                string.Join('\n', librariesToLoad.Select(l => $"System.loadLibrary(\"{l}\");")))
            .Replace("%EnvVariables%", envVariables);

        File.WriteAllText(monoRunnerPath, monoRunner);

        File.WriteAllText(Path.Combine(OutputDir, "AndroidManifest.xml"),
            Utils.GetEmbeddedResource("AndroidManifest.xml")
                .Replace("%PackageName%", packageId)
                .Replace("%MinSdkLevel%", MinApiLevel)
                .Replace("%TargetSdkVersion%", TargetApiLevel));

        string javaCompilerArgs = $"-d obj -classpath src -bootclasspath {androidJar} -source 1.8 -target 1.8 ";
        Utils.RunProcess(logger, javac, javaCompilerArgs + javaActivityPath, workingDir: OutputDir);
        Utils.RunProcess(logger, javac, javaCompilerArgs + monoRunnerPath, workingDir: OutputDir);

        if (androidSdkHelper.HasD8)
        {
            string[] classFiles = Directory.GetFiles(Path.Combine(OutputDir, "obj"), "*.class", SearchOption.AllDirectories);

            if (classFiles.Length == 0)
                throw new InvalidOperationException("Didn't find any .class files");

            Utils.RunProcess(logger, androidSdkHelper.D8Path, $"--no-desugaring {string.Join(" ", classFiles)}", workingDir: OutputDir);
        }
        else
        {
            Utils.RunProcess(logger, androidSdkHelper.DxPath, "--dex --output=classes.dex obj", workingDir: OutputDir);
        }

        // 3. Generate APK

        string debugModeArg = StripDebugSymbols ? string.Empty : "--debug-mode";
        string apkFile = Path.Combine(OutputDir, "bin", $"{ProjectName}.unaligned.apk");
        Utils.RunProcess(logger, androidSdkHelper.AaptPath, $"package -f -m -F {apkFile} -A assets -M AndroidManifest.xml -I {androidJar} {debugModeArg}", workingDir: OutputDir);

        var dynamicLibs = new List<string>();
        if (!IsNativeAOT)
            dynamicLibs.Add(Path.Combine(OutputDir, "monodroid", "libmonodroid.so"));

        if (IsLibraryMode)
        {
            dynamicLibs.AddRange(NativeDependencies);
        }
        else
        {
            var excludedLibs = new HashSet<string> { "libmonodroid.so" };
            if (IsCoreCLR)
            {
                if (StripDebugSymbols)
                {
                    // exclude debugger support libs
                    excludedLibs.Add("libmscordbi.so");
                    excludedLibs.Add("libmscordaccore.so");
                }
            }
            if (!StaticLinkedRuntime)
                dynamicLibs.AddRange(Directory.GetFiles(AppDir, "*.so").Where(file => !excludedLibs.Contains(Path.GetFileName(file))));
        }

        // add all *.so files to lib/%abi%/
        Directory.CreateDirectory(Path.Combine(OutputDir, "lib", abi));
        foreach (var dynamicLib in dynamicLibs)
        {
            string dynamicLibName = Path.GetFileName(dynamicLib);
            string destRelative = Path.Combine("lib", abi, dynamicLibName);

            if (dynamicLibName == "libmonosgen-2.0.so" && StaticLinkedRuntime)
            {
                // we link mono runtime statically into libmonodroid.so
                // make sure dynamic runtime is not included in package.
                if (File.Exists(destRelative))
                    File.Delete(destRelative);
                continue;
            }

            if (dynamicLibName.Contains("libmono-component-", StringComparison.OrdinalIgnoreCase))
            {
                bool includeComponent = false;
                if (!StaticLinkedRuntime)
                {
                    foreach (string runtimeComponent in RuntimeComponents)
                    {
                        if (dynamicLibName.Contains(runtimeComponent, StringComparison.OrdinalIgnoreCase))
                        {
                            includeComponent = true;
                            break;
                        }
                    }
                }

                if (!includeComponent)
                {
                    // make sure dynamic component is not included in package.
                    if (File.Exists(destRelative))
                        File.Delete(destRelative);
                    continue;
                }
            }

            // NOTE: we can run android-strip tool from NDK to shrink native binaries here even more.
            File.Copy(dynamicLib, Path.Combine(OutputDir, destRelative), true);
            Utils.RunProcess(logger, androidSdkHelper.AaptPath, $"add {apkFile} {NormalizePathToUnix(destRelative)}", workingDir: OutputDir);
        }
        Utils.RunProcess(logger, androidSdkHelper.AaptPath, $"add {apkFile} classes.dex", workingDir: OutputDir);

        // Include prebuilt .dex files
        int sequence = 2;
        var dexFiles = Directory.GetFiles(AppDir, "*.dex");
        foreach (var dexFile in dexFiles)
        {
            var classesFileName = $"classes{sequence++}.dex";
            File.Copy(dexFile, Path.Combine(OutputDir, classesFileName));
            logger.LogMessage(MessageImportance.High, $"Adding dex file {Path.GetFileName(dexFile)} as {classesFileName}");
            Utils.RunProcess(logger, androidSdkHelper.AaptPath, $"add {apkFile} {classesFileName}", workingDir: OutputDir);
        }

        // 4. Align APK

        string alignedApk = Path.Combine(OutputDir, "bin", $"{ProjectName}.apk");
        AlignApk(apkFile, alignedApk, androidSdkHelper.ZipalignPath);
        // we don't need the unaligned one any more
        File.Delete(apkFile);

        // 5. Generate key (if needed) & sign the apk
        SignApk(alignedApk, androidSdkHelper.ApksignerPath);

        logger.LogMessage(MessageImportance.High, $"\nAPK size: {(new FileInfo(alignedApk).Length / 1000_000.0):0.#} Mb.\n");

        return (alignedApk, packageId);
    }

    private void AlignApk(string unalignedApkPath, string apkOutPath, string zipalign)
    {
        Utils.RunProcess(logger, zipalign, $"-v 4 {unalignedApkPath} {apkOutPath}", workingDir: OutputDir);
    }

    private void SignApk(string apkPath, string apksigner)
    {
        string defaultKey = Path.Combine(OutputDir, "debug.keystore");
        string signingKey = string.IsNullOrEmpty(KeyStorePath) ?
            defaultKey : Path.Combine(KeyStorePath, "debug.keystore");

        if (!File.Exists(signingKey))
        {
            Utils.RunProcess(logger, "keytool", "-genkey -v -keystore debug.keystore -storepass android -alias " +
                "androiddebugkey -keypass android -keyalg RSA -keysize 2048 -noprompt " +
                "-dname \"CN=Android Debug,O=Android,C=US\"", workingDir: OutputDir, silent: true);
        }
        else if (Path.GetFullPath(signingKey) != Path.GetFullPath(defaultKey))
        {
            File.Copy(signingKey, Path.Combine(OutputDir, "debug.keystore"));
        }
        Utils.RunProcess(logger, apksigner, $"sign --min-sdk-version {MinApiLevel} --ks debug.keystore " +
            $"--ks-pass pass:android --key-pass pass:android {apkPath}", workingDir: OutputDir);
    }

    public void ZipAndSignApk(string apkPath)
    {
        if (string.IsNullOrEmpty(AndroidSdk))
            AndroidSdk = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

        if (string.IsNullOrEmpty(AndroidSdk) || !Directory.Exists(AndroidSdk))
            throw new ArgumentException($"Android SDK='{AndroidSdk}' was not found or incorrect (can be set via ANDROID_SDK_ROOT envvar).");

        AndroidSdkHelper androidSdkHelper = new AndroidSdkHelper(AndroidSdk, BuildApiLevel, BuildToolsVersion);

        if (string.IsNullOrEmpty(MinApiLevel))
            MinApiLevel = DefaultMinApiLevel;

        string alignedApkPath = $"{apkPath}.aligned";
        AlignApk(apkPath, alignedApkPath, androidSdkHelper.ZipalignPath);
        logger.LogMessage(MessageImportance.High, $"\nMoving '{alignedApkPath}' to '{apkPath}'.\n");
        File.Move(alignedApkPath, apkPath, overwrite: true);
        SignApk(apkPath, androidSdkHelper.ApksignerPath);
    }

    public void ReplaceFileInApk(string file)
    {
        if (string.IsNullOrEmpty(AndroidSdk))
            AndroidSdk = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

        if (string.IsNullOrEmpty(AndroidSdk) || !Directory.Exists(AndroidSdk))
            throw new ArgumentException($"Android SDK='{AndroidSdk}' was not found or incorrect (can be set via ANDROID_SDK_ROOT envvar).");

        AndroidSdkHelper androidSdkHelper = new AndroidSdkHelper(AndroidSdk, BuildApiLevel, BuildToolsVersion);

        if (string.IsNullOrEmpty(MinApiLevel))
            MinApiLevel = DefaultMinApiLevel;

        string apkPath;
        if (string.IsNullOrEmpty(ProjectName))
            apkPath = Directory.GetFiles(Path.Combine(OutputDir, "bin"), "*.apk").First();
        else
            apkPath = Path.Combine(OutputDir, "bin", $"{ProjectName}.apk");

        if (!File.Exists(apkPath))
            throw new Exception($"{apkPath} was not found");

        Utils.RunProcess(logger, androidSdkHelper.AaptPath, $"remove -v bin/{Path.GetFileName(apkPath)} {file}", workingDir: OutputDir);
        Utils.RunProcess(logger, androidSdkHelper.AaptPath, $"add -v bin/{Path.GetFileName(apkPath)} {file}", workingDir: OutputDir);

        // we need to re-sign the apk
        SignApk(apkPath, androidSdkHelper.ApksignerPath);
    }

    private static string NormalizePathToUnix(string path)
    {
        return path.Replace("\\", "/");
    }

    [GeneratedRegex(@"\.(\d)")]
    private static partial Regex DotNumberRegex();

    private string RenderMonodroidCoreClrTemplate(string monodroidContent)
    {
        // At the moment, we only set the AppContext properties, so it's all done here for simplicity.
        // If we need to add more rendering logic, we can refactor this method later.
        var appContextKeys = new StringBuilder();
        appContextKeys.AppendLine("    appctx_keys[0] = \"RUNTIME_IDENTIFIER\";");
        appContextKeys.AppendLine("    appctx_keys[1] = \"APP_CONTEXT_BASE_DIRECTORY\";");
        appContextKeys.AppendLine("    appctx_keys[2] = \"HOST_RUNTIME_CONTRACT\";");

        var appContextValues = new StringBuilder();
        appContextValues.AppendLine("    appctx_values[0] = ANDROID_RUNTIME_IDENTIFIER;");
        appContextValues.AppendLine("    appctx_values[1] = g_bundle_path;");
        appContextValues.AppendLine();
        appContextValues.AppendLine("    char contract_str[19];"); // 0x + 16 hex digits + '\0'
        appContextValues.AppendLine("    snprintf(contract_str, 19, \"0x%zx\", (size_t)(&g_host_contract));");
        appContextValues.AppendLine("    appctx_values[2] = contract_str;");
        appContextValues.AppendLine();

        // Parse runtime config properties and add them to the AppContext keys and values.
        Dictionary<string, string> configProperties = ParseRuntimeConfigProperties();
        int hardwiredAppContextProperties = 3; // For the hardwired AppContext keys and values above.
        int i = 0;
        foreach ((string key, string value) in configProperties)
        {
            appContextKeys.AppendLine($"    appctx_keys[{i + hardwiredAppContextProperties}] = \"{key}\";");
            appContextValues.AppendLine($"    appctx_values[{i + hardwiredAppContextProperties}] = \"{value}\";");
            i++;
        }

        // Replace the template placeholders.
        string updatedContent = monodroidContent.Replace("%AppContextPropertyCount%", (configProperties.Count + hardwiredAppContextProperties).ToString())
            .Replace("%AppContextKeys%", appContextKeys.ToString().TrimEnd())
            .Replace("%AppContextValues%", appContextValues.ToString().TrimEnd());
        return updatedContent;
    }

    private Dictionary<string, string> ParseRuntimeConfigProperties()
    {
        // This method reads the binary runtimeconfig.bin file created by RuntimeConfigParserTask.ConvertDictionaryToBlob.
        // The binary format is: compressed integer count, followed by count pairs of length-prefixed UTF8 strings (key, value).
        // See src/tasks/MonoTargetsTasks/RuntimeConfigParser/RuntimeConfigParser.cs for the corresponding write logic.

        var configProperties = new Dictionary<string, string>();
        string runtimeConfigPath = Path.Combine(AppDir ?? throw new InvalidOperationException("AppDir is not set"), "runtimeconfig.bin");

        try
        {
            byte[] fileBytes = File.ReadAllBytes(runtimeConfigPath);
            unsafe
            {
                fixed (byte* ptr = fileBytes)
                {
                    var blobReader = new BlobReader(ptr, fileBytes.Length);

                    // Read the compressed integer count
                    int count = blobReader.ReadCompressedInteger();

                    // Read each key-value pair
                    for (int i = 0; i < count; i++)
                    {
                        string key = blobReader.ReadSerializedString() ?? string.Empty;
                        string value = blobReader.ReadSerializedString() ?? string.Empty;
                        configProperties[key] = value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogMessage(MessageImportance.High, $"Error while parsing runtime config at {runtimeConfigPath}: {ex.Message}");
        }

        return configProperties;
    }
}
