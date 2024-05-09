// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This is a generated file - do not manually edit!

#pragma warning disable 649
#pragma warning disable 169
#pragma warning disable 282 // There is no defined ordering between fields in multiple declarations of partial class or struct
#pragma warning disable CA1066 // IEquatable<T> implementations aren't used
#pragma warning disable CA1822
#pragma warning disable IDE0059
#pragma warning disable SA1121
#pragma warning disable IDE0036, SA1129

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Internal.NativeFormat;

namespace Internal.Metadata.NativeFormat
{
#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ArraySignature
    {
        private readonly MetadataReader _reader;
        private readonly ArraySignatureHandle _handle;

        internal ArraySignature(MetadataReader reader, ArraySignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _elementType);
            offset = streamReader.Read(offset, out _rank);
            offset = streamReader.Read(offset, out _sizes);
            offset = streamReader.Read(offset, out _lowerBounds);
        }

        public ArraySignatureHandle Handle => _handle;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public Handle ElementType => _elementType;
        private readonly Handle _elementType;

        public int Rank => _rank;
        private readonly int _rank;

        public Int32Collection Sizes => _sizes;
        private readonly Int32Collection _sizes;

        public Int32Collection LowerBounds => _lowerBounds;
        private readonly Int32Collection _lowerBounds;
    } // ArraySignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ArraySignatureHandle
    {
        internal readonly int _value;

        internal ArraySignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal ArraySignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ArraySignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ArraySignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ArraySignatureHandle)
                return _value == ((ArraySignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ArraySignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ArraySignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ArraySignature GetArraySignature(MetadataReader reader)
            => new ArraySignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ArraySignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ArraySignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ByReferenceSignature
    {
        private readonly MetadataReader _reader;
        private readonly ByReferenceSignatureHandle _handle;

        internal ByReferenceSignature(MetadataReader reader, ByReferenceSignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _type);
        }

        public ByReferenceSignatureHandle Handle => _handle;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public Handle Type => _type;
        private readonly Handle _type;
    } // ByReferenceSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ByReferenceSignatureHandle
    {
        internal readonly int _value;

        internal ByReferenceSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal ByReferenceSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ByReferenceSignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ByReferenceSignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ByReferenceSignatureHandle)
                return _value == ((ByReferenceSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ByReferenceSignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ByReferenceSignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ByReferenceSignature GetByReferenceSignature(MetadataReader reader)
            => new ByReferenceSignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ByReferenceSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ByReferenceSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantBooleanArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantBooleanArrayHandle _handle;

        internal ConstantBooleanArray(MetadataReader reader, ConstantBooleanArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantBooleanArrayHandle Handle => _handle;

        public BooleanCollection Value => _value;
        private readonly BooleanCollection _value;
    } // ConstantBooleanArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantBooleanArrayHandle
    {
        internal readonly int _value;

        internal ConstantBooleanArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantBooleanArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantBooleanArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantBooleanArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantBooleanArrayHandle)
                return _value == ((ConstantBooleanArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantBooleanArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantBooleanArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantBooleanArray GetConstantBooleanArray(MetadataReader reader)
            => new ConstantBooleanArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantBooleanArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantBooleanArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantBooleanValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantBooleanValueHandle _handle;

        internal ConstantBooleanValue(MetadataReader reader, ConstantBooleanValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantBooleanValueHandle Handle => _handle;

        public bool Value => _value;
        private readonly bool _value;
    } // ConstantBooleanValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantBooleanValueHandle
    {
        internal readonly int _value;

        internal ConstantBooleanValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantBooleanValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantBooleanValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantBooleanValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantBooleanValueHandle)
                return _value == ((ConstantBooleanValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantBooleanValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantBooleanValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantBooleanValue GetConstantBooleanValue(MetadataReader reader)
            => new ConstantBooleanValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantBooleanValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantBooleanValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantByteArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantByteArrayHandle _handle;

        internal ConstantByteArray(MetadataReader reader, ConstantByteArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantByteArrayHandle Handle => _handle;

        public ByteCollection Value => _value;
        private readonly ByteCollection _value;
    } // ConstantByteArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantByteArrayHandle
    {
        internal readonly int _value;

        internal ConstantByteArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantByteArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantByteArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantByteArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantByteArrayHandle)
                return _value == ((ConstantByteArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantByteArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantByteArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantByteArray GetConstantByteArray(MetadataReader reader)
            => new ConstantByteArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantByteArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantByteArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantByteValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantByteValueHandle _handle;

        internal ConstantByteValue(MetadataReader reader, ConstantByteValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantByteValueHandle Handle => _handle;

        public byte Value => _value;
        private readonly byte _value;
    } // ConstantByteValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantByteValueHandle
    {
        internal readonly int _value;

        internal ConstantByteValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantByteValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantByteValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantByteValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantByteValueHandle)
                return _value == ((ConstantByteValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantByteValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantByteValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantByteValue GetConstantByteValue(MetadataReader reader)
            => new ConstantByteValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantByteValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantByteValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantCharArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantCharArrayHandle _handle;

        internal ConstantCharArray(MetadataReader reader, ConstantCharArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantCharArrayHandle Handle => _handle;

        public CharCollection Value => _value;
        private readonly CharCollection _value;
    } // ConstantCharArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantCharArrayHandle
    {
        internal readonly int _value;

        internal ConstantCharArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantCharArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantCharArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantCharArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantCharArrayHandle)
                return _value == ((ConstantCharArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantCharArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantCharArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantCharArray GetConstantCharArray(MetadataReader reader)
            => new ConstantCharArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantCharArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantCharArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantCharValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantCharValueHandle _handle;

        internal ConstantCharValue(MetadataReader reader, ConstantCharValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantCharValueHandle Handle => _handle;

        public char Value => _value;
        private readonly char _value;
    } // ConstantCharValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantCharValueHandle
    {
        internal readonly int _value;

        internal ConstantCharValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantCharValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantCharValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantCharValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantCharValueHandle)
                return _value == ((ConstantCharValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantCharValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantCharValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantCharValue GetConstantCharValue(MetadataReader reader)
            => new ConstantCharValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantCharValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantCharValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantDoubleArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantDoubleArrayHandle _handle;

        internal ConstantDoubleArray(MetadataReader reader, ConstantDoubleArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantDoubleArrayHandle Handle => _handle;

        public DoubleCollection Value => _value;
        private readonly DoubleCollection _value;
    } // ConstantDoubleArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantDoubleArrayHandle
    {
        internal readonly int _value;

        internal ConstantDoubleArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantDoubleArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantDoubleArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantDoubleArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantDoubleArrayHandle)
                return _value == ((ConstantDoubleArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantDoubleArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantDoubleArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantDoubleArray GetConstantDoubleArray(MetadataReader reader)
            => new ConstantDoubleArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantDoubleArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantDoubleArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantDoubleValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantDoubleValueHandle _handle;

        internal ConstantDoubleValue(MetadataReader reader, ConstantDoubleValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantDoubleValueHandle Handle => _handle;

        public double Value => _value;
        private readonly double _value;
    } // ConstantDoubleValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantDoubleValueHandle
    {
        internal readonly int _value;

        internal ConstantDoubleValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantDoubleValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantDoubleValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantDoubleValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantDoubleValueHandle)
                return _value == ((ConstantDoubleValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantDoubleValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantDoubleValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantDoubleValue GetConstantDoubleValue(MetadataReader reader)
            => new ConstantDoubleValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantDoubleValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantDoubleValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantEnumArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantEnumArrayHandle _handle;

        internal ConstantEnumArray(MetadataReader reader, ConstantEnumArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _elementType);
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantEnumArrayHandle Handle => _handle;

        public Handle ElementType => _elementType;
        private readonly Handle _elementType;

        public Handle Value => _value;
        private readonly Handle _value;
    } // ConstantEnumArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantEnumArrayHandle
    {
        internal readonly int _value;

        internal ConstantEnumArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantEnumArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantEnumArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantEnumArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantEnumArrayHandle)
                return _value == ((ConstantEnumArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantEnumArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantEnumArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantEnumArray GetConstantEnumArray(MetadataReader reader)
            => new ConstantEnumArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantEnumArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantEnumArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantEnumValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantEnumValueHandle _handle;

        internal ConstantEnumValue(MetadataReader reader, ConstantEnumValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
            offset = streamReader.Read(offset, out _type);
        }

        public ConstantEnumValueHandle Handle => _handle;

        public Handle Value => _value;
        private readonly Handle _value;

        public Handle Type => _type;
        private readonly Handle _type;
    } // ConstantEnumValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantEnumValueHandle
    {
        internal readonly int _value;

        internal ConstantEnumValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantEnumValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantEnumValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantEnumValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantEnumValueHandle)
                return _value == ((ConstantEnumValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantEnumValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantEnumValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantEnumValue GetConstantEnumValue(MetadataReader reader)
            => new ConstantEnumValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantEnumValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantEnumValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantHandleArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantHandleArrayHandle _handle;

        internal ConstantHandleArray(MetadataReader reader, ConstantHandleArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantHandleArrayHandle Handle => _handle;

        public HandleCollection Value => _value;
        private readonly HandleCollection _value;
    } // ConstantHandleArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantHandleArrayHandle
    {
        internal readonly int _value;

        internal ConstantHandleArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantHandleArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantHandleArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantHandleArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantHandleArrayHandle)
                return _value == ((ConstantHandleArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantHandleArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantHandleArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantHandleArray GetConstantHandleArray(MetadataReader reader)
            => new ConstantHandleArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantHandleArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantHandleArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt16Array
    {
        private readonly MetadataReader _reader;
        private readonly ConstantInt16ArrayHandle _handle;

        internal ConstantInt16Array(MetadataReader reader, ConstantInt16ArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantInt16ArrayHandle Handle => _handle;

        public Int16Collection Value => _value;
        private readonly Int16Collection _value;
    } // ConstantInt16Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt16ArrayHandle
    {
        internal readonly int _value;

        internal ConstantInt16ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt16ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantInt16Array || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt16Array) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantInt16ArrayHandle)
                return _value == ((ConstantInt16ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt16ArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantInt16ArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantInt16Array GetConstantInt16Array(MetadataReader reader)
            => new ConstantInt16Array(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt16Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantInt16ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt16Value
    {
        private readonly MetadataReader _reader;
        private readonly ConstantInt16ValueHandle _handle;

        internal ConstantInt16Value(MetadataReader reader, ConstantInt16ValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantInt16ValueHandle Handle => _handle;

        public short Value => _value;
        private readonly short _value;
    } // ConstantInt16Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt16ValueHandle
    {
        internal readonly int _value;

        internal ConstantInt16ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt16ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantInt16Value || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt16Value) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantInt16ValueHandle)
                return _value == ((ConstantInt16ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt16ValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantInt16ValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantInt16Value GetConstantInt16Value(MetadataReader reader)
            => new ConstantInt16Value(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt16Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantInt16ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt32Array
    {
        private readonly MetadataReader _reader;
        private readonly ConstantInt32ArrayHandle _handle;

        internal ConstantInt32Array(MetadataReader reader, ConstantInt32ArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantInt32ArrayHandle Handle => _handle;

        public Int32Collection Value => _value;
        private readonly Int32Collection _value;
    } // ConstantInt32Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt32ArrayHandle
    {
        internal readonly int _value;

        internal ConstantInt32ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt32ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantInt32Array || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt32Array) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantInt32ArrayHandle)
                return _value == ((ConstantInt32ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt32ArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantInt32ArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantInt32Array GetConstantInt32Array(MetadataReader reader)
            => new ConstantInt32Array(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt32Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantInt32ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt32Value
    {
        private readonly MetadataReader _reader;
        private readonly ConstantInt32ValueHandle _handle;

        internal ConstantInt32Value(MetadataReader reader, ConstantInt32ValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantInt32ValueHandle Handle => _handle;

        public int Value => _value;
        private readonly int _value;
    } // ConstantInt32Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt32ValueHandle
    {
        internal readonly int _value;

        internal ConstantInt32ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt32ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantInt32Value || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt32Value) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantInt32ValueHandle)
                return _value == ((ConstantInt32ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt32ValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantInt32ValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantInt32Value GetConstantInt32Value(MetadataReader reader)
            => new ConstantInt32Value(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt32Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantInt32ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt64Array
    {
        private readonly MetadataReader _reader;
        private readonly ConstantInt64ArrayHandle _handle;

        internal ConstantInt64Array(MetadataReader reader, ConstantInt64ArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantInt64ArrayHandle Handle => _handle;

        public Int64Collection Value => _value;
        private readonly Int64Collection _value;
    } // ConstantInt64Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt64ArrayHandle
    {
        internal readonly int _value;

        internal ConstantInt64ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt64ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantInt64Array || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt64Array) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantInt64ArrayHandle)
                return _value == ((ConstantInt64ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt64ArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantInt64ArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantInt64Array GetConstantInt64Array(MetadataReader reader)
            => new ConstantInt64Array(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt64Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantInt64ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt64Value
    {
        private readonly MetadataReader _reader;
        private readonly ConstantInt64ValueHandle _handle;

        internal ConstantInt64Value(MetadataReader reader, ConstantInt64ValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantInt64ValueHandle Handle => _handle;

        public long Value => _value;
        private readonly long _value;
    } // ConstantInt64Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantInt64ValueHandle
    {
        internal readonly int _value;

        internal ConstantInt64ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt64ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantInt64Value || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt64Value) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantInt64ValueHandle)
                return _value == ((ConstantInt64ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt64ValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantInt64ValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantInt64Value GetConstantInt64Value(MetadataReader reader)
            => new ConstantInt64Value(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt64Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantInt64ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantReferenceValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantReferenceValueHandle _handle;

        internal ConstantReferenceValue(MetadataReader reader, ConstantReferenceValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
        }

        public ConstantReferenceValueHandle Handle => _handle;
    } // ConstantReferenceValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantReferenceValueHandle
    {
        internal readonly int _value;

        internal ConstantReferenceValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantReferenceValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantReferenceValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantReferenceValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantReferenceValueHandle)
                return _value == ((ConstantReferenceValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantReferenceValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantReferenceValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantReferenceValue GetConstantReferenceValue(MetadataReader reader)
            => new ConstantReferenceValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantReferenceValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantReferenceValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantSByteArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantSByteArrayHandle _handle;

        internal ConstantSByteArray(MetadataReader reader, ConstantSByteArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantSByteArrayHandle Handle => _handle;

        public SByteCollection Value => _value;
        private readonly SByteCollection _value;
    } // ConstantSByteArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantSByteArrayHandle
    {
        internal readonly int _value;

        internal ConstantSByteArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantSByteArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantSByteArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantSByteArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantSByteArrayHandle)
                return _value == ((ConstantSByteArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantSByteArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantSByteArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantSByteArray GetConstantSByteArray(MetadataReader reader)
            => new ConstantSByteArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantSByteArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantSByteArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantSByteValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantSByteValueHandle _handle;

        internal ConstantSByteValue(MetadataReader reader, ConstantSByteValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantSByteValueHandle Handle => _handle;

        public sbyte Value => _value;
        private readonly sbyte _value;
    } // ConstantSByteValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantSByteValueHandle
    {
        internal readonly int _value;

        internal ConstantSByteValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantSByteValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantSByteValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantSByteValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantSByteValueHandle)
                return _value == ((ConstantSByteValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantSByteValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantSByteValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantSByteValue GetConstantSByteValue(MetadataReader reader)
            => new ConstantSByteValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantSByteValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantSByteValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantSingleArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantSingleArrayHandle _handle;

        internal ConstantSingleArray(MetadataReader reader, ConstantSingleArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantSingleArrayHandle Handle => _handle;

        public SingleCollection Value => _value;
        private readonly SingleCollection _value;
    } // ConstantSingleArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantSingleArrayHandle
    {
        internal readonly int _value;

        internal ConstantSingleArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantSingleArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantSingleArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantSingleArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantSingleArrayHandle)
                return _value == ((ConstantSingleArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantSingleArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantSingleArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantSingleArray GetConstantSingleArray(MetadataReader reader)
            => new ConstantSingleArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantSingleArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantSingleArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantSingleValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantSingleValueHandle _handle;

        internal ConstantSingleValue(MetadataReader reader, ConstantSingleValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantSingleValueHandle Handle => _handle;

        public float Value => _value;
        private readonly float _value;
    } // ConstantSingleValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantSingleValueHandle
    {
        internal readonly int _value;

        internal ConstantSingleValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantSingleValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantSingleValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantSingleValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantSingleValueHandle)
                return _value == ((ConstantSingleValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantSingleValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantSingleValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantSingleValue GetConstantSingleValue(MetadataReader reader)
            => new ConstantSingleValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantSingleValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantSingleValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantStringArray
    {
        private readonly MetadataReader _reader;
        private readonly ConstantStringArrayHandle _handle;

        internal ConstantStringArray(MetadataReader reader, ConstantStringArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantStringArrayHandle Handle => _handle;

        /// One of: ConstantStringValue, ConstantReferenceValue
        public HandleCollection Value => _value;
        private readonly HandleCollection _value;
    } // ConstantStringArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantStringArrayHandle
    {
        internal readonly int _value;

        internal ConstantStringArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantStringArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantStringArray || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantStringArray) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantStringArrayHandle)
                return _value == ((ConstantStringArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantStringArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantStringArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantStringArray GetConstantStringArray(MetadataReader reader)
            => new ConstantStringArray(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantStringArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantStringArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantStringValue
    {
        private readonly MetadataReader _reader;
        private readonly ConstantStringValueHandle _handle;

        internal ConstantStringValue(MetadataReader reader, ConstantStringValueHandle handle)
        {
            if (handle.IsNil)
                return;
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantStringValueHandle Handle => _handle;

        public string Value => _value;
        private readonly string _value;
    } // ConstantStringValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantStringValueHandle
    {
        internal readonly int _value;

        internal ConstantStringValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantStringValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantStringValue || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantStringValue) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantStringValueHandle)
                return _value == ((ConstantStringValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantStringValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantStringValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantStringValue GetConstantStringValue(MetadataReader reader)
            => new ConstantStringValue(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantStringValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantStringValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt16Array
    {
        private readonly MetadataReader _reader;
        private readonly ConstantUInt16ArrayHandle _handle;

        internal ConstantUInt16Array(MetadataReader reader, ConstantUInt16ArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantUInt16ArrayHandle Handle => _handle;

        public UInt16Collection Value => _value;
        private readonly UInt16Collection _value;
    } // ConstantUInt16Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt16ArrayHandle
    {
        internal readonly int _value;

        internal ConstantUInt16ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt16ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantUInt16Array || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt16Array) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt16ArrayHandle)
                return _value == ((ConstantUInt16ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt16ArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantUInt16ArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantUInt16Array GetConstantUInt16Array(MetadataReader reader)
            => new ConstantUInt16Array(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt16Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantUInt16ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt16Value
    {
        private readonly MetadataReader _reader;
        private readonly ConstantUInt16ValueHandle _handle;

        internal ConstantUInt16Value(MetadataReader reader, ConstantUInt16ValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantUInt16ValueHandle Handle => _handle;

        public ushort Value => _value;
        private readonly ushort _value;
    } // ConstantUInt16Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt16ValueHandle
    {
        internal readonly int _value;

        internal ConstantUInt16ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt16ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantUInt16Value || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt16Value) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt16ValueHandle)
                return _value == ((ConstantUInt16ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt16ValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantUInt16ValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantUInt16Value GetConstantUInt16Value(MetadataReader reader)
            => new ConstantUInt16Value(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt16Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantUInt16ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt32Array
    {
        private readonly MetadataReader _reader;
        private readonly ConstantUInt32ArrayHandle _handle;

        internal ConstantUInt32Array(MetadataReader reader, ConstantUInt32ArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantUInt32ArrayHandle Handle => _handle;

        public UInt32Collection Value => _value;
        private readonly UInt32Collection _value;
    } // ConstantUInt32Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt32ArrayHandle
    {
        internal readonly int _value;

        internal ConstantUInt32ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt32ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantUInt32Array || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt32Array) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt32ArrayHandle)
                return _value == ((ConstantUInt32ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt32ArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantUInt32ArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantUInt32Array GetConstantUInt32Array(MetadataReader reader)
            => new ConstantUInt32Array(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt32Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantUInt32ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt32Value
    {
        private readonly MetadataReader _reader;
        private readonly ConstantUInt32ValueHandle _handle;

        internal ConstantUInt32Value(MetadataReader reader, ConstantUInt32ValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantUInt32ValueHandle Handle => _handle;

        public uint Value => _value;
        private readonly uint _value;
    } // ConstantUInt32Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt32ValueHandle
    {
        internal readonly int _value;

        internal ConstantUInt32ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt32ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantUInt32Value || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt32Value) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt32ValueHandle)
                return _value == ((ConstantUInt32ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt32ValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantUInt32ValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantUInt32Value GetConstantUInt32Value(MetadataReader reader)
            => new ConstantUInt32Value(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt32Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantUInt32ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt64Array
    {
        private readonly MetadataReader _reader;
        private readonly ConstantUInt64ArrayHandle _handle;

        internal ConstantUInt64Array(MetadataReader reader, ConstantUInt64ArrayHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantUInt64ArrayHandle Handle => _handle;

        public UInt64Collection Value => _value;
        private readonly UInt64Collection _value;
    } // ConstantUInt64Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt64ArrayHandle
    {
        internal readonly int _value;

        internal ConstantUInt64ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt64ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantUInt64Array || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt64Array) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt64ArrayHandle)
                return _value == ((ConstantUInt64ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt64ArrayHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantUInt64ArrayHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantUInt64Array GetConstantUInt64Array(MetadataReader reader)
            => new ConstantUInt64Array(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt64Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantUInt64ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt64Value
    {
        private readonly MetadataReader _reader;
        private readonly ConstantUInt64ValueHandle _handle;

        internal ConstantUInt64Value(MetadataReader reader, ConstantUInt64ValueHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _value);
        }

        public ConstantUInt64ValueHandle Handle => _handle;

        public ulong Value => _value;
        private readonly ulong _value;
    } // ConstantUInt64Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantUInt64ValueHandle
    {
        internal readonly int _value;

        internal ConstantUInt64ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt64ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ConstantUInt64Value || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt64Value) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt64ValueHandle)
                return _value == ((ConstantUInt64ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt64ValueHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantUInt64ValueHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ConstantUInt64Value GetConstantUInt64Value(MetadataReader reader)
            => new ConstantUInt64Value(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt64Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ConstantUInt64ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct CustomAttribute
    {
        private readonly MetadataReader _reader;
        private readonly CustomAttributeHandle _handle;

        internal CustomAttribute(MetadataReader reader, CustomAttributeHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _constructor);
            offset = streamReader.Read(offset, out _fixedArguments);
            offset = streamReader.Read(offset, out _namedArguments);
        }

        public CustomAttributeHandle Handle => _handle;

        /// One of: QualifiedMethod, MemberReference
        public Handle Constructor => _constructor;
        private readonly Handle _constructor;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantEnumValue, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value
        public HandleCollection FixedArguments => _fixedArguments;
        private readonly HandleCollection _fixedArguments;

        public NamedArgumentHandleCollection NamedArguments => _namedArguments;
        private readonly NamedArgumentHandleCollection _namedArguments;
    } // CustomAttribute

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct CustomAttributeHandle
    {
        internal readonly int _value;

        internal CustomAttributeHandle(Handle handle) : this(handle._value)
        {
        }

        internal CustomAttributeHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.CustomAttribute || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.CustomAttribute) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is CustomAttributeHandle)
                return _value == ((CustomAttributeHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(CustomAttributeHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(CustomAttributeHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public CustomAttribute GetCustomAttribute(MetadataReader reader)
            => new CustomAttribute(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.CustomAttribute)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // CustomAttributeHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Event
    {
        private readonly MetadataReader _reader;
        private readonly EventHandle _handle;

        internal Event(MetadataReader reader, EventHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _type);
            offset = streamReader.Read(offset, out _methodSemantics);
            offset = streamReader.Read(offset, out _customAttributes);
        }

        public EventHandle Handle => _handle;

        public EventAttributes Flags => _flags;
        private readonly EventAttributes _flags;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        /// One of: TypeDefinition, TypeReference, TypeSpecification
        public Handle Type => _type;
        private readonly Handle _type;

        public MethodSemanticsHandleCollection MethodSemantics => _methodSemantics;
        private readonly MethodSemanticsHandleCollection _methodSemantics;

        public CustomAttributeHandleCollection CustomAttributes => _customAttributes;
        private readonly CustomAttributeHandleCollection _customAttributes;
    } // Event

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct EventHandle
    {
        internal readonly int _value;

        internal EventHandle(Handle handle) : this(handle._value)
        {
        }

        internal EventHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.Event || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Event) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is EventHandle)
                return _value == ((EventHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(EventHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(EventHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public Event GetEvent(MetadataReader reader)
            => new Event(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Event)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // EventHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Field
    {
        private readonly MetadataReader _reader;
        private readonly FieldHandle _handle;

        internal Field(MetadataReader reader, FieldHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _signature);
            offset = streamReader.Read(offset, out _defaultValue);
            offset = streamReader.Read(offset, out _offset);
            offset = streamReader.Read(offset, out _customAttributes);
        }

        public FieldHandle Handle => _handle;

        public FieldAttributes Flags => _flags;
        private readonly FieldAttributes _flags;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public FieldSignatureHandle Signature => _signature;
        private readonly FieldSignatureHandle _signature;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantEnumValue, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value
        public Handle DefaultValue => _defaultValue;
        private readonly Handle _defaultValue;

        public uint Offset => _offset;
        private readonly uint _offset;

        public CustomAttributeHandleCollection CustomAttributes => _customAttributes;
        private readonly CustomAttributeHandleCollection _customAttributes;
    } // Field

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct FieldHandle
    {
        internal readonly int _value;

        internal FieldHandle(Handle handle) : this(handle._value)
        {
        }

        internal FieldHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.Field || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Field) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is FieldHandle)
                return _value == ((FieldHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(FieldHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(FieldHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public Field GetField(MetadataReader reader)
            => new Field(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Field)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // FieldHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct FieldSignature
    {
        private readonly MetadataReader _reader;
        private readonly FieldSignatureHandle _handle;

        internal FieldSignature(MetadataReader reader, FieldSignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _type);
        }

        public FieldSignatureHandle Handle => _handle;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public Handle Type => _type;
        private readonly Handle _type;
    } // FieldSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct FieldSignatureHandle
    {
        internal readonly int _value;

        internal FieldSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal FieldSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.FieldSignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.FieldSignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is FieldSignatureHandle)
                return _value == ((FieldSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(FieldSignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(FieldSignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public FieldSignature GetFieldSignature(MetadataReader reader)
            => new FieldSignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.FieldSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // FieldSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct FunctionPointerSignature
    {
        private readonly MetadataReader _reader;
        private readonly FunctionPointerSignatureHandle _handle;

        internal FunctionPointerSignature(MetadataReader reader, FunctionPointerSignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _signature);
        }

        public FunctionPointerSignatureHandle Handle => _handle;

        public MethodSignatureHandle Signature => _signature;
        private readonly MethodSignatureHandle _signature;
    } // FunctionPointerSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct FunctionPointerSignatureHandle
    {
        internal readonly int _value;

        internal FunctionPointerSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal FunctionPointerSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.FunctionPointerSignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.FunctionPointerSignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is FunctionPointerSignatureHandle)
                return _value == ((FunctionPointerSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(FunctionPointerSignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(FunctionPointerSignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public FunctionPointerSignature GetFunctionPointerSignature(MetadataReader reader)
            => new FunctionPointerSignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.FunctionPointerSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // FunctionPointerSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct GenericParameter
    {
        private readonly MetadataReader _reader;
        private readonly GenericParameterHandle _handle;

        internal GenericParameter(MetadataReader reader, GenericParameterHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _number);
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _kind);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _constraints);
            offset = streamReader.Read(offset, out _customAttributes);
        }

        public GenericParameterHandle Handle => _handle;

        public ushort Number => _number;
        private readonly ushort _number;

        public GenericParameterAttributes Flags => _flags;
        private readonly GenericParameterAttributes _flags;

        public GenericParameterKind Kind => _kind;
        private readonly GenericParameterKind _kind;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public HandleCollection Constraints => _constraints;
        private readonly HandleCollection _constraints;

        public CustomAttributeHandleCollection CustomAttributes => _customAttributes;
        private readonly CustomAttributeHandleCollection _customAttributes;
    } // GenericParameter

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct GenericParameterHandle
    {
        internal readonly int _value;

        internal GenericParameterHandle(Handle handle) : this(handle._value)
        {
        }

        internal GenericParameterHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.GenericParameter || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.GenericParameter) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is GenericParameterHandle)
                return _value == ((GenericParameterHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(GenericParameterHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(GenericParameterHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public GenericParameter GetGenericParameter(MetadataReader reader)
            => new GenericParameter(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.GenericParameter)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // GenericParameterHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MemberReference
    {
        private readonly MetadataReader _reader;
        private readonly MemberReferenceHandle _handle;

        internal MemberReference(MetadataReader reader, MemberReferenceHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _parent);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _signature);
        }

        public MemberReferenceHandle Handle => _handle;

        /// One of: TypeDefinition, TypeReference, TypeSpecification
        public Handle Parent => _parent;
        private readonly Handle _parent;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        /// One of: MethodSignature, FieldSignature
        public Handle Signature => _signature;
        private readonly Handle _signature;
    } // MemberReference

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MemberReferenceHandle
    {
        internal readonly int _value;

        internal MemberReferenceHandle(Handle handle) : this(handle._value)
        {
        }

        internal MemberReferenceHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.MemberReference || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MemberReference) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is MemberReferenceHandle)
                return _value == ((MemberReferenceHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MemberReferenceHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(MemberReferenceHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public MemberReference GetMemberReference(MetadataReader reader)
            => new MemberReference(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MemberReference)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // MemberReferenceHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Method
    {
        private readonly MetadataReader _reader;
        private readonly MethodHandle _handle;

        internal Method(MetadataReader reader, MethodHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _implFlags);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _signature);
            offset = streamReader.Read(offset, out _parameters);
            offset = streamReader.Read(offset, out _genericParameters);
            offset = streamReader.Read(offset, out _customAttributes);
        }

        public MethodHandle Handle => _handle;

        public MethodAttributes Flags => _flags;
        private readonly MethodAttributes _flags;

        public MethodImplAttributes ImplFlags => _implFlags;
        private readonly MethodImplAttributes _implFlags;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public MethodSignatureHandle Signature => _signature;
        private readonly MethodSignatureHandle _signature;

        public ParameterHandleCollection Parameters => _parameters;
        private readonly ParameterHandleCollection _parameters;

        public GenericParameterHandleCollection GenericParameters => _genericParameters;
        private readonly GenericParameterHandleCollection _genericParameters;

        public CustomAttributeHandleCollection CustomAttributes => _customAttributes;
        private readonly CustomAttributeHandleCollection _customAttributes;
    } // Method

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodHandle
    {
        internal readonly int _value;

        internal MethodHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.Method || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Method) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodHandle)
                return _value == ((MethodHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(MethodHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public Method GetMethod(MetadataReader reader)
            => new Method(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Method)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // MethodHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodInstantiation
    {
        private readonly MetadataReader _reader;
        private readonly MethodInstantiationHandle _handle;

        internal MethodInstantiation(MetadataReader reader, MethodInstantiationHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _method);
            offset = streamReader.Read(offset, out _genericTypeArguments);
        }

        public MethodInstantiationHandle Handle => _handle;

        /// One of: QualifiedMethod, MemberReference
        public Handle Method => _method;
        private readonly Handle _method;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public HandleCollection GenericTypeArguments => _genericTypeArguments;
        private readonly HandleCollection _genericTypeArguments;
    } // MethodInstantiation

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodInstantiationHandle
    {
        internal readonly int _value;

        internal MethodInstantiationHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodInstantiationHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.MethodInstantiation || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MethodInstantiation) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodInstantiationHandle)
                return _value == ((MethodInstantiationHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodInstantiationHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(MethodInstantiationHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public MethodInstantiation GetMethodInstantiation(MetadataReader reader)
            => new MethodInstantiation(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MethodInstantiation)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // MethodInstantiationHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodSemantics
    {
        private readonly MetadataReader _reader;
        private readonly MethodSemanticsHandle _handle;

        internal MethodSemantics(MetadataReader reader, MethodSemanticsHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _attributes);
            offset = streamReader.Read(offset, out _method);
        }

        public MethodSemanticsHandle Handle => _handle;

        public MethodSemanticsAttributes Attributes => _attributes;
        private readonly MethodSemanticsAttributes _attributes;

        public MethodHandle Method => _method;
        private readonly MethodHandle _method;
    } // MethodSemantics

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodSemanticsHandle
    {
        internal readonly int _value;

        internal MethodSemanticsHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodSemanticsHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.MethodSemantics || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MethodSemantics) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodSemanticsHandle)
                return _value == ((MethodSemanticsHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodSemanticsHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(MethodSemanticsHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public MethodSemantics GetMethodSemantics(MetadataReader reader)
            => new MethodSemantics(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MethodSemantics)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // MethodSemanticsHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodSignature
    {
        private readonly MetadataReader _reader;
        private readonly MethodSignatureHandle _handle;

        internal MethodSignature(MetadataReader reader, MethodSignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _callingConvention);
            offset = streamReader.Read(offset, out _genericParameterCount);
            offset = streamReader.Read(offset, out _returnType);
            offset = streamReader.Read(offset, out _parameters);
            offset = streamReader.Read(offset, out _varArgParameters);
        }

        public MethodSignatureHandle Handle => _handle;

        public SignatureCallingConvention CallingConvention => _callingConvention;
        private readonly SignatureCallingConvention _callingConvention;

        public int GenericParameterCount => _genericParameterCount;
        private readonly int _genericParameterCount;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public Handle ReturnType => _returnType;
        private readonly Handle _returnType;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public HandleCollection Parameters => _parameters;
        private readonly HandleCollection _parameters;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public HandleCollection VarArgParameters => _varArgParameters;
        private readonly HandleCollection _varArgParameters;
    } // MethodSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodSignatureHandle
    {
        internal readonly int _value;

        internal MethodSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.MethodSignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MethodSignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodSignatureHandle)
                return _value == ((MethodSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodSignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(MethodSignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public MethodSignature GetMethodSignature(MetadataReader reader)
            => new MethodSignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MethodSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // MethodSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodTypeVariableSignature
    {
        private readonly MetadataReader _reader;
        private readonly MethodTypeVariableSignatureHandle _handle;

        internal MethodTypeVariableSignature(MetadataReader reader, MethodTypeVariableSignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _number);
        }

        public MethodTypeVariableSignatureHandle Handle => _handle;

        public int Number => _number;
        private readonly int _number;
    } // MethodTypeVariableSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodTypeVariableSignatureHandle
    {
        internal readonly int _value;

        internal MethodTypeVariableSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodTypeVariableSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.MethodTypeVariableSignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MethodTypeVariableSignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodTypeVariableSignatureHandle)
                return _value == ((MethodTypeVariableSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodTypeVariableSignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(MethodTypeVariableSignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public MethodTypeVariableSignature GetMethodTypeVariableSignature(MetadataReader reader)
            => new MethodTypeVariableSignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MethodTypeVariableSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // MethodTypeVariableSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ModifiedType
    {
        private readonly MetadataReader _reader;
        private readonly ModifiedTypeHandle _handle;

        internal ModifiedType(MetadataReader reader, ModifiedTypeHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _isOptional);
            offset = streamReader.Read(offset, out _modifierType);
            offset = streamReader.Read(offset, out _type);
        }

        public ModifiedTypeHandle Handle => _handle;

        public bool IsOptional => _isOptional;
        private readonly bool _isOptional;

        /// One of: TypeDefinition, TypeReference, TypeSpecification
        public Handle ModifierType => _modifierType;
        private readonly Handle _modifierType;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public Handle Type => _type;
        private readonly Handle _type;
    } // ModifiedType

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ModifiedTypeHandle
    {
        internal readonly int _value;

        internal ModifiedTypeHandle(Handle handle) : this(handle._value)
        {
        }

        internal ModifiedTypeHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ModifiedType || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ModifiedType) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ModifiedTypeHandle)
                return _value == ((ModifiedTypeHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ModifiedTypeHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ModifiedTypeHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ModifiedType GetModifiedType(MetadataReader reader)
            => new ModifiedType(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ModifiedType)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ModifiedTypeHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct NamedArgument
    {
        private readonly MetadataReader _reader;
        private readonly NamedArgumentHandle _handle;

        internal NamedArgument(MetadataReader reader, NamedArgumentHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _type);
            offset = streamReader.Read(offset, out _value);
        }

        public NamedArgumentHandle Handle => _handle;

        public NamedArgumentMemberKind Flags => _flags;
        private readonly NamedArgumentMemberKind _flags;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        /// One of: TypeDefinition, TypeReference, TypeSpecification
        public Handle Type => _type;
        private readonly Handle _type;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantEnumValue, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value
        public Handle Value => _value;
        private readonly Handle _value;
    } // NamedArgument

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct NamedArgumentHandle
    {
        internal readonly int _value;

        internal NamedArgumentHandle(Handle handle) : this(handle._value)
        {
        }

        internal NamedArgumentHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.NamedArgument || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.NamedArgument) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is NamedArgumentHandle)
                return _value == ((NamedArgumentHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(NamedArgumentHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(NamedArgumentHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public NamedArgument GetNamedArgument(MetadataReader reader)
            => new NamedArgument(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.NamedArgument)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // NamedArgumentHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct NamespaceDefinition
    {
        private readonly MetadataReader _reader;
        private readonly NamespaceDefinitionHandle _handle;

        internal NamespaceDefinition(MetadataReader reader, NamespaceDefinitionHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _parentScopeOrNamespace);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _typeDefinitions);
            offset = streamReader.Read(offset, out _typeForwarders);
            offset = streamReader.Read(offset, out _namespaceDefinitions);
        }

        public NamespaceDefinitionHandle Handle => _handle;

        /// One of: NamespaceDefinition, ScopeDefinition
        public Handle ParentScopeOrNamespace => _parentScopeOrNamespace;
        private readonly Handle _parentScopeOrNamespace;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public TypeDefinitionHandleCollection TypeDefinitions => _typeDefinitions;
        private readonly TypeDefinitionHandleCollection _typeDefinitions;

        public TypeForwarderHandleCollection TypeForwarders => _typeForwarders;
        private readonly TypeForwarderHandleCollection _typeForwarders;

        public NamespaceDefinitionHandleCollection NamespaceDefinitions => _namespaceDefinitions;
        private readonly NamespaceDefinitionHandleCollection _namespaceDefinitions;
    } // NamespaceDefinition

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct NamespaceDefinitionHandle
    {
        internal readonly int _value;

        internal NamespaceDefinitionHandle(Handle handle) : this(handle._value)
        {
        }

        internal NamespaceDefinitionHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.NamespaceDefinition || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.NamespaceDefinition) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is NamespaceDefinitionHandle)
                return _value == ((NamespaceDefinitionHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(NamespaceDefinitionHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(NamespaceDefinitionHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public NamespaceDefinition GetNamespaceDefinition(MetadataReader reader)
            => new NamespaceDefinition(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.NamespaceDefinition)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // NamespaceDefinitionHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct NamespaceReference
    {
        private readonly MetadataReader _reader;
        private readonly NamespaceReferenceHandle _handle;

        internal NamespaceReference(MetadataReader reader, NamespaceReferenceHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _parentScopeOrNamespace);
            offset = streamReader.Read(offset, out _name);
        }

        public NamespaceReferenceHandle Handle => _handle;

        /// One of: NamespaceReference, ScopeReference
        public Handle ParentScopeOrNamespace => _parentScopeOrNamespace;
        private readonly Handle _parentScopeOrNamespace;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;
    } // NamespaceReference

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct NamespaceReferenceHandle
    {
        internal readonly int _value;

        internal NamespaceReferenceHandle(Handle handle) : this(handle._value)
        {
        }

        internal NamespaceReferenceHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.NamespaceReference || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.NamespaceReference) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is NamespaceReferenceHandle)
                return _value == ((NamespaceReferenceHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(NamespaceReferenceHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(NamespaceReferenceHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public NamespaceReference GetNamespaceReference(MetadataReader reader)
            => new NamespaceReference(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.NamespaceReference)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // NamespaceReferenceHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Parameter
    {
        private readonly MetadataReader _reader;
        private readonly ParameterHandle _handle;

        internal Parameter(MetadataReader reader, ParameterHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _sequence);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _defaultValue);
            offset = streamReader.Read(offset, out _customAttributes);
        }

        public ParameterHandle Handle => _handle;

        public ParameterAttributes Flags => _flags;
        private readonly ParameterAttributes _flags;

        public ushort Sequence => _sequence;
        private readonly ushort _sequence;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantEnumValue, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value
        public Handle DefaultValue => _defaultValue;
        private readonly Handle _defaultValue;

        public CustomAttributeHandleCollection CustomAttributes => _customAttributes;
        private readonly CustomAttributeHandleCollection _customAttributes;
    } // Parameter

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ParameterHandle
    {
        internal readonly int _value;

        internal ParameterHandle(Handle handle) : this(handle._value)
        {
        }

        internal ParameterHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.Parameter || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Parameter) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ParameterHandle)
                return _value == ((ParameterHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ParameterHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ParameterHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public Parameter GetParameter(MetadataReader reader)
            => new Parameter(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Parameter)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ParameterHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct PointerSignature
    {
        private readonly MetadataReader _reader;
        private readonly PointerSignatureHandle _handle;

        internal PointerSignature(MetadataReader reader, PointerSignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _type);
        }

        public PointerSignatureHandle Handle => _handle;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public Handle Type => _type;
        private readonly Handle _type;
    } // PointerSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct PointerSignatureHandle
    {
        internal readonly int _value;

        internal PointerSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal PointerSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.PointerSignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.PointerSignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is PointerSignatureHandle)
                return _value == ((PointerSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(PointerSignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(PointerSignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public PointerSignature GetPointerSignature(MetadataReader reader)
            => new PointerSignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.PointerSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // PointerSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Property
    {
        private readonly MetadataReader _reader;
        private readonly PropertyHandle _handle;

        internal Property(MetadataReader reader, PropertyHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _signature);
            offset = streamReader.Read(offset, out _methodSemantics);
            offset = streamReader.Read(offset, out _defaultValue);
            offset = streamReader.Read(offset, out _customAttributes);
        }

        public PropertyHandle Handle => _handle;

        public PropertyAttributes Flags => _flags;
        private readonly PropertyAttributes _flags;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public PropertySignatureHandle Signature => _signature;
        private readonly PropertySignatureHandle _signature;

        public MethodSemanticsHandleCollection MethodSemantics => _methodSemantics;
        private readonly MethodSemanticsHandleCollection _methodSemantics;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantEnumValue, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value
        public Handle DefaultValue => _defaultValue;
        private readonly Handle _defaultValue;

        public CustomAttributeHandleCollection CustomAttributes => _customAttributes;
        private readonly CustomAttributeHandleCollection _customAttributes;
    } // Property

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct PropertyHandle
    {
        internal readonly int _value;

        internal PropertyHandle(Handle handle) : this(handle._value)
        {
        }

        internal PropertyHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.Property || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Property) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is PropertyHandle)
                return _value == ((PropertyHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(PropertyHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(PropertyHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public Property GetProperty(MetadataReader reader)
            => new Property(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Property)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // PropertyHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct PropertySignature
    {
        private readonly MetadataReader _reader;
        private readonly PropertySignatureHandle _handle;

        internal PropertySignature(MetadataReader reader, PropertySignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _callingConvention);
            offset = streamReader.Read(offset, out _type);
            offset = streamReader.Read(offset, out _parameters);
        }

        public PropertySignatureHandle Handle => _handle;

        public CallingConventions CallingConvention => _callingConvention;
        private readonly CallingConventions _callingConvention;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public Handle Type => _type;
        private readonly Handle _type;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public HandleCollection Parameters => _parameters;
        private readonly HandleCollection _parameters;
    } // PropertySignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct PropertySignatureHandle
    {
        internal readonly int _value;

        internal PropertySignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal PropertySignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.PropertySignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.PropertySignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is PropertySignatureHandle)
                return _value == ((PropertySignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(PropertySignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(PropertySignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public PropertySignature GetPropertySignature(MetadataReader reader)
            => new PropertySignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.PropertySignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // PropertySignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct QualifiedField
    {
        private readonly MetadataReader _reader;
        private readonly QualifiedFieldHandle _handle;

        internal QualifiedField(MetadataReader reader, QualifiedFieldHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _field);
            offset = streamReader.Read(offset, out _enclosingType);
        }

        public QualifiedFieldHandle Handle => _handle;

        public FieldHandle Field => _field;
        private readonly FieldHandle _field;

        public TypeDefinitionHandle EnclosingType => _enclosingType;
        private readonly TypeDefinitionHandle _enclosingType;
    } // QualifiedField

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct QualifiedFieldHandle
    {
        internal readonly int _value;

        internal QualifiedFieldHandle(Handle handle) : this(handle._value)
        {
        }

        internal QualifiedFieldHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.QualifiedField || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.QualifiedField) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is QualifiedFieldHandle)
                return _value == ((QualifiedFieldHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(QualifiedFieldHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(QualifiedFieldHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public QualifiedField GetQualifiedField(MetadataReader reader)
            => new QualifiedField(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.QualifiedField)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // QualifiedFieldHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct QualifiedMethod
    {
        private readonly MetadataReader _reader;
        private readonly QualifiedMethodHandle _handle;

        internal QualifiedMethod(MetadataReader reader, QualifiedMethodHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _method);
            offset = streamReader.Read(offset, out _enclosingType);
        }

        public QualifiedMethodHandle Handle => _handle;

        public MethodHandle Method => _method;
        private readonly MethodHandle _method;

        public TypeDefinitionHandle EnclosingType => _enclosingType;
        private readonly TypeDefinitionHandle _enclosingType;
    } // QualifiedMethod

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct QualifiedMethodHandle
    {
        internal readonly int _value;

        internal QualifiedMethodHandle(Handle handle) : this(handle._value)
        {
        }

        internal QualifiedMethodHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.QualifiedMethod || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.QualifiedMethod) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is QualifiedMethodHandle)
                return _value == ((QualifiedMethodHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(QualifiedMethodHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(QualifiedMethodHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public QualifiedMethod GetQualifiedMethod(MetadataReader reader)
            => new QualifiedMethod(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.QualifiedMethod)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // QualifiedMethodHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct SZArraySignature
    {
        private readonly MetadataReader _reader;
        private readonly SZArraySignatureHandle _handle;

        internal SZArraySignature(MetadataReader reader, SZArraySignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _elementType);
        }

        public SZArraySignatureHandle Handle => _handle;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public Handle ElementType => _elementType;
        private readonly Handle _elementType;
    } // SZArraySignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct SZArraySignatureHandle
    {
        internal readonly int _value;

        internal SZArraySignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal SZArraySignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.SZArraySignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.SZArraySignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is SZArraySignatureHandle)
                return _value == ((SZArraySignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(SZArraySignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(SZArraySignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public SZArraySignature GetSZArraySignature(MetadataReader reader)
            => new SZArraySignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.SZArraySignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // SZArraySignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ScopeDefinition
    {
        private readonly MetadataReader _reader;
        private readonly ScopeDefinitionHandle _handle;

        internal ScopeDefinition(MetadataReader reader, ScopeDefinitionHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _hashAlgorithm);
            offset = streamReader.Read(offset, out _majorVersion);
            offset = streamReader.Read(offset, out _minorVersion);
            offset = streamReader.Read(offset, out _buildNumber);
            offset = streamReader.Read(offset, out _revisionNumber);
            offset = streamReader.Read(offset, out _publicKey);
            offset = streamReader.Read(offset, out _culture);
            offset = streamReader.Read(offset, out _rootNamespaceDefinition);
            offset = streamReader.Read(offset, out _entryPoint);
            offset = streamReader.Read(offset, out _globalModuleType);
            offset = streamReader.Read(offset, out _customAttributes);
            offset = streamReader.Read(offset, out _moduleName);
            offset = streamReader.Read(offset, out _mvid);
            offset = streamReader.Read(offset, out _moduleCustomAttributes);
        }

        public ScopeDefinitionHandle Handle => _handle;

        public AssemblyFlags Flags => _flags;
        private readonly AssemblyFlags _flags;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public AssemblyHashAlgorithm HashAlgorithm => _hashAlgorithm;
        private readonly AssemblyHashAlgorithm _hashAlgorithm;

        public ushort MajorVersion => _majorVersion;
        private readonly ushort _majorVersion;

        public ushort MinorVersion => _minorVersion;
        private readonly ushort _minorVersion;

        public ushort BuildNumber => _buildNumber;
        private readonly ushort _buildNumber;

        public ushort RevisionNumber => _revisionNumber;
        private readonly ushort _revisionNumber;

        public ByteCollection PublicKey => _publicKey;
        private readonly ByteCollection _publicKey;

        public ConstantStringValueHandle Culture => _culture;
        private readonly ConstantStringValueHandle _culture;

        public NamespaceDefinitionHandle RootNamespaceDefinition => _rootNamespaceDefinition;
        private readonly NamespaceDefinitionHandle _rootNamespaceDefinition;

        public QualifiedMethodHandle EntryPoint => _entryPoint;
        private readonly QualifiedMethodHandle _entryPoint;

        public TypeDefinitionHandle GlobalModuleType => _globalModuleType;
        private readonly TypeDefinitionHandle _globalModuleType;

        public CustomAttributeHandleCollection CustomAttributes => _customAttributes;
        private readonly CustomAttributeHandleCollection _customAttributes;

        public ConstantStringValueHandle ModuleName => _moduleName;
        private readonly ConstantStringValueHandle _moduleName;

        public ByteCollection Mvid => _mvid;
        private readonly ByteCollection _mvid;

        public CustomAttributeHandleCollection ModuleCustomAttributes => _moduleCustomAttributes;
        private readonly CustomAttributeHandleCollection _moduleCustomAttributes;
    } // ScopeDefinition

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ScopeDefinitionHandle
    {
        internal readonly int _value;

        internal ScopeDefinitionHandle(Handle handle) : this(handle._value)
        {
        }

        internal ScopeDefinitionHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ScopeDefinition || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ScopeDefinition) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ScopeDefinitionHandle)
                return _value == ((ScopeDefinitionHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ScopeDefinitionHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ScopeDefinitionHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ScopeDefinition GetScopeDefinition(MetadataReader reader)
            => new ScopeDefinition(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ScopeDefinition)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ScopeDefinitionHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ScopeReference
    {
        private readonly MetadataReader _reader;
        private readonly ScopeReferenceHandle _handle;

        internal ScopeReference(MetadataReader reader, ScopeReferenceHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _majorVersion);
            offset = streamReader.Read(offset, out _minorVersion);
            offset = streamReader.Read(offset, out _buildNumber);
            offset = streamReader.Read(offset, out _revisionNumber);
            offset = streamReader.Read(offset, out _publicKeyOrToken);
            offset = streamReader.Read(offset, out _culture);
        }

        public ScopeReferenceHandle Handle => _handle;

        public AssemblyFlags Flags => _flags;
        private readonly AssemblyFlags _flags;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public ushort MajorVersion => _majorVersion;
        private readonly ushort _majorVersion;

        public ushort MinorVersion => _minorVersion;
        private readonly ushort _minorVersion;

        public ushort BuildNumber => _buildNumber;
        private readonly ushort _buildNumber;

        public ushort RevisionNumber => _revisionNumber;
        private readonly ushort _revisionNumber;

        public ByteCollection PublicKeyOrToken => _publicKeyOrToken;
        private readonly ByteCollection _publicKeyOrToken;

        public ConstantStringValueHandle Culture => _culture;
        private readonly ConstantStringValueHandle _culture;
    } // ScopeReference

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ScopeReferenceHandle
    {
        internal readonly int _value;

        internal ScopeReferenceHandle(Handle handle) : this(handle._value)
        {
        }

        internal ScopeReferenceHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.ScopeReference || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ScopeReference) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ScopeReferenceHandle)
                return _value == ((ScopeReferenceHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ScopeReferenceHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ScopeReferenceHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public ScopeReference GetScopeReference(MetadataReader reader)
            => new ScopeReference(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ScopeReference)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // ScopeReferenceHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeDefinition
    {
        private readonly MetadataReader _reader;
        private readonly TypeDefinitionHandle _handle;

        internal TypeDefinition(MetadataReader reader, TypeDefinitionHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _flags);
            offset = streamReader.Read(offset, out _baseType);
            offset = streamReader.Read(offset, out _namespaceDefinition);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _size);
            offset = streamReader.Read(offset, out _packingSize);
            offset = streamReader.Read(offset, out _enclosingType);
            offset = streamReader.Read(offset, out _nestedTypes);
            offset = streamReader.Read(offset, out _methods);
            offset = streamReader.Read(offset, out _fields);
            offset = streamReader.Read(offset, out _properties);
            offset = streamReader.Read(offset, out _events);
            offset = streamReader.Read(offset, out _genericParameters);
            offset = streamReader.Read(offset, out _interfaces);
            offset = streamReader.Read(offset, out _customAttributes);
        }

        public TypeDefinitionHandle Handle => _handle;

        public TypeAttributes Flags => _flags;
        private readonly TypeAttributes _flags;

        /// One of: TypeDefinition, TypeReference, TypeSpecification
        public Handle BaseType => _baseType;
        private readonly Handle _baseType;

        public NamespaceDefinitionHandle NamespaceDefinition => _namespaceDefinition;
        private readonly NamespaceDefinitionHandle _namespaceDefinition;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public uint Size => _size;
        private readonly uint _size;

        public ushort PackingSize => _packingSize;
        private readonly ushort _packingSize;

        public TypeDefinitionHandle EnclosingType => _enclosingType;
        private readonly TypeDefinitionHandle _enclosingType;

        public TypeDefinitionHandleCollection NestedTypes => _nestedTypes;
        private readonly TypeDefinitionHandleCollection _nestedTypes;

        public MethodHandleCollection Methods => _methods;
        private readonly MethodHandleCollection _methods;

        public FieldHandleCollection Fields => _fields;
        private readonly FieldHandleCollection _fields;

        public PropertyHandleCollection Properties => _properties;
        private readonly PropertyHandleCollection _properties;

        public EventHandleCollection Events => _events;
        private readonly EventHandleCollection _events;

        public GenericParameterHandleCollection GenericParameters => _genericParameters;
        private readonly GenericParameterHandleCollection _genericParameters;

        /// One of: TypeDefinition, TypeReference, TypeSpecification
        public HandleCollection Interfaces => _interfaces;
        private readonly HandleCollection _interfaces;

        public CustomAttributeHandleCollection CustomAttributes => _customAttributes;
        private readonly CustomAttributeHandleCollection _customAttributes;
    } // TypeDefinition

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeDefinitionHandle
    {
        internal readonly int _value;

        internal TypeDefinitionHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeDefinitionHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.TypeDefinition || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeDefinition) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeDefinitionHandle)
                return _value == ((TypeDefinitionHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeDefinitionHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(TypeDefinitionHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public TypeDefinition GetTypeDefinition(MetadataReader reader)
            => new TypeDefinition(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeDefinition)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // TypeDefinitionHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeForwarder
    {
        private readonly MetadataReader _reader;
        private readonly TypeForwarderHandle _handle;

        internal TypeForwarder(MetadataReader reader, TypeForwarderHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _scope);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _nestedTypes);
        }

        public TypeForwarderHandle Handle => _handle;

        public ScopeReferenceHandle Scope => _scope;
        private readonly ScopeReferenceHandle _scope;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public TypeForwarderHandleCollection NestedTypes => _nestedTypes;
        private readonly TypeForwarderHandleCollection _nestedTypes;
    } // TypeForwarder

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeForwarderHandle
    {
        internal readonly int _value;

        internal TypeForwarderHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeForwarderHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.TypeForwarder || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeForwarder) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeForwarderHandle)
                return _value == ((TypeForwarderHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeForwarderHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(TypeForwarderHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public TypeForwarder GetTypeForwarder(MetadataReader reader)
            => new TypeForwarder(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeForwarder)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // TypeForwarderHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeInstantiationSignature
    {
        private readonly MetadataReader _reader;
        private readonly TypeInstantiationSignatureHandle _handle;

        internal TypeInstantiationSignature(MetadataReader reader, TypeInstantiationSignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _genericType);
            offset = streamReader.Read(offset, out _genericTypeArguments);
        }

        public TypeInstantiationSignatureHandle Handle => _handle;

        /// One of: TypeDefinition, TypeReference, TypeSpecification
        public Handle GenericType => _genericType;
        private readonly Handle _genericType;

        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType
        public HandleCollection GenericTypeArguments => _genericTypeArguments;
        private readonly HandleCollection _genericTypeArguments;
    } // TypeInstantiationSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeInstantiationSignatureHandle
    {
        internal readonly int _value;

        internal TypeInstantiationSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeInstantiationSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.TypeInstantiationSignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeInstantiationSignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeInstantiationSignatureHandle)
                return _value == ((TypeInstantiationSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeInstantiationSignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(TypeInstantiationSignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public TypeInstantiationSignature GetTypeInstantiationSignature(MetadataReader reader)
            => new TypeInstantiationSignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeInstantiationSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // TypeInstantiationSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeReference
    {
        private readonly MetadataReader _reader;
        private readonly TypeReferenceHandle _handle;

        internal TypeReference(MetadataReader reader, TypeReferenceHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _parentNamespaceOrType);
            offset = streamReader.Read(offset, out _typeName);
        }

        public TypeReferenceHandle Handle => _handle;

        /// One of: NamespaceReference, TypeReference
        public Handle ParentNamespaceOrType => _parentNamespaceOrType;
        private readonly Handle _parentNamespaceOrType;

        public ConstantStringValueHandle TypeName => _typeName;
        private readonly ConstantStringValueHandle _typeName;
    } // TypeReference

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeReferenceHandle
    {
        internal readonly int _value;

        internal TypeReferenceHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeReferenceHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.TypeReference || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeReference) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeReferenceHandle)
                return _value == ((TypeReferenceHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeReferenceHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(TypeReferenceHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public TypeReference GetTypeReference(MetadataReader reader)
            => new TypeReference(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeReference)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // TypeReferenceHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeSpecification
    {
        private readonly MetadataReader _reader;
        private readonly TypeSpecificationHandle _handle;

        internal TypeSpecification(MetadataReader reader, TypeSpecificationHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _signature);
        }

        public TypeSpecificationHandle Handle => _handle;

        /// One of: TypeDefinition, TypeReference, TypeInstantiationSignature, SZArraySignature, ArraySignature, PointerSignature, FunctionPointerSignature, ByReferenceSignature, TypeVariableSignature, MethodTypeVariableSignature
        public Handle Signature => _signature;
        private readonly Handle _signature;
    } // TypeSpecification

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeSpecificationHandle
    {
        internal readonly int _value;

        internal TypeSpecificationHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeSpecificationHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.TypeSpecification || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeSpecification) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeSpecificationHandle)
                return _value == ((TypeSpecificationHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeSpecificationHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(TypeSpecificationHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public TypeSpecification GetTypeSpecification(MetadataReader reader)
            => new TypeSpecification(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeSpecification)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // TypeSpecificationHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeVariableSignature
    {
        private readonly MetadataReader _reader;
        private readonly TypeVariableSignatureHandle _handle;

        internal TypeVariableSignature(MetadataReader reader, TypeVariableSignatureHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _number);
        }

        public TypeVariableSignatureHandle Handle => _handle;

        public int Number => _number;
        private readonly int _number;
    } // TypeVariableSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeVariableSignatureHandle
    {
        internal readonly int _value;

        internal TypeVariableSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeVariableSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            Debug.Assert(hType == 0 || hType == HandleType.TypeVariableSignature || hType == HandleType.Null);
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeVariableSignature) << 24);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeVariableSignatureHandle)
                return _value == ((TypeVariableSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeVariableSignatureHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(TypeVariableSignatureHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x00FFFFFF);

        public TypeVariableSignature GetTypeVariableSignature(MetadataReader reader)
            => new TypeVariableSignature(reader, this);

        public bool IsNil => (_value & 0x00FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeVariableSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString() => string.Format("{0:X8}", _value);
    } // TypeVariableSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct NamedArgumentHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal NamedArgumentHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private NamedArgumentHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(NamedArgumentHandle);
            }

            public NamedArgumentHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // NamedArgumentHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodSemanticsHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal MethodSemanticsHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private MethodSemanticsHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(MethodSemanticsHandle);
            }

            public MethodSemanticsHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // MethodSemanticsHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct CustomAttributeHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal CustomAttributeHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private CustomAttributeHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(CustomAttributeHandle);
            }

            public CustomAttributeHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // CustomAttributeHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ParameterHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal ParameterHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private ParameterHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(ParameterHandle);
            }

            public ParameterHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // ParameterHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct GenericParameterHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal GenericParameterHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private GenericParameterHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(GenericParameterHandle);
            }

            public GenericParameterHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // GenericParameterHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeDefinitionHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal TypeDefinitionHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private TypeDefinitionHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(TypeDefinitionHandle);
            }

            public TypeDefinitionHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // TypeDefinitionHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeForwarderHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal TypeForwarderHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private TypeForwarderHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(TypeForwarderHandle);
            }

            public TypeForwarderHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // TypeForwarderHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct NamespaceDefinitionHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal NamespaceDefinitionHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private NamespaceDefinitionHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(NamespaceDefinitionHandle);
            }

            public NamespaceDefinitionHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // NamespaceDefinitionHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct MethodHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal MethodHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private MethodHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(MethodHandle);
            }

            public MethodHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // MethodHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct FieldHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal FieldHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private FieldHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(FieldHandle);
            }

            public FieldHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // FieldHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct PropertyHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal PropertyHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private PropertyHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(PropertyHandle);
            }

            public PropertyHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // PropertyHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct EventHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal EventHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private EventHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(EventHandle);
            }

            public EventHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // EventHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ScopeDefinitionHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal ScopeDefinitionHandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private ScopeDefinitionHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(ScopeDefinitionHandle);
            }

            public ScopeDefinitionHandle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // ScopeDefinitionHandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct BooleanCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal BooleanCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private bool _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(bool);
            }

            public bool Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // BooleanCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct CharCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal CharCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private char _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(char);
            }

            public char Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // CharCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ByteCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal ByteCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private byte _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(byte);
            }

            public byte Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // ByteCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct SByteCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal SByteCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private sbyte _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(sbyte);
            }

            public sbyte Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // SByteCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Int16Collection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal Int16Collection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private short _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(short);
            }

            public short Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // Int16Collection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct UInt16Collection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal UInt16Collection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private ushort _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(ushort);
            }

            public ushort Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // UInt16Collection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Int32Collection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal Int32Collection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private int _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(int);
            }

            public int Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // Int32Collection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct UInt32Collection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal UInt32Collection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private uint _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(uint);
            }

            public uint Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // UInt32Collection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Int64Collection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal Int64Collection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private long _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(long);
            }

            public long Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // Int64Collection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct UInt64Collection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal UInt64Collection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private ulong _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(ulong);
            }

            public ulong Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // UInt64Collection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct SingleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal SingleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private float _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(float);
            }

            public float Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // SingleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct DoubleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal DoubleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private double _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(double);
            }

            public double Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // DoubleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Handle
    {
        public ArraySignatureHandle ToArraySignatureHandle(MetadataReader reader)
        {
            return new ArraySignatureHandle(this);
        } // ToArraySignatureHandle

        public ByReferenceSignatureHandle ToByReferenceSignatureHandle(MetadataReader reader)
        {
            return new ByReferenceSignatureHandle(this);
        } // ToByReferenceSignatureHandle

        public ConstantBooleanArrayHandle ToConstantBooleanArrayHandle(MetadataReader reader)
        {
            return new ConstantBooleanArrayHandle(this);
        } // ToConstantBooleanArrayHandle

        public ConstantBooleanValueHandle ToConstantBooleanValueHandle(MetadataReader reader)
        {
            return new ConstantBooleanValueHandle(this);
        } // ToConstantBooleanValueHandle

        public ConstantByteArrayHandle ToConstantByteArrayHandle(MetadataReader reader)
        {
            return new ConstantByteArrayHandle(this);
        } // ToConstantByteArrayHandle

        public ConstantByteValueHandle ToConstantByteValueHandle(MetadataReader reader)
        {
            return new ConstantByteValueHandle(this);
        } // ToConstantByteValueHandle

        public ConstantCharArrayHandle ToConstantCharArrayHandle(MetadataReader reader)
        {
            return new ConstantCharArrayHandle(this);
        } // ToConstantCharArrayHandle

        public ConstantCharValueHandle ToConstantCharValueHandle(MetadataReader reader)
        {
            return new ConstantCharValueHandle(this);
        } // ToConstantCharValueHandle

        public ConstantDoubleArrayHandle ToConstantDoubleArrayHandle(MetadataReader reader)
        {
            return new ConstantDoubleArrayHandle(this);
        } // ToConstantDoubleArrayHandle

        public ConstantDoubleValueHandle ToConstantDoubleValueHandle(MetadataReader reader)
        {
            return new ConstantDoubleValueHandle(this);
        } // ToConstantDoubleValueHandle

        public ConstantEnumArrayHandle ToConstantEnumArrayHandle(MetadataReader reader)
        {
            return new ConstantEnumArrayHandle(this);
        } // ToConstantEnumArrayHandle

        public ConstantEnumValueHandle ToConstantEnumValueHandle(MetadataReader reader)
        {
            return new ConstantEnumValueHandle(this);
        } // ToConstantEnumValueHandle

        public ConstantHandleArrayHandle ToConstantHandleArrayHandle(MetadataReader reader)
        {
            return new ConstantHandleArrayHandle(this);
        } // ToConstantHandleArrayHandle

        public ConstantInt16ArrayHandle ToConstantInt16ArrayHandle(MetadataReader reader)
        {
            return new ConstantInt16ArrayHandle(this);
        } // ToConstantInt16ArrayHandle

        public ConstantInt16ValueHandle ToConstantInt16ValueHandle(MetadataReader reader)
        {
            return new ConstantInt16ValueHandle(this);
        } // ToConstantInt16ValueHandle

        public ConstantInt32ArrayHandle ToConstantInt32ArrayHandle(MetadataReader reader)
        {
            return new ConstantInt32ArrayHandle(this);
        } // ToConstantInt32ArrayHandle

        public ConstantInt32ValueHandle ToConstantInt32ValueHandle(MetadataReader reader)
        {
            return new ConstantInt32ValueHandle(this);
        } // ToConstantInt32ValueHandle

        public ConstantInt64ArrayHandle ToConstantInt64ArrayHandle(MetadataReader reader)
        {
            return new ConstantInt64ArrayHandle(this);
        } // ToConstantInt64ArrayHandle

        public ConstantInt64ValueHandle ToConstantInt64ValueHandle(MetadataReader reader)
        {
            return new ConstantInt64ValueHandle(this);
        } // ToConstantInt64ValueHandle

        public ConstantReferenceValueHandle ToConstantReferenceValueHandle(MetadataReader reader)
        {
            return new ConstantReferenceValueHandle(this);
        } // ToConstantReferenceValueHandle

        public ConstantSByteArrayHandle ToConstantSByteArrayHandle(MetadataReader reader)
        {
            return new ConstantSByteArrayHandle(this);
        } // ToConstantSByteArrayHandle

        public ConstantSByteValueHandle ToConstantSByteValueHandle(MetadataReader reader)
        {
            return new ConstantSByteValueHandle(this);
        } // ToConstantSByteValueHandle

        public ConstantSingleArrayHandle ToConstantSingleArrayHandle(MetadataReader reader)
        {
            return new ConstantSingleArrayHandle(this);
        } // ToConstantSingleArrayHandle

        public ConstantSingleValueHandle ToConstantSingleValueHandle(MetadataReader reader)
        {
            return new ConstantSingleValueHandle(this);
        } // ToConstantSingleValueHandle

        public ConstantStringArrayHandle ToConstantStringArrayHandle(MetadataReader reader)
        {
            return new ConstantStringArrayHandle(this);
        } // ToConstantStringArrayHandle

        public ConstantStringValueHandle ToConstantStringValueHandle(MetadataReader reader)
        {
            return new ConstantStringValueHandle(this);
        } // ToConstantStringValueHandle

        public ConstantUInt16ArrayHandle ToConstantUInt16ArrayHandle(MetadataReader reader)
        {
            return new ConstantUInt16ArrayHandle(this);
        } // ToConstantUInt16ArrayHandle

        public ConstantUInt16ValueHandle ToConstantUInt16ValueHandle(MetadataReader reader)
        {
            return new ConstantUInt16ValueHandle(this);
        } // ToConstantUInt16ValueHandle

        public ConstantUInt32ArrayHandle ToConstantUInt32ArrayHandle(MetadataReader reader)
        {
            return new ConstantUInt32ArrayHandle(this);
        } // ToConstantUInt32ArrayHandle

        public ConstantUInt32ValueHandle ToConstantUInt32ValueHandle(MetadataReader reader)
        {
            return new ConstantUInt32ValueHandle(this);
        } // ToConstantUInt32ValueHandle

        public ConstantUInt64ArrayHandle ToConstantUInt64ArrayHandle(MetadataReader reader)
        {
            return new ConstantUInt64ArrayHandle(this);
        } // ToConstantUInt64ArrayHandle

        public ConstantUInt64ValueHandle ToConstantUInt64ValueHandle(MetadataReader reader)
        {
            return new ConstantUInt64ValueHandle(this);
        } // ToConstantUInt64ValueHandle

        public CustomAttributeHandle ToCustomAttributeHandle(MetadataReader reader)
        {
            return new CustomAttributeHandle(this);
        } // ToCustomAttributeHandle

        public EventHandle ToEventHandle(MetadataReader reader)
        {
            return new EventHandle(this);
        } // ToEventHandle

        public FieldHandle ToFieldHandle(MetadataReader reader)
        {
            return new FieldHandle(this);
        } // ToFieldHandle

        public FieldSignatureHandle ToFieldSignatureHandle(MetadataReader reader)
        {
            return new FieldSignatureHandle(this);
        } // ToFieldSignatureHandle

        public FunctionPointerSignatureHandle ToFunctionPointerSignatureHandle(MetadataReader reader)
        {
            return new FunctionPointerSignatureHandle(this);
        } // ToFunctionPointerSignatureHandle

        public GenericParameterHandle ToGenericParameterHandle(MetadataReader reader)
        {
            return new GenericParameterHandle(this);
        } // ToGenericParameterHandle

        public MemberReferenceHandle ToMemberReferenceHandle(MetadataReader reader)
        {
            return new MemberReferenceHandle(this);
        } // ToMemberReferenceHandle

        public MethodHandle ToMethodHandle(MetadataReader reader)
        {
            return new MethodHandle(this);
        } // ToMethodHandle

        public MethodInstantiationHandle ToMethodInstantiationHandle(MetadataReader reader)
        {
            return new MethodInstantiationHandle(this);
        } // ToMethodInstantiationHandle

        public MethodSemanticsHandle ToMethodSemanticsHandle(MetadataReader reader)
        {
            return new MethodSemanticsHandle(this);
        } // ToMethodSemanticsHandle

        public MethodSignatureHandle ToMethodSignatureHandle(MetadataReader reader)
        {
            return new MethodSignatureHandle(this);
        } // ToMethodSignatureHandle

        public MethodTypeVariableSignatureHandle ToMethodTypeVariableSignatureHandle(MetadataReader reader)
        {
            return new MethodTypeVariableSignatureHandle(this);
        } // ToMethodTypeVariableSignatureHandle

        public ModifiedTypeHandle ToModifiedTypeHandle(MetadataReader reader)
        {
            return new ModifiedTypeHandle(this);
        } // ToModifiedTypeHandle

        public NamedArgumentHandle ToNamedArgumentHandle(MetadataReader reader)
        {
            return new NamedArgumentHandle(this);
        } // ToNamedArgumentHandle

        public NamespaceDefinitionHandle ToNamespaceDefinitionHandle(MetadataReader reader)
        {
            return new NamespaceDefinitionHandle(this);
        } // ToNamespaceDefinitionHandle

        public NamespaceReferenceHandle ToNamespaceReferenceHandle(MetadataReader reader)
        {
            return new NamespaceReferenceHandle(this);
        } // ToNamespaceReferenceHandle

        public ParameterHandle ToParameterHandle(MetadataReader reader)
        {
            return new ParameterHandle(this);
        } // ToParameterHandle

        public PointerSignatureHandle ToPointerSignatureHandle(MetadataReader reader)
        {
            return new PointerSignatureHandle(this);
        } // ToPointerSignatureHandle

        public PropertyHandle ToPropertyHandle(MetadataReader reader)
        {
            return new PropertyHandle(this);
        } // ToPropertyHandle

        public PropertySignatureHandle ToPropertySignatureHandle(MetadataReader reader)
        {
            return new PropertySignatureHandle(this);
        } // ToPropertySignatureHandle

        public QualifiedFieldHandle ToQualifiedFieldHandle(MetadataReader reader)
        {
            return new QualifiedFieldHandle(this);
        } // ToQualifiedFieldHandle

        public QualifiedMethodHandle ToQualifiedMethodHandle(MetadataReader reader)
        {
            return new QualifiedMethodHandle(this);
        } // ToQualifiedMethodHandle

        public SZArraySignatureHandle ToSZArraySignatureHandle(MetadataReader reader)
        {
            return new SZArraySignatureHandle(this);
        } // ToSZArraySignatureHandle

        public ScopeDefinitionHandle ToScopeDefinitionHandle(MetadataReader reader)
        {
            return new ScopeDefinitionHandle(this);
        } // ToScopeDefinitionHandle

        public ScopeReferenceHandle ToScopeReferenceHandle(MetadataReader reader)
        {
            return new ScopeReferenceHandle(this);
        } // ToScopeReferenceHandle

        public TypeDefinitionHandle ToTypeDefinitionHandle(MetadataReader reader)
        {
            return new TypeDefinitionHandle(this);
        } // ToTypeDefinitionHandle

        public TypeForwarderHandle ToTypeForwarderHandle(MetadataReader reader)
        {
            return new TypeForwarderHandle(this);
        } // ToTypeForwarderHandle

        public TypeInstantiationSignatureHandle ToTypeInstantiationSignatureHandle(MetadataReader reader)
        {
            return new TypeInstantiationSignatureHandle(this);
        } // ToTypeInstantiationSignatureHandle

        public TypeReferenceHandle ToTypeReferenceHandle(MetadataReader reader)
        {
            return new TypeReferenceHandle(this);
        } // ToTypeReferenceHandle

        public TypeSpecificationHandle ToTypeSpecificationHandle(MetadataReader reader)
        {
            return new TypeSpecificationHandle(this);
        } // ToTypeSpecificationHandle

        public TypeVariableSignatureHandle ToTypeVariableSignatureHandle(MetadataReader reader)
        {
            return new TypeVariableSignatureHandle(this);
        } // ToTypeVariableSignatureHandle
    } // Handle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct HandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal HandleCollection(NativeReader reader, uint offset)
        {
            _offset = offset;
            _reader = reader;
        }

        public int Count
        {
            get
            {
                uint count;
                _reader.DecodeUnsigned(_offset, out count);
                return (int)count;
            }
        } // Count

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_reader, _offset);
        } // GetEnumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private readonly NativeReader _reader;
            private uint _offset;
            private uint _remaining;
            private Handle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(Handle);
            }

            public Handle Current
            {
                get
                {
                    return _current;
                }
            } // Current

            public bool MoveNext()
            {
                if (_remaining == 0)
                    return false;
                _remaining--;
                _offset = _reader.Read(_offset, out _current);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator
    } // HandleCollection

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public partial class MetadataReader
    {
        public ArraySignature GetArraySignature(ArraySignatureHandle handle)
        {
            return new ArraySignature(this, handle);
        } // GetArraySignature

        public ByReferenceSignature GetByReferenceSignature(ByReferenceSignatureHandle handle)
        {
            return new ByReferenceSignature(this, handle);
        } // GetByReferenceSignature

        public ConstantBooleanArray GetConstantBooleanArray(ConstantBooleanArrayHandle handle)
        {
            return new ConstantBooleanArray(this, handle);
        } // GetConstantBooleanArray

        public ConstantBooleanValue GetConstantBooleanValue(ConstantBooleanValueHandle handle)
        {
            return new ConstantBooleanValue(this, handle);
        } // GetConstantBooleanValue

        public ConstantByteArray GetConstantByteArray(ConstantByteArrayHandle handle)
        {
            return new ConstantByteArray(this, handle);
        } // GetConstantByteArray

        public ConstantByteValue GetConstantByteValue(ConstantByteValueHandle handle)
        {
            return new ConstantByteValue(this, handle);
        } // GetConstantByteValue

        public ConstantCharArray GetConstantCharArray(ConstantCharArrayHandle handle)
        {
            return new ConstantCharArray(this, handle);
        } // GetConstantCharArray

        public ConstantCharValue GetConstantCharValue(ConstantCharValueHandle handle)
        {
            return new ConstantCharValue(this, handle);
        } // GetConstantCharValue

        public ConstantDoubleArray GetConstantDoubleArray(ConstantDoubleArrayHandle handle)
        {
            return new ConstantDoubleArray(this, handle);
        } // GetConstantDoubleArray

        public ConstantDoubleValue GetConstantDoubleValue(ConstantDoubleValueHandle handle)
        {
            return new ConstantDoubleValue(this, handle);
        } // GetConstantDoubleValue

        public ConstantEnumArray GetConstantEnumArray(ConstantEnumArrayHandle handle)
        {
            return new ConstantEnumArray(this, handle);
        } // GetConstantEnumArray

        public ConstantEnumValue GetConstantEnumValue(ConstantEnumValueHandle handle)
        {
            return new ConstantEnumValue(this, handle);
        } // GetConstantEnumValue

        public ConstantHandleArray GetConstantHandleArray(ConstantHandleArrayHandle handle)
        {
            return new ConstantHandleArray(this, handle);
        } // GetConstantHandleArray

        public ConstantInt16Array GetConstantInt16Array(ConstantInt16ArrayHandle handle)
        {
            return new ConstantInt16Array(this, handle);
        } // GetConstantInt16Array

        public ConstantInt16Value GetConstantInt16Value(ConstantInt16ValueHandle handle)
        {
            return new ConstantInt16Value(this, handle);
        } // GetConstantInt16Value

        public ConstantInt32Array GetConstantInt32Array(ConstantInt32ArrayHandle handle)
        {
            return new ConstantInt32Array(this, handle);
        } // GetConstantInt32Array

        public ConstantInt32Value GetConstantInt32Value(ConstantInt32ValueHandle handle)
        {
            return new ConstantInt32Value(this, handle);
        } // GetConstantInt32Value

        public ConstantInt64Array GetConstantInt64Array(ConstantInt64ArrayHandle handle)
        {
            return new ConstantInt64Array(this, handle);
        } // GetConstantInt64Array

        public ConstantInt64Value GetConstantInt64Value(ConstantInt64ValueHandle handle)
        {
            return new ConstantInt64Value(this, handle);
        } // GetConstantInt64Value

        public ConstantReferenceValue GetConstantReferenceValue(ConstantReferenceValueHandle handle)
        {
            return new ConstantReferenceValue(this, handle);
        } // GetConstantReferenceValue

        public ConstantSByteArray GetConstantSByteArray(ConstantSByteArrayHandle handle)
        {
            return new ConstantSByteArray(this, handle);
        } // GetConstantSByteArray

        public ConstantSByteValue GetConstantSByteValue(ConstantSByteValueHandle handle)
        {
            return new ConstantSByteValue(this, handle);
        } // GetConstantSByteValue

        public ConstantSingleArray GetConstantSingleArray(ConstantSingleArrayHandle handle)
        {
            return new ConstantSingleArray(this, handle);
        } // GetConstantSingleArray

        public ConstantSingleValue GetConstantSingleValue(ConstantSingleValueHandle handle)
        {
            return new ConstantSingleValue(this, handle);
        } // GetConstantSingleValue

        public ConstantStringArray GetConstantStringArray(ConstantStringArrayHandle handle)
        {
            return new ConstantStringArray(this, handle);
        } // GetConstantStringArray

        public ConstantStringValue GetConstantStringValue(ConstantStringValueHandle handle)
        {
            return new ConstantStringValue(this, handle);
        } // GetConstantStringValue

        public ConstantUInt16Array GetConstantUInt16Array(ConstantUInt16ArrayHandle handle)
        {
            return new ConstantUInt16Array(this, handle);
        } // GetConstantUInt16Array

        public ConstantUInt16Value GetConstantUInt16Value(ConstantUInt16ValueHandle handle)
        {
            return new ConstantUInt16Value(this, handle);
        } // GetConstantUInt16Value

        public ConstantUInt32Array GetConstantUInt32Array(ConstantUInt32ArrayHandle handle)
        {
            return new ConstantUInt32Array(this, handle);
        } // GetConstantUInt32Array

        public ConstantUInt32Value GetConstantUInt32Value(ConstantUInt32ValueHandle handle)
        {
            return new ConstantUInt32Value(this, handle);
        } // GetConstantUInt32Value

        public ConstantUInt64Array GetConstantUInt64Array(ConstantUInt64ArrayHandle handle)
        {
            return new ConstantUInt64Array(this, handle);
        } // GetConstantUInt64Array

        public ConstantUInt64Value GetConstantUInt64Value(ConstantUInt64ValueHandle handle)
        {
            return new ConstantUInt64Value(this, handle);
        } // GetConstantUInt64Value

        public CustomAttribute GetCustomAttribute(CustomAttributeHandle handle)
        {
            return new CustomAttribute(this, handle);
        } // GetCustomAttribute

        public Event GetEvent(EventHandle handle)
        {
            return new Event(this, handle);
        } // GetEvent

        public Field GetField(FieldHandle handle)
        {
            return new Field(this, handle);
        } // GetField

        public FieldSignature GetFieldSignature(FieldSignatureHandle handle)
        {
            return new FieldSignature(this, handle);
        } // GetFieldSignature

        public FunctionPointerSignature GetFunctionPointerSignature(FunctionPointerSignatureHandle handle)
        {
            return new FunctionPointerSignature(this, handle);
        } // GetFunctionPointerSignature

        public GenericParameter GetGenericParameter(GenericParameterHandle handle)
        {
            return new GenericParameter(this, handle);
        } // GetGenericParameter

        public MemberReference GetMemberReference(MemberReferenceHandle handle)
        {
            return new MemberReference(this, handle);
        } // GetMemberReference

        public Method GetMethod(MethodHandle handle)
        {
            return new Method(this, handle);
        } // GetMethod

        public MethodInstantiation GetMethodInstantiation(MethodInstantiationHandle handle)
        {
            return new MethodInstantiation(this, handle);
        } // GetMethodInstantiation

        public MethodSemantics GetMethodSemantics(MethodSemanticsHandle handle)
        {
            return new MethodSemantics(this, handle);
        } // GetMethodSemantics

        public MethodSignature GetMethodSignature(MethodSignatureHandle handle)
        {
            return new MethodSignature(this, handle);
        } // GetMethodSignature

        public MethodTypeVariableSignature GetMethodTypeVariableSignature(MethodTypeVariableSignatureHandle handle)
        {
            return new MethodTypeVariableSignature(this, handle);
        } // GetMethodTypeVariableSignature

        public ModifiedType GetModifiedType(ModifiedTypeHandle handle)
        {
            return new ModifiedType(this, handle);
        } // GetModifiedType

        public NamedArgument GetNamedArgument(NamedArgumentHandle handle)
        {
            return new NamedArgument(this, handle);
        } // GetNamedArgument

        public NamespaceDefinition GetNamespaceDefinition(NamespaceDefinitionHandle handle)
        {
            return new NamespaceDefinition(this, handle);
        } // GetNamespaceDefinition

        public NamespaceReference GetNamespaceReference(NamespaceReferenceHandle handle)
        {
            return new NamespaceReference(this, handle);
        } // GetNamespaceReference

        public Parameter GetParameter(ParameterHandle handle)
        {
            return new Parameter(this, handle);
        } // GetParameter

        public PointerSignature GetPointerSignature(PointerSignatureHandle handle)
        {
            return new PointerSignature(this, handle);
        } // GetPointerSignature

        public Property GetProperty(PropertyHandle handle)
        {
            return new Property(this, handle);
        } // GetProperty

        public PropertySignature GetPropertySignature(PropertySignatureHandle handle)
        {
            return new PropertySignature(this, handle);
        } // GetPropertySignature

        public QualifiedField GetQualifiedField(QualifiedFieldHandle handle)
        {
            return new QualifiedField(this, handle);
        } // GetQualifiedField

        public QualifiedMethod GetQualifiedMethod(QualifiedMethodHandle handle)
        {
            return new QualifiedMethod(this, handle);
        } // GetQualifiedMethod

        public SZArraySignature GetSZArraySignature(SZArraySignatureHandle handle)
        {
            return new SZArraySignature(this, handle);
        } // GetSZArraySignature

        public ScopeDefinition GetScopeDefinition(ScopeDefinitionHandle handle)
        {
            return new ScopeDefinition(this, handle);
        } // GetScopeDefinition

        public ScopeReference GetScopeReference(ScopeReferenceHandle handle)
        {
            return new ScopeReference(this, handle);
        } // GetScopeReference

        public TypeDefinition GetTypeDefinition(TypeDefinitionHandle handle)
        {
            return new TypeDefinition(this, handle);
        } // GetTypeDefinition

        public TypeForwarder GetTypeForwarder(TypeForwarderHandle handle)
        {
            return new TypeForwarder(this, handle);
        } // GetTypeForwarder

        public TypeInstantiationSignature GetTypeInstantiationSignature(TypeInstantiationSignatureHandle handle)
        {
            return new TypeInstantiationSignature(this, handle);
        } // GetTypeInstantiationSignature

        public TypeReference GetTypeReference(TypeReferenceHandle handle)
        {
            return new TypeReference(this, handle);
        } // GetTypeReference

        public TypeSpecification GetTypeSpecification(TypeSpecificationHandle handle)
        {
            return new TypeSpecification(this, handle);
        } // GetTypeSpecification

        public TypeVariableSignature GetTypeVariableSignature(TypeVariableSignatureHandle handle)
        {
            return new TypeVariableSignature(this, handle);
        } // GetTypeVariableSignature
    } // MetadataReader
} // Internal.Metadata.NativeFormat
