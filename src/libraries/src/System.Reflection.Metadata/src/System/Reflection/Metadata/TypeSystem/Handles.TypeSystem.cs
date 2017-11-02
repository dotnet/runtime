// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct ModuleDefinitionHandle : IEquatable<ModuleDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Module;
        private const byte tokenTypeSmall = (byte)HandleType.Module;
        private readonly int _rowId;

        internal ModuleDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ModuleDefinitionHandle FromRowId(int rowId)
        {
            return new ModuleDefinitionHandle(rowId);
        }

        public static implicit operator Handle(ModuleDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ModuleDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ModuleDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new ModuleDefinitionHandle(handle.RowId);
        }

        public static explicit operator ModuleDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct AssemblyDefinitionHandle : IEquatable<AssemblyDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Assembly;
        private const byte tokenTypeSmall = (byte)HandleType.Assembly;
        private readonly int _rowId;

        internal AssemblyDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static AssemblyDefinitionHandle FromRowId(int rowId)
        {
            return new AssemblyDefinitionHandle(rowId);
        }

        public static implicit operator Handle(AssemblyDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(AssemblyDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator AssemblyDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new AssemblyDefinitionHandle(handle.RowId);
        }

        public static explicit operator AssemblyDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct InterfaceImplementationHandle : IEquatable<InterfaceImplementationHandle>
    {
        private const uint tokenType = TokenTypeIds.InterfaceImpl;
        private const byte tokenTypeSmall = (byte)HandleType.InterfaceImpl;
        private readonly int _rowId;

        internal InterfaceImplementationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static InterfaceImplementationHandle FromRowId(int rowId)
        {
            return new InterfaceImplementationHandle(rowId);
        }

        public static implicit operator Handle(InterfaceImplementationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(InterfaceImplementationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator InterfaceImplementationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new InterfaceImplementationHandle(handle.RowId);
        }

        public static explicit operator InterfaceImplementationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct MethodDefinitionHandle : IEquatable<MethodDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodDef;
        private const byte tokenTypeSmall = (byte)HandleType.MethodDef;
        private readonly int _rowId;

        private MethodDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodDefinitionHandle FromRowId(int rowId)
        {
            return new MethodDefinitionHandle(rowId);
        }

        public static implicit operator Handle(MethodDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MethodDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MethodDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new MethodDefinitionHandle(handle.RowId);
        }

        public static explicit operator MethodDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

        /// <summary>
        /// Returns a handle to <see cref="MethodDebugInformation"/> corresponding to this handle.
        /// </summary>
        /// <remarks>
        /// The resulting handle is only valid within the context of a <see cref="MetadataReader"/> open on the Portable PDB blob,
        /// which in case of standalone PDB file is a different reader than the one containing this method definition.
        /// </remarks>
        public MethodDebugInformationHandle ToDebugInformationHandle()
        {
            return MethodDebugInformationHandle.FromRowId(_rowId);
        }
    }

    public readonly struct MethodImplementationHandle : IEquatable<MethodImplementationHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodImpl;
        private const byte tokenTypeSmall = (byte)HandleType.MethodImpl;
        private readonly int _rowId;

        private MethodImplementationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodImplementationHandle FromRowId(int rowId)
        {
            return new MethodImplementationHandle(rowId);
        }

        public static implicit operator Handle(MethodImplementationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MethodImplementationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MethodImplementationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new MethodImplementationHandle(handle.RowId);
        }

        public static explicit operator MethodImplementationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct MethodSpecificationHandle : IEquatable<MethodSpecificationHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodSpec;
        private const byte tokenTypeSmall = (byte)HandleType.MethodSpec;
        private readonly int _rowId;

        private MethodSpecificationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodSpecificationHandle FromRowId(int rowId)
        {
            return new MethodSpecificationHandle(rowId);
        }

        public static implicit operator Handle(MethodSpecificationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MethodSpecificationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MethodSpecificationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new MethodSpecificationHandle(handle.RowId);
        }

        public static explicit operator MethodSpecificationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct TypeDefinitionHandle : IEquatable<TypeDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeDef;
        private const byte tokenTypeSmall = (byte)HandleType.TypeDef;
        private readonly int _rowId;

        private TypeDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeDefinitionHandle FromRowId(int rowId)
        {
            return new TypeDefinitionHandle(rowId);
        }

        public static implicit operator Handle(TypeDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(TypeDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator TypeDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new TypeDefinitionHandle(handle.RowId);
        }

        public static explicit operator TypeDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct ExportedTypeHandle : IEquatable<ExportedTypeHandle>
    {
        private const uint tokenType = TokenTypeIds.ExportedType;
        private const byte tokenTypeSmall = (byte)HandleType.ExportedType;
        private readonly int _rowId;

        private ExportedTypeHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ExportedTypeHandle FromRowId(int rowId)
        {
            return new ExportedTypeHandle(rowId);
        }

        public static implicit operator Handle(ExportedTypeHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ExportedTypeHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ExportedTypeHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new ExportedTypeHandle(handle.RowId);
        }

        public static explicit operator ExportedTypeHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct TypeReferenceHandle : IEquatable<TypeReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeRef;
        private const byte tokenTypeSmall = (byte)HandleType.TypeRef;
        private readonly int _rowId;

        private TypeReferenceHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeReferenceHandle FromRowId(int rowId)
        {
            return new TypeReferenceHandle(rowId);
        }

        public static implicit operator Handle(TypeReferenceHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(TypeReferenceHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator TypeReferenceHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new TypeReferenceHandle(handle.RowId);
        }

        public static explicit operator TypeReferenceHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct TypeSpecificationHandle : IEquatable<TypeSpecificationHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeSpec;
        private const byte tokenTypeSmall = (byte)HandleType.TypeSpec;
        private readonly int _rowId;

        private TypeSpecificationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeSpecificationHandle FromRowId(int rowId)
        {
            return new TypeSpecificationHandle(rowId);
        }

        public static implicit operator Handle(TypeSpecificationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(TypeSpecificationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator TypeSpecificationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new TypeSpecificationHandle(handle.RowId);
        }

        public static explicit operator TypeSpecificationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct MemberReferenceHandle : IEquatable<MemberReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.MemberRef;
        private const byte tokenTypeSmall = (byte)HandleType.MemberRef;
        private readonly int _rowId;

        private MemberReferenceHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MemberReferenceHandle FromRowId(int rowId)
        {
            return new MemberReferenceHandle(rowId);
        }

        public static implicit operator Handle(MemberReferenceHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MemberReferenceHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MemberReferenceHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new MemberReferenceHandle(handle.RowId);
        }

        public static explicit operator MemberReferenceHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct FieldDefinitionHandle : IEquatable<FieldDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.FieldDef;
        private const byte tokenTypeSmall = (byte)HandleType.FieldDef;
        private readonly int _rowId;

        private FieldDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static FieldDefinitionHandle FromRowId(int rowId)
        {
            return new FieldDefinitionHandle(rowId);
        }

        public static implicit operator Handle(FieldDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(FieldDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator FieldDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new FieldDefinitionHandle(handle.RowId);
        }

        public static explicit operator FieldDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct EventDefinitionHandle : IEquatable<EventDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Event;
        private const byte tokenTypeSmall = (byte)HandleType.Event;
        private readonly int _rowId;

        private EventDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static EventDefinitionHandle FromRowId(int rowId)
        {
            return new EventDefinitionHandle(rowId);
        }

        public static implicit operator Handle(EventDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(EventDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator EventDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new EventDefinitionHandle(handle.RowId);
        }

        public static explicit operator EventDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct PropertyDefinitionHandle : IEquatable<PropertyDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Property;
        private const byte tokenTypeSmall = (byte)HandleType.Property;
        private readonly int _rowId;

        private PropertyDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static PropertyDefinitionHandle FromRowId(int rowId)
        {
            return new PropertyDefinitionHandle(rowId);
        }

        public static implicit operator Handle(PropertyDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(PropertyDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator PropertyDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new PropertyDefinitionHandle(handle.RowId);
        }

        public static explicit operator PropertyDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct StandaloneSignatureHandle : IEquatable<StandaloneSignatureHandle>
    {
        private const uint tokenType = TokenTypeIds.Signature;
        private const byte tokenTypeSmall = (byte)HandleType.Signature;
        private readonly int _rowId;

        private StandaloneSignatureHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static StandaloneSignatureHandle FromRowId(int rowId)
        {
            return new StandaloneSignatureHandle(rowId);
        }

        public static implicit operator Handle(StandaloneSignatureHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(StandaloneSignatureHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator StandaloneSignatureHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new StandaloneSignatureHandle(handle.RowId);
        }

        public static explicit operator StandaloneSignatureHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct ParameterHandle : IEquatable<ParameterHandle>
    {
        private const uint tokenType = TokenTypeIds.ParamDef;
        private const byte tokenTypeSmall = (byte)HandleType.ParamDef;
        private readonly int _rowId;

        private ParameterHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ParameterHandle FromRowId(int rowId)
        {
            return new ParameterHandle(rowId);
        }

        public static implicit operator Handle(ParameterHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ParameterHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ParameterHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new ParameterHandle(handle.RowId);
        }

        public static explicit operator ParameterHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct GenericParameterHandle : IEquatable<GenericParameterHandle>
    {
        private const uint tokenType = TokenTypeIds.GenericParam;
        private const byte tokenTypeSmall = (byte)HandleType.GenericParam;
        private readonly int _rowId;

        private GenericParameterHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static GenericParameterHandle FromRowId(int rowId)
        {
            return new GenericParameterHandle(rowId);
        }

        public static implicit operator Handle(GenericParameterHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(GenericParameterHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator GenericParameterHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new GenericParameterHandle(handle.RowId);
        }

        public static explicit operator GenericParameterHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct GenericParameterConstraintHandle : IEquatable<GenericParameterConstraintHandle>
    {
        private const uint tokenType = TokenTypeIds.GenericParamConstraint;
        private const byte tokenTypeSmall = (byte)HandleType.GenericParamConstraint;
        private readonly int _rowId;

        private GenericParameterConstraintHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static GenericParameterConstraintHandle FromRowId(int rowId)
        {
            return new GenericParameterConstraintHandle(rowId);
        }

        public static implicit operator Handle(GenericParameterConstraintHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(GenericParameterConstraintHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator GenericParameterConstraintHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new GenericParameterConstraintHandle(handle.RowId);
        }

        public static explicit operator GenericParameterConstraintHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct ModuleReferenceHandle : IEquatable<ModuleReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.ModuleRef;
        private const byte tokenTypeSmall = (byte)HandleType.ModuleRef;
        private readonly int _rowId;

        private ModuleReferenceHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ModuleReferenceHandle FromRowId(int rowId)
        {
            return new ModuleReferenceHandle(rowId);
        }

        public static implicit operator Handle(ModuleReferenceHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ModuleReferenceHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ModuleReferenceHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new ModuleReferenceHandle(handle.RowId);
        }

        public static explicit operator ModuleReferenceHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct AssemblyReferenceHandle : IEquatable<AssemblyReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.AssemblyRef;
        private const byte tokenTypeSmall = (byte)HandleType.AssemblyRef;

        // bits:
        //     31: IsVirtual
        // 24..30: 0
        //  0..23: Heap offset or Virtual index
        private readonly uint _value;

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

        private AssemblyReferenceHandle(uint value)
        {
            _value = value;
        }

        internal static AssemblyReferenceHandle FromRowId(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            return new AssemblyReferenceHandle((uint)rowId);
        }

        internal static AssemblyReferenceHandle FromVirtualIndex(VirtualIndex virtualIndex)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new AssemblyReferenceHandle(TokenTypeIds.VirtualBit | (uint)virtualIndex);
        }

        public static implicit operator Handle(AssemblyReferenceHandle handle)
        {
            return Handle.FromVToken(handle.VToken);
        }

        public static implicit operator EntityHandle(AssemblyReferenceHandle handle)
        {
            return new EntityHandle(handle.VToken);
        }

        public static explicit operator AssemblyReferenceHandle(Handle handle)
        {
            if (handle.Type != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new AssemblyReferenceHandle(handle.SpecificEntityHandleValue);
        }

        public static explicit operator AssemblyReferenceHandle(EntityHandle handle)
        {
            if (handle.Type != tokenType)
            {
                Throw.InvalidCast();
            }

            return new AssemblyReferenceHandle(handle.SpecificHandleValue);
        }

        internal uint Value
        {
            get { return _value; }
        }

        private uint VToken
        {
            get { return _value | tokenType; }
        }

        public bool IsNil
        {
            get { return _value == 0; }
        }

        internal bool IsVirtual
        {
            get { return (_value & TokenTypeIds.VirtualBit) != 0; }
        }

        internal int RowId { get { return (int)(_value & TokenTypeIds.RIDMask); } }

        public static bool operator ==(AssemblyReferenceHandle left, AssemblyReferenceHandle right)
        {
            return left._value == right._value;
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyReferenceHandle && ((AssemblyReferenceHandle)obj)._value == _value;
        }

        public bool Equals(AssemblyReferenceHandle other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static bool operator !=(AssemblyReferenceHandle left, AssemblyReferenceHandle right)
        {
            return left._value != right._value;
        }
    }

    public readonly struct CustomAttributeHandle : IEquatable<CustomAttributeHandle>
    {
        private const uint tokenType = TokenTypeIds.CustomAttribute;
        private const byte tokenTypeSmall = (byte)HandleType.CustomAttribute;
        private readonly int _rowId;

        private CustomAttributeHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static CustomAttributeHandle FromRowId(int rowId)
        {
            return new CustomAttributeHandle(rowId);
        }

        public static implicit operator Handle(CustomAttributeHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(CustomAttributeHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator CustomAttributeHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new CustomAttributeHandle(handle.RowId);
        }

        public static explicit operator CustomAttributeHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct DeclarativeSecurityAttributeHandle : IEquatable<DeclarativeSecurityAttributeHandle>
    {
        private const uint tokenType = TokenTypeIds.DeclSecurity;
        private const byte tokenTypeSmall = (byte)HandleType.DeclSecurity;
        private readonly int _rowId;

        private DeclarativeSecurityAttributeHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static DeclarativeSecurityAttributeHandle FromRowId(int rowId)
        {
            return new DeclarativeSecurityAttributeHandle(rowId);
        }

        public static implicit operator Handle(DeclarativeSecurityAttributeHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(DeclarativeSecurityAttributeHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator DeclarativeSecurityAttributeHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new DeclarativeSecurityAttributeHandle(handle.RowId);
        }

        public static explicit operator DeclarativeSecurityAttributeHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct ConstantHandle : IEquatable<ConstantHandle>
    {
        private const uint tokenType = TokenTypeIds.Constant;
        private const byte tokenTypeSmall = (byte)HandleType.Constant;
        private readonly int _rowId;

        private ConstantHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ConstantHandle FromRowId(int rowId)
        {
            return new ConstantHandle(rowId);
        }

        public static implicit operator Handle(ConstantHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ConstantHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ConstantHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new ConstantHandle(handle.RowId);
        }

        public static explicit operator ConstantHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct ManifestResourceHandle : IEquatable<ManifestResourceHandle>
    {
        private const uint tokenType = TokenTypeIds.ManifestResource;
        private const byte tokenTypeSmall = (byte)HandleType.ManifestResource;
        private readonly int _rowId;

        private ManifestResourceHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ManifestResourceHandle FromRowId(int rowId)
        {
            return new ManifestResourceHandle(rowId);
        }

        public static implicit operator Handle(ManifestResourceHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ManifestResourceHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ManifestResourceHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new ManifestResourceHandle(handle.RowId);
        }

        public static explicit operator ManifestResourceHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    public readonly struct AssemblyFileHandle : IEquatable<AssemblyFileHandle>
    {
        private const uint tokenType = TokenTypeIds.File;
        private const byte tokenTypeSmall = (byte)HandleType.File;
        private readonly int _rowId;

        private AssemblyFileHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static AssemblyFileHandle FromRowId(int rowId)
        {
            return new AssemblyFileHandle(rowId);
        }

        public static implicit operator Handle(AssemblyFileHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(AssemblyFileHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator AssemblyFileHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new AssemblyFileHandle(handle.RowId);
        }

        public static explicit operator AssemblyFileHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
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

        internal int RowId { get { return _rowId; } }

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

    /// <summary>
    /// #UserString heap handle.
    /// </summary>
    /// <remarks>
    /// The handle is 32-bit wide.
    /// </remarks>
    public readonly struct UserStringHandle : IEquatable<UserStringHandle>
    {
        // bits:
        //     31: 0
        // 24..30: 0
        //  0..23: index
        private readonly int _offset;

        private UserStringHandle(int offset)
        {
            // #US string indices must fit into 24bits since they are used in IL stream tokens
            Debug.Assert((offset & 0xFF000000) == 0);
            _offset = offset;
        }

        internal static UserStringHandle FromOffset(int heapOffset)
        {
            return new UserStringHandle(heapOffset);
        }

        public static implicit operator Handle(UserStringHandle handle)
        {
            return new Handle((byte)HandleType.UserString, handle._offset);
        }

        public static explicit operator UserStringHandle(Handle handle)
        {
            if (handle.VType != HandleType.UserString)
            {
                Throw.InvalidCast();
            }

            return new UserStringHandle(handle.Offset);
        }

        public bool IsNil
        {
            get { return _offset == 0; }
        }

        internal int GetHeapOffset()
        {
            return _offset;
        }

        public static bool operator ==(UserStringHandle left, UserStringHandle right)
        {
            return left._offset == right._offset;
        }

        public override bool Equals(object obj)
        {
            return obj is UserStringHandle && ((UserStringHandle)obj)._offset == _offset;
        }

        public bool Equals(UserStringHandle other)
        {
            return _offset == other._offset;
        }

        public override int GetHashCode()
        {
            return _offset.GetHashCode();
        }

        public static bool operator !=(UserStringHandle left, UserStringHandle right)
        {
            return left._offset != right._offset;
        }
    }

    // #String heap handle
    public readonly struct StringHandle : IEquatable<StringHandle>
    {
        // bits:
        //     31: IsVirtual
        // 29..31: type (non-virtual: String, DotTerminatedString; virtual: VirtualString, WinRTPrefixedString)
        //  0..28: Heap offset or Virtual index
        private readonly uint _value;

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

        private StringHandle(uint value)
        {
            Debug.Assert((value & StringHandleType.TypeMask) == StringHandleType.String ||
                         (value & StringHandleType.TypeMask) == StringHandleType.VirtualString ||
                         (value & StringHandleType.TypeMask) == StringHandleType.WinRTPrefixedString ||
                         (value & StringHandleType.TypeMask) == StringHandleType.DotTerminatedString);

            _value = value;
        }

        internal static StringHandle FromOffset(int heapOffset)
        {
            return new StringHandle(StringHandleType.String | (uint)heapOffset);
        }

        internal static StringHandle FromVirtualIndex(VirtualIndex virtualIndex)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new StringHandle(StringHandleType.VirtualString | (uint)virtualIndex);
        }

        internal static StringHandle FromWriterVirtualIndex(int virtualIndex)
        {
            return new StringHandle(StringHandleType.VirtualString | (uint)virtualIndex);
        }

        internal StringHandle WithWinRTPrefix()
        {
            Debug.Assert(StringKind == StringKind.Plain);
            return new StringHandle(StringHandleType.WinRTPrefixedString | _value);
        }

        internal StringHandle WithDotTermination()
        {
            Debug.Assert(StringKind == StringKind.Plain);
            return new StringHandle(StringHandleType.DotTerminatedString | _value);
        }

        internal StringHandle SuffixRaw(int prefixByteLength)
        {
            Debug.Assert(StringKind == StringKind.Plain);
            Debug.Assert(prefixByteLength >= 0);
            return new StringHandle(StringHandleType.String | (_value + (uint)prefixByteLength));
        }

        public static implicit operator Handle(StringHandle handle)
        {
            // VTTx xxxx xxxx xxxx  xxxx xxxx xxxx xxxx -> V111 10TT
            return new Handle(
                (byte)((handle._value & HeapHandleType.VirtualBit) >> 24 | HandleType.String | (handle._value & StringHandleType.NonVirtualTypeMask) >> HeapHandleType.OffsetBitCount),
                (int)(handle._value & HeapHandleType.OffsetMask));
        }

        public static explicit operator StringHandle(Handle handle)
        {
            if ((handle.VType & ~(HandleType.VirtualBit | HandleType.NonVirtualStringTypeMask)) != HandleType.String)
            {
                Throw.InvalidCast();
            }

            // V111 10TT -> VTTx xxxx xxxx xxxx  xxxx xxxx xxxx xxxx
            return new StringHandle(
                (handle.VType & HandleType.VirtualBit) << 24 | 
                (handle.VType & HandleType.NonVirtualStringTypeMask) << HeapHandleType.OffsetBitCount | 
                (uint)handle.Offset);
        }

        internal uint RawValue => _value;

        internal bool IsVirtual
        {
            get { return (_value & HeapHandleType.VirtualBit) != 0; }
        }

        public bool IsNil
        {
            get
            {
                // virtual strings are never nil, so include virtual bit
                return (_value & (HeapHandleType.VirtualBit | HeapHandleType.OffsetMask)) == 0;
            }
        }

        internal int GetHeapOffset()
        {
            // WinRT prefixed strings are virtual, the value is a heap offset
            Debug.Assert(!IsVirtual || StringKind == StringKind.WinRTPrefixed);
            return (int)(_value & HeapHandleType.OffsetMask);
        }

        internal VirtualIndex GetVirtualIndex()
        {
            Debug.Assert(IsVirtual && StringKind != StringKind.WinRTPrefixed);
            return (VirtualIndex)(_value & HeapHandleType.OffsetMask);
        }

        internal int GetWriterVirtualIndex()
        {
            Debug.Assert(IsNil || IsVirtual && StringKind == StringKind.Virtual);
            return (int)(_value & HeapHandleType.OffsetMask);
        }

        internal StringKind StringKind
        {
            get { return (StringKind)(_value >> HeapHandleType.OffsetBitCount); }
        }

        public override bool Equals(object obj)
        {
            return obj is StringHandle && Equals((StringHandle)obj);
        }

        public bool Equals(StringHandle other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)_value);
        }

        public static bool operator ==(StringHandle left, StringHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringHandle left, StringHandle right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// A handle that represents a namespace definition. 
    /// </summary>
    public readonly struct NamespaceDefinitionHandle : IEquatable<NamespaceDefinitionHandle>
    {
        // Non-virtual (namespace having at least one type or forwarder of its own) 
        // heap offset is to the null-terminated full name of the namespace in the 
        // #String heap.
        //
        // Virtual (namespace having child namespaces but no types of its own) 
        // the virtual index is an auto-incremented value and serves solely to 
        // create unique values for indexing into the NamespaceCache.

        // bits:
        //     31: IsVirtual
        // 29..31: 0
        //  0..28: Heap offset or Virtual index
        private readonly uint _value;

        private NamespaceDefinitionHandle(uint value)
        {
            _value = value;
        }

        internal static NamespaceDefinitionHandle FromFullNameOffset(int stringHeapOffset)
        {
            return new NamespaceDefinitionHandle((uint)stringHeapOffset);
        }

        internal static NamespaceDefinitionHandle FromVirtualIndex(uint virtualIndex)
        {
            // we arbitrarily disallow 0 virtual index to simplify nil check.
            Debug.Assert(virtualIndex != 0); 

            if (!HeapHandleType.IsValidHeapOffset(virtualIndex))
            {
                // only a pathological assembly would hit this, but it must fit in 29 bits.
                Throw.TooManySubnamespaces();
            }

            return new NamespaceDefinitionHandle(TokenTypeIds.VirtualBit | virtualIndex);
        }

        public static implicit operator Handle(NamespaceDefinitionHandle handle)
        {
            return new Handle(
                (byte)((handle._value & HeapHandleType.VirtualBit) >> 24 | HandleType.Namespace),
                (int)(handle._value & HeapHandleType.OffsetMask));
        }

        public static explicit operator NamespaceDefinitionHandle(Handle handle)
        {
            if ((handle.VType & HandleType.TypeMask) != HandleType.Namespace)
            {
                Throw.InvalidCast();
            }

            return new NamespaceDefinitionHandle(
                (handle.VType & HandleType.VirtualBit) << TokenTypeIds.RowIdBitCount |
                (uint)handle.Offset);
        }

        public bool IsNil
        {
            get
            {
                return _value == 0;
            }
        }

        internal bool IsVirtual
        {
            get { return (_value & HeapHandleType.VirtualBit) != 0; }
        }

        internal int GetHeapOffset()
        {
            Debug.Assert(!IsVirtual);
            return (int)(_value & HeapHandleType.OffsetMask);
        }

        internal bool HasFullName
        {
            get { return !IsVirtual; }
        }

        internal StringHandle GetFullName()
        {
            Debug.Assert(HasFullName);
            return StringHandle.FromOffset(GetHeapOffset());
        }

        public override bool Equals(object obj)
        {
            return obj is NamespaceDefinitionHandle && Equals((NamespaceDefinitionHandle)obj);
        }

        public bool Equals(NamespaceDefinitionHandle other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)_value);
        }

        public static bool operator ==(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
        {
            return !left.Equals(right);
        }
    }

    // #Blob heap handle
    public readonly struct BlobHandle : IEquatable<BlobHandle>
    {
        // bits:
        //     31: IsVirtual
        // 29..30: 0
        //  0..28: Heap offset or Virtual Value (16 bits) + Virtual Index (8 bits)
        private readonly uint _value;

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

        private BlobHandle(uint value)
        {
            _value = value;
        }

        internal static BlobHandle FromOffset(int heapOffset)
        {
            return new BlobHandle((uint)heapOffset);
        }

        internal static BlobHandle FromVirtualIndex(VirtualIndex virtualIndex, ushort virtualValue)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new BlobHandle(TokenTypeIds.VirtualBit | (uint)(virtualValue << 8) | (uint)virtualIndex);
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
            // V... -> V111 0001
            return new Handle(
                (byte)((handle._value & HeapHandleType.VirtualBit) >> 24 | HandleType.Blob), 
                (int)(handle._value & HeapHandleType.OffsetMask));
        }

        public static explicit operator BlobHandle(Handle handle)
        {
            if ((handle.VType & HandleType.TypeMask) != HandleType.Blob)
            {
                Throw.InvalidCast();
            }

            return new BlobHandle(
                (handle.VType & HandleType.VirtualBit) << TokenTypeIds.RowIdBitCount |
                (uint)handle.Offset);
        }

        internal uint RawValue => _value;

        public bool IsNil
        {
            get { return _value == 0; }
        }

        internal int GetHeapOffset()
        {
            Debug.Assert(!IsVirtual);
            return (int)_value;
        }

        internal VirtualIndex GetVirtualIndex()
        {
            Debug.Assert(IsVirtual);
            return (VirtualIndex)(_value & 0xff);
        }

        internal bool IsVirtual
        {
            get { return (_value & TokenTypeIds.VirtualBit) != 0; }
        }

        private ushort VirtualValue
        {
            get { return unchecked((ushort)(_value >> 8)); }
        }

        public override bool Equals(object obj)
        {
            return obj is BlobHandle && Equals((BlobHandle)obj);
        }

        public bool Equals(BlobHandle other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)_value);
        }

        public static bool operator ==(BlobHandle left, BlobHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlobHandle left, BlobHandle right)
        {
            return !left.Equals(right);
        }
    }

    // #Guid heap handle
    public readonly struct GuidHandle : IEquatable<GuidHandle>
    {
        // The Guid heap is an array of GUIDs, each 16 bytes wide. 
        // Its first element is numbered 1, its second 2, and so on.
        private readonly int _index;

        private GuidHandle(int index)
        {
            _index = index;
        }

        internal static GuidHandle FromIndex(int heapIndex)
        {
            return new GuidHandle(heapIndex);
        }

        public static implicit operator Handle(GuidHandle handle)
        {
            return new Handle((byte)HandleType.Guid, handle._index);
        }

        public static explicit operator GuidHandle(Handle handle)
        {
            if (handle.VType != HandleType.Guid)
            {
                Throw.InvalidCast();
            }

            return new GuidHandle(handle.Offset);
        }

        public bool IsNil
        {
            get { return _index == 0; }
        }

        internal int Index
        {
            get { return _index; }
        }

        public override bool Equals(object obj)
        {
            return obj is GuidHandle && Equals((GuidHandle)obj);
        }

        public bool Equals(GuidHandle other)
        {
            return _index == other._index;
        }

        public override int GetHashCode()
        {
            return _index;
        }

        public static bool operator ==(GuidHandle left, GuidHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GuidHandle left, GuidHandle right)
        {
            return !left.Equals(right);
        }
    }
}
