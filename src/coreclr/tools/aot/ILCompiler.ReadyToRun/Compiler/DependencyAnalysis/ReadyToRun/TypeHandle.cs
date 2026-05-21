// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.CorConstants;
using Internal.Runtime;
using Internal.Runtime.CallingConvention;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Crossgen2's implementation of ITypeHandle, backed by Internal.TypeSystem.TypeDesc.
    /// </summary>
    internal struct TypeHandle : ITypeHandle
    {
        public TypeHandle(TypeDesc type)
        {
            _type = type;
            _isByRef = _type.IsByRef;
            if (_isByRef)
            {
                _type = ((ByRefType)_type).ParameterType;
            }
        }

        private readonly TypeDesc _type;
        private readonly bool _isByRef;

        public bool Equals(TypeHandle other)
        {
            return _isByRef == other._isByRef && _type == other._type;
        }

        public override int GetHashCode() { return (int)_type.GetHashCode(); }

        public bool IsNull() { return _type == null && !_isByRef; }
        public bool IsValueType() { if (_isByRef) return false; return _type.IsValueType; }
        public bool IsPointerType() { if (_isByRef) return false; return _type.IsPointer; }

        public bool HasIndeterminateSize() { return IsValueType() && ((DefType)_type).InstanceFieldSize.IsIndeterminate; }

        public int PointerSize => _type.Context.Target.PointerSize;

        public int GetSize()
        {
            if (IsValueType())
                return ((DefType)_type).InstanceFieldSize.AsInt;
            else
                return PointerSize;
        }

        public bool RequiresAlign8()
        {
            if (_type.Context.Target.Architecture != TargetArchitecture.ARM)
            {
                return false;
            }
            if (_isByRef)
            {
                return false;
            }
            return _type.RequiresAlign8();
        }

        public bool IsHomogeneousAggregate()
        {
            TargetArchitecture targetArch = _type.Context.Target.Architecture;
            if ((targetArch != TargetArchitecture.ARM) && (targetArch != TargetArchitecture.ARM64))
            {
                return false;
            }
            if (_isByRef)
            {
                return false;
            }
            return _type is DefType defType && defType.IsHomogeneousAggregate;
        }

        public int GetHomogeneousAggregateElementSize()
        {
            Debug.Assert(IsHomogeneousAggregate());
            switch (_type.Context.Target.Architecture)
            {
                case TargetArchitecture.ARM:
                    return RequiresAlign8() ? 8 : 4;

                case TargetArchitecture.ARM64:
                    return ((DefType)_type).GetHomogeneousAggregateElementSize();
            }
            throw new InvalidOperationException();
        }

        public CorElementType GetCorElementType()
        {
            if (_isByRef)
            {
                return CorElementType.ELEMENT_TYPE_BYREF;
            }

            Internal.TypeSystem.TypeFlags category = _type.UnderlyingType.Category;
            // We use the UnderlyingType to handle Enums properly
            return category switch
            {
                Internal.TypeSystem.TypeFlags.Boolean => CorElementType.ELEMENT_TYPE_BOOLEAN,
                Internal.TypeSystem.TypeFlags.Char => CorElementType.ELEMENT_TYPE_CHAR,
                Internal.TypeSystem.TypeFlags.SByte => CorElementType.ELEMENT_TYPE_I1,
                Internal.TypeSystem.TypeFlags.Byte => CorElementType.ELEMENT_TYPE_U1,
                Internal.TypeSystem.TypeFlags.Int16 => CorElementType.ELEMENT_TYPE_I2,
                Internal.TypeSystem.TypeFlags.UInt16 => CorElementType.ELEMENT_TYPE_U2,
                Internal.TypeSystem.TypeFlags.Int32 => CorElementType.ELEMENT_TYPE_I4,
                Internal.TypeSystem.TypeFlags.UInt32 => CorElementType.ELEMENT_TYPE_U4,
                Internal.TypeSystem.TypeFlags.Int64 => CorElementType.ELEMENT_TYPE_I8,
                Internal.TypeSystem.TypeFlags.UInt64 => CorElementType.ELEMENT_TYPE_U8,
                Internal.TypeSystem.TypeFlags.IntPtr => CorElementType.ELEMENT_TYPE_I,
                Internal.TypeSystem.TypeFlags.UIntPtr => CorElementType.ELEMENT_TYPE_U,
                Internal.TypeSystem.TypeFlags.Single => CorElementType.ELEMENT_TYPE_R4,
                Internal.TypeSystem.TypeFlags.Double => CorElementType.ELEMENT_TYPE_R8,
                Internal.TypeSystem.TypeFlags.ValueType => CorElementType.ELEMENT_TYPE_VALUETYPE,
                Internal.TypeSystem.TypeFlags.Nullable => CorElementType.ELEMENT_TYPE_VALUETYPE,
                Internal.TypeSystem.TypeFlags.Void => CorElementType.ELEMENT_TYPE_VOID,
                Internal.TypeSystem.TypeFlags.Pointer => CorElementType.ELEMENT_TYPE_PTR,
                Internal.TypeSystem.TypeFlags.FunctionPointer => CorElementType.ELEMENT_TYPE_FNPTR,

                _ => CorElementType.ELEMENT_TYPE_CLASS
            };
        }

        public void GetSystemVAmd64PassStructInRegisterDescriptor(out SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor)
        {
            SystemVStructClassificator.GetSystemVAmd64PassStructInRegisterDescriptor(_type, out descriptor);
        }

        public FpStructInRegistersInfo GetFpStructInRegistersInfo(TargetArchitecture architecture)
        {
            return RiscVLoongArch64FpStruct.GetFpStructInRegistersInfo(_type, architecture);
        }

        public bool IsTrivialPointerSizedStruct()
        {
            Debug.Assert(IsValueType());
            if (GetSize() != 4)
            {
                return false;
            }
            TypeDesc typeOfEmbeddedField = null;
            foreach (var field in _type.GetFields())
            {
                if (field.IsStatic)
                    continue;
                if (typeOfEmbeddedField != null)
                {
                    return false;
                }

                typeOfEmbeddedField = field.FieldType;
            }

            if ((typeOfEmbeddedField != null) && ((typeOfEmbeddedField.IsValueType) || (typeOfEmbeddedField.IsPointer)))
            {
                switch (typeOfEmbeddedField.UnderlyingType.Category)
                {
                    case TypeFlags.IntPtr:
                    case TypeFlags.UIntPtr:
                    case TypeFlags.Int32:
                    case TypeFlags.UInt32:
                    case TypeFlags.Pointer:
                        return true;
                    case TypeFlags.ValueType:
                        return new TypeHandle(typeOfEmbeddedField).IsTrivialPointerSizedStruct();
                }
            }
            return false;
        }

        public int GetFieldAlignment()
        {
            return ((DefType)_type).InstanceFieldAlignment.AsInt;
        }

        /// <summary>
        /// Escape hatch for crossgen2-specific code that needs the underlying TypeDesc.
        /// Not part of the ITypeHandle interface.
        /// </summary>
        public TypeDesc GetRuntimeTypeHandle() { return _type; }
    }
}
