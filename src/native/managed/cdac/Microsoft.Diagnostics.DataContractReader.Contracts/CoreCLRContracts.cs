// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Registers all CoreCLR contract implementations.
/// External packages (NativeAOT, Mono, etc.) follow the same pattern
/// with their own static Register method.
/// </summary>
public static class CoreCLRContracts
{
    public static void Register(ContractRegistry registry)
    {
        registry.Register<IException>(1, static t => new Exception_1(t));
        registry.Register<ILoader>(1, static t => new Loader_1(t));
        registry.Register<IEcmaMetadata>(1, static t => new EcmaMetadata_1(t));
        registry.Register<IDacStreams>(1, static t => new DacStreams_1(t));
        registry.Register<ICodeVersions>(1, static t => new CodeVersions_1(t));
        registry.Register<IStackWalk>(1, static t => new StackWalk_1(t));
        registry.Register<IRuntimeInfo>(1, static t => new RuntimeInfo_1(t));
        registry.Register<IComWrappers>(1, static t => new ComWrappers_1(t));
        registry.Register<ISHash>(1, static t => new SHash_1(t));
        registry.Register<INotifications>(1, static t => new Notifications_1(t));
        registry.Register<ISignatureDecoder>(1, static t => new SignatureDecoder_1(t));
        registry.Register<IBuiltInCOM>(1, static t => new BuiltInCOM_1(t));
        registry.Register<IConditionalWeakTable>(1, static t => new ConditionalWeakTable_1(t));
        registry.Register<IAuxiliarySymbols>(1, static t => new AuxiliarySymbols_1(t));
        registry.Register<IDebugger>(1, static t => new Debugger_1(t));

        registry.Register<IDebugInfo>(1, static t => new DebugInfo_1(t));
        registry.Register<IDebugInfo>(2, static t => new DebugInfo_2(t));
        registry.Register<IStressLog>(1, static t => new StressLog_1(t));
        registry.Register<IStressLog>(2, static t => new StressLog_2(t));

        registry.Register<IThread>(1, static t => new Thread_1(t));

        registry.Register<IRuntimeTypeSystem>(1, static t => new RuntimeTypeSystem_1(t));

        registry.Register<IObject>(1, static t => new Object_1(t));

        registry.Register<IPlatformMetadata>(1, static t => new PlatformMetadata_1(t));

        registry.Register<IPrecodeStubs>(1, static t => new PrecodeStubs_1(t));
        registry.Register<IPrecodeStubs>(2, static t => new PrecodeStubs_2(t));
        registry.Register<IPrecodeStubs>(3, static t => new PrecodeStubs_3(t));

        registry.Register<IReJIT>(1, static t => new ReJIT_1(t));

        registry.Register<IGC>(1, static t => new GC_1(t));

        registry.Register<IGCInfo>(1, static t =>
        {
            RuntimeInfoArchitecture arch = t.Contracts.RuntimeInfo.GetTargetArchitecture();
            return arch switch
            {
                RuntimeInfoArchitecture.X64 => new GCInfo_1<AMD64GCInfoTraits>(t),
                RuntimeInfoArchitecture.Arm64 => new GCInfo_1<ARM64GCInfoTraits>(t),
                RuntimeInfoArchitecture.Arm => new GCInfo_1<ARMGCInfoTraits>(t),
                RuntimeInfoArchitecture.LoongArch64 => new GCInfo_1<LoongArch64GCInfoTraits>(t),
                RuntimeInfoArchitecture.RiscV64 => new GCInfo_1<RISCV64GCInfoTraits>(t),
                _ => default(GCInfo),
            };
        });

        registry.Register<ISyncBlock>(1, static t => new SyncBlock_1(t));

        registry.Register<IExecutionManager>(1, static t => new ExecutionManager_1(t));
        registry.Register<IExecutionManager>(2, static t => new ExecutionManager_2(t));
    }
}
