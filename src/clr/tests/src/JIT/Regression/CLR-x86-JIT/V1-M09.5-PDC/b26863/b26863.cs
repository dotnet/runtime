// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
JE1 JR1 C(null) I1 S(null) G0
..\regalloc.cpp, Line 8037 : Assertion failed 'passes <= 4' in 'DefaultNamespace.Obj.Static3():ref'
Running time 0.310 sec
JE1 JR1 C(null) I0 S(null) G0
Running time 1.693 sec
JE0 JR0 C(null) I0 S(null) G0
Running time 0.921 sec
*/
namespace Test
{
    using System;

    class Obj
    {
        bool[] Method1() { return null; }
        uint Method2(bool param1) { return 0; }
        int Method3() { return 0; }
        bool Method4() { return false; }

        static uint[] Recurse(float[] param1, bool param2, uint[] param3)
        { return null; }

        static double[] Static2() { return null; }

        static float[] Static3()
        {
            Obj obj = new Obj();
            do
            {
                do
                {

                    Recurse(new float[4], new Obj().Method1()[2],
                    Recurse(new float[4], obj.Method1()[2],
                    Recurse(new float[4], true,
                    Recurse(new float[4], obj.Method3() !=
                      Recurse(new float[4], new Obj().Method4(),
                      Recurse(new float[4], false, null))[2],
                    Recurse(new float[4], new Obj().Method1()[2],
                    Recurse(new float[4], obj.Method1()[2],
                    Recurse(new float[4], obj.Method1()[2],
                    Recurse(new float[4], true,
                    null))))))));

                    obj.Method1();

                } while (new Random().Next(16) != 5 && new Obj().Method4());

                obj.Method1();

            } while (new Random().Next(16) != 5 && new Obj().Method4());

            return new float[4];
        }

        static int Main()
        {
            try
            {
                Static3();
            }
            catch (Exception)
            {
            }
            return 100;
        }
    }
}
