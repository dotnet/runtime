using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ApkBuilder
{
    public static (string apk, string packageId) BuildApk(
        string appName,
        string workplace,
        string appDir,
        string entryPointLib,
        string monoRuntimeHeaders,
        string sdkPath,
        string ndkPath,
        string abi,
        string minApiLevel,
        string? buildApiLevel = null,
        string? buildToolsVer = null)
    {
        if (string.IsNullOrEmpty(appName) || appName.Contains(' '))
            throw new ArgumentException($"appName='{appName}' shouldn't not be empty or contain spaces.");
        
        if (!Directory.Exists(sdkPath))
            throw new ArgumentException($"Android SDK='{sdkPath}' was not found.");

        if (!Directory.Exists(ndkPath))
            throw new ArgumentException($"Android NDK='{ndkPath}' was not found.");

        // Try to get the latest build-tools version if not specified
        if (string.IsNullOrEmpty(buildToolsVer))
            buildToolsVer = GetLatestBuildTools(sdkPath);

        // Try to get the latest API level if not specified
        if (string.IsNullOrEmpty(buildApiLevel))
            buildApiLevel = GetLatestApiLevel(sdkPath);
        
        // make sure buildApiLevel >= minApiLevel
        // only if these api levels are not "preview" (not integers)
        if (int.TryParse(buildApiLevel, out int intApi) && 
            int.TryParse(minApiLevel, out int intMinApi) && 
            intApi < intMinApi)
            throw new ArgumentException($"buildApiLevel={buildApiLevel} <= minApiLevel={minApiLevel}. " +
                "Make sure you've downloaded some recent build-tools in Android SDK");

        string buildToolsFolder = Path.Combine(sdkPath, "build-tools", buildToolsVer);
        if (!Directory.Exists(buildToolsFolder))
            throw new ArgumentException($"{buildToolsFolder} was not found.");

        Directory.CreateDirectory(workplace);
        Directory.CreateDirectory(Path.Combine(workplace, "bin"));
        Directory.CreateDirectory(Path.Combine(workplace, "obj"));
        Directory.CreateDirectory(Path.Combine(workplace, "assets"));
        
        // Copy AppDir to workplace/assets (ignore native files)
        Utils.DirectoryCopy(appDir, Path.Combine(workplace, "assets"), file =>
        {
            var extension = Path.GetExtension(file);
            // ignore native files, those go to lib/%abi%
            if (extension == ".so" || extension == ".a")
            {
                // ignore ".pdb" and ".dbg" to make APK smaller
                return false;
            }
            return true;
        });

        // tools:
        string dx = Path.Combine(buildToolsFolder, "dx");
        string aapt = Path.Combine(buildToolsFolder, "aapt");
        string apksigner = Path.Combine(buildToolsFolder, "apksigner");
        string androidJar = Path.Combine(sdkPath, "platforms", "android-" + buildApiLevel, "android.jar");
        string androidToolchain = Path.Combine(ndkPath, "build", "cmake", "android.toolchain.cmake");
        string keytool = "keytool";
        string javac = "javac";
        string cmake = "cmake";
        
        if (!File.Exists(androidJar))
            throw new ArgumentException($"API level={buildApiLevel} is not downloaded in Android SDK");

        // 1. Build libruntime-android.so` via cmake

        string monoRuntimeLib = Directory.GetFiles(appDir)
            .First(f => Path.GetFileName(f) == "libmonosgen-2.0.a");

        string cmakeLists = Utils.GetEmbeddedResource("CMakeLists-android.txt")
            .Replace("%MonoInclude%", monoRuntimeHeaders)
            .Replace("%NativeLibrariesToLink%", monoRuntimeLib);
        File.WriteAllText(Path.Combine(workplace, "CMakeLists.txt"), cmakeLists);

        string runtimeAndroidSrc = Utils.GetEmbeddedResource("runtime-android.c")
            .Replace("%EntryPointLibName%", Path.GetFileName(entryPointLib));
        File.WriteAllText(Path.Combine(workplace, "runtime-android.c"), runtimeAndroidSrc);
        
        Utils.RunProcess(cmake, workingDir: workplace,
            args: $"-DCMAKE_TOOLCHAIN_FILE={androidToolchain} -DANDROID_ABI={abi} -DANDROID_STL=none " + 
            "-DANDROID_NATIVE_API_LEVEL={minApiLevel} -B runtime-android");
        Utils.RunProcess("make", workingDir: Path.Combine(workplace, "runtime-android"));

        // 2. Compile Java files

        string javaSrcFolder = Path.Combine(workplace, "src", "net", "dot");
        Directory.CreateDirectory(javaSrcFolder);

        string packageId = $"net.dot.{appName}";

        File.WriteAllText(Path.Combine(javaSrcFolder, "MainActivity.java"), 
            Utils.GetEmbeddedResource("MainActivity.java"));
        File.WriteAllText(Path.Combine(javaSrcFolder, "MonoRunner.java"), 
            Utils.GetEmbeddedResource("MonoRunner.java"));
        File.WriteAllText(Path.Combine(workplace, "AndroidManifest.xml"), 
            Utils.GetEmbeddedResource("AndroidManifest.xml")
                .Replace("%PackageName%", packageId)
                .Replace("%MinSdkLevel%", minApiLevel));

        string javaCompilerArgs = $"-d obj -classpath src -bootclasspath {androidJar} -source 1.8 -target 1.8 ";
        Utils.RunProcess(javac, javaCompilerArgs + Path.Combine(javaSrcFolder, "MainActivity.java"), workingDir: workplace);
        Utils.RunProcess(javac, javaCompilerArgs + Path.Combine(javaSrcFolder, "MonoRunner.java"), workingDir: workplace);
        Utils.RunProcess(dx, "--dex --output=classes.dex obj", workingDir: workplace);

        // 3. Generate APK

        string apkFile = Path.Combine(workplace, "bin", appName + ".unaligned.apk");
        Utils.RunProcess(aapt, $"package -f -m -F {apkFile} -A assets -M AndroidManifest.xml -I {androidJar}", workingDir: workplace);
        
        var dynamicLibs = new List<string>();
        dynamicLibs.Add(Path.Combine(workplace, "runtime-android", "libruntime-android.so"));
        dynamicLibs.AddRange(Directory.GetFiles(appDir, "*.so"));

        // add all *.so files to lib/%abi%/
        Directory.CreateDirectory(Path.Combine(workplace, "lib", abi));
        foreach (var dynamicLib in dynamicLibs)
        {
            string destRelative = Path.Combine("lib", abi, Path.GetFileName(dynamicLib));
            File.Copy(dynamicLib, Path.Combine(workplace, destRelative), true);
            Utils.RunProcess(aapt, $"add {apkFile} {destRelative}", workingDir: workplace);
        }
        Utils.RunProcess(aapt, $"add {apkFile} classes.dex", workingDir: workplace);

        // 4. Generate key
        
        string signingKey = Path.Combine(workplace, "debug.keystore");
        if (!File.Exists(signingKey))
        {
            Utils.RunProcess(keytool, "-genkey -v -keystore debug.keystore -storepass android -alias " +
                "androiddebugkey -keypass android -keyalg RSA -keysize 2048 -noprompt -dname \"CN=Android Debug,O=Android,C=US\"", workingDir: workplace, silent: true);
        }

        // 5. Sign APK

        Utils.RunProcess(apksigner, $"sign --min-sdk-version {minApiLevel} --ks debug.keystore --ks-pass pass:android " +
            $"--key-pass pass:android {apkFile}", workingDir: workplace);

        return (apkFile, packageId);
    }
    
    /// <summary>
    /// Scan android SDK for build tools (ignore preview versions)
    /// </summary>
    private static string GetLatestBuildTools(string androidSdkDir)
    {
        string? buildTools = Directory.GetDirectories(Path.Combine(androidSdkDir, "build-tools"))
            .Select(Path.GetFileName)
            .Where(file => !file!.Contains("-"))
            .Select(file => Version.TryParse(Path.GetFileName(file), out Version? version) ? version : default)
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
