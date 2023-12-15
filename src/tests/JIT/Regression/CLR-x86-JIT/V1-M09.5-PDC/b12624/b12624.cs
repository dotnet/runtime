// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{

    using System;

    public struct X0
    {
        public int X0_0;
        public int X0_4;
        public int X0_8;
        public int X0_C;
    }

    public struct X1
    {
        /*public X0 X1_0;
        public X0 X1_1;
        public X0 X1_2;
        public X0 X1_3;
        public X0 X1_4;
        public X0 X1_5;
        public X0 X1_6;
        public X0 X1_7;*/
        public X0 X1_8;
        public X0 X1_9;
        public X0 X1_A;
        public X0 X1_B;
        public X0 X1_C;
        public X0 X1_D;
        public X0 X1_E;
        public X0 X1_F;
    }

    public struct X2
    {
        public X1 X2_0;
        public X1 X2_1;
        public X1 X2_2;
        public X1 X2_3;
        public X1 X2_4;
        public X1 X2_5;
        public X1 X2_6;
        public X1 X2_7;
        /*public X1 X2_8;
        public X1 X2_9;
        public X1 X2_A;
        public X1 X2_B;
        public X1 X2_C;
        public X1 X2_D;
        public X1 X2_E;
        public X1 X2_F;*/
    }

    public struct X3
    {
        public X2 X3_0;
        public X2 X3_1;
        public X2 X3_2;
        public X2 X3_3;
        /*public X2 X3_4;
        public X2 X3_5;
        public X2 X3_6;
        public X2 X3_7;
        public X2 X3_8;
        public X2 X3_9;
        public X2 X3_A;
        public X2 X3_B;
        public X2 X3_C;
        public X2 X3_D;
        public X2 X3_E;
        public X2 X3_F;*/
    }

    public struct X4
    {
        public X3 X4_0;
        public X3 X4_1;
        public X3 X4_2;
        public X3 X4_3;
        public X3 X4_4;
        public X3 X4_5;
        public X3 X4_6;
        /*public X3 X4_7;
        public X3 X4_8;
        public X3 X4_9;
        public X3 X4_A;
        public X3 X4_B;
        public X3 X4_C;
        public X3 X4_D;
        public X3 X4_E;
        public X3 X4_F;*/
    }

    public struct X5
    {
        public X4 X5_0;
        public X4 X5_1;
        /*
            public X4 X5_2;
            public X4 X5_3;
            public X4 X5_4;
            public X4 X5_5;
            public X4 X5_6;
            public X4 X5_7;
            public X4 X5_8;
            public X4 X5_9;
            public X4 X5_A;
            public X4 X5_B;
            public X4 X5_C;
            public X4 X5_D;
            public X4 X5_E;
            public X4 X5_F;
        */
    }

    /*
    public struct X6
    {
        public X5 X6_0;
        public X5 X6_1;
        public X5 X6_2;
        public X5 X6_3;
        public X5 X6_4;       // ldfld  with NULL object
        public X5 X6_5;
        public X5 X6_6;
        public X5 X6_7;
        public X5 X6_8;
        public X5 X6_9;
        public X5 X6_A;
        public X5 X6_B;
        public X5 X6_C;
        public X5 X6_D;
        public X5 X6_E;
        public X5 X6_F;
    }

    public struct X7
    {
        public X6 X7_0;
        public X6 X7_1;
        public X6 X7_2;
        public X6 X7_3;
        public X6 X7_4;
        public X6 X7_5;
        public X6 X7_6;
        public X6 X7_7;
        public X6 X7_8;
        public X6 X7_9;
        public X6 X7_A;
        public X6 X7_B;
        public X6 X7_C;
        public X6 X7_D;
        public X6 X7_E;
        public X6 X7_F;
    }
    */

    public class Foo
    {

        public static int Read(ref int x)
        {
            return x;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int result = 0;
            try
            {
                // ldflda with NULL object
                result += Read(ref Base.mem.X5_1.X4_6.X3_3.X2_7.X1_8.X0_0);

                // ldfld  with NULL object
                result += Base.mem.X5_1.X4_6.X3_3.X2_7.X1_8.X0_0;
            }
            catch (NullReferenceException)
            {
                return 100;
            }
            return 1;
        }

        public static Foo Base = null;
        public X5 mem;
    }
}
