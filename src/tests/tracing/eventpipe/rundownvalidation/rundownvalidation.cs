// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using TestLibrary;
using Tracing.Tests.Common;
using Xunit;

namespace Tracing.Tests.RundownValidation
{
    [EventSource(Name = "RundownValidationEventSource")]
    internal sealed class RundownValidationEventSource : EventSource
    {
        public static readonly RundownValidationEventSource Log = new RundownValidationEventSource();

        private RundownValidationEventSource()
        {
        }

        [Event(3)]
        public void JumpStubCollectionStart()
        {
            WriteEvent(3);
        }

        [Event(4)]
        public void JumpStubCollectionStop()
        {
            WriteEvent(4);
        }
    }

    public class RundownValidation
    {
        private const string LcgJumpStubChildEnvironmentVariable = "RundownValidation_LcgJumpStubChild";

        [ActiveIssue("https://github.com/dotnet/runtime/issues/83051: not supported in net8", typeof(Utilities), nameof(Utilities.IsNativeAot))]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [SkipOnCoreClr("This test is sensitive to JIT optimizations.", RuntimeTestModes.AnyJitOptimizationStress)]
        [SkipOnCoreClr("Tracing tests routinely time out with JIT stress and GC stress.", RuntimeTestModes.AnyGCStress)]
        [Fact]
        public static int TestEntryPoint()
        {
            if (Environment.GetEnvironmentVariable(LcgJumpStubChildEnvironmentVariable) == "1")
            {
                GenerateLcgJumpStubActivity();

                RundownValidationEventSource.Log.JumpStubCollectionStart();
                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                finally
                {
                    RundownValidationEventSource.Log.JumpStubCollectionStop();
                }

                return 100;
            }

            // This test validates that the rundown events are present
            // and that the rundown contains the necessary events to get
            // symbols in a nettrace file.

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose),
                new EventPipeProvider(
                    "Microsoft-Windows-DotNETRuntime",
                    EventLevel.Verbose,
                    (long)ClrTraceEventParser.Keywords.Jit)
            };

            int result = IpcTraceTest.RunAndValidateEventCounts(
                _expectedEventCounts,
                _eventGeneratingAction,
                providers,
                1024,
                _DoesRundownContainMethodEvents);

            if (result != 100 ||
                !PlatformDetection.IsCoreCLR ||
                !RuntimeFeature.IsDynamicCodeCompiled ||
                !OperatingSystem.IsWindows() ||
                !PlatformDetection.Is64BitProcess ||
                (!TestLibrary.CoreClrConfigurationDetection.IsDebugRuntime &&
                    !TestLibrary.CoreClrConfigurationDetection.IsCheckedRuntime))
            {
                return result;
            }

