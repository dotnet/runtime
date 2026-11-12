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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.ArraySignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.ArraySignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public ArraySignature GetArraySignature(MetadataReader reader)
            => new ArraySignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.ArraySignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.ByReferenceSignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.ByReferenceSignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public ByReferenceSignature GetByReferenceSignature(MetadataReader reader)
            => new ByReferenceSignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.ByReferenceSignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
    } // ByReferenceSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct Constant
    {
        private readonly MetadataReader _reader;
        private readonly ConstantHandle _handle;

        internal Constant(MetadataReader reader, ConstantHandle handle)
        {
            _reader = reader;
            _handle = handle;
            uint offset = (uint)handle.Offset;
            NativeReader streamReader = reader._streamReader;
            offset = streamReader.Read(offset, out _typeCode);
            _blob = new ByteCollection(streamReader, offset);
        }

        public ConstantHandle Handle => _handle;

        public ConstantTypeCode TypeCode => _typeCode;
        private readonly ConstantTypeCode _typeCode;

        public ByteCollection Blob => _blob;
        private readonly ByteCollection _blob;
    } // Constant

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantHandle
    {
        internal readonly int _value;

        internal ConstantHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantHandle(int value)
        {
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.Constant || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.Constant) << 25);
            _Validate();
        }

        public override bool Equals(object obj)
        {
            if (obj is ConstantHandle)
                return _value == ((ConstantHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantHandle handle) => _value == handle._value;

        public bool Equals(Handle handle) => _value == handle._value;

        public override int GetHashCode() => (int)_value;

        public static implicit operator Handle(ConstantHandle handle)
            => new Handle(handle._value);

        internal int Offset => (_value & 0x01FFFFFF);

        public Constant GetConstant(MetadataReader reader)
            => new Constant(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.Constant)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
    } // ConstantHandle

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

        public ConstantStringValueHandleCollection Value => _value;
        private readonly ConstantStringValueHandleCollection _value;
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.ConstantStringArray || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.ConstantStringArray) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public ConstantStringArray GetConstantStringArray(MetadataReader reader)
            => new ConstantStringArray(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.ConstantStringArray)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.ConstantStringValue || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.ConstantStringValue) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public ConstantStringValue GetConstantStringValue(MetadataReader reader)
            => new ConstantStringValue(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.ConstantStringValue)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
    } // ConstantStringValueHandle

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
            _blob = new ByteCollection(streamReader, offset);
        }

        public CustomAttributeHandle Handle => _handle;

        /// One of: QualifiedMethod, MemberReference
        public Handle Constructor => _constructor;
        private readonly Handle _constructor;

        public ByteCollection Blob => _blob;
        private readonly ByteCollection _blob;
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.CustomAttribute || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.CustomAttribute) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public CustomAttribute GetCustomAttribute(MetadataReader reader)
            => new CustomAttribute(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.CustomAttribute)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.Event || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.Event) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public Event GetEvent(MetadataReader reader)
            => new Event(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.Event)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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

        public ConstantHandle DefaultValue => _defaultValue;
        private readonly ConstantHandle _defaultValue;

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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.Field || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.Field) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public Field GetField(MetadataReader reader)
            => new Field(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.Field)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.FieldSignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.FieldSignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public FieldSignature GetFieldSignature(MetadataReader reader)
            => new FieldSignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.FieldSignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.FunctionPointerSignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.FunctionPointerSignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public FunctionPointerSignature GetFunctionPointerSignature(MetadataReader reader)
            => new FunctionPointerSignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.FunctionPointerSignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.GenericParameter || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.GenericParameter) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public GenericParameter GetGenericParameter(MetadataReader reader)
            => new GenericParameter(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.GenericParameter)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.MemberReference || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.MemberReference) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public MemberReference GetMemberReference(MetadataReader reader)
            => new MemberReference(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.MemberReference)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.Method || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.Method) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public Method GetMethod(MetadataReader reader)
            => new Method(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.Method)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.MethodInstantiation || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.MethodInstantiation) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public MethodInstantiation GetMethodInstantiation(MetadataReader reader)
            => new MethodInstantiation(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.MethodInstantiation)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.MethodSemantics || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.MethodSemantics) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public MethodSemantics GetMethodSemantics(MetadataReader reader)
            => new MethodSemantics(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.MethodSemantics)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.MethodSignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.MethodSignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public MethodSignature GetMethodSignature(MetadataReader reader)
            => new MethodSignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.MethodSignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.MethodTypeVariableSignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.MethodTypeVariableSignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public MethodTypeVariableSignature GetMethodTypeVariableSignature(MetadataReader reader)
            => new MethodTypeVariableSignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.MethodTypeVariableSignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.ModifiedType || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.ModifiedType) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public ModifiedType GetModifiedType(MetadataReader reader)
            => new ModifiedType(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.ModifiedType)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
    } // ModifiedTypeHandle

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
            offset = streamReader.Read(offset, out _scope);
            offset = streamReader.Read(offset, out _name);
        }

        public NamespaceDefinitionHandle Handle => _handle;

        public ScopeDefinitionHandle Scope => _scope;
        private readonly ScopeDefinitionHandle _scope;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.NamespaceDefinition || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.NamespaceDefinition) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public NamespaceDefinition GetNamespaceDefinition(MetadataReader reader)
            => new NamespaceDefinition(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.NamespaceDefinition)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            offset = streamReader.Read(offset, out _scope);
            offset = streamReader.Read(offset, out _name);
        }

        public NamespaceReferenceHandle Handle => _handle;

        public ScopeReferenceHandle Scope => _scope;
        private readonly ScopeReferenceHandle _scope;

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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.NamespaceReference || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.NamespaceReference) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public NamespaceReference GetNamespaceReference(MetadataReader reader)
            => new NamespaceReference(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.NamespaceReference)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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

        public ConstantHandle DefaultValue => _defaultValue;
        private readonly ConstantHandle _defaultValue;

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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.Parameter || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.Parameter) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public Parameter GetParameter(MetadataReader reader)
            => new Parameter(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.Parameter)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.PointerSignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.PointerSignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public PointerSignature GetPointerSignature(MetadataReader reader)
            => new PointerSignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.PointerSignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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

        public ConstantHandle DefaultValue => _defaultValue;
        private readonly ConstantHandle _defaultValue;

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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.Property || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.Property) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public Property GetProperty(MetadataReader reader)
            => new Property(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.Property)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.PropertySignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.PropertySignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public PropertySignature GetPropertySignature(MetadataReader reader)
            => new PropertySignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.PropertySignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.QualifiedField || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.QualifiedField) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public QualifiedField GetQualifiedField(MetadataReader reader)
            => new QualifiedField(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.QualifiedField)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.QualifiedMethod || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.QualifiedMethod) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public QualifiedMethod GetQualifiedMethod(MetadataReader reader)
            => new QualifiedMethod(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.QualifiedMethod)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.SZArraySignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.SZArraySignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public SZArraySignature GetSZArraySignature(MetadataReader reader)
            => new SZArraySignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.SZArraySignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            offset = streamReader.Read(offset, out _majorVersion);
            offset = streamReader.Read(offset, out _minorVersion);
            offset = streamReader.Read(offset, out _buildNumber);
            offset = streamReader.Read(offset, out _revisionNumber);
            offset = streamReader.Read(offset, out _publicKey);
            offset = streamReader.Read(offset, out _culture);
            offset = streamReader.Read(offset, out _typeDefinitions);
            offset = streamReader.Read(offset, out _typeForwarders);
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

        public TypeDefinitionHandleHashtable TypeDefinitions => _typeDefinitions;
        private readonly TypeDefinitionHandleHashtable _typeDefinitions;

        public TypeForwarderHandleHashtable TypeForwarders => _typeForwarders;
        private readonly TypeForwarderHandleHashtable _typeForwarders;

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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.ScopeDefinition || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.ScopeDefinition) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public ScopeDefinition GetScopeDefinition(MetadataReader reader)
            => new ScopeDefinition(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.ScopeDefinition)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.ScopeReference || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.ScopeReference) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public ScopeReference GetScopeReference(MetadataReader reader)
            => new ScopeReference(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.ScopeReference)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            offset = streamReader.Read(offset, out _namespaceOrEnclosingType);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _size);
            offset = streamReader.Read(offset, out _packingSize);
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

        /// One of: NamespaceDefinition, TypeDefinition
        public Handle NamespaceOrEnclosingType => _namespaceOrEnclosingType;
        private readonly Handle _namespaceOrEnclosingType;

        public ConstantStringValueHandle Name => _name;
        private readonly ConstantStringValueHandle _name;

        public uint Size => _size;
        private readonly uint _size;

        public ushort PackingSize => _packingSize;
        private readonly ushort _packingSize;

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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.TypeDefinition || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.TypeDefinition) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public TypeDefinition GetTypeDefinition(MetadataReader reader)
            => new TypeDefinition(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.TypeDefinition)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            offset = streamReader.Read(offset, out _namespace);
            offset = streamReader.Read(offset, out _name);
            offset = streamReader.Read(offset, out _nestedTypes);
        }

        public TypeForwarderHandle Handle => _handle;

        public NamespaceReferenceHandle Namespace => _namespace;
        private readonly NamespaceReferenceHandle _namespace;

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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.TypeForwarder || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.TypeForwarder) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public TypeForwarder GetTypeForwarder(MetadataReader reader)
            => new TypeForwarder(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.TypeForwarder)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.TypeInstantiationSignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.TypeInstantiationSignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public TypeInstantiationSignature GetTypeInstantiationSignature(MetadataReader reader)
            => new TypeInstantiationSignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.TypeInstantiationSignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            offset = streamReader.Read(offset, out _namespaceOrEnclosingType);
            offset = streamReader.Read(offset, out _typeName);
        }

        public TypeReferenceHandle Handle => _handle;

        /// One of: NamespaceReference, TypeReference
        public Handle NamespaceOrEnclosingType => _namespaceOrEnclosingType;
        private readonly Handle _namespaceOrEnclosingType;

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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.TypeReference || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.TypeReference) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public TypeReference GetTypeReference(MetadataReader reader)
            => new TypeReference(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.TypeReference)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.TypeSpecification || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.TypeSpecification) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public TypeSpecification GetTypeSpecification(MetadataReader reader)
            => new TypeSpecification(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.TypeSpecification)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
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
            HandleType hType = (HandleType)((uint)value >> 25);
            Debug.Assert(hType == HandleType.TypeVariableSignature || hType == HandleType.Null);
            _value = (value & 0x01FFFFFF) | (((int)HandleType.TypeVariableSignature) << 25);
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

        internal int Offset => (_value & 0x01FFFFFF);

        public TypeVariableSignature GetTypeVariableSignature(MetadataReader reader)
            => new TypeVariableSignature(reader, this);

        public bool IsNil => (_value & 0x01FFFFFF) == 0;

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((uint)_value >> 25) != HandleType.TypeVariableSignature)
                throw new ArgumentException();
        } // _Validate

        #if DEBUG
        public override string ToString() => string.Format("{0:X8}", _value);
        #endif
    } // TypeVariableSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct ConstantStringValueHandleCollection
    {
        private readonly NativeReader _reader;
        private readonly uint _offset;

        internal ConstantStringValueHandleCollection(NativeReader reader, uint offset)
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
            private ConstantStringValueHandle _current;

            internal Enumerator(NativeReader reader, uint offset)
            {
                _reader = reader;
                _offset = reader.DecodeUnsigned(offset, out _remaining);
                _current = default(ConstantStringValueHandle);
            }

            public ConstantStringValueHandle Current
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
    } // ConstantStringValueHandleCollection

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
    public readonly partial struct TypeDefinitionHandleHashtable
    {
        private readonly NativeHashtable _hashtable;

        internal TypeDefinitionHandleHashtable(NativeHashtable hashtable)
        {
            _hashtable = hashtable;
        }

        public Enumerator GetEnumerator() => new Enumerator(_hashtable.EnumerateAllEntries());

        public LookupEnumerator Lookup(int hashCode) => new LookupEnumerator(_hashtable.Lookup(hashCode));

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private NativeHashtable.AllEntriesEnumerator _enumerator;
            private TypeDefinitionHandle _current;

            internal Enumerator(NativeHashtable.AllEntriesEnumerator enumerator)
            {
                _enumerator = enumerator;
                _current = default;
            }

            public TypeDefinitionHandle Current => _current;

            public bool MoveNext()
            {
                NativeParser parser = _enumerator.GetNext();
                if (parser.IsNull)
                    return false;
                _current = new TypeDefinitionHandle((int)parser.Offset);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct LookupEnumerator
        {
            private NativeHashtable.Enumerator _enumerator;
            private TypeDefinitionHandle _current;

            internal LookupEnumerator(NativeHashtable.Enumerator enumerator)
            {
                _enumerator = enumerator;
                _current = default;
            }

            public TypeDefinitionHandle Current => _current;

            public LookupEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                NativeParser parser = _enumerator.GetNext();
                if (parser.IsNull)
                    return false;
                _current = new TypeDefinitionHandle((int)parser.Offset);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // LookupEnumerator
    } // TypeDefinitionHandleHashtable

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public readonly partial struct TypeForwarderHandleHashtable
    {
        private readonly NativeHashtable _hashtable;

        internal TypeForwarderHandleHashtable(NativeHashtable hashtable)
        {
            _hashtable = hashtable;
        }

        public Enumerator GetEnumerator() => new Enumerator(_hashtable.EnumerateAllEntries());

        public LookupEnumerator Lookup(int hashCode) => new LookupEnumerator(_hashtable.Lookup(hashCode));

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct Enumerator
        {
            private NativeHashtable.AllEntriesEnumerator _enumerator;
            private TypeForwarderHandle _current;

            internal Enumerator(NativeHashtable.AllEntriesEnumerator enumerator)
            {
                _enumerator = enumerator;
                _current = default;
            }

            public TypeForwarderHandle Current => _current;

            public bool MoveNext()
            {
                NativeParser parser = _enumerator.GetNext();
                if (parser.IsNull)
                    return false;
                _current = new TypeForwarderHandle((int)parser.Offset);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // Enumerator

#if SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
        public struct LookupEnumerator
        {
            private NativeHashtable.Enumerator _enumerator;
            private TypeForwarderHandle _current;

            internal LookupEnumerator(NativeHashtable.Enumerator enumerator)
            {
                _enumerator = enumerator;
                _current = default;
            }

            public TypeForwarderHandle Current => _current;

            public LookupEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                NativeParser parser = _enumerator.GetNext();
                if (parser.IsNull)
                    return false;
                _current = new TypeForwarderHandle((int)parser.Offset);
                return true;
            } // MoveNext

            public void Dispose()
            {
            } // Dispose
        } // LookupEnumerator
    } // TypeForwarderHandleHashtable

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

        public ConstantHandle ToConstantHandle(MetadataReader reader)
        {
            return new ConstantHandle(this);
        } // ToConstantHandle

        public ConstantStringArrayHandle ToConstantStringArrayHandle(MetadataReader reader)
        {
            return new ConstantStringArrayHandle(this);
        } // ToConstantStringArrayHandle

        public ConstantStringValueHandle ToConstantStringValueHandle(MetadataReader reader)
        {
            return new ConstantStringValueHandle(this);
        } // ToConstantStringValueHandle

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

        public Constant GetConstant(ConstantHandle handle)
        {
            return new Constant(this, handle);
        } // GetConstant

        public ConstantStringArray GetConstantStringArray(ConstantStringArrayHandle handle)
        {
            return new ConstantStringArray(this, handle);
        } // GetConstantStringArray

        public ConstantStringValue GetConstantStringValue(ConstantStringValueHandle handle)
        {
            return new ConstantStringValue(this, handle);
        } // GetConstantStringValue

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
