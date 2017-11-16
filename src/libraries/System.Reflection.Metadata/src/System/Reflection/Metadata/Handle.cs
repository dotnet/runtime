// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents any metadata entity (type reference/definition/specification, method definition, custom attribute, etc.) or value (string, blob, guid, user string).
    /// </summary>
    /// <remarks>
    /// Use <see cref="Handle"/> to store multiple kinds of handles.
    /// </remarks>
    public readonly struct Handle : IEquatable<Handle>
    {
        private readonly int _value;

        // bits:
        //    7: IsVirtual
        // 0..6: token type
        private readonly byte _vType;

        /// <summary>
        /// Creates <see cref="Handle"/> from a token or a token combined with a virtual flag.
        /// </summary>
        internal static Handle FromVToken(uint vToken)
        {
            return new Handle((byte)(vToken >> TokenTypeIds.RowIdBitCount), (int)(vToken & TokenTypeIds.RIDMask));
        }

        internal Handle(byte vType, int value)
        {
            _vType = vType;
            _value = value;

            Debug.Assert(value >= 0);

            // No table can have more than 2^24 rows.
            // User String heap is also limited by 2^24 since user strings have tokens in IL.
            // We limit the size of #Blob, #String and #GUID heaps to 2^29 (max compressed integer) in order 
            // to keep the sizes of corresponding handles to 32 bit. As a result we don't support reading metadata 
            // files with heaps larger than 0.5GB.
            Debug.Assert(IsHeapHandle && value <= HeapHandleType.OffsetMask || 
                         !IsHeapHandle && value <= TokenTypeIds.RIDMask);
        }

        // for entity handles:
        internal int RowId
        {
            get
            {
                Debug.Assert(!IsHeapHandle);
                return _value;
            }
        }

        // for heap handles:
        internal int Offset
        {
            get
            {
                Debug.Assert(IsHeapHandle);
                return _value;
            }
        }

        /// <summary>
        /// Token type (0x##000000), does not include virtual flag.
        /// </summary>
        internal uint EntityHandleType
        {
            get { return Type << TokenTypeIds.RowIdBitCount; }
        }

        /// <summary>
        /// Small token type (0x##), does not include virtual flag.
        /// </summary>
        internal uint Type
        {
            get { return _vType & HandleType.TypeMask; }
        }

        /// <summary>
        /// Value stored in an <see cref="EntityHandle"/>.
        /// </summary>
        internal uint EntityHandleValue
        {
            get
            {
                Debug.Assert((_value & TokenTypeIds.RIDMask) == _value);
                return (uint)_vType << TokenTypeIds.RowIdBitCount | (uint)_value;
            }
        }

        /// <summary>
        /// Value stored in a concrete entity handle (see <see cref="TypeDefinitionHandle"/>, <see cref="MethodDefinitionHandle"/>, etc.).
        /// </summary>
        internal uint SpecificEntityHandleValue
        {
            get
            {
                Debug.Assert((_value & TokenTypeIds.RIDMask) == _value);
                return (_vType & HandleType.VirtualBit) << TokenTypeIds.RowIdBitCount | (uint)_value;
            }
        }

        internal byte VType
        {
            get { return _vType; }
        }

        internal bool IsVirtual
        {
            get { return (_vType & HandleType.VirtualBit) != 0; }
        }

        internal bool IsHeapHandle
        {
            get { return (_vType & HandleType.HeapMask) == HandleType.HeapMask; }
        }

        public HandleKind Kind
        {
            get
            {
                uint type = Type;

                // Do not surface extra non-virtual string type bits in public handle kind
                if ((type & ~HandleType.NonVirtualStringTypeMask) == HandleType.String)
                {
                    return HandleKind.String;
                }

                return (HandleKind)type;
            }
        }

        public bool IsNil
        {
            // virtual handles are never nil
            get { return ((uint)_value | (_vType & HandleType.VirtualBit)) == 0; }
        }

        internal bool IsEntityOrUserStringHandle
        {
            get { return Type <= HandleType.UserString; }
        }

        internal int Token
        {
            get
            {
                Debug.Assert(IsEntityOrUserStringHandle);
                Debug.Assert(!IsVirtual);
                Debug.Assert((_value & TokenTypeIds.RIDMask) == _value);

                return _vType << TokenTypeIds.RowIdBitCount | _value;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Handle && Equals((Handle)obj);
        }

        public bool Equals(Handle other)
        {
            return _value == other._value && _vType == other._vType;
        }

        public override int GetHashCode()
        {
            return _value ^ (_vType << 24);
        }

        public static bool operator ==(Handle left, Handle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Handle left, Handle right)
        {
            return !left.Equals(right);
        }

        internal static int Compare(Handle left, Handle right)
        {
            // All virtual tokens will be sorted after non-virtual tokens.
            // The order of handles that differ in kind is undefined, 
            // but we include it so that we ensure consistency with == and != operators.
            return ((long)(uint)left._value | (long)left._vType << 32).CompareTo((long)(uint)right._value | (long)right._vType << 32);
        }

        public static readonly ModuleDefinitionHandle ModuleDefinition = new ModuleDefinitionHandle(1);
        public static readonly AssemblyDefinitionHandle AssemblyDefinition = new AssemblyDefinitionHandle(1);
    }
}
