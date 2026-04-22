// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.Tests.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for IXCLRDataMethodDefinition methods.
/// Tests GetName, HasClassOrMethodInstantiation, and Start/Enum/EndEnumInstances
/// using the StackWalk and TypeHierarchy debuggee dumps.
/// </summary>
public unsafe class IXCLRDataMethodDefinitionDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "full";

    // ========== GetName ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_QueryLengthOnly_ReturnsSizeWithoutBuffer(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataMethodDefinition methodDef = GetMethodDefinitionFromStack("MethodC");

        uint nameLen;
        int hr = methodDef.GetName(0, 0, &nameLen, null);

        AssertHResult(HResults.S_OK, hr);
        Assert.True(nameLen > 1, "Expected nameLen > 1 (name + null terminator)");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_FullRetrieval_ReturnsExpectedName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataMethodDefinition methodDef = GetMethodDefinitionFromStack("MethodC");

        uint nameLen;
        int hr = methodDef.GetName(0, 0, &nameLen, null);
        AssertHResult(HResults.S_OK, hr);

        char[] nameBuf = new char[nameLen];
        uint nameLen2;
        fixed (char* pName = nameBuf)
        {
            hr = methodDef.GetName(0, nameLen, &nameLen2, pName);
        }

        AssertHResult(HResults.S_OK, hr);
        string name = new string(nameBuf, 0, (int)nameLen2 - 1);
        Assert.False(string.IsNullOrEmpty(name), "Expected non-empty method name");
        Assert.Contains("MethodC", name);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_TruncatedBuffer_ReturnsSFalse(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataMethodDefinition methodDef = GetMethodDefinitionFromStack("MethodC");

        uint fullLen;
        int hr = methodDef.GetName(0, 0, &fullLen, null);
        AssertHResult(HResults.S_OK, hr);
        Assert.True(fullLen > 2, "Need a name long enough to truncate");

        uint truncLen = fullLen - 1;
        char[] nameBuf = new char[truncLen];
        uint reportedLen;
        fixed (char* pName = nameBuf)
        {
            hr = methodDef.GetName(0, truncLen, &reportedLen, pName);
            AssertHResult(HResults.S_FALSE, hr);
        }

        Assert.Equal(fullLen, reportedLen);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetName_NameIsNullTerminated(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataMethodDefinition methodDef = GetMethodDefinitionFromStack("MethodC");

        uint nameLen;
        int hr = methodDef.GetName(0, 0, &nameLen, null);
        AssertHResult(HResults.S_OK, hr);

        char[] nameBuf = new char[nameLen];
        fixed (char* pName = nameBuf)
        {
            hr = methodDef.GetName(0, nameLen, null, pName);
            AssertHResult(HResults.S_OK, hr);
            Assert.Equal('\0', pName[nameLen - 1]);
        }
    }

    // ========== HasClassOrMethodInstantiation ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void HasClassOrMethodInstantiation_NonGenericMethod_ReturnsFalse(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataMethodDefinition methodDef = GetMethodDefinitionFromStack("MethodC");

        int bGeneric;
        int hr = methodDef.HasClassOrMethodInstantiation(&bGeneric);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal((int)Interop.BOOL.FALSE, bGeneric);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void HasClassOrMethodInstantiation_GenericTypeMethod_ReturnsTrue(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataMethodDefinition methodDef = GetGenericMethodDefinition();

        int bGeneric;
        int hr = methodDef.HasClassOrMethodInstantiation(&bGeneric);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal((int)Interop.BOOL.TRUE, bGeneric);
    }

    // ========== StartEnumInstances / EnumInstance / EndEnumInstances ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void EnumInstances_NonGenericMethod_ReturnsExactlyOneInstance(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataMethodDefinition methodDef = GetMethodDefinitionFromStack("MethodC");

        ulong handle;
        int hr = methodDef.StartEnumInstances(null, &handle);
        AssertHResult(HResults.S_OK, hr);
        Assert.NotEqual(0ul, handle);

        try
        {
            DacComNullableByRef<IXCLRDataMethodInstance> instanceOut = new(isNullRef: false);
            hr = methodDef.EnumInstance(&handle, instanceOut);
            AssertHResult(HResults.S_OK, hr);
            Assert.NotNull(instanceOut.Interface);

            DacComNullableByRef<IXCLRDataMethodInstance> instanceOut2 = new(isNullRef: false);
            hr = methodDef.EnumInstance(&handle, instanceOut2);
            AssertHResult(HResults.S_FALSE, hr);
        }
        finally
        {
            hr = methodDef.EndEnumInstances(handle);
            AssertHResult(HResults.S_OK, hr);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void EndEnumInstances_ZeroHandle_ReturnsError(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IXCLRDataMethodDefinition methodDef = GetMethodDefinitionFromStack("MethodC");

        int hr = methodDef.EndEnumInstances(0);
        Assert.True(hr < 0, $"Expected failure HRESULT for handle=0, got {FormatHResult(hr)}");
    }

    // ========== Helpers ==========

    /// <summary>
    /// Finds a managed method by name on the crashing thread's stack, then creates
    /// a <see cref="ClrDataMethodDefinition"/> from its module and token.
    /// </summary>
    private IXCLRDataMethodDefinition GetMethodDefinitionFromStack(string methodName)
    {
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle frame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            if (methodDescPtr == TargetPointer.Null)
                continue;

            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            string? name = DumpTestHelpers.GetMethodName(Target, mdHandle);
            if (name is null || !name.Contains(methodName))
                continue;

            uint token = rts.GetMethodToken(mdHandle);
            TargetPointer mt = rts.GetMethodTable(mdHandle);
            TargetPointer modulePtr = rts.GetModule(rts.GetTypeHandle(mt));

            return new ClrDataMethodDefinition(Target, modulePtr, token, legacyImpl: null);
        }

        Assert.Fail($"Could not find method '{methodName}' on the crashing thread's stack");
        return null!;
    }

    /// <summary>
    /// Finds a method on a loaded generic type definition in System.Private.CoreLib
    /// (e.g. <c>List&lt;&gt;</c>) and creates a <see cref="ClrDataMethodDefinition"/>
    /// for it.
    /// </summary>
    private IXCLRDataMethodDefinition GetGenericMethodDefinition()
    {
        ILoader loader = Target.Contracts.Loader;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        TargetPointer systemAssembly = loader.GetSystemAssembly();
        Contracts.ModuleHandle coreLibModule = loader.GetModuleHandleFromAssemblyPtr(systemAssembly);
        TypeHandle listTypeDef = rts.GetTypeByNameAndModule(
            "List`1",
            "System.Collections.Generic",
            coreLibModule);
        Assert.True(listTypeDef.Address != 0, "Could not find List<> type definition in CoreLib");

        TargetPointer modulePtr = rts.GetModule(listTypeDef);
        IEcmaMetadata ecmaMetadata = Target.Contracts.EcmaMetadata;
        MetadataReader reader = ecmaMetadata.GetMetadata(coreLibModule)
            ?? throw new InvalidOperationException("Failed to get metadata reader for CoreLib");

        uint typeToken = rts.GetTypeDefToken(listTypeDef);
        int rowId = (int)(typeToken & 0x00FFFFFF);
        TypeDefinitionHandle tdh = MetadataTokens.TypeDefinitionHandle(rowId);
        TypeDefinition td = reader.GetTypeDefinition(tdh);

        ModuleLookupTables tables = loader.GetLookupTables(coreLibModule);
        foreach (MethodDefinitionHandle mdh in td.GetMethods())
        {
            uint token = (uint)MetadataTokens.GetToken(mdh);
            TargetPointer mdAddr = loader.GetModuleLookupMapElement(
                tables.MethodDefToDesc, token, out _);
            if (mdAddr == TargetPointer.Null)
                continue;

            return new ClrDataMethodDefinition(Target, modulePtr, token, legacyImpl: null);
        }

        Assert.Fail("Could not find a loaded method on List<> in CoreLib");
        return null!;
    }
}
