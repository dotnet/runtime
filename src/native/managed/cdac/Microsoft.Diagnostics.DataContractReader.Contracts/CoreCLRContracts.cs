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
    /// provided for the target. Contract availability is checked without instantiating the
    /// contracts; <see cref="IRuntimeInfo"/> is read to determine the target operating system so
    /// that OS-specific contracts are validated only when the target platform actually uses them.
    /// In-box (main-descriptor) contracts are required unconditionally. Contracts published by a
    /// sub-descriptor are version-checked always, but their absence is tolerated while their
    /// sub-descriptor is still pending.
    /// </summary>
    /// <param name="target">The target being validated (source of the contract registry and
    /// sub-descriptor resolution state).</param>
    /// <exception cref="ContractNotAvailableException">
    /// Thrown for the first required contract that cannot be provided. The concrete exception type
    /// and its <see cref="System.Exception.HResult"/> identify the failure:
    /// <see cref="ContractMissingException"/> / <see cref="CdacHResults.CDAC_E_CONTRACT_NOT_ADVERTISED"/>
    /// if the target does not advertise a required contract,
    /// <see cref="ContractUnrecognizedException"/> / <see cref="CdacHResults.CDAC_E_CONTRACT_UNRECOGNIZED"/>
    /// if the advertised version is unknown to this cDAC, or
    /// <see cref="ContractObsoleteException"/> / <see cref="CdacHResults.CDAC_E_CONTRACT_UNSUPPORTED"/>
    /// if the advertised version is recognized but intentionally unimplemented.
    /// </exception>
    public static void ValidateForDataAccess(Target target)
    {
        ContractRegistry registry = target.Contracts;

        // In-box (main-descriptor) contract accesses across the ISOSDac* and IXCLRData* surface that
        // SOSDacImpl exposes. These live in the main descriptor, present as soon as the runtime module
        // is loaded, so they are required eagerly and unconditionally - a genuinely-missing one is a
        // serviceability failure even at early attach. IObjectiveCMarshal is intentionally omitted:
        // SOS reaches it through TryGetContract so its absence degrades gracefully rather than faulting.
        Validate<IAuxiliarySymbols>(registry);
        Validate<ICodeNotifications>(registry);
        Validate<ICodeVersions>(registry);
        Validate<IComWrappers>(registry);
        Validate<IDacStreams>(registry);
        Validate<IDebugInfo>(registry);
        Validate<IEcmaMetadata>(registry);
        Validate<IException>(registry);
        Validate<IExecutionManager>(registry);
        Validate<IFeatureFlags>(registry);
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

        // Operating-system-specific in-box contracts, gated on the target's platform. IRuntimeInfo is
        // in the main descriptor (present at attach), so reading the OS here is safe. These contracts
        // are advertised only where the runtime is built for that platform, so the gate keeps them
        // from being required where the runtime never advertises them - this is genuine absence, not a
        // sub-descriptor deferral.
        RuntimeInfoOperatingSystem targetOperatingSystem = registry.RuntimeInfo.GetTargetOperatingSystem();
        if (targetOperatingSystem == RuntimeInfoOperatingSystem.Windows)
        {
            // IBuiltInCOM is only advertised on runtimes built with classic COM interop (Windows).
            Validate<IBuiltInCOM>(registry);            // SOSDacImpl.cs GetCCWData/GetRCWData/etc.
            Validate<IWindowsErrorReporting>(registry); // SOSDacImpl.cs GetClrWatsonBuckets
        }

        // DBI-only in-box contract: used by the DacDbi path (Legacy/Dbi/DacDbiImpl.cs).
        // Since cDAC is all-or-nothing for some tools debugger, a target either
        // exposes a fully serviceable set of contracts, or we fall back/fail.
        Validate<IRuntimeMutableTypeSystem>(registry); // DacDbiImpl.cs (edit-and-continue mutable type system)

        // Sub-descriptor-provided contracts. Only validated if they have been published.
        //  A version this cDAC cannot service is always rejected, but a missing one is rejected only once the
        // sub-descriptor is resolved. Defer and let the tool APIs see a degradation to E_NOTIMPL.
        ValidateSubDescriptorContract<IGC>(target);

        static void Validate<TContract>(ContractRegistry registry) where TContract : IContract
        {
            if (registry.TryValidate<TContract>(out System.Exception? failure))
            {
                return;
            }

            // TryValidate reports the failure through a contract availability exception that already
            // carries the appropriate cDAC HRESULT, so rethrow it directly. The null-coalescing arm
            // only guards a registry that violates the TryValidate contract (false without a failure).
            throw failure ?? new ContractNotAvailableException(
                TContract.Name,
                contractVersion: null,
                message: $"Contract '{TContract.Name}' validation failed but no reason was reported.");
        }

        static void ValidateSubDescriptorContract<TContract>(Target target) where TContract : IContract
        {
            if (target.Contracts.TryValidate<TContract>(out System.Exception? failure))
            {
                return;
            }

            // A version this cDAC cannot service (unrecognized or obsolete) is always a failure, even
            // during early attach. A not-advertised contract is a failure only once the sub-descriptor
            // that publishes it has resolved; while that provider is still pending the contract may yet
            // be published, so defer rather than fail creation.
            bool providerResolved = target.IsSubDescriptorResolved(TContract.Name);
            if (failure is ContractUnrecognizedException or ContractObsoleteException || providerResolved)
            {
                throw failure ?? new ContractNotAvailableException(
                    TContract.Name,
                    contractVersion: null,
                    message: $"Contract '{TContract.Name}' validation failed but no reason was reported.");
            }
        }
    }
}
