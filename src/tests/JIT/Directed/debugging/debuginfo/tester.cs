extern alias tests_d;
extern alias tests_r;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Tracing.Tests.Common;
using DebugInfoMethodsD = tests_d::DebugInfoMethods;
using DebugInfoMethodsR = tests_r::DebugInfoMethods;
using Xunit;

public unsafe class DebugInfoTest
{
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        var keywords =
            ClrTraceEventParser.Keywords.Jit | ClrTraceEventParser.Keywords.JittedMethodILToNativeMap;

        var dotnetRuntimeProvider = new List<EventPipeProvider>
        {
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime", eventLevel: EventLevel.Verbose, keywords: (long)keywords)
        };

        return
            IpcTraceTest.RunAndValidateEventCounts(
                new Dictionary<string, ExpectedEventCount>(),
                JitMethods,
                dotnetRuntimeProvider,
                1024,
                ValidateMappings);
    }

    private static void JitMethods()
    {
        ProcessType(typeof(DebugInfoMethodsD));
        ProcessType(typeof(DebugInfoMethodsR));
    }

    private static void ProcessType(Type t)
    {
        foreach (MethodInfo mi in t.GetMethods())
        {
            if (mi.GetCustomAttribute<ExpectedILMappings>() != null)
            {
                RuntimeHelpers.PrepareMethod(mi.MethodHandle);
            }
        }
    }

    private static Func<int> ValidateMappings(EventPipeEventSource source)
    {
        List<(long MethodID, OptimizationTier Tier, (int ILOffset, int NativeOffset)[] Mappings)> methodsWithMappings = new();
        Dictionary<long, OptimizationTier> methodTier = new();

        source.Clr.MethodLoad += e => methodTier[e.MethodID] = e.OptimizationTier;
        source.Clr.MethodLoadVerbose += e => methodTier[e.MethodID] = e.OptimizationTier;
        source.Clr.MethodILToNativeMap += e =>
        {
            if (e.MethodID == 0)
                return;

            var mappings = new (int, int)[e.CountOfMapEntries];
            for (int i = 0; i < mappings.Length; i++)
                mappings[i] = (e.ILOffset(i), e.NativeOffset(i));

            if (!methodTier.TryGetValue(e.MethodID, out OptimizationTier tier))
                tier = OptimizationTier.Unknown;

            methodsWithMappings.Add((e.MethodID, tier, mappings));
        };

        return () =>
        {
            int result = 100;
            foreach ((long methodID, OptimizationTier tier, (int ILOffset, int NativeOffset)[] mappings) in methodsWithMappings)
            {
                MethodBase meth = s_getMethodBaseByHandle(null, (IntPtr)(void*)methodID);
                ExpectedILMappings attrib = meth.GetCustomAttribute<ExpectedILMappings>();
                if (attrib == null)
                {
                    continue;
                }

                string name = $"[{meth.DeclaringType.Assembly.GetName().Name}]{meth.DeclaringType.FullName}.{meth.Name}";

                // If DebuggableAttribute is saying that the assembly must be debuggable, then verify debug mappings.
                // Otherwise verify release mappings.
                // This may seem a little strange since we do not use the tier at all -- however, we expect debug
                // to never tier and in release, we expect the release mappings to be the "least common denominator",
                // i.e. tier0 and tier1 mappings should both be a superset.
                // Note that tier0 and MinOptJitted differs in mappings generated exactly due to DebuggableAttribute.
                DebuggableAttribute debuggableAttrib = meth.DeclaringType.Assembly.GetCustomAttribute<DebuggableAttribute>();
                bool debuggableMappings = debuggableAttrib != null && debuggableAttrib.IsJITOptimizerDisabled;

                Console.WriteLine("{0}: Validate mappings for {1} codegen (tier: {2})", name, debuggableMappings ? "debuggable" : "optimized", tier);

                int[] expected = debuggableMappings ? attrib.Debug : attrib.Opts;
                if (expected == null)
                {
                    continue;
                }

                if (!ValidateSingle(expected, mappings))
                {
                    Console.WriteLine("  Validation failed: expected mappings at IL offsets {0}", string.Join(", ", expected.Select(il => $"{il:x3}")));
                    Console.WriteLine("  Actual (IL <-> native):");
                    foreach ((int ilOffset, int nativeOffset) in mappings)
                    {
                        string ilOffsetName = Enum.IsDefined((SpecialILOffset)ilOffset) ? ((SpecialILOffset)ilOffset).ToString() : $"{ilOffset:x3}";
                        Console.WriteLine("    {0:x3} <-> {1:x3}", ilOffsetName, nativeOffset);
                    }

                    result = -1;
                }
            }

            return result;
        };
    }

    // Validate that all IL offsets we expected had mappings generated for them.
    private static bool ValidateSingle(int[] expected, (int ILOffset, int NativeOffset)[] mappings)
    {
        return expected.All(il => mappings.Any(t => t.ILOffset == il));
    }

    private enum SpecialILOffset
    {
        NoMapping = -1,
        Prolog = -2,
        Epilog = -3,
    }

    static DebugInfoTest()
    {
        Type runtimeMethodHandleInternalType = typeof(RuntimeMethodHandle).Assembly.GetType("System.RuntimeMethodHandleInternal");
        Type runtimeTypeType = typeof(RuntimeMethodHandle).Assembly.GetType("System.RuntimeType");
        MethodInfo getMethodBaseMethod = runtimeTypeType.GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, new[] { runtimeTypeType, runtimeMethodHandleInternalType });
        s_getMethodBaseByHandle = (delegate*<object, IntPtr, MethodBase>)getMethodBaseMethod.MethodHandle .GetFunctionPointer();
    }

    // Needed to go from MethodID -> MethodBase
    private static readonly delegate*<object, IntPtr, MethodBase> s_getMethodBaseByHandle;
}
