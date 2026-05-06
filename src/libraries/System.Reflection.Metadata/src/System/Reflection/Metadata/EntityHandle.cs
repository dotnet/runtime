// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents a metadata entity (type reference/definition/specification, method definition, custom attribute, etc.).
    /// </summary>
    /// <remarks>
    /// Use <see cref="EntityHandle"/> to store multiple kinds of entity handles.
    /// It has smaller memory footprint than <see cref="Handle"/>.
    /// </remarks>
    public readonly struct EntityHandle : IEquatable<EntityHandle>
    {
        // bits:
        //     31: IsVirtual
        // 24..30: type
        //  0..23: row id
        private readonly uint _vToken;

        internal EntityHandle(uint vToken)
        {
            _vToken = vToken;
        }

        public static implicit operator Handle(EntityHandle handle)
        {
            return Handle.FromVToken(handle._vToken);
        }

        public static explicit operator EntityHandle(Handle handle)
        {
            if (handle.IsHeapHandle)
            {
                Throw.InvalidCast();
            }

            return new EntityHandle(handle.EntityHandleValue);
        }

        internal uint Type
        {
            get { return _vToken & TokenTypeIds.TypeMask; }
        }

        internal uint VType
        {
            get { return _vToken & (TokenTypeIds.VirtualBit | TokenTypeIds.TypeMask); }
        }

        internal bool IsVirtual
        {
            get { return (_vToken & TokenTypeIds.VirtualBit) != 0; }
        }

        public bool IsNil
        {
            // virtual handle is never nil
            get { return (_vToken & (TokenTypeIds.VirtualBit | TokenTypeIds.RIDMask)) == 0; }
        }

        internal int RowId
        {
            get { return (int)(_vToken & TokenTypeIds.RIDMask); }
        }

        /// <summary>
        /// Value stored in a specific entity handle (see <see cref="TypeDefinitionHandle"/>, <see cref="MethodDefinitionHandle"/>, etc.).
        /// </summary>
        internal uint SpecificHandleValue
        {
            get { return _vToken & (TokenTypeIds.VirtualBit | TokenTypeIds.RIDMask); }
        }

        public HandleKind Kind
        {
            get
            {
                // EntityHandles cannot be StringHandles and therefore we do not need
                // to handle stripping the extra non-virtual string type bits here.
                return (HandleKind)(Type >> TokenTypeIds.RowIdBitCount);
            }
        }

        internal int Token
        {
            get
            {
                Debug.Assert(!IsVirtual);
                return (int)_vToken;
            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is EntityHandle entityHandle && Equals(entityHandle);
        }

        public bool Equals(EntityHandle other)
        {
            return _vToken == other._vToken;
        }

        public override int GetHashCode()
        {
            return unchecked((int)_vToken);
        }

        public static bool operator ==(EntityHandle left, EntityHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityHandle left, EntityHandle right)
        {
            return !left.Equals(right);
        }

        internal static int Compare(EntityHandle left, EntityHandle right)
        {
            // All virtual tokens will be sorted after non-virtual tokens.
            // The order of handles that differ in kind is undefined,
            // but we include it so that we ensure consistency with == and != operators.
            return left._vToken.CompareTo(right._vToken);
        }

        public static readonly ModuleDefinitionHandle ModuleDefinition = new ModuleDefinitionHandle(1);
        public static readonly AssemblyDefinitionHandle AssemblyDefinition = new AssemblyDefinitionHandle(1);
    }
}
