// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.SymbolStore;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class ILGeneratorScopesAndSequencePointsTests
    {
        [Fact]
        public void SetLocalSymInfo_UsingNamespace_Validations()
        {
            ModuleBuilder mb = new PersistedAssemblyBuilder(new AssemblyName("MyAssembly2"), typeof(object).Assembly).DefineDynamicModule("MyModule2");
            TypeBuilder tb = mb.DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
            ILGenerator il = tb.DefineMethod("SumMethod", MethodAttributes.Public | MethodAttributes.Static).GetILGenerator();
            LocalBuilder local = il.DeclareLocal(typeof(int));
            Assert.Throws<ArgumentNullException>("usingNamespace", () => il.UsingNamespace(null));
            Assert.Throws<ArgumentException>("usingNamespace", () => il.UsingNamespace(string.Empty));
            Assert.Throws<ArgumentNullException>("name", () => local.SetLocalSymInfo(null));
            il.Emit(OpCodes.Ret);
            tb.CreateType();
            Assert.Throws<InvalidOperationException>(() => local.SetLocalSymInfo("myInt1")); // type created
        }

        [Fact]
        public void LocalWithoutSymInfoWillNotAddedToLocalVariablesTable()
        {
            PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(new AssemblyName("MyAssembly"), typeof(object).Assembly);
            ModuleBuilder mb = ab.DefineDynamicModule("MyModule");
            TypeBuilder tb = mb.DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
            ISymbolDocumentWriter srcDoc = mb.DefineDocument("MySourceFile.cs", SymLanguageType.CSharp);
            MethodBuilder method = tb.DefineMethod("SumMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(int)]);
            ILGenerator il = method.GetILGenerator();
            LocalBuilder local = il.DeclareLocal(typeof(int));
            local.SetLocalSymInfo("myInt1");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_0);
            LocalBuilder local2 = il.DeclareLocal(typeof(string));
            il.Emit(OpCodes.Ldstr, "MyAssembly");
            il.Emit(OpCodes.Stloc, local2);
            LocalBuilder local3 = il.DeclareLocal(typeof(int));
            local3.SetLocalSymInfo("myInt2");
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Stloc_2);
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ret);
            tb.CreateType();

            MetadataBuilder mdb = ab.GenerateMetadata(out BlobBuilder _, out BlobBuilder _, out MetadataBuilder pdbMetadata);

            BlobBuilder portablePdbBlob = new BlobBuilder();
            PortablePdbBuilder pdbBuilder = new PortablePdbBuilder(pdbMetadata, mdb.GetRowCounts(), default);
            pdbBuilder.Serialize(portablePdbBlob);
            using TempFile pdbFile = TempFile.Create();
            using var pdbFileStream = new FileStream(pdbFile.Path, FileMode.Create, FileAccess.Write);
            portablePdbBlob.WriteContentTo(pdbFileStream);
            pdbFileStream.Close();

            using var fs = new FileStream(pdbFile.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(fs);
            MetadataReader reader = provider.GetMetadataReader();
            MethodDebugInformation mdi = reader.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(method.MetadataToken));
            SequencePointCollection.Enumerator spcEnumerator = mdi.GetSequencePoints().GetEnumerator();
            Assert.False(spcEnumerator.MoveNext());

            LocalScopeHandleCollection.Enumerator localScopes = reader.GetLocalScopes(MetadataTokens.MethodDefinitionHandle(method.MetadataToken)).GetEnumerator();
            Assert.True(localScopes.MoveNext());
            LocalScope localScope = reader.GetLocalScope(localScopes.Current);
            LocalVariableHandleCollection.Enumerator localEnumerator = localScope.GetLocalVariables().GetEnumerator();
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("myInt1", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("myInt2", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.False(localEnumerator.MoveNext());
            Assert.False(localScopes.MoveNext());
        }

        [Fact]
        public void LocalsNamespacesWithinNestedScopes()
        {
            PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(new AssemblyName("MyAssembly"), typeof(object).Assembly);
            ModuleBuilder mb = ab.DefineDynamicModule("MyModule");
            TypeBuilder tb = mb.DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
            ISymbolDocumentWriter srcDoc = mb.DefineDocument("MySourceFile.cs", SymLanguageType.CSharp);
            MethodBuilder method = tb.DefineMethod("SumMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(int)]);
            ILGenerator il = method.GetILGenerator();
            LocalBuilder local = il.DeclareLocal(typeof(int));
            il.UsingNamespace("System");
            local.SetLocalSymInfo("myInt1");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_0);
            LocalBuilder local2 = il.DeclareLocal(typeof(string));
            local2.SetLocalSymInfo("myString");
            il.Emit(OpCodes.Ldstr, "MyAssembly");
            il.Emit(OpCodes.Stloc, local2);
            il.BeginScope();
            il.UsingNamespace("System.Reflection");
            LocalBuilder local3 = il.DeclareLocal(typeof(AssemblyName));
            local3.SetLocalSymInfo("myAssembly");
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Newobj, typeof(AssemblyName).GetConstructor([typeof(string)]));
            il.Emit(OpCodes.Stloc_2);
            il.BeginScope();
            LocalBuilder local4 = il.DeclareLocal(typeof(int));
            local4.SetLocalSymInfo("myInt2");
            LocalBuilder local5 = il.DeclareLocal(typeof(int));
            local5.SetLocalSymInfo("myInt3");
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Stloc_3);
            il.Emit(OpCodes.Ldc_I4_5);
            il.Emit(OpCodes.Stloc_S, 4);
            il.Emit(OpCodes.Ldloc_S, 4);
            il.Emit(OpCodes.Ldloc_3);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_0);
            il.EndScope();
            il.UsingNamespace("System.Reflection.Emit");
            il.EndScope();
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
            tb.CreateType();

            MetadataBuilder mdb = ab.GenerateMetadata(out BlobBuilder _, out BlobBuilder _, out MetadataBuilder pdbMetadata);

            BlobBuilder portablePdbBlob = new BlobBuilder();
            PortablePdbBuilder pdbBuilder = new PortablePdbBuilder(pdbMetadata, mdb.GetRowCounts(), default);
            pdbBuilder.Serialize(portablePdbBlob);
            using TempFile pdbFile = TempFile.Create();
            using var pdbFileStream = new FileStream(pdbFile.Path, FileMode.Create, FileAccess.Write);
            portablePdbBlob.WriteContentTo(pdbFileStream);
            pdbFileStream.Close();

            using var fs = new FileStream(pdbFile.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(fs);
            MetadataReader reader = provider.GetMetadataReader();
            MethodDebugInformation mdi = reader.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(method.MetadataToken));
            SequencePointCollection.Enumerator spcEnumerator = mdi.GetSequencePoints().GetEnumerator();
            Assert.False(spcEnumerator.MoveNext());

            LocalScopeHandleCollection.Enumerator localScopes = reader.GetLocalScopes(MetadataTokens.MethodDefinitionHandle(method.MetadataToken)).GetEnumerator();
            Assert.True(localScopes.MoveNext());
            LocalScope localScope = reader.GetLocalScope(localScopes.Current);
            Assert.Equal(0, localScope.StartOffset);
            Assert.Equal(35, localScope.EndOffset);
            LocalVariableHandleCollection.Enumerator localEnumerator = localScope.GetLocalVariables().GetEnumerator();
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("myInt1", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("myString", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.False(localEnumerator.MoveNext());

            ImportScope importScope = reader.GetImportScope(localScope.ImportScope);
            Assert.True(importScope.Parent.IsNil);
            ImportDefinitionCollection.Enumerator importEnumerator = importScope.GetImports().GetEnumerator();
            Assert.True(importEnumerator.MoveNext());
            ImportDefinition importDef = importEnumerator.Current;
            Assert.Equal(ImportDefinitionKind.ImportNamespace, importDef.Kind);
            BlobReader blobReader = reader.GetBlobReader(importDef.TargetNamespace);
            Assert.Equal("System", blobReader.ReadUTF8(blobReader.Length));
            Assert.False(importEnumerator.MoveNext());

            Assert.True(localScopes.MoveNext());
            LocalScope innerScope = reader.GetLocalScope(localScopes.Current);
            Assert.Equal(10, innerScope.StartOffset);
            Assert.Equal(33, innerScope.EndOffset);
            localEnumerator = innerScope.GetLocalVariables().GetEnumerator();
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("myAssembly", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.False(localEnumerator.MoveNext());

            ImportScope innerImport = reader.GetImportScope(innerScope.ImportScope);
            Assert.Equal(importScope, reader.GetImportScope(innerImport.Parent));
            importEnumerator = innerImport.GetImports().GetEnumerator();
            Assert.True(importEnumerator.MoveNext());
            importDef = importEnumerator.Current;
            Assert.Equal(ImportDefinitionKind.ImportNamespace, importDef.Kind);
            blobReader = reader.GetBlobReader(importDef.TargetNamespace);
            Assert.Equal("System.Reflection", blobReader.ReadUTF8(blobReader.Length));
            Assert.True(importEnumerator.MoveNext());
            importDef = importEnumerator.Current;
            Assert.Equal(ImportDefinitionKind.ImportNamespace, importDef.Kind);
            blobReader = reader.GetBlobReader(importDef.TargetNamespace);
            Assert.Equal("System.Reflection.Emit", blobReader.ReadUTF8(blobReader.Length));
            Assert.False(importEnumerator.MoveNext());

            Assert.True(localScopes.MoveNext());
            LocalScope innerMost = reader.GetLocalScope(localScopes.Current);
            Assert.Equal(17, innerMost.StartOffset);
            Assert.Equal(33, innerMost.EndOffset);
            localEnumerator = innerMost.GetLocalVariables().GetEnumerator();
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("myInt2", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("myInt3", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.False(localEnumerator.MoveNext());
            Assert.False(localScopes.MoveNext());

            Assert.True(innerMost.ImportScope.IsNil);
        }

        [Fact]
        public void DefineDocument_MarkSequencePoint_Validations()
        {
            ModuleBuilder runtimeModule = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("T"), AssemblyBuilderAccess.Run).DefineDynamicModule("T");
            Assert.Throws<InvalidOperationException>(() => runtimeModule.DefineDocument("MySourceFile.cs", SymLanguageType.CSharp));

            ModuleBuilder mb = new PersistedAssemblyBuilder(new AssemblyName("Assembly"), typeof(object).Assembly).DefineDynamicModule("MyModule");
            TypeBuilder tb = mb.DefineType("Type", TypeAttributes.Public | TypeAttributes.Class);
            Assert.Throws<ArgumentNullException>("url", () => mb.DefineDocument(null));
            Assert.Throws<ArgumentException>("url", () => mb.DefineDocument(string.Empty));

            ILGenerator il = tb.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static).GetILGenerator();
            Assert.Throws<ArgumentNullException>("document", () => il.MarkSequencePoint(null, 0, 0, 0, 1));
            Assert.Throws<ArgumentException>("document", () => il.MarkSequencePoint(new TestDocument(), 0, 0, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("startLine", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), -1, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("startColumn", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 1, -1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("endLine", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 1, 1, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("endColumn", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 1, 1, 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>("startLine", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 0x20000000, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("startColumn", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 1, 0x10000, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("endLine", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 1, 1, 0x20000000, 1));
            Assert.Throws<ArgumentOutOfRangeException>("endColumn", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 1, 1, 1, 0x10000));
            Assert.Throws<ArgumentOutOfRangeException>("endColumn", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 1, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("endLine", () => il.MarkSequencePoint(mb.DefineDocument("MySourceFile.cs"), 1, 1, 0, 1));
        }

        [Fact]
        public void MultipleDocumentsAndSequencePoints()
        {
            PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(new AssemblyName("MyAssembly"), typeof(object).Assembly);
            ModuleBuilder mb = ab.DefineDynamicModule("MyModule");
            TypeBuilder tb = mb.DefineType("MyType", TypeAttributes.Public | TypeAttributes.Class);
            ISymbolDocumentWriter srcDoc1 = mb.DefineDocument("MySource1.cs", SymLanguageType.CSharp);
            ISymbolDocumentWriter srcDoc2 = mb.DefineDocument("MySource2.cs", SymLanguageType.CSharp);
            ISymbolDocumentWriter srcDoc3 = mb.DefineDocument("MySource3.cs", SymLanguageType.CSharp);
            MethodBuilder method = tb.DefineMethod("SumMethod", MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator il1 = method.GetILGenerator();
            LocalBuilder local = il1.DeclareLocal(typeof(int));
            local.SetLocalSymInfo("MyInt");
            il1.MarkSequencePoint(srcDoc2, 7, 0, 7, 20);
            il1.Emit(OpCodes.Ldarg_0);
            il1.Emit(OpCodes.Ldarg_1);
            il1.Emit(OpCodes.Add);
            il1.Emit(OpCodes.Stloc_0);
            il1.MarkSequencePoint(srcDoc1, 8, 0, 9, 18);
            il1.Emit(OpCodes.Ldc_I4_2);
            il1.Emit(OpCodes.Stloc_1);
            il1.MarkSequencePoint(srcDoc1, 0xfeefee, 0, 0xfeefee, 0); // hidden sequence point
            il1.Emit(OpCodes.Ldloc_0);
            il1.Emit(OpCodes.Ldloc_1);
            il1.Emit(OpCodes.Add);
            il1.Emit(OpCodes.Stloc_0);
            il1.MarkSequencePoint(srcDoc1, 11, 1, 11, 20);
            il1.Emit(OpCodes.Ldloc_0);
            il1.Emit(OpCodes.Ldloc_1);
            il1.Emit(OpCodes.Add);
            il1.Emit(OpCodes.Stloc_0);
            il1.MarkSequencePoint(srcDoc3, 5, 0, 5, 20);
            il1.Emit(OpCodes.Ldloc_0);
            il1.Emit(OpCodes.Ret);

            MethodBuilder entryPoint = tb.DefineMethod("Main", MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static);
            ILGenerator il2 = entryPoint.GetILGenerator();
            il2.Emit(OpCodes.Ldc_I4_S, 10);
            il2.Emit(OpCodes.Ldc_I4_1);
            il2.Emit(OpCodes.Call, method);
            il2.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [typeof(int)]));
            il2.Emit(OpCodes.Ret);
            tb.CreateType();

            MetadataBuilder mdb = ab.GenerateMetadata(out BlobBuilder _, out BlobBuilder _, out MetadataBuilder pdbMetadata);

            BlobBuilder portablePdbBlob = new BlobBuilder();
            PortablePdbBuilder pdbBuilder = new PortablePdbBuilder(pdbMetadata, mdb.GetRowCounts(), default);
            pdbBuilder.Serialize(portablePdbBlob);
            using TempFile pdbFile = TempFile.Create();
            using var pdbFileStream = new FileStream(pdbFile.Path, FileMode.Create, FileAccess.Write);
            portablePdbBlob.WriteContentTo(pdbFileStream);
            pdbFileStream.Close();

            using var fs = new FileStream(pdbFile.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(fs);
            MetadataReader reader = provider.GetMetadataReader();
            DocumentHandleCollection.Enumerator docEnumerator = reader.Documents.GetEnumerator();
            Assert.Equal(3, reader.Documents.Count);
            Assert.True(docEnumerator.MoveNext());
            Document doc1 = reader.GetDocument(docEnumerator.Current);
            Assert.Equal("MySource2.cs", reader.GetString(doc1.Name));
            Assert.Equal(SymLanguageType.CSharp, reader.GetGuid(doc1.Language));
            Assert.True(docEnumerator.MoveNext());
            Document doc2 = reader.GetDocument(docEnumerator.Current);
            Assert.Equal("MySource1.cs", reader.GetString(doc2.Name));
            Assert.Equal(SymLanguageType.CSharp, reader.GetGuid(doc2.Language));
            Assert.True(docEnumerator.MoveNext());
            Document doc3 = reader.GetDocument(docEnumerator.Current);
            Assert.Equal("MySource3.cs", reader.GetString(doc3.Name));
            Assert.Equal(SymLanguageType.CSharp, reader.GetGuid(doc3.Language));
            Assert.False(docEnumerator.MoveNext());

            MethodDebugInformation mdi1 = reader.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(method.MetadataToken));
            Assert.True(mdi1.Document.IsNil);
            SequencePointCollection.Enumerator spcEnumerator = mdi1.GetSequencePoints().GetEnumerator();
            Assert.True(spcEnumerator.MoveNext());
            SequencePoint sp = spcEnumerator.Current;
            Assert.Equal(doc1, reader.GetDocument(sp.Document));
            Assert.Equal(7, sp.StartLine);
            Assert.False(sp.IsHidden);
            Assert.Equal(0, sp.Offset);
            Assert.Equal(0, sp.StartColumn);
            Assert.Equal(7, sp.EndLine);
            Assert.Equal(20, sp.EndColumn);
            Assert.True(spcEnumerator.MoveNext());
            sp = spcEnumerator.Current;
            Assert.Equal(doc2, reader.GetDocument(sp.Document));
            Assert.Equal(4, sp.Offset);
            Assert.Equal(8, sp.StartLine);
            Assert.Equal(0, sp.StartColumn);
            Assert.Equal(9, sp.EndLine);
            Assert.Equal(18, sp.EndColumn);       
            Assert.True(spcEnumerator.MoveNext());
            sp = spcEnumerator.Current;
            Assert.Equal(doc2, reader.GetDocument(sp.Document));
            Assert.True(sp.IsHidden);
            Assert.Equal(6, sp.Offset);
            Assert.True(spcEnumerator.MoveNext());
            sp = spcEnumerator.Current;
            Assert.Equal(doc2, reader.GetDocument(sp.Document));
            Assert.Equal(10, sp.Offset);
            Assert.Equal(11, sp.StartLine);
            Assert.Equal(1, sp.StartColumn);
            Assert.Equal(11, sp.EndLine);
            Assert.Equal(20, sp.EndColumn);
            Assert.True(spcEnumerator.MoveNext());
            sp = spcEnumerator.Current;
            Assert.Equal(doc3, reader.GetDocument(sp.Document));
            Assert.Equal(24, sp.Offset);
            Assert.Equal(5, sp.StartLine);
            Assert.Equal(0, sp.StartColumn);
            Assert.Equal(5, sp.EndLine);
            Assert.Equal(20, sp.EndColumn);
            Assert.False(spcEnumerator.MoveNext());

            LocalScopeHandleCollection.Enumerator localScopes = reader.GetLocalScopes(MetadataTokens.MethodDefinitionHandle(method.MetadataToken)).GetEnumerator();
            Assert.True(localScopes.MoveNext());
            LocalScope locals = reader.GetLocalScope(localScopes.Current);
            Assert.Equal(0, locals.StartOffset);
            Assert.Equal(16, locals.EndOffset);
            LocalVariableHandleCollection.Enumerator localEnumerator = locals.GetLocalVariables().GetEnumerator();
            Assert.True(localEnumerator.MoveNext());
            Assert.Equal("MyInt", reader.GetString(reader.GetLocalVariable(localEnumerator.Current).Name));
            Assert.False(localEnumerator.MoveNext());
            Assert.False(localScopes.MoveNext());
        }

        private class TestDocument : ISymbolDocumentWriter
        {
            public void SetCheckSum(Guid algorithmId, byte[] checkSum) => throw new NotImplementedException();
            public void SetSource(byte[] source) => throw new NotImplementedException();
        }
    }
}
