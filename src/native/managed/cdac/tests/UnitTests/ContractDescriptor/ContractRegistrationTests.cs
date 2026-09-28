// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure.ContractDescriptor;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

/// <summary>
/// Tests contract-version resolution through a real <see cref="ContractDescriptorTarget"/>
/// (and thus the production <c>CachingContractRegistry</c>), in particular the
/// empty-string "default" registration used as a fallback when the target does not
/// advertise a version for a contract.
/// </summary>
public class ContractRegistrationTests
{
    private interface IFakeContract : IContract
    {
        static string IContract.Name { get; } = "FakeContract";
        string Tag { get; }
    }

    private sealed class FakeContract(string tag) : IFakeContract
    {
        public string Tag => tag;
    }

    private static ContractDescriptorTarget CreateTarget(
        MockTarget.Architecture arch,
        string[] advertisedContracts,
        Action<ContractRegistry> registerFake)
    {
        TargetTestHelpers helpers = new(arch);
        ContractDescriptorBuilder builder = new(helpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptor = new(builder);
        descriptor.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetGlobals(Array.Empty<(string, ulong, string?)>())
            .SetContracts(advertisedContracts);

        bool created = builder.TryCreateTarget(
            descriptor,
            out ContractDescriptorTarget? target,
            [Contracts.CoreCLRContracts.Register, registerFake]);
        Assert.True(created);
        return target!;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void NoAdvertisedVersion_FallsBackToDefaultRegistration(MockTarget.Architecture arch)
    {
        // Target advertises no version for FakeContract -> the empty-string
        // "default" registration is used.
        ContractDescriptorTarget target = CreateTarget(
            arch,
            advertisedContracts: [],
            registerFake: static r => r.Register<IFakeContract>(string.Empty, static t => new FakeContract("default")));

        Assert.True(target.Contracts.TryGetContract<IFakeContract>(out IFakeContract? contract));
        Assert.Equal("default", contract.Tag);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void NoAdvertisedVersion_NoDefaultRegistration_Fails(MockTarget.Architecture arch)
    {
        // FakeContract is neither advertised nor registered.
        ContractDescriptorTarget target = CreateTarget(
            arch,
            advertisedContracts: [],
            registerFake: static _ => { });

        Assert.False(target.Contracts.TryGetContract<IFakeContract>(out IFakeContract? contract));
        Assert.Null(contract);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void AdvertisedVersion_UsesVersionedRegistration_NotDefault(MockTarget.Architecture arch)
    {
        // Target advertises FakeContract (the builder emits version "c1"); both a
        // versioned and a default registration exist, and the advertised version
        // must win.
        ContractDescriptorTarget target = CreateTarget(
            arch,
            advertisedContracts: ["FakeContract"],
            registerFake: static r =>
            {
                r.Register<IFakeContract>("c1", static t => new FakeContract("v1"));
                r.Register<IFakeContract>(string.Empty, static t => new FakeContract("default"));
            });

        Assert.True(target.Contracts.TryGetContract<IFakeContract>(out IFakeContract? contract));
        Assert.Equal("v1", contract.Tag);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void AdvertisedVersion_NoMatchingRegistration_DoesNotFallBackToDefault(MockTarget.Architecture arch)
    {
        // Target advertises FakeContract (version "c1"), but only a default ("")
        // registration exists. This is a version-skew failure and must NOT
        // silently use the default registration.
        ContractDescriptorTarget target = CreateTarget(
            arch,
            advertisedContracts: ["FakeContract"],
            registerFake: static r => r.Register<IFakeContract>(string.Empty, static t => new FakeContract("default")));

        Assert.False(target.Contracts.TryGetContract<IFakeContract>(out IFakeContract? contract));
        Assert.Null(contract);
    }
}
