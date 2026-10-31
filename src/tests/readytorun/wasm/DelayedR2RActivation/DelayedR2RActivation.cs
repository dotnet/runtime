// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
#if !PARTITIONED_WASM_R2R_BOUNDARY
using DelayedR2RActivationHelpers;
using Xunit;
#endif

#if PARTITIONED_WASM_R2R_BOUNDARY
public static class PartitionedR2RBoundary
{
#if PARTITIONED_WASM_R2R_ALL_HOT
    public static int Main()
    {
        const int PrefersInterpreterEntryPoint = 0x4;
        MethodInfo method = typeof(PartitionedR2RBoundary).GetMethod(nameof(Main));
        IntPtr entryPoint = method.MethodHandle.GetFunctionPointer();
        return (Marshal.ReadInt32(entryPoint, 3 * IntPtr.Size) & PrefersInterpreterEntryPoint) == 0 ? 100 : 101;
    }
#else
    private static int Main() => 100;
#endif
}
#else
namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}

public class DelayedR2RActivation
{
    // Mirrors the private layout of PortableEntryPoint in src/coreclr/vm/precode_portable.hpp: _flags follows
    // the _pActualCode, _pMD, and _pInterpreterData pointers. Keep these in sync with that file.
    private static readonly int PortableEntryPointFlagsOffset = 3 * IntPtr.Size;
    private const int PrefersInterpreterEntryPoint = 0x4;
    private const int PendingAuxiliaryModule = 0x10;
    private const int AwaitingAuxiliaryEntryPoints = 0x20;

