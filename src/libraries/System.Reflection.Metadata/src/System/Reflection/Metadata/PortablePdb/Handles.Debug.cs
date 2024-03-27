// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct DocumentHandle : IEquatable<DocumentHandle>
    {
        private const uint tokenType = TokenTypeIds.Document;
        private const byte tokenTypeSmall = (byte)HandleType.Document;
        private readonly int _rowId;

        private DocumentHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static DocumentHandle FromRowId(int rowId)
        {
            return new DocumentHandle(rowId);
        }

        public static implicit operator Handle(DocumentHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(DocumentHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator DocumentHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new DocumentHandle(handle.RowId);
        }

        public static explicit operator DocumentHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
            }

            return new DocumentHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(DocumentHandle left, DocumentHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object? obj)
        {
            return obj is DocumentHandle documentHandle && documentHandle._rowId == _rowId;
        }

        public bool Equals(DocumentHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(DocumentHandle left, DocumentHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public readonly struct MethodDebugInformationHandle : IEquatable<MethodDebugInformationHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodDebugInformation;
        private const byte tokenTypeSmall = (byte)HandleType.MethodDebugInformation;
        private readonly int _rowId;

        private MethodDebugInformationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodDebugInformationHandle FromRowId(int rowId)
        {
            return new MethodDebugInformationHandle(rowId);
        }

        public static implicit operator Handle(MethodDebugInformationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MethodDebugInformationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MethodDebugInformationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new MethodDebugInformationHandle(handle.RowId);
        }

        public static explicit operator MethodDebugInformationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
            }

            return new MethodDebugInformationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(MethodDebugInformationHandle left, MethodDebugInformationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object? obj)
        {
            return obj is MethodDebugInformationHandle mdih && mdih._rowId == _rowId;
        }

        public bool Equals(MethodDebugInformationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MethodDebugInformationHandle left, MethodDebugInformationHandle right)
        {
            return left._rowId != right._rowId;
        }

        /// <summary>
        /// Returns a handle to <see cref="MethodDefinition"/> corresponding to this handle.
        /// </summary>
        /// <remarks>
        /// The resulting handle is only valid within the context of a <see cref="MetadataReader"/> open on the type system metadata blob,
        /// which in case of standalone PDB file is a different reader than the one containing this method debug information.
        /// </remarks>
        public MethodDefinitionHandle ToDefinitionHandle()
        {
            return MethodDefinitionHandle.FromRowId(_rowId);
        }
    }

    public readonly struct LocalScopeHandle : IEquatable<LocalScopeHandle>
    {
        private const uint tokenType = TokenTypeIds.LocalScope;
        private const byte tokenTypeSmall = (byte)HandleType.LocalScope;
        private readonly int _rowId;

        private LocalScopeHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static LocalScopeHandle FromRowId(int rowId)
        {
            return new LocalScopeHandle(rowId);
        }

        public static implicit operator Handle(LocalScopeHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(LocalScopeHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator LocalScopeHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new LocalScopeHandle(handle.RowId);
        }

        public static explicit operator LocalScopeHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
            }

            return new LocalScopeHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(LocalScopeHandle left, LocalScopeHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object? obj)
        {
            return obj is LocalScopeHandle lsh && lsh._rowId == _rowId;
        }

        public bool Equals(LocalScopeHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(LocalScopeHandle left, LocalScopeHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public readonly struct LocalVariableHandle : IEquatable<LocalVariableHandle>
    {
        private const uint tokenType = TokenTypeIds.LocalVariable;
        private const byte tokenTypeSmall = (byte)HandleType.LocalVariable;
        private readonly int _rowId;

        private LocalVariableHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static LocalVariableHandle FromRowId(int rowId)
        {
            return new LocalVariableHandle(rowId);
        }

        public static implicit operator Handle(LocalVariableHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(LocalVariableHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator LocalVariableHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new LocalVariableHandle(handle.RowId);
        }

        public static explicit operator LocalVariableHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
            }

            return new LocalVariableHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(LocalVariableHandle left, LocalVariableHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object? obj)
        {
            return obj is LocalVariableHandle lvh && lvh._rowId == _rowId;
        }

        public bool Equals(LocalVariableHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(LocalVariableHandle left, LocalVariableHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public readonly struct LocalConstantHandle : IEquatable<LocalConstantHandle>
    {
        private const uint tokenType = TokenTypeIds.LocalConstant;
        private const byte tokenTypeSmall = (byte)HandleType.LocalConstant;
        private readonly int _rowId;

        private LocalConstantHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static LocalConstantHandle FromRowId(int rowId)
        {
            return new LocalConstantHandle(rowId);
        }

        public static implicit operator Handle(LocalConstantHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(LocalConstantHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator LocalConstantHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new LocalConstantHandle(handle.RowId);
        }

        public static explicit operator LocalConstantHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
            }

            return new LocalConstantHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(LocalConstantHandle left, LocalConstantHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is LocalConstantHandle lch && lch._rowId == _rowId;
        }

        public bool Equals(LocalConstantHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(LocalConstantHandle left, LocalConstantHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public readonly struct ImportScopeHandle : IEquatable<ImportScopeHandle>
    {
        private const uint tokenType = TokenTypeIds.ImportScope;
        private const byte tokenTypeSmall = (byte)HandleType.ImportScope;
        private readonly int _rowId;

        private ImportScopeHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ImportScopeHandle FromRowId(int rowId)
        {
            return new ImportScopeHandle(rowId);
        }

        public static implicit operator Handle(ImportScopeHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ImportScopeHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ImportScopeHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new ImportScopeHandle(handle.RowId);
        }

        public static explicit operator ImportScopeHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
            }

            return new ImportScopeHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(ImportScopeHandle left, ImportScopeHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is ImportScopeHandle ish && ish._rowId == _rowId;
        }

        public bool Equals(ImportScopeHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ImportScopeHandle left, ImportScopeHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public readonly struct CustomDebugInformationHandle : IEquatable<CustomDebugInformationHandle>
    {
        private const uint tokenType = TokenTypeIds.CustomDebugInformation;
        private const byte tokenTypeSmall = (byte)HandleType.CustomDebugInformation;
        private readonly int _rowId;

        private CustomDebugInformationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static CustomDebugInformationHandle FromRowId(int rowId)
        {
            return new CustomDebugInformationHandle(rowId);
        }

        public static implicit operator Handle(CustomDebugInformationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(CustomDebugInformationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator CustomDebugInformationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new CustomDebugInformationHandle(handle.RowId);
        }

        public static explicit operator CustomDebugInformationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
            }

            return new CustomDebugInformationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(CustomDebugInformationHandle left, CustomDebugInformationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is CustomDebugInformationHandle cdih && cdih._rowId == _rowId;
        }

        public bool Equals(CustomDebugInformationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(CustomDebugInformationHandle left, CustomDebugInformationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }
}
