// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: Pointer Type to a MethodTable in the runtime.
**
**
===========================================================*/

using System.Runtime;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Internal.Runtime.CompilerServices;

using MethodTable = Internal.Runtime.MethodTable;
using MethodTableList = Internal.Runtime.MethodTableList;
using EETypeElementType = Internal.Runtime.EETypeElementType;
using CorElementType = System.Reflection.CorElementType;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EETypePtr : IEquatable<EETypePtr>
    {
        private MethodTable* _value;

        public EETypePtr(IntPtr value)
        {
            _value = (MethodTable*)value;
        }

        internal EETypePtr(MethodTable* value)
        {
            _value = value;
        }

        internal MethodTable* ToPointer()
        {
            return _value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is EETypePtr)
            {
                return this == (EETypePtr)obj;
            }
            return false;
        }

        public bool Equals(EETypePtr p)
        {
            return this == p;
        }

        public static bool operator ==(EETypePtr value1, EETypePtr value2)
        {
            return value1._value == value2._value;
        }

        public static bool operator !=(EETypePtr value1, EETypePtr value2)
        {
            return !(value1 == value2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return (int)_value->HashCode;
        }

        // Caution: You cannot safely compare RawValue's as RH does NOT unify EETypes. Use the == or Equals() methods exposed by EETypePtr itself.
        internal IntPtr RawValue
        {
            get
            {
                return (IntPtr)_value;
            }
        }

        internal bool IsNull
        {
            get
            {
                return _value == null;
            }
        }

        internal bool IsArray
        {
            get
            {
                return _value->IsArray;
            }
        }

        internal bool IsSzArray
        {
            get
            {
                return _value->IsSzArray;
            }
        }

        internal bool IsPointer
        {
            get
            {
                return _value->IsPointerType;
            }
        }

        internal bool IsFunctionPointer
        {
            get
            {
                return _value->IsFunctionPointerType;
            }
        }

        internal bool IsByRef
        {
            get
            {
                return _value->IsByRefType;
            }
        }

        internal bool IsValueType
        {
            get
            {
                return _value->IsValueType;
            }
        }

        internal bool IsString
        {
            get
            {
                return _value->IsString;
            }
        }

        // Warning! UNLIKE the similarly named Reflection api, this method also returns "true" for Enums.
        internal bool IsPrimitive
        {
            get
            {
                return _value->IsPrimitive;
            }
        }

        // WARNING: Never call unless the MethodTable came from an instanced object. Nested enums can be open generics (typeof(Outer<>).NestedEnum)
        // and this helper has undefined behavior when passed such as a enum.
        internal bool IsEnum
        {
            get
            {
                // Q: When is an enum type a constructed generic type?
                // A: When it's nested inside a generic type.
                if (!(IsDefType))
                    return false;

                // Generic type definitions that return true for IsPrimitive are type definitions of generic enums.
                // Otherwise check the base type.
                return (IsGenericTypeDefinition && IsPrimitive) || this.BaseType == EETypePtr.EETypePtrOf<Enum>();
            }
        }

        // Gets a value indicating whether this is a generic type definition (an uninstantiated generic type).
        internal bool IsGenericTypeDefinition
        {
            get
            {
                return _value->IsGenericTypeDefinition;
            }
        }

        // Gets a value indicating whether this is an instantiated generic type.
        internal bool IsGeneric
        {
            get
            {
                return _value->IsGeneric;
            }
        }

        internal GenericArgumentCollection Instantiation
        {
            get
            {
                return new GenericArgumentCollection(_value->GenericArity, _value->GenericArguments);
            }
        }

        internal EETypePtr GenericDefinition
        {
            get
            {
                return new EETypePtr(_value->GenericDefinition);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a class, a struct, an enum, or an interface.
        /// </summary>
        internal bool IsDefType
        {
            get
            {
                return !_value->IsParameterizedType && !_value->IsFunctionPointerType;
            }
        }

        internal bool IsDynamicType
        {
            get
            {
                return _value->IsDynamicType;
            }
        }

        internal bool IsInterface
        {
            get
            {
                return _value->IsInterface;
            }
        }

        internal bool IsByRefLike
        {
            get
            {
                return _value->IsByRefLike;
            }
        }

        internal bool IsNullable
        {
            get
            {
                return _value->IsNullable;
            }
        }

        internal bool HasCctor
        {
            get
            {
                return _value->HasCctor;
            }
        }

        internal bool IsTrackedReferenceWithFinalizer
        {
            get
            {
                return _value->IsTrackedReferenceWithFinalizer;
            }
        }

        internal EETypePtr NullableType
        {
            get
            {
                return new EETypePtr(_value->NullableType);
            }
        }

        internal EETypePtr ArrayElementType
        {
            get
            {
                return new EETypePtr(_value->RelatedParameterType);
            }
        }

        internal int ArrayRank
        {
            get
            {
                return _value->ArrayRank;
            }
        }

        internal InterfaceCollection Interfaces
        {
            get
            {
                return new InterfaceCollection(_value);
            }
        }

        internal EETypePtr BaseType
        {
            get
            {
                if (IsArray)
                    return EETypePtr.EETypePtrOf<Array>();

                if (IsPointer || IsByRef || IsFunctionPointer)
                    return new EETypePtr(default(IntPtr));

                EETypePtr baseEEType = new EETypePtr(_value->NonArrayBaseType);
                return baseEEType;
            }
        }

        internal IntPtr DispatchMap
        {
            get
            {
                return (IntPtr)_value->DispatchMap;
            }
        }

        // Instance contains pointers to managed objects.
        internal bool ContainsGCPointers
        {
            get
            {
                return _value->ContainsGCPointers;
            }
        }

        internal uint ValueTypeSize
        {
            get
            {
                return _value->ValueTypeSize;
            }
        }

        internal CorElementType CorElementType
        {
            get
            {
                ReadOnlySpan<byte> map = new byte[]
                {
                    default,
                    (byte)CorElementType.ELEMENT_TYPE_VOID,      // EETypeElementType.Void
                    (byte)CorElementType.ELEMENT_TYPE_BOOLEAN,   // EETypeElementType.Boolean
                    (byte)CorElementType.ELEMENT_TYPE_CHAR,      // EETypeElementType.Char
                    (byte)CorElementType.ELEMENT_TYPE_I1,        // EETypeElementType.SByte
                    (byte)CorElementType.ELEMENT_TYPE_U1,        // EETypeElementType.Byte
                    (byte)CorElementType.ELEMENT_TYPE_I2,        // EETypeElementType.Int16
                    (byte)CorElementType.ELEMENT_TYPE_U2,        // EETypeElementType.UInt16
                    (byte)CorElementType.ELEMENT_TYPE_I4,        // EETypeElementType.Int32
                    (byte)CorElementType.ELEMENT_TYPE_U4,        // EETypeElementType.UInt32
                    (byte)CorElementType.ELEMENT_TYPE_I8,        // EETypeElementType.Int64
                    (byte)CorElementType.ELEMENT_TYPE_U8,        // EETypeElementType.UInt64
                    (byte)CorElementType.ELEMENT_TYPE_I,         // EETypeElementType.IntPtr
                    (byte)CorElementType.ELEMENT_TYPE_U,         // EETypeElementType.UIntPtr
                    (byte)CorElementType.ELEMENT_TYPE_R4,        // EETypeElementType.Single
                    (byte)CorElementType.ELEMENT_TYPE_R8,        // EETypeElementType.Double

                    (byte)CorElementType.ELEMENT_TYPE_VALUETYPE, // EETypeElementType.ValueType
                    (byte)CorElementType.ELEMENT_TYPE_VALUETYPE,
                    (byte)CorElementType.ELEMENT_TYPE_VALUETYPE, // EETypeElementType.Nullable
                    (byte)CorElementType.ELEMENT_TYPE_VALUETYPE,
                    (byte)CorElementType.ELEMENT_TYPE_CLASS,     // EETypeElementType.Class
                    (byte)CorElementType.ELEMENT_TYPE_CLASS,     // EETypeElementType.Interface
                    (byte)CorElementType.ELEMENT_TYPE_CLASS,     // EETypeElementType.SystemArray
                    (byte)CorElementType.ELEMENT_TYPE_ARRAY,     // EETypeElementType.Array
                    (byte)CorElementType.ELEMENT_TYPE_SZARRAY,   // EETypeElementType.SzArray
                    (byte)CorElementType.ELEMENT_TYPE_BYREF,     // EETypeElementType.ByRef
                    (byte)CorElementType.ELEMENT_TYPE_PTR,       // EETypeElementType.Pointer
                    (byte)CorElementType.ELEMENT_TYPE_FNPTR,     // EETypeElementType.FunctionPointer
                    default, // Pad the map to 32 elements to enable range check elimination
                    default,
                    default,
                    default
                };

                // Verify last element of the map
                Debug.Assert((byte)CorElementType.ELEMENT_TYPE_FNPTR == map[(int)EETypeElementType.FunctionPointer]);

                return (CorElementType)map[(int)ElementType];
            }
        }

        internal EETypeElementType ElementType
        {
            get
            {
                return _value->ElementType;
            }
        }

        internal RuntimeImports.RhCorElementTypeInfo CorElementTypeInfo
        {
            get
            {
                return RuntimeImports.GetRhCorElementTypeInfo(CorElementType);
            }
        }

        internal ref T GetWritableData<T>() where T : unmanaged
        {
            Debug.Assert(Internal.Runtime.WritableData.GetSize(IntPtr.Size) == sizeof(T));
            return ref Unsafe.AsRef<T>((void*)_value->WritableData);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EETypePtr EETypePtrOf<T>()
        {
            // Compilers are required to provide a low level implementation of this method.
            throw new NotImplementedException();
        }

        public struct InterfaceCollection
        {
            private MethodTable* _value;

            internal InterfaceCollection(MethodTable* value)
            {
                _value = value;
            }

            public int Count
            {
                get
                {
                    return _value->NumInterfaces;
                }
            }

            public EETypePtr this[int index]
            {
                get
                {
                    Debug.Assert((uint)index < _value->NumInterfaces);

                    return new EETypePtr(_value->InterfaceMap[index]);
                }
            }
        }

        public struct GenericArgumentCollection
        {
            private MethodTableList _arguments;
            private uint _argumentCount;

            internal GenericArgumentCollection(uint argumentCount, MethodTableList arguments)
            {
                _argumentCount = argumentCount;
                _arguments = arguments;
            }

            public int Length
            {
                get
                {
                    return (int)_argumentCount;
                }
            }

            public EETypePtr this[int index]
            {
                get
                {
                    Debug.Assert((uint)index < _argumentCount);
                    return new EETypePtr(_arguments[index]);
                }
            }
        }
    }
}