            return ValidateLcgJumpStubUnloadEvents();
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 }
        };

        private static Action _eventGeneratingAction = GenerateVirtualStubDispatchActivity;

        private static Func<EventPipeEventSource, Func<int>> _DoesRundownContainMethodEvents = (source) =>
        {
            bool hasRuntimeStart = false;
            bool hasMethodDCStopInit = false;
            bool hasMethodDCStopComplete = false;
            bool hasLoaderModuleDCStop = false;
            bool hasLoaderDomainModuleDCStop = false;
            bool hasAssemblyModuleDCStop = false;
            bool hasMethodDCStopVerbose = false;
            bool hasMethodILToNativeMap = false;
            bool hasAppDomainDCStop = false;
            var liveStubBlocks = new List<HelperEvent>();
            var rundownStubBlocks = new List<HelperEvent>();
            var rundownWriteBarriers = new List<HelperEvent>();

            ClrTraceEventParser runtimeParser = new ClrTraceEventParser(source);
            runtimeParser.MethodLoadVerbose += (eventData) =>
                AddHelperEvent(eventData, liveStubBlocks, IsStubBlockName);

            ClrRundownTraceEventParser rundownParser = new ClrRundownTraceEventParser(source);
            rundownParser.RuntimeStart += (eventData) => hasRuntimeStart = true;
            rundownParser.MethodDCStopInit += (eventData) => hasMethodDCStopInit = true;
            rundownParser.MethodDCStopComplete += (eventData) => hasMethodDCStopComplete = true;
            rundownParser.LoaderModuleDCStop += (eventData) => hasLoaderModuleDCStop = true;
            rundownParser.LoaderDomainModuleDCStop += (eventData) => hasLoaderDomainModuleDCStop = true;
            rundownParser.LoaderAssemblyDCStop += (eventData) => hasAssemblyModuleDCStop = true;
            rundownParser.MethodDCStopVerbose += (eventData) =>
            {
                hasMethodDCStopVerbose = true;
                AddHelperEvent(eventData, rundownStubBlocks, IsStubBlockName);
                AddHelperEvent(eventData, rundownWriteBarriers, IsWriteBarrierName);
            };
            rundownParser.MethodILToNativeMapDCStop += (eventData) => hasMethodILToNativeMap = true;
            rundownParser.LoaderAppDomainDCStop += (eventData) => hasAppDomainDCStop = true;
            return () =>
            {
                Logger.logger.Log("hasRuntimeStart: " + hasRuntimeStart);
                Logger.logger.Log("hasMethodDCStopInit: " + hasMethodDCStopInit);
                Logger.logger.Log("hasMethodDCStopComplete: " + hasMethodDCStopComplete);
                Logger.logger.Log("hasLoaderModuleDCStop: " + hasLoaderModuleDCStop);
                Logger.logger.Log("hasLoaderDomainModuleDCStop: " + hasLoaderDomainModuleDCStop);
                Logger.logger.Log("hasAssemblyModuleDCStop: " + hasAssemblyModuleDCStop);
                Logger.logger.Log("hasMethodDCStopVerbose: " + hasMethodDCStopVerbose);
                Logger.logger.Log("hasMethodILToNativeMap: " + hasMethodILToNativeMap);
                Logger.logger.Log("hasAppDomainDCStop: " + hasAppDomainDCStop);
                Logger.logger.Log("liveStubBlocks: " + liveStubBlocks.Count);
                Logger.logger.Log("rundownStubBlocks: " + rundownStubBlocks.Count);
                Logger.logger.Log("rundownWriteBarriers: " + rundownWriteBarriers.Count);
                bool hasValidCoreClrHelpers =
                    !PlatformDetection.IsCoreCLR ||
                    !RuntimeFeature.IsDynamicCodeCompiled ||
                    (ValidateHelperEvents(liveStubBlocks) &&
                        ValidateHelperEvents(rundownStubBlocks) &&
                        ValidateHelperEvents(rundownWriteBarriers) &&
                        HaveMatchingStubBlocks(liveStubBlocks, rundownStubBlocks));
                return hasRuntimeStart && hasMethodDCStopInit && hasMethodDCStopComplete &&
                hasLoaderModuleDCStop && hasLoaderDomainModuleDCStop && hasAssemblyModuleDCStop &&
                hasMethodDCStopVerbose && hasMethodILToNativeMap && hasAppDomainDCStop &&
                hasValidCoreClrHelpers ? 100 : -1;
            };
        };

        private static void AddHelperEvent(
            MethodLoadUnloadVerboseTraceData eventData,
            List<HelperEvent> helperEvents,
            Func<string, bool> isExpectedName)
        {
            const int JitHelperMethod = 0x10;

            if (((int)eventData.MethodFlags & JitHelperMethod) == 0 ||
                !isExpectedName(eventData.MethodName))
            {
                return;
            }

            helperEvents.Add(new HelperEvent(
                eventData.MethodID,
                eventData.ModuleID,
                eventData.MethodStartAddress,
                eventData.MethodSize,
                eventData.MethodToken,
                eventData.MethodName,
                eventData.TimeStampRelativeMSec));
        }

        private static List<HelperEvent> GetEventsInRange(
            List<HelperEvent> helperEvents,
            double startTime,
            double stopTime)
        {
            return helperEvents
                .Where(e => e.TimeStampRelativeMSec >= startTime && e.TimeStampRelativeMSec <= stopTime)
                .ToList();
        }

        private static bool ValidateHelperEvents(List<HelperEvent> helperEvents)
        {
            if (helperEvents.Count == 0)
            {
                return false;
            }

            HelperEvent[] orderedEvents = helperEvents.OrderBy(e => e.StartAddress).ToArray();
            for (int i = 0; i < orderedEvents.Length; i++)
            {
                HelperEvent helperEvent = orderedEvents[i];
                if (!ValidateHelperEvent(helperEvent))
                {
                    return false;
                }

                ulong endAddress = helperEvent.StartAddress + (uint)helperEvent.Size;
                if (i + 1 < orderedEvents.Length && endAddress > orderedEvents[i + 1].StartAddress)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateHelperEvent(HelperEvent helperEvent)
        {
            if (unchecked((ulong)helperEvent.MethodId) != helperEvent.StartAddress ||
                helperEvent.ModuleId != 0 ||
                helperEvent.Size == 0 ||
                helperEvent.MethodToken != 0)
            {
                return false;
            }

            return helperEvent.StartAddress + (uint)helperEvent.Size > helperEvent.StartAddress;
        }

        private static bool HaveMatchingStubBlocks(List<HelperEvent> liveStubBlocks, List<HelperEvent> rundownStubBlocks)
        {
            return liveStubBlocks.Any(live =>
                rundownStubBlocks.Any(rundown => DoesRundownStubContainLiveStub(live, rundown)));
        }

        private static bool DoesRundownStubContainLiveStub(HelperEvent live, HelperEvent rundown)
        {
            if (!ValidateHelperEvent(live) ||
                !ValidateHelperEvent(rundown) ||
                live.StartAddress != rundown.StartAddress ||
                live.Name != rundown.Name)
            {
                return false;
            }

            // Live events report the requested CodeFragmentHeap block size, while rundown bounds
            // the block by the next aligned allocation and can therefore include alignment padding.
            return rundown.StartAddress + (uint)rundown.Size >= live.StartAddress + (uint)live.Size;
        }

        private static bool HaveMatchingLoadForEveryUnload(
            List<HelperEvent> loadedStubBlocks,
            List<HelperEvent> unloadedStubBlocks)
        {
            if (unloadedStubBlocks.Count == 0)
            {
                return false;
            }

            var unmatchedLoads = new List<HelperEvent>(loadedStubBlocks);
            foreach (HelperEvent unload in unloadedStubBlocks.OrderBy(e => e.TimeStampRelativeMSec))
            {
                if (!ValidateHelperEvent(unload))
                {
                    return false;
                }

                int matchingLoadIndex = unmatchedLoads.FindLastIndex(load =>
                    load.TimeStampRelativeMSec <= unload.TimeStampRelativeMSec &&
                    ValidateHelperEvent(load) &&
                    AreMatchingStubBlocks(load, unload));
                if (matchingLoadIndex < 0)
                {
                    return false;
                }

                unmatchedLoads.RemoveAt(matchingLoadIndex);
            }

            return true;
        }

        private static bool AreMatchingStubBlocks(HelperEvent first, HelperEvent second)
        {
            return first.StartAddress == second.StartAddress &&
                first.Size == second.Size &&
                first.Name == second.Name;
        }

        private static bool IsStubBlockName(string name)
        {
            return name is "JumpStub" or
                "MethodCallThunk" or
                "VSD_DispatchStub" or
                "VSD_ResolveStub" or
                "VSD_LookupStub" or
                "VSD_VTableStub";
        }

        private static bool IsJumpStubBlockName(string name)
        {
            return name == "JumpStub";
        }

        private static bool IsWriteBarrierName(string name)
        {
            return name.StartsWith("WriteBarrier", StringComparison.Ordinal) ||
                name == "CheckedWriteBarrier";
        }

        private static void GenerateVirtualStubDispatchActivity()
        {
            if (!PlatformDetection.IsCoreCLR || !RuntimeFeature.IsDynamicCodeCompiled)
            {
                return;
            }

            const int InterfaceMethodCount = 128;

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("RundownValidation.StubActivity"),
                AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("StubActivity");
            TypeBuilder interfaceBuilder = moduleBuilder.DefineType(
                "IStubActivity",
                TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Public);
            MethodBuilder[] interfaceMethods = new MethodBuilder[InterfaceMethodCount];

            for (int i = 0; i < interfaceMethods.Length; i++)
            {
                interfaceMethods[i] = interfaceBuilder.DefineMethod(
                    "Method" + i,
                    MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                    typeof(int),
                    Type.EmptyTypes);
            }

            Type interfaceType = interfaceBuilder.CreateType();
            TypeBuilder implementationBuilder = moduleBuilder.DefineType(
                "StubActivity",
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);
            implementationBuilder.AddInterfaceImplementation(interfaceType);

            for (int i = 0; i < interfaceMethods.Length; i++)
            {
                MethodBuilder implementationMethod = implementationBuilder.DefineMethod(
                    interfaceMethods[i].Name,
                    MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
                    typeof(int),
                    Type.EmptyTypes);
                ILGenerator implementationIl = implementationMethod.GetILGenerator();
                implementationIl.Emit(OpCodes.Ldc_I4, i);
                implementationIl.Emit(OpCodes.Ret);
                implementationBuilder.DefineMethodOverride(
                    implementationMethod,
                    interfaceType.GetMethod(interfaceMethods[i].Name));
            }

            Type implementationType = implementationBuilder.CreateType();
            object instance = Activator.CreateInstance(implementationType);
            Type delegateType = typeof(Func<,>).MakeGenericType(interfaceType, typeof(int));

            for (int i = 0; i < interfaceMethods.Length; i++)
            {
                MethodInfo interfaceMethod = interfaceType.GetMethod(interfaceMethods[i].Name);
                DynamicMethod caller = new DynamicMethod(
                    "Call" + i,
                    typeof(int),
                    new[] { interfaceType },
                    typeof(RundownValidation).Module,
                    skipVisibility: true);
                ILGenerator callerIl = caller.GetILGenerator();
                callerIl.Emit(OpCodes.Ldarg_0);
                callerIl.Emit(OpCodes.Callvirt, interfaceMethod);
                callerIl.Emit(OpCodes.Ret);
                Delegate call = caller.CreateDelegate(delegateType);
                int result = (int)call.DynamicInvoke(instance);
                if (result != i)
                {
                    throw new InvalidOperationException("Interface dispatch returned an unexpected result.");
                }
            }
        }

        private static int ValidateLcgJumpStubUnloadEvents()
        {
            string outputPathPattern = Path.Combine(
                AppContext.BaseDirectory,
                $"rundownvalidation-lcg-{Stopwatch.GetTimestamp()}-{{pid}}.nettrace");

            using var process = new Process();
            process.StartInfo.FileName = Environment.ProcessPath;
            process.StartInfo.ArgumentList.Add(
                Path.Combine(
                    AppContext.BaseDirectory,
                    typeof(RundownValidation).Assembly.GetName().Name + ".dll"));
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Environment[LcgJumpStubChildEnvironmentVariable] = "1";
            process.StartInfo.Environment["DOTNET_EnableEventPipe"] = "1";
            process.StartInfo.Environment["DOTNET_EventPipeConfig"] =
                "Microsoft-Windows-DotNETRuntime:4c14fccbd:5,RundownValidationEventSource:0:5";
            process.StartInfo.Environment["DOTNET_EventPipeOutputPath"] = outputPathPattern;
            process.StartInfo.Environment["DOTNET_EventPipeRundown"] = "0";
            process.StartInfo.Environment["DOTNET_TieredCompilation"] = "0";
            process.StartInfo.Environment["COMPlus_ForceRelocs"] = "1";

            process.Start();
            string tracePath = outputPathPattern.Replace("{pid}", process.Id.ToString());
            try
            {
                if (!process.WaitForExit(5 * 60 * 1000))
                {
                    process.Kill();
                    process.WaitForExit();
                    Logger.logger.Log("LCG jump-stub child process timed out.");
                    return -1;
                }

                if (process.ExitCode != 100 || !File.Exists(tracePath))
                {
                    Logger.logger.Log($"LCG jump-stub child exited with {process.ExitCode}; trace exists: {File.Exists(tracePath)}.");
                    return -1;
                }

                var loadedJumpStubBlocks = new List<HelperEvent>();
                var unloadedJumpStubBlocks = new List<HelperEvent>();
                double collectionStart = double.NaN;
                double collectionStop = double.NaN;
                using (var source = new EventPipeEventSource(tracePath))
                {
                    source.Dynamic.All += (eventData) =>
                    {
                        if (eventData.ProviderName != "RundownValidationEventSource")
                        {
                            return;
                        }

                        if (eventData.EventName == "JumpStubCollection/Start")
                        {
                            collectionStart = eventData.TimeStampRelativeMSec;
                        }
                        else if (eventData.EventName == "JumpStubCollection/Stop")
                        {
                            collectionStop = eventData.TimeStampRelativeMSec;
                        }
                    };

                    var parser = new ClrTraceEventParser(source);
                    parser.MethodLoadVerbose += (eventData) =>
                        AddHelperEvent(eventData, loadedJumpStubBlocks, IsJumpStubBlockName);
                    parser.MethodUnloadVerbose += (eventData) =>
                        AddHelperEvent(eventData, unloadedJumpStubBlocks, IsJumpStubBlockName);
                    source.Process();
                }

                List<HelperEvent> reclaimedJumpStubBlocks =
                    GetEventsInRange(unloadedJumpStubBlocks, collectionStart, collectionStop);
                Logger.logger.Log("LCG jump-stub loads: " + loadedJumpStubBlocks.Count);
                Logger.logger.Log("LCG jump-stub unloads: " + reclaimedJumpStubBlocks.Count);
                return HaveMatchingLoadForEveryUnload(loadedJumpStubBlocks, reclaimedJumpStubBlocks)
                    ? 100
                    : -1;
            }
            finally
            {
                if (File.Exists(tracePath))
                {
                    File.Delete(tracePath);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void GenerateLcgJumpStubActivity()
        {
            DynamicMethod dynamicMethod = new DynamicMethod("JumpStubMethod", typeof(object), null);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Newobj, typeof(object).GetConstructor(Type.EmptyTypes));
            ilGenerator.Emit(OpCodes.Ret);
            var dynamicMethodDelegate = (Func<object>)dynamicMethod.CreateDelegate(typeof(Func<object>));
            dynamicMethodDelegate();
        }

        private readonly struct HelperEvent
        {
            public HelperEvent(
                long methodId,
                long moduleId,
                ulong startAddress,
                int size,
                int methodToken,
                string name,
                double timeStampRelativeMSec)
            {
                MethodId = methodId;
                ModuleId = moduleId;
                StartAddress = startAddress;
                Size = size;
                MethodToken = methodToken;
                Name = name;
                TimeStampRelativeMSec = timeStampRelativeMSec;
            }

            public long MethodId { get; }
            public long ModuleId { get; }
            public ulong StartAddress { get; }
            public int Size { get; }
            public int MethodToken { get; }
            public string Name { get; }
            public double TimeStampRelativeMSec { get; }
        }
    }
}
