// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Managed mirror of NativeFormatWriter.h/.cpp
namespace Internal.NativeFormat
{
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    abstract class Vertex
    {
        internal int _offset = NotPlaced;
        internal int _iteration = -1; // Iteration that the offset is valid for

        internal const int NotPlaced = -1;
        internal const int Placed = -2;
        internal const int Unified = -3;

        public Vertex()
        {
        }

        internal abstract void Save(NativeWriter writer);

        public int VertexOffset
        {
            get
            {
                Debug.Assert(_offset >= 0);
                return _offset;
            }
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class Section
    {
        internal List<Vertex> _items = new List<Vertex>();
        internal Dictionary<Vertex, Vertex> _placedMap = new Dictionary<Vertex, Vertex>();

        public Section()
        {
        }

        public Vertex Place(Vertex vertex)
        {
            if (vertex._offset == Vertex.Unified)
            {
                Vertex placedVertex;
                if (_placedMap.TryGetValue(vertex, out placedVertex))
                    return placedVertex;

                placedVertex = new PlacedVertex(vertex);
                _placedMap.Add(vertex, placedVertex);
                vertex = placedVertex;
            }

            Debug.Assert(vertex._offset == Vertex.NotPlaced);
            vertex._offset = Vertex.Placed;
            _items.Add(vertex);

            return vertex;
        }

        public Vertex Pop()
        {
            Vertex vertex = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            Debug.Assert(vertex._offset == Vertex.Placed);
            vertex._offset = Vertex.NotPlaced;
            return vertex;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class NativeWriter
    {
        private List<Section> _sections = new List<Section>();

        private enum SavePhase
        {
            Initial,
            Shrinking,
            Growing
        }

        private int _iteration;
        private SavePhase _phase; // Current save phase
        private int _offsetAdjustment; // Cumulative offset adjustment compared to previous iteration
        private int _paddingSize; // How much padding was used

        private Dictionary<Vertex, Vertex> _unifier = new Dictionary<Vertex, Vertex>();

        private NativePrimitiveEncoder _encoder;

#if NATIVEFORMAT_COMPRESSION
        struct Tentative
        {
            internal Vertex Vertex;
            internal int PreviousOffset;
        }

        // State used by compression
        List<Tentative> _tentativelyWritten = new List<Tentative>(); // Tentatively written Vertices.
        int _compressionDepth = 0;
#endif

        public NativeWriter()
        {
            _encoder.Init();
        }

        public Section NewSection()
        {
            Section section = new Section();
            _sections.Add(section);
            return section;
        }

        public void WriteByte(byte b) { _encoder.WriteByte(b); }
        public void WriteUInt8(byte value) { _encoder.WriteUInt8(value); }
        public void WriteUInt16(ushort value) { _encoder.WriteUInt16(value); }
        public void WriteUInt32(uint value) { _encoder.WriteUInt32(value); }
        public void WriteUInt64(ulong value) { _encoder.WriteUInt64(value); }
        public void WriteUnsigned(uint d) { _encoder.WriteUnsigned(d); }
        public void WriteSigned(int i) { _encoder.WriteSigned(i); }
        public void WriteUnsignedLong(ulong i) { _encoder.WriteUnsignedLong(i); }
        public void WriteSignedLong(long i) { _encoder.WriteSignedLong(i); }
        public void WriteFloat(float value) { _encoder.WriteFloat(value); }
        public void WriteDouble(double value) { _encoder.WriteDouble(value); }

        public void WritePad(int size)
        {
            while (size > 0)
            {
                _encoder.WriteByte(0);
                size--;
            }
        }

        public bool IsGrowing()
        {
            return _phase == SavePhase.Growing;
        }

        public void UpdateOffsetAdjustment(int offsetDelta)
        {
            switch (_phase)
            {
                case SavePhase.Shrinking:
                    _offsetAdjustment = Math.Min(_offsetAdjustment, offsetDelta);
                    break;
                case SavePhase.Growing:
                    _offsetAdjustment = Math.Max(_offsetAdjustment, offsetDelta);
                    break;
                default:
                    break;
            }
        }

        public void RollbackTo(int offset)
        {
            _encoder.RollbackTo(offset);
        }

        public void RollbackTo(int offset, int offsetAdjustment)
        {
            _offsetAdjustment = offsetAdjustment;
            RollbackTo(offset);
        }

        public void PatchByteAt(int offset, byte value)
        {
            _encoder.PatchByteAt(offset, value);
        }

        // Swallow exceptions if invalid encoding is detected.
        // This is the price we have to pay for using UTF8. Thing like High Surrogate Start Char - '\ud800'
        // can be expressed in UTF-16 (which is the format used to store ECMA metadata), but don't have
        // a representation in UTF-8.
        private static readonly UTF8Encoding s_stringEncoding = new UTF8Encoding(false, false);

        public void WriteString(string s)
        {
            // The actual bytes are only necessary for the final version during the growing phase
            if (IsGrowing())
            {
                byte[] bytes = s_stringEncoding.GetBytes(s);

                _encoder.WriteUnsigned((uint)bytes.Length);
                for (int i = 0; i < bytes.Length; i++)
                    _encoder.WriteByte(bytes[i]);
            }
            else
            {
                int byteCount = s_stringEncoding.GetByteCount(s);
                _encoder.WriteUnsigned((uint)byteCount);
                WritePad(byteCount);
            }
        }

        public void WriteRelativeOffset(Vertex val)
        {
            if (val._iteration == -1)
            {
                // If the offsets are not determined yet, use the maximum possible encoding
                _encoder.WriteSigned(0x7FFFFFFF);
                return;
            }

            int offset = val._offset;

            // If the offset was not update in this iteration yet, adjust it by delta we have accumulated in this iteration so far.
            // This adjustment allows the offsets to converge faster.
            if (val._iteration < _iteration)
                offset += _offsetAdjustment;

            _encoder.WriteSigned(offset - GetCurrentOffset());
        }

        public int GetExpectedOffset(Vertex val)
        {
            Debug.Assert(val._offset != Vertex.NotPlaced);

            if (val._iteration == -1)
            {
                // If the offsets are not determined yet, use the maximum possible encoding
                return 0x7FFFFFFF;
            }

            int offset = val._offset;

            // If the offset was not update in this iteration yet, adjust it by delta we have accumulated in this iteration so far.
            // This adjustment allows the offsets to converge faster.
            if (val._iteration < _iteration)
                offset += _offsetAdjustment;

            return offset;
        }

        public int GetCurrentOffset(Vertex val)
        {
            if (val._iteration != _iteration)
                return -1;

            return val._offset;
        }

        public void SetCurrentOffset(Vertex val)
        {
            val._iteration = _iteration;
            val._offset = GetCurrentOffset();
        }

        public int GetCurrentOffset()
        {
            return _encoder.Size;
        }

        public int GetNumberOfIterations()
        {
            return _iteration;
        }

        public int GetPaddingSize()
        {
            return _paddingSize;
        }

        public void Save(Stream stream)
        {
            _encoder.Clear();

            _phase = SavePhase.Initial;
            foreach (var section in _sections) foreach (var vertex in section._items)
            {
                vertex._offset = GetCurrentOffset();
                vertex._iteration = _iteration;
                vertex.Save(this);

#if NATIVEFORMAT_COMPRESSION
                // Ensure that the compressor state is fully flushed
                Debug.Assert(_TentativelyWritten.Count == 0);
                Debug.Assert(_compressionDepth == 0);
#endif
            }

            // Aggressive phase that only allows offsets to shrink.
            _phase = SavePhase.Shrinking;
            for (; ; )
            {
                _iteration++;
                _encoder.Clear();

                _offsetAdjustment = 0;

                foreach (var section in _sections) foreach (var vertex in section._items)
                {
                    int currentOffset = GetCurrentOffset();

                    // Only allow the offsets to shrink.
                    _offsetAdjustment = Math.Min(_offsetAdjustment, currentOffset - vertex._offset);

                    vertex._offset += _offsetAdjustment;

                    if (vertex._offset < currentOffset)
                    {
                        // It is possible for the encoding of relative offsets to grow during some iterations.
                        // Ignore this growth because of it should disappear during next iteration.
                        RollbackTo(vertex._offset);
                    }
                    Debug.Assert(vertex._offset == GetCurrentOffset());

                    vertex._iteration = _iteration;

                    vertex.Save(this);

#if NATIVEFORMAT_COMPRESSION
                    // Ensure that the compressor state is fully flushed
                    Debug.Assert(_tentativelyWritten.Count == 0);
                    Debug.Assert(_compressionDepth == 0);
#endif
                }

                // We are not able to shrink anymore. We cannot just return here. It is possible that we have rolledback
                // above because of we shrunk too much.
                if (_offsetAdjustment == 0)
                    break;

                // Limit number of shrinking interations. This limit is meant to be hit in corner cases only.
                if (_iteration > 10)
                    break;
            }

            // Conservative phase that only allows the offsets to grow. It is guaranteed to converge.
            _phase = SavePhase.Growing;
            for (; ; )
            {
                _iteration++;
                _encoder.Clear();

                _offsetAdjustment = 0;
                _paddingSize = 0;

                foreach (var section in _sections) foreach (var vertex in section._items)
                {
                    int currentOffset = GetCurrentOffset();

                    // Only allow the offsets to grow.
                    _offsetAdjustment = Math.Max(_offsetAdjustment, currentOffset - vertex._offset);

                    vertex._offset += _offsetAdjustment;

                    if (vertex._offset > currentOffset)
                    {
                        // Padding
                        int padding = vertex._offset - currentOffset;
                        _paddingSize += padding;
                        WritePad(padding);
                    }
                    Debug.Assert(vertex._offset == GetCurrentOffset());

                    vertex._iteration = _iteration;

                    vertex.Save(this);

#if NATIVEFORMAT_COMPRESSION
                    // Ensure that the compressor state is fully flushed
                    Debug.Assert(_tentativelyWritten.Count == 0);
                    Debug.Assert(_compressionDepth == 0);
#endif
                }

                if (_offsetAdjustment == 0)
                {
                    _encoder.Save(stream);
                    return;
                }
            }
        }

        private T Unify<T>(T vertex) where T : Vertex
        {
            Vertex existing;
            if (_unifier.TryGetValue(vertex, out existing))
                return (T)existing;

            Debug.Assert(vertex._offset == Vertex.NotPlaced);
            vertex._offset = Vertex.Unified;
            _unifier.Add(vertex, vertex);

            return vertex;
        }

        public Vertex GetUnsignedConstant(uint value)
        {
            UnsignedConstant vertex = new UnsignedConstant(value);
            return Unify(vertex);
        }

        public Vertex GetTuple(Vertex item1, Vertex item2)
        {
            Tuple vertex = new Tuple(item1, item2);
            return Unify(vertex);
        }

        public Vertex GetTuple(Vertex item1, Vertex item2, Vertex item3)
        {
            Tuple vertex = new Tuple(item1, item2, item3);
            return Unify(vertex);
        }

        public Vertex GetMethodNameAndSigSignature(string name, Vertex signature)
        {
            MethodNameAndSigSignature sig = new MethodNameAndSigSignature(
                GetStringConstant(name),
                GetRelativeOffsetSignature(signature));
            return Unify(sig);
        }

        public Vertex GetStringConstant(string value)
        {
            StringConstant vertex = new StringConstant(value);
            return Unify(vertex);
        }

        public Vertex GetRelativeOffsetSignature(Vertex item)
        {
            RelativeOffsetSignature sig = new RelativeOffsetSignature(item);
            return Unify(sig);
        }

        public Vertex GetOffsetSignature(Vertex item)
        {
            OffsetSignature sig = new OffsetSignature(item);
            return Unify(sig);
        }

        public Vertex GetExternalTypeSignature(uint externalTypeId)
        {
            ExternalTypeSignature sig = new ExternalTypeSignature(externalTypeId);
            return Unify(sig);
        }

        public Vertex GetMethodSignature(uint flags, uint fptrReferenceId, Vertex containingType, Vertex methodNameAndSig, Vertex[] args)
        {
            MethodSignature sig = new MethodSignature(flags, fptrReferenceId, containingType, methodNameAndSig, args);
            return Unify(sig);
        }

        public Vertex GetFieldSignature(Vertex containingType, string name)
        {
            FieldSignature sig = new FieldSignature(containingType, name);
            return Unify(sig);
        }

        public Vertex GetFixupSignature(FixupSignatureKind kind, Vertex signature)
        {
            FixupSignature sig = new FixupSignature(kind, signature);
            return Unify(sig);
        }

        public Vertex GetStaticDataSignature(Vertex type, StaticDataKind staticDataKind)
        {
            StaticDataSignature sig = new StaticDataSignature(type, staticDataKind);
            return Unify(sig);
        }

        public Vertex GetMethodSlotSignature(Vertex type, uint slot)
        {
            MethodSlotSignature sig = new MethodSlotSignature(type, slot);
            return Unify(sig);
        }

        public Vertex GetMethodSigSignature(uint callingConvention, uint genericArgCount, Vertex returnType, Vertex[] parameters)
        {
            MethodSigSignature sig = new MethodSigSignature(callingConvention, genericArgCount, returnType, parameters);
            return Unify(sig);
        }

        public Vertex GetModifierTypeSignature(TypeModifierKind modifier, Vertex param)
        {
            ModifierTypeSignature sig = new ModifierTypeSignature(modifier, param);
            return Unify(sig);
        }

        public Vertex GetVariableTypeSignature(uint index, bool method)
        {
            VariableTypeSignature sig = new VariableTypeSignature(index, method);
            return Unify(sig);
        }

        public Vertex GetInstantiationTypeSignature(Vertex typeDef, Vertex[] arguments)
        {
            InstantiationTypeSignature sig = new InstantiationTypeSignature(typeDef, arguments);
            return Unify(sig);
        }

        public Vertex GetMDArrayTypeSignature(Vertex elementType, uint rank, uint[] bounds, uint[] lowerBounds)
        {
            MDArrayTypeSignature sig = new MDArrayTypeSignature(elementType, rank, bounds, lowerBounds);
            return Unify(sig);
        }

        public Vertex GetFunctionPointerTypeSignature(Vertex methodSignature)
        {
            FunctionPointerTypeSignature sig = new FunctionPointerTypeSignature(methodSignature);
            return Unify(sig);
        }
    }

    internal sealed class PlacedVertex : Vertex
    {
        private Vertex _unified;

        public PlacedVertex(Vertex unified)
        {
            _unified = unified;
        }

        internal override void Save(NativeWriter writer)
        {
            _unified.Save(writer);
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class UnsignedConstant : Vertex
    {
        private uint _value;

        public UnsignedConstant(uint value)
        {
            _value = value;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned(_value);
        }

        public override int GetHashCode()
        {
            return 6659 + ((int)_value) * 19;
        }
        public override bool Equals(object other)
        {
            if (!(other is UnsignedConstant))
                return false;

            UnsignedConstant p = (UnsignedConstant)other;
            if (_value != p._value) return false;
            return true;
        }
    }

    internal sealed class Tuple : Vertex
    {
        private Vertex _item1;
        private Vertex _item2;
        private Vertex _item3;

        public Tuple(Vertex item1, Vertex item2, Vertex item3 = null)
        {
            _item1 = item1;
            _item2 = item2;
            _item3 = item3;
        }

        internal override void Save(NativeWriter writer)
        {
            _item1.Save(writer);
            _item2.Save(writer);
            _item3?.Save(writer);
        }

        public override int GetHashCode()
        {
            int hash = _item1.GetHashCode() * 93481 + _item2.GetHashCode() + 3492;
            if (_item3 != null)
                hash += (hash << 7) + _item3.GetHashCode() * 34987 + 213;
            return hash;
        }

        public override bool Equals(object obj)
        {
            Tuple other = obj as Tuple;
            if (other == null)
                return false;

            return Equals(_item1, other._item1) &&
                Equals(_item2, other._item2) &&
                Equals(_item3, other._item3);
        }
    }

    //
    // Bag of <id, data> pairs. Good for extensible information (e.g. type info)
    //
    // Data can be either relative offset of another vertex, or arbitrary integer.
    //
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class VertexBag : Vertex
    {
        private enum EntryType
        {
            Vertex,
            Unsigned,
            Signed
        }

        private struct Entry
        {
            internal BagElementKind _id;
            internal EntryType _type;
            internal object _value;

            internal Entry(BagElementKind id, Vertex value)
            {
                _id = id;
                _type = EntryType.Vertex;
                _value = value;
            }

            internal Entry(BagElementKind id, uint value)
            {
                _id = id;
                _type = EntryType.Unsigned;
                _value = value;
            }

            internal Entry(BagElementKind id, int value)
            {
                _id = id;
                _type = EntryType.Signed;
                _value = value;
            }
        }

        private List<Entry> _elements;

        public VertexBag()
        {
            _elements = new List<Entry>();
        }

        public void Append(BagElementKind id, Vertex value)
        {
            _elements.Add(new Entry(id, value));
        }

        public void AppendUnsigned(BagElementKind id, uint value)
        {
            _elements.Add(new Entry(id, value));
        }

        public void AppendSigned(BagElementKind id, int value)
        {
            _elements.Add(new Entry(id, value));
        }

        internal override void Save(NativeWriter writer)
        {
            foreach (var elem in _elements)
            {
                writer.WriteUnsigned((uint)elem._id);

                switch (elem._type)
                {
                    case EntryType.Vertex:
                        writer.WriteRelativeOffset((Vertex)elem._value);
                        break;

                    case EntryType.Unsigned:
                        writer.WriteUnsigned((uint)elem._value);
                        break;

                    case EntryType.Signed:
                        writer.WriteSigned((int)elem._value);
                        break;

                }
            }
            writer.WriteUnsigned((uint)BagElementKind.End);
        }

        public int ElementsCount => _elements.Count;
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class VertexSequence : Vertex
    {
        private List<Vertex> _elements;

        public VertexSequence()
        {
            _elements = new List<Vertex>();
        }

        public void Append(Vertex vertex)
        {
            _elements.Add(vertex);
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)_elements.Count);
            foreach (var elem in _elements)
                elem.Save(writer);
        }

        public override bool Equals(object obj)
        {
            var other = obj as VertexSequence;
            if (other == null || other._elements.Count != _elements.Count)
                return false;

            for (int i = 0; i < _elements.Count; i++)
                if (!Equals(_elements[i], other._elements[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 13;
            foreach (var element in _elements)
            {
                int value = (element != null ? element.GetHashCode() : 0) * 0x5498341 + 0x832424;
                hashCode = hashCode * 31 + value;
            }

            return hashCode;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class MethodNameAndSigSignature : Vertex
    {
        private Vertex _methodName;
        private Vertex _signature;

        public MethodNameAndSigSignature(Vertex methodName, Vertex signature)
        {
            _methodName = methodName;
            _signature = signature;
        }

        internal override void Save(NativeWriter writer)
        {
            _methodName.Save(writer);
            _signature.Save(writer);
        }

        public override int GetHashCode()
        {
            return 509 * 197 + _methodName.GetHashCode() + 647 * _signature.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            MethodNameAndSigSignature other = obj as MethodNameAndSigSignature;
            if (other == null)
                return false;

            return Equals(_methodName, other._methodName) && Equals(_signature, other._signature);
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class StringConstant : Vertex
    {
        private string _value;

        public StringConstant(string value)
        {
            _value = value;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteString(_value);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            StringConstant other = obj as StringConstant;
            if (other == null)
                return false;

            return _value == other._value;
        }
    }

    //
    // Performs indirection to an existing native layout signature by writing out the
    // relative offset.
    //
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class RelativeOffsetSignature : Vertex
    {
        private Vertex _item;

        public RelativeOffsetSignature(Vertex item)
        {
            _item = item;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteRelativeOffset(_item);
        }

        public override int GetHashCode()
        {
            return _item.GetHashCode() >> 3;
        }

        public override bool Equals(object obj)
        {
            RelativeOffsetSignature other = obj as RelativeOffsetSignature;
            if (other == null)
                return false;

            return Equals(_item, other._item);
        }
    }

    //
    // Performs indirection to an existing native layout signature using offset from the
    // beginning of the native format. This allows cross-native layout references. You must
    // ensure that the native layout writer of the pointee is saved before that of the pointer
    // so the offsets are locked down.
    //
#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class OffsetSignature : Vertex
    {
        private Vertex _item;

        public OffsetSignature(Vertex item)
        {
            _item = item;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)_item.VertexOffset);
        }

        public override int GetHashCode()
        {
            return _item.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            OffsetSignature other = obj as OffsetSignature;
            if (other == null)
                return false;

            return Equals(_item, other._item);
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class ExternalTypeSignature : Vertex
    {
        private uint _externalTypeId;

        public ExternalTypeSignature(uint externalTypeId)
        {
            _externalTypeId = externalTypeId;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)TypeSignatureKind.External | (_externalTypeId << 4));
        }

        public override int GetHashCode()
        {
            return 32439 + 11 * (int)_externalTypeId;
        }

        public override bool Equals(object obj)
        {
            ExternalTypeSignature other = obj as ExternalTypeSignature;
            if (other == null)
                return false;

            return _externalTypeId == other._externalTypeId;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class MethodSignature : Vertex
    {
        private uint _flags;
        private uint _fptrReferenceId;
        private Vertex _containingType;
        private Vertex _methodNameAndSig;
        private Vertex[] _args;

        public MethodSignature(uint flags, uint fptrReferenceId, Vertex containingType, Vertex methodNameAndSig, Vertex[] args)
        {
            _flags = flags;
            _fptrReferenceId = fptrReferenceId;
            _containingType = containingType;
            _methodNameAndSig = methodNameAndSig;
            _args = args;

            if ((flags & (uint)MethodFlags.HasInstantiation) != 0)
                Debug.Assert(args != null && args.Length > 0);
            if ((flags & (uint)MethodFlags.HasFunctionPointer) == 0)
                Debug.Assert(fptrReferenceId == 0);
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned(_flags);
            if ((_flags & (uint)MethodFlags.HasFunctionPointer) != 0)
                writer.WriteUnsigned(_fptrReferenceId);
            _containingType.Save(writer);
            _methodNameAndSig.Save(writer);
            if ((_flags & (uint)MethodFlags.HasInstantiation) != 0)
            {
                writer.WriteUnsigned((uint)_args.Length);
                for (uint iArg = 0; _args != null && iArg < _args.Length; iArg++)
                    _args[iArg].Save(writer);
            }
        }

        public override int GetHashCode()
        {
            int hash = _args != null ? _args.Length : 0;
            hash += (hash << 5) + (int)_flags * 23;
            hash += (hash << 5) + (int)_fptrReferenceId * 119;
            hash += (hash << 5) + _containingType.GetHashCode();
            for (uint iArg = 0; _args != null && iArg < _args.Length; iArg++)
                hash += (hash << 5) + _args[iArg].GetHashCode();
            hash += (hash << 5) + _methodNameAndSig.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            MethodSignature other = obj as MethodSignature;
            if (other == null)
                return false;

            if (!(
                _flags == other._flags &&
                _fptrReferenceId == other._fptrReferenceId &&
                Equals(_containingType, other._containingType) &&
                Equals(_methodNameAndSig, other._methodNameAndSig)))
            {
                return false;
            }

            if (_args != null)
            {
                if (other._args == null) return false;
                if (other._args.Length != _args.Length) return false;
                for (uint iArg = 0; _args != null && iArg < _args.Length; iArg++)
                    if (!Equals(_args[iArg], other._args[iArg]))
                        return false;
            }
            else if (other._args != null)
                return false;

            return true;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class FieldSignature : Vertex
    {
        private Vertex _containingType;
        private string _name;

        public FieldSignature(Vertex containingType, string name)
        {
            _containingType = containingType;
            _name = name;
        }

        internal override void Save(NativeWriter writer)
        {
            _containingType.Save(writer);
            writer.WriteString(_name);
        }

        public override int GetHashCode()
        {
            int hash = 113 + 97 * _containingType.GetHashCode();
            foreach (char c in _name)
                hash += (hash << 5) + c * 19;

            return hash;
        }

        public override bool Equals(object obj)
        {
            var other = obj as FieldSignature;
            if (other == null)
                return false;

            if (!Equals(other._containingType, _containingType))
                return false;

            if (!Equals(other._name, _name))
                return false;

            return true;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class FixupSignature : Vertex
    {
        private FixupSignatureKind _kind;
        private Vertex _signature;

        public FixupSignature(FixupSignatureKind kind, Vertex signature)
        {
            _kind = kind;
            _signature = signature;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)_kind);
            _signature?.Save(writer);
        }

        public override int GetHashCode()
        {
            return 53345 + 97 * (int)_kind + ((_signature != null) ? _signature.GetHashCode() : 0);
        }

        public override bool Equals(object obj)
        {
            var other = obj as FixupSignature;
            if (other == null)
                return false;

            if (other._kind != _kind)
                return false;

            if (!Equals(other._signature, _signature))
                return false;

            return true;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class StaticDataSignature : Vertex
    {
        private Vertex _type;
        private StaticDataKind _staticDataKind;

        public StaticDataSignature(Vertex type, StaticDataKind staticDataKind)
        {
            _type = type;
            _staticDataKind = staticDataKind;
        }

        internal override void Save(NativeWriter writer)
        {
            _type.Save(writer);
            writer.WriteUnsigned((uint)_staticDataKind);
        }

        public override int GetHashCode()
        {
            return 456789 + 101 * (int)_staticDataKind + _type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as StaticDataSignature;
            if (other == null)
                return false;

            if (!Equals(other._type, _type))
                return false;

            if (other._staticDataKind != _staticDataKind)
                return false;

            return true;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class MethodSlotSignature : Vertex
    {
        private Vertex _type;
        private uint _slot;

        public MethodSlotSignature(Vertex type, uint slot)
        {
            _type = type;
            _slot = slot;
        }

        internal override void Save(NativeWriter writer)
        {
            _type.Save(writer);
            writer.WriteUnsigned(_slot);
        }

        public override int GetHashCode()
        {
            return 124121 + 47 * (int)_slot + _type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as MethodSlotSignature;
            if (other == null)
                return false;

            if (!Equals(other._type, _type))
                return false;

            if (other._slot != _slot)
                return false;

            return true;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class MethodSigSignature : Vertex
    {
        private uint _callingConvention;
        private uint _genericArgCount;
        private Vertex _returnType;
        private Vertex[] _parameters;

        public MethodSigSignature(uint callingConvention, uint genericArgCount, Vertex returnType, Vertex[] parameters)
        {
            _callingConvention = callingConvention;
            _returnType = returnType;
            _genericArgCount = genericArgCount;
            _parameters = parameters;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned(_callingConvention);

            // Signatures omit the generic type parameter count for non-generic methods
            if (_genericArgCount > 0)
                writer.WriteUnsigned(_genericArgCount);

            writer.WriteUnsigned((uint)_parameters.Length);

            _returnType.Save(writer);

            foreach (var p in _parameters)
                p.Save(writer);
        }

        public override int GetHashCode()
        {
            int hash = 317 + 709 * (int)_callingConvention + 953 * (int)_genericArgCount + 31 * _returnType.GetHashCode();
            foreach (var p in _parameters)
                hash += (hash << 5) + p.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            MethodSigSignature other = obj as MethodSigSignature;
            if (other == null)
                return false;

            if (!(
                _callingConvention == other._callingConvention &&
                _genericArgCount == other._genericArgCount &&
                _parameters.Length == other._parameters.Length &&
                Equals(_returnType, other._returnType)))
            {
                return false;
            }

            for (int i = 0; i < _parameters.Length; i++)
                if (!Equals(_parameters[i], other._parameters[i]))
                    return false;

            return true;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class ModifierTypeSignature : Vertex
    {
        private TypeModifierKind _modifier;
        private Vertex _param;

        public ModifierTypeSignature(TypeModifierKind modifier, Vertex param)
        {
            _modifier = modifier;
            _param = param;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)TypeSignatureKind.Modifier | ((uint)_modifier << 4));
            _param.Save(writer);
        }

        public override int GetHashCode()
        {
            return 432981 + 37 * (int)_modifier + _param.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            ModifierTypeSignature other = obj as ModifierTypeSignature;
            if (other == null)
                return false;

            return _modifier == other._modifier && Equals(_param, other._param);
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class VariableTypeSignature : Vertex
    {
        private uint _variableId;

        public VariableTypeSignature(uint index, bool method)
        {
            _variableId = (index << 1) | (method ? (uint)1 : 0);
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)TypeSignatureKind.Variable | (_variableId << 4));
        }

        public override int GetHashCode()
        {
            return 6093 + 7 * (int)_variableId;
        }

        public override bool Equals(object obj)
        {
            VariableTypeSignature other = obj as VariableTypeSignature;
            if (other == null)
                return false;

            return _variableId == other._variableId;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class InstantiationTypeSignature : Vertex
    {
        private Vertex _typeDef;
        private Vertex[] _args;

        public InstantiationTypeSignature(Vertex typeDef, Vertex[] args)
        {
            _typeDef = typeDef;
            _args = args;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)TypeSignatureKind.Instantiation | ((uint)_args.Length << 4));
            _typeDef.Save(writer);
            for (int iArg = 0; iArg < _args.Length; iArg++)
                _args[iArg].Save(writer);
        }

        public override int GetHashCode()
        {
            int hash = _args.Length;

            hash += (hash << 5) + _typeDef.GetHashCode();
            for (int iArg = 0; iArg < _args.Length; iArg++)
                hash += (hash << 5) + _args[iArg].GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            InstantiationTypeSignature other = obj as InstantiationTypeSignature;
            if (other == null)
                return false;

            if (_args.Length != other._args.Length || !Equals(_typeDef, other._typeDef))
                return false;

            for (uint iArg = 0; iArg < _args.Length; iArg++)
                if (!Equals(_args[iArg], other._args[iArg]))
                    return false;

            return true;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class MDArrayTypeSignature : Vertex
    {
        private Vertex _arrayElementType;
        private uint _rank;
        private uint[] _bounds;
        private uint[] _lowerBounds;

        public MDArrayTypeSignature(Vertex arrayElementType, uint rank, uint[] bounds, uint[] lowerBounds)
        {
            Debug.Assert(bounds != null && lowerBounds != null);

            _arrayElementType = arrayElementType;
            _rank = rank;
            _bounds = bounds;
            _lowerBounds = lowerBounds;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)TypeSignatureKind.MultiDimArray | ((uint)_rank << 4));
            _arrayElementType.Save(writer);

            writer.WriteUnsigned((uint)_bounds.Length);
            foreach (uint b in _bounds)
                writer.WriteUnsigned(b);

            writer.WriteUnsigned((uint)_lowerBounds.Length);
            foreach (uint b in _lowerBounds)
                writer.WriteUnsigned(b);
        }

        public override int GetHashCode()
        {
            int hash = 79 + 971 * (int)_rank + 83 * _arrayElementType.GetHashCode();

            foreach (uint b in _bounds)
                hash += (hash << 5) + (int)b * 19;

            foreach (uint b in _lowerBounds)
                hash += (hash << 5) + (int)b * 19;

            return hash;
        }

        public override bool Equals(object obj)
        {
            MDArrayTypeSignature other = obj as MDArrayTypeSignature;
            if (other == null)
                return false;

            if (!Equals(_arrayElementType, other._arrayElementType) ||
                _rank != other._rank ||
                _bounds.Length != other._bounds.Length ||
                _lowerBounds.Length != other._lowerBounds.Length)
            {
                return false;
            }
            for (int i = 0; i < _bounds.Length; i++)
            {
                if (_bounds[i] != other._bounds[i])
                    return false;
            }
            for (int i = 0; i < _lowerBounds.Length; i++)
            {
                if (_lowerBounds[i] != other._lowerBounds[i])
                    return false;
            }

            return true;
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class FunctionPointerTypeSignature : Vertex
    {
        private Vertex _methodSignature;

        public FunctionPointerTypeSignature(Vertex methodSignature)
        {
            _methodSignature = methodSignature;
        }

        internal override void Save(NativeWriter writer)
        {
            writer.WriteUnsigned((uint)TypeSignatureKind.FunctionPointer);
            _methodSignature.Save(writer);
        }

        public override int GetHashCode()
        {
            return _methodSignature.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is FunctionPointerTypeSignature fnptrSig && _methodSignature.Equals(fnptrSig._methodSignature);
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class BlobVertex : Vertex
    {
        private byte[] _data;

        public BlobVertex(byte[] data)
        {
            _data = data;
        }

        public int GetSize()
        {
            return _data.Length;
        }

        internal override void Save(NativeWriter writer)
        {
            foreach (byte b in _data)
            {
                writer.WriteByte(b);
            }
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class EntryPointVertex : Vertex
    {
        private uint _methodIndex;

        private BlobVertex _fixups;

        public EntryPointVertex(uint methodIndex, BlobVertex fixups)
        {
            _methodIndex = methodIndex;
            _fixups = fixups;
        }

        internal override void Save(NativeWriter writer)
        {
            if (_fixups != null)
            {
                int existingOffset = writer.GetCurrentOffset(_fixups);
                if (existingOffset != -1)
                {
                    writer.WriteUnsigned((_methodIndex << 2) | 3);
                    writer.WriteUnsigned((uint)(writer.GetCurrentOffset() - existingOffset));
                }
                else
                {
                    writer.WriteUnsigned((_methodIndex << 2) | 1);
                    writer.SetCurrentOffset(_fixups);
                    _fixups.Save(writer);
                }
            }
            else
            {
                writer.WriteUnsigned(_methodIndex << 1);
            }
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class EntryPointWithBlobVertex : EntryPointVertex
    {
        private BlobVertex _blob;

        public EntryPointWithBlobVertex(uint methodIndex, BlobVertex fixups, BlobVertex blob)
            : base(methodIndex, fixups)
        {
            _blob = blob;
        }

        internal override void Save(NativeWriter writer)
        {
            _blob.Save(writer);
            base.Save(writer);
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class PgoInstrumentedDataVertex : Vertex
    {
        private uint _version;

        private BlobVertex _instrumentationData;

        public PgoInstrumentedDataVertex(uint version, BlobVertex instrumentationData)
        {
            _version = version;
            _instrumentationData = instrumentationData;
        }

        internal override void Save(NativeWriter writer)
        {
            int existingOffset = _instrumentationData._offset;
            if (existingOffset != -1)
            {
                writer.WriteUnsigned((_version << 2) | 3);
                writer.WriteUnsigned((uint)(writer.GetCurrentOffset() - existingOffset));
            }
            else
            {
                writer.WriteUnsigned((_version << 2) | 1);
                _instrumentationData.Save(writer);
            }
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class PgoInstrumentedDataWithSignatureBlobVertex : PgoInstrumentedDataVertex
    {
        private BlobVertex _signatureBlob;

        public PgoInstrumentedDataWithSignatureBlobVertex(BlobVertex signaureBlob, uint version, BlobVertex instrumentationData)
            : base(version, instrumentationData)
        {
            _signatureBlob = signaureBlob;
        }

        internal override void Save(NativeWriter writer)
        {
            _signatureBlob.Save(writer);
            base.Save(writer);
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class DebugInfoVertex : Vertex
    {
        private BlobVertex _debugInfo;

        public DebugInfoVertex(BlobVertex debugInfo)
        {
            _debugInfo = debugInfo;
        }

        internal override void Save(NativeWriter writer)
        {
            int existingOffset = writer.GetCurrentOffset(_debugInfo);
            if (existingOffset != -1)
            {
                Debug.Assert(writer.GetCurrentOffset() > existingOffset);
                writer.WriteUnsigned((uint)(writer.GetCurrentOffset() - existingOffset));
            }
            else
            {
                writer.WriteUnsigned(0);
                writer.SetCurrentOffset(_debugInfo);
                _debugInfo.Save(writer);
            }
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class VertexArray : Vertex
    {
        private const int BlockSize = 16;

        private Section _section;

        private List<Vertex> _entries;

        private List<Vertex> _blocks;

        /// <summary>
        /// Current size of index entry: 0 - uint8, 1 - uint16, 2 - uint32
        /// </summary>
        private uint _entryIndexSize;

        private sealed class VertexLeaf : Vertex
        {
            private Vertex _vertex;
            private int _leafIndex;

            public VertexLeaf(Vertex vertex, int leafIndex)
            {
                _vertex = vertex;
                _leafIndex = leafIndex;
            }

            internal override void Save(NativeWriter writer)
            {
                writer.WriteUnsigned((uint)_leafIndex << 2);

                _vertex?.Save(writer);
            }
        }

        private sealed class VertexTree : Vertex
        {
            private Vertex _first;
            private Vertex _second;

            public VertexTree()
            {
                _first = null;
                _second = null;
            }

            public VertexTree(Vertex first, Vertex second)
            {
                _first = first;
                _second = second;
            }

            public void Update(Vertex first, Vertex second)
            {
                _first = first;
                _second = second;
            }

            internal override void Save(NativeWriter writer)
            {
                uint value = (_first != null ? 1u : 0u);

                if (_second != null)
                {
                    value |= 2;

                    int delta = writer.GetExpectedOffset(_second) - writer.GetCurrentOffset();
                    Debug.Assert(delta >= 0);
                    value |= ((uint)delta << 2);
                }

                writer.WriteUnsigned(value);

                _first?.Save(writer);
            }
        }

        public VertexArray(Section section)
        {
            _section = section;
            _entries = new List<Vertex>();
            _blocks = new List<Vertex>();
            _entryIndexSize = 0;
        }

        private Vertex ExpandBlock(int index, int depth, bool place, out bool isLeaf)
        {
            if (depth == 1)
            {
                Vertex first = (index < _entries.Count ? _entries[index] : null);
                Vertex second = (index + 1 < _entries.Count ? _entries[index + 1] : null);

                if (first == null && second == null)
                {
                    isLeaf = true;
                    return null;
                }

                if (first == null || second == null)
                {
                    VertexLeaf leaf = new VertexLeaf(
                        first ?? second,
                        (first == null ? index + 1 : index) & (BlockSize - 1));

                    if (place)
                    {
                        _section.Place(leaf);
                    }

                    isLeaf = true;
                    return leaf;
                }

                VertexTree tree = new VertexTree(first, second);
                if (place)
                    _section.Place(tree);

                _section.Place(second);

                isLeaf = false;
                return tree;
            }
            else
            {
                VertexTree tree = new VertexTree();
                if (place)
                    _section.Place(tree);

                bool firstIsLeaf;
                Vertex first = ExpandBlock(index, depth - 1, false, out firstIsLeaf);

                bool secondIsLeaf;
                Vertex second = ExpandBlock(index + (1 << (depth - 1)), depth - 1, true, out secondIsLeaf);

                if (first == null && second == null)
                {
                    if (place)
                    {
                        Vertex pop = _section.Pop();
                        Debug.Assert(pop == tree);
                    }
                    isLeaf = true;
                    return null;
                }

                if (first == null && secondIsLeaf)
                {
                    Vertex pop = _section.Pop();
                    Debug.Assert(pop == second);
                    if (place)
                    {
                        pop = _section.Pop();
                        Debug.Assert(pop == tree);
                        _section.Place(second);
                    }

                    isLeaf = true;
                    return second;
                }

                if (second == null && firstIsLeaf)
                {
                    if (place)
                    {
                        Vertex pop = _section.Pop();
                        Debug.Assert(pop == tree);
                        _section.Place(first);
                    }

                    isLeaf = true;
                    return first;
                }

                tree.Update(first, second);
                isLeaf = false;
                return tree;
            }
        }

        public void Set(int index, Vertex element)
        {
            while (index >= _entries.Count)
                _entries.Add(null);

            _entries[index] = element;
        }

        public void ExpandLayout()
        {
            VertexLeaf nullBlock = null;
            for (int i = 0; i < _entries.Count; i += BlockSize)
            {
                Vertex block = ExpandBlock(i, 4, true, out _);

                if (block == null)
                {
                    if (nullBlock == null)
                    {
                        nullBlock = new VertexLeaf(vertex: null, leafIndex: BlockSize);
                        _section.Place(nullBlock);
                    }
                    block = nullBlock;
                }

                _blocks.Add(block);
            }

            // Start with maximum size entries
            _entryIndexSize = 2;
        }

        internal override void Save(NativeWriter writer)
        {
            // Lowest two bits are entry index size, the rest is number of elements
            writer.WriteUnsigned(((uint)_entries.Count << 2) | _entryIndexSize);

            int blocksOffset = writer.GetCurrentOffset();
            int maxOffset = 0;

            foreach (Vertex block in _blocks)
            {
                int offset = writer.GetExpectedOffset(block) - blocksOffset;
                Debug.Assert(offset >= 0);

                maxOffset = Math.Max(offset, maxOffset);

                if (_entryIndexSize == 0)
                {
                    writer.WriteByte((byte)offset);
                }
                else
                if (_entryIndexSize == 1)
                {
                    writer.WriteUInt16((ushort)offset);
                }
                else
                {
                    writer.WriteUInt32((uint)offset);
                }
            }

            uint newEntryIndexSize = 0;
            if (maxOffset > 0xFF)
            {
                newEntryIndexSize++;
                if (maxOffset > 0xFFFF)
                    newEntryIndexSize++;
            }

            if (writer.IsGrowing())
            {
                if (newEntryIndexSize > _entryIndexSize)
                {
                    // Ensure that the table will be redone with new entry index size
                    writer.UpdateOffsetAdjustment(1);

                    _entryIndexSize = newEntryIndexSize;
                }
            }
            else
            {
                if (newEntryIndexSize < _entryIndexSize)
                {
                    // Ensure that the table will be redone with new entry index size
                    writer.UpdateOffsetAdjustment(-1);

                    _entryIndexSize = newEntryIndexSize;
                }
            }
        }
    }

#if NATIVEFORMAT_PUBLICWRITER
    public
#else
    internal
#endif
    class VertexHashtable : Vertex
    {
        private struct Entry
        {
            public Entry(uint hashcode, Vertex vertex)
            {
                Offset = 0;
                Hashcode = hashcode;
                Vertex = vertex;
            }

            public int Offset;

            public uint Hashcode;
            public Vertex Vertex;

            public static int Comparison(Entry a, Entry b)
            {
                return (int)(a.Hashcode /*& mask*/) - (int)(b.Hashcode /*& mask*/);
            }
        }

        private List<Entry> _Entries;

        // How many entries to target per bucket. Higher fill factor means smaller size, but worse runtime perf.
        private int _nFillFactor;

        // Number of buckets chosen for the table. Must be power of two. 0 means that the table is still open for mutation.
        private uint _nBuckets;

        // Current size of index entry
        private int _entryIndexSize; // 0 - uint8, 1 - uint16, 2 - uint32

        public const int DefaultFillFactor = 13;

        public VertexHashtable(int fillFactor = DefaultFillFactor)
        {
            _Entries = new List<Entry>();
            _nFillFactor = fillFactor;
            _nBuckets = 0;
            _entryIndexSize = 0;
        }

        public void Append(uint hashcode, Vertex element)
        {
            // The table needs to be open for mutation
            Debug.Assert(_nBuckets == 0);

            _Entries.Add(new Entry(hashcode, element));
        }

        // Returns 1 + log2(x) rounded up, 0 iff x == 0
        private static int HighestBit(uint x)
        {
            int ret = 0;
            while (x != 0)
            {
                x >>= 1;
                ret++;
            }
            return ret;
        }

        // Helper method to back patch entry index in the bucket table
        private static void PatchEntryIndex(NativeWriter writer, int patchOffset, int entryIndexSize, int entryIndex)
        {
            if (entryIndexSize == 0)
            {
                writer.PatchByteAt(patchOffset, (byte)entryIndex);
            }
            else if (entryIndexSize == 1)
            {
                writer.PatchByteAt(patchOffset, (byte)entryIndex);
                writer.PatchByteAt(patchOffset + 1, (byte)(entryIndex >> 8));
            }
            else
            {
                writer.PatchByteAt(patchOffset, (byte)entryIndex);
                writer.PatchByteAt(patchOffset + 1, (byte)(entryIndex >> 8));
                writer.PatchByteAt(patchOffset + 2, (byte)(entryIndex >> 16));
                writer.PatchByteAt(patchOffset + 3, (byte)(entryIndex >> 24));
            }
        }

        private void ComputeLayout()
        {
            uint bucketsEstimate = (uint)(_Entries.Count / _nFillFactor);

            // Round number of buckets up to the power of two
            _nBuckets = (uint)(1 << HighestBit(bucketsEstimate));

            // Lowest byte of the hashcode is used for lookup within the bucket. Keep it sorted too so that
            // we can use the ordering to terminate the lookup prematurely.
            uint mask = ((_nBuckets - 1) << 8) | 0xFF;

            // Sort by hashcode. This sort must be stable since we need determinism even if two entries have
            // the same hashcode. This is deterministic if entries are added in a deterministic order.
            _Entries = _Entries.OrderBy(entry => entry.Hashcode & mask).ToList();

            // Start with maximum size entries
            _entryIndexSize = 2;
        }

        internal override void Save(NativeWriter writer)
        {
            // Compute the layout of the table if we have not done it yet
            if (_nBuckets == 0)
                ComputeLayout();

            int nEntries = _Entries.Count;
            uint bucketMask = (_nBuckets - 1);

            // Lowest two bits are entry index size, the rest is log2 number of buckets
            int numberOfBucketsShift = HighestBit(_nBuckets) - 1;
            writer.WriteByte((byte)((numberOfBucketsShift << 2) | _entryIndexSize));

            int bucketsOffset = writer.GetCurrentOffset();

            writer.WritePad((int)((_nBuckets + 1) << _entryIndexSize));

            // For faster lookup at runtime, we store the first entry index even though it is redundant (the
            // value can be inferred from number of buckets)
            PatchEntryIndex(writer, bucketsOffset, _entryIndexSize, writer.GetCurrentOffset() - bucketsOffset);

            int iEntry = 0;

            for (int iBucket = 0; iBucket < _nBuckets; iBucket++)
            {
                while (iEntry < nEntries)
                {
                    if (((_Entries[iEntry].Hashcode >> 8) & bucketMask) != iBucket)
                        break;

                    Entry curEntry = _Entries[iEntry];

                    int currentOffset = writer.GetCurrentOffset();
                    writer.UpdateOffsetAdjustment(currentOffset - curEntry.Offset);
                    curEntry.Offset = currentOffset;
                    _Entries[iEntry] = curEntry;

                    writer.WriteByte((byte)curEntry.Hashcode);
                    writer.WriteRelativeOffset(curEntry.Vertex);

                    iEntry++;
                }

                int patchOffset = bucketsOffset + ((iBucket + 1) << _entryIndexSize);

                PatchEntryIndex(writer, patchOffset, _entryIndexSize, writer.GetCurrentOffset() - bucketsOffset);
            }
            Debug.Assert(iEntry == nEntries);

            int maxIndexEntry = (writer.GetCurrentOffset() - bucketsOffset);
            int newEntryIndexSize = 0;
            if (maxIndexEntry > 0xFF)
            {
                newEntryIndexSize++;
                if (maxIndexEntry > 0xFFFF)
                    newEntryIndexSize++;
            }

            if (writer.IsGrowing())
            {
                if (newEntryIndexSize > _entryIndexSize)
                {
                    // Ensure that the table will be redone with new entry index size
                    writer.UpdateOffsetAdjustment(1);

                    _entryIndexSize = newEntryIndexSize;
                }
            }
            else
            {
                if (newEntryIndexSize < _entryIndexSize)
                {
                    // Ensure that the table will be redone with new entry index size
                    writer.UpdateOffsetAdjustment(-1);

                    _entryIndexSize = newEntryIndexSize;
                }
            }
        }

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
