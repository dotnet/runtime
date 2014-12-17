// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata
{
    public struct Handle : IEquatable<Handle>
    {
        internal readonly uint value;

        internal Handle(uint value)
        {
            this.value = value;
        }

        internal uint RowId { get { return value & TokenTypeIds.RIDMask; } }
        internal uint TokenType { get { return value & TokenTypeIds.TokenTypeMask; } }

        internal bool IsVirtual
        {
            get { return (value & TokenTypeIds.VirtualTokenMask) != 0; }
        }

        internal bool IsHeapHandle
        {
            get { return (value & TokenTypeIds.HeapMask) == TokenTypeIds.HeapMask; }
        }

        public HandleKind Kind
        {
            get
            {
                uint tokenType = TokenType;

                // Do not surface special string and namespace token sub-types (e.g. dot terminated, winrt prefixed, synthetic) 
                // in public-facing handle type.
                if (tokenType > TokenTypeIds.String)
                {
                    tokenType &= ~TokenTypeIds.StringOrNamespaceKindMask;
                    Debug.Assert(tokenType == TokenTypeIds.String || tokenType == TokenTypeIds.Namespace);
                }

                return (HandleKind)(tokenType >> TokenTypeIds.RowIdBitCount);
            }
        }

        public bool IsNil
        {
            get
            {
                return (value & TokenTypeIds.VirtualBitAndRowIdMask) == 0;
            }
        }

        public static bool operator ==(Handle left, Handle right)
        {
            return left.value == right.value;
        }

        public override bool Equals(object obj)
        {
            return obj is Handle && ((Handle)obj).value == this.value;
        }

        public bool Equals(Handle other)
        {
            return this.value == other.value;
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        public static bool operator !=(Handle left, Handle right)
        {
            return left.value != right.value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidCast()
        {
            throw new InvalidCastException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidCodedIndex()
        {
            throw new BadImageFormatException(MetadataResources.InvalidCodedIndex);
        }

        public static readonly ModuleDefinitionHandle ModuleDefinition = new ModuleDefinitionHandle(1);
        public static readonly AssemblyDefinitionHandle AssemblyDefinition = new AssemblyDefinitionHandle(1);
    }

    public struct ModuleDefinitionHandle : IEquatable<ModuleDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Module;
        private readonly uint _rowId;

        internal ModuleDefinitionHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ModuleDefinitionHandle FromRowId(uint rowId)
        {
            return new ModuleDefinitionHandle(rowId);
        }

        public static implicit operator Handle(ModuleDefinitionHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator ModuleDefinitionHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ModuleDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(ModuleDefinitionHandle left, ModuleDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ModuleDefinitionHandle && ((ModuleDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ModuleDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ModuleDefinitionHandle left, ModuleDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct AssemblyDefinitionHandle : IEquatable<AssemblyDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Assembly;
        private readonly uint _rowId;

        internal AssemblyDefinitionHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static AssemblyDefinitionHandle FromRowId(uint rowId)
        {
            return new AssemblyDefinitionHandle(rowId);
        }

        public static implicit operator Handle(AssemblyDefinitionHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator AssemblyDefinitionHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(AssemblyDefinitionHandle left, AssemblyDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyDefinitionHandle && ((AssemblyDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(AssemblyDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(AssemblyDefinitionHandle left, AssemblyDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct InterfaceImplementationHandle : IEquatable<InterfaceImplementationHandle>
    {
        private const uint tokenType = TokenTypeIds.InterfaceImpl;
        private readonly uint _rowId;

        internal InterfaceImplementationHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static InterfaceImplementationHandle FromRowId(uint rowId)
        {
            return new InterfaceImplementationHandle(rowId);
        }

        public static implicit operator Handle(InterfaceImplementationHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator InterfaceImplementationHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new InterfaceImplementationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(InterfaceImplementationHandle left, InterfaceImplementationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is InterfaceImplementationHandle && ((InterfaceImplementationHandle)obj)._rowId == _rowId;
        }

        public bool Equals(InterfaceImplementationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(InterfaceImplementationHandle left, InterfaceImplementationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct MethodDefinitionHandle : IEquatable<MethodDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodDef;
        private readonly uint _rowId;

        private MethodDefinitionHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodDefinitionHandle FromRowId(uint rowId)
        {
            return new MethodDefinitionHandle(rowId);
        }

        public static implicit operator Handle(MethodDefinitionHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator MethodDefinitionHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(MethodDefinitionHandle left, MethodDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodDefinitionHandle && ((MethodDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MethodDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MethodDefinitionHandle left, MethodDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct MethodImplementationHandle : IEquatable<MethodImplementationHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodImpl;
        private readonly uint _rowId;

        private MethodImplementationHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodImplementationHandle FromRowId(uint rowId)
        {
            return new MethodImplementationHandle(rowId);
        }

        public static implicit operator Handle(MethodImplementationHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator MethodImplementationHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodImplementationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(MethodImplementationHandle left, MethodImplementationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodImplementationHandle && ((MethodImplementationHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MethodImplementationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MethodImplementationHandle left, MethodImplementationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct MethodSpecificationHandle : IEquatable<MethodSpecificationHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodSpec;
        private readonly uint _rowId;

        private MethodSpecificationHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodSpecificationHandle FromRowId(uint rowId)
        {
            return new MethodSpecificationHandle(rowId);
        }

        public static implicit operator Handle(MethodSpecificationHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator MethodSpecificationHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodSpecificationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(MethodSpecificationHandle left, MethodSpecificationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodSpecificationHandle && ((MethodSpecificationHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MethodSpecificationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MethodSpecificationHandle left, MethodSpecificationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct TypeDefinitionHandle : IEquatable<TypeDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeDef;
        private readonly uint _rowId;

        private TypeDefinitionHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeDefinitionHandle FromRowId(uint rowId)
        {
            return new TypeDefinitionHandle(rowId);
        }

        public static implicit operator Handle(TypeDefinitionHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator TypeDefinitionHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(TypeDefinitionHandle left, TypeDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeDefinitionHandle && ((TypeDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(TypeDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(TypeDefinitionHandle left, TypeDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ExportedTypeHandle : IEquatable<ExportedTypeHandle>
    {
        private const uint tokenType = TokenTypeIds.ExportedType;
        private readonly uint _rowId;

        private ExportedTypeHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ExportedTypeHandle FromRowId(uint rowId)
        {
            return new ExportedTypeHandle(rowId);
        }

        public static implicit operator Handle(ExportedTypeHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator ExportedTypeHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ExportedTypeHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(ExportedTypeHandle left, ExportedTypeHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ExportedTypeHandle && ((ExportedTypeHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ExportedTypeHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ExportedTypeHandle left, ExportedTypeHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct TypeReferenceHandle : IEquatable<TypeReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeRef;
        private readonly uint _rowId;

        private TypeReferenceHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeReferenceHandle FromRowId(uint rowId)
        {
            return new TypeReferenceHandle(rowId);
        }

        public static implicit operator Handle(TypeReferenceHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator TypeReferenceHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeReferenceHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(TypeReferenceHandle left, TypeReferenceHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeReferenceHandle && ((TypeReferenceHandle)obj)._rowId == _rowId;
        }

        public bool Equals(TypeReferenceHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(TypeReferenceHandle left, TypeReferenceHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct TypeSpecificationHandle : IEquatable<TypeSpecificationHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeSpec;
        private readonly uint _rowId;

        private TypeSpecificationHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeSpecificationHandle FromRowId(uint rowId)
        {
            return new TypeSpecificationHandle(rowId);
        }

        public static implicit operator Handle(TypeSpecificationHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator TypeSpecificationHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeSpecificationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(TypeSpecificationHandle left, TypeSpecificationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeSpecificationHandle && ((TypeSpecificationHandle)obj)._rowId == _rowId;
        }

        public bool Equals(TypeSpecificationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(TypeSpecificationHandle left, TypeSpecificationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct MemberReferenceHandle : IEquatable<MemberReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.MemberRef;
        private readonly uint _rowId;

        private MemberReferenceHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MemberReferenceHandle FromRowId(uint rowId)
        {
            return new MemberReferenceHandle(rowId);
        }

        public static implicit operator Handle(MemberReferenceHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator MemberReferenceHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new MemberReferenceHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(MemberReferenceHandle left, MemberReferenceHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MemberReferenceHandle && ((MemberReferenceHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MemberReferenceHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MemberReferenceHandle left, MemberReferenceHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct FieldDefinitionHandle : IEquatable<FieldDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.FieldDef;
        private readonly uint _rowId;

        private FieldDefinitionHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static FieldDefinitionHandle FromRowId(uint rowId)
        {
            return new FieldDefinitionHandle(rowId);
        }

        public static implicit operator Handle(FieldDefinitionHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator FieldDefinitionHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new FieldDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(FieldDefinitionHandle left, FieldDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is FieldDefinitionHandle && ((FieldDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(FieldDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(FieldDefinitionHandle left, FieldDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct EventDefinitionHandle : IEquatable<EventDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Event;
        private readonly uint _rowId;

        private EventDefinitionHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static EventDefinitionHandle FromRowId(uint rowId)
        {
            return new EventDefinitionHandle(rowId);
        }

        public static implicit operator Handle(EventDefinitionHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator EventDefinitionHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new EventDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(EventDefinitionHandle left, EventDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is EventDefinitionHandle && ((EventDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(EventDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(EventDefinitionHandle left, EventDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct PropertyDefinitionHandle : IEquatable<PropertyDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Property;
        private readonly uint _rowId;

        private PropertyDefinitionHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static PropertyDefinitionHandle FromRowId(uint rowId)
        {
            return new PropertyDefinitionHandle(rowId);
        }

        public static implicit operator Handle(PropertyDefinitionHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator PropertyDefinitionHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new PropertyDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(PropertyDefinitionHandle left, PropertyDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is PropertyDefinitionHandle && ((PropertyDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(PropertyDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(PropertyDefinitionHandle left, PropertyDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct StandaloneSignatureHandle : IEquatable<StandaloneSignatureHandle>
    {
        private const uint tokenType = TokenTypeIds.Signature;
        private readonly uint _rowId;

        private StandaloneSignatureHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static StandaloneSignatureHandle FromRowId(uint rowId)
        {
            return new StandaloneSignatureHandle(rowId);
        }

        public static implicit operator Handle(StandaloneSignatureHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator StandaloneSignatureHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new StandaloneSignatureHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(StandaloneSignatureHandle left, StandaloneSignatureHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is StandaloneSignatureHandle && ((StandaloneSignatureHandle)obj)._rowId == _rowId;
        }

        public bool Equals(StandaloneSignatureHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(StandaloneSignatureHandle left, StandaloneSignatureHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ParameterHandle : IEquatable<ParameterHandle>
    {
        private const uint tokenType = TokenTypeIds.ParamDef;
        private readonly uint _rowId;

        private ParameterHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ParameterHandle FromRowId(uint rowId)
        {
            return new ParameterHandle(rowId);
        }

        public static implicit operator Handle(ParameterHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator ParameterHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ParameterHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(ParameterHandle left, ParameterHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ParameterHandle && ((ParameterHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ParameterHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ParameterHandle left, ParameterHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct GenericParameterHandle : IEquatable<GenericParameterHandle>
    {
        private const uint tokenType = TokenTypeIds.GenericParam;
        private readonly uint _rowId;

        private GenericParameterHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static GenericParameterHandle FromRowId(uint rowId)
        {
            return new GenericParameterHandle(rowId);
        }

        public static implicit operator Handle(GenericParameterHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator GenericParameterHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new GenericParameterHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(GenericParameterHandle left, GenericParameterHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is GenericParameterHandle && ((GenericParameterHandle)obj)._rowId == _rowId;
        }

        public bool Equals(GenericParameterHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(GenericParameterHandle left, GenericParameterHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct GenericParameterConstraintHandle : IEquatable<GenericParameterConstraintHandle>
    {
        private const uint tokenType = TokenTypeIds.GenericParamConstraint;
        private readonly uint _rowId;

        private GenericParameterConstraintHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static GenericParameterConstraintHandle FromRowId(uint rowId)
        {
            return new GenericParameterConstraintHandle(rowId);
        }

        public static implicit operator Handle(GenericParameterConstraintHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator GenericParameterConstraintHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new GenericParameterConstraintHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(GenericParameterConstraintHandle left, GenericParameterConstraintHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is GenericParameterConstraintHandle && ((GenericParameterConstraintHandle)obj)._rowId == _rowId;
        }

        public bool Equals(GenericParameterConstraintHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(GenericParameterConstraintHandle left, GenericParameterConstraintHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ModuleReferenceHandle : IEquatable<ModuleReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.ModuleRef;
        private readonly uint _rowId;

        private ModuleReferenceHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ModuleReferenceHandle FromRowId(uint rowId)
        {
            return new ModuleReferenceHandle(rowId);
        }

        public static implicit operator Handle(ModuleReferenceHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator ModuleReferenceHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ModuleReferenceHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(ModuleReferenceHandle left, ModuleReferenceHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ModuleReferenceHandle && ((ModuleReferenceHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ModuleReferenceHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ModuleReferenceHandle left, ModuleReferenceHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct AssemblyReferenceHandle : IEquatable<AssemblyReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.AssemblyRef;

        // bits:
        //  0..24: Heap index or Virtual index
        // 25..30: TokenTypeId: AssemblyRef
        //     31: IsVirtual
        private readonly uint _token;

        internal enum VirtualIndex
        {
            System_Runtime,
            System_Runtime_InteropServices_WindowsRuntime,
            System_ObjectModel,
            System_Runtime_WindowsRuntime,
            System_Runtime_WindowsRuntime_UI_Xaml,
            System_Numerics_Vectors,

            Count
        }

        private AssemblyReferenceHandle(uint token)
        {
            _token = token;
        }

        internal static AssemblyReferenceHandle FromRowId(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            return new AssemblyReferenceHandle(tokenType | rowId);
        }

        internal static AssemblyReferenceHandle FromVirtualIndex(VirtualIndex virtualIndex)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new AssemblyReferenceHandle(TokenTypeIds.VirtualTokenMask | tokenType | (uint)virtualIndex);
        }

        public static implicit operator Handle(AssemblyReferenceHandle handle)
        {
            return new Handle(handle._token);
        }

        public static explicit operator AssemblyReferenceHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyReferenceHandle(handle.value);
        }

        public bool IsNil
        {
            get
            {
                return (_token & TokenTypeIds.VirtualBitAndRowIdMask) == 0;
            }
        }

        internal bool IsVirtual
        {
            get { return (_token & TokenTypeIds.VirtualTokenMask) != 0; }
        }

        internal uint Token { get { return _token; } }

        internal uint RowId { get { return _token & TokenTypeIds.RIDMask; } }

        public static bool operator ==(AssemblyReferenceHandle left, AssemblyReferenceHandle right)
        {
            return left._token == right._token;
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyReferenceHandle && ((AssemblyReferenceHandle)obj)._token == _token;
        }

        public bool Equals(AssemblyReferenceHandle other)
        {
            return _token == other._token;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        public static bool operator !=(AssemblyReferenceHandle left, AssemblyReferenceHandle right)
        {
            return left._token != right._token;
        }
    }

    public struct CustomAttributeHandle : IEquatable<CustomAttributeHandle>
    {
        private const uint tokenType = TokenTypeIds.CustomAttribute;
        private readonly uint _rowId;

        private CustomAttributeHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static CustomAttributeHandle FromRowId(uint rowId)
        {
            return new CustomAttributeHandle(rowId);
        }

        public static implicit operator Handle(CustomAttributeHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator CustomAttributeHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new CustomAttributeHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return _rowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(CustomAttributeHandle left, CustomAttributeHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is CustomAttributeHandle && ((CustomAttributeHandle)obj)._rowId == _rowId;
        }

        public bool Equals(CustomAttributeHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(CustomAttributeHandle left, CustomAttributeHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct DeclarativeSecurityAttributeHandle : IEquatable<DeclarativeSecurityAttributeHandle>
    {
        private const uint tokenType = TokenTypeIds.DeclSecurity;
        private readonly uint _rowId;

        private DeclarativeSecurityAttributeHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static DeclarativeSecurityAttributeHandle FromRowId(uint rowId)
        {
            return new DeclarativeSecurityAttributeHandle(rowId);
        }

        public static implicit operator Handle(DeclarativeSecurityAttributeHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator DeclarativeSecurityAttributeHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new DeclarativeSecurityAttributeHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return _rowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(DeclarativeSecurityAttributeHandle left, DeclarativeSecurityAttributeHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is DeclarativeSecurityAttributeHandle && ((DeclarativeSecurityAttributeHandle)obj)._rowId == _rowId;
        }

        public bool Equals(DeclarativeSecurityAttributeHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(DeclarativeSecurityAttributeHandle left, DeclarativeSecurityAttributeHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ConstantHandle : IEquatable<ConstantHandle>
    {
        private const uint tokenType = TokenTypeIds.Constant;
        private readonly uint _rowId;

        private ConstantHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ConstantHandle FromRowId(uint rowId)
        {
            return new ConstantHandle(rowId);
        }

        public static implicit operator Handle(ConstantHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator ConstantHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ConstantHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(ConstantHandle left, ConstantHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ConstantHandle && ((ConstantHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ConstantHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ConstantHandle left, ConstantHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ManifestResourceHandle : IEquatable<ManifestResourceHandle>
    {
        private const uint tokenType = TokenTypeIds.ManifestResource;
        private readonly uint _rowId;

        private ManifestResourceHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ManifestResourceHandle FromRowId(uint rowId)
        {
            return new ManifestResourceHandle(rowId);
        }

        public static implicit operator Handle(ManifestResourceHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator ManifestResourceHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ManifestResourceHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(ManifestResourceHandle left, ManifestResourceHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ManifestResourceHandle && ((ManifestResourceHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ManifestResourceHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ManifestResourceHandle left, ManifestResourceHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct AssemblyFileHandle : IEquatable<AssemblyFileHandle>
    {
        private const uint tokenType = TokenTypeIds.File;
        private readonly uint _rowId;

        private AssemblyFileHandle(uint rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static AssemblyFileHandle FromRowId(uint rowId)
        {
            return new AssemblyFileHandle(rowId);
        }

        public static implicit operator Handle(AssemblyFileHandle handle)
        {
            return new Handle(handle._rowId | tokenType);
        }

        public static explicit operator AssemblyFileHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyFileHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal uint RowId { get { return _rowId; } }

        public static bool operator ==(AssemblyFileHandle left, AssemblyFileHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyFileHandle && ((AssemblyFileHandle)obj)._rowId == _rowId;
        }

        public bool Equals(AssemblyFileHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(AssemblyFileHandle left, AssemblyFileHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    // #UserString heap handle
    public struct UserStringHandle : IEquatable<UserStringHandle>
    {
        private const uint tokenType = TokenTypeIds.UserString;
        private readonly uint _token;

        private UserStringHandle(uint token)
        {
            Debug.Assert((token & TokenTypeIds.TokenTypeMask) == tokenType);
            _token = token;
        }

        internal static UserStringHandle FromIndex(uint heapIndex)
        {
            return new UserStringHandle(heapIndex | tokenType);
        }

        public static implicit operator Handle(UserStringHandle handle)
        {
            return new Handle(handle._token);
        }

        public static explicit operator UserStringHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new UserStringHandle(handle.value);
        }

        public bool IsNil
        {
            get
            {
                return (_token & TokenTypeIds.RIDMask) == 0;
            }
        }

        internal int Index
        {
            get
            {
                return (int)(_token & TokenTypeIds.RIDMask);
            }
        }

        public static bool operator ==(UserStringHandle left, UserStringHandle right)
        {
            return left._token == right._token;
        }

        public override bool Equals(object obj)
        {
            return obj is UserStringHandle && ((UserStringHandle)obj)._token == _token;
        }

        public bool Equals(UserStringHandle other)
        {
            return _token == other._token;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        public static bool operator !=(UserStringHandle left, UserStringHandle right)
        {
            return left._token != right._token;
        }
    }

    // #String heap handle
    public struct StringHandle : IEquatable<StringHandle>
    {
        // bits:
        //  0..24: Heap index or Virtual index
        // 25..30: TokenTypeId: String, WinRTPrefixedString
        //     31: IsVirtual
        private readonly uint _token;

        internal enum VirtualIndex
        {
            System_Runtime_WindowsRuntime,
            System_Runtime,
            System_ObjectModel,
            System_Runtime_WindowsRuntime_UI_Xaml,
            System_Runtime_InteropServices_WindowsRuntime,
            System_Numerics_Vectors,

            Dispose,

            AttributeTargets,
            AttributeUsageAttribute,
            Color,
            CornerRadius,
            DateTimeOffset,
            Duration,
            DurationType,
            EventHandler1,
            EventRegistrationToken,
            Exception,
            GeneratorPosition,
            GridLength,
            GridUnitType,
            ICommand,
            IDictionary2,
            IDisposable,
            IEnumerable,
            IEnumerable1,
            IList,
            IList1,
            INotifyCollectionChanged,
            INotifyPropertyChanged,
            IReadOnlyDictionary2,
            IReadOnlyList1,
            KeyTime,
            KeyValuePair2,
            Matrix,
            Matrix3D,
            Matrix3x2,
            Matrix4x4,
            NotifyCollectionChangedAction,
            NotifyCollectionChangedEventArgs,
            NotifyCollectionChangedEventHandler,
            Nullable1,
            Plane,
            Point,
            PropertyChangedEventArgs,
            PropertyChangedEventHandler,
            Quaternion,
            Rect,
            RepeatBehavior,
            RepeatBehaviorType,
            Size,
            System,
            System_Collections,
            System_Collections_Generic,
            System_Collections_Specialized,
            System_ComponentModel,
            System_Numerics,
            System_Windows_Input,
            Thickness,
            TimeSpan,
            Type,
            Uri,
            Vector2,
            Vector3,
            Vector4,
            Windows_Foundation,
            Windows_UI,
            Windows_UI_Xaml,
            Windows_UI_Xaml_Controls_Primitives,
            Windows_UI_Xaml_Media,
            Windows_UI_Xaml_Media_Animation,
            Windows_UI_Xaml_Media_Media3D,

            Count
        }

        private StringHandle(uint token)
        {
            Debug.Assert((token & TokenTypeIds.TokenTypeMask) == TokenTypeIds.String ||
                         (token & TokenTypeIds.TokenTypeMask) == TokenTypeIds.WinRTPrefixedString ||
                         (token & TokenTypeIds.TokenTypeMask) == TokenTypeIds.DotTerminatedString);
            _token = token;
        }

        internal static StringHandle FromIndex(uint heapIndex)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(heapIndex));
            return new StringHandle(TokenTypeIds.String | heapIndex);
        }

        internal static StringHandle FromVirtualIndex(VirtualIndex virtualIndex)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new StringHandle(TokenTypeIds.VirtualTokenMask | TokenTypeIds.String | (uint)virtualIndex);
        }

        internal StringHandle WithWinRTPrefix()
        {
            Debug.Assert(!IsVirtual);
            return new StringHandle(TokenTypeIds.VirtualTokenMask | TokenTypeIds.WinRTPrefixedString | (uint)Index);
        }

        internal StringHandle WithDotTermination()
        {
            Debug.Assert(!IsVirtual);
            return new StringHandle(TokenTypeIds.DotTerminatedString | (uint)Index);
        }

        internal StringHandle SuffixRaw(int prefixByteLength)
        {
            Debug.Assert(!IsVirtual);
            return new StringHandle(TokenTypeIds.String | (uint)(Index + prefixByteLength));
        }

        public static implicit operator Handle(StringHandle handle)
        {
            return new Handle(handle._token);
        }

        public static explicit operator StringHandle(Handle handle)
        {
            if (handle.TokenType < TokenTypeIds.String || handle.TokenType > TokenTypeIds.MaxString)
            {
                Handle.ThrowInvalidCast();
            }

            return new StringHandle(handle.value);
        }

        public bool IsNil
        {
            get
            {
                return (_token & (TokenTypeIds.VirtualTokenMask | TokenTypeIds.RIDMask)) == 0;
            }
        }

        internal bool IsVirtual
        {
            get { return (_token & TokenTypeIds.VirtualTokenMask) != 0; }
        }

        internal int Index
        {
            get
            {
                return (int)(_token & TokenTypeIds.RIDMask);
            }
        }

        internal StringKind StringKind
        {
            get
            {
                return (StringKind)((_token & TokenTypeIds.StringOrNamespaceKindMask) >> TokenTypeIds.RowIdBitCount);
            }
        }

        public static bool operator ==(StringHandle left, StringHandle right)
        {
            return left._token == right._token;
        }

        public override bool Equals(object obj)
        {
            return obj is StringHandle && ((StringHandle)obj)._token == _token;
        }

        public bool Equals(StringHandle other)
        {
            return _token == other._token;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        public static bool operator !=(StringHandle left, StringHandle right)
        {
            return left._token != right._token;
        }
    }

    /// <summary>
    /// A handle that represents a namespace definition. 
    /// </summary>
    public struct NamespaceDefinitionHandle : IEquatable<NamespaceDefinitionHandle>
    {
        //
        // bits:
        //  0..24: Heap index or Virtual index
        // 25..30: TokenTypeId: Namespace or SyntheticNamespace
        //     31: IsVirtual
        //
        // At this time, IsVirtual is always false because namespace names come from type definitions 
        // and type forwarders only, which never get their namespaces projected.
        //
        // For standard Namespace TokenTypeId, the index is to the null-terminated full name of the 
        // namespace in the string heap.
        //
        // For SyntheticNamespace, the index points to the dot-terminated simple name of the namespace
        // in the string heap. This is used to represent namespaces that are parents of other namespaces
        // but no type definitions or forwarders of their own.
        //
        private readonly uint _token;

        private NamespaceDefinitionHandle(uint token)
        {
            Debug.Assert((token & TokenTypeIds.TokenTypeMask) == TokenTypeIds.Namespace ||
                         (token & TokenTypeIds.TokenTypeMask) == TokenTypeIds.SyntheticNamespace);

            _token = token;
        }

        internal static NamespaceDefinitionHandle FromIndexOfFullName(uint stringHeapIndex)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(stringHeapIndex));
            return new NamespaceDefinitionHandle(TokenTypeIds.Namespace | stringHeapIndex);
        }

        internal static NamespaceDefinitionHandle FromIndexOfSimpleName(uint stringHeapIndex)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(stringHeapIndex));
            return new NamespaceDefinitionHandle(TokenTypeIds.SyntheticNamespace | stringHeapIndex);
        }

        public static implicit operator Handle(NamespaceDefinitionHandle handle)
        {
            return new Handle(handle._token);
        }

        public static explicit operator NamespaceDefinitionHandle(Handle handle)
        {
            if (handle.TokenType < TokenTypeIds.Namespace || handle.TokenType > TokenTypeIds.MaxNamespace)
            {
                Handle.ThrowInvalidCast();
            }

            return new NamespaceDefinitionHandle(handle.value);
        }

        public bool IsNil
        {
            get
            {
                return (_token & (TokenTypeIds.VirtualTokenMask | TokenTypeIds.RIDMask)) == 0;
            }
        }

        internal int Index
        {
            get
            {
                return (int)(_token & TokenTypeIds.RIDMask);
            }
        }

        internal bool IsVirtual
        {
            get
            {
                return (_token & TokenTypeIds.VirtualTokenMask) != 0;
            }
        }

        internal NamespaceKind NamespaceKind
        {
            get
            {
                return (NamespaceKind)((_token & TokenTypeIds.StringOrNamespaceKindMask) >> TokenTypeIds.RowIdBitCount);
            }
        }

        internal bool HasFullName
        {
            get
            {
                return (_token & (TokenTypeIds.TokenTypeMask)) != TokenTypeIds.SyntheticNamespace;
            }
        }

        internal StringHandle GetFullName()
        {
            Debug.Assert(!IsVirtual);
            Debug.Assert(HasFullName);
            return StringHandle.FromIndex((uint)Index);
        }

        public static bool operator ==(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
        {
            return left._token == right._token;
        }

        public int CompareTo(NamespaceDefinitionHandle other)
        {
            return TokenTypeIds.CompareTokens(_token, other._token);
        }

        public override bool Equals(object obj)
        {
            return obj is NamespaceDefinitionHandle && ((NamespaceDefinitionHandle)obj)._token == _token;
        }

        public bool Equals(NamespaceDefinitionHandle other)
        {
            return _token == other._token;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        public static bool operator !=(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
        {
            return left._token != right._token;
        }
    }

    // #Blob heap handle
    public struct BlobHandle : IEquatable<BlobHandle>
    {
        private const uint tokenType = TokenTypeIds.Blob;

        // bits:
        //  0..24: Heap index or Virtual Value (16 bits) + Virtual Index (8 bits)
        // 25..30: TokenTypeId: Blob
        //     31: IsVirtual
        private readonly uint _token;

        internal enum VirtualIndex : byte
        {
            Nil,

            // B0 3F 5F 7F 11 D5 0A 3A
            ContractPublicKeyToken,

            // 00, 24, 00, 00, 04, ...
            ContractPublicKey,

            // Template for projected AttributeUsage attribute blob
            AttributeUsage_AllowSingle,

            // Template for projected AttributeUsage attribute blob with AllowMultiple=true
            AttributeUsage_AllowMultiple,

            Count
        }

        private BlobHandle(uint token)
        {
            Debug.Assert((token & TokenTypeIds.TokenTypeMask) == tokenType);
            _token = token;
        }

        internal static BlobHandle FromIndex(uint heapIndex)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(heapIndex));
            return new BlobHandle(heapIndex | tokenType);
        }

        internal static BlobHandle FromVirtualIndex(VirtualIndex virtualIndex, ushort virtualValue)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new BlobHandle(TokenTypeIds.VirtualTokenMask | tokenType | (uint)(virtualValue << 8) | (uint)virtualIndex);
        }

        internal const int TemplateParameterOffset_AttributeUsageTarget = 2;

        internal unsafe void SubstituteTemplateParameters(byte[] blob)
        {
            Debug.Assert(blob.Length >= TemplateParameterOffset_AttributeUsageTarget + 4);

            fixed (byte* ptr = &blob[TemplateParameterOffset_AttributeUsageTarget])
            {
                *((uint*)ptr) = VirtualValue;
            }
        }

        public static implicit operator Handle(BlobHandle handle)
        {
            return new Handle(handle._token);
        }

        public static explicit operator BlobHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new BlobHandle(handle.value);
        }

        public bool IsNil
        {
            get
            {
                return (_token & (TokenTypeIds.VirtualTokenMask | TokenTypeIds.RIDMask)) == 0;
            }
        }

        internal int Index
        {
            get
            {
                Debug.Assert(!IsVirtual);
                return (int)(_token & TokenTypeIds.RIDMask);
            }
        }

        internal bool IsVirtual
        {
            get
            {
                return (_token & TokenTypeIds.VirtualTokenMask) != 0;
            }
        }

        internal VirtualIndex GetVirtualIndex()
        {
            Debug.Assert(IsVirtual);
            return (VirtualIndex)(_token & 0xff);
        }

        private ushort VirtualValue
        {
            get { return unchecked((ushort)(_token >> 8)); }
        }

        public static bool operator ==(BlobHandle left, BlobHandle right)
        {
            return left._token == right._token;
        }

        public override bool Equals(object obj)
        {
            return obj is BlobHandle && ((BlobHandle)obj)._token == _token;
        }

        public bool Equals(BlobHandle other)
        {
            return _token == other._token;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        public static bool operator !=(BlobHandle left, BlobHandle right)
        {
            return left._token != right._token;
        }
    }

    // #Guid heap handle
    public struct GuidHandle : IEquatable<GuidHandle>
    {
        private const uint tokenType = TokenTypeIds.Guid;
        private readonly uint _token;

        private GuidHandle(uint token)
        {
            Debug.Assert((token & TokenTypeIds.TokenTypeMask) == tokenType);
            _token = token;
        }

        internal static GuidHandle FromIndex(uint heapIndex)
        {
            return new GuidHandle(heapIndex | tokenType);
        }

        public static implicit operator Handle(GuidHandle handle)
        {
            return new Handle(handle._token);
        }

        public static explicit operator GuidHandle(Handle handle)
        {
            if (handle.TokenType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new GuidHandle(handle.value);
        }

        public bool IsNil
        {
            get
            {
                return (_token & (TokenTypeIds.VirtualTokenMask | TokenTypeIds.RIDMask)) == 0;
            }
        }

        internal int Index
        {
            get
            {
                return (int)(_token & TokenTypeIds.RIDMask);
            }
        }

        public static bool operator ==(GuidHandle left, GuidHandle right)
        {
            return left._token == right._token;
        }

        public override bool Equals(object obj)
        {
            return obj is GuidHandle && ((GuidHandle)obj)._token == _token;
        }

        public bool Equals(GuidHandle other)
        {
            return _token == other._token;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        public static bool operator !=(GuidHandle left, GuidHandle right)
        {
            return left._token != right._token;
        }
    }
}
