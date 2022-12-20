// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This is a generated file - do not manually edit!

#pragma warning disable 649
#pragma warning disable 169
#pragma warning disable 282 // There is no defined ordering between fields in multiple declarations of partial class or struct
#pragma warning disable CA1066 // IEquatable<T> implementations aren't used
#pragma warning disable CA1822
#pragma warning disable IDE0059

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Internal.NativeFormat;

namespace Internal.Metadata.NativeFormat
{
#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ArraySignature
    {
        internal MetadataReader _reader;
        internal ArraySignatureHandle _handle;

        public ArraySignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle ElementType
        {
            get
            {
                return _elementType;
            }
        } // ElementType

        internal Handle _elementType;

        public int Rank
        {
            get
            {
                return _rank;
            }
        } // Rank

        internal int _rank;

        public Int32Collection Sizes
        {
            get
            {
                return _sizes;
            }
        } // Sizes

        internal Int32Collection _sizes;

        public Int32Collection LowerBounds
        {
            get
            {
                return _lowerBounds;
            }
        } // LowerBounds

        internal Int32Collection _lowerBounds;
    } // ArraySignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ArraySignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ArraySignatureHandle)
                return _value == ((ArraySignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ArraySignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ArraySignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal ArraySignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ArraySignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ArraySignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ArraySignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ArraySignature GetArraySignature(MetadataReader reader)
        {
            return reader.GetArraySignature(this);
        } // GetArraySignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ArraySignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ArraySignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ByReferenceSignature
    {
        internal MetadataReader _reader;
        internal ByReferenceSignatureHandle _handle;

        public ByReferenceSignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle Type
        {
            get
            {
                return _type;
            }
        } // Type

        internal Handle _type;
    } // ByReferenceSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ByReferenceSignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ByReferenceSignatureHandle)
                return _value == ((ByReferenceSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ByReferenceSignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ByReferenceSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal ByReferenceSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ByReferenceSignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ByReferenceSignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ByReferenceSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ByReferenceSignature GetByReferenceSignature(MetadataReader reader)
        {
            return reader.GetByReferenceSignature(this);
        } // GetByReferenceSignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ByReferenceSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ByReferenceSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantBooleanArray
    {
        internal MetadataReader _reader;
        internal ConstantBooleanArrayHandle _handle;

        public ConstantBooleanArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public BooleanCollection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal BooleanCollection _value;
    } // ConstantBooleanArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantBooleanArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantBooleanArrayHandle)
                return _value == ((ConstantBooleanArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantBooleanArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantBooleanArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantBooleanArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantBooleanArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantBooleanArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantBooleanArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantBooleanArray GetConstantBooleanArray(MetadataReader reader)
        {
            return reader.GetConstantBooleanArray(this);
        } // GetConstantBooleanArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantBooleanArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantBooleanArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantBooleanValue
    {
        internal MetadataReader _reader;
        internal ConstantBooleanValueHandle _handle;

        public ConstantBooleanValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public bool Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal bool _value;
    } // ConstantBooleanValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantBooleanValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantBooleanValueHandle)
                return _value == ((ConstantBooleanValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantBooleanValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantBooleanValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantBooleanValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantBooleanValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantBooleanValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantBooleanValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantBooleanValue GetConstantBooleanValue(MetadataReader reader)
        {
            return reader.GetConstantBooleanValue(this);
        } // GetConstantBooleanValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantBooleanValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantBooleanValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantBoxedEnumValue
    {
        internal MetadataReader _reader;
        internal ConstantBoxedEnumValueHandle _handle;

        public ConstantBoxedEnumValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: ConstantByteValue, ConstantSByteValue, ConstantInt16Value, ConstantUInt16Value, ConstantInt32Value, ConstantUInt32Value, ConstantInt64Value, ConstantUInt64Value

        public Handle Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal Handle _value;
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle Type
        {
            get
            {
                return _type;
            }
        } // Type

        internal Handle _type;
    } // ConstantBoxedEnumValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantBoxedEnumValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantBoxedEnumValueHandle)
                return _value == ((ConstantBoxedEnumValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantBoxedEnumValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantBoxedEnumValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantBoxedEnumValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantBoxedEnumValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantBoxedEnumValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantBoxedEnumValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantBoxedEnumValue GetConstantBoxedEnumValue(MetadataReader reader)
        {
            return reader.GetConstantBoxedEnumValue(this);
        } // GetConstantBoxedEnumValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantBoxedEnumValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantBoxedEnumValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantByteArray
    {
        internal MetadataReader _reader;
        internal ConstantByteArrayHandle _handle;

        public ConstantByteArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public ByteCollection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal ByteCollection _value;
    } // ConstantByteArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantByteArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantByteArrayHandle)
                return _value == ((ConstantByteArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantByteArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantByteArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantByteArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantByteArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantByteArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantByteArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantByteArray GetConstantByteArray(MetadataReader reader)
        {
            return reader.GetConstantByteArray(this);
        } // GetConstantByteArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantByteArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantByteArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantByteValue
    {
        internal MetadataReader _reader;
        internal ConstantByteValueHandle _handle;

        public ConstantByteValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public byte Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal byte _value;
    } // ConstantByteValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantByteValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantByteValueHandle)
                return _value == ((ConstantByteValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantByteValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantByteValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantByteValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantByteValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantByteValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantByteValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantByteValue GetConstantByteValue(MetadataReader reader)
        {
            return reader.GetConstantByteValue(this);
        } // GetConstantByteValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantByteValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantByteValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantCharArray
    {
        internal MetadataReader _reader;
        internal ConstantCharArrayHandle _handle;

        public ConstantCharArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public CharCollection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal CharCollection _value;
    } // ConstantCharArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantCharArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantCharArrayHandle)
                return _value == ((ConstantCharArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantCharArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantCharArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantCharArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantCharArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantCharArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantCharArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantCharArray GetConstantCharArray(MetadataReader reader)
        {
            return reader.GetConstantCharArray(this);
        } // GetConstantCharArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantCharArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantCharArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantCharValue
    {
        internal MetadataReader _reader;
        internal ConstantCharValueHandle _handle;

        public ConstantCharValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public char Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal char _value;
    } // ConstantCharValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantCharValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantCharValueHandle)
                return _value == ((ConstantCharValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantCharValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantCharValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantCharValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantCharValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantCharValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantCharValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantCharValue GetConstantCharValue(MetadataReader reader)
        {
            return reader.GetConstantCharValue(this);
        } // GetConstantCharValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantCharValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantCharValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantDoubleArray
    {
        internal MetadataReader _reader;
        internal ConstantDoubleArrayHandle _handle;

        public ConstantDoubleArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public DoubleCollection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal DoubleCollection _value;
    } // ConstantDoubleArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantDoubleArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantDoubleArrayHandle)
                return _value == ((ConstantDoubleArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantDoubleArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantDoubleArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantDoubleArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantDoubleArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantDoubleArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantDoubleArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantDoubleArray GetConstantDoubleArray(MetadataReader reader)
        {
            return reader.GetConstantDoubleArray(this);
        } // GetConstantDoubleArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantDoubleArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantDoubleArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantDoubleValue
    {
        internal MetadataReader _reader;
        internal ConstantDoubleValueHandle _handle;

        public ConstantDoubleValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public double Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal double _value;
    } // ConstantDoubleValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantDoubleValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantDoubleValueHandle)
                return _value == ((ConstantDoubleValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantDoubleValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantDoubleValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantDoubleValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantDoubleValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantDoubleValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantDoubleValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantDoubleValue GetConstantDoubleValue(MetadataReader reader)
        {
            return reader.GetConstantDoubleValue(this);
        } // GetConstantDoubleValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantDoubleValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantDoubleValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantEnumArray
    {
        internal MetadataReader _reader;
        internal ConstantEnumArrayHandle _handle;

        public ConstantEnumArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public Handle ElementType
        {
            get
            {
                return _elementType;
            }
        } // ElementType

        internal Handle _elementType;

        public Handle Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal Handle _value;
    } // ConstantEnumArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantEnumArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantEnumArrayHandle)
                return _value == ((ConstantEnumArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantEnumArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantEnumArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantEnumArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantEnumArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantEnumArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantEnumArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantEnumArray GetConstantEnumArray(MetadataReader reader)
        {
            return reader.GetConstantEnumArray(this);
        } // GetConstantEnumArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantEnumArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantEnumArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantHandleArray
    {
        internal MetadataReader _reader;
        internal ConstantHandleArrayHandle _handle;

        public ConstantHandleArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public HandleCollection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal HandleCollection _value;
    } // ConstantHandleArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantHandleArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantHandleArrayHandle)
                return _value == ((ConstantHandleArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantHandleArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantHandleArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantHandleArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantHandleArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantHandleArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantHandleArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantHandleArray GetConstantHandleArray(MetadataReader reader)
        {
            return reader.GetConstantHandleArray(this);
        } // GetConstantHandleArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantHandleArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantHandleArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt16Array
    {
        internal MetadataReader _reader;
        internal ConstantInt16ArrayHandle _handle;

        public ConstantInt16ArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public Int16Collection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal Int16Collection _value;
    } // ConstantInt16Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt16ArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantInt16ArrayHandle)
                return _value == ((ConstantInt16ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt16ArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantInt16ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt16ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantInt16Array || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt16Array) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantInt16ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantInt16Array GetConstantInt16Array(MetadataReader reader)
        {
            return reader.GetConstantInt16Array(this);
        } // GetConstantInt16Array

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt16Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantInt16ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt16Value
    {
        internal MetadataReader _reader;
        internal ConstantInt16ValueHandle _handle;

        public ConstantInt16ValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public short Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal short _value;
    } // ConstantInt16Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt16ValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantInt16ValueHandle)
                return _value == ((ConstantInt16ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt16ValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantInt16ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt16ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantInt16Value || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt16Value) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantInt16ValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantInt16Value GetConstantInt16Value(MetadataReader reader)
        {
            return reader.GetConstantInt16Value(this);
        } // GetConstantInt16Value

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt16Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantInt16ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt32Array
    {
        internal MetadataReader _reader;
        internal ConstantInt32ArrayHandle _handle;

        public ConstantInt32ArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public Int32Collection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal Int32Collection _value;
    } // ConstantInt32Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt32ArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantInt32ArrayHandle)
                return _value == ((ConstantInt32ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt32ArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantInt32ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt32ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantInt32Array || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt32Array) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantInt32ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantInt32Array GetConstantInt32Array(MetadataReader reader)
        {
            return reader.GetConstantInt32Array(this);
        } // GetConstantInt32Array

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt32Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantInt32ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt32Value
    {
        internal MetadataReader _reader;
        internal ConstantInt32ValueHandle _handle;

        public ConstantInt32ValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public int Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal int _value;
    } // ConstantInt32Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt32ValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantInt32ValueHandle)
                return _value == ((ConstantInt32ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt32ValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantInt32ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt32ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantInt32Value || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt32Value) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantInt32ValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantInt32Value GetConstantInt32Value(MetadataReader reader)
        {
            return reader.GetConstantInt32Value(this);
        } // GetConstantInt32Value

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt32Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantInt32ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt64Array
    {
        internal MetadataReader _reader;
        internal ConstantInt64ArrayHandle _handle;

        public ConstantInt64ArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public Int64Collection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal Int64Collection _value;
    } // ConstantInt64Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt64ArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantInt64ArrayHandle)
                return _value == ((ConstantInt64ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt64ArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantInt64ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt64ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantInt64Array || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt64Array) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantInt64ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantInt64Array GetConstantInt64Array(MetadataReader reader)
        {
            return reader.GetConstantInt64Array(this);
        } // GetConstantInt64Array

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt64Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantInt64ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt64Value
    {
        internal MetadataReader _reader;
        internal ConstantInt64ValueHandle _handle;

        public ConstantInt64ValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public long Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal long _value;
    } // ConstantInt64Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantInt64ValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantInt64ValueHandle)
                return _value == ((ConstantInt64ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantInt64ValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantInt64ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantInt64ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantInt64Value || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantInt64Value) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantInt64ValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantInt64Value GetConstantInt64Value(MetadataReader reader)
        {
            return reader.GetConstantInt64Value(this);
        } // GetConstantInt64Value

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantInt64Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantInt64ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantReferenceValue
    {
        internal MetadataReader _reader;
        internal ConstantReferenceValueHandle _handle;

        public ConstantReferenceValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
    } // ConstantReferenceValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantReferenceValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantReferenceValueHandle)
                return _value == ((ConstantReferenceValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantReferenceValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantReferenceValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantReferenceValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantReferenceValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantReferenceValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantReferenceValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantReferenceValue GetConstantReferenceValue(MetadataReader reader)
        {
            return reader.GetConstantReferenceValue(this);
        } // GetConstantReferenceValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantReferenceValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantReferenceValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantSByteArray
    {
        internal MetadataReader _reader;
        internal ConstantSByteArrayHandle _handle;

        public ConstantSByteArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public SByteCollection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal SByteCollection _value;
    } // ConstantSByteArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantSByteArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantSByteArrayHandle)
                return _value == ((ConstantSByteArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantSByteArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantSByteArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantSByteArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantSByteArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantSByteArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantSByteArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantSByteArray GetConstantSByteArray(MetadataReader reader)
        {
            return reader.GetConstantSByteArray(this);
        } // GetConstantSByteArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantSByteArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantSByteArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantSByteValue
    {
        internal MetadataReader _reader;
        internal ConstantSByteValueHandle _handle;

        public ConstantSByteValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public sbyte Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal sbyte _value;
    } // ConstantSByteValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantSByteValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantSByteValueHandle)
                return _value == ((ConstantSByteValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantSByteValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantSByteValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantSByteValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantSByteValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantSByteValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantSByteValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantSByteValue GetConstantSByteValue(MetadataReader reader)
        {
            return reader.GetConstantSByteValue(this);
        } // GetConstantSByteValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantSByteValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantSByteValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantSingleArray
    {
        internal MetadataReader _reader;
        internal ConstantSingleArrayHandle _handle;

        public ConstantSingleArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public SingleCollection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal SingleCollection _value;
    } // ConstantSingleArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantSingleArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantSingleArrayHandle)
                return _value == ((ConstantSingleArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantSingleArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantSingleArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantSingleArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantSingleArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantSingleArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantSingleArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantSingleArray GetConstantSingleArray(MetadataReader reader)
        {
            return reader.GetConstantSingleArray(this);
        } // GetConstantSingleArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantSingleArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantSingleArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantSingleValue
    {
        internal MetadataReader _reader;
        internal ConstantSingleValueHandle _handle;

        public ConstantSingleValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public float Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal float _value;
    } // ConstantSingleValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantSingleValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantSingleValueHandle)
                return _value == ((ConstantSingleValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantSingleValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantSingleValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantSingleValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantSingleValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantSingleValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantSingleValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantSingleValue GetConstantSingleValue(MetadataReader reader)
        {
            return reader.GetConstantSingleValue(this);
        } // GetConstantSingleValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantSingleValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantSingleValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantStringArray
    {
        internal MetadataReader _reader;
        internal ConstantStringArrayHandle _handle;

        public ConstantStringArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: ConstantStringValue, ConstantReferenceValue

        public HandleCollection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal HandleCollection _value;
    } // ConstantStringArray

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantStringArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantStringArrayHandle)
                return _value == ((ConstantStringArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantStringArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantStringArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantStringArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantStringArray || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantStringArray) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantStringArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantStringArray GetConstantStringArray(MetadataReader reader)
        {
            return reader.GetConstantStringArray(this);
        } // GetConstantStringArray

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantStringArray)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantStringArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantStringValue
    {
        internal MetadataReader _reader;
        internal ConstantStringValueHandle _handle;

        public ConstantStringValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public string Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal string _value;
    } // ConstantStringValue

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantStringValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantStringValueHandle)
                return _value == ((ConstantStringValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantStringValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantStringValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantStringValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantStringValue || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantStringValue) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantStringValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantStringValue GetConstantStringValue(MetadataReader reader)
        {
            return reader.GetConstantStringValue(this);
        } // GetConstantStringValue

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantStringValue)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantStringValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt16Array
    {
        internal MetadataReader _reader;
        internal ConstantUInt16ArrayHandle _handle;

        public ConstantUInt16ArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public UInt16Collection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal UInt16Collection _value;
    } // ConstantUInt16Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt16ArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt16ArrayHandle)
                return _value == ((ConstantUInt16ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt16ArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantUInt16ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt16ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantUInt16Array || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt16Array) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantUInt16ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantUInt16Array GetConstantUInt16Array(MetadataReader reader)
        {
            return reader.GetConstantUInt16Array(this);
        } // GetConstantUInt16Array

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt16Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantUInt16ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt16Value
    {
        internal MetadataReader _reader;
        internal ConstantUInt16ValueHandle _handle;

        public ConstantUInt16ValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public ushort Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal ushort _value;
    } // ConstantUInt16Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt16ValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt16ValueHandle)
                return _value == ((ConstantUInt16ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt16ValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantUInt16ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt16ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantUInt16Value || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt16Value) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantUInt16ValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantUInt16Value GetConstantUInt16Value(MetadataReader reader)
        {
            return reader.GetConstantUInt16Value(this);
        } // GetConstantUInt16Value

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt16Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantUInt16ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt32Array
    {
        internal MetadataReader _reader;
        internal ConstantUInt32ArrayHandle _handle;

        public ConstantUInt32ArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public UInt32Collection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal UInt32Collection _value;
    } // ConstantUInt32Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt32ArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt32ArrayHandle)
                return _value == ((ConstantUInt32ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt32ArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantUInt32ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt32ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantUInt32Array || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt32Array) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantUInt32ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantUInt32Array GetConstantUInt32Array(MetadataReader reader)
        {
            return reader.GetConstantUInt32Array(this);
        } // GetConstantUInt32Array

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt32Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantUInt32ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt32Value
    {
        internal MetadataReader _reader;
        internal ConstantUInt32ValueHandle _handle;

        public ConstantUInt32ValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public uint Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal uint _value;
    } // ConstantUInt32Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt32ValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt32ValueHandle)
                return _value == ((ConstantUInt32ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt32ValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantUInt32ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt32ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantUInt32Value || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt32Value) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantUInt32ValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantUInt32Value GetConstantUInt32Value(MetadataReader reader)
        {
            return reader.GetConstantUInt32Value(this);
        } // GetConstantUInt32Value

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt32Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantUInt32ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt64Array
    {
        internal MetadataReader _reader;
        internal ConstantUInt64ArrayHandle _handle;

        public ConstantUInt64ArrayHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public UInt64Collection Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal UInt64Collection _value;
    } // ConstantUInt64Array

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt64ArrayHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt64ArrayHandle)
                return _value == ((ConstantUInt64ArrayHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt64ArrayHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantUInt64ArrayHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt64ArrayHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantUInt64Array || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt64Array) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantUInt64ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantUInt64Array GetConstantUInt64Array(MetadataReader reader)
        {
            return reader.GetConstantUInt64Array(this);
        } // GetConstantUInt64Array

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt64Array)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantUInt64ArrayHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt64Value
    {
        internal MetadataReader _reader;
        internal ConstantUInt64ValueHandle _handle;

        public ConstantUInt64ValueHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public ulong Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal ulong _value;
    } // ConstantUInt64Value

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ConstantUInt64ValueHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ConstantUInt64ValueHandle)
                return _value == ((ConstantUInt64ValueHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ConstantUInt64ValueHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ConstantUInt64ValueHandle(Handle handle) : this(handle._value)
        {
        }

        internal ConstantUInt64ValueHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ConstantUInt64Value || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ConstantUInt64Value) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ConstantUInt64ValueHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ConstantUInt64Value GetConstantUInt64Value(MetadataReader reader)
        {
            return reader.GetConstantUInt64Value(this);
        } // GetConstantUInt64Value

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ConstantUInt64Value)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ConstantUInt64ValueHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct CustomAttribute
    {
        internal MetadataReader _reader;
        internal CustomAttributeHandle _handle;

        public CustomAttributeHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: QualifiedMethod, MemberReference

        public Handle Constructor
        {
            get
            {
                return _constructor;
            }
        } // Constructor

        internal Handle _constructor;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value

        public HandleCollection FixedArguments
        {
            get
            {
                return _fixedArguments;
            }
        } // FixedArguments

        internal HandleCollection _fixedArguments;

        public NamedArgumentHandleCollection NamedArguments
        {
            get
            {
                return _namedArguments;
            }
        } // NamedArguments

        internal NamedArgumentHandleCollection _namedArguments;
    } // CustomAttribute

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct CustomAttributeHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is CustomAttributeHandle)
                return _value == ((CustomAttributeHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(CustomAttributeHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal CustomAttributeHandle(Handle handle) : this(handle._value)
        {
        }

        internal CustomAttributeHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.CustomAttribute || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.CustomAttribute) << 24);
            _Validate();
        }

        public static implicit operator  Handle(CustomAttributeHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public CustomAttribute GetCustomAttribute(MetadataReader reader)
        {
            return reader.GetCustomAttribute(this);
        } // GetCustomAttribute

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.CustomAttribute)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // CustomAttributeHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct Event
    {
        internal MetadataReader _reader;
        internal EventHandle _handle;

        public EventHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public EventAttributes Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal EventAttributes _flags;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle Type
        {
            get
            {
                return _type;
            }
        } // Type

        internal Handle _type;

        public MethodSemanticsHandleCollection MethodSemantics
        {
            get
            {
                return _methodSemantics;
            }
        } // MethodSemantics

        internal MethodSemanticsHandleCollection _methodSemantics;

        public CustomAttributeHandleCollection CustomAttributes
        {
            get
            {
                return _customAttributes;
            }
        } // CustomAttributes

        internal CustomAttributeHandleCollection _customAttributes;
    } // Event

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct EventHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is EventHandle)
                return _value == ((EventHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(EventHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal EventHandle(Handle handle) : this(handle._value)
        {
        }

        internal EventHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.Event || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Event) << 24);
            _Validate();
        }

        public static implicit operator  Handle(EventHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public Event GetEvent(MetadataReader reader)
        {
            return reader.GetEvent(this);
        } // GetEvent

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Event)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // EventHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct Field
    {
        internal MetadataReader _reader;
        internal FieldHandle _handle;

        public FieldHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public FieldAttributes Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal FieldAttributes _flags;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;

        public FieldSignatureHandle Signature
        {
            get
            {
                return _signature;
            }
        } // Signature

        internal FieldSignatureHandle _signature;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value

        public Handle DefaultValue
        {
            get
            {
                return _defaultValue;
            }
        } // DefaultValue

        internal Handle _defaultValue;

        public uint Offset
        {
            get
            {
                return _offset;
            }
        } // Offset

        internal uint _offset;

        public CustomAttributeHandleCollection CustomAttributes
        {
            get
            {
                return _customAttributes;
            }
        } // CustomAttributes

        internal CustomAttributeHandleCollection _customAttributes;
    } // Field

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct FieldHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is FieldHandle)
                return _value == ((FieldHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(FieldHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal FieldHandle(Handle handle) : this(handle._value)
        {
        }

        internal FieldHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.Field || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Field) << 24);
            _Validate();
        }

        public static implicit operator  Handle(FieldHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public Field GetField(MetadataReader reader)
        {
            return reader.GetField(this);
        } // GetField

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Field)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // FieldHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct FieldSignature
    {
        internal MetadataReader _reader;
        internal FieldSignatureHandle _handle;

        public FieldSignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public Handle Type
        {
            get
            {
                return _type;
            }
        } // Type

        internal Handle _type;
    } // FieldSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct FieldSignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is FieldSignatureHandle)
                return _value == ((FieldSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(FieldSignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal FieldSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal FieldSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.FieldSignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.FieldSignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(FieldSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public FieldSignature GetFieldSignature(MetadataReader reader)
        {
            return reader.GetFieldSignature(this);
        } // GetFieldSignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.FieldSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // FieldSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct FunctionPointerSignature
    {
        internal MetadataReader _reader;
        internal FunctionPointerSignatureHandle _handle;

        public FunctionPointerSignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public MethodSignatureHandle Signature
        {
            get
            {
                return _signature;
            }
        } // Signature

        internal MethodSignatureHandle _signature;
    } // FunctionPointerSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct FunctionPointerSignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is FunctionPointerSignatureHandle)
                return _value == ((FunctionPointerSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(FunctionPointerSignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal FunctionPointerSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal FunctionPointerSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.FunctionPointerSignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.FunctionPointerSignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(FunctionPointerSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public FunctionPointerSignature GetFunctionPointerSignature(MetadataReader reader)
        {
            return reader.GetFunctionPointerSignature(this);
        } // GetFunctionPointerSignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.FunctionPointerSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // FunctionPointerSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct GenericParameter
    {
        internal MetadataReader _reader;
        internal GenericParameterHandle _handle;

        public GenericParameterHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public ushort Number
        {
            get
            {
                return _number;
            }
        } // Number

        internal ushort _number;

        public GenericParameterAttributes Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal GenericParameterAttributes _flags;

        public GenericParameterKind Kind
        {
            get
            {
                return _kind;
            }
        } // Kind

        internal GenericParameterKind _kind;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public HandleCollection Constraints
        {
            get
            {
                return _constraints;
            }
        } // Constraints

        internal HandleCollection _constraints;

        public CustomAttributeHandleCollection CustomAttributes
        {
            get
            {
                return _customAttributes;
            }
        } // CustomAttributes

        internal CustomAttributeHandleCollection _customAttributes;
    } // GenericParameter

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct GenericParameterHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is GenericParameterHandle)
                return _value == ((GenericParameterHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(GenericParameterHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal GenericParameterHandle(Handle handle) : this(handle._value)
        {
        }

        internal GenericParameterHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.GenericParameter || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.GenericParameter) << 24);
            _Validate();
        }

        public static implicit operator  Handle(GenericParameterHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public GenericParameter GetGenericParameter(MetadataReader reader)
        {
            return reader.GetGenericParameter(this);
        } // GetGenericParameter

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.GenericParameter)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // GenericParameterHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MemberReference
    {
        internal MetadataReader _reader;
        internal MemberReferenceHandle _handle;

        public MemberReferenceHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle Parent
        {
            get
            {
                return _parent;
            }
        } // Parent

        internal Handle _parent;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;
        /// One of: MethodSignature, FieldSignature

        public Handle Signature
        {
            get
            {
                return _signature;
            }
        } // Signature

        internal Handle _signature;
    } // MemberReference

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MemberReferenceHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is MemberReferenceHandle)
                return _value == ((MemberReferenceHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MemberReferenceHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal MemberReferenceHandle(Handle handle) : this(handle._value)
        {
        }

        internal MemberReferenceHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.MemberReference || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MemberReference) << 24);
            _Validate();
        }

        public static implicit operator  Handle(MemberReferenceHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public MemberReference GetMemberReference(MetadataReader reader)
        {
            return reader.GetMemberReference(this);
        } // GetMemberReference

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MemberReference)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // MemberReferenceHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct Method
    {
        internal MetadataReader _reader;
        internal MethodHandle _handle;

        public MethodHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public MethodAttributes Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal MethodAttributes _flags;

        public MethodImplAttributes ImplFlags
        {
            get
            {
                return _implFlags;
            }
        } // ImplFlags

        internal MethodImplAttributes _implFlags;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;

        public MethodSignatureHandle Signature
        {
            get
            {
                return _signature;
            }
        } // Signature

        internal MethodSignatureHandle _signature;

        public ParameterHandleCollection Parameters
        {
            get
            {
                return _parameters;
            }
        } // Parameters

        internal ParameterHandleCollection _parameters;

        public GenericParameterHandleCollection GenericParameters
        {
            get
            {
                return _genericParameters;
            }
        } // GenericParameters

        internal GenericParameterHandleCollection _genericParameters;

        public CustomAttributeHandleCollection CustomAttributes
        {
            get
            {
                return _customAttributes;
            }
        } // CustomAttributes

        internal CustomAttributeHandleCollection _customAttributes;
    } // Method

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is MethodHandle)
                return _value == ((MethodHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal MethodHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.Method || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Method) << 24);
            _Validate();
        }

        public static implicit operator  Handle(MethodHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public Method GetMethod(MetadataReader reader)
        {
            return reader.GetMethod(this);
        } // GetMethod

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Method)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // MethodHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodInstantiation
    {
        internal MetadataReader _reader;
        internal MethodInstantiationHandle _handle;

        public MethodInstantiationHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: QualifiedMethod, MemberReference

        public Handle Method
        {
            get
            {
                return _method;
            }
        } // Method

        internal Handle _method;
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public HandleCollection GenericTypeArguments
        {
            get
            {
                return _genericTypeArguments;
            }
        } // GenericTypeArguments

        internal HandleCollection _genericTypeArguments;
    } // MethodInstantiation

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodInstantiationHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is MethodInstantiationHandle)
                return _value == ((MethodInstantiationHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodInstantiationHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal MethodInstantiationHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodInstantiationHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.MethodInstantiation || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MethodInstantiation) << 24);
            _Validate();
        }

        public static implicit operator  Handle(MethodInstantiationHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public MethodInstantiation GetMethodInstantiation(MetadataReader reader)
        {
            return reader.GetMethodInstantiation(this);
        } // GetMethodInstantiation

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MethodInstantiation)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // MethodInstantiationHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodSemantics
    {
        internal MetadataReader _reader;
        internal MethodSemanticsHandle _handle;

        public MethodSemanticsHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public MethodSemanticsAttributes Attributes
        {
            get
            {
                return _attributes;
            }
        } // Attributes

        internal MethodSemanticsAttributes _attributes;

        public MethodHandle Method
        {
            get
            {
                return _method;
            }
        } // Method

        internal MethodHandle _method;
    } // MethodSemantics

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodSemanticsHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is MethodSemanticsHandle)
                return _value == ((MethodSemanticsHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodSemanticsHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal MethodSemanticsHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodSemanticsHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.MethodSemantics || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MethodSemantics) << 24);
            _Validate();
        }

        public static implicit operator  Handle(MethodSemanticsHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public MethodSemantics GetMethodSemantics(MetadataReader reader)
        {
            return reader.GetMethodSemantics(this);
        } // GetMethodSemantics

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MethodSemantics)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // MethodSemanticsHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodSignature
    {
        internal MetadataReader _reader;
        internal MethodSignatureHandle _handle;

        public MethodSignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public CallingConventions CallingConvention
        {
            get
            {
                return _callingConvention;
            }
        } // CallingConvention

        internal CallingConventions _callingConvention;

        public int GenericParameterCount
        {
            get
            {
                return _genericParameterCount;
            }
        } // GenericParameterCount

        internal int _genericParameterCount;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public Handle ReturnType
        {
            get
            {
                return _returnType;
            }
        } // ReturnType

        internal Handle _returnType;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public HandleCollection Parameters
        {
            get
            {
                return _parameters;
            }
        } // Parameters

        internal HandleCollection _parameters;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public HandleCollection VarArgParameters
        {
            get
            {
                return _varArgParameters;
            }
        } // VarArgParameters

        internal HandleCollection _varArgParameters;
    } // MethodSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodSignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is MethodSignatureHandle)
                return _value == ((MethodSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodSignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal MethodSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.MethodSignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MethodSignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(MethodSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public MethodSignature GetMethodSignature(MetadataReader reader)
        {
            return reader.GetMethodSignature(this);
        } // GetMethodSignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MethodSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // MethodSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodTypeVariableSignature
    {
        internal MetadataReader _reader;
        internal MethodTypeVariableSignatureHandle _handle;

        public MethodTypeVariableSignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public int Number
        {
            get
            {
                return _number;
            }
        } // Number

        internal int _number;
    } // MethodTypeVariableSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct MethodTypeVariableSignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is MethodTypeVariableSignatureHandle)
                return _value == ((MethodTypeVariableSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(MethodTypeVariableSignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal MethodTypeVariableSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal MethodTypeVariableSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.MethodTypeVariableSignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.MethodTypeVariableSignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(MethodTypeVariableSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public MethodTypeVariableSignature GetMethodTypeVariableSignature(MetadataReader reader)
        {
            return reader.GetMethodTypeVariableSignature(this);
        } // GetMethodTypeVariableSignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.MethodTypeVariableSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // MethodTypeVariableSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ModifiedType
    {
        internal MetadataReader _reader;
        internal ModifiedTypeHandle _handle;

        public ModifiedTypeHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public bool IsOptional
        {
            get
            {
                return _isOptional;
            }
        } // IsOptional

        internal bool _isOptional;
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle ModifierType
        {
            get
            {
                return _modifierType;
            }
        } // ModifierType

        internal Handle _modifierType;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public Handle Type
        {
            get
            {
                return _type;
            }
        } // Type

        internal Handle _type;
    } // ModifiedType

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ModifiedTypeHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ModifiedTypeHandle)
                return _value == ((ModifiedTypeHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ModifiedTypeHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ModifiedTypeHandle(Handle handle) : this(handle._value)
        {
        }

        internal ModifiedTypeHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ModifiedType || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ModifiedType) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ModifiedTypeHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ModifiedType GetModifiedType(MetadataReader reader)
        {
            return reader.GetModifiedType(this);
        } // GetModifiedType

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ModifiedType)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ModifiedTypeHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct NamedArgument
    {
        internal MetadataReader _reader;
        internal NamedArgumentHandle _handle;

        public NamedArgumentHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public NamedArgumentMemberKind Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal NamedArgumentMemberKind _flags;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle Type
        {
            get
            {
                return _type;
            }
        } // Type

        internal Handle _type;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value

        public Handle Value
        {
            get
            {
                return _value;
            }
        } // Value

        internal Handle _value;
    } // NamedArgument

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct NamedArgumentHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is NamedArgumentHandle)
                return _value == ((NamedArgumentHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(NamedArgumentHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal NamedArgumentHandle(Handle handle) : this(handle._value)
        {
        }

        internal NamedArgumentHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.NamedArgument || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.NamedArgument) << 24);
            _Validate();
        }

        public static implicit operator  Handle(NamedArgumentHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public NamedArgument GetNamedArgument(MetadataReader reader)
        {
            return reader.GetNamedArgument(this);
        } // GetNamedArgument

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.NamedArgument)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // NamedArgumentHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct NamespaceDefinition
    {
        internal MetadataReader _reader;
        internal NamespaceDefinitionHandle _handle;

        public NamespaceDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: NamespaceDefinition, ScopeDefinition

        public Handle ParentScopeOrNamespace
        {
            get
            {
                return _parentScopeOrNamespace;
            }
        } // ParentScopeOrNamespace

        internal Handle _parentScopeOrNamespace;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;

        public TypeDefinitionHandleCollection TypeDefinitions
        {
            get
            {
                return _typeDefinitions;
            }
        } // TypeDefinitions

        internal TypeDefinitionHandleCollection _typeDefinitions;

        public TypeForwarderHandleCollection TypeForwarders
        {
            get
            {
                return _typeForwarders;
            }
        } // TypeForwarders

        internal TypeForwarderHandleCollection _typeForwarders;

        public NamespaceDefinitionHandleCollection NamespaceDefinitions
        {
            get
            {
                return _namespaceDefinitions;
            }
        } // NamespaceDefinitions

        internal NamespaceDefinitionHandleCollection _namespaceDefinitions;
    } // NamespaceDefinition

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct NamespaceDefinitionHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is NamespaceDefinitionHandle)
                return _value == ((NamespaceDefinitionHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(NamespaceDefinitionHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal NamespaceDefinitionHandle(Handle handle) : this(handle._value)
        {
        }

        internal NamespaceDefinitionHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.NamespaceDefinition || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.NamespaceDefinition) << 24);
            _Validate();
        }

        public static implicit operator  Handle(NamespaceDefinitionHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public NamespaceDefinition GetNamespaceDefinition(MetadataReader reader)
        {
            return reader.GetNamespaceDefinition(this);
        } // GetNamespaceDefinition

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.NamespaceDefinition)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // NamespaceDefinitionHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct NamespaceReference
    {
        internal MetadataReader _reader;
        internal NamespaceReferenceHandle _handle;

        public NamespaceReferenceHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: NamespaceReference, ScopeReference

        public Handle ParentScopeOrNamespace
        {
            get
            {
                return _parentScopeOrNamespace;
            }
        } // ParentScopeOrNamespace

        internal Handle _parentScopeOrNamespace;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;
    } // NamespaceReference

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct NamespaceReferenceHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is NamespaceReferenceHandle)
                return _value == ((NamespaceReferenceHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(NamespaceReferenceHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal NamespaceReferenceHandle(Handle handle) : this(handle._value)
        {
        }

        internal NamespaceReferenceHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.NamespaceReference || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.NamespaceReference) << 24);
            _Validate();
        }

        public static implicit operator  Handle(NamespaceReferenceHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public NamespaceReference GetNamespaceReference(MetadataReader reader)
        {
            return reader.GetNamespaceReference(this);
        } // GetNamespaceReference

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.NamespaceReference)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // NamespaceReferenceHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct Parameter
    {
        internal MetadataReader _reader;
        internal ParameterHandle _handle;

        public ParameterHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public ParameterAttributes Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal ParameterAttributes _flags;

        public ushort Sequence
        {
            get
            {
                return _sequence;
            }
        } // Sequence

        internal ushort _sequence;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value

        public Handle DefaultValue
        {
            get
            {
                return _defaultValue;
            }
        } // DefaultValue

        internal Handle _defaultValue;

        public CustomAttributeHandleCollection CustomAttributes
        {
            get
            {
                return _customAttributes;
            }
        } // CustomAttributes

        internal CustomAttributeHandleCollection _customAttributes;
    } // Parameter

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ParameterHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ParameterHandle)
                return _value == ((ParameterHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ParameterHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ParameterHandle(Handle handle) : this(handle._value)
        {
        }

        internal ParameterHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.Parameter || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Parameter) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ParameterHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public Parameter GetParameter(MetadataReader reader)
        {
            return reader.GetParameter(this);
        } // GetParameter

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Parameter)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ParameterHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct PointerSignature
    {
        internal MetadataReader _reader;
        internal PointerSignatureHandle _handle;

        public PointerSignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public Handle Type
        {
            get
            {
                return _type;
            }
        } // Type

        internal Handle _type;
    } // PointerSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct PointerSignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is PointerSignatureHandle)
                return _value == ((PointerSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(PointerSignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal PointerSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal PointerSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.PointerSignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.PointerSignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(PointerSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public PointerSignature GetPointerSignature(MetadataReader reader)
        {
            return reader.GetPointerSignature(this);
        } // GetPointerSignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.PointerSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // PointerSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct Property
    {
        internal MetadataReader _reader;
        internal PropertyHandle _handle;

        public PropertyHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public PropertyAttributes Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal PropertyAttributes _flags;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;

        public PropertySignatureHandle Signature
        {
            get
            {
                return _signature;
            }
        } // Signature

        internal PropertySignatureHandle _signature;

        public MethodSemanticsHandleCollection MethodSemantics
        {
            get
            {
                return _methodSemantics;
            }
        } // MethodSemantics

        internal MethodSemanticsHandleCollection _methodSemantics;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ConstantBooleanArray, ConstantBooleanValue, ConstantByteArray, ConstantByteValue, ConstantCharArray, ConstantCharValue, ConstantDoubleArray, ConstantDoubleValue, ConstantEnumArray, ConstantHandleArray, ConstantInt16Array, ConstantInt16Value, ConstantInt32Array, ConstantInt32Value, ConstantInt64Array, ConstantInt64Value, ConstantReferenceValue, ConstantSByteArray, ConstantSByteValue, ConstantSingleArray, ConstantSingleValue, ConstantStringArray, ConstantStringValue, ConstantUInt16Array, ConstantUInt16Value, ConstantUInt32Array, ConstantUInt32Value, ConstantUInt64Array, ConstantUInt64Value

        public Handle DefaultValue
        {
            get
            {
                return _defaultValue;
            }
        } // DefaultValue

        internal Handle _defaultValue;

        public CustomAttributeHandleCollection CustomAttributes
        {
            get
            {
                return _customAttributes;
            }
        } // CustomAttributes

        internal CustomAttributeHandleCollection _customAttributes;
    } // Property

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct PropertyHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is PropertyHandle)
                return _value == ((PropertyHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(PropertyHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal PropertyHandle(Handle handle) : this(handle._value)
        {
        }

        internal PropertyHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.Property || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.Property) << 24);
            _Validate();
        }

        public static implicit operator  Handle(PropertyHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public Property GetProperty(MetadataReader reader)
        {
            return reader.GetProperty(this);
        } // GetProperty

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.Property)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // PropertyHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct PropertySignature
    {
        internal MetadataReader _reader;
        internal PropertySignatureHandle _handle;

        public PropertySignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public CallingConventions CallingConvention
        {
            get
            {
                return _callingConvention;
            }
        } // CallingConvention

        internal CallingConventions _callingConvention;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public Handle Type
        {
            get
            {
                return _type;
            }
        } // Type

        internal Handle _type;
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public HandleCollection Parameters
        {
            get
            {
                return _parameters;
            }
        } // Parameters

        internal HandleCollection _parameters;
    } // PropertySignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct PropertySignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is PropertySignatureHandle)
                return _value == ((PropertySignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(PropertySignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal PropertySignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal PropertySignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.PropertySignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.PropertySignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(PropertySignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public PropertySignature GetPropertySignature(MetadataReader reader)
        {
            return reader.GetPropertySignature(this);
        } // GetPropertySignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.PropertySignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // PropertySignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct QualifiedField
    {
        internal MetadataReader _reader;
        internal QualifiedFieldHandle _handle;

        public QualifiedFieldHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public FieldHandle Field
        {
            get
            {
                return _field;
            }
        } // Field

        internal FieldHandle _field;

        public TypeDefinitionHandle EnclosingType
        {
            get
            {
                return _enclosingType;
            }
        } // EnclosingType

        internal TypeDefinitionHandle _enclosingType;
    } // QualifiedField

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct QualifiedFieldHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is QualifiedFieldHandle)
                return _value == ((QualifiedFieldHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(QualifiedFieldHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal QualifiedFieldHandle(Handle handle) : this(handle._value)
        {
        }

        internal QualifiedFieldHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.QualifiedField || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.QualifiedField) << 24);
            _Validate();
        }

        public static implicit operator  Handle(QualifiedFieldHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public QualifiedField GetQualifiedField(MetadataReader reader)
        {
            return reader.GetQualifiedField(this);
        } // GetQualifiedField

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.QualifiedField)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // QualifiedFieldHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct QualifiedMethod
    {
        internal MetadataReader _reader;
        internal QualifiedMethodHandle _handle;

        public QualifiedMethodHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public MethodHandle Method
        {
            get
            {
                return _method;
            }
        } // Method

        internal MethodHandle _method;

        public TypeDefinitionHandle EnclosingType
        {
            get
            {
                return _enclosingType;
            }
        } // EnclosingType

        internal TypeDefinitionHandle _enclosingType;
    } // QualifiedMethod

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct QualifiedMethodHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is QualifiedMethodHandle)
                return _value == ((QualifiedMethodHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(QualifiedMethodHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal QualifiedMethodHandle(Handle handle) : this(handle._value)
        {
        }

        internal QualifiedMethodHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.QualifiedMethod || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.QualifiedMethod) << 24);
            _Validate();
        }

        public static implicit operator  Handle(QualifiedMethodHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public QualifiedMethod GetQualifiedMethod(MetadataReader reader)
        {
            return reader.GetQualifiedMethod(this);
        } // GetQualifiedMethod

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.QualifiedMethod)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // QualifiedMethodHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct SZArraySignature
    {
        internal MetadataReader _reader;
        internal SZArraySignatureHandle _handle;

        public SZArraySignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: TypeDefinition, TypeReference, TypeSpecification, ModifiedType

        public Handle ElementType
        {
            get
            {
                return _elementType;
            }
        } // ElementType

        internal Handle _elementType;
    } // SZArraySignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct SZArraySignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is SZArraySignatureHandle)
                return _value == ((SZArraySignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(SZArraySignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal SZArraySignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal SZArraySignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.SZArraySignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.SZArraySignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(SZArraySignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public SZArraySignature GetSZArraySignature(MetadataReader reader)
        {
            return reader.GetSZArraySignature(this);
        } // GetSZArraySignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.SZArraySignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // SZArraySignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ScopeDefinition
    {
        internal MetadataReader _reader;
        internal ScopeDefinitionHandle _handle;

        public ScopeDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public AssemblyFlags Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal AssemblyFlags _flags;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;

        public AssemblyHashAlgorithm HashAlgorithm
        {
            get
            {
                return _hashAlgorithm;
            }
        } // HashAlgorithm

        internal AssemblyHashAlgorithm _hashAlgorithm;

        public ushort MajorVersion
        {
            get
            {
                return _majorVersion;
            }
        } // MajorVersion

        internal ushort _majorVersion;

        public ushort MinorVersion
        {
            get
            {
                return _minorVersion;
            }
        } // MinorVersion

        internal ushort _minorVersion;

        public ushort BuildNumber
        {
            get
            {
                return _buildNumber;
            }
        } // BuildNumber

        internal ushort _buildNumber;

        public ushort RevisionNumber
        {
            get
            {
                return _revisionNumber;
            }
        } // RevisionNumber

        internal ushort _revisionNumber;

        public ByteCollection PublicKey
        {
            get
            {
                return _publicKey;
            }
        } // PublicKey

        internal ByteCollection _publicKey;

        public ConstantStringValueHandle Culture
        {
            get
            {
                return _culture;
            }
        } // Culture

        internal ConstantStringValueHandle _culture;

        public NamespaceDefinitionHandle RootNamespaceDefinition
        {
            get
            {
                return _rootNamespaceDefinition;
            }
        } // RootNamespaceDefinition

        internal NamespaceDefinitionHandle _rootNamespaceDefinition;

        public QualifiedMethodHandle EntryPoint
        {
            get
            {
                return _entryPoint;
            }
        } // EntryPoint

        internal QualifiedMethodHandle _entryPoint;

        public TypeDefinitionHandle GlobalModuleType
        {
            get
            {
                return _globalModuleType;
            }
        } // GlobalModuleType

        internal TypeDefinitionHandle _globalModuleType;

        public CustomAttributeHandleCollection CustomAttributes
        {
            get
            {
                return _customAttributes;
            }
        } // CustomAttributes

        internal CustomAttributeHandleCollection _customAttributes;

        public ConstantStringValueHandle ModuleName
        {
            get
            {
                return _moduleName;
            }
        } // ModuleName

        internal ConstantStringValueHandle _moduleName;

        public ByteCollection Mvid
        {
            get
            {
                return _mvid;
            }
        } // Mvid

        internal ByteCollection _mvid;

        public CustomAttributeHandleCollection ModuleCustomAttributes
        {
            get
            {
                return _moduleCustomAttributes;
            }
        } // ModuleCustomAttributes

        internal CustomAttributeHandleCollection _moduleCustomAttributes;
    } // ScopeDefinition

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ScopeDefinitionHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ScopeDefinitionHandle)
                return _value == ((ScopeDefinitionHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ScopeDefinitionHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ScopeDefinitionHandle(Handle handle) : this(handle._value)
        {
        }

        internal ScopeDefinitionHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ScopeDefinition || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ScopeDefinition) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ScopeDefinitionHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ScopeDefinition GetScopeDefinition(MetadataReader reader)
        {
            return reader.GetScopeDefinition(this);
        } // GetScopeDefinition

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ScopeDefinition)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ScopeDefinitionHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ScopeReference
    {
        internal MetadataReader _reader;
        internal ScopeReferenceHandle _handle;

        public ScopeReferenceHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public AssemblyFlags Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal AssemblyFlags _flags;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;

        public ushort MajorVersion
        {
            get
            {
                return _majorVersion;
            }
        } // MajorVersion

        internal ushort _majorVersion;

        public ushort MinorVersion
        {
            get
            {
                return _minorVersion;
            }
        } // MinorVersion

        internal ushort _minorVersion;

        public ushort BuildNumber
        {
            get
            {
                return _buildNumber;
            }
        } // BuildNumber

        internal ushort _buildNumber;

        public ushort RevisionNumber
        {
            get
            {
                return _revisionNumber;
            }
        } // RevisionNumber

        internal ushort _revisionNumber;

        public ByteCollection PublicKeyOrToken
        {
            get
            {
                return _publicKeyOrToken;
            }
        } // PublicKeyOrToken

        internal ByteCollection _publicKeyOrToken;

        public ConstantStringValueHandle Culture
        {
            get
            {
                return _culture;
            }
        } // Culture

        internal ConstantStringValueHandle _culture;
    } // ScopeReference

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct ScopeReferenceHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is ScopeReferenceHandle)
                return _value == ((ScopeReferenceHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(ScopeReferenceHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal ScopeReferenceHandle(Handle handle) : this(handle._value)
        {
        }

        internal ScopeReferenceHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.ScopeReference || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.ScopeReference) << 24);
            _Validate();
        }

        public static implicit operator  Handle(ScopeReferenceHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public ScopeReference GetScopeReference(MetadataReader reader)
        {
            return reader.GetScopeReference(this);
        } // GetScopeReference

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.ScopeReference)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // ScopeReferenceHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeDefinition
    {
        internal MetadataReader _reader;
        internal TypeDefinitionHandle _handle;

        public TypeDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public TypeAttributes Flags
        {
            get
            {
                return _flags;
            }
        } // Flags

        internal TypeAttributes _flags;
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle BaseType
        {
            get
            {
                return _baseType;
            }
        } // BaseType

        internal Handle _baseType;

        public NamespaceDefinitionHandle NamespaceDefinition
        {
            get
            {
                return _namespaceDefinition;
            }
        } // NamespaceDefinition

        internal NamespaceDefinitionHandle _namespaceDefinition;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;

        public uint Size
        {
            get
            {
                return _size;
            }
        } // Size

        internal uint _size;

        public ushort PackingSize
        {
            get
            {
                return _packingSize;
            }
        } // PackingSize

        internal ushort _packingSize;

        public TypeDefinitionHandle EnclosingType
        {
            get
            {
                return _enclosingType;
            }
        } // EnclosingType

        internal TypeDefinitionHandle _enclosingType;

        public TypeDefinitionHandleCollection NestedTypes
        {
            get
            {
                return _nestedTypes;
            }
        } // NestedTypes

        internal TypeDefinitionHandleCollection _nestedTypes;

        public MethodHandleCollection Methods
        {
            get
            {
                return _methods;
            }
        } // Methods

        internal MethodHandleCollection _methods;

        public FieldHandleCollection Fields
        {
            get
            {
                return _fields;
            }
        } // Fields

        internal FieldHandleCollection _fields;

        public PropertyHandleCollection Properties
        {
            get
            {
                return _properties;
            }
        } // Properties

        internal PropertyHandleCollection _properties;

        public EventHandleCollection Events
        {
            get
            {
                return _events;
            }
        } // Events

        internal EventHandleCollection _events;

        public GenericParameterHandleCollection GenericParameters
        {
            get
            {
                return _genericParameters;
            }
        } // GenericParameters

        internal GenericParameterHandleCollection _genericParameters;
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public HandleCollection Interfaces
        {
            get
            {
                return _interfaces;
            }
        } // Interfaces

        internal HandleCollection _interfaces;

        public CustomAttributeHandleCollection CustomAttributes
        {
            get
            {
                return _customAttributes;
            }
        } // CustomAttributes

        internal CustomAttributeHandleCollection _customAttributes;
    } // TypeDefinition

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeDefinitionHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is TypeDefinitionHandle)
                return _value == ((TypeDefinitionHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeDefinitionHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal TypeDefinitionHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeDefinitionHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.TypeDefinition || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeDefinition) << 24);
            _Validate();
        }

        public static implicit operator  Handle(TypeDefinitionHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public TypeDefinition GetTypeDefinition(MetadataReader reader)
        {
            return reader.GetTypeDefinition(this);
        } // GetTypeDefinition

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeDefinition)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // TypeDefinitionHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeForwarder
    {
        internal MetadataReader _reader;
        internal TypeForwarderHandle _handle;

        public TypeForwarderHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public ScopeReferenceHandle Scope
        {
            get
            {
                return _scope;
            }
        } // Scope

        internal ScopeReferenceHandle _scope;

        public ConstantStringValueHandle Name
        {
            get
            {
                return _name;
            }
        } // Name

        internal ConstantStringValueHandle _name;

        public TypeForwarderHandleCollection NestedTypes
        {
            get
            {
                return _nestedTypes;
            }
        } // NestedTypes

        internal TypeForwarderHandleCollection _nestedTypes;
    } // TypeForwarder

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeForwarderHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is TypeForwarderHandle)
                return _value == ((TypeForwarderHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeForwarderHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal TypeForwarderHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeForwarderHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.TypeForwarder || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeForwarder) << 24);
            _Validate();
        }

        public static implicit operator  Handle(TypeForwarderHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public TypeForwarder GetTypeForwarder(MetadataReader reader)
        {
            return reader.GetTypeForwarder(this);
        } // GetTypeForwarder

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeForwarder)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // TypeForwarderHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeInstantiationSignature
    {
        internal MetadataReader _reader;
        internal TypeInstantiationSignatureHandle _handle;

        public TypeInstantiationSignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public Handle GenericType
        {
            get
            {
                return _genericType;
            }
        } // GenericType

        internal Handle _genericType;
        /// One of: TypeDefinition, TypeReference, TypeSpecification

        public HandleCollection GenericTypeArguments
        {
            get
            {
                return _genericTypeArguments;
            }
        } // GenericTypeArguments

        internal HandleCollection _genericTypeArguments;
    } // TypeInstantiationSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeInstantiationSignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is TypeInstantiationSignatureHandle)
                return _value == ((TypeInstantiationSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeInstantiationSignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal TypeInstantiationSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeInstantiationSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.TypeInstantiationSignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeInstantiationSignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(TypeInstantiationSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public TypeInstantiationSignature GetTypeInstantiationSignature(MetadataReader reader)
        {
            return reader.GetTypeInstantiationSignature(this);
        } // GetTypeInstantiationSignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeInstantiationSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // TypeInstantiationSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeReference
    {
        internal MetadataReader _reader;
        internal TypeReferenceHandle _handle;

        public TypeReferenceHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: NamespaceReference, TypeReference

        public Handle ParentNamespaceOrType
        {
            get
            {
                return _parentNamespaceOrType;
            }
        } // ParentNamespaceOrType

        internal Handle _parentNamespaceOrType;

        public ConstantStringValueHandle TypeName
        {
            get
            {
                return _typeName;
            }
        } // TypeName

        internal ConstantStringValueHandle _typeName;
    } // TypeReference

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeReferenceHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is TypeReferenceHandle)
                return _value == ((TypeReferenceHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeReferenceHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal TypeReferenceHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeReferenceHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.TypeReference || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeReference) << 24);
            _Validate();
        }

        public static implicit operator  Handle(TypeReferenceHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public TypeReference GetTypeReference(MetadataReader reader)
        {
            return reader.GetTypeReference(this);
        } // GetTypeReference

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeReference)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // TypeReferenceHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeSpecification
    {
        internal MetadataReader _reader;
        internal TypeSpecificationHandle _handle;

        public TypeSpecificationHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle
        /// One of: TypeDefinition, TypeReference, TypeInstantiationSignature, SZArraySignature, ArraySignature, PointerSignature, FunctionPointerSignature, ByReferenceSignature, TypeVariableSignature, MethodTypeVariableSignature

        public Handle Signature
        {
            get
            {
                return _signature;
            }
        } // Signature

        internal Handle _signature;
    } // TypeSpecification

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeSpecificationHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is TypeSpecificationHandle)
                return _value == ((TypeSpecificationHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeSpecificationHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal TypeSpecificationHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeSpecificationHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.TypeSpecification || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeSpecification) << 24);
            _Validate();
        }

        public static implicit operator  Handle(TypeSpecificationHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public TypeSpecification GetTypeSpecification(MetadataReader reader)
        {
            return reader.GetTypeSpecification(this);
        } // GetTypeSpecification

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeSpecification)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // TypeSpecificationHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeVariableSignature
    {
        internal MetadataReader _reader;
        internal TypeVariableSignatureHandle _handle;

        public TypeVariableSignatureHandle Handle
        {
            get
            {
                return _handle;
            }
        } // Handle

        public int Number
        {
            get
            {
                return _number;
            }
        } // Number

        internal int _number;
    } // TypeVariableSignature

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct TypeVariableSignatureHandle
    {
        public override bool Equals(object obj)
        {
            if (obj is TypeVariableSignatureHandle)
                return _value == ((TypeVariableSignatureHandle)obj)._value;
            else if (obj is Handle)
                return _value == ((Handle)obj)._value;
            else
                return false;
        } // Equals

        public bool Equals(TypeVariableSignatureHandle handle)
        {
            return _value == handle._value;
        } // Equals

        public bool Equals(Handle handle)
        {
            return _value == handle._value;
        } // Equals

        public override int GetHashCode()
        {
            return (int)_value;
        } // GetHashCode

        internal int _value;

        internal TypeVariableSignatureHandle(Handle handle) : this(handle._value)
        {
        }

        internal TypeVariableSignatureHandle(int value)
        {
            HandleType hType = (HandleType)(value >> 24);
            if (!(hType == 0 || hType == HandleType.TypeVariableSignature || hType == HandleType.Null))
                throw new ArgumentException();
            _value = (value & 0x00FFFFFF) | (((int)HandleType.TypeVariableSignature) << 24);
            _Validate();
        }

        public static implicit operator  Handle(TypeVariableSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // Handle

        internal int Offset
        {
            get
            {
                return (this._value & 0x00FFFFFF);
            }
        } // Offset

        public TypeVariableSignature GetTypeVariableSignature(MetadataReader reader)
        {
            return reader.GetTypeVariableSignature(this);
        } // GetTypeVariableSignature

        public bool IsNull(MetadataReader reader)
        {
            return reader.IsNull(this);
        } // IsNull

        public Handle ToHandle(MetadataReader reader)
        {
            return reader.ToHandle(this);
        } // ToHandle

        [System.Diagnostics.Conditional("DEBUG")]
        internal void _Validate()
        {
            if ((HandleType)((_value & 0xFF000000) >> 24) != HandleType.TypeVariableSignature)
                throw new ArgumentException();
        } // _Validate

        public override string ToString()
        {
            return string.Format("{0:X8}", _value);
        } // ToString
    } // TypeVariableSignatureHandle

#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
    [ReflectionBlocked]
#endif
    public partial struct NamedArgumentHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct MethodSemanticsHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct CustomAttributeHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct ParameterHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct GenericParameterHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct TypeDefinitionHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct TypeForwarderHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct NamespaceDefinitionHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct MethodHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct FieldHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct PropertyHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct EventHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct ScopeDefinitionHandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct BooleanCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct CharCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct ByteCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct SByteCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct Int16Collection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct UInt16Collection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct Int32Collection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct UInt32Collection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct Int64Collection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct UInt64Collection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct SingleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct DoubleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial struct Handle
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

        public ConstantBoxedEnumValueHandle ToConstantBoxedEnumValueHandle(MetadataReader reader)
        {
            return new ConstantBoxedEnumValueHandle(this);
        } // ToConstantBoxedEnumValueHandle

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
    [ReflectionBlocked]
#endif
    public partial struct HandleCollection
    {
        private NativeReader _reader;
        private uint _offset;

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
        [ReflectionBlocked]
#endif
        public struct Enumerator
        {
            private NativeReader _reader;
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
    [ReflectionBlocked]
#endif
    public partial class MetadataReader
    {
        public ArraySignature GetArraySignature(ArraySignatureHandle handle)
        {
            ArraySignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._elementType);
            offset = _streamReader.Read(offset, out record._rank);
            offset = _streamReader.Read(offset, out record._sizes);
            offset = _streamReader.Read(offset, out record._lowerBounds);
            return record;
        } // GetArraySignature

        public ByReferenceSignature GetByReferenceSignature(ByReferenceSignatureHandle handle)
        {
            ByReferenceSignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._type);
            return record;
        } // GetByReferenceSignature

        public ConstantBooleanArray GetConstantBooleanArray(ConstantBooleanArrayHandle handle)
        {
            ConstantBooleanArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantBooleanArray

        public ConstantBooleanValue GetConstantBooleanValue(ConstantBooleanValueHandle handle)
        {
            ConstantBooleanValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantBooleanValue

        public ConstantBoxedEnumValue GetConstantBoxedEnumValue(ConstantBoxedEnumValueHandle handle)
        {
            ConstantBoxedEnumValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            offset = _streamReader.Read(offset, out record._type);
            return record;
        } // GetConstantBoxedEnumValue

        public ConstantByteArray GetConstantByteArray(ConstantByteArrayHandle handle)
        {
            ConstantByteArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantByteArray

        public ConstantByteValue GetConstantByteValue(ConstantByteValueHandle handle)
        {
            ConstantByteValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantByteValue

        public ConstantCharArray GetConstantCharArray(ConstantCharArrayHandle handle)
        {
            ConstantCharArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantCharArray

        public ConstantCharValue GetConstantCharValue(ConstantCharValueHandle handle)
        {
            ConstantCharValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantCharValue

        public ConstantDoubleArray GetConstantDoubleArray(ConstantDoubleArrayHandle handle)
        {
            ConstantDoubleArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantDoubleArray

        public ConstantDoubleValue GetConstantDoubleValue(ConstantDoubleValueHandle handle)
        {
            ConstantDoubleValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantDoubleValue

        public ConstantEnumArray GetConstantEnumArray(ConstantEnumArrayHandle handle)
        {
            ConstantEnumArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._elementType);
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantEnumArray

        public ConstantHandleArray GetConstantHandleArray(ConstantHandleArrayHandle handle)
        {
            ConstantHandleArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantHandleArray

        public ConstantInt16Array GetConstantInt16Array(ConstantInt16ArrayHandle handle)
        {
            ConstantInt16Array record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantInt16Array

        public ConstantInt16Value GetConstantInt16Value(ConstantInt16ValueHandle handle)
        {
            ConstantInt16Value record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantInt16Value

        public ConstantInt32Array GetConstantInt32Array(ConstantInt32ArrayHandle handle)
        {
            ConstantInt32Array record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantInt32Array

        public ConstantInt32Value GetConstantInt32Value(ConstantInt32ValueHandle handle)
        {
            ConstantInt32Value record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantInt32Value

        public ConstantInt64Array GetConstantInt64Array(ConstantInt64ArrayHandle handle)
        {
            ConstantInt64Array record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantInt64Array

        public ConstantInt64Value GetConstantInt64Value(ConstantInt64ValueHandle handle)
        {
            ConstantInt64Value record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantInt64Value

        public ConstantReferenceValue GetConstantReferenceValue(ConstantReferenceValueHandle handle)
        {
            ConstantReferenceValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            return record;
        } // GetConstantReferenceValue

        public ConstantSByteArray GetConstantSByteArray(ConstantSByteArrayHandle handle)
        {
            ConstantSByteArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantSByteArray

        public ConstantSByteValue GetConstantSByteValue(ConstantSByteValueHandle handle)
        {
            ConstantSByteValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantSByteValue

        public ConstantSingleArray GetConstantSingleArray(ConstantSingleArrayHandle handle)
        {
            ConstantSingleArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantSingleArray

        public ConstantSingleValue GetConstantSingleValue(ConstantSingleValueHandle handle)
        {
            ConstantSingleValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantSingleValue

        public ConstantStringArray GetConstantStringArray(ConstantStringArrayHandle handle)
        {
            ConstantStringArray record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantStringArray

        public ConstantStringValue GetConstantStringValue(ConstantStringValueHandle handle)
        {
            if (IsNull(handle))
                return default(ConstantStringValue);
            ConstantStringValue record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantStringValue

        public ConstantUInt16Array GetConstantUInt16Array(ConstantUInt16ArrayHandle handle)
        {
            ConstantUInt16Array record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantUInt16Array

        public ConstantUInt16Value GetConstantUInt16Value(ConstantUInt16ValueHandle handle)
        {
            ConstantUInt16Value record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantUInt16Value

        public ConstantUInt32Array GetConstantUInt32Array(ConstantUInt32ArrayHandle handle)
        {
            ConstantUInt32Array record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantUInt32Array

        public ConstantUInt32Value GetConstantUInt32Value(ConstantUInt32ValueHandle handle)
        {
            ConstantUInt32Value record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantUInt32Value

        public ConstantUInt64Array GetConstantUInt64Array(ConstantUInt64ArrayHandle handle)
        {
            ConstantUInt64Array record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantUInt64Array

        public ConstantUInt64Value GetConstantUInt64Value(ConstantUInt64ValueHandle handle)
        {
            ConstantUInt64Value record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetConstantUInt64Value

        public CustomAttribute GetCustomAttribute(CustomAttributeHandle handle)
        {
            CustomAttribute record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._constructor);
            offset = _streamReader.Read(offset, out record._fixedArguments);
            offset = _streamReader.Read(offset, out record._namedArguments);
            return record;
        } // GetCustomAttribute

        public Event GetEvent(EventHandle handle)
        {
            Event record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._type);
            offset = _streamReader.Read(offset, out record._methodSemantics);
            offset = _streamReader.Read(offset, out record._customAttributes);
            return record;
        } // GetEvent

        public Field GetField(FieldHandle handle)
        {
            Field record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._signature);
            offset = _streamReader.Read(offset, out record._defaultValue);
            offset = _streamReader.Read(offset, out record._offset);
            offset = _streamReader.Read(offset, out record._customAttributes);
            return record;
        } // GetField

        public FieldSignature GetFieldSignature(FieldSignatureHandle handle)
        {
            FieldSignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._type);
            return record;
        } // GetFieldSignature

        public FunctionPointerSignature GetFunctionPointerSignature(FunctionPointerSignatureHandle handle)
        {
            FunctionPointerSignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._signature);
            return record;
        } // GetFunctionPointerSignature

        public GenericParameter GetGenericParameter(GenericParameterHandle handle)
        {
            GenericParameter record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._number);
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._kind);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._constraints);
            offset = _streamReader.Read(offset, out record._customAttributes);
            return record;
        } // GetGenericParameter

        public MemberReference GetMemberReference(MemberReferenceHandle handle)
        {
            MemberReference record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._parent);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._signature);
            return record;
        } // GetMemberReference

        public Method GetMethod(MethodHandle handle)
        {
            Method record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._implFlags);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._signature);
            offset = _streamReader.Read(offset, out record._parameters);
            offset = _streamReader.Read(offset, out record._genericParameters);
            offset = _streamReader.Read(offset, out record._customAttributes);
            return record;
        } // GetMethod

        public MethodInstantiation GetMethodInstantiation(MethodInstantiationHandle handle)
        {
            MethodInstantiation record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._method);
            offset = _streamReader.Read(offset, out record._genericTypeArguments);
            return record;
        } // GetMethodInstantiation

        public MethodSemantics GetMethodSemantics(MethodSemanticsHandle handle)
        {
            MethodSemantics record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._attributes);
            offset = _streamReader.Read(offset, out record._method);
            return record;
        } // GetMethodSemantics

        public MethodSignature GetMethodSignature(MethodSignatureHandle handle)
        {
            MethodSignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._callingConvention);
            offset = _streamReader.Read(offset, out record._genericParameterCount);
            offset = _streamReader.Read(offset, out record._returnType);
            offset = _streamReader.Read(offset, out record._parameters);
            offset = _streamReader.Read(offset, out record._varArgParameters);
            return record;
        } // GetMethodSignature

        public MethodTypeVariableSignature GetMethodTypeVariableSignature(MethodTypeVariableSignatureHandle handle)
        {
            MethodTypeVariableSignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._number);
            return record;
        } // GetMethodTypeVariableSignature

        public ModifiedType GetModifiedType(ModifiedTypeHandle handle)
        {
            ModifiedType record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._isOptional);
            offset = _streamReader.Read(offset, out record._modifierType);
            offset = _streamReader.Read(offset, out record._type);
            return record;
        } // GetModifiedType

        public NamedArgument GetNamedArgument(NamedArgumentHandle handle)
        {
            NamedArgument record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._type);
            offset = _streamReader.Read(offset, out record._value);
            return record;
        } // GetNamedArgument

        public NamespaceDefinition GetNamespaceDefinition(NamespaceDefinitionHandle handle)
        {
            NamespaceDefinition record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._parentScopeOrNamespace);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._typeDefinitions);
            offset = _streamReader.Read(offset, out record._typeForwarders);
            offset = _streamReader.Read(offset, out record._namespaceDefinitions);
            return record;
        } // GetNamespaceDefinition

        public NamespaceReference GetNamespaceReference(NamespaceReferenceHandle handle)
        {
            NamespaceReference record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._parentScopeOrNamespace);
            offset = _streamReader.Read(offset, out record._name);
            return record;
        } // GetNamespaceReference

        public Parameter GetParameter(ParameterHandle handle)
        {
            Parameter record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._sequence);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._defaultValue);
            offset = _streamReader.Read(offset, out record._customAttributes);
            return record;
        } // GetParameter

        public PointerSignature GetPointerSignature(PointerSignatureHandle handle)
        {
            PointerSignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._type);
            return record;
        } // GetPointerSignature

        public Property GetProperty(PropertyHandle handle)
        {
            Property record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._signature);
            offset = _streamReader.Read(offset, out record._methodSemantics);
            offset = _streamReader.Read(offset, out record._defaultValue);
            offset = _streamReader.Read(offset, out record._customAttributes);
            return record;
        } // GetProperty

        public PropertySignature GetPropertySignature(PropertySignatureHandle handle)
        {
            PropertySignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._callingConvention);
            offset = _streamReader.Read(offset, out record._type);
            offset = _streamReader.Read(offset, out record._parameters);
            return record;
        } // GetPropertySignature

        public QualifiedField GetQualifiedField(QualifiedFieldHandle handle)
        {
            QualifiedField record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._field);
            offset = _streamReader.Read(offset, out record._enclosingType);
            return record;
        } // GetQualifiedField

        public QualifiedMethod GetQualifiedMethod(QualifiedMethodHandle handle)
        {
            QualifiedMethod record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._method);
            offset = _streamReader.Read(offset, out record._enclosingType);
            return record;
        } // GetQualifiedMethod

        public SZArraySignature GetSZArraySignature(SZArraySignatureHandle handle)
        {
            SZArraySignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._elementType);
            return record;
        } // GetSZArraySignature

        public ScopeDefinition GetScopeDefinition(ScopeDefinitionHandle handle)
        {
            ScopeDefinition record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._hashAlgorithm);
            offset = _streamReader.Read(offset, out record._majorVersion);
            offset = _streamReader.Read(offset, out record._minorVersion);
            offset = _streamReader.Read(offset, out record._buildNumber);
            offset = _streamReader.Read(offset, out record._revisionNumber);
            offset = _streamReader.Read(offset, out record._publicKey);
            offset = _streamReader.Read(offset, out record._culture);
            offset = _streamReader.Read(offset, out record._rootNamespaceDefinition);
            offset = _streamReader.Read(offset, out record._entryPoint);
            offset = _streamReader.Read(offset, out record._globalModuleType);
            offset = _streamReader.Read(offset, out record._customAttributes);
            offset = _streamReader.Read(offset, out record._moduleName);
            offset = _streamReader.Read(offset, out record._mvid);
            offset = _streamReader.Read(offset, out record._moduleCustomAttributes);
            return record;
        } // GetScopeDefinition

        public ScopeReference GetScopeReference(ScopeReferenceHandle handle)
        {
            ScopeReference record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._majorVersion);
            offset = _streamReader.Read(offset, out record._minorVersion);
            offset = _streamReader.Read(offset, out record._buildNumber);
            offset = _streamReader.Read(offset, out record._revisionNumber);
            offset = _streamReader.Read(offset, out record._publicKeyOrToken);
            offset = _streamReader.Read(offset, out record._culture);
            return record;
        } // GetScopeReference

        public TypeDefinition GetTypeDefinition(TypeDefinitionHandle handle)
        {
            TypeDefinition record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._flags);
            offset = _streamReader.Read(offset, out record._baseType);
            offset = _streamReader.Read(offset, out record._namespaceDefinition);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._size);
            offset = _streamReader.Read(offset, out record._packingSize);
            offset = _streamReader.Read(offset, out record._enclosingType);
            offset = _streamReader.Read(offset, out record._nestedTypes);
            offset = _streamReader.Read(offset, out record._methods);
            offset = _streamReader.Read(offset, out record._fields);
            offset = _streamReader.Read(offset, out record._properties);
            offset = _streamReader.Read(offset, out record._events);
            offset = _streamReader.Read(offset, out record._genericParameters);
            offset = _streamReader.Read(offset, out record._interfaces);
            offset = _streamReader.Read(offset, out record._customAttributes);
            return record;
        } // GetTypeDefinition

        public TypeForwarder GetTypeForwarder(TypeForwarderHandle handle)
        {
            TypeForwarder record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._scope);
            offset = _streamReader.Read(offset, out record._name);
            offset = _streamReader.Read(offset, out record._nestedTypes);
            return record;
        } // GetTypeForwarder

        public TypeInstantiationSignature GetTypeInstantiationSignature(TypeInstantiationSignatureHandle handle)
        {
            TypeInstantiationSignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._genericType);
            offset = _streamReader.Read(offset, out record._genericTypeArguments);
            return record;
        } // GetTypeInstantiationSignature

        public TypeReference GetTypeReference(TypeReferenceHandle handle)
        {
            TypeReference record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._parentNamespaceOrType);
            offset = _streamReader.Read(offset, out record._typeName);
            return record;
        } // GetTypeReference

        public TypeSpecification GetTypeSpecification(TypeSpecificationHandle handle)
        {
            TypeSpecification record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._signature);
            return record;
        } // GetTypeSpecification

        public TypeVariableSignature GetTypeVariableSignature(TypeVariableSignatureHandle handle)
        {
            TypeVariableSignature record;
            record._reader = this;
            record._handle = handle;
            var offset = (uint)handle.Offset;
            offset = _streamReader.Read(offset, out record._number);
            return record;
        } // GetTypeVariableSignature

        internal Handle ToHandle(ArraySignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ByReferenceSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantBooleanArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantBooleanValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantBoxedEnumValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantByteArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantByteValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantCharArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantCharValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantDoubleArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantDoubleValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantEnumArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantHandleArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantInt16ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantInt16ValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantInt32ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantInt32ValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantInt64ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantInt64ValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantReferenceValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantSByteArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantSByteValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantSingleArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantSingleValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantStringArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantStringValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantUInt16ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantUInt16ValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantUInt32ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantUInt32ValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantUInt64ArrayHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ConstantUInt64ValueHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(CustomAttributeHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(EventHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(FieldHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(FieldSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(FunctionPointerSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(GenericParameterHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(MemberReferenceHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(MethodHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(MethodInstantiationHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(MethodSemanticsHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(MethodSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(MethodTypeVariableSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ModifiedTypeHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(NamedArgumentHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(NamespaceDefinitionHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(NamespaceReferenceHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ParameterHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(PointerSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(PropertyHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(PropertySignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(QualifiedFieldHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(QualifiedMethodHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(SZArraySignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ScopeDefinitionHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(ScopeReferenceHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(TypeDefinitionHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(TypeForwarderHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(TypeInstantiationSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(TypeReferenceHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(TypeSpecificationHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal Handle ToHandle(TypeVariableSignatureHandle handle)
        {
            return new Handle(handle._value);
        } // ToHandle

        internal ArraySignatureHandle ToArraySignatureHandle(Handle handle)
        {
            return new ArraySignatureHandle(handle._value);
        } // ToArraySignatureHandle

        internal ByReferenceSignatureHandle ToByReferenceSignatureHandle(Handle handle)
        {
            return new ByReferenceSignatureHandle(handle._value);
        } // ToByReferenceSignatureHandle

        internal ConstantBooleanArrayHandle ToConstantBooleanArrayHandle(Handle handle)
        {
            return new ConstantBooleanArrayHandle(handle._value);
        } // ToConstantBooleanArrayHandle

        internal ConstantBooleanValueHandle ToConstantBooleanValueHandle(Handle handle)
        {
            return new ConstantBooleanValueHandle(handle._value);
        } // ToConstantBooleanValueHandle

        internal ConstantBoxedEnumValueHandle ToConstantBoxedEnumValueHandle(Handle handle)
        {
            return new ConstantBoxedEnumValueHandle(handle._value);
        } // ToConstantBoxedEnumValueHandle

        internal ConstantByteArrayHandle ToConstantByteArrayHandle(Handle handle)
        {
            return new ConstantByteArrayHandle(handle._value);
        } // ToConstantByteArrayHandle

        internal ConstantByteValueHandle ToConstantByteValueHandle(Handle handle)
        {
            return new ConstantByteValueHandle(handle._value);
        } // ToConstantByteValueHandle

        internal ConstantCharArrayHandle ToConstantCharArrayHandle(Handle handle)
        {
            return new ConstantCharArrayHandle(handle._value);
        } // ToConstantCharArrayHandle

        internal ConstantCharValueHandle ToConstantCharValueHandle(Handle handle)
        {
            return new ConstantCharValueHandle(handle._value);
        } // ToConstantCharValueHandle

        internal ConstantDoubleArrayHandle ToConstantDoubleArrayHandle(Handle handle)
        {
            return new ConstantDoubleArrayHandle(handle._value);
        } // ToConstantDoubleArrayHandle

        internal ConstantDoubleValueHandle ToConstantDoubleValueHandle(Handle handle)
        {
            return new ConstantDoubleValueHandle(handle._value);
        } // ToConstantDoubleValueHandle

        internal ConstantEnumArrayHandle ToConstantEnumArrayHandle(Handle handle)
        {
            return new ConstantEnumArrayHandle(handle._value);
        } // ToConstantEnumArrayHandle

        internal ConstantHandleArrayHandle ToConstantHandleArrayHandle(Handle handle)
        {
            return new ConstantHandleArrayHandle(handle._value);
        } // ToConstantHandleArrayHandle

        internal ConstantInt16ArrayHandle ToConstantInt16ArrayHandle(Handle handle)
        {
            return new ConstantInt16ArrayHandle(handle._value);
        } // ToConstantInt16ArrayHandle

        internal ConstantInt16ValueHandle ToConstantInt16ValueHandle(Handle handle)
        {
            return new ConstantInt16ValueHandle(handle._value);
        } // ToConstantInt16ValueHandle

        internal ConstantInt32ArrayHandle ToConstantInt32ArrayHandle(Handle handle)
        {
            return new ConstantInt32ArrayHandle(handle._value);
        } // ToConstantInt32ArrayHandle

        internal ConstantInt32ValueHandle ToConstantInt32ValueHandle(Handle handle)
        {
            return new ConstantInt32ValueHandle(handle._value);
        } // ToConstantInt32ValueHandle

        internal ConstantInt64ArrayHandle ToConstantInt64ArrayHandle(Handle handle)
        {
            return new ConstantInt64ArrayHandle(handle._value);
        } // ToConstantInt64ArrayHandle

        internal ConstantInt64ValueHandle ToConstantInt64ValueHandle(Handle handle)
        {
            return new ConstantInt64ValueHandle(handle._value);
        } // ToConstantInt64ValueHandle

        internal ConstantReferenceValueHandle ToConstantReferenceValueHandle(Handle handle)
        {
            return new ConstantReferenceValueHandle(handle._value);
        } // ToConstantReferenceValueHandle

        internal ConstantSByteArrayHandle ToConstantSByteArrayHandle(Handle handle)
        {
            return new ConstantSByteArrayHandle(handle._value);
        } // ToConstantSByteArrayHandle

        internal ConstantSByteValueHandle ToConstantSByteValueHandle(Handle handle)
        {
            return new ConstantSByteValueHandle(handle._value);
        } // ToConstantSByteValueHandle

        internal ConstantSingleArrayHandle ToConstantSingleArrayHandle(Handle handle)
        {
            return new ConstantSingleArrayHandle(handle._value);
        } // ToConstantSingleArrayHandle

        internal ConstantSingleValueHandle ToConstantSingleValueHandle(Handle handle)
        {
            return new ConstantSingleValueHandle(handle._value);
        } // ToConstantSingleValueHandle

        internal ConstantStringArrayHandle ToConstantStringArrayHandle(Handle handle)
        {
            return new ConstantStringArrayHandle(handle._value);
        } // ToConstantStringArrayHandle

        internal ConstantStringValueHandle ToConstantStringValueHandle(Handle handle)
        {
            return new ConstantStringValueHandle(handle._value);
        } // ToConstantStringValueHandle

        internal ConstantUInt16ArrayHandle ToConstantUInt16ArrayHandle(Handle handle)
        {
            return new ConstantUInt16ArrayHandle(handle._value);
        } // ToConstantUInt16ArrayHandle

        internal ConstantUInt16ValueHandle ToConstantUInt16ValueHandle(Handle handle)
        {
            return new ConstantUInt16ValueHandle(handle._value);
        } // ToConstantUInt16ValueHandle

        internal ConstantUInt32ArrayHandle ToConstantUInt32ArrayHandle(Handle handle)
        {
            return new ConstantUInt32ArrayHandle(handle._value);
        } // ToConstantUInt32ArrayHandle

        internal ConstantUInt32ValueHandle ToConstantUInt32ValueHandle(Handle handle)
        {
            return new ConstantUInt32ValueHandle(handle._value);
        } // ToConstantUInt32ValueHandle

        internal ConstantUInt64ArrayHandle ToConstantUInt64ArrayHandle(Handle handle)
        {
            return new ConstantUInt64ArrayHandle(handle._value);
        } // ToConstantUInt64ArrayHandle

        internal ConstantUInt64ValueHandle ToConstantUInt64ValueHandle(Handle handle)
        {
            return new ConstantUInt64ValueHandle(handle._value);
        } // ToConstantUInt64ValueHandle

        internal CustomAttributeHandle ToCustomAttributeHandle(Handle handle)
        {
            return new CustomAttributeHandle(handle._value);
        } // ToCustomAttributeHandle

        internal EventHandle ToEventHandle(Handle handle)
        {
            return new EventHandle(handle._value);
        } // ToEventHandle

        internal FieldHandle ToFieldHandle(Handle handle)
        {
            return new FieldHandle(handle._value);
        } // ToFieldHandle

        internal FieldSignatureHandle ToFieldSignatureHandle(Handle handle)
        {
            return new FieldSignatureHandle(handle._value);
        } // ToFieldSignatureHandle

        internal FunctionPointerSignatureHandle ToFunctionPointerSignatureHandle(Handle handle)
        {
            return new FunctionPointerSignatureHandle(handle._value);
        } // ToFunctionPointerSignatureHandle

        internal GenericParameterHandle ToGenericParameterHandle(Handle handle)
        {
            return new GenericParameterHandle(handle._value);
        } // ToGenericParameterHandle

        internal MemberReferenceHandle ToMemberReferenceHandle(Handle handle)
        {
            return new MemberReferenceHandle(handle._value);
        } // ToMemberReferenceHandle

        internal MethodHandle ToMethodHandle(Handle handle)
        {
            return new MethodHandle(handle._value);
        } // ToMethodHandle

        internal MethodInstantiationHandle ToMethodInstantiationHandle(Handle handle)
        {
            return new MethodInstantiationHandle(handle._value);
        } // ToMethodInstantiationHandle

        internal MethodSemanticsHandle ToMethodSemanticsHandle(Handle handle)
        {
            return new MethodSemanticsHandle(handle._value);
        } // ToMethodSemanticsHandle

        internal MethodSignatureHandle ToMethodSignatureHandle(Handle handle)
        {
            return new MethodSignatureHandle(handle._value);
        } // ToMethodSignatureHandle

        internal MethodTypeVariableSignatureHandle ToMethodTypeVariableSignatureHandle(Handle handle)
        {
            return new MethodTypeVariableSignatureHandle(handle._value);
        } // ToMethodTypeVariableSignatureHandle

        internal ModifiedTypeHandle ToModifiedTypeHandle(Handle handle)
        {
            return new ModifiedTypeHandle(handle._value);
        } // ToModifiedTypeHandle

        internal NamedArgumentHandle ToNamedArgumentHandle(Handle handle)
        {
            return new NamedArgumentHandle(handle._value);
        } // ToNamedArgumentHandle

        internal NamespaceDefinitionHandle ToNamespaceDefinitionHandle(Handle handle)
        {
            return new NamespaceDefinitionHandle(handle._value);
        } // ToNamespaceDefinitionHandle

        internal NamespaceReferenceHandle ToNamespaceReferenceHandle(Handle handle)
        {
            return new NamespaceReferenceHandle(handle._value);
        } // ToNamespaceReferenceHandle

        internal ParameterHandle ToParameterHandle(Handle handle)
        {
            return new ParameterHandle(handle._value);
        } // ToParameterHandle

        internal PointerSignatureHandle ToPointerSignatureHandle(Handle handle)
        {
            return new PointerSignatureHandle(handle._value);
        } // ToPointerSignatureHandle

        internal PropertyHandle ToPropertyHandle(Handle handle)
        {
            return new PropertyHandle(handle._value);
        } // ToPropertyHandle

        internal PropertySignatureHandle ToPropertySignatureHandle(Handle handle)
        {
            return new PropertySignatureHandle(handle._value);
        } // ToPropertySignatureHandle

        internal QualifiedFieldHandle ToQualifiedFieldHandle(Handle handle)
        {
            return new QualifiedFieldHandle(handle._value);
        } // ToQualifiedFieldHandle

        internal QualifiedMethodHandle ToQualifiedMethodHandle(Handle handle)
        {
            return new QualifiedMethodHandle(handle._value);
        } // ToQualifiedMethodHandle

        internal SZArraySignatureHandle ToSZArraySignatureHandle(Handle handle)
        {
            return new SZArraySignatureHandle(handle._value);
        } // ToSZArraySignatureHandle

        internal ScopeDefinitionHandle ToScopeDefinitionHandle(Handle handle)
        {
            return new ScopeDefinitionHandle(handle._value);
        } // ToScopeDefinitionHandle

        internal ScopeReferenceHandle ToScopeReferenceHandle(Handle handle)
        {
            return new ScopeReferenceHandle(handle._value);
        } // ToScopeReferenceHandle

        internal TypeDefinitionHandle ToTypeDefinitionHandle(Handle handle)
        {
            return new TypeDefinitionHandle(handle._value);
        } // ToTypeDefinitionHandle

        internal TypeForwarderHandle ToTypeForwarderHandle(Handle handle)
        {
            return new TypeForwarderHandle(handle._value);
        } // ToTypeForwarderHandle

        internal TypeInstantiationSignatureHandle ToTypeInstantiationSignatureHandle(Handle handle)
        {
            return new TypeInstantiationSignatureHandle(handle._value);
        } // ToTypeInstantiationSignatureHandle

        internal TypeReferenceHandle ToTypeReferenceHandle(Handle handle)
        {
            return new TypeReferenceHandle(handle._value);
        } // ToTypeReferenceHandle

        internal TypeSpecificationHandle ToTypeSpecificationHandle(Handle handle)
        {
            return new TypeSpecificationHandle(handle._value);
        } // ToTypeSpecificationHandle

        internal TypeVariableSignatureHandle ToTypeVariableSignatureHandle(Handle handle)
        {
            return new TypeVariableSignatureHandle(handle._value);
        } // ToTypeVariableSignatureHandle

        internal bool IsNull(ArraySignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ByReferenceSignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantBooleanArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantBooleanValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantBoxedEnumValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantByteArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantByteValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantCharArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantCharValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantDoubleArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantDoubleValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantEnumArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantHandleArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantInt16ArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantInt16ValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantInt32ArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantInt32ValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantInt64ArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantInt64ValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantReferenceValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantSByteArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantSByteValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantSingleArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantSingleValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantStringArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantStringValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantUInt16ArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantUInt16ValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantUInt32ArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantUInt32ValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantUInt64ArrayHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ConstantUInt64ValueHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(CustomAttributeHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(EventHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(FieldHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(FieldSignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(FunctionPointerSignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(GenericParameterHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(MemberReferenceHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(MethodHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(MethodInstantiationHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(MethodSemanticsHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(MethodSignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(MethodTypeVariableSignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ModifiedTypeHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(NamedArgumentHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(NamespaceDefinitionHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(NamespaceReferenceHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ParameterHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(PointerSignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(PropertyHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(PropertySignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(QualifiedFieldHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(QualifiedMethodHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(SZArraySignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ScopeDefinitionHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(ScopeReferenceHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(TypeDefinitionHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(TypeForwarderHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(TypeInstantiationSignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(TypeReferenceHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(TypeSpecificationHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull

        internal bool IsNull(TypeVariableSignatureHandle handle)
        {
            return (handle._value & 0x00FFFFFF) == 0;
        } // IsNull
    } // MetadataReader
} // Internal.Metadata.NativeFormat
