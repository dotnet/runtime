// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class PortablePdbStandalonePdbTest
    {
        [Fact]
        public void CreateStandalonePDBAndVerifyTest()
        {
            using (TempFile pdbFile = TempFile.Create())
            using (TempFile file = TempFile.Create())
            {
                MethodBuilder method1, entryPoint;
                MetadataBuilder metadataBuilder = GenerateAssemblyAndMetadata(out method1, out entryPoint, out BlobBuilder ilStream, out MetadataBuilder pdbMetadata);
                MethodDefinitionHandle entryPointHandle = MetadataTokens.MethodDefinitionHandle(entryPoint.MetadataToken);

                BlobBuilder portablePdbBlob = new BlobBuilder();
                PortablePdbBuilder pdbBuilder = new PortablePdbBuilder(pdbMetadata, metadataBuilder.GetRowCounts(), entryPointHandle);
                BlobContentId pdbContentId = pdbBuilder.Serialize(portablePdbBlob);
                using var pdbFileStream = new FileStream(pdbFile.Path, FileMode.Create, FileAccess.Write);
                portablePdbBlob.WriteContentTo(pdbFileStream);
                pdbFileStream.Close();

                DebugDirectoryBuilder debugDirectoryBuilder = new DebugDirectoryBuilder();
                debugDirectoryBuilder.AddCodeViewEntry(pdbFile.Path, pdbContentId, pdbBuilder.FormatVersion);

                ManagedPEBuilder peBuilder = new ManagedPEBuilder(
                                header: new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage),
                                metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                                ilStream: ilStream,
                                debugDirectoryBuilder: debugDirectoryBuilder,
                                entryPoint: entryPointHandle);

                BlobBuilder peBlob = new BlobBuilder();
                peBuilder.Serialize(peBlob);
                using var assemblyFileStream = new FileStream(file.Path, FileMode.Create, FileAccess.Write);
                peBlob.WriteContentTo(assemblyFileStream);
                assemblyFileStream.Close();

                using var fs = new FileStream(pdbFile.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(fs);
                ValidatePDB(method1, entryPoint, provider.GetMetadataReader());
            }
        }

        private static void ValidatePDB(MethodBuilder method, MethodBuilder entryPoint, MetadataReader reader)
        {
            DocumentHandleCollection.Enumerator docEnumerator = reader.Documents.GetEnumerator();
            Assert.Equal(1, reader.Documents.Count);
            Assert.True(docEnumerator.MoveNext());
            Document doc = reader.GetDocument(docEnumerator.Current);
            Assert.Equal("MySourceFile.cs", reader.GetString(doc.Name));
            Assert.Equal(SymLanguageType.CSharp, reader.GetGuid(doc.Language));
            Assert.Equal(default, reader.GetGuid(doc.HashAlgorithm));
            Assert.False(docEnumerator.MoveNext());

            MethodDebugInformation mdi1 = reader.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(method.MetadataToken));
            Assert.Equal(doc, reader.GetDocument(mdi1.Document));
            SequencePointCollection.Enumerator spce = mdi1.GetSequencePoints().GetEnumerator();
            Assert.True(spce.MoveNext());
            SequencePoint sp = spce.Current;
            Assert.False(sp.IsHidden);
            Assert.Equal(7, sp.StartLine);
            Assert.Equal(0, sp.StartColumn);
            Assert.Equal(7, sp.EndLine);
            Assert.Equal(20, sp.EndColumn);
            Assert.True(spce.MoveNext());
            sp = spce.Current;
            Assert.False(sp.IsHidden);
            Assert.Equal(8, sp.StartLine);
            Assert.Equal(0, sp.StartColumn);
            Assert.Equal(9, sp.EndLine);
            Assert.Equal(18, sp.EndColumn);
            Assert.False(spce.MoveNext());

            LocalScopeHandleCollection.Enumerator localScopes = reader.GetLocalScopes(MetadataTokens.MethodDefinitionHandle(method.MetadataToken)).GetEnumerator();
            Assert.True(localScopes.MoveNext());
            LocalScope localScope = reader.GetLocalScope(localScopes.Current);
            Assert.Equal(0, localScope.StartOffset);
            Assert.Equal(12, localScope.EndOffset);
            LocalVariableHandleCollection.Enumerator localEnumerator = localScope.GetLocalVariables().GetEnumerator();
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("MyInt", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.False(localEnumerator.MoveNext());

            Assert.True(localScope.ImportScope.IsNil);

            Assert.True(localScopes.MoveNext());
            localScope = reader.GetLocalScope(localScopes.Current);
            Assert.Equal(4, localScope.StartOffset);
            Assert.Equal(10, localScope.EndOffset);
            localEnumerator = localScope.GetLocalVariables().GetEnumerator();
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("MyInt2", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.False(localEnumerator.MoveNext());
            Assert.False(localScopes.MoveNext());

            ImportScope importScope = reader.GetImportScope(localScope.ImportScope);
            Assert.True(importScope.Parent.IsNil);
            ImportDefinitionCollection.Enumerator importEnumerator = importScope.GetImports().GetEnumerator();
            Assert.True(importEnumerator.MoveNext());
            ImportDefinition importDef = importEnumerator.Current;
            Assert.Equal(ImportDefinitionKind.ImportNamespace, importDef.Kind);
            BlobReader blobReader = reader.GetBlobReader(importDef.TargetNamespace);
            Assert.Equal("System.Reflection", blobReader.ReadUTF8(blobReader.Length));

            mdi1 = reader.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(entryPoint.MetadataToken));
            Assert.Equal(doc, reader.GetDocument(mdi1.Document));
            spce = mdi1.GetSequencePoints().GetEnumerator();
            Assert.True(spce.MoveNext());
            sp = spce.Current;
            Assert.False(sp.IsHidden);
            Assert.Equal(12, sp.StartLine);
            Assert.Equal(0, sp.StartColumn);
            Assert.Equal(12, sp.EndLine);
            Assert.Equal(37, sp.EndColumn);
            Assert.False(spce.MoveNext());

            localScopes = reader.GetLocalScopes(MetadataTokens.MethodDefinitionHandle(entryPoint.MetadataToken)).GetEnumerator();
            Assert.True(localScopes.MoveNext());
            localScope = reader.GetLocalScope(localScopes.Current);
            Assert.Equal(0, localScope.StartOffset);
            Assert.Equal(21, localScope.EndOffset);
            localEnumerator = localScope.GetLocalVariables().GetEnumerator();
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("MyLoc1", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("MyLoc2", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.False(localEnumerator.MoveNext());
            Assert.False(localScopes.MoveNext());
        }

        private static MetadataBuilder GenerateAssemblyAndMetadata(out MethodBuilder method,
            out MethodBuilder entryPoint, out BlobBuilder ilStream, out MetadataBuilder pdbMetadata)
        {
            PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(new AssemblyName("MyAssembly2"), typeof(object).Assembly);
            ModuleBuilder mb = ab.DefineDynamicModule("MyModule2");
            TypeBuilder tb = mb.DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
            ISymbolDocumentWriter srcdoc = mb.DefineDocument("MySourceFile.cs", SymLanguageType.CSharp);
            method = tb.DefineMethod("SumMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(int)]);
            ILGenerator il1 = method.GetILGenerator();
            LocalBuilder local = il1.DeclareLocal(typeof(int));
            local.SetLocalSymInfo("MyInt");
            il1.MarkSequencePoint(srcdoc, 7, 0, 7, 20);
            il1.Emit(OpCodes.Ldarg_0);
            il1.Emit(OpCodes.Ldarg_1);
            il1.Emit(OpCodes.Add);
            il1.Emit(OpCodes.Stloc_0);
            il1.MarkSequencePoint(srcdoc, 8, 0, 9, 18);
            il1.BeginScope();
            il1.UsingNamespace("System.Reflection");
            LocalBuilder local2 = il1.DeclareLocal(typeof(int));
            local2.SetLocalSymInfo("MyInt2");
            il1.Emit(OpCodes.Ldc_I4_2);
            il1.Emit(OpCodes.Stloc_1);
            il1.Emit(OpCodes.Ldloc_0);
            il1.Emit(OpCodes.Ldloc_1);
            il1.Emit(OpCodes.Add);
            il1.Emit(OpCodes.Stloc_0);
            il1.EndScope();
            il1.Emit(OpCodes.Ldloc_0);
            il1.Emit(OpCodes.Ret);

            entryPoint = tb.DefineMethod("Mm", MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator il2 = entryPoint.GetILGenerator();
            local = il2.DeclareLocal(typeof(int));
            local.SetLocalSymInfo("MyLoc1");
            il2.MarkSequencePoint(srcdoc, 12, 0, 12, 37);
            local2 = il2.DeclareLocal(typeof(int));
            local2.SetLocalSymInfo("MyLoc2");
            il2.Emit(OpCodes.Ldc_I4_S, 10);
            il2.Emit(OpCodes.Stloc_0);
            il2.Emit(OpCodes.Ldc_I4_1);
            il2.Emit(OpCodes.Stloc_1);
            il2.Emit(OpCodes.Ldloc_0);
            il2.Emit(OpCodes.Ldloc_1);
            il2.Emit(OpCodes.Call, method);
            il2.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [typeof(int)]));
            il2.Emit(OpCodes.Ret);
            tb.CreateType();
            return ab.GenerateMetadata(out ilStream, out BlobBuilder _, out pdbMetadata);
        }

        [Fact]
        public void CreateEmbeddedPDBAndVerifyTest()
        {
            using (TempFile file = TempFile.Create())
            {
                MethodBuilder method1, entryPoint;
                MetadataBuilder metadataBuilder = GenerateAssemblyAndMetadata(out method1, out entryPoint, out BlobBuilder ilStream, out MetadataBuilder pdbMetadata);
                MethodDefinitionHandle entryPointHandle = MetadataTokens.MethodDefinitionHandle(entryPoint.MetadataToken);

                BlobBuilder portablePdbBlob = new BlobBuilder();
                PortablePdbBuilder pdbBuilder = new PortablePdbBuilder(pdbMetadata, metadataBuilder.GetRowCounts(), entryPointHandle);

                BlobContentId pdbContentId = pdbBuilder.Serialize(portablePdbBlob);
                DebugDirectoryBuilder debugDirectoryBuilder = new DebugDirectoryBuilder();
                debugDirectoryBuilder.AddEmbeddedPortablePdbEntry(portablePdbBlob, pdbBuilder.FormatVersion);

                ManagedPEBuilder peBuilder = new ManagedPEBuilder(
                                header: new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage),
                                metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                                ilStream: ilStream,
                                debugDirectoryBuilder: debugDirectoryBuilder,
                                entryPoint: entryPointHandle);

                BlobBuilder peBlob = new BlobBuilder();
                peBuilder.Serialize(peBlob);
                using var fileStream = new FileStream(file.Path, FileMode.Create, FileAccess.Write);
                peBlob.WriteContentTo(fileStream);
                fileStream.Close();

                using var fs = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var peReader = new PEReader(fs);
                ImmutableArray<DebugDirectoryEntry> entries = peReader.ReadDebugDirectory();
                Assert.Equal(1, entries.Length);
                Assert.Equal(DebugDirectoryEntryType.EmbeddedPortablePdb, entries[0].Type);

                using MetadataReaderProvider provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entries[0]);
                ValidatePDB(method1, entryPoint, provider.GetMetadataReader());
            }
        }
    }
}
