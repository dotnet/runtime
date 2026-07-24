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
        registry.Register<ISignature>("c1", static t => new Signature_1(t));
        registry.Register<ICallingConvention>("c1", static t => new CallingConvention_1(t));
        registry.Register<IBuiltInCOM>("c1", static t => new BuiltInCOM_1(t));
        registry.Register<IObjectiveCMarshal>("c1", static t => new ObjectiveCMarshal_1(t));
        registry.Register<IConditionalWeakTable>("c1", static t => new ConditionalWeakTable_1(t));
        registry.Register<IManagedTypeSource>("c1", static t => new ManagedTypeSource_1(t));
        registry.Register<IAuxiliarySymbols>("c1", static t => new AuxiliarySymbols_1(t));
        registry.Register<IDebugger>("c1", static t => new Debugger_1(t));

        registry.Register<IDebugInfo>("c1", static t => new DebugInfo_1(t));
        registry.Register<IDebugInfo>("c2", static t => new DebugInfo_2(t));
        registry.Register<IStressLog>("c1", static t => new StressLog_1(t));
        registry.Register<IStressLog>("c2", static t => new StressLog_2(t));

        registry.Register<IThread>("c1", static t => new Thread_1(t));
        registry.Register<IWindowsErrorReporting>("c1", static t => new WindowsErrorReporting_1(t));

        registry.Register<IRuntimeTypeSystem>("c1", static t => new RuntimeTypeSystem_1(t));

        registry.Register<IObject>("c1", static t => new Object_1(t));

        registry.Register<IPlatformMetadata>("c1", static t => new PlatformMetadata_1(t));

        registry.Register<IFeatureFlags>("c1", static t => new FeatureFlags_1(t));

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
                RuntimeInfoArchitecture.X86 => new GCInfoX86_1(t),
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

        registry.Register<IRuntimeMutableTypeSystem>("c1", static t => new RuntimeMutableTypeSystem_1(t));
    }

    /// <summary>
    /// Eagerly validates that every contract required by the cDAC data-access interfaces can be
    /// provided for the target, without instantiating any of them. This is intended to run at
    /// interface-creation time so that an unsupported target fails fast with a distinct
    /// <see cref="CdacHResults"/> code instead of failing lazily inside a later data-access call.
    /// </summary>
    /// <param name="registry">The contract registry for the target being validated.</param>
    /// <exception cref="ContractValidationException">
    /// Thrown for the first required contract that cannot be provided. The exception's
    /// <see cref="ContractValidationException.HResult"/> is
    /// <see cref="CdacHResults.CDAC_E_CONTRACT_NOT_ADVERTISED"/> if the target does not advertise the
    /// contract, <see cref="CdacHResults.CDAC_E_CONTRACT_UNRECOGNIZED"/> if the advertised version is
    /// unknown to this cDAC, or <see cref="CdacHResults.CDAC_E_CONTRACT_UNSUPPORTED"/> if the advertised
    /// version is recognized but intentionally unimplemented.
    /// </exception>
    public static void ValidateForDataAccess(ContractRegistry registry)
    {
        // Direct contract accesses from SOSDacImpl.cs and SOSDacImpl.IXCLRDataProcess.cs.
        // IObjectiveCMarshal is optional because SOS uses TryGetContract for Apple-only support.
        Validate<IAuxiliarySymbols>(registry);
        Validate<IBuiltInCOM>(registry);
        Validate<ICodeNotifications>(registry);
        Validate<ICodeVersions>(registry);
        Validate<IComWrappers>(registry);
        Validate<IDacStreams>(registry);
        Validate<IEcmaMetadata>(registry);
        Validate<IException>(registry);
        Validate<IExecutionManager>(registry);
        Validate<IGC>(registry);
        Validate<IGCInfo>(registry);
        Validate<ILoader>(registry);
        Validate<INotifications>(registry);
        Validate<IObject>(registry);
        Validate<IPrecodeStubs>(registry);
        Validate<IReJIT>(registry);
        Validate<IRuntimeInfo>(registry);
        Validate<IRuntimeTypeSystem>(registry);
        Validate<ISignature>(registry);
        Validate<IStackWalk>(registry);
        Validate<IStressLog>(registry);
        Validate<ISyncBlock>(registry);
        Validate<IThread>(registry);

        // Transitive contract accesses from the implementations above.
        Validate<IConditionalWeakTable>(registry); // IComWrappers: ComWrappers_1.cs
        Validate<IDebugger>(registry);             // IStackWalk: StackWalk_1.cs
        Validate<IPlatformMetadata>(registry);     // IAuxiliarySymbols/IPrecodeStubs: CodePointerUtils.cs, PrecodeStubs_Common.cs
        Validate<ISHash>(registry);                // ILoader: Loader_1.cs

        static void Validate<TContract>(ContractRegistry registry) where TContract : IContract
        {
            if (registry.TryValidate<TContract>(out System.Exception? failure))
            {
                return;
            }

            throw failure switch
            {
                ContractObsoleteException obsolete => new ContractValidationException(CdacHResults.CDAC_E_CONTRACT_UNSUPPORTED, obsolete),
                ContractUnrecognizedException unrecognized => new ContractValidationException(CdacHResults.CDAC_E_CONTRACT_UNRECOGNIZED, unrecognized),
                ContractNotAvailableException notAvailable => new ContractValidationException(CdacHResults.CDAC_E_CONTRACT_NOT_ADVERTISED, notAvailable),
                _ => failure,
            };
        }
    }
}
