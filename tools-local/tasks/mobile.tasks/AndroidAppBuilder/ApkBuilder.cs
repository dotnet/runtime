using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ApkBuilder
{
    private const string DefaultMinApiLevel = "21";

    public string? ProjectName { get; set; }
    public string? AndroidNdk { get; set; }
    public string? AndroidSdk { get; set; }
    public string? MinApiLevel { get; set; }
    public string? BuildApiLevel { get; set; }
    public string? BuildToolsVersion { get; set; }
    public string? OutputDir { get; set; }
    public bool StripDebugSymbols { get; set; }

    public (string apk, string packageId) BuildApk(
        string sourceDir, string abi, string entryPointLib, string monoRuntimeHeaders)
    {
        if (!Directory.Exists(sourceDir))
            throw new ArgumentException($"sourceDir='{sourceDir}' is empty or doesn't exist");

        if (string.IsNullOrEmpty(abi))
            throw new ArgumentException("abi shoudln't be empty (e.g. x86, x86_64, armeabi-v7a or arm64-v8a");

        if (string.IsNullOrEmpty(entryPointLib))
            throw new ArgumentException("entryPointLib shouldn't be empty");

        if (!File.Exists(Path.Combine(sourceDir, entryPointLib)))
            throw new ArgumentException($"{entryPointLib} was not found in sourceDir='{sourceDir}'");

        if (string.IsNullOrEmpty(ProjectName))
            ProjectName = Path.GetFileNameWithoutExtension(entryPointLib);

        if (string.IsNullOrEmpty(OutputDir))
            OutputDir = Path.Combine(sourceDir, "bin-" + abi);

        if (ProjectName.Contains(' '))
            throw new ArgumentException($"ProjectName='{ProjectName}' shouldn't not contain spaces.");

        if (string.IsNullOrEmpty(AndroidSdk))
            AndroidSdk = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

        if (string.IsNullOrEmpty(AndroidNdk))
            AndroidNdk = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");

        if (string.IsNullOrEmpty(AndroidSdk) || !Directory.Exists(AndroidSdk))
            throw new ArgumentException($"Android SDK='{AndroidSdk}' was not found or empty (can be set via ANDROID_SDK_ROOT envvar).");

        if (string.IsNullOrEmpty(AndroidNdk) || !Directory.Exists(AndroidNdk))
            throw new ArgumentException($"Android NDK='{AndroidNdk}' was not found or empty (can be set via ANDROID_NDK_ROOT envvar).");

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
            throw new ArgumentException($"{buildToolsFolder} was not found.");

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

        // Copy AppDir to OutputDir/assets-tozip (ignore native files)
        // these files then will be zipped and copied to apk/assets/assets.zip
        Utils.DirectoryCopy(sourceDir, Path.Combine(OutputDir, "assets-tozip"), file =>
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

        // tools:
        string dx = Path.Combine(buildToolsFolder, "dx");
        string aapt = Path.Combine(buildToolsFolder, "aapt");
        string zipalign = Path.Combine(buildToolsFolder, "zipalign");
        string apksigner = Path.Combine(buildToolsFolder, "apksigner");
        string androidJar = Path.Combine(AndroidSdk, "platforms", "android-" + BuildApiLevel, "android.jar");
        string androidToolchain = Path.Combine(AndroidNdk, "build", "cmake", "android.toolchain.cmake");
        string keytool = "keytool";
        string javac = "javac";
        string cmake = "cmake";
        string zip = "zip";

        Utils.RunProcess(zip, workingDir: Path.Combine(OutputDir, "assets-tozip"), args: "-q -r ../assets/assets.zip .");
        Directory.Delete(Path.Combine(OutputDir, "assets-tozip"), true);
        
        if (!File.Exists(androidJar))
            throw new ArgumentException($"API level={BuildApiLevel} is not downloaded in Android SDK");

        // 1. Build libmonodroid.so` via cmake

        string monoRuntimeLib = Path.Combine(sourceDir, "libmonosgen-2.0.a");
        if (!File.Exists(monoRuntimeLib))
            throw new ArgumentException($"libmonosgen-2.0.a was not found in {sourceDir}");

        string cmakeLists = Utils.GetEmbeddedResource("CMakeLists-android.txt")
            .Replace("%MonoInclude%", monoRuntimeHeaders)
            .Replace("%NativeLibrariesToLink%", monoRuntimeLib);
        File.WriteAllText(Path.Combine(OutputDir, "CMakeLists.txt"), cmakeLists);

        string monodroidSrc = Utils.GetEmbeddedResource("monodroid.c")
            .Replace("%EntryPointLibName%", Path.GetFileName(entryPointLib)
            .Replace("%RID%", GetRid(abi)));
        File.WriteAllText(Path.Combine(OutputDir, "monodroid.c"), monodroidSrc);
        
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

        Utils.RunProcess(cmake, workingDir: OutputDir, args: cmakeGenArgs);
        Utils.RunProcess(cmake, workingDir: OutputDir, args: cmakeBuildArgs);

        // 2. Compile Java files

        string javaSrcFolder = Path.Combine(OutputDir, "src", "net", "dot");
        Directory.CreateDirectory(javaSrcFolder);

        string packageId = $"net.dot.{ProjectName}";

        File.WriteAllText(Path.Combine(javaSrcFolder, "MainActivity.java"), 
            Utils.GetEmbeddedResource("MainActivity.java"));
        File.WriteAllText(Path.Combine(javaSrcFolder, "MonoRunner.java"), 
            Utils.GetEmbeddedResource("MonoRunner.java"));
        File.WriteAllText(Path.Combine(OutputDir, "AndroidManifest.xml"), 
            Utils.GetEmbeddedResource("AndroidManifest.xml")
                .Replace("%PackageName%", packageId)
                .Replace("%MinSdkLevel%", MinApiLevel));

        string javaCompilerArgs = $"-d obj -classpath src -bootclasspath {androidJar} -source 1.8 -target 1.8 ";
        Utils.RunProcess(javac, javaCompilerArgs + Path.Combine(javaSrcFolder, "MainActivity.java"), workingDir: OutputDir);
        Utils.RunProcess(javac, javaCompilerArgs + Path.Combine(javaSrcFolder, "MonoRunner.java"), workingDir: OutputDir);
        Utils.RunProcess(dx, "--dex --output=classes.dex obj", workingDir: OutputDir);

        // 3. Generate APK

        string apkFile = Path.Combine(OutputDir, "bin", $"{ProjectName}.unaligned.apk");
        Utils.RunProcess(aapt, $"package -f -m -F {apkFile} -A assets -M AndroidManifest.xml -I {androidJar}", workingDir: OutputDir);
        
        var dynamicLibs = new List<string>();
        dynamicLibs.Add(Path.Combine(OutputDir, "monodroid", "libmonodroid.so"));
        dynamicLibs.AddRange(Directory.GetFiles(sourceDir, "*.so"));

        // add all *.so files to lib/%abi%/
        Directory.CreateDirectory(Path.Combine(OutputDir, "lib", abi));
        foreach (var dynamicLib in dynamicLibs)
        {
            string dynamicLibName = Path.GetFileName(dynamicLib);
            if (dynamicLibName == "libmonosgen-2.0.so")
            {
                // we link mono runtime statically into libmonodroid.so
                continue;
            }

            // NOTE: we can run android-strip tool from NDK to shrink native binaries here even more.

            string destRelative = Path.Combine("lib", abi, dynamicLibName);
            File.Copy(dynamicLib, Path.Combine(OutputDir, destRelative), true);
            Utils.RunProcess(aapt, $"add {apkFile} {destRelative}", workingDir: OutputDir);
        }
        Utils.RunProcess(aapt, $"add {apkFile} classes.dex", workingDir: OutputDir);

        // 4. Align APK

        string alignedApk = Path.Combine(OutputDir, "bin", $"{ProjectName}.apk");
        Utils.RunProcess(zipalign, $"-v 4 {apkFile} {alignedApk}", workingDir: OutputDir);
        // we don't need the unaligned one any more
        File.Delete(apkFile);

        // 5. Generate key
        
        string signingKey = Path.Combine(OutputDir, "debug.keystore");
        if (!File.Exists(signingKey))
        {
            Utils.RunProcess(keytool, "-genkey -v -keystore debug.keystore -storepass android -alias " +
                "androiddebugkey -keypass android -keyalg RSA -keysize 2048 -noprompt " +
                "-dname \"CN=Android Debug,O=Android,C=US\"", workingDir: OutputDir, silent: true);
        }

        // 6. Sign APK

        Utils.RunProcess(apksigner, $"sign --min-sdk-version {MinApiLevel} --ks debug.keystore " + 
            $"--ks-pass pass:android --key-pass pass:android {alignedApk}", workingDir: OutputDir);

        Utils.LogInfo($"\nAPK size: {(new FileInfo(alignedApk).Length / 1000_000.0):0.#} Mb.\n");

        return (alignedApk, packageId);
    }

    private static string GetRid(string abi) => abi switch 
        {
            "arm64-v8a" => "android-arm64",
            "armeabi-v7a" => "android-arm",
            "x86_64" => "android-x64",
            _ => "android-" + abi
        };
    
    /// <summary>
    /// Scan android SDK for build tools (ignore preview versions)
    /// </summary>
    private static string GetLatestBuildTools(string androidSdkDir)
    {
        string? buildTools = Directory.GetDirectories(Path.Combine(androidSdkDir, "build-tools"))
            .Select(Path.GetFileName)
            .Where(file => !file!.Contains("-"))
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
