// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_135192
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetJit();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ProcessShutdownWork(IntPtr jit);

    private static int Compile(string name)
    {
        DynamicMethod method = new(name, typeof(int), Type.EmptyTypes);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<int>>()();
    }

    private static string FindDescriptor(string outputPath)
    {
        foreach (string path in Directory.GetFiles("/proc/self/fd"))
        {
            if (new FileInfo(path).LinkTarget == outputPath)
            {
                return path;
            }
        }

        Assert.Fail($"No open descriptor found for {outputPath}");
        return null;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        string outputPath = Path.GetFullPath(Environment.GetEnvironmentVariable("DOTNET_JitStdOutFile")!);
        string timingPath = Path.GetFullPath(Environment.GetEnvironmentVariable("DOTNET_JitTimeLogCsv")!);
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        IntPtr jitLibrary = NativeLibrary.Load(Path.Combine(runtimeDirectory, "libclrjit.so"));
        string jitInterfacePath = Assert.Single(Directory.GetFiles(runtimeDirectory, "libjitinterface_*.so"));
        IntPtr jitInterface = NativeLibrary.Load(jitInterfacePath);
        GetJit getJit = Marshal.GetDelegateForFunctionPointer<GetJit>(NativeLibrary.GetExport(jitLibrary, "getJit"));
        ProcessShutdownWork shutdown = Marshal.GetDelegateForFunctionPointer<ProcessShutdownWork>(
            NativeLibrary.GetExport(jitInterface, "JitProcessShutdownWork"));

        try
        {
            Assert.Equal(42, Compile("BeforeShutdown"));

            string descriptorPath = FindDescriptor(outputPath);
            string timingDescriptorPath = FindDescriptor(timingPath);
            IntPtr jit = getJit();
            Assert.NotEqual(IntPtr.Zero, jit);

            // Force shutdown reporting before the next compilation, without relying on a scheduling race.
            shutdown(jit);

            Assert.Equal(outputPath, new FileInfo(descriptorPath).LinkTarget);
            Assert.Equal(timingPath, new FileInfo(timingDescriptorPath).LinkTarget);
            Assert.Equal(42, Compile("AfterShutdown"));
            Assert.Contains("AfterShutdown", File.ReadAllText(outputPath));
            Assert.Contains("AfterShutdown", File.ReadAllText(timingPath));
        }
        finally
        {
            NativeLibrary.Free(jitInterface);
            NativeLibrary.Free(jitLibrary);
        }
    }
}