    // A delayed method waits twice: first for the auxiliary EntryPoints module that says where its code is
    // (awaiting EntryPoints), then for the auxiliary module that holds the code (pending auxiliary module).
    // Each wait is simulated by counting calls to the waiting methods.
    private static readonly int s_activationCallCount = ReadCallCount("DOTNET_R2RDelayedActivationCallCount", 0);
    private static readonly int s_entryPointsCallCount =
        ReadCallCount("DOTNET_R2RDelayedEntryPointsActivationCallCount", s_activationCallCount);
    private static readonly bool s_entryPointsDelayed = s_entryPointsCallCount >= 1000;
    private static readonly bool s_moduleDelayed = s_activationCallCount >= 1000;
    // Counts between 1 and 999 can run out while the test sets up, so only check states for 0 or at least 1000.
    private static readonly bool s_checkStates =
        (s_entryPointsCallCount == 0 || s_entryPointsDelayed) && (s_activationCallCount == 0 || s_moduleDelayed);
    private static Action s_atRecursiveBottom;

#if PARTITIONED_WASM_R2R
    [Fact]
    [BypassReadyToRun]
    [RuntimeAsyncMethodGeneration(false)]
    public static async Task ActivatesDownloadedModulesAsync()
    {
        bool manualActivation = Environment.GetEnvironmentVariable("DOTNET_WasmAuxiliaryManualActivation") == "1";
        bool bodyFirst = Environment.GetEnvironmentVariable("DOTNET_WasmAuxiliaryBodyFirst") == "1";
        MethodInfo coldMethod = typeof(DelayedR2RActivation).GetMethod(
            nameof(PartitionedColdTarget), BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo genericMethod = typeof(DelayedR2RActivation).GetMethod(
            nameof(PartitionedGenericTarget), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeof(int));
        IntPtr coldEntryPoint = PrepareMethod(coldMethod);
        IntPtr genericEntryPoint = PrepareMethod(genericMethod);
        IntPtr virtualEntryPoint = PrepareMethod(
            typeof(PartitionedVirtualTarget<string>).GetMethod(nameof(PartitionedVirtualTarget<string>.Invoke)));
        IntPtr exceptionEntryPoint = PrepareMethod(typeof(DelayedR2RActivation).GetMethod(
            nameof(PartitionedExceptionTarget), BindingFlags.Static | BindingFlags.NonPublic));
        IntPtr suspendedEntryPoint = PrepareMethod(typeof(DelayedR2RActivation).GetMethod(
            nameof(PartitionedSuspendedTarget), BindingFlags.Static | BindingFlags.NonPublic));
        IntPtr crossModuleEntryPoint = PrepareMethod(
            typeof(List<CrossModuleValue>).GetMethod("AddWithResize", BindingFlags.Instance | BindingFlags.NonPublic));
        // Unmanaged function pointers are Wasm table indices, not PortableEntryPoint addresses.
        PrepareMethod(typeof(DelayedR2RActivation).GetMethod(nameof(PartitionedUnmanagedTarget)));
        Assert.Equal(42, CallPartitionedUnmanagedTarget(1));
        object marker = new();
        TaskCompletionSource<int> source = new();
        Task<object> suspended = PartitionedSuspendedTarget(source.Task, marker);
        Assert.False(suspended.IsCompleted, nameof(PartitionedSuspendedTarget));

        if (manualActivation)
        {
            Assert.True(PrefersInterpreter(coldEntryPoint));
            Assert.True(IsAwaitingAuxiliaryEntryPoints(coldEntryPoint));
            Assert.True(IsAwaitingAuxiliaryEntryPoints(genericEntryPoint));
            Assert.True(IsAwaitingAuxiliaryEntryPoints(virtualEntryPoint));
            Assert.True(IsAwaitingAuxiliaryEntryPoints(exceptionEntryPoint));
            Assert.True(IsAwaitingAuxiliaryEntryPoints(suspendedEntryPoint));
            Assert.True(IsAwaitingAuxiliaryEntryPoints(crossModuleEntryPoint));
            VerifyPartitionedCalls(2, marker);

            if (bodyFirst)
            {
                Console.WriteLine("AUXILIARY_R2R_ACTIVATE_MODULES");
                await Task.Delay(20);
                VerifyPartitionedCalls(3, marker);
                Assert.True(IsAwaitingAuxiliaryEntryPoints(coldEntryPoint));
                Assert.True(PrefersInterpreter(coldEntryPoint));
                Console.WriteLine("AUXILIARY_R2R_ACTIVATE_ENTRYPOINTS");
            }
            else
            {
                Console.WriteLine("AUXILIARY_R2R_ACTIVATE_ENTRYPOINTS");
                long deadline = Environment.TickCount64 + 30000;
                while (IsAwaitingAuxiliaryEntryPoints(coldEntryPoint) && Environment.TickCount64 < deadline)
                {
                    await Task.Delay(1);
                    Assert.Equal(34, PartitionedColdTarget(3));
                }
                Assert.True(IsPendingAuxiliaryModule(coldEntryPoint));
                Assert.True(PrefersInterpreter(coldEntryPoint));
                VerifyPartitionedCalls(3, marker);
                Console.WriteLine("AUXILIARY_R2R_ACTIVATE_MODULES");
            }
        }

        long activationDeadline = Environment.TickCount64 + 30000;
        while (PrefersInterpreter(coldEntryPoint) && Environment.TickCount64 < activationDeadline)
        {
            await Task.Delay(1);
            Assert.Equal(41, PartitionedColdTarget(4));
        }
        Assert.False(PrefersInterpreter(coldEntryPoint), nameof(PartitionedColdTarget));
        VerifyPartitionedCalls(5, marker);
        Assert.False(PrefersInterpreter(genericEntryPoint), nameof(PartitionedGenericTarget));
        Assert.False(PrefersInterpreter(virtualEntryPoint), nameof(PartitionedVirtualTarget<string>.Invoke));
        Assert.False(PrefersInterpreter(exceptionEntryPoint), nameof(PartitionedExceptionTarget));
        Assert.False(PrefersInterpreter(crossModuleEntryPoint), "AddWithResize");
        Assert.False(IsDelayed(coldEntryPoint));
        Assert.False(IsDelayed(genericEntryPoint));

        GC.Collect();
        source.SetResult(17);
        Assert.Same(marker, await suspended);
        Assert.Same(marker, await PartitionedSuspendedTarget(Task.FromResult(19), marker));
        Assert.False(PrefersInterpreter(suspendedEntryPoint), nameof(PartitionedSuspendedTarget));
    }

    [BypassReadyToRun]
    private static void VerifyPartitionedCalls(int value, object marker)
    {
        Func<int, int> callback = PartitionedColdTarget;
        Assert.Equal(value * 7 + 13, PartitionedColdTarget(value));
        Assert.Equal(value * 7 + 13, callback(value));
        Assert.Equal(value * 7 + 13, CallPartitionedFunctionPointer(value));
        MethodInfo method = typeof(DelayedR2RActivation).GetMethod(
            nameof(PartitionedColdTarget), BindingFlags.Static | BindingFlags.NonPublic);
        Assert.Equal(value * 7 + 13, (int)method.Invoke(null, new object[] { value }));
        Assert.Equal(value + 23, PartitionedGenericTarget<int>(value));
        Assert.Equal(value + 29, ((IVirtualTarget)new PartitionedVirtualTarget<string>()).Invoke(value));
        Assert.Same(marker, PartitionedExceptionTarget(marker));
        Assert.Equal(5, CrossModuleGenericCaller());
        Assert.Equal(value * 9 + 65, PartitionedCompiledCaller(value));
        Type[] primaryTypes = PartitionedPrimaryImports();
        Type[] coldTypes = PartitionedColdImports();
        Assert.Same(typeof(PartitionedBHot), primaryTypes[0]);
        Assert.Same(typeof(PartitionedDHot), primaryTypes[1]);
        Assert.Same(typeof(PartitionedShared), primaryTypes[2]);
        Assert.Same(typeof(PartitionedACold), coldTypes[0]);
        Assert.Same(typeof(PartitionedCCold), coldTypes[1]);
        Assert.Same(typeof(PartitionedECold), coldTypes[2]);
        Assert.Same(primaryTypes[2], coldTypes[3]);
        Assert.Same(typeof(PartitionedShared), PartitionedImportGeneric<PartitionedShared>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type[] PartitionedPrimaryImports() =>
        new[] { typeof(PartitionedBHot), typeof(PartitionedDHot), typeof(PartitionedShared) };

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static Type[] PartitionedColdImports() =>
        new[] { typeof(PartitionedACold), typeof(PartitionedCCold), typeof(PartitionedECold), typeof(PartitionedShared) };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type PartitionedImportGeneric<T>() => typeof(T);

    private sealed class PartitionedACold { }
    private sealed class PartitionedBHot { }
    private sealed class PartitionedCCold { }
    private sealed class PartitionedDHot { }
    private sealed class PartitionedECold { }
    private sealed class PartitionedShared { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int PartitionedColdTarget(int value) => value * 7 + 13;

    [BypassReadyToRun]
    private static unsafe int CallPartitionedFunctionPointer(int value)
    {
        delegate*<int, int> target = &PartitionedColdTarget;
        return target(value);
    }

    [BypassReadyToRun]
    private static unsafe int CallPartitionedUnmanagedTarget(int value)
    {
        delegate* unmanaged<int, int> target = &PartitionedUnmanagedTarget;
        return target(value);
    }

    [UnmanagedCallersOnly]
    public static int PartitionedUnmanagedTarget(int value) => value + 41;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int PartitionedGenericTarget<T>(int value)
    {
        GC.KeepAlive(typeof(T));
        return value + 23;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int PartitionedCompiledCaller(int value)
    {
        object marker = new();
        if (!ReferenceEquals(marker, PartitionedExceptionTarget(marker)))
            throw new InvalidOperationException();

        return PartitionedColdTarget(value) + PartitionedGenericTarget<int>(value) +
            ((IVirtualTarget)new PartitionedVirtualTarget<string>()).Invoke(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object PartitionedExceptionTarget(object value)
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch (InvalidOperationException)
        {
            GC.Collect();
            return value;
        }
        finally
        {
            GC.KeepAlive(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<object> PartitionedSuspendedTarget(Task<int> pending, object value)
    {
        await pending;
        GC.KeepAlive(value);
        return value;
    }

    private sealed class PartitionedVirtualTarget<T> : IVirtualTarget
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Invoke(int value)
        {
            GC.KeepAlive(typeof(T));
            return value + 29;
        }
    }
#else
    [Fact]
    [BypassReadyToRun]
    [RuntimeAsyncMethodGeneration(false)]
    public static async Task ActivatesCompositeImageAsync()
    {
        IntPtr suspendedEntryPoint = PrepareMethod(nameof(SuspendedTargetAsync));
        IntPtr repeatedlySuspendedEntryPoint = PrepareMethod(nameof(RepeatedlySuspendedTargetAsync));
        IntPtr[] entryPoints =
        [
            PrepareMethod(nameof(DirectTarget)),
            PrepareMethod(nameof(DelegateTarget)),
            PrepareMethod(nameof(ReflectionTarget)),
            PrepareMethod(typeof(DelayedR2RActivation).GetMethod(nameof(GenericTarget)).MakeGenericMethod(typeof(int))),
            PrepareMethod(nameof(GenericCaller)),
            PrepareMethod(nameof(RecursiveTarget)),
            PrepareMethod(nameof(ExceptionTarget)),
            PrepareMethod(nameof(CompiledCaller)),
            PrepareMethod(nameof(NativeToInterpreterTarget)),
            PrepareMethod(typeof(VirtualTarget).GetMethod(nameof(VirtualTarget.Invoke))),
            PrepareMethod(typeof(CovariantTarget).GetMethod(nameof(CovariantTarget.Invoke))),
            suspendedEntryPoint,
            repeatedlySuspendedEntryPoint,
            PrepareMethod(nameof(CrossModuleGenericCaller)),
            PrepareMethod(nameof(CalliTarget)),
            PrepareMethod(typeof(List<CrossModuleValue>).GetMethod("AddWithResize", BindingFlags.Instance | BindingFlags.NonPublic)),
            PrepareMethod(typeof(AliasTarget).GetMethod(nameof(AliasTarget.Implementation)))
        ];
        IntPtr untouchedEntryPoint = PrepareMethod(nameof(UntouchedTarget));
        IntPtr cctorEntryPoint = PrepareMethod(typeof(CctorTarget).TypeInitializer);
        IntPtr interpreterOnlyEntryPoint = PrepareMethod(nameof(InterpreterOnlyTarget));
        bool untouchedWasDelayed = IsDelayed(untouchedEntryPoint);

        if (s_checkStates)
        {
            // Methods wait for their EntryPoints first. With EntryPoints available, they wait for their module.
            bool expectAwaiting = s_entryPointsDelayed;
            bool expectPending = !s_entryPointsDelayed && s_moduleDelayed;
            for (int index = 0; index < entryPoints.Length; index++)
            {
                Assert.True((expectAwaiting || expectPending) == PrefersInterpreter(entryPoints[index]), $"PrefersInterpreter, entryPoints[{index}]");
                Assert.True(expectAwaiting == IsAwaitingAuxiliaryEntryPoints(entryPoints[index]), $"IsAwaitingAuxiliaryEntryPoints, entryPoints[{index}]");
                Assert.True(expectPending == IsPendingAuxiliaryModule(entryPoints[index]), $"IsPendingAuxiliaryModule, entryPoints[{index}]");
            }
            Assert.Equal(expectAwaiting || expectPending, untouchedWasDelayed);
            Assert.True(expectAwaiting == IsAwaitingAuxiliaryEntryPoints(cctorEntryPoint), "IsAwaitingAuxiliaryEntryPoints, class constructor");
            Assert.True(expectPending == IsPendingAuxiliaryModule(cctorEntryPoint), "IsPendingAuxiliaryModule, class constructor");
        }

        VerifyCalls(1, callCompiledTarget: false);

        object value = new();
        TaskCompletionSource<int> originalSource = new();
        Task<object> original = SuspendedTargetAsync(originalSource.Task, value);
        Assert.False(original.IsCompleted);

        int suspensionCount = 3;
        TaskCompletionSource<int>[] sources = new TaskCompletionSource<int>[suspensionCount];
        Task<int>[] pending = new Task<int>[suspensionCount];
        for (int index = 0; index < suspensionCount; index++)
        {
            sources[index] = new();
            pending[index] = sources[index].Task;
        }

        long[] counts = new long[3];
        Task<object> suspended = RepeatedlySuspendedTargetAsync(pending, value, counts);
        if (s_checkStates && (s_entryPointsDelayed || s_moduleDelayed))
        {
            // Both continuations must still be interpreted so they resume after their methods are published.
            Assert.True(IsDelayed(suspendedEntryPoint));
            Assert.True(IsDelayed(repeatedlySuspendedEntryPoint));
        }

        s_atRecursiveBottom = () =>
        {
            s_atRecursiveBottom = null;
            int invocation = 0;
            for (int entryPointsInvocation = 0;
                entryPointsInvocation < s_entryPointsCallCount && IsAwaitingAuxiliaryEntryPoints(entryPoints[0]);
                entryPointsInvocation++, invocation++)
            {
                Assert.Equal(invocation * 7 + 13, DirectTarget(invocation));
            }

            if (s_checkStates && s_entryPointsDelayed)
            {
                // The EntryPoints were activated, and the retried lookup found DirectTarget in its auxiliary module.
                Assert.False(IsAwaitingAuxiliaryEntryPoints(entryPoints[0]), "IsAwaitingAuxiliaryEntryPoints, DirectTarget after EntryPoints activation");
                Assert.Equal(s_moduleDelayed, IsPendingAuxiliaryModule(entryPoints[0]));
                Assert.Equal(s_moduleDelayed, PrefersInterpreter(entryPoints[0]));
            }

            for (int moduleInvocation = 0;
                moduleInvocation < s_activationCallCount && IsPendingAuxiliaryModule(entryPoints[0]);
                moduleInvocation++, invocation++)
            {
                Assert.Equal(invocation * 7 + 13, DirectTarget(invocation));
            }

            if (s_checkStates && (s_entryPointsDelayed || s_moduleDelayed))
            {
                Assert.False(IsDelayed(entryPoints[0]), "IsDelayed, DirectTarget after activation");
                Assert.False(PrefersInterpreter(entryPoints[0]), "PrefersInterpreter, DirectTarget after activation");
                Assert.True(IsDelayed(suspendedEntryPoint));
                Assert.True(IsDelayed(repeatedlySuspendedEntryPoint));
            }

            for (int index = 0; index < suspensionCount; index++)
            {
                Assert.False(suspended.IsCompleted);
                sources[index].SetResult(index + 1);
                Assert.Equal(((long)index + 1) * (index + 2) / 2, counts[1]);
                if (index == 0 || index == suspensionCount - 1)
                {
                    GC.Collect();
                }
            }

            Assert.Equal(0, RecursiveTarget(0));
            Task<object> completed = SuspendedTargetAsync(Task.FromResult(37), value);
            Assert.True(completed.IsCompletedSuccessfully);
            Assert.Same(value, completed.GetAwaiter().GetResult());
        };
        Assert.Equal(10, RecursiveTarget(4));
        Assert.Null(s_atRecursiveBottom);

        Assert.Same(value, await suspended);
        Assert.Equal(1L, counts[0]);
        Assert.Equal(1L, counts[2]);
        Assert.Same(value, await RepeatedlySuspendedTargetAsync(Array.Empty<Task<int>>(), value, new long[3]));

        originalSource.SetResult(43);
        Assert.Same(value, await original);

        // Not called until both waits are over, so its first call finds and publishes its R2R code.
        Assert.Equal(untouchedWasDelayed, IsDelayed(untouchedEntryPoint));
        Assert.Equal(untouchedWasDelayed, PrefersInterpreter(untouchedEntryPoint));
        Assert.Equal(36, UntouchedTarget(5));
        Assert.False(IsDelayed(untouchedEntryPoint), "IsDelayed, untouched");
        Assert.False(PrefersInterpreter(untouchedEntryPoint));

        // The class constructor is first run by the VM after activation, through a function-pointer call.
        Assert.Equal(59, CctorTarget.Value);
        Assert.False(IsDelayed(cctorEntryPoint), "IsDelayed, class constructor");
        Assert.False(PrefersInterpreter(cctorEntryPoint));

        IntPtr lateEntryPoint = PrepareMethod(nameof(LatePreparedTarget));
        Assert.False(IsDelayed(lateEntryPoint), "IsDelayed, late prepared");
        Assert.False(PrefersInterpreter(lateEntryPoint));
        Assert.Equal(46, LatePreparedTarget(5));

        VerifyCalls(7, callCompiledTarget: true);
        foreach (IntPtr entryPoint in entryPoints)
        {
            Assert.False(IsDelayed(entryPoint), "IsDelayed, after activation");
            Assert.False(PrefersInterpreter(entryPoint));
        }

        for (int invocation = 0; invocation < 3; invocation++)
        {
            Assert.Equal(invocation + 11, InterpreterOnlyTarget(invocation));
            // Implementation has already activated, so these calls only take the alias's redirect.
            Assert.Equal(invocation + 53, AliasCaller.CallDeclared(new AliasTarget(), invocation));
            Assert.True(PrefersInterpreter(interpreterOnlyEntryPoint));
            // A later image may still have code for it, so a miss keeps waiting for the next EntryPoints era.
            Assert.True(IsAwaitingAuxiliaryEntryPoints(interpreterOnlyEntryPoint), "IsAwaitingAuxiliaryEntryPoints, interpreter only");
        }
    }

#endif

    [BypassReadyToRun]
    private static IntPtr PrepareMethod(string methodName) =>
        PrepareMethod(typeof(DelayedR2RActivation).GetMethod(methodName));

    [BypassReadyToRun]
    private static IntPtr PrepareMethod(MethodBase method)
    {
        RuntimeHelpers.PrepareMethod(method.MethodHandle);
        return method.MethodHandle.GetFunctionPointer();
    }

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int CallThroughFunctionPointer(int value)
    {
        delegate*<int, int> calliTarget = &CalliTarget;
        return calliTarget(value);
    }

    [BypassReadyToRun]
    private static void VerifyCalls(int value, bool callCompiledTarget)
    {
        Func<int, int> callback = DelegateTarget;
        Assert.Equal(value * 7 + 13, DirectTarget(value));
        Assert.Equal(value * 7 + 13, callback(value));
        Assert.Equal(value * 7 + 13,
            (int)typeof(DelayedR2RActivation).GetMethod(nameof(ReflectionTarget)).Invoke(null, new object[] { value }));
        Assert.Equal(value * 7 + 13, GenericTarget<int>(value));
        // The compiled caller roots both generic bodies; interpreter-only callers do not.
        Assert.Equal(2 * (value * 7 + 13), GenericCaller(value));
        Assert.Equal(5, CrossModuleGenericCaller());
        object marker = new();
        Assert.Same(marker, ExceptionTarget(marker));
        Assert.Equal(callCompiledTarget ? value + 17 : value, CompiledCaller(callCompiledTarget, value));
        VirtualBase virtualTarget = new VirtualTarget();
        Assert.Equal(value + 23, virtualTarget.Invoke(value));
        Assert.Equal(value + 23, ((IVirtualTarget)virtualTarget).Invoke(value));
        CovariantBase covariantTarget = new CovariantTarget();
        Assert.Equal("activated", covariantTarget.Invoke());
        Assert.Equal(value + 19, CallThroughFunctionPointer(value));
        IVirtualTarget structTarget = new StructTarget();
        Assert.Equal(value + 29, structTarget.Invoke(value));
        // Calls Implementation through the interpreter's alias on Declared. After activation, the alias
        // must send the call to Implementation's R2R code.
        Assert.Equal(value + 53, AliasCaller.CallDeclared(new AliasTarget(), value));
    }

    [BypassReadyToRun]
    private static int ReadCallCount(string variable, int defaultValue) =>
        Environment.GetEnvironmentVariable(variable) is string value
            ? int.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : defaultValue;

    [BypassReadyToRun]
    private static bool IsPendingAuxiliaryModule(IntPtr entryPoint)
    {
        return (Marshal.ReadInt32(entryPoint, PortableEntryPointFlagsOffset) & PendingAuxiliaryModule) != 0;
    }

    [BypassReadyToRun]
    private static bool IsAwaitingAuxiliaryEntryPoints(IntPtr entryPoint)
    {
        return (Marshal.ReadInt32(entryPoint, PortableEntryPointFlagsOffset) & AwaitingAuxiliaryEntryPoints) != 0;
    }

    // True while the method waits for either its EntryPoints or its auxiliary module.
    [BypassReadyToRun]
    private static bool IsDelayed(IntPtr entryPoint)
    {
        return (Marshal.ReadInt32(entryPoint, PortableEntryPointFlagsOffset) & (PendingAuxiliaryModule | AwaitingAuxiliaryEntryPoints)) != 0;
    }

    [BypassReadyToRun]
    private static bool PrefersInterpreter(IntPtr entryPoint)
    {
        return (Marshal.ReadInt32(entryPoint, PortableEntryPointFlagsOffset) & PrefersInterpreterEntryPoint) != 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int DirectTarget(int value) => value * 7 + 13;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int DelegateTarget(int value) => value * 7 + 13;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ReflectionTarget(int value) => value * 7 + 13;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GenericTarget<T>(int value) => value * 7 + 13;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GenericCaller(int value) => GenericTarget<int>(value) + GenericTarget<string>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int RecursiveTarget(int depth)
    {
        if (depth == 0)
        {
            s_atRecursiveBottom?.Invoke();
            return 0;
        }

        return depth + RecursiveTarget(depth - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CompiledCaller(bool callTarget, int value) => callTarget ? NativeToInterpreterTarget(value) : value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int NativeToInterpreterTarget(int value) => value + 17;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int UntouchedTarget(int value) => value + 31;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CalliTarget(int value) => value + 19;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int LatePreparedTarget(int value) => value + 41;

    public struct CrossModuleValue
    {
        public int Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CrossModuleGenericCaller()
    {
        List<CrossModuleValue> values = new();
        values.Add(new CrossModuleValue { Value = 5 });
        return values[0].Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<object> SuspendedTargetAsync(Task<int> pending, object value)
    {
        await pending;
        GC.KeepAlive(value);
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<object> RepeatedlySuspendedTargetAsync(Task<int>[] pending, object value, long[] counts)
    {
        counts[0]++;
        try
        {
            for (int index = 0; index < pending.Length; index++)
            {
                counts[1] += await pending[index];
            }

            return value;
        }
        finally
        {
            counts[2]++;
            GC.KeepAlive(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object ExceptionTarget(object value)
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch (InvalidOperationException)
        {
            return value;
        }
        finally
        {
            GC.KeepAlive(value);
        }
    }

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int InterpreterOnlyTarget(int value) => value + 11;

    public interface IVirtualTarget
    {
        int Invoke(int value);
    }

    public class VirtualBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int Invoke(int value) => -1;
    }

    public sealed class VirtualTarget : VirtualBase, IVirtualTarget
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int Invoke(int value) => value + 23;
    }

    public class CovariantBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual object Invoke() => null;
    }

    public sealed class CovariantTarget : CovariantBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override string Invoke() => "activated";
    }

    public struct StructTarget : IVirtualTarget
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Invoke(int value) => value + 29;
    }

    public static class CctorTarget
    {
        private static readonly int s_value;

        static CctorTarget()
        {
            s_value = 59;
        }

        public static int Value
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => s_value;
        }
    }
}
#endif
