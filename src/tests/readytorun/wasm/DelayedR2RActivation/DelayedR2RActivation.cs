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
using DelayedR2RActivationHelpers;
using Xunit;

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

    // A delayed method waits for the auxiliary module that holds its code (pending auxiliary module). The wait is
    // simulated by counting calls to the waiting methods.
    private static readonly int s_activationCallCount = ReadCallCount("DOTNET_R2RDelayedActivationCallCount", 0);
    private static readonly bool s_moduleDelayed = s_activationCallCount >= 1000;
    // Counts between 1 and 999 can run out while the test sets up, so only check states for 0 or at least 1000.
    private static readonly bool s_checkStates = s_activationCallCount == 0 || s_moduleDelayed;
    private static Action s_atRecursiveBottom;

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
            bool expectPending = s_moduleDelayed;
            for (int index = 0; index < entryPoints.Length; index++)
            {
                Assert.True(expectPending == PrefersInterpreter(entryPoints[index]), $"PrefersInterpreter, entryPoints[{index}]");
                Assert.True(expectPending == IsPendingAuxiliaryModule(entryPoints[index]), $"IsPendingAuxiliaryModule, entryPoints[{index}]");
            }
            Assert.Equal(expectPending, untouchedWasDelayed);
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
        if (s_checkStates && s_moduleDelayed)
        {
            // Both continuations must still be interpreted so they resume after their methods are published.
            Assert.True(IsDelayed(suspendedEntryPoint));
            Assert.True(IsDelayed(repeatedlySuspendedEntryPoint));
        }

        s_atRecursiveBottom = () =>
        {
            s_atRecursiveBottom = null;
            int invocation = 0;
            for (int moduleInvocation = 0;
                moduleInvocation < s_activationCallCount && IsPendingAuxiliaryModule(entryPoints[0]);
                moduleInvocation++, invocation++)
            {
                Assert.Equal(invocation * 7 + 13, DirectTarget(invocation));
            }

            if (s_checkStates && s_moduleDelayed)
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

        // Not called until the wait is over, so its first call finds and publishes its R2R code.
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
            // It has no R2R code, so there is nothing to wait for.
            Assert.False(IsDelayed(interpreterOnlyEntryPoint), "IsDelayed, interpreter only");
        }
    }

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

    // True while the method waits for its auxiliary module.
    [BypassReadyToRun]
    private static bool IsDelayed(IntPtr entryPoint) => IsPendingAuxiliaryModule(entryPoint);

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
