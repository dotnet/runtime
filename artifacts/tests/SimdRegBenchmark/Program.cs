using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CoreRun;

public class SimdRegConfig : ManualConfig
{
    public SimdRegConfig()
    {
        var coreRunPath = new FileInfo(
            @"C:\Users\reedz\runtime\artifacts\bin\coreclr\windows.x64.Release\corerun.exe");

        var baseline = Job.Default
            .WithToolchain(new CoreRunToolchain(
                coreRun: coreRunPath,
                createCopy: false,
                targetFrameworkMoniker: "net11.0",
                customDotNetCliPath: new FileInfo(@"C:\Users\reedz\runtime\.dotnet\dotnet.exe"),
                displayName: "ByRef"))
            .WithEnvironmentVariable("DOTNET_ReadyToRun", "0")
            .WithEnvironmentVariable("DOTNET_TieredCompilation", "0")
            .WithEnvironmentVariable("CORE_LIBRARIES",
                @"C:\Users\reedz\runtime\.dotnet\shared\Microsoft.NETCore.App\11.0.0-alpha.1.26064.118")
            .WithId("ByRef_Baseline")
            .AsBaseline();

        var optimized = Job.Default
            .WithToolchain(new CoreRunToolchain(
                coreRun: coreRunPath,
                createCopy: false,
                targetFrameworkMoniker: "net11.0",
                customDotNetCliPath: new FileInfo(@"C:\Users\reedz\runtime\.dotnet\dotnet.exe"),
                displayName: "Register"))
            .WithEnvironmentVariable("DOTNET_ReadyToRun", "0")
            .WithEnvironmentVariable("DOTNET_TieredCompilation", "0")
            .WithEnvironmentVariable("DOTNET_JitPassSimdInReg", "1")
            .WithEnvironmentVariable("CORE_LIBRARIES",
                @"C:\Users\reedz\runtime\.dotnet\shared\Microsoft.NETCore.App\11.0.0-alpha.1.26064.118")
            .WithId("Register_Optimized");

        AddJob(baseline);
        AddJob(optimized);
        AddColumn(StatisticColumn.Median);
        WithOption(ConfigOptions.DisableOptimizationsValidator, true);
    }
}

[Config(typeof(SimdRegConfig))]
[DisassemblyDiagnoser(maxDepth: 1)]
public class VectorBenchmarks
{
    private Vector128<float> v128a, v128b, v128c, v128d;
    private Vector256<float> v256a, v256b;

    [GlobalSetup]
    public void Setup()
    {
        v128a = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
        v128b = Vector128.Create(10.0f, 20.0f, 30.0f, 40.0f);
        v128c = Vector128.Create(100.0f, 200.0f, 300.0f, 400.0f);
        v128d = Vector128.Create(1000.0f, 2000.0f, 3000.0f, 4000.0f);
        v256a = Vector256.Create(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f);
        v256b = Vector256.Create(10.0f, 20.0f, 30.0f, 40.0f, 50.0f, 60.0f, 70.0f, 80.0f);
    }

    [Benchmark]
    public Vector128<float> SingleVec128() => Add128(v128a, v128b);

    [Benchmark]
    public Vector128<float> FourVec128() => Add128_4(v128a, v128b, v128c, v128d);

    [Benchmark]
    public Vector256<float> SingleVec256() => Add256(v256a, v256b);

    [Benchmark]
    public Vector128<float> MixedIntVec128() => MixedAdd(42, v128a, 99, v128b);

    [Benchmark]
    public Vector128<float> ChainedVec128()
    {
        var r = Add128(v128a, v128b);
        r = Add128(r, v128c);
        return Add128(r, v128d);
    }

    [Benchmark]
    public Vector128<float> Identity128() => ReturnIdentity128(v128a);

    [Benchmark]
    public Vector256<float> Identity256() => ReturnIdentity256(v256a);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Add128(Vector128<float> a, Vector128<float> b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Add128_4(
        Vector128<float> a, Vector128<float> b, Vector128<float> c, Vector128<float> d)
        => a + b + c + d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<float> Add256(Vector256<float> a, Vector256<float> b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> MixedAdd(int x, Vector128<float> a, int y, Vector128<float> b)
        => a + b + Vector128.Create((float)(x + y));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> ReturnIdentity128(Vector128<float> a) => a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<float> ReturnIdentity256(Vector256<float> a) => a;
}

public class Program
{
    public static void Main(string[] args) => BenchmarkRunner.Run<VectorBenchmarks>(args: args);
}
