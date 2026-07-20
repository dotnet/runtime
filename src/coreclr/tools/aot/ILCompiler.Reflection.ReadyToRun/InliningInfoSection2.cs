// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public class InliningInfoSection2
    {
        private readonly ReadyToRunReader _r2r;
        private readonly int _startOffset;
        private readonly int _endOffset;
        private readonly uint _ownerModuleIndex;

        public InliningInfoSection2(ReadyToRunReader reader, int offset, int endOffset)
            : this(reader, offset, endOffset, ownerModuleIndex: 0)
        {
        }

        /// <summary>
        /// Creates a reader for an InliningInfo2 section.
        /// </summary>
        /// <param name="ownerModuleIndex">
        /// Module index (composite-relative) that owns this section's "local" methods
        /// (i.e. methods encoded without a module index). For non-composite images
        /// or the global InliningInfo2 section this is 0; for per-assembly sections
        /// inside a composite image this is <c>assemblyIndex + ComponentAssemblyIndexOffset</c>.
        /// </param>
        public InliningInfoSection2(ReadyToRunReader reader, int offset, int endOffset, uint ownerModuleIndex)
        {
            _r2r = reader;
            _startOffset = offset;
            _endOffset = endOffset;
            _ownerModuleIndex = ownerModuleIndex;
        }

        /// <summary>
        /// A raw inlining entry: one inlinee with its list of inliners.
        /// RIDs are MethodDef row numbers. Module index identifies which component
        /// assembly in a composite image the method belongs to (0 = owner module).
        /// </summary>
        public readonly struct InliningEntry
        {
            public int InlineeRid { get; }
            public uint InlineeModuleIndex { get; }
            public bool InlineeHasModule { get; }
            public IReadOnlyList<(int Rid, uint ModuleIndex, bool HasModule)> Inliners { get; }

            public InliningEntry(int inlineeRid, uint inlineeModuleIndex, bool inlineeHasModule,
                                 IReadOnlyList<(int, uint, bool)> inliners)
            {
                InlineeRid = inlineeRid;
                InlineeModuleIndex = inlineeModuleIndex;
                InlineeHasModule = inlineeHasModule;
                Inliners = inliners;
            }
        }

        /// <summary>
        /// Parses all entries from the InliningInfo2 section.
        /// </summary>
        public List<InliningEntry> GetEntries()
        {
            var entries = new List<InliningEntry>();

            NativeParser parser = new NativeParser(_r2r.ImageReader, (uint)_startOffset);
            NativeHashtable hashtable = new NativeHashtable(_r2r.ImageReader, parser, (uint)_endOffset);

            var enumerator = hashtable.EnumerateAllEntries();

            NativeParser curParser = enumerator.GetNext();
            while (!curParser.IsNull())
            {
                int count = (int)curParser.GetUnsigned();
                int inlineeRidAndFlag = (int)curParser.GetUnsigned();
                count--;
                int inlineeRid = inlineeRidAndFlag >> 1;

                uint inlineeModule = 0;
                bool inlineeHasModule = (inlineeRidAndFlag & 1) != 0;
                if (inlineeHasModule)
                {
                    inlineeModule = curParser.GetUnsigned();
                    count--;
                }

                var inliners = new List<(int, uint, bool)>();
                int currentRid = 0;
                while (count > 0)
                {
                    int inlinerDeltaAndFlag = (int)curParser.GetUnsigned();
                    count--;
                    int inlinerDelta = inlinerDeltaAndFlag >> 1;
                    currentRid += inlinerDelta;

                    uint inlinerModule = 0;
                    bool inlinerHasModule = (inlinerDeltaAndFlag & 1) != 0;
                    if (inlinerHasModule)
                    {
                        inlinerModule = curParser.GetUnsigned();
                        count--;
                    }

                    inliners.Add((currentRid, inlinerModule, inlinerHasModule));
                }

                entries.Add(new InliningEntry(inlineeRid, inlineeModule, inlineeHasModule, inliners));
                curParser = enumerator.GetNext();
            }

            return entries;
        }

        /// <summary>
        /// Returns all inlining pairs with resolved method names.
        /// </summary>
        public IEnumerable<(string InlinerName, string InlineeName)> GetInliningPairs()
        {
            _localMethodMap ??= BuildLocalMethodMap();

            foreach (var entry in GetEntries())
            {
                string inlineeName = ResolveMethod(entry.InlineeRid, entry.InlineeModuleIndex, entry.InlineeHasModule);
                foreach (var (rid, moduleIndex, hasModule) in entry.Inliners)
                {
                    string inlinerName = ResolveMethod(rid, moduleIndex, hasModule);
                    yield return (inlinerName, inlineeName);
                }
            }
        }

        private string ResolveMethod(int rid, uint moduleIndex, bool hasModule)
        {
            if (hasModule)
            {
                string moduleName = TryGetModuleName(moduleIndex);
                return $"{moduleName}!{ResolveMethodInModule(rid, moduleIndex)}";
            }

            if (_localMethodMap.TryGetValue((_ownerModuleIndex, (uint)rid), out string name))
                return name;

            // Fallback: in composite images the same RID may have been recorded under
            // module index 0 when the image's own module is the owner.
            if (_ownerModuleIndex != 0 &&
                _localMethodMap.TryGetValue((0, (uint)rid), out name))
                return name;

            return $"<MethodDef 0x{RidToMethodDef(rid):X8}>";
        }

        private string ResolveMethodInModule(int rid, uint moduleIndex)
        {
            try
            {
                IAssemblyMetadata asmMeta = _r2r.OpenReferenceAssembly((int)moduleIndex);
                if (asmMeta is not null)
                {
                    var mdReader = asmMeta.MetadataReader;
                    var handle = MetadataTokens.MethodDefinitionHandle(rid);
                    if (mdReader.GetTableRowCount(TableIndex.MethodDef) >= rid)
                    {
                        var methodDef = mdReader.GetMethodDefinition(handle);
                        string typeName = "";
                        if (!methodDef.GetDeclaringType().IsNil)
                        {
                            var typeDef = mdReader.GetTypeDefinition(methodDef.GetDeclaringType());
                            typeName = mdReader.GetString(typeDef.Name) + ".";
                        }
                        return typeName + mdReader.GetString(methodDef.Name);
                    }
                }
            }
            catch
            {
                // Fall through to token-based name
            }

            return $"<MethodDef 0x{RidToMethodDef(rid):X8}>";
        }

        private string TryGetModuleName(uint moduleIndex)
        {
            if (moduleIndex == 0)
                return Path.GetFileNameWithoutExtension(_r2r.Filename);

            try
            {
                return _r2r.GetReferenceAssemblyName((int)moduleIndex);
            }
            catch
            {
                return $"<module index {moduleIndex}>";
            }
        }

        private Dictionary<(uint ModuleIndex, uint Rid), string> BuildLocalMethodMap()
        {
            var map = new Dictionary<(uint ModuleIndex, uint Rid), string>();
            for (int assemblyIndex = 0; assemblyIndex < _r2r.ReadyToRunAssemblies.Count; assemblyIndex++)
            {
                uint moduleIndex = _r2r.Composite
                    ? (uint)(assemblyIndex + _r2r.ComponentAssemblyIndexOffset)
                    : 0;

                foreach (var method in _r2r.ReadyToRunAssemblies[assemblyIndex].Methods)
                {
                    if (method.MethodHandle.Kind == HandleKind.MethodDefinition)
                    {
                        uint methodRid = (uint)MetadataTokens.GetRowNumber((MethodDefinitionHandle)method.MethodHandle);
                        map[(moduleIndex, methodRid)] = method.SignatureString;
                    }
                }
            }

            foreach (var instanceEntry in _r2r.InstanceMethods)
            {
                if (instanceEntry.Method.MethodHandle.Kind == HandleKind.MethodDefinition)
                {
                    uint methodRid = (uint)MetadataTokens.GetRowNumber((MethodDefinitionHandle)instanceEntry.Method.MethodHandle);
                    map.TryAdd((0, methodRid), instanceEntry.Method.SignatureString);
                }
            }

            return map;
        }

        private Dictionary<(uint ModuleIndex, uint Rid), string> _localMethodMap;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            NativeParser parser = new NativeParser(_r2r.ImageReader, (uint)_startOffset);
            NativeHashtable hashtable = new NativeHashtable(_r2r.ImageReader, parser, (uint)_endOffset);

            var enumerator = hashtable.EnumerateAllEntries();

            NativeParser curParser = enumerator.GetNext();
            while (!curParser.IsNull())
            {
                int count = (int)curParser.GetUnsigned();
                int inlineeRidAndFlag = (int)curParser.GetUnsigned();
                count--;
                int inlineeToken = RidToMethodDef(inlineeRidAndFlag >> 1);
                if ((inlineeRidAndFlag & 1) != 0)
                {
                    uint module = curParser.GetUnsigned();
                    count--;
                    string moduleName = TryGetModuleName(module);
                    sb.AppendLine($"Inliners for inlinee {inlineeToken:X8} (module {moduleName}):");
                }
                else
                {
                    sb.AppendLine($"Inliners for inlinee {inlineeToken:X8}:");
                }

                int currentRid = 0;
                while (count > 0)
                {
                    int inlinerDeltaAndFlag = (int)curParser.GetUnsigned();
                    count--;
                    int inlinerDelta = inlinerDeltaAndFlag >> 1;
                    currentRid += inlinerDelta;
                    int inlinerToken = RidToMethodDef(currentRid);

                    if ((inlinerDeltaAndFlag & 1) != 0)
                    {
                        uint module = curParser.GetUnsigned();
                        count--;
                        string moduleName = TryGetModuleName(module);
                        sb.AppendLine($"  {inlinerToken:X8} (module {moduleName})");
                    }
                    else
                    {
                        sb.AppendLine($" {inlinerToken:X8}");
                    }

                }

                curParser = enumerator.GetNext();
            }

            return sb.ToString();
        }

        static int RidToMethodDef(int rid) => MetadataTokens.GetToken(MetadataTokens.MethodDefinitionHandle(rid));
    }
}
