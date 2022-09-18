// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This is a generated file - do not manually edit!

#pragma warning disable 649

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Internal.NativeFormat;
using Debug = System.Diagnostics.Debug;

namespace Internal.Metadata.NativeFormat.Writer
{
    public partial class ArraySignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ArraySignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            ElementType = visitor.Visit(this, ElementType);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ArraySignature;
            if (other == null) return false;
            if (!Equals(ElementType, other.ElementType)) return false;
            if (Rank != other.Rank) return false;
            if (!Sizes.SequenceEqual(other.Sizes)) return false;
            if (!LowerBounds.SequenceEqual(other.LowerBounds)) return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1450734452;
            hash = ((hash << 13) - (hash >> 19)) ^ (ElementType == null ? 0 : ElementType.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ Rank.GetHashCode();
            if (Sizes != null)
            {
                for (int i = 0; i < Sizes.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Sizes[i].GetHashCode();
                }
            }
            if (LowerBounds != null)
            {
                for (int i = 0; i < LowerBounds.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ LowerBounds[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(ElementType == null ||
                ElementType.HandleType == HandleType.TypeDefinition ||
                ElementType.HandleType == HandleType.TypeReference ||
                ElementType.HandleType == HandleType.TypeSpecification);
            writer.Write(ElementType);
            writer.Write(Rank);
            writer.Write(Sizes);
            writer.Write(LowerBounds);
        } // Save

        internal static ArraySignatureHandle AsHandle(ArraySignature record)
        {
            if (record == null)
            {
                return new ArraySignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ArraySignatureHandle Handle
        {
            get
            {
                return new ArraySignatureHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord ElementType;
        public int Rank;
        public int[] Sizes;
        public int[] LowerBounds;
    } // ArraySignature

    public partial class ByReferenceSignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ByReferenceSignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Type = visitor.Visit(this, Type);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ByReferenceSignature;
            if (other == null) return false;
            if (!Equals(Type, other.Type)) return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -2085375797;
            hash = ((hash << 13) - (hash >> 19)) ^ (Type == null ? 0 : Type.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Type == null ||
                Type.HandleType == HandleType.TypeDefinition ||
                Type.HandleType == HandleType.TypeReference ||
                Type.HandleType == HandleType.TypeSpecification);
            writer.Write(Type);
        } // Save

        internal static ByReferenceSignatureHandle AsHandle(ByReferenceSignature record)
        {
            if (record == null)
            {
                return new ByReferenceSignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ByReferenceSignatureHandle Handle
        {
            get
            {
                return new ByReferenceSignatureHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord Type;
    } // ByReferenceSignature

    public partial class ConstantBooleanArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantBooleanArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ConstantBooleanArray;
            if (other == null) return false;
            if (!Value.SequenceEqual(other.Value)) return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1817079014;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantBooleanArrayHandle AsHandle(ConstantBooleanArray record)
        {
            if (record == null)
            {
                return new ConstantBooleanArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantBooleanArrayHandle Handle
        {
            get
            {
                return new ConstantBooleanArrayHandle(HandleOffset);
            }
        } // Handle

        public bool[] Value;
    } // ConstantBooleanArray

    public partial class ConstantBooleanValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantBooleanValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ConstantBooleanValue;
            if (other == null) return false;
            if (Value != other.Value) return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 555429541;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantBooleanValueHandle AsHandle(ConstantBooleanValue record)
        {
            if (record == null)
            {
                return new ConstantBooleanValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantBooleanValueHandle Handle
        {
            get
            {
                return new ConstantBooleanValueHandle(HandleOffset);
            }
        } // Handle

        public bool Value;
    } // ConstantBooleanValue

    public partial class ConstantBoxedEnumValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantBoxedEnumValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Value = visitor.Visit(this, Value);
            Type = visitor.Visit(this, Type);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ConstantBoxedEnumValue;
            if (other == null) return false;
            if (!Equals(Value, other.Value)) return false;
            if (!Equals(Type, other.Type)) return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 879057725;
            hash = ((hash << 13) - (hash >> 19)) ^ (Value == null ? 0 : Value.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Type == null ? 0 : Type.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Value == null ||
                Value.HandleType == HandleType.ConstantByteValue ||
                Value.HandleType == HandleType.ConstantSByteValue ||
                Value.HandleType == HandleType.ConstantInt16Value ||
                Value.HandleType == HandleType.ConstantUInt16Value ||
                Value.HandleType == HandleType.ConstantInt32Value ||
                Value.HandleType == HandleType.ConstantUInt32Value ||
                Value.HandleType == HandleType.ConstantInt64Value ||
                Value.HandleType == HandleType.ConstantUInt64Value);
            writer.Write(Value);
            Debug.Assert(Type == null ||
                Type.HandleType == HandleType.TypeDefinition ||
                Type.HandleType == HandleType.TypeReference ||
                Type.HandleType == HandleType.TypeSpecification);
            writer.Write(Type);
        } // Save

        internal static ConstantBoxedEnumValueHandle AsHandle(ConstantBoxedEnumValue record)
        {
            if (record == null)
            {
                return new ConstantBoxedEnumValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantBoxedEnumValueHandle Handle
        {
            get
            {
                return new ConstantBoxedEnumValueHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord Value;
        public MetadataRecord Type;
    } // ConstantBoxedEnumValue

    public partial class ConstantByteArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantByteArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ConstantByteArray;
            if (other == null) return false;
            if (!Value.SequenceEqual(other.Value)) return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 2080036690;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantByteArrayHandle AsHandle(ConstantByteArray record)
        {
            if (record == null)
            {
                return new ConstantByteArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantByteArrayHandle Handle
        {
            get
            {
                return new ConstantByteArrayHandle(HandleOffset);
            }
        } // Handle

        public byte[] Value;
    } // ConstantByteArray

    public partial class ConstantByteValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantByteValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantByteValue;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -452758418;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantByteValueHandle AsHandle(ConstantByteValue record)
        {
            if (record == null)
            {
                return new ConstantByteValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantByteValueHandle Handle
        {
            get
            {
                return new ConstantByteValueHandle(HandleOffset);
            }
        } // Handle

        public byte Value;
    } // ConstantByteValue

    public partial class ConstantCharArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantCharArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantCharArray;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -210173789;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantCharArrayHandle AsHandle(ConstantCharArray record)
        {
            if (record == null)
            {
                return new ConstantCharArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantCharArrayHandle Handle
        {
            get
            {
                return new ConstantCharArrayHandle(HandleOffset);
            }
        } // Handle

        public char[] Value;
    } // ConstantCharArray

    public partial class ConstantCharValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantCharValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantCharValue;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 2135306273;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantCharValueHandle AsHandle(ConstantCharValue record)
        {
            if (record == null)
            {
                return new ConstantCharValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantCharValueHandle Handle
        {
            get
            {
                return new ConstantCharValueHandle(HandleOffset);
            }
        } // Handle

        public char Value;
    } // ConstantCharValue

    public partial class ConstantDoubleArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantDoubleArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantDoubleArray;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value, DoubleComparer.Instance))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1195490519;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantDoubleArrayHandle AsHandle(ConstantDoubleArray record)
        {
            if (record == null)
            {
                return new ConstantDoubleArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantDoubleArrayHandle Handle
        {
            get
            {
                return new ConstantDoubleArrayHandle(HandleOffset);
            }
        } // Handle

        public double[] Value;
    } // ConstantDoubleArray

    public partial class ConstantDoubleValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantDoubleValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantDoubleValue;
            if (other == null)
                return false;
            if (!CustomComparer.Equals(Value, other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -621001209;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantDoubleValueHandle AsHandle(ConstantDoubleValue record)
        {
            if (record == null)
            {
                return new ConstantDoubleValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantDoubleValueHandle Handle
        {
            get
            {
                return new ConstantDoubleValueHandle(HandleOffset);
            }
        } // Handle

        public double Value;
    } // ConstantDoubleValue

    public partial class ConstantEnumArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantEnumArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            ElementType = visitor.Visit(this, ElementType);
            Value = visitor.Visit(this, Value);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantEnumArray;
            if (other == null)
                return false;
            if (!Equals(ElementType, other.ElementType))
                return false;
            if (!Equals(Value, other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1812865730;
            hash = ((hash << 13) - (hash >> 19)) ^ (ElementType == null ? 0 : ElementType.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Value == null ? 0 : Value.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(ElementType);
            writer.Write(Value);
        } // Save

        internal static ConstantEnumArrayHandle AsHandle(ConstantEnumArray record)
        {
            if (record == null)
            {
                return new ConstantEnumArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantEnumArrayHandle Handle
        {
            get
            {
                return new ConstantEnumArrayHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord ElementType;
        public MetadataRecord Value;
    } // ConstantEnumArray

    public partial class ConstantHandleArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantHandleArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Value = visitor.Visit(this, Value);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantHandleArray;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1728604822;
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantHandleArrayHandle AsHandle(ConstantHandleArray record)
        {
            if (record == null)
            {
                return new ConstantHandleArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantHandleArrayHandle Handle
        {
            get
            {
                return new ConstantHandleArrayHandle(HandleOffset);
            }
        } // Handle

        public List<MetadataRecord> Value = new List<MetadataRecord>();
    } // ConstantHandleArray

    public partial class ConstantInt16Array : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantInt16Array;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantInt16Array;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1341795012;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantInt16ArrayHandle AsHandle(ConstantInt16Array record)
        {
            if (record == null)
            {
                return new ConstantInt16ArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantInt16ArrayHandle Handle
        {
            get
            {
                return new ConstantInt16ArrayHandle(HandleOffset);
            }
        } // Handle

        public short[] Value;
    } // ConstantInt16Array

    public partial class ConstantInt16Value : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantInt16Value;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantInt16Value;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 813581618;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantInt16ValueHandle AsHandle(ConstantInt16Value record)
        {
            if (record == null)
            {
                return new ConstantInt16ValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantInt16ValueHandle Handle
        {
            get
            {
                return new ConstantInt16ValueHandle(HandleOffset);
            }
        } // Handle

        public short Value;
    } // ConstantInt16Value

    public partial class ConstantInt32Array : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantInt32Array;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantInt32Array;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -889690268;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantInt32ArrayHandle AsHandle(ConstantInt32Array record)
        {
            if (record == null)
            {
                return new ConstantInt32ArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantInt32ArrayHandle Handle
        {
            get
            {
                return new ConstantInt32ArrayHandle(HandleOffset);
            }
        } // Handle

        public int[] Value;
    } // ConstantInt32Array

    public partial class ConstantInt32Value : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantInt32Value;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantInt32Value;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1381815736;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantInt32ValueHandle AsHandle(ConstantInt32Value record)
        {
            if (record == null)
            {
                return new ConstantInt32ValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantInt32ValueHandle Handle
        {
            get
            {
                return new ConstantInt32ValueHandle(HandleOffset);
            }
        } // Handle

        public int Value;
    } // ConstantInt32Value

    public partial class ConstantInt64Array : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantInt64Array;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantInt64Array;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1304463479;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantInt64ArrayHandle AsHandle(ConstantInt64Array record)
        {
            if (record == null)
            {
                return new ConstantInt64ArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantInt64ArrayHandle Handle
        {
            get
            {
                return new ConstantInt64ArrayHandle(HandleOffset);
            }
        } // Handle

        public long[] Value;
    } // ConstantInt64Array

    public partial class ConstantInt64Value : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantInt64Value;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantInt64Value;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 826277577;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantInt64ValueHandle AsHandle(ConstantInt64Value record)
        {
            if (record == null)
            {
                return new ConstantInt64ValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantInt64ValueHandle Handle
        {
            get
            {
                return new ConstantInt64ValueHandle(HandleOffset);
            }
        } // Handle

        public long Value;
    } // ConstantInt64Value

    public partial class ConstantReferenceValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantReferenceValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as ConstantReferenceValue;
            if (other == null) return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -2104982909;
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
        } // Save

        internal static ConstantReferenceValueHandle AsHandle(ConstantReferenceValue record)
        {
            if (record == null)
            {
                return new ConstantReferenceValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantReferenceValueHandle Handle
        {
            get
            {
                return new ConstantReferenceValueHandle(HandleOffset);
            }
        } // Handle

    } // ConstantReferenceValue

    public partial class ConstantSByteArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantSByteArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantSByteArray;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 234859551;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantSByteArrayHandle AsHandle(ConstantSByteArray record)
        {
            if (record == null)
            {
                return new ConstantSByteArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantSByteArrayHandle Handle
        {
            get
            {
                return new ConstantSByteArrayHandle(HandleOffset);
            }
        } // Handle

        public sbyte[] Value;
    } // ConstantSByteArray

    public partial class ConstantSByteValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantSByteValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantSByteValue;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -222060848;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantSByteValueHandle AsHandle(ConstantSByteValue record)
        {
            if (record == null)
            {
                return new ConstantSByteValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantSByteValueHandle Handle
        {
            get
            {
                return new ConstantSByteValueHandle(HandleOffset);
            }
        } // Handle

        public sbyte Value;
    } // ConstantSByteValue

    public partial class ConstantSingleArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantSingleArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantSingleArray;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value, SingleComparer.Instance))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -2043917844;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantSingleArrayHandle AsHandle(ConstantSingleArray record)
        {
            if (record == null)
            {
                return new ConstantSingleArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantSingleArrayHandle Handle
        {
            get
            {
                return new ConstantSingleArrayHandle(HandleOffset);
            }
        } // Handle

        public float[] Value;
    } // ConstantSingleArray

    public partial class ConstantSingleValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantSingleValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantSingleValue;
            if (other == null)
                return false;
            if (!CustomComparer.Equals(Value, other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1809397893;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantSingleValueHandle AsHandle(ConstantSingleValue record)
        {
            if (record == null)
            {
                return new ConstantSingleValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantSingleValueHandle Handle
        {
            get
            {
                return new ConstantSingleValueHandle(HandleOffset);
            }
        } // Handle

        public float Value;
    } // ConstantSingleValue

    public partial class ConstantStringArray : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantStringArray;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Value = visitor.Visit(this, Value);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantStringArray;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -106723178;
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Value.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.ConstantStringValue ||
                handle.HandleType == HandleType.ConstantReferenceValue));
            writer.Write(Value);
        } // Save

        internal static ConstantStringArrayHandle AsHandle(ConstantStringArray record)
        {
            if (record == null)
            {
                return new ConstantStringArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantStringArrayHandle Handle
        {
            get
            {
                return new ConstantStringArrayHandle(HandleOffset);
            }
        } // Handle

        public List<MetadataRecord> Value = new List<MetadataRecord>();
    } // ConstantStringArray

    public partial class ConstantStringValue : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantStringValue;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantStringValue;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 697695316;
            hash = ((hash << 13) - (hash >> 19)) ^ (Value == null ? 0 : Value.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            if (Value == null)
                return;

            writer.Write(Value);
        } // Save

        internal static ConstantStringValueHandle AsHandle(ConstantStringValue record)
        {
            if (record == null)
            {
                return new ConstantStringValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantStringValueHandle Handle
        {
            get
            {
                if (Value == null)
                    return new ConstantStringValueHandle(0);
                else
                    return new ConstantStringValueHandle(HandleOffset);
            }
        } // Handle

        public string Value;
    } // ConstantStringValue

    public partial class ConstantUInt16Array : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantUInt16Array;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantUInt16Array;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -89281077;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantUInt16ArrayHandle AsHandle(ConstantUInt16Array record)
        {
            if (record == null)
            {
                return new ConstantUInt16ArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantUInt16ArrayHandle Handle
        {
            get
            {
                return new ConstantUInt16ArrayHandle(HandleOffset);
            }
        } // Handle

        public ushort[] Value;
    } // ConstantUInt16Array

    public partial class ConstantUInt16Value : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantUInt16Value;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantUInt16Value;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1363963764;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantUInt16ValueHandle AsHandle(ConstantUInt16Value record)
        {
            if (record == null)
            {
                return new ConstantUInt16ValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantUInt16ValueHandle Handle
        {
            get
            {
                return new ConstantUInt16ValueHandle(HandleOffset);
            }
        } // Handle

        public ushort Value;
    } // ConstantUInt16Value

    public partial class ConstantUInt32Array : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantUInt32Array;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantUInt32Array;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1294553100;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantUInt32ArrayHandle AsHandle(ConstantUInt32Array record)
        {
            if (record == null)
            {
                return new ConstantUInt32ArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantUInt32ArrayHandle Handle
        {
            get
            {
                return new ConstantUInt32ArrayHandle(HandleOffset);
            }
        } // Handle

        public uint[] Value;
    } // ConstantUInt32Array

    public partial class ConstantUInt32Value : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantUInt32Value;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantUInt32Value;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1681229940;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantUInt32ValueHandle AsHandle(ConstantUInt32Value record)
        {
            if (record == null)
            {
                return new ConstantUInt32ValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantUInt32ValueHandle Handle
        {
            get
            {
                return new ConstantUInt32ValueHandle(HandleOffset);
            }
        } // Handle

        public uint Value;
    } // ConstantUInt32Value

    public partial class ConstantUInt64Array : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantUInt64Array;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantUInt64Array;
            if (other == null)
                return false;
            if (!Value.SequenceEqual(other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 47301549;
            if (Value != null)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Value[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantUInt64ArrayHandle AsHandle(ConstantUInt64Array record)
        {
            if (record == null)
            {
                return new ConstantUInt64ArrayHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantUInt64ArrayHandle Handle
        {
            get
            {
                return new ConstantUInt64ArrayHandle(HandleOffset);
            }
        } // Handle

        public ulong[] Value;
    } // ConstantUInt64Array

    public partial class ConstantUInt64Value : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ConstantUInt64Value;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ConstantUInt64Value;
            if (other == null)
                return false;
            if (Value != other.Value)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1162192418;
            hash = ((hash << 13) - (hash >> 19)) ^ Value.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Value);
        } // Save

        internal static ConstantUInt64ValueHandle AsHandle(ConstantUInt64Value record)
        {
            if (record == null)
            {
                return new ConstantUInt64ValueHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ConstantUInt64ValueHandle Handle
        {
            get
            {
                return new ConstantUInt64ValueHandle(HandleOffset);
            }
        } // Handle

        public ulong Value;
    } // ConstantUInt64Value

    public partial class CustomAttribute : MetadataRecord
    {
        public CustomAttribute()
        {
            _equalsReentrancyGuard = new ThreadLocal<ReentrancyGuardStack>(() => new ReentrancyGuardStack());
        }

        public override HandleType HandleType
        {
            get
            {
                return HandleType.CustomAttribute;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Constructor = visitor.Visit(this, Constructor);
            FixedArguments = visitor.Visit(this, FixedArguments);
            NamedArguments = visitor.Visit(this, NamedArguments);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            var other = obj as CustomAttribute;
            if (other == null) return false;
            if (_equalsReentrancyGuard.Value.Contains(other))
                return true;
            _equalsReentrancyGuard.Value.Push(other);
            try
            {
            if (!Equals(Constructor, other.Constructor)) return false;
            if (!FixedArguments.SequenceEqual(other.FixedArguments)) return false;
            if (!NamedArguments.SequenceEqual(other.NamedArguments)) return false;
            }
            finally
            {
                var popped = _equalsReentrancyGuard.Value.Pop();
                Debug.Assert(ReferenceEquals(other, popped));
            }
            return true;
        } // Equals
        private ThreadLocal<ReentrancyGuardStack> _equalsReentrancyGuard;

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 478371161;
            hash = ((hash << 13) - (hash >> 19)) ^ (Constructor == null ? 0 : Constructor.GetHashCode());
            if (FixedArguments != null)
            {
            for (int i = 0; i < FixedArguments.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (FixedArguments[i] == null ? 0 : FixedArguments[i].GetHashCode());
                }
            }
            if (NamedArguments != null)
            {
            for (int i = 0; i < NamedArguments.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (NamedArguments[i] == null ? 0 : NamedArguments[i].GetHashCode());
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Constructor == null ||
                Constructor.HandleType == HandleType.QualifiedMethod ||
                Constructor.HandleType == HandleType.MemberReference);
            writer.Write(Constructor);
            Debug.Assert(FixedArguments.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.TypeDefinition ||
                handle.HandleType == HandleType.TypeReference ||
                handle.HandleType == HandleType.TypeSpecification ||
                handle.HandleType == HandleType.ConstantBooleanArray ||
                handle.HandleType == HandleType.ConstantBooleanValue ||
                handle.HandleType == HandleType.ConstantByteArray ||
                handle.HandleType == HandleType.ConstantByteValue ||
                handle.HandleType == HandleType.ConstantCharArray ||
                handle.HandleType == HandleType.ConstantCharValue ||
                handle.HandleType == HandleType.ConstantDoubleArray ||
                handle.HandleType == HandleType.ConstantDoubleValue ||
                handle.HandleType == HandleType.ConstantEnumArray ||
                handle.HandleType == HandleType.ConstantHandleArray ||
                handle.HandleType == HandleType.ConstantInt16Array ||
                handle.HandleType == HandleType.ConstantInt16Value ||
                handle.HandleType == HandleType.ConstantInt32Array ||
                handle.HandleType == HandleType.ConstantInt32Value ||
                handle.HandleType == HandleType.ConstantInt64Array ||
                handle.HandleType == HandleType.ConstantInt64Value ||
                handle.HandleType == HandleType.ConstantReferenceValue ||
                handle.HandleType == HandleType.ConstantSByteArray ||
                handle.HandleType == HandleType.ConstantSByteValue ||
                handle.HandleType == HandleType.ConstantSingleArray ||
                handle.HandleType == HandleType.ConstantSingleValue ||
                handle.HandleType == HandleType.ConstantStringArray ||
                handle.HandleType == HandleType.ConstantStringValue ||
                handle.HandleType == HandleType.ConstantUInt16Array ||
                handle.HandleType == HandleType.ConstantUInt16Value ||
                handle.HandleType == HandleType.ConstantUInt32Array ||
                handle.HandleType == HandleType.ConstantUInt32Value ||
                handle.HandleType == HandleType.ConstantUInt64Array ||
                handle.HandleType == HandleType.ConstantUInt64Value));
            writer.Write(FixedArguments);
            writer.Write(NamedArguments);
        } // Save

        internal static CustomAttributeHandle AsHandle(CustomAttribute record)
        {
            if (record == null)
            {
                return new CustomAttributeHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new CustomAttributeHandle Handle
        {
            get
            {
                return new CustomAttributeHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord Constructor;
        public List<MetadataRecord> FixedArguments = new List<MetadataRecord>();
        public List<NamedArgument> NamedArguments = new List<NamedArgument>();
    } // CustomAttribute

    public partial class Event : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.Event;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            Type = visitor.Visit(this, Type);
            MethodSemantics = visitor.Visit(this, MethodSemantics);
            CustomAttributes = visitor.Visit(this, CustomAttributes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as Event;
            if (other == null)
                return false;
            if (Flags != other.Flags)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Equals(Type, other.Type))
                return false;
            if (!MethodSemantics.SequenceEqual(other.MethodSemantics))
                return false;
            if (!CustomAttributes.SequenceEqual(other.CustomAttributes))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1454825650;
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Type == null ? 0 : Type.GetHashCode());
            if (MethodSemantics != null)
            {
            for (int i = 0; i < MethodSemantics.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (MethodSemantics[i] == null ? 0 : MethodSemantics[i].GetHashCode());
                }
            }
            if (CustomAttributes != null)
            {
            for (int i = 0; i < CustomAttributes.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (CustomAttributes[i] == null ? 0 : CustomAttributes[i].GetHashCode());
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            writer.Write(Name);
            Debug.Assert(Type == null ||
                Type.HandleType == HandleType.TypeDefinition ||
                Type.HandleType == HandleType.TypeReference ||
                Type.HandleType == HandleType.TypeSpecification);
            writer.Write(Type);
            writer.Write(MethodSemantics);
            writer.Write(CustomAttributes);
        } // Save

        internal static EventHandle AsHandle(Event record)
        {
            if (record == null)
            {
                return new EventHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new EventHandle Handle
        {
            get
            {
                return new EventHandle(HandleOffset);
            }
        } // Handle

        public EventAttributes Flags;
        public ConstantStringValue Name;
        public MetadataRecord Type;
        public List<MethodSemantics> MethodSemantics = new List<MethodSemantics>();
        public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
    } // Event

    public partial class Field : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.Field;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            Signature = visitor.Visit(this, Signature);
            DefaultValue = visitor.Visit(this, DefaultValue);
            CustomAttributes = visitor.Visit(this, CustomAttributes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as Field;
            if (other == null)
                return false;
            if (Flags != other.Flags)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Equals(Signature, other.Signature))
                return false;
            if (!Equals(DefaultValue, other.DefaultValue))
                return false;
            if (Offset != other.Offset)
                return false;
            if (!CustomAttributes.SequenceEqual(other.CustomAttributes))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -540975116;
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Signature == null ? 0 : Signature.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (DefaultValue == null ? 0 : DefaultValue.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ Offset.GetHashCode();
            if (CustomAttributes != null)
            {
            for (int i = 0; i < CustomAttributes.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (CustomAttributes[i] == null ? 0 : CustomAttributes[i].GetHashCode());
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            writer.Write(Name);
            writer.Write(Signature);
            Debug.Assert(DefaultValue == null ||
                DefaultValue.HandleType == HandleType.TypeDefinition ||
                DefaultValue.HandleType == HandleType.TypeReference ||
                DefaultValue.HandleType == HandleType.TypeSpecification ||
                DefaultValue.HandleType == HandleType.ConstantBooleanArray ||
                DefaultValue.HandleType == HandleType.ConstantBooleanValue ||
                DefaultValue.HandleType == HandleType.ConstantByteArray ||
                DefaultValue.HandleType == HandleType.ConstantByteValue ||
                DefaultValue.HandleType == HandleType.ConstantCharArray ||
                DefaultValue.HandleType == HandleType.ConstantCharValue ||
                DefaultValue.HandleType == HandleType.ConstantDoubleArray ||
                DefaultValue.HandleType == HandleType.ConstantDoubleValue ||
                DefaultValue.HandleType == HandleType.ConstantEnumArray ||
                DefaultValue.HandleType == HandleType.ConstantHandleArray ||
                DefaultValue.HandleType == HandleType.ConstantInt16Array ||
                DefaultValue.HandleType == HandleType.ConstantInt16Value ||
                DefaultValue.HandleType == HandleType.ConstantInt32Array ||
                DefaultValue.HandleType == HandleType.ConstantInt32Value ||
                DefaultValue.HandleType == HandleType.ConstantInt64Array ||
                DefaultValue.HandleType == HandleType.ConstantInt64Value ||
                DefaultValue.HandleType == HandleType.ConstantReferenceValue ||
                DefaultValue.HandleType == HandleType.ConstantSByteArray ||
                DefaultValue.HandleType == HandleType.ConstantSByteValue ||
                DefaultValue.HandleType == HandleType.ConstantSingleArray ||
                DefaultValue.HandleType == HandleType.ConstantSingleValue ||
                DefaultValue.HandleType == HandleType.ConstantStringArray ||
                DefaultValue.HandleType == HandleType.ConstantStringValue ||
                DefaultValue.HandleType == HandleType.ConstantUInt16Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt16Value ||
                DefaultValue.HandleType == HandleType.ConstantUInt32Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt32Value ||
                DefaultValue.HandleType == HandleType.ConstantUInt64Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt64Value);
            writer.Write(DefaultValue);
            writer.Write(Offset);
            writer.Write(CustomAttributes);
        } // Save

        internal static FieldHandle AsHandle(Field record)
        {
            if (record == null)
            {
                return new FieldHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new FieldHandle Handle
        {
            get
            {
                return new FieldHandle(HandleOffset);
            }
        } // Handle

        public FieldAttributes Flags;
        public ConstantStringValue Name;
        public FieldSignature Signature;
        public MetadataRecord DefaultValue;
        public uint Offset;
        public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
    } // Field

    public partial class FieldSignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.FieldSignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Type = visitor.Visit(this, Type);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as FieldSignature;
            if (other == null)
                return false;
            if (!Equals(Type, other.Type))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1865977400;
            hash = ((hash << 13) - (hash >> 19)) ^ (Type == null ? 0 : Type.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Type == null ||
                Type.HandleType == HandleType.TypeDefinition ||
                Type.HandleType == HandleType.TypeReference ||
                Type.HandleType == HandleType.TypeSpecification ||
                Type.HandleType == HandleType.ModifiedType);
            writer.Write(Type);
        } // Save

        internal static FieldSignatureHandle AsHandle(FieldSignature record)
        {
            if (record == null)
            {
                return new FieldSignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new FieldSignatureHandle Handle
        {
            get
            {
                return new FieldSignatureHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord Type;
    } // FieldSignature

    public partial class FunctionPointerSignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.FunctionPointerSignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Signature = visitor.Visit(this, Signature);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as FunctionPointerSignature;
            if (other == null)
                return false;
            if (!Equals(Signature, other.Signature))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1400760676;
            hash = ((hash << 13) - (hash >> 19)) ^ (Signature == null ? 0 : Signature.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Signature);
        } // Save

        internal static FunctionPointerSignatureHandle AsHandle(FunctionPointerSignature record)
        {
            if (record == null)
            {
                return new FunctionPointerSignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new FunctionPointerSignatureHandle Handle
        {
            get
            {
                return new FunctionPointerSignatureHandle(HandleOffset);
            }
        } // Handle

        public MethodSignature Signature;
    } // FunctionPointerSignature

    public partial class GenericParameter : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.GenericParameter;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            Constraints = visitor.Visit(this, Constraints);
            CustomAttributes = visitor.Visit(this, CustomAttributes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as GenericParameter;
            if (other == null)
                return false;
            if (Number != other.Number)
                return false;
            if (Flags != other.Flags)
                return false;
            if (Kind != other.Kind)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Constraints.SequenceEqual(other.Constraints))
                return false;
            if (!CustomAttributes.SequenceEqual(other.CustomAttributes))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1389630306;
            hash = ((hash << 13) - (hash >> 19)) ^ Number.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ Kind.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            if (Constraints != null)
            {
            for (int i = 0; i < Constraints.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (Constraints[i] == null ? 0 : Constraints[i].GetHashCode());
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Number);
            writer.Write(Flags);
            writer.Write(Kind);
            writer.Write(Name);
            Debug.Assert(Constraints.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.TypeDefinition ||
                handle.HandleType == HandleType.TypeReference ||
                handle.HandleType == HandleType.TypeSpecification ||
                handle.HandleType == HandleType.ModifiedType));
            writer.Write(Constraints);
            writer.Write(CustomAttributes);
        } // Save

        internal static GenericParameterHandle AsHandle(GenericParameter record)
        {
            if (record == null)
            {
                return new GenericParameterHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new GenericParameterHandle Handle
        {
            get
            {
                return new GenericParameterHandle(HandleOffset);
            }
        } // Handle

        public ushort Number;
        public GenericParameterAttributes Flags;
        public GenericParameterKind Kind;
        public ConstantStringValue Name;
        public List<MetadataRecord> Constraints = new List<MetadataRecord>();
        public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
    } // GenericParameter

    public partial class MemberReference : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.MemberReference;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Parent = visitor.Visit(this, Parent);
            Name = visitor.Visit(this, Name);
            Signature = visitor.Visit(this, Signature);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as MemberReference;
            if (other == null)
                return false;
            if (!Equals(Parent, other.Parent))
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Equals(Signature, other.Signature))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -875402938;
            hash = ((hash << 13) - (hash >> 19)) ^ (Parent == null ? 0 : Parent.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Signature == null ? 0 : Signature.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Parent == null ||
                Parent.HandleType == HandleType.TypeDefinition ||
                Parent.HandleType == HandleType.TypeReference ||
                Parent.HandleType == HandleType.TypeSpecification);
            writer.Write(Parent);
            writer.Write(Name);
            Debug.Assert(Signature == null ||
                Signature.HandleType == HandleType.MethodSignature ||
                Signature.HandleType == HandleType.FieldSignature);
            writer.Write(Signature);
        } // Save

        internal static MemberReferenceHandle AsHandle(MemberReference record)
        {
            if (record == null)
            {
                return new MemberReferenceHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new MemberReferenceHandle Handle
        {
            get
            {
                return new MemberReferenceHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord Parent;
        public ConstantStringValue Name;
        public MetadataRecord Signature;
    } // MemberReference

    public partial class Method : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.Method;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            Signature = visitor.Visit(this, Signature);
            Parameters = visitor.Visit(this, Parameters);
            GenericParameters = visitor.Visit(this, GenericParameters);
            CustomAttributes = visitor.Visit(this, CustomAttributes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as Method;
            if (other == null)
                return false;
            if (Flags != other.Flags)
                return false;
            if (ImplFlags != other.ImplFlags)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Equals(Signature, other.Signature))
                return false;
            if (!Parameters.SequenceEqual(other.Parameters))
                return false;
            if (!GenericParameters.SequenceEqual(other.GenericParameters))
                return false;
            if (!CustomAttributes.SequenceEqual(other.CustomAttributes))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1225154478;
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ ImplFlags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Signature == null ? 0 : Signature.GetHashCode());
            if (Parameters != null)
            {
            for (int i = 0; i < Parameters.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (Parameters[i] == null ? 0 : Parameters[i].GetHashCode());
                }
            }
            if (GenericParameters != null)
            {
            for (int i = 0; i < GenericParameters.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (GenericParameters[i] == null ? 0 : GenericParameters[i].GetHashCode());
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            writer.Write(ImplFlags);
            writer.Write(Name);
            writer.Write(Signature);
            writer.Write(Parameters);
            writer.Write(GenericParameters);
            writer.Write(CustomAttributes);
        } // Save

        internal static MethodHandle AsHandle(Method record)
        {
            if (record == null)
            {
                return new MethodHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new MethodHandle Handle
        {
            get
            {
                return new MethodHandle(HandleOffset);
            }
        } // Handle

        public MethodAttributes Flags;
        public MethodImplAttributes ImplFlags;
        public ConstantStringValue Name;
        public MethodSignature Signature;
        public List<Parameter> Parameters = new List<Parameter>();
        public List<GenericParameter> GenericParameters = new List<GenericParameter>();
        public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
    } // Method

    public partial class MethodInstantiation : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.MethodInstantiation;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Method = visitor.Visit(this, Method);
            GenericTypeArguments = visitor.Visit(this, GenericTypeArguments);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as MethodInstantiation;
            if (other == null)
                return false;
            if (!Equals(Method, other.Method))
                return false;
            if (!GenericTypeArguments.SequenceEqual(other.GenericTypeArguments))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1777408040;
            hash = ((hash << 13) - (hash >> 19)) ^ (Method == null ? 0 : Method.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Method == null ||
                Method.HandleType == HandleType.QualifiedMethod ||
                Method.HandleType == HandleType.MemberReference);
            writer.Write(Method);
            Debug.Assert(GenericTypeArguments.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.TypeDefinition ||
                handle.HandleType == HandleType.TypeReference ||
                handle.HandleType == HandleType.TypeSpecification));
            writer.Write(GenericTypeArguments);
        } // Save

        internal static MethodInstantiationHandle AsHandle(MethodInstantiation record)
        {
            if (record == null)
            {
                return new MethodInstantiationHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new MethodInstantiationHandle Handle
        {
            get
            {
                return new MethodInstantiationHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord Method;
        public List<MetadataRecord> GenericTypeArguments = new List<MetadataRecord>();
    } // MethodInstantiation

    public partial class MethodSemantics : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.MethodSemantics;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Method = visitor.Visit(this, Method);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as MethodSemantics;
            if (other == null)
                return false;
            if (Attributes != other.Attributes)
                return false;
            if (!Equals(Method, other.Method))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1967828724;
            hash = ((hash << 13) - (hash >> 19)) ^ Attributes.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Method == null ? 0 : Method.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Attributes);
            writer.Write(Method);
        } // Save

        internal static MethodSemanticsHandle AsHandle(MethodSemantics record)
        {
            if (record == null)
            {
                return new MethodSemanticsHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new MethodSemanticsHandle Handle
        {
            get
            {
                return new MethodSemanticsHandle(HandleOffset);
            }
        } // Handle

        public MethodSemanticsAttributes Attributes;
        public Method Method;
    } // MethodSemantics

    public partial class MethodSignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.MethodSignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            ReturnType = visitor.Visit(this, ReturnType);
            Parameters = visitor.Visit(this, Parameters);
            VarArgParameters = visitor.Visit(this, VarArgParameters);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as MethodSignature;
            if (other == null)
                return false;
            if (CallingConvention != other.CallingConvention)
                return false;
            if (GenericParameterCount != other.GenericParameterCount)
                return false;
            if (!Equals(ReturnType, other.ReturnType))
                return false;
            if (!Parameters.SequenceEqual(other.Parameters))
                return false;
            if (!VarArgParameters.SequenceEqual(other.VarArgParameters))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1362083279;
            hash = ((hash << 13) - (hash >> 19)) ^ CallingConvention.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ GenericParameterCount.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (ReturnType == null ? 0 : ReturnType.GetHashCode());
            if (Parameters != null)
            {
            for (int i = 0; i < Parameters.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (Parameters[i] == null ? 0 : Parameters[i].GetHashCode());
                }
            }
            if (VarArgParameters != null)
            {
            for (int i = 0; i < VarArgParameters.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (VarArgParameters[i] == null ? 0 : VarArgParameters[i].GetHashCode());
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(CallingConvention);
            writer.Write(GenericParameterCount);
            Debug.Assert(ReturnType == null ||
                ReturnType.HandleType == HandleType.TypeDefinition ||
                ReturnType.HandleType == HandleType.TypeReference ||
                ReturnType.HandleType == HandleType.TypeSpecification ||
                ReturnType.HandleType == HandleType.ModifiedType);
            writer.Write(ReturnType);
            Debug.Assert(Parameters.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.TypeDefinition ||
                handle.HandleType == HandleType.TypeReference ||
                handle.HandleType == HandleType.TypeSpecification ||
                handle.HandleType == HandleType.ModifiedType));
            writer.Write(Parameters);
            Debug.Assert(VarArgParameters.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.TypeDefinition ||
                handle.HandleType == HandleType.TypeReference ||
                handle.HandleType == HandleType.TypeSpecification ||
                handle.HandleType == HandleType.ModifiedType));
            writer.Write(VarArgParameters);
        } // Save

        internal static MethodSignatureHandle AsHandle(MethodSignature record)
        {
            if (record == null)
            {
                return new MethodSignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new MethodSignatureHandle Handle
        {
            get
            {
                return new MethodSignatureHandle(HandleOffset);
            }
        } // Handle

        public CallingConventions CallingConvention;
        public int GenericParameterCount;
        public MetadataRecord ReturnType;
        public List<MetadataRecord> Parameters = new List<MetadataRecord>();
        public List<MetadataRecord> VarArgParameters = new List<MetadataRecord>();
    } // MethodSignature

    public partial class MethodTypeVariableSignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.MethodTypeVariableSignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as MethodTypeVariableSignature;
            if (other == null)
                return false;
            if (Number != other.Number)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 542622499;
            hash = ((hash << 13) - (hash >> 19)) ^ Number.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Number);
        } // Save

        internal static MethodTypeVariableSignatureHandle AsHandle(MethodTypeVariableSignature record)
        {
            if (record == null)
            {
                return new MethodTypeVariableSignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new MethodTypeVariableSignatureHandle Handle
        {
            get
            {
                return new MethodTypeVariableSignatureHandle(HandleOffset);
            }
        } // Handle

        public int Number;
    } // MethodTypeVariableSignature

    public partial class ModifiedType : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ModifiedType;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            ModifierType = visitor.Visit(this, ModifierType);
            Type = visitor.Visit(this, Type);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ModifiedType;
            if (other == null)
                return false;
            if (IsOptional != other.IsOptional)
                return false;
            if (!Equals(ModifierType, other.ModifierType))
                return false;
            if (!Equals(Type, other.Type))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -707476144;
            hash = ((hash << 13) - (hash >> 19)) ^ IsOptional.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (ModifierType == null ? 0 : ModifierType.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Type == null ? 0 : Type.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(IsOptional);
            Debug.Assert(ModifierType == null ||
                ModifierType.HandleType == HandleType.TypeDefinition ||
                ModifierType.HandleType == HandleType.TypeReference ||
                ModifierType.HandleType == HandleType.TypeSpecification);
            writer.Write(ModifierType);
            Debug.Assert(Type == null ||
                Type.HandleType == HandleType.TypeDefinition ||
                Type.HandleType == HandleType.TypeReference ||
                Type.HandleType == HandleType.TypeSpecification ||
                Type.HandleType == HandleType.ModifiedType);
            writer.Write(Type);
        } // Save

        internal static ModifiedTypeHandle AsHandle(ModifiedType record)
        {
            if (record == null)
            {
                return new ModifiedTypeHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ModifiedTypeHandle Handle
        {
            get
            {
                return new ModifiedTypeHandle(HandleOffset);
            }
        } // Handle

        public bool IsOptional;
        public MetadataRecord ModifierType;
        public MetadataRecord Type;
    } // ModifiedType

    public partial class NamedArgument : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.NamedArgument;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            Type = visitor.Visit(this, Type);
            Value = visitor.Visit(this, Value);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as NamedArgument;
            if (other == null)
                return false;
            if (Flags != other.Flags)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Equals(Type, other.Type))
                return false;
            if (!Equals(Value, other.Value))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -469180039;
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Type == null ? 0 : Type.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Value == null ? 0 : Value.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            writer.Write(Name);
            Debug.Assert(Type == null ||
                Type.HandleType == HandleType.TypeDefinition ||
                Type.HandleType == HandleType.TypeReference ||
                Type.HandleType == HandleType.TypeSpecification);
            writer.Write(Type);
            Debug.Assert(Value == null ||
                Value.HandleType == HandleType.TypeDefinition ||
                Value.HandleType == HandleType.TypeReference ||
                Value.HandleType == HandleType.TypeSpecification ||
                Value.HandleType == HandleType.ConstantBooleanArray ||
                Value.HandleType == HandleType.ConstantBooleanValue ||
                Value.HandleType == HandleType.ConstantByteArray ||
                Value.HandleType == HandleType.ConstantByteValue ||
                Value.HandleType == HandleType.ConstantCharArray ||
                Value.HandleType == HandleType.ConstantCharValue ||
                Value.HandleType == HandleType.ConstantDoubleArray ||
                Value.HandleType == HandleType.ConstantDoubleValue ||
                Value.HandleType == HandleType.ConstantEnumArray ||
                Value.HandleType == HandleType.ConstantHandleArray ||
                Value.HandleType == HandleType.ConstantInt16Array ||
                Value.HandleType == HandleType.ConstantInt16Value ||
                Value.HandleType == HandleType.ConstantInt32Array ||
                Value.HandleType == HandleType.ConstantInt32Value ||
                Value.HandleType == HandleType.ConstantInt64Array ||
                Value.HandleType == HandleType.ConstantInt64Value ||
                Value.HandleType == HandleType.ConstantReferenceValue ||
                Value.HandleType == HandleType.ConstantSByteArray ||
                Value.HandleType == HandleType.ConstantSByteValue ||
                Value.HandleType == HandleType.ConstantSingleArray ||
                Value.HandleType == HandleType.ConstantSingleValue ||
                Value.HandleType == HandleType.ConstantStringArray ||
                Value.HandleType == HandleType.ConstantStringValue ||
                Value.HandleType == HandleType.ConstantUInt16Array ||
                Value.HandleType == HandleType.ConstantUInt16Value ||
                Value.HandleType == HandleType.ConstantUInt32Array ||
                Value.HandleType == HandleType.ConstantUInt32Value ||
                Value.HandleType == HandleType.ConstantUInt64Array ||
                Value.HandleType == HandleType.ConstantUInt64Value);
            writer.Write(Value);
        } // Save

        internal static NamedArgumentHandle AsHandle(NamedArgument record)
        {
            if (record == null)
            {
                return new NamedArgumentHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new NamedArgumentHandle Handle
        {
            get
            {
                return new NamedArgumentHandle(HandleOffset);
            }
        } // Handle

        public NamedArgumentMemberKind Flags;
        public ConstantStringValue Name;
        public MetadataRecord Type;
        public MetadataRecord Value;
    } // NamedArgument

    public partial class NamespaceDefinition : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.NamespaceDefinition;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            ParentScopeOrNamespace = visitor.Visit(this, ParentScopeOrNamespace);
            Name = visitor.Visit(this, Name);
            TypeDefinitions = visitor.Visit(this, TypeDefinitions);
            TypeForwarders = visitor.Visit(this, TypeForwarders);
            NamespaceDefinitions = visitor.Visit(this, NamespaceDefinitions);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as NamespaceDefinition;
            if (other == null)
                return false;
            if (!Equals(ParentScopeOrNamespace, other.ParentScopeOrNamespace))
                return false;
            if (!Equals(Name, other.Name))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 633688634;
            hash = ((hash << 13) - (hash >> 19)) ^ (ParentScopeOrNamespace == null ? 0 : ParentScopeOrNamespace.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(ParentScopeOrNamespace == null ||
                ParentScopeOrNamespace.HandleType == HandleType.NamespaceDefinition ||
                ParentScopeOrNamespace.HandleType == HandleType.ScopeDefinition);
            writer.Write(ParentScopeOrNamespace);
            writer.Write(Name);
            writer.Write(TypeDefinitions);
            writer.Write(TypeForwarders);
            writer.Write(NamespaceDefinitions);
        } // Save

        internal static NamespaceDefinitionHandle AsHandle(NamespaceDefinition record)
        {
            if (record == null)
            {
                return new NamespaceDefinitionHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new NamespaceDefinitionHandle Handle
        {
            get
            {
                return new NamespaceDefinitionHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord ParentScopeOrNamespace;
        public ConstantStringValue Name;
        public List<TypeDefinition> TypeDefinitions = new List<TypeDefinition>();
        public List<TypeForwarder> TypeForwarders = new List<TypeForwarder>();
        public List<NamespaceDefinition> NamespaceDefinitions = new List<NamespaceDefinition>();
    } // NamespaceDefinition

    public partial class NamespaceReference : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.NamespaceReference;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            ParentScopeOrNamespace = visitor.Visit(this, ParentScopeOrNamespace);
            Name = visitor.Visit(this, Name);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as NamespaceReference;
            if (other == null)
                return false;
            if (!Equals(ParentScopeOrNamespace, other.ParentScopeOrNamespace))
                return false;
            if (!Equals(Name, other.Name))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1563382231;
            hash = ((hash << 13) - (hash >> 19)) ^ (ParentScopeOrNamespace == null ? 0 : ParentScopeOrNamespace.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(ParentScopeOrNamespace == null ||
                ParentScopeOrNamespace.HandleType == HandleType.NamespaceReference ||
                ParentScopeOrNamespace.HandleType == HandleType.ScopeReference);
            writer.Write(ParentScopeOrNamespace);
            writer.Write(Name);
        } // Save

        internal static NamespaceReferenceHandle AsHandle(NamespaceReference record)
        {
            if (record == null)
            {
                return new NamespaceReferenceHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new NamespaceReferenceHandle Handle
        {
            get
            {
                return new NamespaceReferenceHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord ParentScopeOrNamespace;
        public ConstantStringValue Name;
    } // NamespaceReference

    public partial class Parameter : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.Parameter;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            DefaultValue = visitor.Visit(this, DefaultValue);
            CustomAttributes = visitor.Visit(this, CustomAttributes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as Parameter;
            if (other == null)
                return false;
            if (Flags != other.Flags)
                return false;
            if (Sequence != other.Sequence)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Equals(DefaultValue, other.DefaultValue))
                return false;
            if (!CustomAttributes.SequenceEqual(other.CustomAttributes))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1049753891;
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ Sequence.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (DefaultValue == null ? 0 : DefaultValue.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            writer.Write(Sequence);
            writer.Write(Name);
            Debug.Assert(DefaultValue == null ||
                DefaultValue.HandleType == HandleType.TypeDefinition ||
                DefaultValue.HandleType == HandleType.TypeReference ||
                DefaultValue.HandleType == HandleType.TypeSpecification ||
                DefaultValue.HandleType == HandleType.ConstantBooleanArray ||
                DefaultValue.HandleType == HandleType.ConstantBooleanValue ||
                DefaultValue.HandleType == HandleType.ConstantByteArray ||
                DefaultValue.HandleType == HandleType.ConstantByteValue ||
                DefaultValue.HandleType == HandleType.ConstantCharArray ||
                DefaultValue.HandleType == HandleType.ConstantCharValue ||
                DefaultValue.HandleType == HandleType.ConstantDoubleArray ||
                DefaultValue.HandleType == HandleType.ConstantDoubleValue ||
                DefaultValue.HandleType == HandleType.ConstantEnumArray ||
                DefaultValue.HandleType == HandleType.ConstantHandleArray ||
                DefaultValue.HandleType == HandleType.ConstantInt16Array ||
                DefaultValue.HandleType == HandleType.ConstantInt16Value ||
                DefaultValue.HandleType == HandleType.ConstantInt32Array ||
                DefaultValue.HandleType == HandleType.ConstantInt32Value ||
                DefaultValue.HandleType == HandleType.ConstantInt64Array ||
                DefaultValue.HandleType == HandleType.ConstantInt64Value ||
                DefaultValue.HandleType == HandleType.ConstantReferenceValue ||
                DefaultValue.HandleType == HandleType.ConstantSByteArray ||
                DefaultValue.HandleType == HandleType.ConstantSByteValue ||
                DefaultValue.HandleType == HandleType.ConstantSingleArray ||
                DefaultValue.HandleType == HandleType.ConstantSingleValue ||
                DefaultValue.HandleType == HandleType.ConstantStringArray ||
                DefaultValue.HandleType == HandleType.ConstantStringValue ||
                DefaultValue.HandleType == HandleType.ConstantUInt16Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt16Value ||
                DefaultValue.HandleType == HandleType.ConstantUInt32Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt32Value ||
                DefaultValue.HandleType == HandleType.ConstantUInt64Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt64Value);
            writer.Write(DefaultValue);
            writer.Write(CustomAttributes);
        } // Save

        internal static ParameterHandle AsHandle(Parameter record)
        {
            if (record == null)
            {
                return new ParameterHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ParameterHandle Handle
        {
            get
            {
                return new ParameterHandle(HandleOffset);
            }
        } // Handle

        public ParameterAttributes Flags;
        public ushort Sequence;
        public ConstantStringValue Name;
        public MetadataRecord DefaultValue;
        public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
    } // Parameter

    public partial class PointerSignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.PointerSignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Type = visitor.Visit(this, Type);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as PointerSignature;
            if (other == null)
                return false;
            if (!Equals(Type, other.Type))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 747249584;
            hash = ((hash << 13) - (hash >> 19)) ^ (Type == null ? 0 : Type.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Type == null ||
                Type.HandleType == HandleType.TypeDefinition ||
                Type.HandleType == HandleType.TypeReference ||
                Type.HandleType == HandleType.TypeSpecification ||
                Type.HandleType == HandleType.ModifiedType);
            writer.Write(Type);
        } // Save

        internal static PointerSignatureHandle AsHandle(PointerSignature record)
        {
            if (record == null)
            {
                return new PointerSignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new PointerSignatureHandle Handle
        {
            get
            {
                return new PointerSignatureHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord Type;
    } // PointerSignature

    public partial class Property : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.Property;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            Signature = visitor.Visit(this, Signature);
            MethodSemantics = visitor.Visit(this, MethodSemantics);
            DefaultValue = visitor.Visit(this, DefaultValue);
            CustomAttributes = visitor.Visit(this, CustomAttributes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as Property;
            if (other == null)
                return false;
            if (Flags != other.Flags)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Equals(Signature, other.Signature))
                return false;
            if (!MethodSemantics.SequenceEqual(other.MethodSemantics))
                return false;
            if (!Equals(DefaultValue, other.DefaultValue))
                return false;
            if (!CustomAttributes.SequenceEqual(other.CustomAttributes))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1324612544;
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Signature == null ? 0 : Signature.GetHashCode());
            if (MethodSemantics != null)
            {
            for (int i = 0; i < MethodSemantics.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (MethodSemantics[i] == null ? 0 : MethodSemantics[i].GetHashCode());
                }
            }
            hash = ((hash << 13) - (hash >> 19)) ^ (DefaultValue == null ? 0 : DefaultValue.GetHashCode());
            if (CustomAttributes != null)
            {
            for (int i = 0; i < CustomAttributes.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (CustomAttributes[i] == null ? 0 : CustomAttributes[i].GetHashCode());
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            writer.Write(Name);
            writer.Write(Signature);
            writer.Write(MethodSemantics);
            Debug.Assert(DefaultValue == null ||
                DefaultValue.HandleType == HandleType.TypeDefinition ||
                DefaultValue.HandleType == HandleType.TypeReference ||
                DefaultValue.HandleType == HandleType.TypeSpecification ||
                DefaultValue.HandleType == HandleType.ConstantBooleanArray ||
                DefaultValue.HandleType == HandleType.ConstantBooleanValue ||
                DefaultValue.HandleType == HandleType.ConstantByteArray ||
                DefaultValue.HandleType == HandleType.ConstantByteValue ||
                DefaultValue.HandleType == HandleType.ConstantCharArray ||
                DefaultValue.HandleType == HandleType.ConstantCharValue ||
                DefaultValue.HandleType == HandleType.ConstantDoubleArray ||
                DefaultValue.HandleType == HandleType.ConstantDoubleValue ||
                DefaultValue.HandleType == HandleType.ConstantEnumArray ||
                DefaultValue.HandleType == HandleType.ConstantHandleArray ||
                DefaultValue.HandleType == HandleType.ConstantInt16Array ||
                DefaultValue.HandleType == HandleType.ConstantInt16Value ||
                DefaultValue.HandleType == HandleType.ConstantInt32Array ||
                DefaultValue.HandleType == HandleType.ConstantInt32Value ||
                DefaultValue.HandleType == HandleType.ConstantInt64Array ||
                DefaultValue.HandleType == HandleType.ConstantInt64Value ||
                DefaultValue.HandleType == HandleType.ConstantReferenceValue ||
                DefaultValue.HandleType == HandleType.ConstantSByteArray ||
                DefaultValue.HandleType == HandleType.ConstantSByteValue ||
                DefaultValue.HandleType == HandleType.ConstantSingleArray ||
                DefaultValue.HandleType == HandleType.ConstantSingleValue ||
                DefaultValue.HandleType == HandleType.ConstantStringArray ||
                DefaultValue.HandleType == HandleType.ConstantStringValue ||
                DefaultValue.HandleType == HandleType.ConstantUInt16Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt16Value ||
                DefaultValue.HandleType == HandleType.ConstantUInt32Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt32Value ||
                DefaultValue.HandleType == HandleType.ConstantUInt64Array ||
                DefaultValue.HandleType == HandleType.ConstantUInt64Value);
            writer.Write(DefaultValue);
            writer.Write(CustomAttributes);
        } // Save

        internal static PropertyHandle AsHandle(Property record)
        {
            if (record == null)
            {
                return new PropertyHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new PropertyHandle Handle
        {
            get
            {
                return new PropertyHandle(HandleOffset);
            }
        } // Handle

        public PropertyAttributes Flags;
        public ConstantStringValue Name;
        public PropertySignature Signature;
        public List<MethodSemantics> MethodSemantics = new List<MethodSemantics>();
        public MetadataRecord DefaultValue;
        public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
    } // Property

    public partial class PropertySignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.PropertySignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Type = visitor.Visit(this, Type);
            Parameters = visitor.Visit(this, Parameters);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as PropertySignature;
            if (other == null)
                return false;
            if (CallingConvention != other.CallingConvention)
                return false;
            if (!Equals(Type, other.Type))
                return false;
            if (!Parameters.SequenceEqual(other.Parameters))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1535652143;
            hash = ((hash << 13) - (hash >> 19)) ^ CallingConvention.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Type == null ? 0 : Type.GetHashCode());
            if (Parameters != null)
            {
            for (int i = 0; i < Parameters.Count; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ (Parameters[i] == null ? 0 : Parameters[i].GetHashCode());
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(CallingConvention);
            Debug.Assert(Type == null ||
                Type.HandleType == HandleType.TypeDefinition ||
                Type.HandleType == HandleType.TypeReference ||
                Type.HandleType == HandleType.TypeSpecification ||
                Type.HandleType == HandleType.ModifiedType);
            writer.Write(Type);
            Debug.Assert(Parameters.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.TypeDefinition ||
                handle.HandleType == HandleType.TypeReference ||
                handle.HandleType == HandleType.TypeSpecification ||
                handle.HandleType == HandleType.ModifiedType));
            writer.Write(Parameters);
        } // Save

        internal static PropertySignatureHandle AsHandle(PropertySignature record)
        {
            if (record == null)
            {
                return new PropertySignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new PropertySignatureHandle Handle
        {
            get
            {
                return new PropertySignatureHandle(HandleOffset);
            }
        } // Handle

        public CallingConventions CallingConvention;
        public MetadataRecord Type;
        public List<MetadataRecord> Parameters = new List<MetadataRecord>();
    } // PropertySignature

    public partial class QualifiedField : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.QualifiedField;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Field = visitor.Visit(this, Field);
            EnclosingType = visitor.Visit(this, EnclosingType);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as QualifiedField;
            if (other == null)
                return false;
            if (!Equals(Field, other.Field))
                return false;
            if (!Equals(EnclosingType, other.EnclosingType))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1470437688;
            hash = ((hash << 13) - (hash >> 19)) ^ (Field == null ? 0 : Field.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (EnclosingType == null ? 0 : EnclosingType.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Field);
            writer.Write(EnclosingType);
        } // Save

        internal static QualifiedFieldHandle AsHandle(QualifiedField record)
        {
            if (record == null)
            {
                return new QualifiedFieldHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new QualifiedFieldHandle Handle
        {
            get
            {
                return new QualifiedFieldHandle(HandleOffset);
            }
        } // Handle

        public Field Field;
        public TypeDefinition EnclosingType;
    } // QualifiedField

    public partial class QualifiedMethod : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.QualifiedMethod;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Method = visitor.Visit(this, Method);
            EnclosingType = visitor.Visit(this, EnclosingType);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as QualifiedMethod;
            if (other == null)
                return false;
            if (!Equals(Method, other.Method))
                return false;
            if (!Equals(EnclosingType, other.EnclosingType))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -620024567;
            hash = ((hash << 13) - (hash >> 19)) ^ (Method == null ? 0 : Method.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (EnclosingType == null ? 0 : EnclosingType.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Method);
            writer.Write(EnclosingType);
        } // Save

        internal static QualifiedMethodHandle AsHandle(QualifiedMethod record)
        {
            if (record == null)
            {
                return new QualifiedMethodHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new QualifiedMethodHandle Handle
        {
            get
            {
                return new QualifiedMethodHandle(HandleOffset);
            }
        } // Handle

        public Method Method;
        public TypeDefinition EnclosingType;
    } // QualifiedMethod

    public partial class SZArraySignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.SZArraySignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            ElementType = visitor.Visit(this, ElementType);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as SZArraySignature;
            if (other == null)
                return false;
            if (!Equals(ElementType, other.ElementType))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -179535243;
            hash = ((hash << 13) - (hash >> 19)) ^ (ElementType == null ? 0 : ElementType.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(ElementType == null ||
                ElementType.HandleType == HandleType.TypeDefinition ||
                ElementType.HandleType == HandleType.TypeReference ||
                ElementType.HandleType == HandleType.TypeSpecification ||
                ElementType.HandleType == HandleType.ModifiedType);
            writer.Write(ElementType);
        } // Save

        internal static SZArraySignatureHandle AsHandle(SZArraySignature record)
        {
            if (record == null)
            {
                return new SZArraySignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new SZArraySignatureHandle Handle
        {
            get
            {
                return new SZArraySignatureHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord ElementType;
    } // SZArraySignature

    public partial class ScopeDefinition : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ScopeDefinition;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            Culture = visitor.Visit(this, Culture);
            RootNamespaceDefinition = visitor.Visit(this, RootNamespaceDefinition);
            EntryPoint = visitor.Visit(this, EntryPoint);
            GlobalModuleType = visitor.Visit(this, GlobalModuleType);
            CustomAttributes = visitor.Visit(this, CustomAttributes);
            ModuleName = visitor.Visit(this, ModuleName);
            ModuleCustomAttributes = visitor.Visit(this, ModuleCustomAttributes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ScopeDefinition;
            if (other == null)
                return false;
            if (Flags != other.Flags)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (HashAlgorithm != other.HashAlgorithm)
                return false;
            if (MajorVersion != other.MajorVersion)
                return false;
            if (MinorVersion != other.MinorVersion)
                return false;
            if (BuildNumber != other.BuildNumber)
                return false;
            if (RevisionNumber != other.RevisionNumber)
                return false;
            if (!PublicKey.SequenceEqual(other.PublicKey))
                return false;
            if (!Equals(Culture, other.Culture))
                return false;
            if (!Equals(ModuleName, other.ModuleName))
                return false;
            if (!Mvid.SequenceEqual(other.Mvid))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 1490364984;
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ HashAlgorithm.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ MajorVersion.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ MinorVersion.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ BuildNumber.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ RevisionNumber.GetHashCode();
            if (PublicKey != null)
            {
                for (int i = 0; i < PublicKey.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ PublicKey[i].GetHashCode();
                }
            }
            hash = ((hash << 13) - (hash >> 19)) ^ (Culture == null ? 0 : Culture.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (ModuleName == null ? 0 : ModuleName.GetHashCode());
            if (Mvid != null)
            {
                for (int i = 0; i < Mvid.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ Mvid[i].GetHashCode();
                }
            }
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            writer.Write(Name);
            writer.Write(HashAlgorithm);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write(BuildNumber);
            writer.Write(RevisionNumber);
            writer.Write(PublicKey);
            writer.Write(Culture);
            writer.Write(RootNamespaceDefinition);
            writer.Write(EntryPoint);
            writer.Write(GlobalModuleType);
            writer.Write(CustomAttributes);
            writer.Write(ModuleName);
            writer.Write(Mvid);
            writer.Write(ModuleCustomAttributes);
        } // Save

        internal static ScopeDefinitionHandle AsHandle(ScopeDefinition record)
        {
            if (record == null)
            {
                return new ScopeDefinitionHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ScopeDefinitionHandle Handle
        {
            get
            {
                return new ScopeDefinitionHandle(HandleOffset);
            }
        } // Handle

        public AssemblyFlags Flags;
        public ConstantStringValue Name;
        public AssemblyHashAlgorithm HashAlgorithm;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort BuildNumber;
        public ushort RevisionNumber;
        public byte[] PublicKey;
        public ConstantStringValue Culture;
        public NamespaceDefinition RootNamespaceDefinition;
        public QualifiedMethod EntryPoint;
        public TypeDefinition GlobalModuleType;
        public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
        public ConstantStringValue ModuleName;
        public byte[] Mvid;
        public List<CustomAttribute> ModuleCustomAttributes = new List<CustomAttribute>();
    } // ScopeDefinition

    public partial class ScopeReference : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.ScopeReference;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Name = visitor.Visit(this, Name);
            Culture = visitor.Visit(this, Culture);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ScopeReference;
            if (other == null)
                return false;
            if (Flags != other.Flags)
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (MajorVersion != other.MajorVersion)
                return false;
            if (MinorVersion != other.MinorVersion)
                return false;
            if (BuildNumber != other.BuildNumber)
                return false;
            if (RevisionNumber != other.RevisionNumber)
                return false;
            if (!PublicKeyOrToken.SequenceEqual(other.PublicKeyOrToken))
                return false;
            if (!Equals(Culture, other.Culture))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 2056651797;
            hash = ((hash << 13) - (hash >> 19)) ^ Flags.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ MajorVersion.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ MinorVersion.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ BuildNumber.GetHashCode();
            hash = ((hash << 13) - (hash >> 19)) ^ RevisionNumber.GetHashCode();
            if (PublicKeyOrToken != null)
            {
                for (int i = 0; i < PublicKeyOrToken.Length; i++)
                {
                    hash = ((hash << 13) - (hash >> 19)) ^ PublicKeyOrToken[i].GetHashCode();
                }
            }
            hash = ((hash << 13) - (hash >> 19)) ^ (Culture == null ? 0 : Culture.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            writer.Write(Name);
            writer.Write(MajorVersion);
            writer.Write(MinorVersion);
            writer.Write(BuildNumber);
            writer.Write(RevisionNumber);
            writer.Write(PublicKeyOrToken);
            writer.Write(Culture);
        } // Save

        internal static ScopeReferenceHandle AsHandle(ScopeReference record)
        {
            if (record == null)
            {
                return new ScopeReferenceHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new ScopeReferenceHandle Handle
        {
            get
            {
                return new ScopeReferenceHandle(HandleOffset);
            }
        } // Handle

        public AssemblyFlags Flags;
        public ConstantStringValue Name;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort BuildNumber;
        public ushort RevisionNumber;
        public byte[] PublicKeyOrToken;
        public ConstantStringValue Culture;
    } // ScopeReference

    public partial class TypeDefinition : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.TypeDefinition;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            BaseType = visitor.Visit(this, BaseType);
            NamespaceDefinition = visitor.Visit(this, NamespaceDefinition);
            Name = visitor.Visit(this, Name);
            EnclosingType = visitor.Visit(this, EnclosingType);
            NestedTypes = visitor.Visit(this, NestedTypes);
            Methods = visitor.Visit(this, Methods);
            Fields = visitor.Visit(this, Fields);
            Properties = visitor.Visit(this, Properties);
            Events = visitor.Visit(this, Events);
            GenericParameters = visitor.Visit(this, GenericParameters);
            Interfaces = visitor.Visit(this, Interfaces);
            CustomAttributes = visitor.Visit(this, CustomAttributes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as TypeDefinition;
            if (other == null)
                return false;
            if (!Equals(NamespaceDefinition, other.NamespaceDefinition))
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!Equals(EnclosingType, other.EnclosingType))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -1095947977;
            hash = ((hash << 13) - (hash >> 19)) ^ (NamespaceDefinition == null ? 0 : NamespaceDefinition.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (EnclosingType == null ? 0 : EnclosingType.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Flags);
            Debug.Assert(BaseType == null ||
                BaseType.HandleType == HandleType.TypeDefinition ||
                BaseType.HandleType == HandleType.TypeReference ||
                BaseType.HandleType == HandleType.TypeSpecification);
            writer.Write(BaseType);
            writer.Write(NamespaceDefinition);
            writer.Write(Name);
            writer.Write(Size);
            writer.Write(PackingSize);
            writer.Write(EnclosingType);
            writer.Write(NestedTypes);
            writer.Write(Methods);
            writer.Write(Fields);
            writer.Write(Properties);
            writer.Write(Events);
            writer.Write(GenericParameters);
            Debug.Assert(Interfaces.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.TypeDefinition ||
                handle.HandleType == HandleType.TypeReference ||
                handle.HandleType == HandleType.TypeSpecification));
            writer.Write(Interfaces);
            writer.Write(CustomAttributes);
        } // Save

        internal static TypeDefinitionHandle AsHandle(TypeDefinition record)
        {
            if (record == null)
            {
                return new TypeDefinitionHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new TypeDefinitionHandle Handle
        {
            get
            {
                return new TypeDefinitionHandle(HandleOffset);
            }
        } // Handle

        public TypeAttributes Flags;
        public MetadataRecord BaseType;
        public NamespaceDefinition NamespaceDefinition;
        public ConstantStringValue Name;
        public uint Size;
        public ushort PackingSize;
        public TypeDefinition EnclosingType;
        public List<TypeDefinition> NestedTypes = new List<TypeDefinition>();
        public List<Method> Methods = new List<Method>();
        public List<Field> Fields = new List<Field>();
        public List<Property> Properties = new List<Property>();
        public List<Event> Events = new List<Event>();
        public List<GenericParameter> GenericParameters = new List<GenericParameter>();
        public List<MetadataRecord> Interfaces = new List<MetadataRecord>();
        public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();
    } // TypeDefinition

    public partial class TypeForwarder : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.TypeForwarder;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Scope = visitor.Visit(this, Scope);
            Name = visitor.Visit(this, Name);
            NestedTypes = visitor.Visit(this, NestedTypes);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as TypeForwarder;
            if (other == null)
                return false;
            if (!Equals(Scope, other.Scope))
                return false;
            if (!Equals(Name, other.Name))
                return false;
            if (!NestedTypes.SequenceEqual(other.NestedTypes))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 467167184;
            hash = ((hash << 13) - (hash >> 19)) ^ (Scope == null ? 0 : Scope.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (Name == null ? 0 : Name.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Scope);
            writer.Write(Name);
            writer.Write(NestedTypes);
        } // Save

        internal static TypeForwarderHandle AsHandle(TypeForwarder record)
        {
            if (record == null)
            {
                return new TypeForwarderHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new TypeForwarderHandle Handle
        {
            get
            {
                return new TypeForwarderHandle(HandleOffset);
            }
        } // Handle

        public ScopeReference Scope;
        public ConstantStringValue Name;
        public List<TypeForwarder> NestedTypes = new List<TypeForwarder>();
    } // TypeForwarder

    public partial class TypeInstantiationSignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.TypeInstantiationSignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            GenericType = visitor.Visit(this, GenericType);
            GenericTypeArguments = visitor.Visit(this, GenericTypeArguments);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as TypeInstantiationSignature;
            if (other == null)
                return false;
            if (!Equals(GenericType, other.GenericType))
                return false;
            if (!GenericTypeArguments.SequenceEqual(other.GenericTypeArguments))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 770132338;
            hash = ((hash << 13) - (hash >> 19)) ^ (GenericType == null ? 0 : GenericType.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(GenericType == null ||
                GenericType.HandleType == HandleType.TypeDefinition ||
                GenericType.HandleType == HandleType.TypeReference ||
                GenericType.HandleType == HandleType.TypeSpecification);
            writer.Write(GenericType);
            Debug.Assert(GenericTypeArguments.TrueForAll(handle => handle == null ||
                handle.HandleType == HandleType.TypeDefinition ||
                handle.HandleType == HandleType.TypeReference ||
                handle.HandleType == HandleType.TypeSpecification));
            writer.Write(GenericTypeArguments);
        } // Save

        internal static TypeInstantiationSignatureHandle AsHandle(TypeInstantiationSignature record)
        {
            if (record == null)
            {
                return new TypeInstantiationSignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new TypeInstantiationSignatureHandle Handle
        {
            get
            {
                return new TypeInstantiationSignatureHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord GenericType;
        public List<MetadataRecord> GenericTypeArguments = new List<MetadataRecord>();
    } // TypeInstantiationSignature

    public partial class TypeReference : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.TypeReference;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            ParentNamespaceOrType = visitor.Visit(this, ParentNamespaceOrType);
            TypeName = visitor.Visit(this, TypeName);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as TypeReference;
            if (other == null)
                return false;
            if (!Equals(ParentNamespaceOrType, other.ParentNamespaceOrType))
                return false;
            if (!Equals(TypeName, other.TypeName))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -540108450;
            hash = ((hash << 13) - (hash >> 19)) ^ (ParentNamespaceOrType == null ? 0 : ParentNamespaceOrType.GetHashCode());
            hash = ((hash << 13) - (hash >> 19)) ^ (TypeName == null ? 0 : TypeName.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(ParentNamespaceOrType == null ||
                ParentNamespaceOrType.HandleType == HandleType.NamespaceReference ||
                ParentNamespaceOrType.HandleType == HandleType.TypeReference);
            writer.Write(ParentNamespaceOrType);
            writer.Write(TypeName);
        } // Save

        internal static TypeReferenceHandle AsHandle(TypeReference record)
        {
            if (record == null)
            {
                return new TypeReferenceHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new TypeReferenceHandle Handle
        {
            get
            {
                return new TypeReferenceHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord ParentNamespaceOrType;
        public ConstantStringValue TypeName;
    } // TypeReference

    public partial class TypeSpecification : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.TypeSpecification;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
            Signature = visitor.Visit(this, Signature);
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as TypeSpecification;
            if (other == null)
                return false;
            if (!Equals(Signature, other.Signature))
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = -902636182;
            hash = ((hash << 13) - (hash >> 19)) ^ (Signature == null ? 0 : Signature.GetHashCode());
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            Debug.Assert(Signature == null ||
                Signature.HandleType == HandleType.TypeDefinition ||
                Signature.HandleType == HandleType.TypeReference ||
                Signature.HandleType == HandleType.TypeInstantiationSignature ||
                Signature.HandleType == HandleType.SZArraySignature ||
                Signature.HandleType == HandleType.ArraySignature ||
                Signature.HandleType == HandleType.PointerSignature ||
                Signature.HandleType == HandleType.FunctionPointerSignature ||
                Signature.HandleType == HandleType.ByReferenceSignature ||
                Signature.HandleType == HandleType.TypeVariableSignature ||
                Signature.HandleType == HandleType.MethodTypeVariableSignature);
            writer.Write(Signature);
        } // Save

        internal static TypeSpecificationHandle AsHandle(TypeSpecification record)
        {
            if (record == null)
            {
                return new TypeSpecificationHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new TypeSpecificationHandle Handle
        {
            get
            {
                return new TypeSpecificationHandle(HandleOffset);
            }
        } // Handle

        public MetadataRecord Signature;
    } // TypeSpecification

    public partial class TypeVariableSignature : MetadataRecord
    {
        public override HandleType HandleType
        {
            get
            {
                return HandleType.TypeVariableSignature;
            }
        } // HandleType

        internal override void Visit(IRecordVisitor visitor)
        {
        } // Visit

        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as TypeVariableSignature;
            if (other == null)
                return false;
            if (Number != other.Number)
                return false;
            return true;
        } // Equals

        public sealed override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            EnterGetHashCode();
            int hash = 711693641;
            hash = ((hash << 13) - (hash >> 19)) ^ Number.GetHashCode();
            LeaveGetHashCode();
            _hash = hash;
            return _hash;
        } // GetHashCode

        internal override void Save(NativeWriter writer)
        {
            writer.Write(Number);
        } // Save

        internal static TypeVariableSignatureHandle AsHandle(TypeVariableSignature record)
        {
            if (record == null)
            {
                return new TypeVariableSignatureHandle(0);
            }
            else
            {
                return record.Handle;
            }
        } // AsHandle

        internal new TypeVariableSignatureHandle Handle
        {
            get
            {
                return new TypeVariableSignatureHandle(HandleOffset);
            }
        } // Handle

        public int Number;
    } // TypeVariableSignature
} // Internal.Metadata.NativeFormat.Writer
