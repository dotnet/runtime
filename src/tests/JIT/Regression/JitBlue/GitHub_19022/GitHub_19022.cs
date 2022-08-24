// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;

namespace JitCrashPOC
{
    class Program
    {
        static public int s_res;

        static int Main(string[] args)
        {
            var map = new ItemRunner();

            s_res = 0;
            map.UpdateItem(0,10);

            if (s_res == 300)
            {
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Failed");
                return 101;
            }
        }
    }

    class Item
    {
        public Vector3 _Position = new Vector3(0.0f, 0.0f, 0.0f);
    }

    class ItemRunner
    {
        public ItemRunner()
        {
            for (int i = 0; i < _Pool.Length; ++i) { _Pool[i] = new Item(); }
        }

        private const float _LengthZ = 1000.0f;

        private static readonly Vector3 _Start = new Vector3(0.0f, -1021.7f, -3451.3f);
        private static readonly Vector3 _Slope = new Vector3(0.0f, 0.286f, 0.958f);

        private Item[] _Pool = new Item[30];

        private Item _LastGenerated;


        // This method qualifies for the optimization:
        // fgMorphRecursiveFastTailCallIntoLoop : Transform a recursive fast tail call into a loop.
        //
        // It also has a Vector3 TYP_SIMD12 local variable that needs initializtion across the tailcall-loop
        // The JIT was asserting or crashing when dealing with this case
        //
        public void UpdateItem(float fDelta, int depth)
        {
            if (depth == 0)
            {
                return;
            }

            Vector3 vDelta;

            for (int i = 0; i < _Pool.Length; i++)
            {
                vDelta = _Slope * fDelta;

                if (_LastGenerated != null) _Pool[i]._Position = _LastGenerated._Position - _Slope * _LengthZ;
                else _Pool[i]._Position = _Start - vDelta;

                _LastGenerated = _Pool[i];
                Program.s_res++;
            }

            UpdateItem(0, depth-1);
        }
    }
}
