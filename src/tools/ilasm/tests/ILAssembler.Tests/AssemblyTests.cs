// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class AssemblyTests
    {
        [Fact]
        public void Diagnostic_AssemblyNotFound()
        {
            // Reference an assembly that doesn't exist in an exported type declaration
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .assembly extern NonExistentAssembly
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            // Expect AssemblyNotFound error + MissingExportedTypeImplementation warning
            Assert.Equal(2, diagnostics.Length);
            var error = diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(DiagnosticIds.AssemblyNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void CoreAssemblyResolution_PrefersSystemRuntime()
        {
            // When System.Runtime is referenced, implicit base types should use it
            // A class with no explicit extends clause implicitly extends System.Object
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Verify System.Runtime is the only assembly reference (no mscorlib created)
            var asmRefs = reader.AssemblyReferences.Select(reader.GetAssemblyReference).ToArray();
            Assert.Single(asmRefs);
            Assert.Equal("System.Runtime", reader.GetString(asmRefs[0].Name));

            // Verify System.Object is referenced from System.Runtime
            var typeRefs = reader.TypeReferences.Select(reader.GetTypeReference).ToArray();
            var objectRef = typeRefs.Single(t => reader.GetString(t.Name) == "Object");
            Assert.Equal("System", reader.GetString(objectRef.Namespace));
            Assert.Equal(asmRefs[0].Name, reader.GetAssemblyReference((AssemblyReferenceHandle)objectRef.ResolutionScope).Name);
        }


        [Fact]
        public void CoreAssemblyResolution_PrefersSystemPrivateCoreLib()
        {
            // When System.Private.CoreLib is referenced, it should be preferred over System.Runtime
            string source = """
                .assembly extern System.Private.CoreLib { }
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Both assemblies should be referenced
            var asmRefs = reader.AssemblyReferences.Select(reader.GetAssemblyReference)
                .Select(a => reader.GetString(a.Name)).ToArray();
            Assert.Contains("System.Private.CoreLib", asmRefs);
            Assert.Contains("System.Runtime", asmRefs);

            // Verify System.Object is referenced from System.Private.CoreLib (preferred)
            var typeRefs = reader.TypeReferences.Select(reader.GetTypeReference).ToArray();
            var objectRef = typeRefs.Single(t => reader.GetString(t.Name) == "Object");
            var resolvedAsm = reader.GetAssemblyReference((AssemblyReferenceHandle)objectRef.ResolutionScope);
            Assert.Equal("System.Private.CoreLib", reader.GetString(resolvedAsm.Name));
        }


        [Fact]
        public void CoreAssemblyResolution_FallsBackToMscorlib()
        {
            // When no core assembly is explicitly referenced, mscorlib should be created
            // for implicit base type resolution. A class with no explicit extends clause
            // implicitly extends System.Object.
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // mscorlib should be created as fallback for System.Object base type
            var asmRefs = reader.AssemblyReferences.Select(reader.GetAssemblyReference)
                .Select(a => reader.GetString(a.Name)).ToArray();
            Assert.Contains("mscorlib", asmRefs);

            // Verify System.Object is referenced from mscorlib
            var typeRefs = reader.TypeReferences.Select(reader.GetTypeReference).ToArray();
            var objectRef = typeRefs.Single(t => reader.GetString(t.Name) == "Object");
            Assert.Equal("System", reader.GetString(objectRef.Namespace));
            var resolvedAsm = reader.GetAssemblyReference((AssemblyReferenceHandle)objectRef.ResolutionScope);
            Assert.Equal("mscorlib", reader.GetString(resolvedAsm.Name));
        }


        [Fact]
        public void AssemblyNoPlatform_SetsNoPlatformFlag()
        {
            string source = """
                .assembly test
                {
                    .hash algorithm 0x00008004
                    .ver 1:0:0:0
                    .custom instance void [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = (01 00 18 2E 4E 45 54 46 72 61 6D 65 77 6F 72 6B 2C 56 65 72 73 69 6F 6E 3D 76 38 2E 30 01 00 54 0E 14 46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79 4E 61 6D 65 08 2E 4E 45 54 20 38 2E 30)
                }
                .assembly extern mscorlib
                {
                    .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
                }
                .class public auto ansi Test { }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());

            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);
        }


        [Fact]
        public void AssemblyNoPlatform_WithKeyword_SetsNoPlatformFlag()
        {
            // Test the 'noplatform' assembly attribute
            string source = """
                .assembly noplatform test
                {
                    .ver 1:0:0:0
                }
                .class public auto ansi Test { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var assembly = reader.GetAssemblyDefinition();

            // NoPlatform = 0x70 (stored in architecture bits of AssemblyFlags)
            Assert.Equal((System.Reflection.AssemblyFlags)0x70, assembly.Flags & (System.Reflection.AssemblyFlags)0xF0);
        }


        [Fact]
        public void AssemblyArchitecture_SetsArchitectureFlags()
        {
            // Test the x86 architecture assembly attribute
            string source = """
                .assembly x86 test
                {
                    .ver 1:0:0:0
                }
                .class public auto ansi Test { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var assembly = reader.GetAssemblyDefinition();

            // x86 = ProcessorArchitecture.X86 (2) << 4 = 0x20
            Assert.Equal((System.Reflection.AssemblyFlags)0x20, assembly.Flags & (System.Reflection.AssemblyFlags)0xF0);
        }


        [Fact]
        public void AssemblyVersion_DefaultsToZero_WhenNoVerDirective()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var asmDef = reader.GetAssemblyDefinition();
            Assert.Equal(new Version(0, 0, 0, 0), asmDef.Version);
        }


        [Fact]
        public void AssemblyRefVersion_DefaultsToZero_WhenNoVerDirective()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var asmRef = reader.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle(1));
            Assert.Equal(new Version(0, 0, 0, 0), asmRef.Version);
        }


        [Fact]
        public void AssemblyVersion_ExplicitVer_IsPreserved()
        {
            string source = """
                .assembly extern System.Runtime { .ver 8:0:0:0 }
                .assembly TestAssembly { .ver 1:2:3:4 }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var asmDef = reader.GetAssemblyDefinition();
            Assert.Equal(new Version(1, 2, 3, 4), asmDef.Version);
            var asmRef = reader.GetAssemblyReference(MetadataTokens.AssemblyReferenceHandle(1));
            Assert.Equal(new Version(8, 0, 0, 0), asmRef.Version);
        }

        [Theory]
        [InlineData("Deterministic.dll", false, false)]
        [InlineData("Deterministic.exe", true, false)]
        [InlineData("Deterministic.dll", false, true)]
        public void ManagedIlasm_DeterministicOutput_IsByteIdentical(string outputFileName, bool executable, bool debug)
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Deterministic
                {
                    .ver 1:2:3:4
                }
                .class public auto ansi beforefieldinit Program extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        ENTRYPOINT
                        ldstr "deterministic"
                        pop
                        ret
                    }
                }
                """.Replace("ENTRYPOINT", executable ? ".entrypoint" : string.Empty);

            var options = new Options
            {
                Debug = debug,
                Deterministic = true,
                OutputFileName = outputFileName,
            };

            ImmutableArray<byte> firstImage = DocumentCompilerTestHelpers.Compile(source, options);
            ImmutableArray<byte> secondImage = DocumentCompilerTestHelpers.Compile(source, options);

            Assert.Equal<byte>(firstImage, secondImage);

            using (var pe = new PEReader(firstImage))
            {
                MetadataReader reader = pe.GetMetadataReader();
                Assert.NotEqual(Guid.Empty, reader.GetGuid(reader.GetModuleDefinition().Mvid));
            }

            var loadContext = new AssemblyLoadContext(nameof(ManagedIlasm_DeterministicOutput_IsByteIdentical), isCollectible: true);
            using (var stream = new MemoryStream(firstImage.ToArray()))
            {
                Assert.Equal("Deterministic", loadContext.LoadFromStream(stream).GetName().Name);
            }
            loadContext.Unload();
        }

        [Fact]
        public void ManagedIlasm_DeterministicPdbOutput_IsByteIdentical()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Deterministic { }
                .class public auto ansi beforefieldinit Program extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        .line 1 "deterministic.il"
                        ret
                    }
                }
                """;

            var options = new Options
            {
                Deterministic = true,
                Pdb = true,
            };

            ImmutableArray<byte> firstPdb = DocumentCompilerTestHelpers.CompileAndGetEmbeddedPortablePdb(source, options);
            ImmutableArray<byte> secondPdb = DocumentCompilerTestHelpers.CompileAndGetEmbeddedPortablePdb(source, options);

            Assert.Equal<byte>(firstPdb, secondPdb);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ManagedIlasm_PdbIdGuid_IsNotEmpty(bool deterministic)
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly PdbId { }
                .class public auto ansi beforefieldinit Program extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        ret
                    }
                }
                """;

            var options = new Options
            {
                Deterministic = deterministic,
                Pdb = true,
            };

            ImmutableArray<byte> pdb = DocumentCompilerTestHelpers.CompileAndGetEmbeddedPortablePdb(source, options);
            using MetadataReaderProvider pdbProvider = MetadataReaderProvider.FromPortablePdbImage(pdb);
            BlobContentId pdbId = new(pdbProvider.GetMetadataReader().DebugMetadataHeader!.Id);

            Assert.NotEqual(Guid.Empty, pdbId.Guid);
        }

        [Theory]
        [InlineData(false, Machine.I386, Characteristics.ExecutableImage | Characteristics.Bit32Machine)]
        [InlineData(true, Machine.I386, Characteristics.ExecutableImage | Characteristics.Bit32Machine | Characteristics.Dll)]
        [InlineData(false, Machine.Amd64, Characteristics.ExecutableImage | Characteristics.LargeAddressAware)]
        [InlineData(true, Machine.Amd64, Characteristics.ExecutableImage | Characteristics.LargeAddressAware | Characteristics.Dll)]
        [InlineData(false, Machine.Arm, Characteristics.ExecutableImage | Characteristics.Bit32Machine)]
        [InlineData(false, Machine.Arm64, Characteristics.ExecutableImage | Characteristics.LargeAddressAware)]
        public void OutputKindAndMachine_SetCoffCharacteristics(
            bool isDll,
            Machine machine,
            Characteristics expected)
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly CoffCharacteristics { }
                .method static int32 Main()
                {
                    ENTRYPOINT
                    ldc.i4.0
                    ret
                }
                """.Replace("ENTRYPOINT", isDll ? string.Empty : ".entrypoint");

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                source,
                new Options
                {
                    IsDll = isDll,
                    Machine = machine,
                    OutputFileName = isDll ? "test.dll" : "test.exe",
                });

            Assert.Equal(expected, pe.PEHeaders.CoffHeader.Characteristics);
            Assert.Equal(!isDll, pe.PEHeaders.IsExe);
        }


        [Fact]
        public void SqstringAssemblyName_ParsedCorrectly()
        {
            string source = """
                .assembly 'My-Assembly_123' { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            // No errors — the SQSTRING assembly name should be accepted
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void TypeRefViaSelfAssembly_ResolvesToTypeDef()
        {
            // When IL references a local type via [self-assembly]Namespace.Type,
            // the TypeRef should resolve to the local TypeDef.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void DoWork() cil managed { ret }
                }
                .class public auto ansi beforefieldinit Caller extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        call void [test]MyClass::DoWork()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The [test]MyClass TypeRef resolves to a local TypeDef, but its TypeRef row is still
            // emitted (matching native ilasm). Its ResolutionScope points at the self-AssemblyRef.
            var myClassTypeRef = reader.GetTypeReference(DocumentCompilerTestHelpers.FindTypeRef(reader, "MyClass"));
            Assert.Equal(HandleKind.AssemblyReference, myClassTypeRef.ResolutionScope.Kind);

            // The method call resolves to MethodDef (not MemberRef), and the IL operand is the
            // resolved MethodDef token rather than a TypeRef/MemberRef token.
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
            int callToken = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Main", ILOpcode.call);
            var callHandle = MetadataTokens.EntityHandle(callToken);
            Assert.Equal(HandleKind.MethodDefinition, callHandle.Kind);
            Assert.Equal("DoWork", reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)callHandle).Name));
        }


        [Fact]
        public void TypeRefViaSelfAssembly_FieldResolves()
        {
            // Field access through a self-assembly TypeRef should resolve to FieldDef.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Data extends [mscorlib]System.Object
                {
                    .field public static int32 Value
                }
                .class public auto ansi beforefieldinit Reader extends [mscorlib]System.Object
                {
                    .method public static int32 Get() cil managed
                    {
                        ldsfld int32 [test]Data::Value
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));
        }


        [Fact]
        public void TypeRefViaSelfAssembly_MemberRefThroughResolved_BecomesMethodDef()
        {
            // A method call through [self-assembly]Type::Method should resolve
            // BOTH the TypeRef to TypeDef AND the MemberRef to MethodDef.
            string source = """
                .assembly extern mscorlib { }
                .assembly myasm { }
                .class public auto ansi beforefieldinit Target extends [mscorlib]System.Object
                {
                    .method public static int32 Compute() cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                }
                .class public auto ansi beforefieldinit Caller extends [mscorlib]System.Object
                {
                    .method public static int32 Main() cil managed
                    {
                        call int32 [myasm]Target::Compute()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal(0, reader.GetTableRowCount(TableIndex.MemberRef));

            // The TypeRef row for "Target" is still emitted even though it resolves to a local
            // TypeDef, and the call IL operand is the resolved MethodDef token.
            var targetTypeRef = reader.GetTypeReference(DocumentCompilerTestHelpers.FindTypeRef(reader, "Target"));
            Assert.Equal(HandleKind.AssemblyReference, targetTypeRef.ResolutionScope.Kind);

            int callToken = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Main", ILOpcode.call);
            var callHandle = MetadataTokens.EntityHandle(callToken);
            Assert.Equal(HandleKind.MethodDefinition, callHandle.Kind);
            Assert.Equal("Compute", reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)callHandle).Name));
        }


        [Fact]
        public void StackReserve_DirectiveValueIsHonored()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .stackreserve 0x00400000
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        .entrypoint
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            Assert.Equal((ulong)0x00400000, pe.PEHeaders.PEHeader!.SizeOfStackReserve);
        }


        [Fact]
        public void StackReserve_DefaultValueUsedWhenNotSpecified()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static void Main() cil managed
                    {
                        .entrypoint
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            Assert.Equal((ulong)0x00100000, pe.PEHeaders.PEHeader!.SizeOfStackReserve);
        }


        [Fact]
        public void CoreLibRedirect_MscorlibToSystemRuntime()
        {
            // When both mscorlib and System.Runtime are declared, type references
            // through [mscorlib] should be redirected to [System.Runtime]
            string source = """
                .assembly extern mscorlib { auto }
                .assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The TypeRef for System.Object should point to System.Runtime, not mscorlib
            var typeRef = reader.TypeReferences
                .Select(h => reader.GetTypeReference(h))
                .First(t => reader.GetString(t.Name) == "Object");

            var scope = reader.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);
            Assert.Equal("System.Runtime", reader.GetString(scope.Name));
        }


        [Fact]
        public void CoreLibRedirect_OnlyCorelibPresent_KeepsMscorlib()
        {
            // When only mscorlib is declared, type references stay as [mscorlib]
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var typeRef = reader.TypeReferences
                .Select(h => reader.GetTypeReference(h))
                .First(t => reader.GetString(t.Name) == "Object");

            var scope = reader.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);
            Assert.Equal("mscorlib", reader.GetString(scope.Name));
        }


        [Fact]
        public void ModReq_WithSelfAssemblyTypeRef_PreservedAfterResolution()
        {
            // modreq referencing a type in the same assembly should still
            // produce a correct signature after TypeRef→TypeDef resolution.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyModifier extends [mscorlib]System.Object
                {
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 modreq([test]MyModifier) myField
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var sigBytes = reader.GetBlobBytes(field.Signature);

            Assert.Equal(0x06, sigBytes[0]); // FIELD header
            Assert.Equal((byte)SignatureTypeCode.RequiredModifier, sigBytes[1]); // CMOD_REQD
            // After the modifier coded index, the underlying type is int32
            Assert.Equal(0x08, sigBytes[^1]);
            // The [test]MyModifier TypeRef resolved to a local TypeDef: its coded index in the
            // signature carries the TypeDef tag (low 2 bits == 0). Its TypeRef row is still emitted.
            Assert.Equal(0, sigBytes[2] & 0x03);
            Assert.Equal(HandleKind.AssemblyReference,
                reader.GetTypeReference(DocumentCompilerTestHelpers.FindTypeRef(reader, "MyModifier")).ResolutionScope.Kind);
        }


        [Fact]
        public void MultiDimArrayParam_WithSelfAssemblyRef_PreservedAfterRewrite()
        {
            // Multi-dimensional array with a self-assembly type reference as element type.
            // Both the TypeRef resolution AND the array shape must be correct.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi sealed MyStruct extends [mscorlib]System.ValueType
                {
                    .field public int32 x
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Process(valuetype [test]MyStruct[0...,0...,0...] data) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Process");
            var sigBytes = reader.GetBlobBytes(method.Signature);

            Assert.Equal(0x00, sigBytes[0]); // DEFAULT
            Assert.Equal(0x01, sigBytes[1]); // 1 param
            Assert.Equal(0x01, sigBytes[2]); // void return
            Assert.Equal(0x14, sigBytes[3]); // ELEMENT_TYPE_ARRAY
            // Element type: VALUETYPE (0x11) + TypeDef coded index (MyStruct resolved)
            Assert.Equal(0x11, sigBytes[4]); // ELEMENT_TYPE_VALUETYPE
            // After the type token: rank = 3
            // Find the rank byte (after the compressed TypeDef coded index)
            int rankIdx = 5;
            // Skip the compressed integer (coded index for MyStruct TypeDef)
            if (sigBytes[rankIdx] < 0x80) rankIdx += 1;
            else if (sigBytes[rankIdx] < 0xC0) rankIdx += 2;
            else rankIdx += 4;
            Assert.Equal(0x03, sigBytes[rankIdx]); // rank = 3

            // The [test]MyStruct element type resolved to a local TypeDef: its coded index carries
            // the TypeDef tag (low 2 bits == 0). Its TypeRef row is still emitted.
            Assert.Equal(0, sigBytes[5] & 0x03);
            Assert.Equal(HandleKind.AssemblyReference,
                reader.GetTypeReference(DocumentCompilerTestHelpers.FindTypeRef(reader, "MyStruct")).ResolutionScope.Kind);
        }


        [Fact]
        public void CatchClause_SelfAssemblyTypeRef_ResolvesToTypeDef()
        {
            // When a catch clause references a type via [self-assembly]Type,
            // the exception handler table must contain the resolved TypeDef token,
            // not the stale PseudoHandle TypeRef token.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi MyException extends [mscorlib]System.Exception
                {
                    .method public specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Exception::.ctor()
                        ret
                    }
                }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TryCatch() cil managed
                    {
                        .try
                        {
                            leave.s DONE
                        }
                        catch [test]MyException
                        {
                            pop
                            leave.s DONE
                        }
                        DONE:
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The [test]MyException TypeRef row is still emitted, but the exception handler's
            // CatchType is the resolved TypeDef token, not the TypeRef.
            Assert.Equal(HandleKind.AssemblyReference,
                reader.GetTypeReference(DocumentCompilerTestHelpers.FindTypeRef(reader, "MyException")).ResolutionScope.Kind);

            // Verify the method has exception handlers and the catch type is a TypeDef
            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TryCatch");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var ehRegions = body.ExceptionRegions;
            Assert.True(ehRegions.Length >= 1, $"Should have at least 1 exception region, got {ehRegions.Length}");

            var catchRegion = ehRegions.First(r => r.Kind == ExceptionRegionKind.Catch);
            Assert.Equal(HandleKind.TypeDefinition, catchRegion.CatchType.Kind);
        }
    }
}
