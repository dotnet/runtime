// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DebuggerTests
{
    public class StepInTest<T>
    {
        public static int TestedMethod(T value)
        {
            // 1) break here and check un-assigned variables
            T r;
            r = value;
            // 2) break here and check assigned variables
            return 0;
        }
    }

    public partial class MONO_TYPE_OBJECT
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            var value = new object();
            return StepInTest<object>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_CLASS
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            var value = new MONO_TYPE_CLASS();
            return StepInTest<MONO_TYPE_CLASS>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_BOOLEAN
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            var value = true;
            return StepInTest<bool>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_CHAR
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            var value = 'a';
            return StepInTest<char>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_I1
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            sbyte value = -1;
            return StepInTest<sbyte>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_I2
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            short value = -1;
            return StepInTest<short>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_I4
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            int value = -1;
            return StepInTest<int>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_I8
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            long value = -1;
            return StepInTest<long>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_U1
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            byte value = 1;
            return StepInTest<byte>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_U2
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            ushort value = 1;
            return StepInTest<ushort>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_U4
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            uint value = 1;
            return StepInTest<uint>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_U8
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            ulong value = 1;
            return StepInTest<ulong>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_R4
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            float value = 3.1415F;
            return StepInTest<float>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_R8
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            double value = 3.1415D;
            return StepInTest<double>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_STRING
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            string value = "hello";
            return StepInTest<string>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_ENUM
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            RGB value = RGB.Blue;
            return StepInTest<RGB>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_ARRAY
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            byte[] value = new byte[2] { 1, 2 };
            return StepInTest<byte[]>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_VALUETYPE
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            Point value = new Point();
            return StepInTest<Point>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_VALUETYPE2
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            Decimal value = 1.1m;
            return StepInTest<Decimal>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_GENERICINST
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public static int Prepare()
        {
            Func<int> value = MONO_TYPE_GENERICINST.Prepare;
            return StepInTest<Func<int>>.TestedMethod(value);
        }
    }

    public partial class MONO_TYPE_FNPTR
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public unsafe static int Prepare()
        {
            delegate*<int> value = &MONO_TYPE_FNPTR.Prepare;
            return TestedMethod(value);
        }

        public unsafe static int TestedMethod(delegate*<int> value)
        {
            delegate*<int> r;
            r = value;
            return 0;
        }
    }

    public partial class MONO_TYPE_PTR
    {
        [System.Runtime.InteropServices.JavaScript.JSExport] public unsafe static int Prepare()
        {
            int a = 1; int* value = &a;
            return TestedMethod(value);
        }

        public unsafe static int TestedMethod(int* value)
        {
            int* r;
            r = value;
            return 0;
        }
    }
}
