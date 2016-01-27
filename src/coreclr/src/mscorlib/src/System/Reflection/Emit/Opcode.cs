// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit {
using System;
using System.Threading;
using System.Security.Permissions;
using System.Diagnostics.Contracts;

[System.Runtime.InteropServices.ComVisible(true)]
public struct OpCode
{
    //
    // Use packed bitfield for flags to avoid code bloat
    //

    internal const int OperandTypeMask              = 0x1F;         // 000000000000000000000000000XXXXX

    internal const int FlowControlShift             = 5;            // 00000000000000000000000XXXX00000
    internal const int FlowControlMask              = 0x0F;

    internal const int OpCodeTypeShift              = 9;            // 00000000000000000000XXX000000000
    internal const int OpCodeTypeMask               = 0x07;

    internal const int StackBehaviourPopShift       = 12;           // 000000000000000XXXXX000000000000
    internal const int StackBehaviourPushShift      = 17;           // 0000000000XXXXX00000000000000000
    internal const int StackBehaviourMask           = 0x1F;

    internal const int SizeShift                    = 22;           // 00000000XX0000000000000000000000
    internal const int SizeMask                     = 0x03;

    internal const int EndsUncondJmpBlkFlag         = 0x01000000;   // 0000000X000000000000000000000000

    // unused                                                       // 0000XXX0000000000000000000000000

    internal const int StackChangeShift             = 28;           // XXXX0000000000000000000000000000

#if FEATURE_CORECLR
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
#else // FEATURE_CORECLR
    //
    // The exact layout is part of the legacy COM mscorlib surface, so it is
    // pretty much set in stone for desktop CLR. Ideally, we would use the packed 
    // bit field like for CoreCLR, but that would be a breaking change.
    //

// disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
    private String m_stringname; // not used - computed lazily
#pragma warning restore 0414
    private StackBehaviour m_pop;
    private StackBehaviour m_push;
    private OperandType m_operand;
    private OpCodeType m_type;
    private int m_size;
    private byte m_s1;
    private byte m_s2;
    private FlowControl m_ctrl;

    // Specifies whether the current instructions causes the control flow to
    // change unconditionally.
    private bool m_endsUncondJmpBlk;


    // Specifies the stack change that the current instruction causes not
    // taking into account the operand dependant stack changes.
    private int m_stackChange;


    internal OpCode(OpCodeValues value, int flags)
    {
        m_stringname = null; // computed lazily
        m_pop = (StackBehaviour)((flags >> StackBehaviourPopShift) & StackBehaviourMask);
        m_push = (StackBehaviour)((flags >> StackBehaviourPushShift) & StackBehaviourMask);
        m_operand = (OperandType)(flags & OperandTypeMask);
        m_type = (OpCodeType)((flags >> OpCodeTypeShift) & OpCodeTypeMask);
        m_size = (flags >> SizeShift) & SizeMask;
        m_s1 = (byte)((int)value >> 8);
        m_s2 = (byte)(int)value;
        m_ctrl = (FlowControl)((flags >> FlowControlShift) & FlowControlMask);
        m_endsUncondJmpBlk = (flags & EndsUncondJmpBlkFlag) != 0;
        m_stackChange = (flags >> StackChangeShift);
    }

    internal bool EndsUncondJmpBlk()
    {
        return m_endsUncondJmpBlk;
    }

    internal int StackChange()
    {
        return m_stackChange;
    }

    public OperandType OperandType
    {
        get
        {
            return (m_operand);
        }
    }

    public FlowControl FlowControl
    {
        get
        {
            return (m_ctrl);
        }
    }

    public OpCodeType OpCodeType
    {
        get
        {
            return (m_type);
        }
    }


    public StackBehaviour StackBehaviourPop
    {
        get
        {
            return (m_pop);
        }
    }

    public StackBehaviour StackBehaviourPush
    {
        get
        {
            return (m_push);
        }
    }

    public int Size
    {
        get
        {
            return (m_size);
        }
    }

    public short Value
    {
        get
        {
            if (m_size == 2)
                return (short)(m_s1 << 8 | m_s2);
            return (short)m_s2;
        }
    }
#endif // FEATURE_CORECLR


    private static volatile string[] g_nameCache;

    public String Name
    {
        get
        {
            if (Size == 0)
                return null;

            // Create and cache the opcode names lazily. They should be rarely used (only for logging, etc.)
            // Note that we do not any locks here because of we always get the same names. The last one wins.
            string[] nameCache = g_nameCache;
            if (nameCache == null) {
                nameCache = new String[0x11f];
                g_nameCache = nameCache;
            }

            OpCodeValues opCodeValue = (OpCodeValues)(ushort)Value;

            int idx = (int)opCodeValue;
            if (idx > 0xFF) {
                if (idx >= 0xfe00 && idx <= 0xfe1e) {
                    // Transform two byte opcode value to lower range that's suitable
                    // for array index
                    idx = 0x100 + (idx - 0xfe00);
                }
                else {
                    // Unknown opcode
                    return null;
                }
            }

            String name = Volatile.Read(ref nameCache[idx]);
            if (name != null)
                return name;

            // Create ilasm style name from the enum value name.
            name = Enum.GetName(typeof(OpCodeValues), opCodeValue).ToLowerInvariant().Replace("_", ".");
            Volatile.Write(ref nameCache[idx], name);
            return name;
        }
    }

    [Pure]
    public override bool Equals(Object obj)
    {
        if (obj is OpCode)
            return Equals((OpCode)obj);
        else
            return false;
    }

    [Pure]
    public bool Equals(OpCode obj)
    {
        return obj.Value == Value;
    }

    [Pure]
    public static bool operator ==(OpCode a, OpCode b)
    {
        return a.Equals(b);
    }

    [Pure]
    public static bool operator !=(OpCode a, OpCode b)
    {
        return !(a == b);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public override String ToString()
    {
        return Name;
    }
}

}
