// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.PortableExecutable.Tests
{
    public class PEBuilderTests
    {
        #region Helpers

        private void VerifyPE(Stream peStream, byte[] expectedSignature = null)
        {
            peStream.Position = 0;

            using (var peReader = new PEReader(peStream))
            {
                var headers = peReader.PEHeaders;
                var mdReader = peReader.GetMetadataReader();

                // TODO: more validation (can we use MetadataVisualizer until managed PEVerifier is available)?

                VerifyStrongNameSignatureDirectory(peReader, expectedSignature);

                Assert.Equal(s_contentId.Stamp, unchecked((uint)peReader.PEHeaders.CoffHeader.TimeDateStamp));
                Assert.Equal(s_guid, mdReader.GetGuid(mdReader.GetModuleDefinition().Mvid));
            }
        }

        private unsafe static void VerifyStrongNameSignatureDirectory(PEReader peReader, byte[] expectedSignature)
        {
            var headers = peReader.PEHeaders;
            int rva = headers.CorHeader.StrongNameSignatureDirectory.RelativeVirtualAddress;
            int size = headers.CorHeader.StrongNameSignatureDirectory.Size;

            // Even if the image is not signed we reserve space for a signature.
            // Validate that the signature is in .text section.
            Assert.Equal(".text", headers.SectionHeaders[headers.GetContainingSectionIndex(rva)].Name);

            var signature = peReader.GetSectionData(rva).GetContent(0, size);
            AssertEx.Equal(expectedSignature ?? new byte[size], signature);
        }

        private static readonly Guid s_guid = new Guid("97F4DBD4-F6D1-4FAD-91B3-1001F92068E5");
        private static readonly BlobContentId s_contentId = new BlobContentId(s_guid, 0x04030201);

        private static void WritePEImage(
            Stream peStream, 
            MetadataBuilder metadataBuilder, 
            BlobBuilder ilBuilder, 
            MethodDefinitionHandle entryPointHandle,
            Blob mvidFixup = default(Blob),
            byte[] privateKeyOpt = null)
        {
            var peBuilder = new ManagedPEBuilder(
                entryPointHandle.IsNil ? PEHeaderBuilder.CreateLibraryHeader() : PEHeaderBuilder.CreateExecutableHeader(),
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                entryPoint: entryPointHandle,
                flags: CorFlags.ILOnly | (privateKeyOpt != null ? CorFlags.StrongNameSigned : 0),
                deterministicIdProvider: content => s_contentId);

            var peBlob = new BlobBuilder();

            var contentId = peBuilder.Serialize(peBlob);

            if (!mvidFixup.IsDefault)
            {
                new BlobWriter(mvidFixup).WriteGuid(contentId.Guid);
            }

            if (privateKeyOpt != null)
            {
                peBuilder.Sign(peBlob, content => SigningUtilities.CalculateRsaSignature(content, privateKeyOpt));
            }

            peBlob.WriteContentTo(peStream);
        }

        #endregion

        [Fact]
        public void ManagedPEBuilder_Errors()
        {
            var hdr = new PEHeaderBuilder();
            var ms = new MetadataRootBuilder(new MetadataBuilder());
            var il = new BlobBuilder();

            Assert.Throws<ArgumentNullException>(() => new ManagedPEBuilder(null, ms, il));
            Assert.Throws<ArgumentNullException>(() => new ManagedPEBuilder(hdr, null, il));
            Assert.Throws<ArgumentNullException>(() => new ManagedPEBuilder(hdr, ms, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ManagedPEBuilder(hdr, ms, il, strongNameSignatureSize: -1));
        }

        [Fact]
        public void BasicValidation()
        {
            using (var peStream = new MemoryStream())
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();
                var entryPoint = BasicValidationEmit(metadataBuilder, ilBuilder);
                WritePEImage(peStream, metadataBuilder, ilBuilder, entryPoint);

                VerifyPE(peStream);
            }
        }

        [Fact]
        public void BasicValidationSigned()
        {
            using (var peStream = new MemoryStream())
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();
                var entryPoint = BasicValidationEmit(metadataBuilder, ilBuilder);
                WritePEImage(peStream, metadataBuilder, ilBuilder, entryPoint, privateKeyOpt: Misc.KeyPair);

                VerifyPE(peStream, expectedSignature: new byte[] 
                {
                    0x58, 0xD4, 0xD7, 0x88, 0x3B, 0xF9, 0x19, 0x9F, 0x3A, 0x55, 0x8F, 0x1B, 0x88, 0xBE, 0xA8, 0x42,
                    0x09, 0x2B, 0xE3, 0xB4, 0xC7, 0x09, 0xD5, 0x96, 0x35, 0x50, 0x0F, 0x3C, 0x87, 0x95, 0x6A, 0x31,
                    0xA5, 0x5C, 0xC7, 0xE1, 0x14, 0x85, 0x8E, 0x63, 0xFC, 0xCF, 0x8F, 0x2A, 0x19, 0x27, 0xD5, 0x12,
                    0x88, 0x75, 0x20, 0xBB, 0xBE, 0xD0, 0xA3, 0x04, 0x2D, 0xD3, 0x44, 0x48, 0xCC, 0xD7, 0x36, 0xBA,
                    0x06, 0x86, 0x17, 0xE9, 0x0D, 0x8C, 0x9C, 0xD6, 0xBA, 0x75, 0x9E, 0x32, 0x0D, 0xCC, 0xC2, 0x8E,
                    0x80, 0xD5, 0x81, 0x71, 0xD2, 0x4A, 0x90, 0x43, 0xA0, 0x67, 0x20, 0x39, 0x0A, 0x9F, 0x61, 0x5B,
                    0x2F, 0x9F, 0xE5, 0x70, 0x42, 0xA8, 0x86, 0x61, 0x42, 0x94, 0xBD, 0x1E, 0x76, 0xDA, 0xB0, 0xF8,
                    0xA6, 0x37, 0x71, 0xD4, 0x7F, 0x12, 0xCD, 0x39, 0x27, 0x6C, 0x4D, 0x28, 0x03, 0x7D, 0xF8, 0x89
                });
            }
        }

        private static MethodDefinitionHandle BasicValidationEmit(MetadataBuilder metadata, BlobBuilder ilBuilder)
        {
            metadata.AddModule(
                0, 
                metadata.GetOrAddString("ConsoleApplication.exe"), 
                metadata.GetOrAddGuid(s_guid),
                default(GuidHandle), 
                default(GuidHandle));

            metadata.AddAssembly(
                metadata.GetOrAddString("ConsoleApplication"),
                version: new Version(1, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: metadata.GetOrAddBlob(ImmutableArray.Create(Misc.KeyPair_PublicKey)),
                flags: AssemblyFlags.PublicKey,
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            var mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: metadata.GetOrAddBlob(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            var systemObjectTypeRef = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Object"));

            var systemConsoleTypeRefHandle = metadata.AddTypeReference(
                mscorlibAssemblyRef,
                metadata.GetOrAddString("System"),
                metadata.GetOrAddString("Console"));

            var consoleWriteLineSignature = new BlobBuilder();

            new BlobEncoder(consoleWriteLineSignature).
                MethodSignature().
                Parameters(1,
                    returnType => returnType.Void(),
                    parameters => parameters.AddParameter().Type().String());

            var consoleWriteLineMemberRef = metadata.AddMemberReference(
                systemConsoleTypeRefHandle,
                metadata.GetOrAddString("WriteLine"),
                metadata.GetOrAddBlob(consoleWriteLineSignature));

            var parameterlessCtorSignature = new BlobBuilder();

            new BlobEncoder(parameterlessCtorSignature).
                MethodSignature(isInstanceMethod: true).
                Parameters(0, returnType => returnType.Void(), parameters => { });

            var parameterlessCtorBlobIndex = metadata.GetOrAddBlob(parameterlessCtorSignature);

            var objectCtorMemberRef = metadata.AddMemberReference(
                systemObjectTypeRef,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex);

            var mainSignature = new BlobBuilder();

            new BlobEncoder(mainSignature).
                MethodSignature().
                Parameters(0, returnType => returnType.Void(), parameters => { });

            var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);

            var codeBuilder = new BlobBuilder();
            InstructionEncoder il;

            //
            // Program::.ctor
            //
            il = new InstructionEncoder(codeBuilder);

            // ldarg.0
            il.LoadArgument(0);

            // call instance void [mscorlib]System.Object::.ctor()
            il.Call(objectCtorMemberRef);

            // ret
            il.OpCode(ILOpCode.Ret);

            int ctorBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();

            //
            // Program::Main
            //
            var flowBuilder = new ControlFlowBuilder();
            il = new InstructionEncoder(codeBuilder, flowBuilder);

            var tryStart = il.DefineLabel();
            var tryEnd = il.DefineLabel();
            var finallyStart = il.DefineLabel();
            var finallyEnd = il.DefineLabel();
            flowBuilder.AddFinallyRegion(tryStart, tryEnd, finallyStart, finallyEnd);

            // .try
            il.MarkLabel(tryStart);

            //   ldstr "hello"
            il.LoadString(metadata.GetOrAddUserString("hello"));

            //   call void [mscorlib]System.Console::WriteLine(string)
            il.Call(consoleWriteLineMemberRef);

            //   leave.s END
            il.Branch(ILOpCode.Leave_s, finallyEnd);
            il.MarkLabel(tryEnd);

            // .finally
            il.MarkLabel(finallyStart);

            //   ldstr "world"
            il.LoadString(metadata.GetOrAddUserString("world"));

            //   call void [mscorlib]System.Console::WriteLine(string)
            il.Call(consoleWriteLineMemberRef);

            // .endfinally
            il.OpCode(ILOpCode.Endfinally);
            il.MarkLabel(finallyEnd);

            // ret
            il.OpCode(ILOpCode.Ret);

            int mainBodyOffset = methodBodyStream.AddMethodBody(il);
            codeBuilder.Clear();
            flowBuilder.Clear();

            var mainMethodDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetOrAddString("Main"),
                metadata.GetOrAddBlob(mainSignature),
                mainBodyOffset,
                parameterList: default(ParameterHandle));

            var ctorDef = metadata.AddMethodDefinition(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                metadata.GetOrAddString(".ctor"),
                parameterlessCtorBlobIndex,
                ctorBodyOffset,
                parameterList: default(ParameterHandle));

            metadata.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadata.GetOrAddString("<Module>"),
                baseType: default(EntityHandle),
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mainMethodDef);

            metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetOrAddString("ConsoleApplication"),
                metadata.GetOrAddString("Program"),
                systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: mainMethodDef);
           
            return mainMethodDef;
        }

        [Fact]
        public void Complex()
        {
            using (var peStream = new MemoryStream())
            {
                var ilBuilder = new BlobBuilder();
                var metadataBuilder = new MetadataBuilder();
                Blob mvidFixup;
                var entryPoint = ComplexEmit(metadataBuilder, ilBuilder, out mvidFixup);

                WritePEImage(peStream, metadataBuilder, ilBuilder, entryPoint, mvidFixup);
                VerifyPE(peStream);
            }
        }

        private static BlobBuilder BuildSignature(Action<BlobEncoder> action)
        {
            var builder = new BlobBuilder();
            action(new BlobEncoder(builder));
            return builder;
        }

        private static MethodDefinitionHandle ComplexEmit(MetadataBuilder metadata, BlobBuilder ilBuilder, out Blob mvidFixup)
        {
            var mvid = metadata.ReserveGuid();
            mvidFixup = mvid.Content;

            metadata.AddModule(
                0,
                metadata.GetOrAddString("ConsoleApplication.exe"),
                mvid.Handle,
                default(GuidHandle),
                default(GuidHandle));

            metadata.AddAssembly(
                metadata.GetOrAddString("ConsoleApplication"),
                version: new Version(0, 0, 0, 0),
                culture: default(StringHandle),
                publicKey: default(BlobHandle),
                flags: default(AssemblyFlags),
                hashAlgorithm: AssemblyHashAlgorithm.Sha1);

            var mscorlibAssemblyRef = metadata.AddAssemblyReference(
                name: metadata.GetOrAddString("mscorlib"),
                version: new Version(4, 0, 0, 0),
                culture: default(StringHandle),
                publicKeyOrToken: metadata.GetOrAddBlob(ImmutableArray.Create<byte>(0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89)),
                flags: default(AssemblyFlags),
                hashValue: default(BlobHandle));

            // TypeRefs:

            var systemObjectTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("Object"));
            var dictionaryTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System.Collections.Generic"), metadata.GetOrAddString("Dictionary`2"));
            var strignBuilderTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System.Text"), metadata.GetOrAddString("StringBuilder"));
            var typeTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("Type"));
            var int32TypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("Int32"));
            var runtimeTypeHandleRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("RuntimeTypeHandle"));
            var invalidOperationExceptionTypeRef = metadata.AddTypeReference(mscorlibAssemblyRef, metadata.GetOrAddString("System"), metadata.GetOrAddString("InvalidOperationException"));

            // TypeDefs:

            metadata.AddTypeDefinition(
               default(TypeAttributes),
               default(StringHandle),
               metadata.GetOrAddString("<Module>"),
               baseType: default(EntityHandle),
               fieldList: MetadataTokens.FieldDefinitionHandle(1),
               methodList: MetadataTokens.MethodDefinitionHandle(1));

            var baseClassTypeDef = metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit | TypeAttributes.Abstract,
                metadata.GetOrAddString("Lib"),
                metadata.GetOrAddString("BaseClass"),
                systemObjectTypeRef,
                fieldList: MetadataTokens.FieldDefinitionHandle(1),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            var derivedClassTypeDef = metadata.AddTypeDefinition(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.BeforeFieldInit,
                metadata.GetOrAddString("Lib"),
                metadata.GetOrAddString("DerivedClass"),
                baseClassTypeDef,
                fieldList: MetadataTokens.FieldDefinitionHandle(4),
                methodList: MetadataTokens.MethodDefinitionHandle(1));

            // FieldDefs:

            // Field1
            var baseClassNumberFieldDef = metadata.AddFieldDefinition(
                FieldAttributes.Private,
                metadata.GetOrAddString("_number"),
                metadata.GetOrAddBlob(BuildSignature(e => e.FieldSignature().Int32())));

            // Field2
            var baseClassNegativeFieldDef = metadata.AddFieldDefinition(
                FieldAttributes.Assembly,
                metadata.GetOrAddString("negative"),
                metadata.GetOrAddBlob(BuildSignature(e => e.FieldSignature().Boolean())));

            // Field3
            var derivedClassSumCacheFieldDef = metadata.AddFieldDefinition(
                FieldAttributes.Assembly,
                metadata.GetOrAddString("_sumCache"),
                metadata.GetOrAddBlob(BuildSignature(e => 
                {
                    var inst = e.FieldSignature().GenericInstantiation(genericType: dictionaryTypeRef, genericArgumentCount: 2, isValueType: false);
                    inst.AddArgument().Int32();
                    inst.AddArgument().Object();
                })));

            // Field4
            var derivedClassCountFieldDef = metadata.AddFieldDefinition(
                FieldAttributes.Assembly,
                metadata.GetOrAddString("_count"),
                metadata.GetOrAddBlob(BuildSignature(e => e.FieldSignature().SZArray().Int32())));

            // Field5
            var derivedClassBCFieldDef = metadata.AddFieldDefinition(
              FieldAttributes.Assembly,
              metadata.GetOrAddString("_bc"),
              metadata.GetOrAddBlob(BuildSignature(e => e.FieldSignature().Type(type: baseClassTypeDef, isValueType: false))));

            var methodBodyStream = new MethodBodyStreamEncoder(ilBuilder);

            var buffer = new BlobBuilder();
            var il = new InstructionEncoder(buffer);

            //
            // Foo
            //
            il.LoadString(metadata.GetOrAddUserString("asdsad"));
            il.OpCode(ILOpCode.Newobj);
            il.Token(invalidOperationExceptionTypeRef);
            il.OpCode(ILOpCode.Throw);

            int fooBodyOffset = methodBodyStream.AddMethodBody(il);

            // Method1
            var derivedClassFooMethodDef = metadata.AddMethodDefinition(
                MethodAttributes.PrivateScope | MethodAttributes.Private | MethodAttributes.HideBySig,
                MethodImplAttributes.IL,
                metadata.GetOrAddString("Foo"),
                metadata.GetOrAddBlob(BuildSignature(e =>
                    e.MethodSignature(isInstanceMethod: true).Parameters(0, returnType => returnType.Void(), parameters => { }))),
                fooBodyOffset,
                default(ParameterHandle));

            return default(MethodDefinitionHandle);
        }

        private class TestResourceSectionBuilder : ResourceSectionBuilder
        {
            public TestResourceSectionBuilder()
            {
            }

            internal protected override void Serialize(BlobBuilder builder, SectionLocation location)
            {
                builder.WriteInt32(0x12345678);
                builder.WriteInt32(location.PointerToRawData);
                builder.WriteInt32(location.RelativeVirtualAddress);
            }
        }

        [Fact]
        public unsafe void NativeResources()
        {
            var peStream = new MemoryStream();
            var ilBuilder = new BlobBuilder();
            var metadataBuilder = new MetadataBuilder();
            
            var peBuilder = new ManagedPEBuilder(
                PEHeaderBuilder.CreateLibraryHeader(),
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                nativeResources: new TestResourceSectionBuilder(),
                deterministicIdProvider: content => s_contentId);
            
            var peBlob = new BlobBuilder();
            
            var contentId = peBuilder.Serialize(peBlob);
            
            peBlob.WriteContentTo(peStream);

            peStream.Position = 0;
            var peReader = new PEReader(peStream);
            var sectionHeader = peReader.PEHeaders.SectionHeaders.Single(s => s.Name == ".rsrc");

            var image = peReader.GetEntireImage();

            var reader = new BlobReader(image.Pointer + sectionHeader.PointerToRawData, sectionHeader.SizeOfRawData);
            Assert.Equal(0x12345678, reader.ReadInt32());
            Assert.Equal(sectionHeader.PointerToRawData, reader.ReadInt32());
            Assert.Equal(sectionHeader.VirtualAddress, reader.ReadInt32());
        }

        private class BadResourceSectionBuilder : ResourceSectionBuilder
        {
            public BadResourceSectionBuilder()
            {
            }

            internal protected override void Serialize(BlobBuilder builder, SectionLocation location)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public unsafe void NativeResources_BadImpl()
        {
            var peStream = new MemoryStream();
            var ilBuilder = new BlobBuilder();
            var metadataBuilder = new MetadataBuilder();

            var peBuilder = new ManagedPEBuilder(
                PEHeaderBuilder.CreateLibraryHeader(),
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                nativeResources: new BadResourceSectionBuilder(),
                deterministicIdProvider: content => s_contentId);

            var peBlob = new BlobBuilder();

            Assert.Throws<NotImplementedException>(() => peBuilder.Serialize(peBlob));
        }
    }
}
