// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.x86
{
    public enum Action
    {
        POP = 0x00,
        PUSH = 0x01,
        KILL = 0x02,
        LIVE = 0x03,
        DEAD = 0x04
    }

    public class CalleeSavedRegister : BaseGcTransition
    {
        public CalleeSavedRegisters Register { get; set; }

        public CalleeSavedRegister() { }

        public CalleeSavedRegister(int codeOffset, CalleeSavedRegisters reg)
            : base(codeOffset)
        {
            Register = reg;
        }

        public override string ToString()
        {
            return $"thisptr in {Register}";
        }
    }

    public class IPtrMask : BaseGcTransition
    {
        public uint IMask { get; set; }

        public IPtrMask() { }

        public IPtrMask(int codeOffset, uint imask)
            : base(codeOffset)
        {
            IMask = imask;
        }

        public override string ToString()
        {
            return $"iptrMask: {IMask}";
        }
    }

    public class GcTransitionRegister : BaseGcTransition
    {
        public Registers Register { get; set; }
        public Action IsLive { get; set; }
        public int PushCountOrPopSize { get; set; }
        public bool IsThis { get; set; }
        public bool Iptr { get; set; }

        public GcTransitionRegister() { }

        public GcTransitionRegister(int codeOffset, Registers reg, Action isLive, bool isThis = false, bool iptr = false, int pushCountOrPopSize = -1)
            : base(codeOffset)
        {
            Register = reg;
            IsLive = isLive;
            PushCountOrPopSize = pushCountOrPopSize;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (IsLive == Action.LIVE)
            {
                sb.Append($"reg {Register} becoming live");
            }
            else if (IsLive == Action.DEAD)
            {
                sb.Append($"reg {Register} becoming dead");
            }
            else
            {
                sb.Append((IsLive == Action.PUSH ? "push" : "pop") + $" {Register}");
                if (PushCountOrPopSize != -1)
                    sb.Append($" {PushCountOrPopSize}");
            }

            if (IsThis)
                sb.Append(" 'this'");
            if (Iptr)
                sb.Append(" (iptr)");

            return sb.ToString();
        }
    }

    public class GcTransitionPointer : BaseGcTransition
    {
        private bool _isEbpFrame;
        public uint ArgOffset { get; set; }
        public uint ArgCount { get; set; }
        public Action Act { get; set; }
        public bool IsPtr { get; set; }
        public bool IsThis { get; set; }
        public bool Iptr { get; set; }

        public GcTransitionPointer() { }

        public GcTransitionPointer(int codeOffset, uint argOffs, uint argCnt, Action act, bool isEbpFrame, bool isThis = false, bool iptr = false, bool isPtr = true)
            : base(codeOffset)
        {
            _isEbpFrame = isEbpFrame;
            CodeOffset = codeOffset;
            ArgOffset = argOffs;
            ArgCount = argCnt;
            Act = act;
            IsPtr = isPtr;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (Act == Action.KILL)
            {
                sb.Append($"kill args {ArgOffset}");
            }
            else
            {
                if (Act == Action.POP)
                {
                    sb.Append($"pop ");
                }
                else
                {
                    sb.Append($"push ");
                }
                if (IsPtr)
                {
                    sb.Append($"{ArgOffset}");
                    if (!_isEbpFrame)
                    {
                        sb.Append($" args ({ArgCount})");
                    }
                    else if (Act == Action.POP)
                    {
                        sb.Append(" ptrs");
                    }

                    if (IsThis)
                        sb.Append(" 'this'");
                    if (Iptr)
                        sb.Append(" (iptr)");
                }
                else
                {
                    sb.Append("non-pointer");
                    sb.Append($" ({ArgCount})");
                }
            }

            return sb.ToString();
        }
    }

    public class GcTransitionCall : BaseGcTransition
    {
        public struct CallRegister
        {
            public Registers Register { get; set; }
            public bool IsByRef { get; set; }

            public CallRegister(Registers reg, bool isByRef)
            {
                Register = reg;
                IsByRef = isByRef;
            }
        }

        public struct PtrArg
        {
            public uint StackOffset { get; set; }
            public uint LowBit { get; set; }

            public PtrArg(uint stackOffset, uint lowBit)
            {
                StackOffset = stackOffset;
                LowBit = lowBit;
            }
        }

        public List<CallRegister> CallRegisters { get; set; }
        public List<PtrArg> PtrArgs { get; set; }
        public uint ArgMask { get; set; }
        public uint IArgs { get; set; }

        public GcTransitionCall() { }

        public GcTransitionCall(int codeOffset)
            : base(codeOffset)
        {
            CallRegisters = new List<CallRegister>();
            PtrArgs = new List<PtrArg>();
            ArgMask = 0;
            IArgs = 0;
        }

        public GcTransitionCall(int codeOffset, bool isEbpFrame, uint regMask, uint byRefRegMask)
            : base(codeOffset)
        {
            CallRegisters = new List<CallRegister>();
            PtrArgs = new List<PtrArg>();
            if ((regMask & 1) != 0)
            {
                Registers reg = Registers.EDI;
                bool isByRef = (byRefRegMask & 1) != 0;
                CallRegisters.Add(new CallRegister(reg, isByRef));
            }
            if ((regMask & 2) != 0)
            {
                Registers reg = Registers.ESI;
                bool isByRef = (byRefRegMask & 2) != 0;
                CallRegisters.Add(new CallRegister(reg, isByRef));
            }
            if ((regMask & 4) != 0)
            {
                Registers reg = Registers.EBX;
                bool isByRef = (byRefRegMask & 4) != 0;
                CallRegisters.Add(new CallRegister(reg, isByRef));
            }
            if (!isEbpFrame)
            {
                if ((regMask & 8) != 0)
                {
                    Registers reg = Registers.EBP;
                    CallRegisters.Add(new CallRegister(reg, false));
                }
            }
            ArgMask = 0;
            IArgs = 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("call [ ");
            foreach (CallRegister reg in CallRegisters)
            {
                sb.Append($"{reg.Register}");
                if (reg.IsByRef)
                    sb.Append("(byref)");
                sb.Append(" ");
            }

            if (PtrArgs.Count > 0)
            {
                sb.Append(" ] ptrArgs=[ ");
                foreach (PtrArg ptrArg in PtrArgs)
                {
                    sb.Append($"{ptrArg.StackOffset}");
                    if (ptrArg.LowBit != 0)
                        sb.Append("i");
                    sb.Append(" ");
                }
                sb.Append(" ]");
            }
            else
            {
                sb.Append($" ] argMask={ArgMask}");
                if (IArgs != 0)
                    sb.Append($" (iargs={IArgs})");
            }

            return sb.ToString();
        }
    }
}
