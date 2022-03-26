// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class ApkBuilder
{
    private const string DefaultMinApiLevel = "21";

    public string? ProjectName { get; set; }
    public string? AppDir { get; set; }
    public string? AndroidNdk { get; set; }
    public string? AndroidSdk { get; set; }
    public string? MinApiLevel { get; set; }
    public string? BuildApiLevel { get; set; }
    public string? BuildToolsVersion { get; set; }
    public string OutputDir { get; set; } = ""!;
    public bool StripDebugSymbols { get; set; }
    public string? NativeMainSource { get; set; }
    public string? KeyStorePath { get; set; }
    public bool ForceInterpreter { get; set; }
    public bool ForceAOT { get; set; }
    public ITaskItem[] EnvironmentVariables { get; set; } = Array.Empty<ITaskItem>();
    public bool InvariantGlobalization { get; set; }
    public bool EnableRuntimeLogging { get; set; }
    public bool StaticLinkedRuntime { get; set; }
    public string? RuntimeComponents { get; set; }
    public string? DiagnosticPorts { get; set; }
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    private TaskLoggingHelper logger;

    public ApkBuilder(TaskLoggingHelper logger)
    {
        this.logger = logger;
    }

    public (string apk, string packageId) BuildApk(
        string abi,
        string mainLibraryFileName,
        string monoRuntimeHeaders)
    {
        if (string.IsNullOrEmpty(AppDir) || !Directory.Exists(AppDir))
        {
            throw new ArgumentException($"AppDir='{AppDir}' is empty or doesn't exist");
        }

        if (!string.IsNullOrEmpty(mainLibraryFileName) && !File.Exists(Path.Combine(AppDir, mainLibraryFileName)))
        {
            throw new ArgumentException($"MainLibraryFileName='{mainLibraryFileName}' was not found in AppDir='{AppDir}'");
        }

        if (string.IsNullOrEmpty(abi))
        {
            throw new ArgumentException("abi should not be empty (e.g. x86, x86_64, armeabi-v7a or arm64-v8a");
        }

        if (!string.IsNullOrEmpty(ProjectName) && ProjectName.Contains(' '))
        {
            throw new ArgumentException($"ProjectName='{ProjectName}' should not not contain spaces.");
        }

        if (string.IsNullOrEmpty(AndroidSdk)){
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

        if (!string.IsNullOrEmpty(DiagnosticPorts))
        {
            bool validDiagnosticsConfig = false;

            if (string.IsNullOrEmpty(RuntimeComponents))
                validDiagnosticsConfig = false;
            else if (RuntimeComponents.Equals("*", StringComparison.OrdinalIgnoreCase))
                validDiagnosticsConfig = true;
            else if (RuntimeComponents.Contains("diagnostics_tracing", StringComparison.OrdinalIgnoreCase))
                validDiagnosticsConfig = true;

            if (!validDiagnosticsConfig)
                throw new ArgumentException("Using DiagnosticPorts require diagnostics_tracing runtime component.");
        }

        // Try to get the latest build-tools version if not specified
        if (string.IsNullOrEmpty(BuildToolsVersion))
            BuildToolsVersion = GetLatestBuildTools(AndroidSdk);

        // Try to get the latest API level if not specified
        if (string.IsNullOrEmpty(BuildApiLevel))
            BuildApiLevel = GetLatestApiLevel(AndroidSdk);

        if (string.IsNullOrEmpty(MinApiLevel))
            MinApiLevel = DefaultMinApiLevel;

        // make sure BuildApiLevel >= MinApiLevel
        // only if these api levels are not "preview" (not integers)
        if (int.TryParse(BuildApiLevel, out int intApi) &&
            int.TryParse(MinApiLevel, out int intMinApi) &&
            intApi < intMinApi)
        {
            throw new ArgumentException($"BuildApiLevel={BuildApiLevel} <= MinApiLevel={MinApiLevel}. " +
                "Make sure you've downloaded some recent build-tools in Android SDK");
        }

        string buildToolsFolder = Path.Combine(AndroidSdk, "build-tools", BuildToolsVersion);
        if (!Directory.Exists(buildToolsFolder))
        {
            throw new ArgumentException($"{buildToolsFolder} was not found.");
        }

        var assemblerFiles = new StringBuilder();
        var assemblerFilesToLink = new StringBuilder();
        var aotLibraryFiles = new List<string>();
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

        Directory.CreateDirectory(OutputDir);
        Directory.CreateDirectory(Path.Combine(OutputDir, "bin"));
        Directory.CreateDirectory(Path.Combine(OutputDir, "obj"));
        Directory.CreateDirectory(Path.Combine(OutputDir, "assets-tozip"));
        Directory.CreateDirectory(Path.Combine(OutputDir, "assets"));

        var extensionsToIgnore = new List<string> { ".so", ".a", ".gz" };
        if (StripDebugSymbols)
        {
            extensionsToIgnore.Add(".pdb");
            extensionsToIgnore.Add(".dbg");
        }

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
            if (fileName.StartsWith("."))
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

        // tools:
        string dx = Path.Combine(buildToolsFolder, "dx");
        string aapt = Path.Combine(buildToolsFolder, "aapt");
        string zipalign = Path.Combine(buildToolsFolder, "zipalign");
        string apksigner = Path.Combine(buildToolsFolder, "apksigner");
        string androidJar = Path.Combine(AndroidSdk, "platforms", "android-" + BuildApiLevel, "android.jar");
        string androidToolchain = Path.Combine(AndroidNdk, "build", "cmake", "android.toolchain.cmake");
        string javac = "javac";
        string cmake = "cmake";
        string zip = "zip";

        Utils.RunProcess(logger, zip, workingDir: assetsToZipDirectory, args: "-q -r ../assets/assets.zip .");
        Directory.Delete(assetsToZipDirectory, true);

        if (!File.Exists(androidJar))
            throw new ArgumentException($"API level={BuildApiLevel} is not downloaded in Android SDK");

        // 1. Build libmonodroid.so` via cmake

        string nativeLibraries = "";
        string monoRuntimeLib = "";
        if (StaticLinkedRuntime)
        {
            monoRuntimeLib = Path.Combine(AppDir, "libmonosgen-2.0.a");
        }
        else
        {
            monoRuntimeLib = Path.Combine(AppDir, "libmonosgen-2.0.so");
        }

        if (!File.Exists(monoRuntimeLib))
        {
            throw new ArgumentException($"{monoRuntimeLib} was not found");
        }
        else
        {
            nativeLibraries += $"{monoRuntimeLib}{Environment.NewLine}";
        }

        if (StaticLinkedRuntime)
        {
            string[] staticComponentStubLibs = Directory.GetFiles(AppDir, "libmono-component-*-stub-static.a");
            bool staticLinkAllComponents = false;
            string[] staticLinkedComponents = Array.Empty<string>();

            if (!string.IsNullOrEmpty(RuntimeComponents) && RuntimeComponents.Equals("*", StringComparison.OrdinalIgnoreCase))
                staticLinkAllComponents = true;
            else if (!string.IsNullOrEmpty(RuntimeComponents))
                staticLinkedComponents = RuntimeComponents.Split(";");

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
                    logger.LogMessage(MessageImportance.High, $"\nCouldn't find static component library: {componentLibToLink}, linking static component stub library: {staticComponentStubLib}.\n");
                    componentLibToLink = staticComponentStubLib;
                }

                nativeLibraries += $"    {componentLibToLink}{Environment.NewLine}";
            }

            // There's a circular dependecy between static mono runtime lib and static component libraries.
            // Adding mono runtime lib before and after component libs will resolve issues with undefined symbols
            // due to circular dependecy.
            nativeLibraries += $"    {monoRuntimeLib}{Environment.NewLine}";
        }

        nativeLibraries += assemblerFilesToLink.ToString();

        string aotSources = assemblerFiles.ToString();

        string cmakeLists = Utils.GetEmbeddedResource("CMakeLists-android.txt")
            .Replace("%MonoInclude%", monoRuntimeHeaders)
            .Replace("%NativeLibrariesToLink%", nativeLibraries)
            .Replace("%AotSources%", aotSources)
            .Replace("%AotModulesSource%", string.IsNullOrEmpty(aotSources) ? "" : "modules.c");

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

        if (!string.IsNullOrEmpty(DiagnosticPorts))
        {
            defines.AppendLine("add_definitions(-DDIAGNOSTIC_PORTS=\"" + DiagnosticPorts + "\")");
        }

        cmakeLists = cmakeLists.Replace("%Defines%", defines.ToString());

        File.WriteAllText(Path.Combine(OutputDir, "CMakeLists.txt"), cmakeLists);

        File.WriteAllText(Path.Combine(OutputDir, "monodroid.c"), Utils.GetEmbeddedResource("monodroid.c"));

        string cmakeGenArgs = $"-DCMAKE_TOOLCHAIN_FILE={androidToolchain} -DANDROID_ABI=\"{abi}\" -DANDROID_STL=none " +
            $"-DANDROID_NATIVE_API_LEVEL={MinApiLevel} -B monodroid";

        string cmakeBuildArgs = "--build monodroid";

        if (StripDebugSymbols)
        {
            // Use "-s" to strip debug symbols, it complains it's unused but it works
            cmakeGenArgs+= " -DCMAKE_BUILD_TYPE=MinSizeRel -DCMAKE_C_FLAGS=\"-s -Wno-unused-command-line-argument\"";
            cmakeBuildArgs += " --config MinSizeRel";
        }
        else
        {
            cmakeGenArgs += " -DCMAKE_BUILD_TYPE=Debug";
            cmakeBuildArgs += " --config Debug";
        }

        Utils.RunProcess(logger, cmake, workingDir: OutputDir, args: cmakeGenArgs);
        Utils.RunProcess(logger, cmake, workingDir: OutputDir, args: cmakeBuildArgs);

        // 2. Compile Java files

        string javaSrcFolder = Path.Combine(OutputDir, "src", "net", "dot");
        Directory.CreateDirectory(javaSrcFolder);

        string javaActivityPath = Path.Combine(javaSrcFolder, "MainActivity.java");
        string monoRunnerPath = Path.Combine(javaSrcFolder, "MonoRunner.java");

        Regex checkNumerics = new Regex(@"\.(\d)");
        if (!string.IsNullOrEmpty(ProjectName) && checkNumerics.IsMatch(ProjectName))
            ProjectName = checkNumerics.Replace(ProjectName, @"_$1");

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

        string monoRunner = Utils.GetEmbeddedResource("MonoRunner.java")
            .Replace("%EntryPointLibName%", Path.GetFileName(mainLibraryFileName))
            .Replace("%EnvVariables%", envVariables);

        File.WriteAllText(monoRunnerPath, monoRunner);

        File.WriteAllText(Path.Combine(OutputDir, "AndroidManifest.xml"),
            Utils.GetEmbeddedResource("AndroidManifest.xml")
                .Replace("%PackageName%", packageId)
                .Replace("%MinSdkLevel%", MinApiLevel));

        string javaCompilerArgs = $"-d obj -classpath src -bootclasspath {androidJar} -source 1.8 -target 1.8 ";
        Utils.RunProcess(logger, javac, javaCompilerArgs + javaActivityPath, workingDir: OutputDir);
        Utils.RunProcess(logger, javac, javaCompilerArgs + monoRunnerPath, workingDir: OutputDir);
        Utils.RunProcess(logger, dx, "--dex --output=classes.dex obj", workingDir: OutputDir);

        // 3. Generate APK

        string debugModeArg = StripDebugSymbols ? string.Empty : "--debug-mode";
        string apkFile = Path.Combine(OutputDir, "bin", $"{ProjectName}.unaligned.apk");
        Utils.RunProcess(logger, aapt, $"package -f -m -F {apkFile} -A assets -M AndroidManifest.xml -I {androidJar} {debugModeArg}", workingDir: OutputDir);

        var dynamicLibs = new List<string>();
        dynamicLibs.Add(Path.Combine(OutputDir, "monodroid", "libmonodroid.so"));
        dynamicLibs.AddRange(Directory.GetFiles(AppDir, "*.so").Where(file => Path.GetFileName(file) != "libmonodroid.so"));

        // add all *.so files to lib/%abi%/

        string[] dynamicLinkedComponents = Array.Empty<string>();
        bool dynamicLinkAllComponents = false;
        if (!StaticLinkedRuntime && !string.IsNullOrEmpty(RuntimeComponents) && RuntimeComponents.Equals("*", StringComparison.OrdinalIgnoreCase))
                dynamicLinkAllComponents = true;
        if (!string.IsNullOrEmpty(RuntimeComponents) && !StaticLinkedRuntime)
            dynamicLinkedComponents = RuntimeComponents.Split(";");

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
                bool includeComponent = dynamicLinkAllComponents;
                if (!StaticLinkedRuntime && !includeComponent)
                {
                    foreach (string dynamicLinkedComponent in dynamicLinkedComponents)
                    {
                        if (dynamicLibName.Contains(dynamicLinkedComponent, StringComparison.OrdinalIgnoreCase))
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
            Utils.RunProcess(logger, aapt, $"add {apkFile} {destRelative}", workingDir: OutputDir);
        }
        Utils.RunProcess(logger, aapt, $"add {apkFile} classes.dex", workingDir: OutputDir);

        // 4. Align APK

        string alignedApk = Path.Combine(OutputDir, "bin", $"{ProjectName}.apk");
        Utils.RunProcess(logger, zipalign, $"-v 4 {apkFile} {alignedApk}", workingDir: OutputDir);
        // we don't need the unaligned one any more
        File.Delete(apkFile);

        // 5. Generate key (if needed) & sign the apk
        SignApk(alignedApk, apksigner);

        logger.LogMessage(MessageImportance.High, $"\nAPK size: {(new FileInfo(alignedApk).Length / 1000_000.0):0.#} Mb.\n");

        return (alignedApk, packageId);
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

    public void ReplaceFileInApk(string file)
    {
        if (string.IsNullOrEmpty(AndroidSdk))
            AndroidSdk = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

        if (string.IsNullOrEmpty(AndroidSdk) || !Directory.Exists(AndroidSdk))
            throw new ArgumentException($"Android SDK='{AndroidSdk}' was not found or incorrect (can be set via ANDROID_SDK_ROOT envvar).");

        if (string.IsNullOrEmpty(BuildToolsVersion))
            BuildToolsVersion = GetLatestBuildTools(AndroidSdk);

        if (string.IsNullOrEmpty(MinApiLevel))
            MinApiLevel = DefaultMinApiLevel;

        string buildToolsFolder = Path.Combine(AndroidSdk, "build-tools", BuildToolsVersion);
        string aapt = Path.Combine(buildToolsFolder, "aapt");
        string apksigner = Path.Combine(buildToolsFolder, "apksigner");

        string apkPath;
        if (string.IsNullOrEmpty(ProjectName))
            apkPath = Directory.GetFiles(Path.Combine(OutputDir, "bin"), "*.apk").First();
        else
            apkPath = Path.Combine(OutputDir, "bin", $"{ProjectName}.apk");

        if (!File.Exists(apkPath))
            throw new Exception($"{apkPath} was not found");

        Utils.RunProcess(logger, aapt, $"remove -v bin/{Path.GetFileName(apkPath)} {file}", workingDir: OutputDir);
        Utils.RunProcess(logger, aapt, $"add -v bin/{Path.GetFileName(apkPath)} {file}", workingDir: OutputDir);

        // we need to re-sign the apk
        SignApk(apkPath, apksigner);
    }

    /// <summary>
    /// Scan android SDK for build tools (ignore preview versions)
    /// </summary>
    private static string GetLatestBuildTools(string androidSdkDir)
    {
        string? buildTools = Directory.GetDirectories(Path.Combine(androidSdkDir, "build-tools"))
            .Select(Path.GetFileName)
            .Where(file => !file!.Contains('-'))
            .Select(file => { Version.TryParse(Path.GetFileName(file), out Version? version); return version; })
            .OrderByDescending(v => v)
            .FirstOrDefault()?.ToString();

        if (string.IsNullOrEmpty(buildTools))
            throw new ArgumentException($"Android SDK ({androidSdkDir}) doesn't contain build-tools.");

        return buildTools;
    }

    /// <summary>
    /// Scan android SDK for api levels (ignore preview versions)
    /// </summary>
    private static string GetLatestApiLevel(string androidSdkDir)
    {
        return Directory.GetDirectories(Path.Combine(androidSdkDir, "platforms"))
            .Select(file => int.TryParse(Path.GetFileName(file).Replace("android-", ""), out int apiLevel) ? apiLevel : -1)
            .OrderByDescending(v => v)
            .FirstOrDefault()
            .ToString();
    }
}
