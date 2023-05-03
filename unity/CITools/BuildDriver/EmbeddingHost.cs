// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using NiceIO;

namespace BuildDriver;

public class EmbeddingHost
{

    public static void Build(GlobalConfig gConfig)
    {
        DotNetManagedSolution(gConfig, test: false);
    }

    private static void DotNetManagedSolution(GlobalConfig gConfig, bool test, string[]? additionalArgs = null, (string Key, string Value)[]? envVars = null)
    {
        string type = test ? "test" : "build";
        Console.WriteLine("******************************");
        Console.WriteLine($"Unity: {type} embedding host");
        Console.WriteLine("******************************");

        var args = new List<string>
        {
            type,
            " unity/managed.sln",
            $"-c {gConfig.Configuration}",
            $"-v:{gConfig.VerbosityLevel}"
        };

        if (additionalArgs != null)
            args.AddRange(additionalArgs);

        ProcessStartInfo psi = new();
        psi.FileName = Paths.RepoRoot.Combine(Paths.BootstrapDotNetExecutableName);
        psi.Arguments = args.AggregateWithSpace();
        psi.WorkingDirectory = Paths.RepoRoot;

        if (envVars != null)
        {
            foreach (var pair in envVars)
            {
                psi.Environment.Add(pair.Key, pair.Value);
            }
        }


        Utils.RunProcess(psi, gConfig);
    }

    static void TestManagedUsingOurRuntime(GlobalConfig gConfig)
    {
        // dotnet test will crash if we attempt to run it with the null gc.
        // We need to defer setting DOTNET_GCName until our test process.
        // Using a runsettings file let's us set env vars for the test process itself
        //
        // There are still issues getting our gc to initialize on macOS & Linux
        var useUnityGc = CanRunWithUnityGc();
        var runSettingsFile = GenerateRunSettings(useUnityGc);

        try
        {
            DotNetManagedSolution(gConfig, test: false,
                additionalArgs: new[] { "-p:TestingUnityCoreClr=true" });

            const string name = "UnityEmbedHost.Tests";
            var testAssemblyPath = Paths.UnityRoot.Combine(name, "bin", gConfig.Configuration).Directories().Single()
                .Combine($"{name}.dll");

            var vsTestPath = Paths.RepoRoot.Combine(".dotnet/sdk").Directories().Single().Combine("vstest.console.dll");

            // We can't use `dotnet test` because of a combination of
            // 1) that requires an SDK and our build does not have one, which means we'd have to rely on a dotnet with an SDK.
            // 2) the dotnet.cmd/dotnet.sh scripts will set DOTNET_ROOT and DOTNET_ROOT_ARCH env vars overriding our ability to get dotnet to pick up our dotnet.
            // I was able to get `dotnet test` working on Windows.  However, on macOS and Linux I hit the above complications.  Using dotnet exec avoids some extra hoop
            // jumping where things can go wrong
            //
            // By using our built dotnet with 'exec' we can use our dotnet to run already built tests.
            var args = new List<string>
            {
                "exec",
                vsTestPath,
                $"/Settings:\"{runSettingsFile}\"",
                testAssemblyPath
            };

            ProcessStartInfo psi = new();

            // corerun works on mac/windows.  However, it does not work on Linux due to
            // https://github.com/microsoft/vstest/blob/2d656fe2133f89248825419fb8ffac5505486906/src/Microsoft.TestPlatform.CoreUtilities/Helpers/DotnetHostHelper.cs#L446
            // not handling Linux.
            // Running with 'dotnet' avoids this code path because vstest won't go searching for a dotnet
            psi.FileName = Utils.UnityTestHostDotNetRoot(gConfig).Combine(Paths.DotNetExecutableName);
            psi.Arguments = args.AggregateWithSpace();
            psi.WorkingDirectory = Paths.RepoRoot;

            // Needed so that corerun (or dotnet) can find the core libraries
            psi.Environment["CORE_LIBRARIES"] = Utils.UnityTestHostDotNetAppDirectory(gConfig);

            // Needed so that vstest can find our dotnet
            NPath dotNetRoot = Utils.UnityTestHostDotNetRoot(gConfig);
            psi.Environment["DOTNET_ROOT"] = $"{dotNetRoot}/";

            // Remove arch specific roots so that they don't take precedence
            if (psi.Environment.ContainsKey("DOTNET_ROOT_ARM64"))
                psi.Environment.Remove("DOTNET_ROOT_ARM64");
            if (psi.Environment.ContainsKey("DOTNET_ROOT_X64"))
                psi.Environment.Remove("DOTNET_ROOT_X64");

            Utils.RunProcess(psi, gConfig);
        }
        finally
        {
            try
            {
                // Avoid accumulating files in the temp directory
                File.Delete(runSettingsFile);
            }
            catch (Exception)
            {
                // Don't fail over trying to clean up the file
            }
        }
    }

