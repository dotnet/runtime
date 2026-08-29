// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
    public class CoffObjectWriterTests
    {
        private const int CoffHeaderSize = 20;
        private const int CoffRelocationSize = 10;
        private const int CoffSectionHeaderSize = 40;
        private const int CoffSymbolSize = 18;
        private const uint ImageScnLnkNRelocOvfl = 0x01000000;

        private static readonly object s_emitLock = new object();
        private static readonly Dictionary<TargetArchitecture, NodeFactory> s_nodeFactories = new Dictionary<TargetArchitecture, NodeFactory>
        {
            { TargetArchitecture.X86, CreateNodeFactory(TargetArchitecture.X86) },
            { TargetArchitecture.X64, CreateNodeFactory(TargetArchitecture.X64) },
            { TargetArchitecture.ARM64, CreateNodeFactory(TargetArchitecture.ARM64) },
        };

        [Fact]
        public void RelocationUsesCompactValueLayout()
        {
            Assert.True(typeof(CoffObjectWriter.CoffRelocation).IsValueType);
            Assert.Equal(12, Unsafe.SizeOf<CoffObjectWriter.CoffRelocation>());
        }

        public static IEnumerable<object[]> RelocationFieldValues()
        {
            var seenValues = new HashSet<int>();
            foreach (CoffObjectWriter.CoffRelocationType type in Enum.GetValues<CoffObjectWriter.CoffRelocationType>())
            {
                int typeValue = (int)type;
                if (seenValues.Add(typeValue))
                {
                    yield return new object[] { 0u, 0u, typeValue };
                }
            }

            yield return new object[] { uint.MaxValue, uint.MaxValue, ushort.MaxValue };
        }

        [Theory]
        [MemberData(nameof(RelocationFieldValues))]
        public void RelocationWritesExpectedBytes(uint virtualAddress, uint symbolTableIndex, int typeValue)
        {
            var relocation = new CoffObjectWriter.CoffRelocation(
                virtualAddress,
                symbolTableIndex,
                (CoffObjectWriter.CoffRelocationType)typeValue);

            using var stream = new MemoryStream();
            relocation.Write(stream);

            byte[] expected = new byte[CoffRelocationSize];
            BinaryPrimitives.WriteUInt32LittleEndian(expected, virtualAddress);
            BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(4), symbolTableIndex);
            BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(8), (ushort)typeValue);
            Assert.Equal(expected, stream.ToArray());
        }

        [Fact]
        public void RelocationUsesCoffWireLayout()
        {
            var relocation = new CoffObjectWriter.CoffRelocation(
                0x11223344,
                0x55667788,
                (CoffObjectWriter.CoffRelocationType)0x99AA);

            using var stream = new MemoryStream();
            relocation.Write(stream);

            Assert.Equal(
                new byte[] { 0x44, 0x33, 0x22, 0x11, 0x88, 0x77, 0x66, 0x55, 0xAA, 0x99 },
                stream.ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void EmitsExpectedRelocationCount(int relocationCount)
        {
            Relocation[] relocations = CreateRelocations(relocationCount, RelocType.IMAGE_REL_BASED_ADDR32NB);
            byte[] objectBytes = EmitObject(new byte[sizeof(uint)], relocations);
            CoffSection section = FindSection(objectBytes, ".rdata");

            Assert.Equal(relocationCount, section.NumberOfRelocations);

            if (relocationCount == 0)
            {
                Assert.Equal(0u, section.PointerToRelocations);
            }
            else
            {
                Assert.NotEqual(0u, section.PointerToRelocations);
            }
        }

        [Fact]
        public void ConvertsRelocationsWithoutChangingOrderOrFields()
        {
            Relocation[] relocations =
            [
                CreateRelocation(0, RelocType.IMAGE_REL_BASED_ABSOLUTE, targetIndex: 0),
                CreateRelocation(4, RelocType.IMAGE_REL_BASED_ADDR32NB, targetIndex: 1),
                CreateRelocation(8, RelocType.IMAGE_REL_BASED_HIGHLOW, targetIndex: 0),
                CreateRelocation(16, RelocType.IMAGE_REL_BASED_DIR64, targetIndex: 1),
                CreateRelocation(24, RelocType.IMAGE_REL_BASED_REL32, targetIndex: 0),
                CreateRelocation(28, RelocType.IMAGE_REL_BASED_RELPTR32, targetIndex: 1),
                CreateRelocation(32, RelocType.IMAGE_REL_SECREL, targetIndex: 0),
                CreateRelocation(36, RelocType.IMAGE_REL_SECTION, targetIndex: 1),
            ];

            byte[] first = EmitObject(new byte[40], relocations, targetCount: 2, sourceSectionPrefixSize: 13);
            byte[] second = EmitObject(new byte[40], relocations, targetCount: 2, sourceSectionPrefixSize: 13);
            Assert.Equal(first, second);

            CoffSection section = FindSection(first, ".rdata");
            Assert.Equal(relocations.Length, section.NumberOfRelocations);

            uint[] expectedAddresses = [16, 20, 24, 32, 40, 44, 48, 52];
            uint[] expectedSymbolIndices = [0, 1, 0, 1, 0, 1, 0, 1];
            ushort[] expectedTypes = [3, 3, 2, 1, 4, 4, 11, 10];

            for (int i = 0; i < relocations.Length; i++)
            {
                CoffRelocation relocation = ReadRelocation(first, section, i);
                Assert.Equal(expectedAddresses[i], relocation.VirtualAddress);
                Assert.Equal(expectedSymbolIndices[i], relocation.SymbolTableIndex);
                Assert.Equal(expectedTypes[i], relocation.Type);
            }

            Assert.Equal("target0", ReadSymbolName(first, 0));
            Assert.Equal("target1", ReadSymbolName(first, 1));
            Assert.Equal("source", ReadSymbolName(first, 2));
        }

        [Fact]
        public void ConvertsArchitectureSpecificRelocationTypes()
        {
            AssertRelocationTypes(
                TargetArchitecture.X86,
                [
                    RelocType.IMAGE_REL_BASED_ABSOLUTE,
                    RelocType.IMAGE_REL_BASED_ADDR32NB,
                    RelocType.IMAGE_REL_BASED_HIGHLOW,
                    RelocType.IMAGE_REL_BASED_REL32,
                    RelocType.IMAGE_REL_BASED_RELPTR32,
                    RelocType.IMAGE_REL_SECREL,
                    RelocType.IMAGE_REL_SECTION,
                ],
                [7, 7, 6, 20, 20, 11, 10]);

            AssertRelocationTypes(
                TargetArchitecture.X64,
                [
                    RelocType.IMAGE_REL_BASED_ABSOLUTE,
                    RelocType.IMAGE_REL_BASED_ADDR32NB,
                    RelocType.IMAGE_REL_BASED_HIGHLOW,
                    RelocType.IMAGE_REL_BASED_DIR64,
                    RelocType.IMAGE_REL_BASED_REL32,
                    RelocType.IMAGE_REL_BASED_RELPTR32,
                    RelocType.IMAGE_REL_SECREL,
                    RelocType.IMAGE_REL_SECTION,
                ],
                [3, 3, 2, 1, 4, 4, 11, 10]);

            AssertRelocationTypes(
                TargetArchitecture.ARM64,
                [
                    RelocType.IMAGE_REL_BASED_ABSOLUTE,
                    RelocType.IMAGE_REL_BASED_ADDR32NB,
                    RelocType.IMAGE_REL_BASED_HIGHLOW,
                    RelocType.IMAGE_REL_BASED_DIR64,
                    RelocType.IMAGE_REL_BASED_REL32,
                    RelocType.IMAGE_REL_BASED_RELPTR32,
                    RelocType.IMAGE_REL_BASED_ARM64_BRANCH26,
                    RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21,
                    RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A,
                    RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12L,
                    RelocType.IMAGE_REL_ARM64_TLS_SECREL_HIGH12A,
                    RelocType.IMAGE_REL_ARM64_TLS_SECREL_LOW12A,
                    RelocType.IMAGE_REL_SECREL,
                    RelocType.IMAGE_REL_SECTION,
                ],
                [2, 2, 1, 14, 17, 17, 3, 4, 6, 7, 10, 9, 8, 13]);
        }

        [Fact]
        public void AppliesAddendsBeforeWritingRelocations()
        {
            byte[] data = new byte[20];
            BinaryPrimitives.WriteInt32LittleEndian(data, 10);
            BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(8), 100);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(16), -2);

            Relocation[] relocations =
            [
                CreateRelocation(0, RelocType.IMAGE_REL_BASED_ADDR32NB, targetIndex: 0, addend: 7),
                CreateRelocation(8, RelocType.IMAGE_REL_BASED_DIR64, targetIndex: 0, addend: -40),
                CreateRelocation(16, RelocType.IMAGE_REL_BASED_RELPTR32, targetIndex: 0, addend: 3),
            ];

            byte[] objectBytes = EmitObject(data, relocations);
            CoffSection section = FindSection(objectBytes, ".rdata");
            ReadOnlySpan<byte> emittedData = objectBytes.AsSpan((int)section.PointerToRawData, data.Length);

            Assert.Equal(17, BinaryPrimitives.ReadInt32LittleEndian(emittedData));
            Assert.Equal(60, BinaryPrimitives.ReadInt64LittleEndian(emittedData.Slice(8)));
            Assert.Equal(5, BinaryPrimitives.ReadInt32LittleEndian(emittedData.Slice(16)));
        }

        [Fact]
        public void EmitsOverflowRelocationRecord()
        {
            const int RelocationCount = ushort.MaxValue + 1;
            Relocation[] relocations = CreateRelocations(RelocationCount, RelocType.IMAGE_REL_BASED_ADDR32NB);

            byte[] objectBytes = EmitObject(new byte[sizeof(uint)], relocations);
            CoffSection section = FindSection(objectBytes, ".rdata");

            Assert.Equal(ushort.MaxValue, section.NumberOfRelocations);
            Assert.NotEqual(0u, section.Characteristics & ImageScnLnkNRelocOvfl);

            CoffRelocation overflow = ReadRelocation(objectBytes, section, 0);
            Assert.Equal((uint)RelocationCount + 1, overflow.VirtualAddress);
            Assert.Equal(0u, overflow.SymbolTableIndex);
            Assert.Equal(0, overflow.Type);

            CoffRelocation first = ReadRelocation(objectBytes, section, 1);
            CoffRelocation last = ReadRelocation(objectBytes, section, RelocationCount);
            Assert.Equal(0u, first.VirtualAddress);
            Assert.Equal(0u, last.VirtualAddress);
            Assert.Equal(3, first.Type);
            Assert.Equal(3, last.Type);
        }

        private static Relocation[] CreateRelocations(int count, RelocType type)
        {
            Relocation[] relocations = new Relocation[count];
            TestObjectNode target = new TestObjectNode("target0", ObjectNodeSection.DataSection, Array.Empty<byte>());
            for (int i = 0; i < relocations.Length; i++)
            {
                relocations[i] = new Relocation(type, offset: 0, target);
            }
            return relocations;
        }

        private static Relocation CreateRelocation(int offset, RelocType type, int targetIndex, int addend = 0)
        {
            var target = new TestObjectNode($"target{targetIndex}", ObjectNodeSection.DataSection, Array.Empty<byte>(), addend);
            return new Relocation(type, offset, target);
        }

        private static void AssertRelocationTypes(
            TargetArchitecture architecture,
            RelocType[] relocationTypes,
            ushort[] expectedTypes)
        {
            var relocations = new Relocation[relocationTypes.Length];
            for (int i = 0; i < relocationTypes.Length; i++)
            {
                relocations[i] = CreateRelocation(i * 8, relocationTypes[i], targetIndex: 0);
            }

            byte[] objectBytes = EmitObject(
                new byte[relocationTypes.Length * 8],
                relocations,
                architecture: architecture);
            CoffSection section = FindSection(objectBytes, ".rdata");
            Assert.Equal(relocationTypes.Length, section.NumberOfRelocations);

            for (int i = 0; i < expectedTypes.Length; i++)
            {
                Assert.Equal(expectedTypes[i], ReadRelocation(objectBytes, section, i).Type);
            }
        }

        private static byte[] EmitObject(
            byte[] data,
            Relocation[] relocations,
            int targetCount = 1,
            TargetArchitecture architecture = TargetArchitecture.X64,
            int sourceSectionPrefixSize = 0)
        {
            int sourceIndex = targetCount;
            var nodes = new DependencyNode[targetCount + 1 + (sourceSectionPrefixSize > 0 ? 1 : 0)];
            for (int i = 0; i < targetCount; i++)
            {
                nodes[i] = new TestObjectNode($"target{i}", ObjectNodeSection.DataSection, [0]);
            }

            if (sourceSectionPrefixSize > 0)
            {
                nodes[sourceIndex++] = new TestDataNode(
                    ObjectNodeSection.ReadOnlyDataSection,
                    new byte[sourceSectionPrefixSize]);
            }

            int sourceAlignment = architecture == TargetArchitecture.X86 ? 4 : 8;
            nodes[sourceIndex] = new TestObjectNode(
                "source",
                ObjectNodeSection.ReadOnlyDataSection,
                data,
                alignment: sourceAlignment,
                relocations: relocations);

            lock (s_emitLock)
            {
                var objectWriter = new CoffObjectWriter(s_nodeFactories[architecture], ObjectWritingOptions.None);
                using var stream = new MemoryStream();
                objectWriter.EmitObject(stream, nodes, dumper: null, Logger.Null);
                return stream.ToArray();
            }
        }

        private static NodeFactory CreateNodeFactory(TargetArchitecture architecture)
        {
            var target = new TargetDetails(architecture, TargetOS.Windows, TargetAbi.NativeAot);
            var context = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All)
            {
                InputFilePaths = new Dictionary<string, string>
                {
                    { "Test.CoreLib", @"Test.CoreLib.dll" },
                    { "ILCompiler.Compiler.Tests.Assets", @"ILCompiler.Compiler.Tests.Assets.dll" },
                },
                ReferenceFilePaths = new Dictionary<string, string>(),
            };

            context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));
            IILScanner scanner = new RyuJitCompilationBuilder(context, new SingleFileCompilationModuleGroup())
                .GetILScannerBuilder()
                .UseCompilationRoots(Array.Empty<ICompilationRootProvider>())
                .ToILScanner();
            NodeFactory nodeFactory = ((Compilation)scanner).NodeFactory;
            nodeFactory.SetMarkingComplete();
            return nodeFactory;
        }

        private static CoffSection FindSection(byte[] objectBytes, string name)
        {
            ushort sectionCount = BinaryPrimitives.ReadUInt16LittleEndian(objectBytes.AsSpan(2));
            for (int i = 0; i < sectionCount; i++)
            {
                int offset = CoffHeaderSize + i * CoffSectionHeaderSize;
                string sectionName = Encoding.ASCII.GetString(objectBytes, offset, 8).TrimEnd('\0');
                if (sectionName == name)
                {
                    return new CoffSection(
                        BinaryPrimitives.ReadUInt32LittleEndian(objectBytes.AsSpan(offset + 20)),
                        BinaryPrimitives.ReadUInt32LittleEndian(objectBytes.AsSpan(offset + 24)),
                        BinaryPrimitives.ReadUInt16LittleEndian(objectBytes.AsSpan(offset + 32)),
                        BinaryPrimitives.ReadUInt32LittleEndian(objectBytes.AsSpan(offset + 36)));
                }
            }

            throw new InvalidOperationException($"Section '{name}' was not found.");
        }

        private static CoffRelocation ReadRelocation(byte[] objectBytes, CoffSection section, int index)
        {
            ReadOnlySpan<byte> relocation = objectBytes.AsSpan(
                checked((int)section.PointerToRelocations + index * CoffRelocationSize),
                CoffRelocationSize);
            return new CoffRelocation(
                BinaryPrimitives.ReadUInt32LittleEndian(relocation),
                BinaryPrimitives.ReadUInt32LittleEndian(relocation.Slice(4)),
                BinaryPrimitives.ReadUInt16LittleEndian(relocation.Slice(8)));
        }

        private static string ReadSymbolName(byte[] objectBytes, int symbolIndex)
        {
            uint symbolTableOffset = BinaryPrimitives.ReadUInt32LittleEndian(objectBytes.AsSpan(8));
            int offset = checked((int)symbolTableOffset + symbolIndex * CoffSymbolSize);
            return Encoding.ASCII.GetString(objectBytes, offset, 8).TrimEnd('\0');
        }

        private readonly struct CoffSection
        {
            public CoffSection(uint pointerToRawData, uint pointerToRelocations, ushort numberOfRelocations, uint characteristics)
            {
                PointerToRawData = pointerToRawData;
                PointerToRelocations = pointerToRelocations;
                NumberOfRelocations = numberOfRelocations;
                Characteristics = characteristics;
            }

            public uint PointerToRawData { get; }
            public uint PointerToRelocations { get; }
            public ushort NumberOfRelocations { get; }
            public uint Characteristics { get; }
        }

        private readonly struct CoffRelocation
        {
            public CoffRelocation(uint virtualAddress, uint symbolTableIndex, ushort type)
            {
                VirtualAddress = virtualAddress;
                SymbolTableIndex = symbolTableIndex;
                Type = type;
            }

            public uint VirtualAddress { get; }
            public uint SymbolTableIndex { get; }
            public ushort Type { get; }
        }

        private sealed class TestObjectNode : ObjectNode, ISymbolDefinitionNode
        {
            private readonly ObjectData _data;
            private readonly Utf8String _name;
            private readonly int _offset;
            private readonly ObjectNodeSection _section;

            public TestObjectNode(
                string name,
                ObjectNodeSection section,
                byte[] data,
                int offset = 0,
                int alignment = 1,
                Relocation[] relocations = null)
            {
                _name = new Utf8String(name);
                _section = section;
                _offset = offset;
                _data = new ObjectData(data, relocations ?? Array.Empty<Relocation>(), alignment, [this]);
            }

            public int Offset => _offset;
            public override bool IsShareable => false;
            public override int ClassCode => 0x6AEB3B7;
            public override bool StaticDependenciesAreComputed => true;

            public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) => sb.Append(_name);

            public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false) => _data;

            public override ObjectNodeSection GetSection(NodeFactory factory) => _section;

            protected override string GetName(NodeFactory factory) => _name.ToString();
        }

        private sealed class TestDataNode : ObjectNode
        {
            private readonly ObjectData _data;
            private readonly ObjectNodeSection _section;

            public TestDataNode(ObjectNodeSection section, byte[] data)
            {
                _section = section;
                _data = new ObjectData(data, Array.Empty<Relocation>(), alignment: 1, Array.Empty<ISymbolDefinitionNode>());
            }

            public override bool IsShareable => false;
            public override int ClassCode => 0x5C68E93;
            public override bool StaticDependenciesAreComputed => true;

            public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false) => _data;

            public override ObjectNodeSection GetSection(NodeFactory factory) => _section;

            protected override string GetName(NodeFactory factory) => nameof(TestDataNode);
        }
    }
}
