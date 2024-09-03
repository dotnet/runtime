using Claunia.PropertyList;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Melanzana.CodeSign.Blobs;
using Melanzana.MachO;
using Melanzana.Streams;
using Melanzana.CodeSign.Requirements;

namespace Melanzana.CodeSign
{
    public class Signer
    {
        private readonly CodeSignOptions codeSignOptions;

        public Signer(CodeSignOptions codeSignOptions)
        {
            this.codeSignOptions = codeSignOptions;
        }

        private string GetTeamId()
        {
            return codeSignOptions.DeveloperCertificate?.GetTeamId() ?? string.Empty;
        }

        /// <summary>
        /// Determine ideal set of hash types necessary for code signing.
        /// </summary>
        /// <remarks>
        /// macOS 10.11.4 and iOS/tvOS 11 have support for SHA-256. If the minimum platform
        /// version specified in the executable is equal or higher than those we only
        /// generate SHA-256. Otherwise we fallback to generating both SHA-1 and SHA-256.
        /// </remarks>
        private static HashType[] GetHashTypesForObjectFile(MachObjectFile objectFile)
        {
            var buildVersion = objectFile.LoadCommands.OfType<MachBuildVersionBase>().FirstOrDefault();
            if (buildVersion != null)
            {
                switch (buildVersion.Platform)
                {
                    case MachPlatform.MacOS:
                        if (buildVersion.MinimumPlatformVersion >= new Version(10, 11, 4))
                            return new[] { HashType.SHA256 };
                        break;
                    case MachPlatform.IOS:
                    case MachPlatform.TvOS:
                        if (buildVersion.MinimumPlatformVersion.Major >= 11)
                            return new[] { HashType.SHA256 };
                        break;
                }
            }
            return new[] { HashType.SHA1, HashType.SHA256 };
        }

