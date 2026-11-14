// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

using Internal.JitInterface;
using Internal.Pgo;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public sealed class PgoInstrumentationSchemaTableNode : HeaderTableNode
    {
        public sealed class Entry
        {
            internal Entry(byte[] data)
            {
                Data = data;
                Key = Convert.ToHexString(data);
                Name = Convert.ToHexString(SHA256.HashData(data));
            }

            internal readonly byte[] Data;
            internal readonly string Key;
            internal readonly string Name;
            internal int Index;
        }

        private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
        private readonly Dictionary<(MethodDesc Method, Entry Schema), PrecodeHelperImport> _imports = new();
        private Entry[] _orderedEntries;

        internal static int Layout(Span<PgoInstrumentationSchema> schema)
        {
            int offset = 0;
            foreach (ref PgoInstrumentationSchema record in schema)
            {
                int size = record.InstrumentationKind switch
                {
                    PgoInstrumentationKind.BasicBlockLongCount or PgoInstrumentationKind.MethodEntryLongCount => sizeof(long),
                    PgoInstrumentationKind.NumRuns or PgoInstrumentationKind.Version => 0,
                    _ => throw new NotSupportedException($"Unsupported R2R instrumentation kind: {record.InstrumentationKind}"),
                };
                if (record.Count < 0 || (size != 0 && record.Count == 0))
                    throw new InvalidOperationException($"Invalid R2R instrumentation count: {record.Count}");
                if (size != 0)
                    offset = checked((offset + size - 1) & -size);
                record.Offset = new IntPtr(offset);
                offset = checked(offset + size * record.Count);
            }

            return checked((offset + 7) & -8);
        }

        public Entry Add(ReadOnlySpan<PgoInstrumentationSchema> schema, int dataSize)
        {
            byte[] data = new byte[checked(8 + schema.Length * 20)];
            BinaryPrimitives.WriteInt32LittleEndian(data, schema.Length);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), dataSize);
            for (int i = 0; i < schema.Length; i++)
            {
                PgoInstrumentationSchema record = schema[i];
                Span<byte> output = data.AsSpan(8 + i * 20, 20);
                BinaryPrimitives.WriteUInt32LittleEndian(output, (uint)record.InstrumentationKind);
                BinaryPrimitives.WriteInt32LittleEndian(output.Slice(4), record.ILOffset);
                BinaryPrimitives.WriteInt32LittleEndian(output.Slice(8), record.Count);
                BinaryPrimitives.WriteInt32LittleEndian(output.Slice(12), record.Other);
                BinaryPrimitives.WriteInt32LittleEndian(output.Slice(16), checked((int)record.Offset));
            }

            Entry entry = new(data);
            lock (_entries)
            {
                Debug.Assert(_orderedEntries is null);
                if (!_entries.TryGetValue(entry.Key, out Entry existing))
                {
                    _entries.Add(entry.Key, entry);
                    return entry;
                }

                return existing;
            }
        }

        public PrecodeHelperImport GetImport(NodeFactory factory, MethodWithToken method, Entry schema)
        {
            lock (_entries)
            {
                if (!_imports.TryGetValue((method.Method, schema), out PrecodeHelperImport import))
                {
                    import = new PrecodeHelperImport(factory, new PgoInstrumentationDataSignature(method, this, schema));
                    _imports.Add((method.Method, schema), import);
                }

                return import;
            }
        }

        private void OrderEntries()
        {
            lock (_entries)
            {
                if (_orderedEntries is not null)
                    return;
                _orderedEntries = new Entry[_entries.Count];
                _entries.Values.CopyTo(_orderedEntries, 0);
                Array.Sort(_orderedEntries, (a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
                for (int i = 0; i < _orderedEntries.Length; i++)
                    _orderedEntries[i].Index = i;
            }
        }

        public int GetIndex(Entry entry)
        {
            OrderEntries();
            return entry.Index;
        }

        internal byte[] Encode()
        {
            OrderEntries();
            int offset = checked(8 + (_orderedEntries.Length + 1) * 4);
            int length = offset;
            foreach (Entry entry in _orderedEntries)
                length = checked(length + entry.Data.Length);
            byte[] data = new byte[length];
            BinaryPrimitives.WriteInt32LittleEndian(data, 1);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), _orderedEntries.Length);
            for (int i = 0; i < _orderedEntries.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8 + i * 4), offset);
                _orderedEntries[i].Data.CopyTo(data, offset);
                offset += _orderedEntries[i].Data.Length;
            }
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(8 + _orderedEntries.Length * 4), offset);

            return data;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            return new ObjectData(relocsOnly ? Array.Empty<byte>() : Encode(),
                Array.Empty<Relocation>(), 4, [this]);
        }

        public override int ClassCode => 1887299453;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__PgoInstrumentationSchemas"u8);
        }
    }
}
