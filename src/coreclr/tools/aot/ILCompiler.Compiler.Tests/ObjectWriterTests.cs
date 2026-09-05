// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter;

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class ObjectWriterTests
    {
        [Fact]
        public void SectionDataSupportsEmptyData()
        {
            var sectionData = new SectionData();
            sectionData.AppendData(ReadOnlyMemory<byte>.Empty);

            Assert.Equal(0, sectionData.Length);
            Assert.Empty(ReadAll(sectionData));
        }

        [Fact]
        public void SectionDataTransitionsFromInlineToOverflowWithoutCopying()
        {
            byte[] first = [1, 2];
            byte[] second = [3];
            byte[] third = [4, 5];
            var sectionData = new SectionData();

            sectionData.AppendData(first);
            sectionData.AppendData(second);
            sectionData.AppendData(third);

            first[0] = 9;
            second[0] = 8;
            third[1] = 7;

            Assert.Equal([9, 2, 8, 4, 7], ReadAll(sectionData));

            using Stream stream = sectionData.GetReadStream();
            stream.Position = 1;
            Span<byte> remaining = stackalloc byte[4];
            Assert.Equal(4, stream.Read(remaining));
            Assert.Equal([2, 8, 4, 7], remaining.ToArray());
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)0x5A)]
        [InlineData((byte)0x90)]
        public void SectionDataPreservesBufferedWritesAndPadding(byte paddingByte)
        {
            var sectionData = new SectionData(paddingByte);

            sectionData.AppendPadding(3);
            Write(sectionData.BufferWriter, [1, 2]);
            sectionData.AppendPadding(2);
            sectionData.AppendData(new byte[] { 3 });
            sectionData.AppendPadding(20);
            sectionData.AppendData(new byte[] { 4 });

            byte[] expected = new byte[29];
            expected.AsSpan(0, 3).Fill(paddingByte);
            expected[3] = 1;
            expected[4] = 2;
            expected.AsSpan(5, 2).Fill(paddingByte);
            expected[7] = 3;
            expected.AsSpan(8, 20).Fill(paddingByte);
            expected[28] = 4;

            Assert.Equal(expected.Length, sectionData.Length);
            Assert.Equal(expected, ReadAll(sectionData));
        }

        [Fact]
        public void CoffObjectWriterPreservesSectionAndRelocationSemantics()
        {
            NodeFactory factory = CreateNodeFactory();
            byte[] objectBytes = EmitObject(factory, out int allocatedRelocationLists);
            using var objectStream = new MemoryStream(objectBytes);
            var headers = new PEHeaders(objectStream);

            Assert.True(headers.IsCoffOnly);
            Assert.Equal(2, allocatedRelocationLists);

            SectionHeader empty = GetSection(headers, ".empty");
            Assert.Equal(0, empty.SizeOfRawData);
            Assert.Equal(0, empty.PointerToRelocations);
            Assert.Equal(0, empty.NumberOfRelocations);

            SectionHeader aligned = GetSection(headers, ".align");
            Assert.Equal([1, 2, 3, 0, 0, 0, 0, 0, 4, 5], GetSectionData(objectBytes, aligned).ToArray());

            SectionHeader code = GetSection(headers, ".code");
            Assert.Equal([0xCC, 0xCC, 0xCC, 0x90, 0x90, 0x90, 0x90, 0x90, 0xC3], GetSectionData(objectBytes, code).ToArray());

            SectionHeader single = GetSection(headers, ".single");
            Assert.Equal(1, single.NumberOfRelocations);
            Assert.Equal(7u, BinaryPrimitives.ReadUInt32LittleEndian(GetSectionData(objectBytes, single)));
            AssertRelocation(objectBytes, headers, single, 0, 0, "targetA");

            SectionHeader multiple = GetSection(headers, ".multi");
            Assert.Equal(2, multiple.NumberOfRelocations);
            ReadOnlySpan<byte> multipleData = GetSectionData(objectBytes, multiple);
            Assert.Equal(7u, BinaryPrimitives.ReadUInt32LittleEndian(multipleData));
            Assert.Equal(11u, BinaryPrimitives.ReadUInt32LittleEndian(multipleData.Slice(4)));
            AssertRelocation(objectBytes, headers, multiple, 0, 4, "targetB");
            AssertRelocation(objectBytes, headers, multiple, 1, 0, "targetA");

            Assert.True(GetSectionIndex(headers, ".empty") < GetSectionIndex(headers, ".align"));
            Assert.True(GetSectionIndex(headers, ".align") < GetSectionIndex(headers, ".single"));
            Assert.True(GetSectionIndex(headers, ".single") < GetSectionIndex(headers, ".multi"));
        }

        [Fact]
        public void CoffObjectWriterIsDeterministic()
        {
            NodeFactory factory = CreateNodeFactory();

            Assert.Equal(EmitObject(factory, out _), EmitObject(factory, out _));
        }

        private static NodeFactory CreateNodeFactory()
        {
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.NativeAot);
            var context = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All)
            {
                InputFilePaths = new Dictionary<string, string>
                {
                    { "Test.CoreLib", @"Test.CoreLib.dll" },
                },
                ReferenceFilePaths = new Dictionary<string, string>(),
            };
            context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));

            var builder = new RyuJitCompilationBuilder(context, new SingleFileCompilationModuleGroup());
            IILScanner scanner = builder.GetILScannerBuilder().ToILScanner();
            NodeFactory factory = ((Compilation)scanner).NodeFactory;
            factory.SetMarkingComplete();
            return factory;
        }

        private static byte[] EmitObject(NodeFactory factory, out int allocatedRelocationLists)
        {
            TestObjectNode targetA = new("targetA", new ObjectNodeSection("ta", SectionType.ReadOnly), [0xAA], referenceOffset: 7);
            TestObjectNode targetB = new("targetB", new ObjectNodeSection("tb", SectionType.ReadOnly), [0xBB], referenceOffset: 11);
            ObjectNodeSection alignedSection = new("align", SectionType.ReadOnly);
            ObjectNodeSection codeSection = new("code", SectionType.Executable);

            DependencyNodeCore<NodeFactory>[] nodes =
            [
                new TestObjectNode("empty", new ObjectNodeSection("empty", SectionType.ReadOnly), []),
                new TestObjectNode("align1", alignedSection, [1, 2, 3]),
                new TestObjectNode("align2", alignedSection, [4, 5], alignment: 8),
                new TestObjectNode("code1", codeSection, [0xCC, 0xCC, 0xCC]),
                new TestObjectNode("code2", codeSection, [0xC3], alignment: 8),
                new TestObjectNode(
                    "single",
                    new ObjectNodeSection("single", SectionType.ReadOnly),
                    new byte[4],
                    [new Relocation(RelocType.IMAGE_REL_BASED_ADDR32NB, 0, targetA)],
                    alignment: 4),
                new TestObjectNode(
                    "multiple",
                    new ObjectNodeSection("multi", SectionType.ReadOnly),
                    new byte[8],
                    [
                        new Relocation(RelocType.IMAGE_REL_BASED_ADDR32NB, 4, targetB),
                        new Relocation(RelocType.IMAGE_REL_BASED_ADDR32NB, 0, targetA),
                    ],
                    alignment: 4),
                targetA,
                targetB,
            ];

            var writer = new InspectableCoffObjectWriter(factory);
            using var output = new MemoryStream();
            writer.EmitObject(output, nodes, dumper: null, Logger.Null);
            allocatedRelocationLists = writer.AllocatedRelocationListCount;
            return output.ToArray();
        }

        private static byte[] ReadAll(SectionData sectionData)
        {
            using Stream stream = sectionData.GetReadStream();
            using var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }

        private static void Write(IBufferWriter<byte> writer, ReadOnlySpan<byte> data)
        {
            data.CopyTo(writer.GetSpan(data.Length));
            writer.Advance(data.Length);
        }

        private static SectionHeader GetSection(PEHeaders headers, string name)
        {
            foreach (SectionHeader section in headers.SectionHeaders)
            {
                if (section.Name == name)
                {
                    return section;
                }
            }

            throw new InvalidOperationException($"Section '{name}' was not found.");
        }

        private static int GetSectionIndex(PEHeaders headers, string name)
        {
            for (int i = 0; i < headers.SectionHeaders.Length; i++)
            {
                if (headers.SectionHeaders[i].Name == name)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"Section '{name}' was not found.");
        }

        private static ReadOnlySpan<byte> GetSectionData(byte[] objectBytes, SectionHeader section)
        {
            return objectBytes.AsSpan(section.PointerToRawData, section.SizeOfRawData);
        }

        private static void AssertRelocation(
            byte[] objectBytes,
            PEHeaders headers,
            SectionHeader section,
            int relocationIndex,
            uint expectedOffset,
            string expectedSymbol)
        {
            const int CoffRelocationSize = 10;
            const ushort ImageRelAmd64Addr32Nb = 3;

            int relocationOffset = section.PointerToRelocations + relocationIndex * CoffRelocationSize;
            ReadOnlySpan<byte> relocation = objectBytes.AsSpan(relocationOffset, CoffRelocationSize);
            Assert.Equal(expectedOffset, BinaryPrimitives.ReadUInt32LittleEndian(relocation));
            uint symbolIndex = BinaryPrimitives.ReadUInt32LittleEndian(relocation.Slice(4));
            Assert.Equal(ImageRelAmd64Addr32Nb, BinaryPrimitives.ReadUInt16LittleEndian(relocation.Slice(8)));
            Assert.Equal(expectedSymbol, GetSymbolName(objectBytes, headers.CoffHeader.PointerToSymbolTable, symbolIndex));
        }

        private static string GetSymbolName(byte[] objectBytes, int symbolTableOffset, uint symbolIndex)
        {
            const int CoffSymbolSize = 18;
            ReadOnlySpan<byte> name = objectBytes.AsSpan(symbolTableOffset + checked((int)symbolIndex) * CoffSymbolSize, 8);
            int length = name.IndexOf((byte)0);
            if (length < 0)
            {
                length = name.Length;
            }

            return Encoding.UTF8.GetString(name.Slice(0, length));
        }

        private sealed class InspectableCoffObjectWriter : CoffObjectWriter
        {
            public InspectableCoffObjectWriter(NodeFactory factory)
                : base(factory, ObjectWritingOptions.None)
            {
            }

            public int AllocatedRelocationListCount
            {
                get
                {
                    int count = 0;
                    foreach (SectionDefinition section in _sections)
                    {
                        if (section.Relocations is not null)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }
        }

        private sealed class TestObjectNode : ObjectNode, ISymbolDefinitionNode
        {
            private readonly string _name;
            private readonly ObjectNodeSection _section;
            private readonly byte[] _data;
            private readonly Relocation[] _relocations;
            private readonly int _alignment;
            private readonly int _referenceOffset;

            public TestObjectNode(
                string name,
                ObjectNodeSection section,
                byte[] data,
                Relocation[] relocations = null,
                int alignment = 1,
                int referenceOffset = 0)
            {
                _name = name;
                _section = section;
                _data = data;
                _relocations = relocations ?? Array.Empty<Relocation>();
                _alignment = alignment;
                _referenceOffset = referenceOffset;
            }

            public int Offset => 0;
            int ISymbolNode.Offset => _referenceOffset;
            public override bool IsShareable => false;
            public override int ClassCode => -1737417254;
            public override bool StaticDependenciesAreComputed => true;

            public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            {
                sb.Append(new Utf8String(_name));
            }

            public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
            {
                return new ObjectData(_data, _relocations, _alignment, [this]);
            }

            public override ObjectNodeSection GetSection(NodeFactory factory) => _section;

            public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
            {
                return StringComparer.Ordinal.Compare(_name, ((TestObjectNode)other)._name);
            }

            protected override string GetName(NodeFactory factory) => _name;
        }
    }
}
