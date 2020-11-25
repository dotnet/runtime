// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0649

namespace Runtime_45090
{
    struct _4
    {
        public byte _0;
        public byte _1;
        public byte _2;
        public byte _3;
    }

    struct _10
    {
        public long _0;
        public long _8;
    }

    struct _100
    {
        public _10 _0;
        public _10 _1;
        public _10 _2;
        public _10 _3;
        public _10 _4;
        public _10 _5;
        public _10 _6;
        public _10 _7;
        public _10 _8;
        public _10 _9;
        public _10 _a;
        public _10 _b;
        public _10 _c;
        public _10 _d;
        public _10 _e;
        public _10 _f;
    }

    struct _e00
    {
        public _100 _0;
        public _100 _1;
        public _100 _2;
        public _100 _3;
        public _100 _4;
        public _100 _5;
        public _100 _6;
        public _100 _7;
        public _100 _8;
        public _100 _9;
        public _100 _a;
        public _100 _b;
        public _100 _c;
        public _100 _d;
    }

    struct _1000
    {
        public _100 _0;
        public _100 _1;
        public _100 _2;
        public _100 _3;
        public _100 _4;
        public _100 _5;
        public _100 _6;
        public _100 _7;
        public _100 _8;
        public _100 _9;
        public _100 _a;
        public _100 _b;
        public _100 _c;
        public _100 _d;
        public _100 _e;
        public _100 _f;
    }

    abstract class AllocFrame
    {
        public abstract int VirtMethodEspBasedFrame();
    }

    class PushReg : AllocFrame
    {
        // Frame size is 4 bytes and allocated with 'push eax' instruction.
        public unsafe override int VirtMethodEspBasedFrame()
        {
            _4 tmp = new _4();
            *(int*)(&tmp) = 45090;
            return (int)tmp._1;
        }
    }

    class NoProbe : AllocFrame
    {
        // Frame size is less than 0x1000 bytes and doesn't require probing.
        public override int VirtMethodEspBasedFrame()
        {
            _100 tmp = new _100();
            return (int)tmp._0._0;
        }
    }

    class InlineProbe : AllocFrame
    {
        // Frame size is less than 0x1000 bytes and uses inline probing with 'test eax, [esp]' instruction.
        public override int VirtMethodEspBasedFrame()
        {
            _e00 tmp = new _e00();
            return (int)tmp._0._0._0;
        }
    }

    class HelperProbe : AllocFrame
    {
        // Frame is greater than 0x1000 bytes and uses JIT_StackProbe helper.
        public override int VirtMethodEspBasedFrame()
        {
            _1000 tmp = new _1000();
            return (int)tmp._0._0._0;
        }
    }

    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSkipAllocFrame(AllocFrame scenario)
        {
            scenario.VirtMethodEspBasedFrame();
        }

        static int Main(string[] args)
        {
            TestSkipAllocFrame(new PushReg());
            TestSkipAllocFrame(new NoProbe());
            TestSkipAllocFrame(new InlineProbe());
            TestSkipAllocFrame(new HelperProbe());

            return 100;
        }
    }
}
