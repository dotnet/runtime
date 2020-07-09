// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

// This test checks whether or not the JIT properly spills side effects in the importer when dumping multi-reg values
// to temps. If the JIT does not do so correctly, the calls to GetString() and GetDecimal() will be reordered and the
// test will fail with exit code 0; if it does do so correctly, the calls will not be reordered and the test will
// pass.

class Test
{
    abstract class ValueSourceBase
    {
        public abstract string GetString();
        public abstract decimal GetDecimal();
        public abstract int GetReturnValue();
    }

    class ValueSource : ValueSourceBase
    {
        int rv;

        public override string GetString()
        {
            rv = 0;
            return "";
        }

        public override decimal GetDecimal()
        {
            rv = 100;
            return 0;
        }

        public override int GetReturnValue()
        {
            return rv;
        }
    }

    Test(string s, decimal d)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(ValueSourceBase vs)
    {
        new Test(vs.GetString(), vs.GetDecimal());
        return vs.GetReturnValue();
    }

    static int Main()
    {
        return M(new ValueSource());
    }
}
