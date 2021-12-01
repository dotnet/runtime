// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;

namespace Runtime_51612
{
    class Program
    {
        struct PassedViaReturnBuffer
        {
            public int _0;
            public int _1;
        }

        abstract class Base
        {
            public unsafe abstract PassedViaReturnBuffer HasEspBasedFrame();
        }

        class Derived : Base
        {
            public Derived(Exception ex)
            {
                _ex = ex;
            }

            readonly Exception _ex;

            public override PassedViaReturnBuffer HasEspBasedFrame()
            {
                DoesNotReturn();
                PassedViaReturnBuffer retBuf;
                retBuf = RequiresGuardStack();
                return retBuf;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static unsafe PassedViaReturnBuffer RequiresGuardStack()
            {
                int* p = stackalloc int[2];

                p[0] = 1;
                p[1] = 2;

                PassedViaReturnBuffer retBuf;

                retBuf._0 = p[0];
                retBuf._1 = p[1];

                return retBuf;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void DoesNotReturn()
            {
                throw _ex;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AssertsThatGuardStackCookieOffsetIsInvalid(Base x)
        {
            x.HasEspBasedFrame();
        }

        static int Main(string[] args)
        {
            try
            {
                AssertsThatGuardStackCookieOffsetIsInvalid(new Derived(new Exception()));
            }
            catch (Exception ex)
            {
                return 100;
            }

            return 0;
        }
    }
}
