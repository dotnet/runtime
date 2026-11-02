// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Xunit;
using Xunit.Abstractions;

namespace ILCompiler.ReadyToRun.Tests;

public class MIbcProfileParserTests
{
    private const string AssemblyName = "MibcMethods";
    private readonly ITestOutputHelper _output;

    public MIbcProfileParserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PartialCompilationAfterUnresolvedMibcMethod(bool includeMissingMethod, bool composite)
    {
        string profilePath = Path.GetTempFileName();
        try
        {
            WriteMibc(profilePath, includeMissingMethod);
            var assembly = new CompiledAssembly
            {
                AssemblyName = AssemblyName,
                SourceResourceNames = ["Mibc/Methods.cs"],
            };

            new R2RTestRunner(_output).Run(new R2RTestCase(
                nameof(PartialCompilationAfterUnresolvedMibcMethod),
                [
                    new(AssemblyName, [new CrossgenAssembly(assembly)])
                    {
                        Options = composite ? [Crossgen2Option.Composite] : [],
                        AdditionalArgs = ["--partial", "--mibc", profilePath],
                        OutputFileExtension = TestPaths.IsWasmTarget ? ".wasm" : ".dll",
                        Validate = reader =>
                        {
                            Assert.True(R2RAssert.HasCompiledMethod(reader, "ProfiledMethods", "AfterMissing", out string diag), diag);
                            Assert.True(R2RAssert.HasCompiledMethod(reader, "ProfiledMethods", "Control", out diag), diag);
                            Assert.False(R2RAssert.HasCompiledMethod(reader, "ProfiledMethods", "NotInProfile", out diag), diag);
                        },
                    },
                ]));
        }
        finally
        {
            File.Delete(profilePath);
        }
    }

    private static void WriteMibc(string path, bool includeMissingMethod)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("Profile.mibc"), default, default, default);
        metadata.AddAssembly(metadata.GetOrAddString("Profile"), new Version(1, 0, 0, 0), default, default, default, default);
        metadata.AddTypeDefinition(
            TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        AssemblyReferenceHandle assembly = metadata.AddAssemblyReference(
            metadata.GetOrAddString(AssemblyName), new Version(0, 0, 0, 0), default, default, default, default);
        TypeReferenceHandle type = metadata.AddTypeReference(assembly, default, metadata.GetOrAddString("ProfiledMethods"));
        var methodSignature = new BlobBuilder();
        new BlobEncoder(methodSignature).MethodSignature().Parameters(
            0, returnType => returnType.Type().Int32(), parameters => { });
        var globalSignature = new BlobBuilder();
        new BlobEncoder(globalSignature).MethodSignature().Parameters(
            0, returnType => returnType.Void(), parameters => { });
        var methodBodies = new MethodBodyStreamEncoder(new BlobBuilder());

        var groupBody = new InstructionEncoder(new BlobBuilder());
        if (includeMissingMethod)
        {
            EmitRecord("Missing");
        }
        EmitRecord("AfterMissing");
        EmitRecord("Control");
        MethodDefinitionHandle group = AddMethod("Group", groupBody);

        var dictionaryBody = new InstructionEncoder(new BlobBuilder());
        dictionaryBody.LoadString(metadata.GetOrAddUserString(AssemblyName + ";"));
        dictionaryBody.OpCode(ILOpCode.Ldtoken);
        dictionaryBody.Token(group);
        dictionaryBody.OpCode(ILOpCode.Pop);
        AddMethod("AssemblyDictionary", dictionaryBody);

        var image = new BlobBuilder();
        new ManagedPEBuilder(
            PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), methodBodies.Builder).Serialize(image);
        using FileStream stream = File.Create(path);
        image.WriteContentTo(stream);

        void EmitRecord(string methodName)
        {
            MemberReferenceHandle method = metadata.AddMemberReference(
                type, metadata.GetOrAddString(methodName), metadata.GetOrAddBlob(methodSignature));
            groupBody.OpCode(ILOpCode.Ldtoken);
            groupBody.Token(method);
            groupBody.OpCode(ILOpCode.Pop);
        }

        MethodDefinitionHandle AddMethod(string name, InstructionEncoder body)
        {
            body.OpCode(ILOpCode.Ret);
            return metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
                metadata.GetOrAddString(name), metadata.GetOrAddBlob(globalSignature),
                methodBodies.AddMethodBody(body), MetadataTokens.ParameterHandle(1));
        }
    }
}
