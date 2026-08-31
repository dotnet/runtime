// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    internal sealed class IncrementalAssemblyBaseline
    {
        private IncrementalAssemblyBaseline(byte[] image, Guid mvid)
        {
            Image = image;
            Mvid = mvid;
            ImageHash = SHA256.HashData(image);
        }

        internal byte[] Image { get; }
        internal byte[] ImageHash { get; }
        internal Guid Mvid { get; }

        internal static bool TryCreate(
            EcmaModule module,
            string path,
            out IncrementalAssemblyBaseline baseline,
            out string reason)
        {
            MetadataReader moduleMetadata = module.MetadataReader;
            return TryCreate(
                module.PEReader.GetEntireImage().GetContent().AsSpan(),
                moduleMetadata.GetGuid(moduleMetadata.GetModuleDefinition().Mvid),
                moduleMetadata.MethodDefinitions.Count,
                path,
                out baseline,
                out reason);
        }

        internal static bool TryCreate(
            ReadOnlySpan<byte> loadedImage,
            Guid loadedMvid,
            int loadedMethodCount,
            string path,
            out IncrementalAssemblyBaseline baseline,
            out string reason)
        {
            baseline = null;
            reason = null;
            byte[] image;
            try
            {
                image = File.ReadAllBytes(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                reason = $"baseline-assembly-read-failed:{ex.Message}";
                return false;
            }

            try
            {
                using var reader = new PEReader(new MemoryStream(image, writable: false));
                if (!reader.HasMetadata)
                {
                    reason = "baseline-assembly-has-no-metadata";
                    return false;
                }
                if (loadedImage.Length < image.Length ||
                    !loadedImage.Slice(0, image.Length).SequenceEqual(image))
                {
                    reason = "baseline-input-does-not-match-loaded-module";
                    return false;
                }

                MetadataReader fileMetadata = reader.GetMetadataReader();
                Guid fileMvid = fileMetadata.GetGuid(fileMetadata.GetModuleDefinition().Mvid);
                if (fileMvid != loadedMvid ||
                    fileMetadata.MethodDefinitions.Count != loadedMethodCount)
                {
                    reason = "baseline-input-does-not-match-loaded-module";
                    return false;
                }

                baseline = new IncrementalAssemblyBaseline(image, fileMvid);
                return true;
            }
            catch (BadImageFormatException)
            {
                reason = "invalid-baseline-assembly";
                return false;
            }
        }
    }

    internal sealed class IncrementalBodyUpdate : ILProvider
    {
        private readonly ILProvider _baseProvider;
        private readonly Guid _baseMvid;
        private readonly Dictionary<int, byte[]> _updatedMethodBodies;

        private IncrementalBodyUpdate(
            ILProvider baseProvider,
            Guid baseMvid,
            Dictionary<int, byte[]> updatedMethodBodies)
        {
            _baseProvider = baseProvider;
            _baseMvid = baseMvid;
            _updatedMethodBodies = updatedMethodBodies;
        }

        internal int ChangedMethodCount => _updatedMethodBodies.Count;

        internal IEnumerable<int> ChangedMethodTokens => _updatedMethodBodies.Keys;

        internal static bool TryCreate(
            ILProvider baseProvider,
            IncrementalAssemblyBaseline baseline,
            string updatedAssemblyPath,
            bool allowUnchangedTarget,
            out IncrementalBodyUpdate update,
            out string reason)
        {
            update = null;
            reason = null;
            byte[] updatedImage;

            try
            {
                updatedImage = File.ReadAllBytes(updatedAssemblyPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                reason = $"updated-assembly-read-failed:{ex.Message}";
                return false;
            }

            byte[] baseImage = baseline.Image;
            if (baseImage.Length != updatedImage.Length)
            {
                reason = "assembly-size-changed";
                return false;
            }

            using var baseReader = new PEReader(new MemoryStream(baseImage, writable: false));
            using var updatedReader = new PEReader(new MemoryStream(updatedImage, writable: false));
            if (!baseReader.HasMetadata || !updatedReader.HasMetadata)
            {
                reason = "assembly-has-no-metadata";
                return false;
            }

            MetadataReader baseMetadata = baseReader.GetMetadataReader();
            MetadataReader updatedMetadata = updatedReader.GetMetadataReader();
            if (baseMetadata.MethodDefinitions.Count != updatedMetadata.MethodDefinitions.Count)
            {
                reason = "method-definition-count-changed";
                return false;
            }

            Guid baseMvid = baseMetadata.GetGuid(baseMetadata.GetModuleDefinition().Mvid);
            Guid updatedMvid = updatedMetadata.GetGuid(updatedMetadata.GetModuleDefinition().Mvid);
            if (baseMvid != baseline.Mvid || baseMvid != updatedMvid)
            {
                reason = "module-version-id-changed";
                return false;
            }

            byte[] sanitizedBaseImage = (byte[])baseImage.Clone();
            byte[] sanitizedUpdatedImage = (byte[])updatedImage.Clone();
            if (!TryMaskNonSemanticDirectories(baseReader, sanitizedBaseImage, out reason) ||
                !TryMaskNonSemanticDirectories(updatedReader, sanitizedUpdatedImage, out reason))
            {
                return false;
            }

            var updatedBodies = new Dictionary<int, byte[]>();
            foreach (MethodDefinitionHandle methodHandle in baseMetadata.MethodDefinitions)
            {
                MethodDefinition baseMethod = baseMetadata.GetMethodDefinition(methodHandle);
                MethodDefinition updatedMethod = updatedMetadata.GetMethodDefinition(methodHandle);
                int baseRva = baseMethod.RelativeVirtualAddress;
                int updatedRva = updatedMethod.RelativeVirtualAddress;
                int token = MetadataTokens.GetToken(methodHandle);

                if ((baseRva == 0) != (updatedRva == 0))
                {
                    reason = $"method-body-presence-changed:{token:X8}";
                    return false;
                }
                if (baseRva == 0)
                    continue;

                MethodBodyBlock baseBody;
                MethodBodyBlock updatedBody;
                try
                {
                    baseBody = baseReader.GetMethodBody(baseRva);
                    updatedBody = updatedReader.GetMethodBody(updatedRva);
                }
                catch (BadImageFormatException)
                {
                    reason = $"invalid-method-body:{token:X8}";
                    return false;
                }

                if (baseBody.Size != updatedBody.Size)
                {
                    reason = $"method-body-size-changed:{token:X8}";
                    return false;
                }

                if (!TryMaskMethodBody(baseReader.PEHeaders, sanitizedBaseImage, baseRva, baseBody.Size) ||
                    !TryMaskMethodBody(updatedReader.PEHeaders, sanitizedUpdatedImage, updatedRva, updatedBody.Size))
                {
                    reason = $"method-body-location-invalid:{token:X8}";
                    return false;
                }

                byte[] baseIl = baseBody.GetILBytes();
                byte[] updatedIl = updatedBody.GetILBytes();
                if (!HasEquivalentMethodBodyShape(baseBody, updatedBody))
                {
                    reason = $"method-body-shape-changed:{token:X8}";
                    return false;
                }
                if (baseIl.AsSpan().SequenceEqual(updatedIl))
                    continue;

                string methodName = baseMetadata.GetString(baseMethod.Name).ToString();
                if (methodName is ".cctor" or ".ctor")
                {
                    reason = $"constructor-body-changed:{token:X8}";
                    return false;
                }

                TypeDefinition declaringType = baseMetadata.GetTypeDefinition(baseMethod.GetDeclaringType());
                if (baseMethod.GetGenericParameters().Count != 0 ||
                    declaringType.GetGenericParameters().Count != 0)
                {
                    reason = $"generic-method-or-type-changed:{token:X8}";
                    return false;
                }

                if (!IsDependencyNeutralConstantChange(baseIl, updatedIl))
                {
                    reason = $"changed-body-is-not-constant-only:{token:X8}";
                    return false;
                }

                updatedBodies.Add(token, updatedIl);
            }

            if (!sanitizedBaseImage.AsSpan().SequenceEqual(sanitizedUpdatedImage))
            {
                reason = "non-method-assembly-content-changed";
                return false;
            }
            if (updatedBodies.Count == 0 && !allowUnchangedTarget)
            {
                reason = "no-method-body-changes";
                return false;
            }

            update = new IncrementalBodyUpdate(baseProvider, baseMvid, updatedBodies);
            return true;
        }

        internal bool IsChangedMethod(MethodDesc method)
        {
            return TryGetTargetMethodToken(method, out _, out int token) &&
                _updatedMethodBodies.ContainsKey(token);
        }

        internal static HashSet<int> GetAffectedMethodTokens(
            IEnumerable<int> currentTokens,
            IEnumerable<int> previousTokens)
        {
            var result = new HashSet<int>(currentTokens);
            if (previousTokens is not null)
                result.UnionWith(previousTokens);
            return result;
        }

        internal bool CanOverlayChangedMethod(MethodDesc method, out string reason)
        {
            if (!IsChangedMethod(method))
            {
                reason = null;
                return true;
            }

            MethodIL original = _baseProvider.GetMethodIL(method);
            if (!IsOverlayableMethodIL(original))
            {
                reason = original is null ?
                    "changed-method-has-no-il" :
                    "changed-method-il-is-not-overlayable";
                return false;
            }

            reason = null;
            return true;
        }

        internal static bool IsOverlayableMethodIL(MethodIL methodIL) =>
            methodIL?.GetMethodILDefinition() is EcmaMethodIL;

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            MethodIL original = _baseProvider.GetMethodIL(method);
            if (original is null ||
                !TryGetTargetMethodToken(method, out EcmaMethod ecmaMethod, out int token) ||
                !_updatedMethodBodies.TryGetValue(token, out byte[] updatedIl))
            {
                return original;
            }

            MethodIL originalDefinition = original.GetMethodILDefinition();
            if (originalDefinition is not EcmaMethodIL)
            {
                throw new InvalidOperationException(
                    $"Incremental IL cannot overlay method token {token:X8}.");
            }

            var updatedDefinition = new UpdatedMethodIL(ecmaMethod, originalDefinition, updatedIl);
            return method == ecmaMethod ?
                updatedDefinition :
                new InstantiatedMethodIL(method, updatedDefinition);
        }

        internal bool TryGetTargetMethodToken(
            MethodDesc method,
            out EcmaMethod ecmaMethod,
            out int token)
        {
            ecmaMethod = method.GetTypicalMethodDefinition() as EcmaMethod;
            if (ecmaMethod is null)
            {
                token = 0;
                return false;
            }

            MetadataReader metadata = ecmaMethod.Module.MetadataReader;
            Guid mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
            if (mvid != _baseMvid)
            {
                token = 0;
                return false;
            }

            token = MetadataTokens.GetToken(ecmaMethod.Handle);
            return true;
        }

        private static bool HasEquivalentMethodBodyShape(MethodBodyBlock left, MethodBodyBlock right)
        {
            if (left.MaxStack != right.MaxStack ||
                left.LocalVariablesInitialized != right.LocalVariablesInitialized ||
                left.LocalSignature != right.LocalSignature ||
                left.ExceptionRegions.Length != right.ExceptionRegions.Length)
            {
                return false;
            }

            for (int i = 0; i < left.ExceptionRegions.Length; i++)
            {
                ExceptionRegion leftRegion = left.ExceptionRegions[i];
                ExceptionRegion rightRegion = right.ExceptionRegions[i];
                if (leftRegion.Kind != rightRegion.Kind ||
                    leftRegion.TryOffset != rightRegion.TryOffset ||
                    leftRegion.TryLength != rightRegion.TryLength ||
                    leftRegion.HandlerOffset != rightRegion.HandlerOffset ||
                    leftRegion.HandlerLength != rightRegion.HandlerLength ||
                    leftRegion.CatchType != rightRegion.CatchType ||
                    leftRegion.FilterOffset != rightRegion.FilterOffset)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsDependencyNeutralConstantChange(ReadOnlySpan<byte> baseIl, ReadOnlySpan<byte> updatedIl)
        {
            var baseReader = new ILReader(baseIl);
            var updatedReader = new ILReader(updatedIl);
            bool foundChangedConstant = false;

            while (baseReader.HasNext && updatedReader.HasNext)
            {
                ILOpcode baseOpcode = baseReader.ReadILOpcode();
                ILOpcode updatedOpcode = updatedReader.ReadILOpcode();
                if (baseOpcode != updatedOpcode || !IsAllowedLeafOpcode(baseOpcode))
                    return false;

                int baseOperandStart = baseReader.Offset;
                int updatedOperandStart = updatedReader.Offset;
                baseReader.Skip(baseOpcode);
                updatedReader.Skip(updatedOpcode);

                ReadOnlySpan<byte> baseOperand =
                    baseIl.Slice(baseOperandStart, baseReader.Offset - baseOperandStart);
                ReadOnlySpan<byte> updatedOperand =
                    updatedIl.Slice(updatedOperandStart, updatedReader.Offset - updatedOperandStart);
                if (!baseOperand.SequenceEqual(updatedOperand))
                {
                    if (baseOpcode is not (
                        ILOpcode.ldc_i4_s or
                        ILOpcode.ldc_i4 or
                        ILOpcode.ldc_i8 or
                        ILOpcode.ldc_r4 or
                        ILOpcode.ldc_r8))
                    {
                        return false;
                    }

                    foundChangedConstant = true;
                }
            }

            return foundChangedConstant &&
                !baseReader.HasNext &&
                !updatedReader.HasNext;
        }

        private static bool IsAllowedLeafOpcode(ILOpcode opcode)
        {
            return opcode is
                ILOpcode.nop or
                ILOpcode.ldarg_0 or
                ILOpcode.ldarg_1 or
                ILOpcode.ldarg_2 or
                ILOpcode.ldarg_3 or
                ILOpcode.ldarg_s or
                ILOpcode.ldarg or
                ILOpcode.ldc_i4_m1 or
                ILOpcode.ldc_i4_0 or
                ILOpcode.ldc_i4_1 or
                ILOpcode.ldc_i4_2 or
                ILOpcode.ldc_i4_3 or
                ILOpcode.ldc_i4_4 or
                ILOpcode.ldc_i4_5 or
                ILOpcode.ldc_i4_6 or
                ILOpcode.ldc_i4_7 or
                ILOpcode.ldc_i4_8 or
                ILOpcode.ldc_i4_s or
                ILOpcode.ldc_i4 or
                ILOpcode.ldc_i8 or
                ILOpcode.ldc_r4 or
                ILOpcode.ldc_r8 or
                ILOpcode.add or
                ILOpcode.sub or
                ILOpcode.mul or
                ILOpcode.div or
                ILOpcode.div_un or
                ILOpcode.rem or
                ILOpcode.rem_un or
                ILOpcode.and or
                ILOpcode.or or
                ILOpcode.xor or
                ILOpcode.shl or
                ILOpcode.shr or
                ILOpcode.shr_un or
                ILOpcode.neg or
                ILOpcode.not or
                ILOpcode.conv_i4 or
                ILOpcode.conv_i8 or
                ILOpcode.conv_u4 or
                ILOpcode.conv_u8 or
                ILOpcode.ret;
        }

        private static bool TryMaskNonSemanticDirectories(
            PEReader reader,
            byte[] image,
            out string reason)
        {
            PEHeaders headers = reader.PEHeaders;
            int timestampOffset = checked(headers.CoffHeaderStartOffset + sizeof(uint));
            if ((uint)timestampOffset > (uint)(image.Length - sizeof(uint)))
            {
                reason = "coff-header-location-invalid";
                return false;
            }
            image.AsSpan(timestampOffset, sizeof(uint)).Clear();

            if (headers.PEHeader is not null)
            {
                const int ChecksumOffsetInPEHeader = 64;
                int checksumOffset = checked(headers.PEHeaderStartOffset + ChecksumOffsetInPEHeader);
                if ((uint)checksumOffset > (uint)(image.Length - sizeof(uint)))
                {
                    reason = "pe-checksum-location-invalid";
                    return false;
                }
                image.AsSpan(checksumOffset, sizeof(uint)).Clear();
            }

            if (headers.CorHeader is not null &&
                !TryMaskDirectory(headers, image, headers.CorHeader.StrongNameSignatureDirectory))
            {
                reason = "strong-name-directory-location-invalid";
                return false;
            }
            if (headers.PEHeader is not null &&
                !TryMaskDirectory(headers, image, headers.PEHeader.DebugTableDirectory))
            {
                reason = "debug-directory-location-invalid";
                return false;
            }

            reason = null;
            return true;
        }

        private static bool TryMaskDirectory(
            PEHeaders headers,
            byte[] image,
            DirectoryEntry directory)
        {
            if (directory.Size == 0)
                return true;

            return TryRvaToFileRange(
                headers,
                image,
                directory.RelativeVirtualAddress,
                directory.Size,
                out int offset) &&
                Clear(image, offset, directory.Size);
        }

        private static bool TryMaskMethodBody(
            PEHeaders headers,
            byte[] image,
            int relativeVirtualAddress,
            int size)
        {
            return TryRvaToFileRange(
                headers,
                image,
                relativeVirtualAddress,
                size,
                out int offset) &&
                Clear(image, offset, size);
        }

        private static bool TryRvaToFileRange(
            PEHeaders headers,
            byte[] image,
            int relativeVirtualAddress,
            int size,
            out int fileOffset)
        {
            fileOffset = 0;
            if (relativeVirtualAddress < 0 || size < 0)
                return false;

            foreach (SectionHeader section in headers.SectionHeaders)
            {
                long offsetInSection = (long)relativeVirtualAddress - section.VirtualAddress;
                if (offsetInSection < 0 ||
                    offsetInSection > section.SizeOfRawData ||
                    size > section.SizeOfRawData - offsetInSection)
                {
                    continue;
                }

                long candidate = (long)section.PointerToRawData + offsetInSection;
                if (candidate < 0 ||
                    candidate > image.Length ||
                    size > image.Length - candidate ||
                    candidate > int.MaxValue)
                {
                    return false;
                }

                fileOffset = (int)candidate;
                return true;
            }

            return false;
        }

        private static bool Clear(byte[] image, int offset, int size)
        {
            image.AsSpan(offset, size).Clear();
            return true;
        }

        private sealed class UpdatedMethodIL : MethodIL
        {
            private readonly EcmaMethod _method;
            private readonly MethodIL _original;
            private readonly byte[] _updatedIl;

            internal UpdatedMethodIL(EcmaMethod method, MethodIL original, byte[] updatedIl)
            {
                _method = method;
                _original = original;
                _updatedIl = updatedIl;
            }

            public override MethodDesc OwningMethod => _method;
            public override int MaxStack => _original.MaxStack;
            public override bool IsInitLocals => _original.IsInitLocals;
            public override byte[] GetILBytes() => _updatedIl;
            public override LocalVariableDefinition[] GetLocals() => _original.GetLocals();
            public override ILExceptionRegion[] GetExceptionRegions() => _original.GetExceptionRegions();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior) =>
                _original.GetObject(token, notFoundBehavior);
            public override Internal.IL.MethodDebugInformation GetDebugInfo() => _original.GetDebugInfo();
        }
    }
}
