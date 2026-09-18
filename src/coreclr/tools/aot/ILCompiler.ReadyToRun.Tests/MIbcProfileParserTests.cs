// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using crossgen2::ILCompiler;
using crossgen2::ILCompiler.IBC;
using crossgen2::Internal.Pgo;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class MIbcProfileParserTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void ParseMIbcFile_UnresolvableRecordsDoNotAffectFollowingRecords(int missingRecordCount, bool missingAssembly)
    {
        using PEReader mibcReader = CreateMibc(missingRecordCount, missingAssembly, out MemberReferenceHandle missingMethod);
        // Use the test host's core library for signature resolution, not a locally built runtime.
        using var coreLibReader = new PEReader(File.OpenRead(typeof(object).Assembly.Location));
        var context = new CompilerTypeSystemContext(
            new TargetDetails(TargetArchitecture.X64, TargetOS.Linux, TargetAbi.NativeAot),
            SharedGenericsMode.Disabled)
        {
            InputFilePaths = new Dictionary<string, string>(),
            ReferenceFilePaths = new Dictionary<string, string>(),
        };
        context.SetSystemModule(EcmaModule.Create(context, coreLibReader, null));

        if (missingRecordCount > 0)
        {
            EcmaModule module = EcmaModule.Create(context, mibcReader, null);
            if (missingAssembly)
            {
                Assert.ThrowsAny<TypeSystemException>(() => module.GetObject(missingMethod, NotFoundBehavior.ReturnNull));
            }
            else
            {
                Assert.Null(module.GetObject(missingMethod, NotFoundBehavior.ReturnNull));
            }
        }

        ProfileData profile = MIbcProfileParser.ParseMIbcFile(
            context, mibcReader, assemblyNamesInVersionBubble: null, onlyDefinedInAssembly: null,
            MIbcProfileParser.MibcGroupParseRules.AllGroups);

        Assert.Collection(profile.GetAllMethodProfileData(),
            before =>
            {
                Assert.Equal("Before", before.Method.Name.ToString());
                AssertOptionalData(before, 7);
            },
            after =>
            {
                Assert.Equal("After", after.Method.Name.ToString());
                Assert.Equal(0, after.ExclusiveWeight);
                Assert.Null(after.CallWeights);
                Assert.Null(after.SchemaData);
            },
            last =>
            {
                Assert.Equal("Last", last.Method.Name.ToString());
                AssertOptionalData(last, 29);
            });
    }

    private static void AssertOptionalData(MethodProfileData data, int weight)
    {
        Assert.Equal(weight, data.ExclusiveWeight);
        KeyValuePair<MethodDesc, int> call = Assert.Single(data.CallWeights);
        Assert.Equal("Before", call.Key.Name.ToString());
        Assert.Equal(weight + 1, call.Value);
        PgoSchemaElem schema = Assert.Single(data.SchemaData);
        Assert.Equal(PgoInstrumentationKind.BasicBlockIntCount, schema.InstrumentationKind);
        Assert.Equal(0, schema.ILOffset);
        Assert.Equal(1, schema.Count);
        Assert.Equal(0, schema.Other);
        Assert.Equal(weight + 2, schema.DataLong);
        Assert.Null(schema.DataObject);
    }

    private static PEReader CreateMibc(int missingRecordCount, bool missingAssembly, out MemberReferenceHandle missingMethod)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("Profile.mibc"), default, default, default);
        metadata.AddAssembly(metadata.GetOrAddString("Profile"), new Version(1, 0, 0, 0), default, default, default, default);
        metadata.AddTypeDefinition(
            TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        var signature = new BlobBuilder();
        new BlobEncoder(signature).MethodSignature().Parameters(0, returnType => returnType.Void(), parameters => { });
        BlobHandle methodSignature = metadata.GetOrAddBlob(signature);
        var methodBodies = new MethodBodyStreamEncoder(new BlobBuilder());
        var emptyBody = new InstructionEncoder(new BlobBuilder());
        emptyBody.OpCode(ILOpCode.Ret);
        MethodDefinitionHandle before = AddMethod("Before", emptyBody);
        MethodDefinitionHandle after = AddMethod("After", emptyBody);
        MethodDefinitionHandle last = AddMethod("Last", emptyBody);

        AssemblyReferenceHandle missingAssemblyReference = metadata.AddAssemblyReference(
            metadata.GetOrAddString("MissingAssembly"), new Version(1, 0, 0, 0), default, default, default, default);
        TypeReferenceHandle missingBase = metadata.AddTypeReference(
            missingAssemblyReference, default, metadata.GetOrAddString("MissingBase"));
        TypeDefinitionHandle missingMethodOwner = MetadataTokens.TypeDefinitionHandle(metadata.GetRowCount(TableIndex.TypeDef) + 1);
        missingMethod = metadata.AddMemberReference(
            missingMethodOwner, metadata.GetOrAddString("MissingMethod"), methodSignature);

        var groupBody = new InstructionEncoder(new BlobBuilder());
        EmitRecord(groupBody, before, 7);
        for (int i = 0; i < missingRecordCount; i++)
        {
            EmitRecord(groupBody, missingMethod, 99 + i);
        }
        EmitRecord(groupBody, after, null);
        EmitRecord(groupBody, last, 29);
        groupBody.OpCode(ILOpCode.Ret);
        MethodDefinitionHandle group = AddMethod("Group", groupBody);

        var dictionaryBody = new InstructionEncoder(new BlobBuilder());
        dictionaryBody.LoadString(metadata.GetOrAddUserString("Profile;"));
        dictionaryBody.OpCode(ILOpCode.Ldtoken);
        dictionaryBody.Token(group);
        dictionaryBody.OpCode(ILOpCode.Pop);
        dictionaryBody.OpCode(ILOpCode.Ret);
        AddMethod("AssemblyDictionary", dictionaryBody);

        // A directly missing assembly returns null. Searching an unavailable base type for a
        // missing method instead throws TypeSystemException, exercising the other recovery path.
        metadata.AddTypeDefinition(
            TypeAttributes.Public, default, metadata.GetOrAddString("MissingMethodOwner"),
            missingAssembly ? missingBase : default(EntityHandle),
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(metadata.GetRowCount(TableIndex.MethodDef) + 1));

        var peBuilder = new ManagedPEBuilder(
            PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), methodBodies.Builder);
        var image = new BlobBuilder();
        peBuilder.Serialize(image);
        return new PEReader(image.ToImmutableArray());

        MethodDefinitionHandle AddMethod(string name, InstructionEncoder body)
        {
            return metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
                metadata.GetOrAddString(name), methodSignature, methodBodies.AddMethodBody(body),
                MetadataTokens.ParameterHandle(1));
        }

        void EmitRecord(InstructionEncoder body, EntityHandle method, int? weight)
        {
            body.OpCode(ILOpCode.Ldtoken);
            body.Token(method);
            if (weight is int value)
            {
                const int UpdateILOffsetKindAndCount = 0x7;

                body.LoadString(metadata.GetOrAddUserString("ExclusiveWeight"));
                body.LoadConstantI4(value);
                body.LoadString(metadata.GetOrAddUserString("WeightedCallData"));
                body.LoadConstantI4(1);
                body.OpCode(ILOpCode.Ldtoken);
                body.Token(before);
                body.LoadConstantI4(value + 1);
                body.LoadString(metadata.GetOrAddUserString("InstrumentationDataStart"));
                body.LoadConstantI4(UpdateILOffsetKindAndCount);
                body.LoadConstantI4(0);
                body.LoadConstantI4((int)PgoInstrumentationKind.BasicBlockIntCount);
                body.LoadConstantI4(1);
                body.LoadConstantI4(value + 2);
                body.LoadString(metadata.GetOrAddUserString("InstrumentationDataEnd"));
            }
            body.OpCode(ILOpCode.Pop);
        }
    }
}
