// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Xunit;
using TestLibrary;

namespace Tracing.Tests.RundownValidation
{

    public class RundownValidation
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/83051: not supported in net8", typeof(Utilities), nameof(Utilities.IsNativeAot))]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [SkipOnCoreClr("This test is sensitive to JIT optimizations.", RuntimeTestModes.AnyJitOptimizationStress)]
        [SkipOnCoreClr("Tracing tests routinely time out with JIT stress and GC stress.", RuntimeTestModes.AnyGCStress)]
        [Fact]
        public static int TestEntryPoint()
        {
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

            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _DoesRundownContainMethodEvents);
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
                eventData.MethodName));
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
                if (unchecked((ulong)helperEvent.MethodId) != helperEvent.StartAddress ||
                    helperEvent.ModuleId != 0 ||
                    helperEvent.Size == 0 ||
                    helperEvent.MethodToken != 0)
                {
                    return false;
                }

                ulong endAddress = helperEvent.StartAddress + (uint)helperEvent.Size;
                if (endAddress <= helperEvent.StartAddress ||
                    (i + 1 < orderedEvents.Length && endAddress > orderedEvents[i + 1].StartAddress))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HaveMatchingStubBlocks(List<HelperEvent> liveStubBlocks, List<HelperEvent> rundownStubBlocks)
        {
            return liveStubBlocks.Any(live =>
                rundownStubBlocks.Any(rundown =>
                    rundown.StartAddress == live.StartAddress &&
                    rundown.Name == live.Name));
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

        private static bool IsWriteBarrierName(string name)
        {
            return name.StartsWith("WriteBarrier", StringComparison.Ordinal) ||
                name == "CheckedWriteBarrier";
        }

        private static void GenerateVirtualStubDispatchActivity()
        {
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

        private readonly struct HelperEvent
        {
            public HelperEvent(
                long methodId,
                long moduleId,
                ulong startAddress,
                int size,
                int methodToken,
                string name)
            {
                MethodId = methodId;
                ModuleId = moduleId;
                StartAddress = startAddress;
                Size = size;
                MethodToken = methodToken;
                Name = name;
            }

            public long MethodId { get; }
            public long ModuleId { get; }
            public ulong StartAddress { get; }
            public int Size { get; }
            public int MethodToken { get; }
            public string Name { get; }
        }
    }
}