    static bool CanRunEmbeddingTests(GlobalConfig gConfig)
    {
        // Tests fail on x86 currently, most likely due to not using the real GC yet.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            gConfig.Architecture.Equals("x86"))
            return false;

        return true;
    }

    static bool CanRunWithUnityGc()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return true;

        // For some reason that I haven't been able to figure out yet both macOS and Linux crash
        // which leads to vstest getting stuck for 90s waiting to hear from the test process
        return false;
    }

    static NPath GenerateRunSettings(bool useUnityGc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<RunSettings>");
        sb.AppendLine("    <RunConfiguration>");
        sb.AppendLine("        <EnvironmentVariables>");
        if (useUnityGc)
            sb.AppendLine($"            <DOTNET_GCName>{Paths.UnityGCFileName}</DOTNET_GCName>");
        sb.AppendLine("        </EnvironmentVariables>");
        sb.AppendLine("    </RunConfiguration>");
        sb.AppendLine("</RunSettings>");

        var tmpFile = NPath.SystemTemp.Combine($"embedding_tests_{Path.GetFileNameWithoutExtension(Path.GetTempFileName())}.runsettings");
        tmpFile.WriteAllText(sb.ToString());
        return tmpFile;
    }

    public static void TestManaged(GlobalConfig gConfig)
    {
        if (!CanRunEmbeddingTests(gConfig))
        {
            Console.WriteLine("******************************");
            Console.WriteLine("Skipping embedding API tests");
            Console.WriteLine("******************************");
            return;
        }

        // Make sure the embed host is up-to-date before running tests
        // Once the csproj can copy to to the testhost dotnet build we can remove this
        Artifacts.CopyUnityEmbedHostToArtifacts(gConfig);

        TestManagedUsingOurRuntime(gConfig);
    }

    public static void TestNative(GlobalConfig gConfig)
    {
        if (!CanRunEmbeddingTests(gConfig))
        {
            Console.WriteLine("******************************");
            Console.WriteLine("Skipping embedding API tests");
            Console.WriteLine("******************************");
            return;
        }

        string args = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $". -A {Utils.WinArchitecture(gConfig.Architecture)}"
            : $"-DCMAKE_BUILD_TYPE={gConfig.Configuration}";
        ProcessStartInfo psi = new();
        psi.FileName = "cmake";
        psi.Arguments = args;
        psi.WorkingDirectory = Paths.UnityEmbedApiTests;

        Utils.RunProcess(psi, gConfig);

        psi.Arguments = "--build .";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            psi.Arguments = $"--build . --config {gConfig.Configuration}";
        Utils.RunProcess(psi, gConfig);


        psi.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Paths.UnityEmbedApiTests.Combine(gConfig.Configuration, "mono_test_app.exe")
            : Paths.UnityEmbedApiTests.Combine("mono_test_app");
        psi.Arguments = string.Empty;
        Utils.RunProcess(psi, gConfig);
    }
}
