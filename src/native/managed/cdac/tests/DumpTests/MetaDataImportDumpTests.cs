// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for MetaDataImportImpl semantic parity.
/// Verifies our managed IMetaDataImport implementation against real metadata from dumps.
/// Uses the MultiModule debuggee which contains non-const fields, user strings, and
/// methods on named types.
/// </summary>
public class MetaDataImportDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "MultiModule";
    protected override string DumpType => "full";

    private (MetadataReader reader, IMetaDataImport mdi) GetRootModuleImport()
    {
        ILoader loader = Target.Contracts.Loader;
        IEcmaMetadata ecmaMetadata = Target.Contracts.EcmaMetadata;

        TargetPointer rootAssembly = loader.GetRootAssembly();
        Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(rootAssembly);

        MetadataReader? reader = ecmaMetadata.GetMetadata(moduleHandle);
        Assert.NotNull(reader);

        return (reader, new MetaDataImportImpl(reader));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void GetFieldProps_NoConstant_ReturnsElementTypeVoid(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (reader, mdi) = GetRootModuleImport();

        // Find a field without a constant value.
        // The MultiModule debuggee has s_nonConstField which has no default constant.
        uint fieldToken = 0;
        foreach (TypeDefinitionHandle tdh in reader.TypeDefinitions)
        {
            TypeDefinition td = reader.GetTypeDefinition(tdh);
            foreach (FieldDefinitionHandle fdh in td.GetFields())
            {
                FieldDefinition fd = reader.GetFieldDefinition(fdh);
                if (fd.GetDefaultValue().IsNil)
                {
                    fieldToken = (uint)MetadataTokens.GetToken(fdh);
                    break;
                }
            }
            if (fieldToken != 0)
                break;
        }

        Assert.True(fieldToken != 0, "Expected at least one field without a constant in MultiModule debuggee");

        uint dwCPlusTypeFlag;
        void* pValue;
        uint cchValue;
        int hr = mdi.GetFieldProps(fieldToken, null, null, 0, null, null, null, null, &dwCPlusTypeFlag, &pValue, &cchValue);

        Assert.Equal(HResults.S_OK, hr);
        // Native RegMeta returns ELEMENT_TYPE_VOID (1) when field has no constant
        Assert.Equal(1u, dwCPlusTypeFlag);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void GetMethodProps_NonGlobalMethod_ReturnsParentClass(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (reader, mdi) = GetRootModuleImport();

        // Find a method on a non-<Module> type (TypeDef RID > 1)
        uint methodToken = 0;
        uint expectedParentToken = 0;
        foreach (TypeDefinitionHandle tdh in reader.TypeDefinitions)
        {
            int typeRid = MetadataTokens.GetRowNumber(tdh);
            if (typeRid == 1) continue; // skip <Module>

            TypeDefinition td = reader.GetTypeDefinition(tdh);
            foreach (MethodDefinitionHandle mdh in td.GetMethods())
            {
                methodToken = (uint)MetadataTokens.GetToken(mdh);
                expectedParentToken = (uint)MetadataTokens.GetToken(tdh);
                break;
            }
            if (methodToken != 0) break;
        }

        Assert.True(methodToken != 0, "Expected at least one method on a non-global type in MultiModule debuggee");

        uint pClass;
        int hr = mdi.GetMethodProps(methodToken, &pClass, null, 0, null, null, null, null, null, null);

        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal(expectedParentToken, pClass);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void GetUserString_ReturnsCharCountWithoutNull(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (reader, mdi) = GetRootModuleImport();

        // Walk the user string heap for a non-empty string.
        // The MultiModule debuggee has string literals that populate the #US heap.
        uint userStringToken = 0;
        int expectedCharCount = 0;
        UserStringHandle ush = MetadataTokens.UserStringHandle(1);
        while (!ush.IsNil)
        {
            string value = reader.GetUserString(ush);
            if (value.Length > 0)
            {
                userStringToken = (uint)MetadataTokens.GetToken(ush);
                expectedCharCount = value.Length;
                break;
            }
            ush = reader.GetNextHandle(ush);
        }

        Assert.True(userStringToken != 0, "Expected at least one user string in MultiModule debuggee");

        uint pchString;
        int hr = mdi.GetUserString(userStringToken, null, 0, &pchString);

        Assert.Equal(HResults.S_OK, hr);
        // Native returns character count WITHOUT null terminator
        Assert.Equal((uint)expectedCharCount, pchString);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void EnumTypeDefs_MatchesMetadataReader(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (reader, mdi) = GetRootModuleImport();

        List<uint> enumTokens = new();
        nint hEnum = 0;
        uint* rTokens = stackalloc uint[16];

        while (true)
        {
            uint count;
            int hr = mdi.EnumTypeDefs(&hEnum, rTokens, 16, &count);
            Assert.True(hr >= 0);
            if (count == 0)
                break;
            for (uint i = 0; i < count; i++)
                enumTokens.Add(rTokens[i]);
        }

        List<uint> readerTokens = new();
        foreach (TypeDefinitionHandle h in reader.TypeDefinitions)
            readerTokens.Add((uint)MetadataTokens.GetToken(h));

        Assert.Equal(readerTokens, enumTokens);
        mdi.CloseEnum(hEnum);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "Assembly type does not include IsDynamic/IsLoaded fields in .NET 10")]
    public unsafe void EnumMethods_MatchesMetadataReader(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (reader, mdi) = GetRootModuleImport();

        // Find a type with methods (skip <Module>)
        uint classToken = 0;
        TypeDefinitionHandle targetType = default;
        foreach (TypeDefinitionHandle tdh in reader.TypeDefinitions)
        {
            if (MetadataTokens.GetRowNumber(tdh) == 1) continue;
            TypeDefinition td = reader.GetTypeDefinition(tdh);
            if (td.GetMethods().Count > 0)
            {
                classToken = (uint)MetadataTokens.GetToken(tdh);
                targetType = tdh;
                break;
            }
        }

        Assert.True(classToken != 0, "Expected at least one non-global type with methods");

        List<uint> enumTokens = new();
        nint hEnum = 0;
        uint* rMethods = stackalloc uint[16];

        while (true)
        {
            uint count;
            int hr = mdi.EnumMethods(&hEnum, classToken, rMethods, 16, &count);
            Assert.True(hr >= 0);
            if (count == 0)
                break;
            for (uint i = 0; i < count; i++)
                enumTokens.Add(rMethods[i]);
        }

        List<uint> readerTokens = new();
        foreach (MethodDefinitionHandle mdh in reader.GetTypeDefinition(targetType).GetMethods())
            readerTokens.Add((uint)MetadataTokens.GetToken(mdh));

        Assert.Equal(readerTokens, enumTokens);
        mdi.CloseEnum(hEnum);
    }
}
