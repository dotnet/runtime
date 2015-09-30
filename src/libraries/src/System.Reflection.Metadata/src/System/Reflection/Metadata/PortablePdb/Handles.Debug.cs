// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public struct DocumentHandle : IEquatable<DocumentHandle>
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

        public override bool Equals(object obj)
        {
            return obj is DocumentHandle && ((DocumentHandle)obj)._rowId == _rowId;
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

    public struct MethodBodyHandle : IEquatable<MethodBodyHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodBody;
        private const byte tokenTypeSmall = (byte)HandleType.MethodBody;
        private readonly int _rowId;

        private MethodBodyHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodBodyHandle FromRowId(int rowId)
        {
            return new MethodBodyHandle(rowId);
        }

        public static implicit operator Handle(MethodBodyHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MethodBodyHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MethodBodyHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Throw.InvalidCast();
            }

            return new MethodBodyHandle(handle.RowId);
        }

        public static explicit operator MethodBodyHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Throw.InvalidCast();
            }

            return new MethodBodyHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(MethodBodyHandle left, MethodBodyHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodBodyHandle && ((MethodBodyHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MethodBodyHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MethodBodyHandle left, MethodBodyHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct LocalScopeHandle : IEquatable<LocalScopeHandle>
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

        public override bool Equals(object obj)
        {
            return obj is LocalScopeHandle && ((LocalScopeHandle)obj)._rowId == _rowId;
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

    public struct LocalVariableHandle : IEquatable<LocalVariableHandle>
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

        public override bool Equals(object obj)
        {
            return obj is LocalVariableHandle && ((LocalVariableHandle)obj)._rowId == _rowId;
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

    public struct LocalConstantHandle : IEquatable<LocalConstantHandle>
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

        public override bool Equals(object obj)
        {
            return obj is LocalConstantHandle && ((LocalConstantHandle)obj)._rowId == _rowId;
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

    public struct ImportScopeHandle : IEquatable<ImportScopeHandle>
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

        public override bool Equals(object obj)
        {
            return obj is ImportScopeHandle && ((ImportScopeHandle)obj)._rowId == _rowId;
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

    public struct CustomDebugInformationHandle : IEquatable<CustomDebugInformationHandle>
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

        public override bool Equals(object obj)
        {
            return obj is CustomDebugInformationHandle && ((CustomDebugInformationHandle)obj)._rowId == _rowId;
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
