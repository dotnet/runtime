// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Reflection.Emit
{
    public struct OpCode
    {
        //
        // Use packed bitfield for flags to avoid code bloat
        //

        internal const int OperandTypeMask = 0x1F;              // 000000000000000000000000000XXXXX

        internal const int FlowControlShift = 5;                // 00000000000000000000000XXXX00000
        internal const int FlowControlMask = 0x0F;

        internal const int OpCodeTypeShift = 9;                 // 00000000000000000000XXX000000000
        internal const int OpCodeTypeMask = 0x07;

        internal const int StackBehaviourPopShift = 12;         // 000000000000000XXXXX000000000000
        internal const int StackBehaviourPushShift = 17;        // 0000000000XXXXX00000000000000000
        internal const int StackBehaviourMask = 0x1F;

        internal const int SizeShift = 22;                      // 00000000XX0000000000000000000000
        internal const int SizeMask = 0x03;

        internal const int EndsUncondJmpBlkFlag = 0x01000000;   // 0000000X000000000000000000000000

        // unused                                               // 0000XXX0000000000000000000000000

        internal const int StackChangeShift = 28;               // XXXX0000000000000000000000000000

        private OpCodeValues m_value;
        private int m_flags;

        internal OpCode(OpCodeValues value, int flags)
        {
            m_value = value;
            m_flags = flags;
        }

        internal bool EndsUncondJmpBlk()
        {
            return (m_flags & EndsUncondJmpBlkFlag) != 0;
        }

        internal int StackChange()
        {
            return (m_flags >> StackChangeShift);
        }

        public OperandType OperandType
        {
            get
            {
                return (OperandType)(m_flags & OperandTypeMask);
            }
        }

        public FlowControl FlowControl
        {
            get
            {
                return (FlowControl)((m_flags >> FlowControlShift) & FlowControlMask);
            }
        }

        public OpCodeType OpCodeType
        {
            get
            {
                return (OpCodeType)((m_flags >> OpCodeTypeShift) & OpCodeTypeMask);
            }
        }

        public StackBehaviour StackBehaviourPop
        {
            get
            {
                return (StackBehaviour)((m_flags >> StackBehaviourPopShift) & StackBehaviourMask);
            }
        }

        public StackBehaviour StackBehaviourPush
        {
            get
            {
                return (StackBehaviour)((m_flags >> StackBehaviourPushShift) & StackBehaviourMask);
            }
        }

        public int Size
        {
            get
            {
                return (m_flags >> SizeShift) & SizeMask;
            }
        }

        public short Value
        {
            get
            {
                return (short)m_value;
            }
        }

        private static volatile string[]? g_nameCache;

        public string? Name
        {
            get
            {
                if (Size == 0)
                    return null;

                // Create and cache the opcode names lazily. They should be rarely used (only for logging, etc.)
                // Note that we do not any locks here because of we always get the same names. The last one wins.
                string[]? nameCache = g_nameCache;
                if (nameCache == null)
                {
                    nameCache = new string[0x11f];
                    g_nameCache = nameCache;
                }

                OpCodeValues opCodeValue = (OpCodeValues)(ushort)Value;

                int idx = (int)opCodeValue;
                if (idx > 0xFF)
                {
                    if (idx >= 0xfe00 && idx <= 0xfe1e)
                    {
                        // Transform two byte opcode value to lower range that's suitable
                        // for array index
                        idx = 0x100 + (idx - 0xfe00);
                    }
                    else
                    {
                        // Unknown opcode
                        return null;
                    }
                }

                string name = Volatile.Read(ref nameCache[idx]);
                if (name != null)
                    return name;

                // Create ilasm style name from the enum value name.
                name = Enum.GetName(typeof(OpCodeValues), opCodeValue)!.ToLowerInvariant().Replace('_', '.');
                Volatile.Write(ref nameCache[idx], name);
                return name;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is OpCode)
                return Equals((OpCode)obj);
            else
                return false;
        }

        public bool Equals(OpCode obj)
        {
            return obj.Value == Value;
        }

        public static bool operator ==(OpCode a, OpCode b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(OpCode a, OpCode b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string? ToString()
        {
            return Name;
        }
    }
}