        private void SignMachO(string executablePath, string? bundleIdentifier = null, byte[]? resourceSealBytes = null, byte[]? infoPlistBytes = null)
        {
            var teamId = GetTeamId();
            
            bundleIdentifier ??= Path.GetFileName(executablePath);

            byte[]? requirementsBlob = null;
            byte[]? entitlementsBlob = null;
            byte[]? entitlementsDerBlob = null;

            var hashTypesPerArch = new Dictionary<(MachCpuType cpuType, uint cpuSubType), HashType[]>();
            var cdBuildersPerArch = new Dictionary<(MachCpuType cpuType, uint cpuSubType), CodeDirectoryBuilder[]>();

            ExecutableSegmentFlags executableSegmentFlags = 0;

            if (codeSignOptions.DeveloperCertificate != null)
            {
                requirementsBlob = RequirementSet.CreateDefault(
                    bundleIdentifier,
                    codeSignOptions.DeveloperCertificate.GetNameInfo(X509NameType.SimpleName, false)).AsBlob();
            }

            if (codeSignOptions.Entitlements is Entitlements entitlements)
            {
                executableSegmentFlags |= entitlements.GetTaskAllow ? ExecutableSegmentFlags.AllowUnsigned : 0;
                executableSegmentFlags |= entitlements.RunUnsignedCode ? ExecutableSegmentFlags.AllowUnsigned : 0;
                executableSegmentFlags |= entitlements.Debugger ? ExecutableSegmentFlags.Debugger : 0;
                executableSegmentFlags |= entitlements.DynamicCodeSigning ? ExecutableSegmentFlags.Jit : 0;
                executableSegmentFlags |= entitlements.SkipLibraryValidation ? ExecutableSegmentFlags.SkipLibraryValidation : 0;
                executableSegmentFlags |= entitlements.CanLoadCdHash ? ExecutableSegmentFlags.CanLoadCdHash : 0;
                executableSegmentFlags |= entitlements.CanExecuteCdHash ? ExecutableSegmentFlags.CanExecuteCdHash : 0;

                entitlementsBlob = EntitlementsBlob.Create(entitlements);
                entitlementsDerBlob = EntitlementsDerBlob.Create(entitlements);
            }

            var inputFile = File.OpenRead(executablePath);
            var objectFiles = MachReader.Read(inputFile).ToList();
            var codeSignAllocate = new CodeSignAllocate(objectFiles);
            foreach (var objectFile in objectFiles)
            {
                var hashTypes = GetHashTypesForObjectFile(objectFile);
                var cdBuilders = new CodeDirectoryBuilder[hashTypes.Length];
    
                hashTypesPerArch.Add((objectFile.CpuType, objectFile.CpuSubType), hashTypes);
                cdBuildersPerArch.Add((objectFile.CpuType, objectFile.CpuSubType), cdBuilders);

                long signatureSizeEstimate = 18000; // Blob Wrapper (CMS)
                for (int i = 0; i < hashTypes.Length; i++)
                {
                    cdBuilders[i] = new CodeDirectoryBuilder(objectFile, bundleIdentifier, teamId)
                    {
                        HashType = hashTypes[i],
                    };

                    cdBuilders[i].ExecutableSegmentFlags |= executableSegmentFlags;

                    if (codeSignOptions.DeveloperCertificate == null)
                        cdBuilders[i].Flags |= CodeDirectoryFlags.Adhoc;
                    if (requirementsBlob != null)
                        cdBuilders[i].SetSpecialSlotData(CodeDirectorySpecialSlot.Requirements, requirementsBlob);
                    if (entitlementsBlob != null)
                        cdBuilders[i].SetSpecialSlotData(CodeDirectorySpecialSlot.Entitlements, entitlementsBlob);
                    if (entitlementsDerBlob != null)
                        cdBuilders[i].SetSpecialSlotData(CodeDirectorySpecialSlot.EntitlementsDer, entitlementsDerBlob);
                    if (resourceSealBytes != null)
                        cdBuilders[i].SetSpecialSlotData(CodeDirectorySpecialSlot.ResourceDirectory, resourceSealBytes);
                    if (infoPlistBytes != null)
                        cdBuilders[i].SetSpecialSlotData(CodeDirectorySpecialSlot.InfoPlist, infoPlistBytes);

                    signatureSizeEstimate += cdBuilders[i].Size(CodeDirectoryVersion.HighestVersion);
                }

                signatureSizeEstimate += requirementsBlob?.Length ?? 0;
                signatureSizeEstimate += entitlementsBlob?.Length ?? 0;
                signatureSizeEstimate += entitlementsBlob?.Length ?? 0;

                var codeSignatureCommand = objectFile.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
                if (codeSignatureCommand == null ||
                    codeSignatureCommand.FileSize < signatureSizeEstimate)
                {
                    codeSignAllocate.SetArchSize(objectFile, (uint)signatureSizeEstimate);
                }
            }

            string tempFileName = codeSignAllocate.Allocate();
            inputFile.Close();

            // Re-read the object files
            inputFile = File.OpenRead(tempFileName);
            objectFiles = MachReader.Read(inputFile).ToList();
            foreach (var objectFile in objectFiles)
            {
                var hashTypes = hashTypesPerArch[(objectFile.CpuType, objectFile.CpuSubType)];
                var cdBuilders = cdBuildersPerArch[(objectFile.CpuType, objectFile.CpuSubType)];

                var blobs = new List<(CodeDirectorySpecialSlot Slot, byte[] Data)>();
                var codeDirectory = cdBuilders[0].Build(objectFile.GetOriginalStream());

                blobs.Add((CodeDirectorySpecialSlot.CodeDirectory, codeDirectory));
                if (requirementsBlob != null)
                    blobs.Add((CodeDirectorySpecialSlot.Requirements, requirementsBlob));
                if (entitlementsBlob != null)
                    blobs.Add((CodeDirectorySpecialSlot.Entitlements, entitlementsBlob));
                if (entitlementsDerBlob != null)
                    blobs.Add((CodeDirectorySpecialSlot.EntitlementsDer, entitlementsDerBlob));

                var cdHashes = new byte[hashTypes.Length][];
                var hasher = hashTypes[0].GetIncrementalHash();
                hasher.AppendData(codeDirectory);
                cdHashes[0] = hasher.GetHashAndReset();
                for (int i = 1; i < hashTypes.Length; i++)
                {
                    byte[] alternativeCodeDirectory = cdBuilders[i].Build(objectFile.GetOriginalStream());
                    blobs.Add((CodeDirectorySpecialSlot.AlternativeCodeDirectory + i - 1, alternativeCodeDirectory));
                    hasher = hashTypes[i].GetIncrementalHash();
                    hasher.AppendData(alternativeCodeDirectory);
                    cdHashes[i] = hasher.GetHashAndReset();
                }

                var cmsWrapperBlob = CmsWrapperBlob.Create(
                    codeSignOptions.DeveloperCertificate,
                    codeSignOptions.PrivateKey,
                    codeDirectory,
                    hashTypes,
                    cdHashes);
                blobs.Add((CodeDirectorySpecialSlot.CmsWrapper, cmsWrapperBlob));

                /// TODO: Hic sunt leones (all code below)

                // Rewrite LC_CODESIGNATURE data
                var codeSignatureCommand = objectFile.LoadCommands.OfType<MachCodeSignature>().First();

                // FIXME: Adjust the size to match LinkEdit section?
                long size = blobs.Sum(b => b.Data != null ? b.Data.Length + 8 : 0);

                var embeddedSignatureBuffer = new byte[12 + (blobs.Count * 8)];
                BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(0, 4), (uint)BlobMagic.EmbeddedSignature);
                BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(4, 4), (uint)(12 + size));
                BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(8, 4), (uint)blobs.Count);
                int bufferOffset = 12;
                int dataOffset = embeddedSignatureBuffer.Length;
                foreach (var (slot, data) in blobs)
                {
                    BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(bufferOffset, 4), (uint)slot);
                    BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(bufferOffset + 4, 4), (uint)dataOffset);
                    dataOffset += data.Length;
                    bufferOffset += 8;
                }
                uint codeSignatureSize = codeSignatureCommand.FileSize;
                using var codeSignatureStream = codeSignatureCommand.Data.GetWriteStream();
                codeSignatureStream.Write(embeddedSignatureBuffer);
                foreach (var (slot, data) in blobs)
                {
                    codeSignatureStream.Write(data);
                }
                codeSignatureStream.WritePadding(codeSignatureSize - codeSignatureStream.Position);
            }

            using var outputFile = File.OpenWrite(executablePath);
            MachWriter.Write(outputFile, objectFiles);
            inputFile.Close();
        }

        public void SignBundle(Bundle bundle)
        {
            var resourceSeal = BuildResourceSeal(bundle);
            var resourceSealBytes = Encoding.UTF8.GetBytes(resourceSeal.ToXmlPropertyList());

            Directory.CreateDirectory(Path.Combine(bundle.ContentsPath, "_CodeSignature"));
            File.WriteAllBytes(Path.Combine(bundle.ContentsPath, "_CodeSignature", "CodeResources"), resourceSealBytes);

            if (bundle.MainExecutable != null)
            {
                SignMachO(bundle.MainExecutable, bundle.BundleIdentifier, resourceSealBytes, bundle.InfoPlistBytes);
            }
        }

        public void Sign(string path)
        {
            var attributes = File.GetAttributes(path);

            // Assume a directory is a bundle
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                var bundle = new Bundle(path);
                SignBundle(bundle);
            }
            else
            {
                // TODO: Add support for signing regular files, etc.
                SignMachO(path);
            }
        }

        private static NSDictionary BuildResourceRulesPList(IEnumerable<ResourceRule> rules)
        {
            var rulesPList = new NSDictionary();

            foreach (var rule in rules)
            {
                if (rule.Weight == 1 && !rule.IsNested && !rule.IsOmitted && !rule.IsOptional)
                {
                    rulesPList.Add(rule.Pattern, true);
                }
                else
                {
                    var rulePList = new NSDictionary();
                    if (rule.IsOptional)
                        rulePList.Add("optional", true);
                    if (rule.IsNested)
                        rulePList.Add("nested", true);
                    if (rule.IsOmitted)
                        rulePList.Add("omit", true);
                    if (rule.Weight != 1)
                        rulePList.Add("weight", (double)rule.Weight);
                    rulesPList.Add(rule.Pattern, rulePList);
                }
            }

            return rulesPList;
        }

        public NSDictionary BuildResourceSeal(Bundle bundle)
        {
            var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
            var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[65536];

            var resourceBuilder = new ResourceBuilder();
            bundle.AddResourceRules(resourceBuilder, useV2Rules: true);

            var rules2 = BuildResourceRulesPList(resourceBuilder.Rules);
            var files2 = new NSDictionary();
            foreach (var resourceAndRule in resourceBuilder.Scan(bundle.ContentsPath))
            {
                var files2Value = new NSDictionary(2);

                if (resourceAndRule.Rule.IsNested)
                {
                    if (codeSignOptions.Deep)
                    {
                        Sign(resourceAndRule.Info.FullName);
                    }

                    var nestedInfo = ReadNested(resourceAndRule.Info.FullName);
                    if (nestedInfo.HasValue)
                    {
                        files2Value.Add("cdhash", new NSData(nestedInfo.Value.CodeDirectoryHash));
                        if (nestedInfo.Value.DesignatedRequirement != null)
                            files2Value.Add("requirement", nestedInfo.Value.DesignatedRequirement.ToString());
                    }
                }
                else if (resourceAndRule.Info.LinkTarget != null)
                {
                    files2Value.Add("symlink", resourceAndRule.Info.LinkTarget);
                }
                else
                {
                    using (var fileStream = File.OpenRead(resourceAndRule.Info.FullName))
                    {
                        int bytesRead;
                        while ((bytesRead = fileStream.Read(buffer)) > 0)
                        {
                            sha1.AppendData(buffer.AsSpan(0, bytesRead));
                            sha256.AppendData(buffer.AsSpan(0, bytesRead));
                        }
                    }

                    files2Value.Add("hash", new NSData(sha1.GetHashAndReset()));
                    files2Value.Add("hash2", new NSData(sha256.GetHashAndReset()));

                    if (resourceAndRule.Rule.IsOptional)
                    {
                        files2Value.Add("optional", true);
                    }
                }

                files2.Add(resourceAndRule.Path, files2Value);
            };

            // Version 1 resources
            resourceBuilder = new ResourceBuilder();
            bundle.AddResourceRules(resourceBuilder, useV2Rules: false);

            var rules = BuildResourceRulesPList(resourceBuilder.Rules);
            var files = new NSDictionary();
            foreach (var resourceAndRule in resourceBuilder.Scan(bundle.ContentsPath))
            {
                NSData hash;

                Debug.Assert(resourceAndRule.Info is FileInfo);

                if (resourceAndRule.Info.LinkTarget != null)
                {
                    continue;
                }

                if (files2.TryGetValue(resourceAndRule.Path, out var files2Value))
                {
                    hash = (NSData)((NSDictionary)files2Value)["hash"];
                }
                else
                {
                    using (var fileStream = File.OpenRead(resourceAndRule.Info.FullName))
                    {
                        int bytesRead;
                        while ((bytesRead = fileStream.Read(buffer)) > 0)
                        {
                            sha1.AppendData(buffer.AsSpan(0, bytesRead));
                        }
                    }

                    hash = new NSData(sha1.GetHashAndReset());
                }

                if (resourceAndRule.Rule.IsOptional)
                {
                    var filesValue = new NSDictionary(2);
                    filesValue.Add("hash", hash);
                    filesValue.Add("optional", true);
                    files.Add(resourceAndRule.Path, filesValue);
                }
                else
                {
                    files.Add(resourceAndRule.Path, hash);
                }
            }

            // Write down the rules and hashes
            return new NSDictionary { { "files", files }, { "files2", files2 }, { "rules", rules }, { "rules2", rules2 } };
        }

        private static (Requirement? DesignatedRequirement, byte[] CodeDirectoryHash)? ReadNestedMachO(string executablePath)
        {
            using var f = File.OpenRead(executablePath);
            var objectFile = MachReader.Read(f).First();
            var codeSignature = objectFile.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
            if (codeSignature == null)
                return null;

            var codeSignatureStream = objectFile.GetStreamAtFileOffset(codeSignature.FileOffset, codeSignature.FileSize);
            var superBlob = new byte[codeSignature.FileSize];
            codeSignatureStream.ReadFully(superBlob);

            var superBlobMagic = BinaryPrimitives.ReadUInt32BigEndian(superBlob.AsSpan(0, 4));
            var superBlobSize = BinaryPrimitives.ReadUInt32BigEndian(superBlob.AsSpan(4, 4));
            var superBlobCount = BinaryPrimitives.ReadInt32BigEndian(superBlob.AsSpan(8, 4));

            Debug.Assert(superBlobMagic == (uint)BlobMagic.EmbeddedSignature);
            var slotOffsetDictionary = new Dictionary<CodeDirectorySpecialSlot, int>(superBlobCount);

            for (int i = 0; i < superBlobCount; i++)
            {
                var slot = (CodeDirectorySpecialSlot)BinaryPrimitives.ReadUInt32BigEndian(superBlob.AsSpan(i * 8 + 12, 4));
                var slotOffset = BinaryPrimitives.ReadInt32BigEndian(superBlob.AsSpan(i * 8 + 16, 4));
                slotOffsetDictionary[slot] = slotOffset;
            }

            Requirement? designatedRequirement = null;
            if (slotOffsetDictionary.TryGetValue(CodeDirectorySpecialSlot.Requirements, out var requirementsOffset))
            {
                var requirementsSize = BinaryPrimitives.ReadInt32BigEndian(superBlob.AsSpan(requirementsOffset + 4, 4));
                var requirementSet = RequirementSet.FromBlob(superBlob.AsSpan(requirementsOffset, requirementsSize));
                requirementSet.TryGetValue(RequirementType.Designated, out designatedRequirement);
            }

            // FIXME: Get correct CD
            if (slotOffsetDictionary.TryGetValue(CodeDirectorySpecialSlot.AlternativeCodeDirectory, out var codeDirectoryOffset) ||
                slotOffsetDictionary.TryGetValue(CodeDirectorySpecialSlot.CodeDirectory, out codeDirectoryOffset))
            {
                var codeDirectorySize = BinaryPrimitives.ReadInt32BigEndian(superBlob.AsSpan(codeDirectoryOffset + 4, 4));
                var codeDirectoryBlob = superBlob.AsSpan(codeDirectoryOffset, codeDirectorySize);
                var codeDirectoryHeader = CodeDirectoryBaselineHeader.Read(codeDirectoryBlob, out _);
                var hasher = codeDirectoryHeader.HashType.GetIncrementalHash();
                hasher.AppendData(codeDirectoryBlob);
                return (designatedRequirement, hasher.GetHashAndReset().AsSpan(0, 20).ToArray());
            }

            return null;
        }

        private static (Requirement? DesignatedRequirement, byte[] CodeDirectoryHash)? ReadNestedBundle(string bundlePath)
        {
            var bundle = new Bundle(bundlePath);
            return bundle.MainExecutable != null ? ReadNestedMachO(bundle.MainExecutable) : null;
        }

        private static (Requirement? DesignatedRequirement, byte[] CodeDirectoryHash)? ReadNested(string path)
        {
            var attributes = File.GetAttributes(path);

            // Assume a directory is a bundle
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                return ReadNestedBundle(path);
            }
            else
            {
                // TODO: Add support for signing regular files, etc.
                return ReadNestedMachO(path);
            }
        }
    }
}
