// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using Internal.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.ObjectWriter;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler
{
    internal sealed class IncrementalObjectLayout
    {
        private readonly Dictionary<ObjectNode, Entry> _entries;
        private string _failureReason;

        internal IncrementalObjectLayout(IReadOnlyCollection<ObjectNode> nodes)
        {
            _entries = new Dictionary<ObjectNode, Entry>(nodes.Count);
            foreach (ObjectNode node in nodes)
            {
                if (!_entries.TryAdd(node, null))
                {
                    _failureReason = $"The incremental candidate '{node.GetType().Name}' occurs more than once.";
                    break;
                }
            }
        }

        internal string FailureReason => _failureReason;
        internal IEnumerable<Entry> Entries => _entries.Values;

        internal void RecordNode(
            object nodeObject,
            int sectionIndex,
            long sectionOffset,
            object dataObject,
            bool isComdat)
        {
            if (nodeObject is not ObjectNode node || dataObject is not ObjectData data)
            {
                _failureReason ??= "The object writer supplied an unsupported incremental emission record.";
                return;
            }

            if (!_entries.TryGetValue(node, out Entry existing))
                return;

            if (existing is not null)
            {
                _failureReason ??= $"The incremental candidate '{node.GetType().Name}' was emitted more than once.";
                return;
            }

            _entries[node] = new Entry(
                node,
                sectionIndex,
                sectionOffset,
                data.Alignment,
                isComdat,
                (byte[])data.Data.Clone(),
                data.Relocs is null ? Array.Empty<Relocation>() : (Relocation[])data.Relocs.Clone(),
                data.DefinedSymbols is null ?
                    Array.Empty<ISymbolDefinitionNode>() :
                    (ISymbolDefinitionNode[])data.DefinedSymbols.Clone());
        }

        internal void Complete(Func<int, long, int, long?> resolver)
        {
            if (_failureReason is not null)
                return;

            var ranges = new List<Entry>(_entries.Count);
            foreach (KeyValuePair<ObjectNode, Entry> pair in _entries)
            {
                Entry entry = pair.Value;
                if (entry is null)
                {
                    _failureReason = $"The incremental candidate '{pair.Key.GetType().Name}' was not emitted.";
                    return;
                }

                long? fileOffset = resolver(
                    entry.SectionIndex,
                    entry.SectionOffset,
                    entry.Data.Length);
                if (!fileOffset.HasValue)
                {
                    _failureReason = $"The object-file location of '{pair.Key.GetType().Name}' could not be resolved.";
                    return;
                }

                entry.FileOffset = fileOffset.GetValueOrDefault();
                ranges.Add(entry);
            }

            ranges.Sort((left, right) => left.FileOffset.CompareTo(right.FileOffset));
            for (int i = 1; i < ranges.Count; i++)
            {
                Entry previous = ranges[i - 1];
                Entry current = ranges[i];
                if (current.FileOffset < checked(previous.FileOffset + previous.Data.Length))
                {
                    _failureReason = "Incremental candidate object ranges overlap.";
                    return;
                }
            }
        }

        internal bool TryGetEntry(ObjectNode node, out Entry entry) => _entries.TryGetValue(node, out entry);

        internal sealed class Entry
        {
            internal Entry(
                ObjectNode node,
                int sectionIndex,
                long sectionOffset,
                int alignment,
                bool isComdat,
                byte[] data,
                Relocation[] relocations,
                ISymbolDefinitionNode[] definedSymbols)
            {
                Node = node;
                SectionIndex = sectionIndex;
                SectionOffset = sectionOffset;
                Alignment = alignment;
                IsComdat = isComdat;
                Data = data;
                Relocations = relocations;
                DefinedSymbols = definedSymbols;
                RelocationTargetOffsets = new int[relocations.Length];
                for (int i = 0; i < relocations.Length; i++)
                    RelocationTargetOffsets[i] = relocations[i].Target.Offset;

                DefinedSymbolOffsets = new int[definedSymbols.Length];
                for (int i = 0; i < definedSymbols.Length; i++)
                    DefinedSymbolOffsets[i] = definedSymbols[i].Offset;
            }

            internal ObjectNode Node { get; }
            internal int SectionIndex { get; }
            internal long SectionOffset { get; }
            internal int Alignment { get; }
            internal bool IsComdat { get; }
            internal byte[] Data { get; }
            internal Relocation[] Relocations { get; }
            internal int[] RelocationTargetOffsets { get; }
            internal ISymbolDefinitionNode[] DefinedSymbols { get; }
            internal int[] DefinedSymbolOffsets { get; }
            internal long FileOffset { get; set; }
        }
    }

    internal sealed class IncrementalObjectBaseline : IDisposable
    {
        private readonly FileStream _baseline;
        private readonly IncrementalObjectLayout _layout;
        private readonly byte[] _assemblyHash;
        private readonly byte[] _configurationHash;
        private readonly long _length;
        private bool _disposed;

        private IncrementalObjectBaseline(
            FileStream baseline,
            IncrementalObjectLayout layout,
            byte[] assemblyHash,
            byte[] configurationHash,
            long length)
        {
            _baseline = baseline;
            _layout = layout;
            _assemblyHash = (byte[])assemblyHash.Clone();
            _configurationHash = (byte[])configurationHash.Clone();
            _length = length;
        }

        internal static bool TryOpenLocked(
            string objectFilePath,
            IncrementalObjectLayout layout,
            long emittedObjectLength,
            byte[] emittedObjectHash,
            byte[] assemblyHash,
            byte[] configurationHash,
            out IncrementalObjectBaseline baseline,
            out string reason)
        {
            baseline = null;
            reason = null;
            FileStream stream = null;

            if (layout.FailureReason is not null)
            {
                reason = layout.FailureReason;
                return false;
            }

            try
            {
                stream = new FileStream(objectFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream.Length != emittedObjectLength)
                {
                    reason = "The baseline object length changed after emission.";
                    return false;
                }

                byte[] actualHash = SHA256.HashData(stream);
                if (!CryptographicOperations.FixedTimeEquals(actualHash, emittedObjectHash))
                {
                    reason = "The baseline object hash changed after emission.";
                    return false;
                }

                foreach (IncrementalObjectLayout.Entry entry in layout.Entries)
                {
                    if (entry.IsComdat)
                    {
                        reason = "COMDAT method bodies are not supported by incremental object patching.";
                        return false;
                    }

                    if (!TryValidateBaselineFragment(stream, entry, out reason))
                        return false;
                }

                stream.Position = 0;
                baseline = new IncrementalObjectBaseline(
                    stream,
                    layout,
                    assemblyHash,
                    configurationHash,
                    emittedObjectLength);
                stream = null;
                return true;
            }
            catch (Exception ex) when (IsExpectedFileException(ex))
            {
                reason = $"The baseline object could not be locked and verified: {ex.Message}";
                return false;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        internal bool TryStagePatchedObject(
            string outputObjectPath,
            IReadOnlyCollection<ObjectNode> nodes,
            NodeFactory factory,
            byte[] assemblyHash,
            byte[] configurationHash,
            out IncrementalStagedObject stagedObject,
            out long patchedByteCount,
            out string reason)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            stagedObject = null;
            patchedByteCount = 0;
            reason = null;

            if (!CryptographicOperations.FixedTimeEquals(_assemblyHash, assemblyHash) ||
                !CryptographicOperations.FixedTimeEquals(_configurationHash, configurationHash))
            {
                reason = "The baseline assembly or compilation configuration does not match.";
                return false;
            }

            string outputPath = Path.GetFullPath(outputObjectPath);
            if (File.Exists(outputPath))
            {
                reason = "The incremental output path already exists.";
                return false;
            }

            if (!TryBuildPatches(nodes, factory, out List<Patch> patches, out reason))
                return false;

            string directory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                reason = "The incremental output directory does not exist.";
                return false;
            }

            string temporaryPath = Path.Combine(
                directory,
                $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                using (var output = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None))
                {
                    _baseline.Position = 0;
                    _baseline.CopyTo(output);
                    if (output.Length != _length)
                    {
                        reason = "The copied baseline object has an unexpected length.";
                        return false;
                    }

                    foreach (Patch patch in patches)
                    {
                        output.Position = patch.FileOffset;
                        output.Write(patch.Data);
                        patchedByteCount += patch.Data.Length;
                    }

                    if (output.Length != _length)
                    {
                        reason = "Patching changed the object-file length.";
                        return false;
                    }

                    output.Flush(flushToDisk: true);
                }

                stagedObject = new IncrementalStagedObject(temporaryPath, outputPath);
                temporaryPath = null;
                return true;
            }
            catch (Exception ex) when (IsExpectedFileException(ex))
            {
                reason = $"The incremental object could not be staged: {ex.Message}";
                return false;
            }
            finally
            {
                if (temporaryPath is not null)
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (Exception ex) when (IsExpectedFileException(ex))
                    {
                        reason = AppendFailure(
                            reason,
                            $"The staged incremental object could not be deleted: {ex.Message}");
                    }
                }

            }
        }

        internal bool TryWritePatchedObject(
            string outputObjectPath,
            IReadOnlyCollection<ObjectNode> nodes,
            NodeFactory factory,
            byte[] assemblyHash,
            byte[] configurationHash,
            out long patchedByteCount,
            out string reason)
        {
            if (!TryStagePatchedObject(
                outputObjectPath,
                nodes,
                factory,
                assemblyHash,
                configurationHash,
                out IncrementalStagedObject stagedObject,
                out patchedByteCount,
                out reason))
            {
                return false;
            }

            if (stagedObject.TryPublish(out reason))
                return true;

            if (!stagedObject.TryCleanup(out string cleanupFailure))
                reason = AppendFailure(reason, cleanupFailure);
            return false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _baseline.Dispose();
                _disposed = true;
            }
        }

        private bool TryBuildPatches(
            IReadOnlyCollection<ObjectNode> nodes,
            NodeFactory factory,
            out List<Patch> patches,
            out string reason)
        {
            patches = new List<Patch>();
            reason = null;
            var seen = new HashSet<ObjectNode>();

            foreach (ObjectNode node in nodes)
            {
                if (!seen.Add(node))
                {
                    reason = "An incremental object node was supplied more than once.";
                    return false;
                }

                if (!_layout.TryGetEntry(node, out IncrementalObjectLayout.Entry entry) || entry is null)
                {
                    reason = "An incremental object node was not present in the baseline layout.";
                    return false;
                }

                if (entry.IsComdat)
                {
                    reason = "COMDAT method bodies are not supported by incremental object patching.";
                    return false;
                }

                ObjectData current = node.GetData(factory, relocsOnly: false);
                if (current.Alignment != entry.Alignment ||
                    current.Data.Length != entry.Data.Length)
                {
                    reason = "An incremental method changed its object size or alignment.";
                    return false;
                }

                if (!HaveSameSymbols(
                    entry.DefinedSymbols,
                    entry.DefinedSymbolOffsets,
                    current.DefinedSymbols ?? Array.Empty<ISymbolDefinitionNode>()))
                {
                    reason = "An incremental method changed its defined symbols.";
                    return false;
                }

                if (!TryCreateRelocationMask(
                    entry.Data.Length,
                    entry.Relocations,
                    entry.RelocationTargetOffsets,
                    current.Relocs ?? Array.Empty<Relocation>(),
                    entry.Data,
                    current.Data,
                    out bool[] relocationMask,
                    out reason))
                {
                    return false;
                }

                int runStart = -1;
                for (int i = 0; i < current.Data.Length; i++)
                {
                    bool differs = current.Data[i] != entry.Data[i];
                    if (differs && relocationMask[i])
                    {
                        reason = "An incremental method changed a relocation addend.";
                        return false;
                    }

                    if (differs && runStart < 0)
                    {
                        runStart = i;
                    }
                    else if ((!differs || relocationMask[i]) && runStart >= 0)
                    {
                        patches.Add(CreatePatch(entry, current.Data, runStart, i));
                        runStart = -1;
                    }
                }

                if (runStart >= 0)
                    patches.Add(CreatePatch(entry, current.Data, runStart, current.Data.Length));
            }

            patches.Sort((left, right) => left.FileOffset.CompareTo(right.FileOffset));
            for (int i = 1; i < patches.Count; i++)
            {
                Patch previous = patches[i - 1];
                Patch current = patches[i];
                if (current.FileOffset < checked(previous.FileOffset + previous.Data.Length))
                {
                    reason = "Incremental object patches overlap.";
                    return false;
                }
            }

            return true;
        }

        private static Patch CreatePatch(
            IncrementalObjectLayout.Entry entry,
            byte[] data,
            int start,
            int end)
        {
            var bytes = new byte[end - start];
            Array.Copy(data, start, bytes, 0, bytes.Length);
            return new Patch(checked(entry.FileOffset + start), bytes);
        }

        private static bool TryValidateBaselineFragment(
            FileStream stream,
            IncrementalObjectLayout.Entry entry,
            out string reason)
        {
            if (entry.FileOffset < 0 ||
                entry.Data.Length > stream.Length ||
                entry.FileOffset > stream.Length - entry.Data.Length)
            {
                reason = "A recorded incremental object range is outside the baseline object.";
                return false;
            }

            if (!TryCreateRelocationMask(
                entry.Data.Length,
                entry.Relocations,
                entry.RelocationTargetOffsets,
                entry.Relocations,
                entry.Data,
                entry.Data,
                out bool[] relocationMask,
                out reason))
            {
                return false;
            }

            var actual = new byte[entry.Data.Length];
            stream.Position = entry.FileOffset;
            stream.ReadExactly(actual);
            for (int i = 0; i < actual.Length; i++)
            {
                if (!relocationMask[i] && actual[i] != entry.Data[i])
                {
                    reason = "A non-relocation byte in the baseline object does not match recorded object data.";
                    return false;
                }
            }

            return true;
        }

        private static bool TryCreateRelocationMask(
            int dataLength,
            Relocation[] baseline,
            int[] baselineTargetOffsets,
            Relocation[] current,
            byte[] baselineData,
            byte[] currentData,
            out bool[] mask,
            out string reason)
        {
            mask = null;
            reason = null;
            if (baseline.Length != current.Length)
            {
                reason = "An incremental method changed its relocation count.";
                return false;
            }

            mask = new bool[dataLength];
            for (int i = 0; i < baseline.Length; i++)
            {
                Relocation left = baseline[i];
                Relocation right = current[i];
                if (left.RelocType != right.RelocType ||
                    left.Offset != right.Offset ||
                    !ReferenceEquals(left.Target, right.Target) ||
                    baselineTargetOffsets[i] != right.Target.Offset)
                {
                    reason = "An incremental method changed a relocation.";
                    return false;
                }

                if (!TryGetWindowsX64RelocationWidth(left.RelocType, out int width) ||
                    left.Offset < 0 ||
                    left.Offset > dataLength - width)
                {
                    reason = $"Relocation type '{left.RelocType}' or its range is unsupported.";
                    return false;
                }

                for (int j = 0; j < width; j++)
                {
                    int offset = left.Offset + j;
                    if (mask[offset])
                    {
                        reason = "Relocation spans overlap.";
                        return false;
                    }

                    mask[offset] = true;
                    if (baselineData[offset] != currentData[offset])
                    {
                        reason = "An incremental method changed a relocation addend.";
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool TryGetWindowsX64RelocationWidth(RelocType relocType, out int width)
        {
            switch (relocType)
            {
                case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                case RelocType.IMAGE_REL_BASED_ADDR32NB:
                case RelocType.IMAGE_REL_BASED_HIGHLOW:
                case RelocType.IMAGE_REL_BASED_REL32:
                case RelocType.IMAGE_REL_BASED_RELPTR32:
                case RelocType.IMAGE_REL_SECREL:
                    width = 4;
                    return true;

                case RelocType.IMAGE_REL_BASED_DIR64:
                    width = 8;
                    return true;

                default:
                    width = 0;
                    return false;
            }
        }

        private static bool HaveSameSymbols(
            ISymbolDefinitionNode[] baseline,
            int[] baselineOffsets,
            ISymbolDefinitionNode[] current)
        {
            if (baseline.Length != current.Length)
                return false;

            for (int i = 0; i < baseline.Length; i++)
            {
                if (!ReferenceEquals(baseline[i], current[i]) ||
                    baselineOffsets[i] != current[i].Offset)
                    return false;
            }

            return true;
        }

        private static bool IsExpectedFileException(Exception ex) =>
            ex is IOException or
            UnauthorizedAccessException or
            DirectoryNotFoundException or
            PathTooLongException or
            NotSupportedException or
            SecurityException;

        internal static string AppendFailure(string reason, string additionalFailure) =>
            string.IsNullOrEmpty(reason) ? additionalFailure : $"{reason} {additionalFailure}";

        private readonly struct Patch
        {
            internal Patch(long fileOffset, byte[] data)
            {
                FileOffset = fileOffset;
                Data = data;
            }

            internal long FileOffset { get; }
            internal byte[] Data { get; }
        }
    }

    internal sealed class IncrementalStagedObject
    {
        private string _temporaryPath;
        private bool _published;

        internal IncrementalStagedObject(string temporaryPath, string outputPath)
        {
            _temporaryPath = temporaryPath;
            OutputPath = outputPath;
        }

        internal string OutputPath { get; }
        internal string TemporaryPath => _temporaryPath;

        internal bool TryPublish(out string reason)
        {
            reason = null;
            if (_published || _temporaryPath is null)
            {
                reason = "The incremental object is not staged.";
                return false;
            }

            try
            {
                File.Move(_temporaryPath, OutputPath, overwrite: false);
                _temporaryPath = null;
                _published = true;
                return true;
            }
            catch (Exception ex) when (IsExpectedFileException(ex))
            {
                reason = $"The staged incremental object could not be published: {ex.Message}";
                return false;
            }
        }

        internal bool TryCleanup(out string reason)
        {
            reason = null;
            string path = _published ? OutputPath : _temporaryPath;
            if (path is null)
                return true;

            try
            {
                File.Delete(path);
                _temporaryPath = null;
                _published = false;
                return true;
            }
            catch (Exception ex) when (IsExpectedFileException(ex))
            {
                reason = $"The incremental object '{path}' could not be deleted: {ex.Message}";
                return false;
            }
        }

        private static bool IsExpectedFileException(Exception ex) =>
            ex is IOException or
            UnauthorizedAccessException or
            DirectoryNotFoundException or
            PathTooLongException or
            NotSupportedException or
            SecurityException;
    }
}
