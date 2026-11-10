// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

namespace GitHub_20269
{
    // This tests a case where 
    // 1) We merge returns.
    // 2) One of the returns has an operand that is a call to a multi-reg returning method.
    // 3) The call is marked as a tail-call candidate in the importer.
    // 3) The tail call is rejected late in morph.
    // 

    public class Program
    {
        static int i;
        [Fact]
        public static int TestEntryPoint()
        {
            i = 1;
            return (int)new Program().GetVector()[0] + 99;
        }

        public virtual Vector<float> GetVector()
        {
            // Address-taken local prevents tail-calling
            // GetVectorHelper.
            int x = 0;
            DoNothing(ref x);

            // 5 returns are needed to trigger merge of returns
            switch(i)
            {
                case 1:
                    // This call is a tail-call candidate rejected late.
                    return GetVectorHelper();

                case 2:
                    return new Vector<float>(2.0f);

                case 3:
                    return new Vector<float>(3.0f);

                case 4:
                    return new Vector<float>(4.0f);

                default:
                    return new Vector<float>(100.0f);

            }
        }

        // This is a multi-reg return method on ARM64
        public virtual Vector<float> GetVectorHelper()
        {
            return new Vector<float>(1.0f);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DoNothing(ref int i)
        {
        }
    }
}
