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
        registry.Register<IException>("c1", static t => new Exception_1(t));
        registry.Register<ILoader>("c1", static t => new Loader_1(t));
        registry.Register<IEcmaMetadata>("c1", static t => new EcmaMetadata_1(t));
        registry.Register<IDacStreams>("c1", static t => new DacStreams_1(t));
        registry.Register<ICodeVersions>("c1", static t => new CodeVersions_1(t));
        registry.Register<IStackWalk>("c1", static t => new StackWalk_1(t));
        registry.Register<IRuntimeInfo>("c1", static t => new RuntimeInfo_1(t));
        registry.Register<IComWrappers>("c1", static t => new ComWrappers_1(t));
        registry.Register<ISHash>("c1", static t => new SHash_1(t));
        registry.Register<INotifications>("c1", static t => new Notifications_1(t));
        registry.Register<ICodeNotifications>("c1", static t => new CodeNotifications_1(t));
        registry.Register<ISignatureDecoder>("c1", static t => new SignatureDecoder_1(t));
        registry.Register<IBuiltInCOM>("c1", static t => new BuiltInCOM_1(t));
        registry.Register<IConditionalWeakTable>("c1", static t => new ConditionalWeakTable_1(t));
        registry.Register<IAuxiliarySymbols>("c1", static t => new AuxiliarySymbols_1(t));
        registry.Register<IDebugger>("c1", static t => new Debugger_1(t));

        registry.Register<IDebugInfo>("c1", static t => new DebugInfo_1(t));
        registry.Register<IDebugInfo>("c2", static t => new DebugInfo_2(t));
        registry.Register<IStressLog>("c1", static t => new StressLog_1(t));
        registry.Register<IStressLog>("c2", static t => new StressLog_2(t));

        registry.Register<IThread>("c1", static t => new Thread_1(t));

        registry.Register<IRuntimeTypeSystem>("c1", static t => new RuntimeTypeSystem_1(t));

        registry.Register<IObject>("c1", static t => new Object_1(t));

        registry.Register<IPlatformMetadata>("c1", static t => new PlatformMetadata_1(t));

        registry.Register<IPrecodeStubs>("c1", static t => new PrecodeStubs_1(t));
        registry.Register<IPrecodeStubs>("c2", static t => new PrecodeStubs_2(t));
        registry.Register<IPrecodeStubs>("c3", static t => new PrecodeStubs_3(t));

        registry.Register<IReJIT>("c1", static t => new ReJIT_1(t));

        registry.Register<IGC>("c1", static t => new GC_1(t));

        registry.Register<IGCInfo>("c1", static t =>
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

        registry.Register<ISyncBlock>("c1", static t => new SyncBlock_1(t));

        registry.Register<IExecutionManager>("c1", static t => new ExecutionManager_1(t));
        registry.Register<IExecutionManager>("c2", static t => new ExecutionManager_2(t));
    }
}
