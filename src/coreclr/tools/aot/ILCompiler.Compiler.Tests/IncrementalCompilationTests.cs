// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

using ILCompiler.DependencyAnalysis;
using ILCompiler.ObjectWriter;

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public sealed class IncrementalCompilationTests
    {
        [Fact]
        public void ConstantOnlyLeafChangeIsAccepted()
        {
            byte[] baseline =
            [
                (byte)ILOpcode.ldarg_0,
                (byte)ILOpcode.ldc_i4, 1, 0, 0, 0,
                (byte)ILOpcode.add,
                (byte)ILOpcode.ret,
            ];
            byte[] updated = (byte[])baseline.Clone();
            updated[2] = 2;

            Assert.True(IncrementalBodyUpdate.IsDependencyNeutralConstantChange(baseline, updated));
        }

        [Fact]
        public void BodyGateRejectsUnsafeOrUnchangedEdits()
        {
            Assert.False(IncrementalBodyUpdate.IsDependencyNeutralConstantChange(
                [(byte)ILOpcode.ldarg_s, 0, (byte)ILOpcode.ret],
                [(byte)ILOpcode.ldarg_s, 1, (byte)ILOpcode.ret]));
            Assert.False(IncrementalBodyUpdate.IsDependencyNeutralConstantChange(
                [(byte)ILOpcode.call, 1, 0, 0, 0, (byte)ILOpcode.ret],
                [(byte)ILOpcode.call, 2, 0, 0, 0, (byte)ILOpcode.ret]));
            Assert.False(IncrementalBodyUpdate.IsDependencyNeutralConstantChange(
                [(byte)ILOpcode.ldc_i4, 1, 0, 0, 0, (byte)ILOpcode.ret],
                [(byte)ILOpcode.ldc_i4, 1, 0, 0, 0, (byte)ILOpcode.ret]));
            Assert.True(IncrementalBodyUpdate.IsDependencyNeutralConstantChange(
                [(byte)ILOpcode.ldc_i4_s, 1, (byte)ILOpcode.ret],
                [(byte)ILOpcode.ldc_i4_s, 2, (byte)ILOpcode.ret]));
        }

        [Theory]
        [InlineData(RelocType.IMAGE_REL_BASED_ABSOLUTE, true, 4)]
        [InlineData(RelocType.IMAGE_REL_BASED_ADDR32NB, true, 4)]
        [InlineData(RelocType.IMAGE_REL_BASED_HIGHLOW, true, 4)]
        [InlineData(RelocType.IMAGE_REL_BASED_DIR64, true, 8)]
        [InlineData(RelocType.IMAGE_REL_BASED_REL32, true, 4)]
        [InlineData(RelocType.IMAGE_REL_BASED_RELPTR32, true, 4)]
        [InlineData(RelocType.IMAGE_REL_SECREL, true, 4)]
        [InlineData(RelocType.IMAGE_REL_SECTION, false, 0)]
        [InlineData(RelocType.IMAGE_REL_BASED_ARM64_BRANCH26, false, 0)]
        public void WindowsX64RelocationWidthsAreExplicit(
            RelocType relocType,
            bool expected,
            int expectedWidth)
        {
            Assert.Equal(
                expected,
                IncrementalObjectBaseline.TryGetWindowsX64RelocationWidth(
                    relocType,
                    out int width));
            Assert.Equal(expectedWidth, width);
        }

        [Fact]
        public void BaselineRejectsNonRelocationByteMismatch()
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                byte[] recorded = [1, 2, 3, 4];
                byte[] actual = [1, 2, 9, 4];
                File.WriteAllBytes(objectPath, actual);

                var node = new TestObjectNode(recorded);
                IncrementalObjectLayout layout = CreateLayout(node, isComdat: false);
                long emissionLength = actual.Length;
                byte[] emissionHash = SHA256.HashData(actual);

                Assert.False(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    emissionLength,
                    emissionHash,
                    SHA256.HashData([1]),
                    SHA256.HashData([2]),
                    out IncrementalObjectBaseline baseline,
                    out string reason));
                Assert.Null(baseline);
                Assert.Contains("non-relocation byte", reason);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void PatchCopiesVerifiedHandleAndPublishesWithoutOverwrite()
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                string outputPath = Path.Combine(directory, "updated.obj");
                string revertedPath = Path.Combine(directory, "reverted.obj");
                byte[] original = [1, 2, 3, 4];
                File.WriteAllBytes(objectPath, original);

                var node = new TestObjectNode(original);
                IncrementalObjectLayout layout = CreateLayout(node, isComdat: false);
                byte[] assemblyHash = SHA256.HashData([1]);
                byte[] configurationHash = SHA256.HashData([2]);
                long emissionLength = original.Length;
                byte[] emissionHash = SHA256.HashData(original);

                Assert.True(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    emissionLength,
                    emissionHash,
                    assemblyHash,
                    configurationHash,
                    out IncrementalObjectBaseline baseline,
                    out string reason),
                    reason);

                using (baseline)
                {
                    node.Data = [1, 9, 3, 4];
                    Assert.True(baseline.TryWritePatchedObject(
                        outputPath,
                        [node],
                        factory: null,
                        assemblyHash,
                        configurationHash,
                        out long patchedByteCount,
                        out reason),
                        reason);
                    Assert.Equal(1, patchedByteCount);
                    Assert.Equal(node.Data, File.ReadAllBytes(outputPath));

                    node.Data = (byte[])original.Clone();
                    Assert.True(baseline.TryWritePatchedObject(
                        revertedPath,
                        [node],
                        factory: null,
                        assemblyHash,
                        configurationHash,
                        out patchedByteCount,
                        out reason),
                        reason);
                    Assert.Equal(0, patchedByteCount);
                    Assert.Equal(original, File.ReadAllBytes(revertedPath));

                    node.Data = [1, 8, 3, 4];
                    Assert.False(baseline.TryWritePatchedObject(
                        outputPath,
                        [node],
                        factory: null,
                        assemblyHash,
                        configurationHash,
                        out _,
                        out reason));
                    Assert.Contains("already exists", reason);
                    Assert.Equal([1, 9, 3, 4], File.ReadAllBytes(outputPath));
                }

                Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void BaselineAndPatchRejectHashBindingMismatches()
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                string outputPath = Path.Combine(directory, "updated.obj");
                byte[] original = [1, 2, 3, 4];
                File.WriteAllBytes(objectPath, original);

                var node = new TestObjectNode(original);
                IncrementalObjectLayout layout = CreateLayout(node, isComdat: false);
                byte[] assemblyHash = SHA256.HashData([1]);
                byte[] configurationHash = SHA256.HashData([2]);

                Assert.False(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    original.Length,
                    SHA256.HashData([9]),
                    assemblyHash,
                    configurationHash,
                    out _,
                    out string reason));
                Assert.Contains("hash", reason);

                Assert.True(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    original.Length,
                    SHA256.HashData(original),
                    assemblyHash,
                    configurationHash,
                    out IncrementalObjectBaseline baseline,
                    out reason),
                    reason);

                using (baseline)
                {
                    Assert.False(baseline.TryWritePatchedObject(
                        outputPath,
                        [node],
                        factory: null,
                        SHA256.HashData([3]),
                        configurationHash,
                        out _,
                        out reason));
                    Assert.Contains("assembly", reason);

                    Assert.False(baseline.TryWritePatchedObject(
                        outputPath,
                        [node],
                        factory: null,
                        assemblyHash,
                        SHA256.HashData([4]),
                        out _,
                        out reason));
                    Assert.Contains("configuration", reason);
                }

                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void PatchRejectsRelocationAddendChange()
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                string outputPath = Path.Combine(directory, "updated.obj");
                byte[] original = [1, 2, 3, 4, 5];
                File.WriteAllBytes(objectPath, original);

                var node = new TestObjectNode(original);
                node.Relocations =
                [
                    new Relocation(RelocType.IMAGE_REL_BASED_REL32, 0, node),
                ];
                IncrementalObjectLayout layout = CreateLayout(node, isComdat: false);
                byte[] assemblyHash = SHA256.HashData([1]);
                byte[] configurationHash = SHA256.HashData([2]);
                long emissionLength = original.Length;
                byte[] emissionHash = SHA256.HashData(original);

                Assert.True(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    emissionLength,
                    emissionHash,
                    assemblyHash,
                    configurationHash,
                    out IncrementalObjectBaseline baseline,
                    out string reason),
                    reason);

                using (baseline)
                {
                    node.Data = [9, 2, 3, 4, 5];
                    Assert.False(baseline.TryWritePatchedObject(
                        outputPath,
                        [node],
                        factory: null,
                        assemblyHash,
                        configurationHash,
                        out _,
                        out reason));
                    Assert.Contains("addend", reason);
                }

                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Theory]
        [InlineData("alignment", "alignment")]
        [InlineData("size", "size or alignment")]
        [InlineData("symbol", "defined symbols")]
        [InlineData("symbol-offset", "defined symbols")]
        [InlineData("relocation-count", "relocation count")]
        [InlineData("relocation-type", "changed a relocation")]
        [InlineData("relocation-offset", "changed a relocation")]
        [InlineData("relocation-target", "changed a relocation")]
        [InlineData("relocation-target-offset", "changed a relocation")]
        public void PatchRejectsObjectShapeChanges(string change, string expectedReason)
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                string outputPath = Path.Combine(directory, "updated.obj");
                byte[] original = [1, 2, 3, 4, 5, 6, 7, 8];
                File.WriteAllBytes(objectPath, original);

                var node = new TestObjectNode(original) { Alignment = 4 };
                var otherNode = new TestObjectNode(original);
                node.Relocations =
                [
                    new Relocation(RelocType.IMAGE_REL_BASED_REL32, 0, otherNode),
                ];
                node.DefinedSymbols = [node];
                IncrementalObjectLayout layout = CreateLayout(node, isComdat: false);
                byte[] assemblyHash = SHA256.HashData([1]);
                byte[] configurationHash = SHA256.HashData([2]);
                long emissionLength = original.Length;
                byte[] emissionHash = SHA256.HashData(original);

                Assert.True(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    emissionLength,
                    emissionHash,
                    assemblyHash,
                    configurationHash,
                    out IncrementalObjectBaseline baseline,
                    out string reason),
                    reason);

                using (baseline)
                {
                    switch (change)
                    {
                        case "alignment":
                            node.Alignment = 8;
                            break;
                        case "size":
                            node.Data = [1, 2, 3, 4, 5, 6, 7, 8, 9];
                            break;
                        case "symbol":
                            node.DefinedSymbols = [otherNode];
                            break;
                        case "symbol-offset":
                            node.OffsetValue = 1;
                            break;
                        case "relocation-count":
                            node.Relocations = [];
                            break;
                        case "relocation-type":
                            node.Relocations =
                            [
                                new Relocation(RelocType.IMAGE_REL_BASED_HIGHLOW, 0, node),
                            ];
                            break;
                        case "relocation-offset":
                            node.Relocations =
                            [
                                new Relocation(RelocType.IMAGE_REL_BASED_REL32, 4, node),
                            ];
                            break;
                        case "relocation-target":
                            node.Relocations =
                            [
                                new Relocation(RelocType.IMAGE_REL_BASED_REL32, 0, node),
                            ];
                            break;
                        case "relocation-target-offset":
                            otherNode.OffsetValue = 1;
                            break;
                    }

                    Assert.False(baseline.TryWritePatchedObject(
                        outputPath,
                        [node],
                        factory: null,
                        assemblyHash,
                        configurationHash,
                        out _,
                        out reason));
                    Assert.Contains(expectedReason, reason);
                }

                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void BaselineRejectsOverlappingRelocations()
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                byte[] original = [1, 2, 3, 4, 5, 6];
                File.WriteAllBytes(objectPath, original);

                var node = new TestObjectNode(original);
                node.Relocations =
                [
                    new Relocation(RelocType.IMAGE_REL_BASED_REL32, 0, node),
                    new Relocation(RelocType.IMAGE_REL_BASED_REL32, 2, node),
                ];
                IncrementalObjectLayout layout = CreateLayout(node, isComdat: false);
                long emissionLength = original.Length;
                byte[] emissionHash = SHA256.HashData(original);

                Assert.False(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    emissionLength,
                    emissionHash,
                    SHA256.HashData([1]),
                    SHA256.HashData([2]),
                    out _,
                    out string reason));
                Assert.Contains("overlap", reason);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void ComdatAndDuplicateRecordsAreRejectedBeforeMutation()
        {
            byte[] data = [1, 2, 3, 4];
            var node = new TestObjectNode(data);
            var layout = new IncrementalObjectLayout([node]);
            layout.RecordNode(node, 0, 0, node.GetData(null), isComdat: false);
            layout.RecordNode(node, 0, 0, node.GetData(null), isComdat: false);
            Assert.Contains("more than once", layout.FailureReason);

            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                File.WriteAllBytes(objectPath, data);
                layout = CreateLayout(node, isComdat: true);
                long emissionLength = data.Length;
                byte[] emissionHash = SHA256.HashData(data);

                Assert.False(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    emissionLength,
                    emissionHash,
                    SHA256.HashData([1]),
                    SHA256.HashData([2]),
                    out _,
                    out string reason));
                Assert.Contains("COMDAT", reason);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void BaselineAllowsPageRoundedLoadedImageAndRejectsPrefixMismatch()
        {
            string assemblyPath = typeof(IncrementalCompilationTests).Assembly.Location;
            byte[] image = File.ReadAllBytes(assemblyPath);
            using var reader = new PEReader(new MemoryStream(image, writable: false));
            MetadataReader metadata = reader.GetMetadataReader();
            Guid mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid);

            int roundedLength = checked((image.Length + 4095) & ~4095);
            if (roundedLength == image.Length)
                roundedLength += 4096;
            byte[] longerLoadedImage = new byte[roundedLength];
            image.CopyTo(longerLoadedImage, 0);
            Assert.True(IncrementalAssemblyBaseline.TryCreate(
                longerLoadedImage,
                mvid,
                metadata.MethodDefinitions.Count,
                assemblyPath,
                out _,
                out string reason),
                reason);

            longerLoadedImage[0] ^= 1;
            Assert.False(IncrementalAssemblyBaseline.TryCreate(
                longerLoadedImage,
                mvid,
                metadata.MethodDefinitions.Count,
                assemblyPath,
                out _,
                out reason));
            Assert.Contains("does-not-match", reason);
        }

        [Fact]
        public void IncrementalFailureContractMatchesCompilerException()
        {
            var exception = new IncrementalCompilationException("rejected");

            Assert.Equal(85, IncrementalFailureContract.CleanFallbackExitCode);
            Assert.Equal(IncrementalFailureContract.FailureHResult, exception.HResult);
            Assert.True(IncrementalFailureContract.IsCleanFallbackRequested(
                exception,
                isEnvironmentRequested: true));
            Assert.False(IncrementalFailureContract.IsCleanFallbackRequested(
                exception,
                isEnvironmentRequested: false));
            Assert.False(IncrementalFailureContract.IsCleanFallbackRequested(
                new InvalidOperationException(),
                isEnvironmentRequested: true));
        }

        [Fact]
        public void PeGateMasksOnlyPermittedBytesAndMethodBodies()
        {
            string directory = CreateTestDirectory();
            try
            {
                byte[] baselineImage = File.ReadAllBytes(
                    typeof(IncrementalCompilationTests).Assembly.Location);
                string baselinePath = Path.Combine(directory, "baseline.dll");
                string updatedPath = Path.Combine(directory, "updated.dll");
                File.WriteAllBytes(baselinePath, baselineImage);

                IncrementalAssemblyBaseline baseline = CreateAssemblyBaseline(
                    baselineImage,
                    baselinePath);
                byte[] updatedImage = (byte[])baselineImage.Clone();
                using (var reader = new PEReader(new MemoryStream(updatedImage, writable: false)))
                {
                    int timestampOffset =
                        checked(reader.PEHeaders.CoffHeaderStartOffset + sizeof(uint));
                    updatedImage[timestampOffset] ^= 0x5A;
                }
                int ilOffset = GetMethodILOffset(updatedImage, nameof(PeFixture));
                Assert.Equal(0x44, updatedImage[ilOffset + 2]);
                updatedImage[ilOffset + 2] = 0x45;
                File.WriteAllBytes(updatedPath, updatedImage);

                Assert.True(IncrementalBodyUpdate.TryCreate(
                    baseProvider: null,
                    baseline,
                    updatedPath,
                    allowUnchangedTarget: false,
                    out IncrementalBodyUpdate update,
                    out string reason),
                    reason);
                Assert.Equal(1, update.ChangedMethodCount);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Theory]
        [InlineData("mvid", "module-version-id-changed")]
        [InlineData("non-body", "non-method-assembly-content-changed")]
        [InlineData("body-size", "method-body-size-changed")]
        [InlineData("body-shape", "method-body-shape-changed")]
        public void PeGateRejectsIdentityAndShapeChanges(string change, string expectedReason)
        {
            string directory = CreateTestDirectory();
            try
            {
                byte[] baselineImage = File.ReadAllBytes(
                    typeof(IncrementalCompilationTests).Assembly.Location);
                string baselinePath = Path.Combine(directory, "baseline.dll");
                string updatedPath = Path.Combine(directory, "updated.dll");
                File.WriteAllBytes(baselinePath, baselineImage);
                IncrementalAssemblyBaseline baseline = CreateAssemblyBaseline(
                    baselineImage,
                    baselinePath);
                byte[] updatedImage = (byte[])baselineImage.Clone();

                switch (change)
                {
                    case "mvid":
                        using (var reader = new PEReader(
                            new MemoryStream(updatedImage, writable: false)))
                        {
                            MetadataReader metadata = reader.GetMetadataReader();
                            Guid mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
                            int offset = FindSequence(updatedImage, mvid.ToByteArray());
                            Assert.True(offset >= 0);
                            updatedImage[offset] ^= 1;
                        }
                        break;
                    case "non-body":
                        updatedImage[2] ^= 1;
                        break;
                    case "body-size":
                        int bodyOffset = GetMethodBodyOffset(updatedImage, nameof(PeFixture));
                        if ((updatedImage[bodyOffset] & 3) == 2)
                        {
                            updatedImage[bodyOffset] = checked((byte)(updatedImage[bodyOffset] + 4));
                        }
                        else
                        {
                            int size = BitConverter.ToInt32(updatedImage, bodyOffset + 4);
                            BitConverter.GetBytes(size + 1).CopyTo(updatedImage, bodyOffset + 4);
                        }
                        break;
                    case "body-shape":
                        int fatBodyOffset = GetMethodBodyOffset(
                            updatedImage,
                            nameof(PeFatFixture));
                        Assert.Equal(3, updatedImage[fatBodyOffset] & 3);
                        updatedImage[fatBodyOffset + 2] ^= 1;
                        break;
                }

                File.WriteAllBytes(updatedPath, updatedImage);
                Assert.False(IncrementalBodyUpdate.TryCreate(
                    baseProvider: null,
                    baseline,
                    updatedPath,
                    allowUnchangedTarget: true,
                    out _,
                    out string reason));
                Assert.Contains(expectedReason, reason);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void NonEcmaMethodIlIsNotOverlayable()
        {
            Assert.False(IncrementalBodyUpdate.IsOverlayableMethodIL(null));
            Assert.False(IncrementalBodyUpdate.IsOverlayableMethodIL(new TestMethodIL()));
        }

        [Fact]
        public void SequentialDirtyUnionIncludesEditDifferentMethodAndRevert()
        {
            Assert.Equal(
                [1],
                Sorted(IncrementalBodyUpdate.GetAffectedMethodTokens([1], previousTokens: null)));
            Assert.Equal(
                [1, 2],
                Sorted(IncrementalBodyUpdate.GetAffectedMethodTokens([2], [1])));
            Assert.Equal(
                [2],
                Sorted(IncrementalBodyUpdate.GetAffectedMethodTokens([], [2])));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void UnsafeCommandLineOutputsAreRejected(int enabledOption)
        {
            var values = new bool[9];
            values[enabledOption] = true;
            Assert.False(RyuJitCompilationBuilder.TryValidateIncrementalCommandLineConfiguration(
                values[0],
                values[1],
                values[2],
                values[3],
                values[4],
                values[5],
                values[6],
                values[7],
                values[8],
                out _,
                out string reason));
            Assert.Contains("unsupported", reason);
        }

        [Theory]
        [InlineData("gc", "gc-info")]
        [InlineData("frame", "frame")]
        [InlineData("eh", "exception-handling")]
        [InlineData("debug", "debug-info")]
        public void CodeStateChangesAreRejected(string change, string expectedReason)
        {
            IncrementalCodeState baseline = CreateCodeState();
            IncrementalCodeState current = change switch
            {
                "gc" => CreateCodeState(gcInfo: [2]),
                "frame" => CreateCodeState(
                    frameInfos: [new FrameInfo(0, 0, 1, [1])]),
                "eh" => CreateCodeState(hasEhInfo: true),
                "debug" => CreateCodeState(
                    debugLocations: [new DebugLocInfo(1, 1)]),
                _ => throw new ArgumentOutOfRangeException(nameof(change)),
            };

            Assert.False(IncrementalCodeStateValidator.Matches(
                baseline,
                current,
                out string reason));
            Assert.Contains(expectedReason, reason);
        }

        [Theory]
        [InlineData("order", "static-dependency")]
        [InlineData("reason", "static-dependency")]
        [InlineData("marked", "unmarked-static-dependency")]
        [InlineData("conditional-reason", "conditional-dependency")]
        [InlineData("conditional-marked", "unmarked-conditional-dependency")]
        public void DependencyStateChangesAreRejected(string change, string expectedReason)
        {
            var first = new object();
            var second = new object();
            IncrementalDependencyEntry[] baselineStatic =
            [
                new(first, "first", marked: true),
                new(second, "second", marked: true),
            ];
            IncrementalConditionalDependencyEntry[] baselineConditional =
            [
                new(first, second, "conditional", nodeMarked: true, otherReasonNodeMarked: true),
            ];
            IncrementalDependencyEntry[] currentStatic = (IncrementalDependencyEntry[])baselineStatic.Clone();
            IncrementalConditionalDependencyEntry[] currentConditional =
                (IncrementalConditionalDependencyEntry[])baselineConditional.Clone();

            switch (change)
            {
                case "order":
                    currentStatic =
                    [
                        new(second, "second", marked: true),
                        new(first, "first", marked: true),
                    ];
                    break;
                case "reason":
                    currentStatic[0] = new(first, "different", marked: true);
                    break;
                case "marked":
                    currentStatic[0] = new(first, "first", marked: false);
                    break;
                case "conditional-reason":
                    currentConditional[0] =
                        new(first, second, "different", nodeMarked: true, otherReasonNodeMarked: true);
                    break;
                case "conditional-marked":
                    currentConditional[0] =
                        new(first, second, "conditional", nodeMarked: true, otherReasonNodeMarked: false);
                    break;
            }

            Assert.False(IncrementalDependencyValidator.Matches(
                baselineStatic,
                baselineConditional,
                currentStatic,
                currentConditional,
                out string reason));
            Assert.Contains(expectedReason, reason);
        }

        [Fact]
        public void LayoutRejectsDuplicateUnresolvedAndOverlappingLocations()
        {
            var first = new TestObjectNode([1, 2, 3, 4]);
            var second = new TestObjectNode([5, 6, 7, 8]);
            var duplicate = new IncrementalObjectLayout([first, first]);
            Assert.Contains("more than once", duplicate.FailureReason);

            var unresolved = new IncrementalObjectLayout([first]);
            unresolved.RecordNode(first, 0, 0, first.GetData(null), isComdat: false);
            unresolved.Complete((int _, long _, int _) => null);
            Assert.Contains("could not be resolved", unresolved.FailureReason);

            var overlap = new IncrementalObjectLayout([first, second]);
            overlap.RecordNode(first, 0, 0, first.GetData(null), isComdat: false);
            overlap.RecordNode(second, 0, 2, second.GetData(null), isComdat: false);
            overlap.Complete(
                (int _, long sectionOffset, int _) => sectionOffset);
            Assert.Contains("overlap", overlap.FailureReason);
        }

        [Fact]
        public void BaselineRejectsOutOfBoundsLocation()
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                byte[] data = [1, 2, 3, 4];
                File.WriteAllBytes(objectPath, data);
                var node = new TestObjectNode(data);
                var layout = new IncrementalObjectLayout([node]);
                layout.RecordNode(node, 0, 0, node.GetData(null), isComdat: false);
                layout.Complete((int _, long _, int _) => 1);

                Assert.False(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    data.Length,
                    SHA256.HashData(data),
                    SHA256.HashData([1]),
                    SHA256.HashData([2]),
                    out _,
                    out string reason));
                Assert.Contains("outside", reason);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void NullRelocationsAndSymbolsAreTreatedAsEmpty()
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                byte[] data = [1, 2, 3, 4];
                File.WriteAllBytes(objectPath, data);
                var node = new TestObjectNode(data)
                {
                    Relocations = null,
                    DefinedSymbols = null,
                };
                IncrementalObjectLayout layout = CreateLayout(node, isComdat: false);

                Assert.True(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    data.Length,
                    SHA256.HashData(data),
                    SHA256.HashData([1]),
                    SHA256.HashData([2]),
                    out IncrementalObjectBaseline baseline,
                    out string reason),
                    reason);
                baseline.Dispose();
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void StagedBatchRollsBackPublishedOutputsAfterLaterFailure()
        {
            string directory = CreateTestDirectory();
            try
            {
                string objectPath = Path.Combine(directory, "baseline.obj");
                byte[] data = [1, 2, 3, 4];
                File.WriteAllBytes(objectPath, data);
                var node = new TestObjectNode(data);
                IncrementalObjectLayout layout = CreateLayout(node, isComdat: false);
                byte[] assemblyHash = SHA256.HashData([1]);
                byte[] configurationHash = SHA256.HashData([2]);
                Assert.True(IncrementalObjectBaseline.TryOpenLocked(
                    objectPath,
                    layout,
                    data.Length,
                    SHA256.HashData(data),
                    assemblyHash,
                    configurationHash,
                    out IncrementalObjectBaseline baseline,
                    out string reason),
                    reason);

                using (baseline)
                {
                    string firstOutput = Path.Combine(directory, "first.obj");
                    string secondOutput = Path.Combine(directory, "second.obj");
                    Assert.True(baseline.TryStagePatchedObject(
                        firstOutput,
                        [node],
                        factory: null,
                        assemblyHash,
                        configurationHash,
                        out IncrementalStagedObject first,
                        out _,
                        out reason),
                        reason);
                    Assert.True(baseline.TryStagePatchedObject(
                        secondOutput,
                        [node],
                        factory: null,
                        assemblyHash,
                        configurationHash,
                        out IncrementalStagedObject second,
                        out _,
                        out reason),
                        reason);

                    File.WriteAllBytes(secondOutput, [9]);
                    Assert.True(first.TryPublish(out reason), reason);
                    Assert.False(second.TryPublish(out reason));
                    Assert.True(first.TryCleanup(out string firstCleanup), firstCleanup);
                    Assert.True(second.TryCleanup(out string secondCleanup), secondCleanup);
                    Assert.False(File.Exists(firstOutput));
                    Assert.Equal([9], File.ReadAllBytes(secondOutput));
                    Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
                }
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void CleanupFailureIsExplicit()
        {
            string directory = CreateTestDirectory();
            try
            {
                string temporaryPath = Path.Combine(directory, "staged.tmp");
                File.WriteAllBytes(temporaryPath, [1]);
                var staged = new IncrementalStagedObject(
                    temporaryPath,
                    Path.Combine(directory, "output.obj"));

                using (new FileStream(
                    staged.TemporaryPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None))
                {
                    Assert.False(staged.TryCleanup(out string reason));
                    Assert.Contains("could not be deleted", reason);
                }

                Assert.True(staged.TryCleanup(out string cleanupReason), cleanupReason);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static IncrementalAssemblyBaseline CreateAssemblyBaseline(
            byte[] image,
            string path)
        {
            using var reader = new PEReader(new MemoryStream(image, writable: false));
            MetadataReader metadata = reader.GetMetadataReader();
            Assert.True(IncrementalAssemblyBaseline.TryCreate(
                image,
                metadata.GetGuid(metadata.GetModuleDefinition().Mvid),
                metadata.MethodDefinitions.Count,
                path,
                out IncrementalAssemblyBaseline baseline,
                out string reason),
                reason);
            return baseline;
        }

        private static int GetMethodBodyOffset(byte[] image, string methodName)
        {
            using var reader = new PEReader(new MemoryStream(image, writable: false));
            MetadataReader metadata = reader.GetMetadataReader();
            foreach (MethodDefinitionHandle handle in metadata.MethodDefinitions)
            {
                MethodDefinition method = metadata.GetMethodDefinition(handle);
                if (metadata.GetString(method.Name) == methodName)
                    return RvaToFileOffset(reader.PEHeaders, method.RelativeVirtualAddress);
            }

            throw new InvalidOperationException($"Method '{methodName}' was not found.");
        }

        private static int GetMethodILOffset(byte[] image, string methodName)
        {
            int bodyOffset = GetMethodBodyOffset(image, methodName);
            if ((image[bodyOffset] & 3) == 2)
                return checked(bodyOffset + 1);

            int headerSize = (BitConverter.ToUInt16(image, bodyOffset) >> 12) * 4;
            return checked(bodyOffset + headerSize);
        }

        private static int RvaToFileOffset(PEHeaders headers, int rva)
        {
            foreach (SectionHeader section in headers.SectionHeaders)
            {
                int sectionSize = Math.Max(section.VirtualSize, section.SizeOfRawData);
                if (rva >= section.VirtualAddress &&
                    rva - section.VirtualAddress < sectionSize)
                {
                    return checked(section.PointerToRawData + rva - section.VirtualAddress);
                }
            }

            throw new InvalidOperationException($"RVA 0x{rva:X8} was not mapped.");
        }

        private static int FindSequence(byte[] image, byte[] sequence)
        {
            for (int i = 0; i <= image.Length - sequence.Length; i++)
            {
                if (image.AsSpan(i, sequence.Length).SequenceEqual(sequence))
                    return i;
            }

            return -1;
        }

        private static int[] Sorted(HashSet<int> values)
        {
            int[] result = new int[values.Count];
            values.CopyTo(result);
            Array.Sort(result);
            return result;
        }

        private static IncrementalCodeState CreateCodeState(
            FrameInfo[] frameInfos = null,
            byte[] gcInfo = null,
            bool hasEhInfo = false,
            DebugLocInfo[] debugLocations = null)
        {
            return new IncrementalCodeState(
                frameInfos ?? Array.Empty<FrameInfo>(),
                gcInfo ?? [1],
                hasEhInfo,
                debugLocations ?? Array.Empty<DebugLocInfo>(),
                Array.Empty<DebugVarInfo>(),
                Array.Empty<DebugEHClauseInfo>(),
                debugInfo: null,
                Array.Empty<TypeDesc>());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int PeFixture(int value) => value + 0x11223344;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int PeFatFixture(int value)
        {
            try
            {
                int result = value + 1;
                return result;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private static IncrementalObjectLayout CreateLayout(
            TestObjectNode node,
            bool isComdat)
        {
            var layout = new IncrementalObjectLayout([node]);
            layout.RecordNode(
                node,
                sectionIndex: 0,
                sectionOffset: 0,
                node.GetData(factory: null),
                isComdat);
            layout.Complete(
                (int sectionIndex, long sectionOffset, int _) =>
                    sectionIndex == 0 ? sectionOffset : null);
            return layout;
        }

        private static string CreateTestDirectory()
        {
            string directory = Path.Combine(
                AppContext.BaseDirectory,
                $"IncrementalCompilationTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private sealed class TestObjectNode : ObjectNode, ISymbolDefinitionNode
        {
            internal TestObjectNode(byte[] data)
            {
                Data = (byte[])data.Clone();
            }

            internal byte[] Data { get; set; }
            internal Relocation[] Relocations { get; set; } = Array.Empty<Relocation>();
            internal int Alignment { get; set; } = 1;
            internal ISymbolDefinitionNode[] DefinedSymbols { get; set; } =
                Array.Empty<ISymbolDefinitionNode>();

            public override int ClassCode => -203822639;
            public override bool IsShareable => false;
            public override bool StaticDependenciesAreComputed => true;
            internal int OffsetValue { get; set; }
            public int Offset => OffsetValue;
            public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) =>
                sb.Append(nameof(TestObjectNode));
            public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false) =>
                new ObjectData(
                    (byte[])Data.Clone(),
                    Relocations is null ? null : (Relocation[])Relocations.Clone(),
                    Alignment,
                    DefinedSymbols is null ?
                        null :
                        (ISymbolDefinitionNode[])DefinedSymbols.Clone());
            public override ObjectNodeSection GetSection(NodeFactory factory) =>
                ObjectNodeSection.DataSection;
            protected override string GetName(NodeFactory factory) => nameof(TestObjectNode);
        }

        private sealed class TestMethodIL : MethodIL
        {
            public override MethodDesc OwningMethod => null;
            public override int MaxStack => 0;
            public override bool IsInitLocals => false;
            public override byte[] GetILBytes() => [(byte)ILOpcode.ret];
            public override LocalVariableDefinition[] GetLocals() =>
                Array.Empty<LocalVariableDefinition>();
            public override ILExceptionRegion[] GetExceptionRegions() =>
                Array.Empty<ILExceptionRegion>();
            public override object GetObject(
                int token,
                NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw) => null;
        }
    }
}
