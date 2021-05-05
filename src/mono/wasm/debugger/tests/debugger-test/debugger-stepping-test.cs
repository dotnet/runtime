// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DebuggerTests
{
    public struct StepInTest<T>
    {
        public static int InnerMethod(T value)
        {
            T r;
            r = value;
            return 0;
        }
    }

    public class MONO_TYPE_OBJECT
    {
        public static int OuterMethod()
        {
            var value = new object();
            return StepInTest<object>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_CLASS
    {
        public static int OuterMethod()
        {
            var value = new MONO_TYPE_CLASS();
            return StepInTest<MONO_TYPE_CLASS>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_BOOLEAN
    {
        public static int OuterMethod()
        {
            var value = true;
            return StepInTest<bool>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_CHAR
    {
        public static int OuterMethod()
        {
            var value = 'a';
            return StepInTest<char>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_I1
    {
        public static int OuterMethod()
        {
            sbyte value = -1;
            return StepInTest<sbyte>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_I2
    {
        public static int OuterMethod()
        {
            short value = -1;
            return StepInTest<short>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_I4
    {
        public static int OuterMethod()
        {
            int value = -1;
            return StepInTest<int>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_I8
    {
        public static int OuterMethod()
        {
            long value = -1;
            return StepInTest<long>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_U1
    {
        public static int OuterMethod()
        {
            byte value = 1;
            return StepInTest<byte>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_U2
    {
        public static int OuterMethod()
        {
            ushort value = 1;
            return StepInTest<ushort>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_U4
    {
        public static int OuterMethod()
        {
            uint value = 1;
            return StepInTest<uint>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_U8
    {
        public static int OuterMethod()
        {
            ulong value = 1;
            return StepInTest<ulong>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_R4
    {
        public static int OuterMethod()
        {
            float value = 3.1415F;
            return StepInTest<float>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_R8
    {
        public static int OuterMethod()
        {
            double value = 3.1415D;
            return StepInTest<double>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_STRING
    {
        public static int OuterMethod()
        {
            string value = "hello";
            return StepInTest<string>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_ENUM
    {
        public static int OuterMethod()
        {
            RGB value = RGB.Blue;
            return StepInTest<RGB>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_ARRAY
    {
        public static int OuterMethod()
        {
            byte[] value = new byte[2] { 1, 2 };
            return StepInTest<byte[]>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_VALUETYPE
    {
        public static int OuterMethod()
        {
            Point value = new Point();
            return StepInTest<Point>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_VALUETYPE2
    {
        public static int OuterMethod()
        {
            Decimal value = 1.1m;
            return StepInTest<Decimal>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_GENERICINST
    {
        public static int OuterMethod()
        {
            Func<int> value = MONO_TYPE_GENERICINST.OuterMethod;
            return StepInTest<Func<int>>.InnerMethod(value);
        }
    }

    public class MONO_TYPE_FNPTR
    {
        public unsafe static int OuterMethod()
        {
            delegate*<int> value = &MONO_TYPE_FNPTR.OuterMethod;
            return InnerMethod(value);
        }

        public unsafe static int InnerMethod(delegate*<int> value)
        {
            delegate*<int> r;
            r = value;
            return 0;
        }
    }

    public class MONO_TYPE_PTR
    {
        public unsafe static int OuterMethod()
        {
            int a = 1; int* value = &a;
            return InnerMethod(value);
        }

        public unsafe static int InnerMethod(int* value)
        {
            int* r;
            r = value;
            return 0;
        }
    }
}
