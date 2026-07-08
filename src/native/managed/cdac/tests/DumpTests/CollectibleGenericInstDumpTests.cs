// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based regression test for the RuntimeTypeSystem contract's
/// <see cref="IRuntimeTypeSystem.GetConstructedType"/> loader-module computation.
///
/// The CollectibleGenericInst debuggee roots a <c>List&lt;CollectibleArg&gt;</c> whose
/// type argument lives in a collectible <c>AssemblyLoadContext</c>. The runtime registers
/// that instantiation in the collectible argument's loader module — not the generic
/// definition's module — so resolving it requires computing the loader module from the
/// type arguments (mirroring <c>ClassLoader::ComputeLoaderModuleWorker</c>). See
/// dotnet/runtime#130143.
/// </summary>
public class CollectibleGenericInstDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "CollectibleGenericInst";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetConstructedType_ResolvesGenericInstWithCollectibleTypeArgument(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        IGC gc = Target.Contracts.GC;
        IObject objectContract = Target.Contracts.Object;

        // Find the List<CollectibleArg> instance rooted by the debuggee. It is the
        // single-argument generic instantiation whose loader module differs from its
        // definition module — the signature of a type argument from a collectible ALC.
        TypeHandle constructed = default;
        foreach (HandleData handle in gc.GetHandles([HandleType.Strong]))
        {
            TargetPointer objAddr = Target.ReadPointer(handle.Handle);
            if (objAddr == TargetPointer.Null)
                continue;

            TypeHandle candidate = rts.GetTypeHandle(objectContract.GetMethodTableAddress(objAddr));
            if (rts.GetInstantiation(candidate).Length == 1 &&
                rts.GetModule(candidate) != rts.GetLoaderModule(candidate) &&
                rts.IsCollectible(candidate))
            {
                constructed = candidate;
                break;
            }
        }

        Assert.NotEqual(TargetPointer.Null, constructed.Address);

        // Confirm the collectible scenario: the constructed type's loader module is the
        // collectible argument's module, distinct from its (CoreLib) definition module.
        Assert.NotEqual(rts.GetModule(constructed), rts.GetLoaderModule(constructed));

        TypeHandle typeArgument = rts.GetInstantiation(constructed)[0];

        // The open List<> definition lives in CoreLib; look it up by name.
        TypeHandle listDefinition = Target.Contracts.ManagedTypeSource.GetTypeHandle(
            "System.Collections.Generic.List`1");
        Assert.NotEqual(TargetPointer.Null, listDefinition.Address);

        // Reconstruct the instantiation. This must search the collectible argument's
        // loader module — searching the definition's module (CoreLib) returns null.
        TypeHandle resolved = rts.GetConstructedType(
            listDefinition,
            CorElementType.GenericInst,
            0,
            [typeArgument]);

        Assert.Equal(constructed.Address, resolved.Address);
    }
}
