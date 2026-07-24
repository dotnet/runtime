// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.TestInfrastructure.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

public unsafe class IXCLRDataTypeInstanceDumpTests : DumpTestBase
{
    private const int HResultErrorInsufficientBuffer = unchecked((int)0x8007007A);

    protected override string DebuggeeName => "TypeHierarchy";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetName_ReturnsFullNameAndLength(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataTypeInstance typeInstance = GetTypeInstance("System.String");

        uint nameLen;
        int hr = typeInstance.GetName(0, 0, &nameLen, null);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal((uint)"System.String".Length + 1, nameLen);

        char[] nameBuf = new char[nameLen];
        fixed (char* name = nameBuf)
        {
            hr = typeInstance.GetName(0, nameLen, null, name);
        }

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal("System.String", new string(nameBuf, 0, (int)nameLen - 1));
        Assert.Equal('\0', nameBuf[^1]);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetName_TruncatedBuffer_ReturnsInsufficientBuffer(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataTypeInstance typeInstance = GetTypeInstance("System.String");
        char[] nameBuf = new char[5];
        uint nameLen;

        int hr;
        fixed (char* name = nameBuf)
        {
            hr = typeInstance.GetName(0, (uint)nameBuf.Length, &nameLen, name);
        }

        AssertHResult(HResultErrorInsufficientBuffer, hr);
        Assert.Equal((uint)"System.String".Length + 1, nameLen);
        Assert.Equal("Syst", new string(nameBuf, 0, nameBuf.Length - 1));
        Assert.Equal('\0', nameBuf[^1]);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetName_InvalidFlags_ReturnsInvalidArgument(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataTypeInstance typeInstance = GetTypeInstance("System.String");

        int hr = typeInstance.GetName(1, 0, null, null);

        AssertHResult(HResults.E_INVALIDARG, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetDefinition_ReturnsTypeDefinition(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataTypeInstance typeInstance = GetTypeInstance("System.String");
        DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition = new(isNullRef: false);

        int hr = typeInstance.GetDefinition(typeDefinition);

        AssertHResult(HResults.S_OK, hr);
        Assert.NotNull(typeDefinition.Interface);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetDefinition_NullOutput_ReturnsNullReference(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataTypeInstance typeInstance = GetTypeInstance("System.String");
        DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition = new(isNullRef: true);

        int hr = typeInstance.GetDefinition(typeDefinition);

        AssertHResult(HResults.E_POINTER, hr);
    }

    private IXCLRDataTypeInstance GetTypeInstance(string fullyQualifiedName)
    {
        ITypeHandle typeHandle = Target.Contracts.ManagedTypeSource.GetTypeHandle(fullyQualifiedName);
        return new ClrDataTypeInstance(Target, typeHandle, legacyImpl: null);
    }
}
