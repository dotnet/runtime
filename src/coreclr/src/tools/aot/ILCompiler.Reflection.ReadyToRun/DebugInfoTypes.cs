// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILCompiler.Reflection.ReadyToRun
{
    public struct DebugInfoBoundsEntry
    {
        public uint NativeOffset;
        public uint ILOffset;
        public SourceTypes SourceTypes;
    }

    public struct NativeVarInfo
    {
        public uint StartOffset;
        public uint EndOffset;
        // TODO: Eliminate this
        public uint VariableNumber;
        public Variable Variable { get; internal set; }
        public VarLoc VariableLocation;
    }

    public enum VariableType
    {
        Parameter,
        Local,
        // TODO: Special
    }

    public class Variable
    {
        public VariableType Type { get; internal set; }
        public int Index { get; internal set; }
    }

    [Flags]
    public enum SourceTypes
    {
        /// <summary>
        /// Indicates that no other options apply
        /// </summary>
        SourceTypeInvalid = 0x00,
        /// <summary>
        /// The debugger asked for it
        /// </summary>
        SequencePoint = 0x01,
        /// <summary>
        /// The stack is empty here
        /// </summary>
        StackEmpty = 0x02,
        /// <summary>
        /// This is a call site
        /// </summary>
        CallSite = 0x04,
        /// <summary>
        /// Indicate an epilog endpoint
        /// </summary>
        NativeEndOffsetUnknown = 0x08,
        /// <summary>
        /// The actual instruction of a call
        /// </summary>
        CallInstruction = 0x10
    }

    public enum DebugInfoBoundsType : uint
    {
        NoMapping = 0xffffffff,
        Prolog = 0xfffffffe,
        Epilog = 0xfffffffd,
        MaxMappingValue = Epilog
    }

    public enum ImplicitILArguments
    {
        VarArgsHandle = -1,
        ReturnBuffer = -2,
        TypeContext = -3,
        Unknown = -4,
        Max = Unknown
    }

    public enum VarLocType
    {
        VLT_REG,        // variable is in a register
        VLT_REG_BYREF,  // address of the variable is in a register
        VLT_REG_FP,     // variable is in an fp register
        VLT_STK,        // variable is on the stack (memory addressed relative to the frame-pointer)
        VLT_STK_BYREF,  // address of the variable is on the stack (memory addressed relative to the frame-pointer)
        VLT_REG_REG,    // variable lives in two registers
        VLT_REG_STK,    // variable lives partly in a register and partly on the stack
        VLT_STK_REG,    // reverse of VLT_REG_STK
        VLT_STK2,       // variable lives in two slots on the stack
        VLT_FPSTK,      // variable lives on the floating-point stack
        VLT_FIXED_VA,   // variable is a fixed argument in a varargs function (relative to VARARGS_HANDLE)

        VLT_COUNT,
        VLT_INVALID,
    }

    public struct VarLoc
    {
        public VarLocType VarLocType;
        // What's stored in the Data# fields changes based on VarLocType and will be
        // interpreted accordingly when the variable location information is dumped.
        public int Data1;
        public int Data2;
        public int Data3;
    }
    public class NativeVarInfoComparer : IComparer<NativeVarInfo>
    {
        public int Compare(NativeVarInfo left, NativeVarInfo right)
        {
            if (left.VariableNumber < right.VariableNumber)
            {
                return -1;
            }
            else if (left.VariableNumber > right.VariableNumber)
            {
                return 1;
            }
            else if (left.StartOffset < right.StartOffset)
            {
                return -1;
            }
            else if (left.StartOffset > right.StartOffset)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
