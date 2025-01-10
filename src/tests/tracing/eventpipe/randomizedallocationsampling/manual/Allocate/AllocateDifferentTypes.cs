// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Allocate
{
    public class AllocateDifferentTypes : IAllocations
    {
        public void Allocate(int count)
        {
            List<object> objects = new List<object>(count);

            for (int i = 0; i < count; i++)
            {
                objects.Add(new string('c', 37));
                objects.Add(new WithFinalizer(i));
                objects.Add(new byte[173]);
                int[,] matrix = { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } };
                objects.Add(matrix);
            }

            Console.WriteLine($"{objects.Count} objects");
        }
    }

    public class WithFinalizer
    {
        private static int _counter;

        private readonly UInt16 _x1;
        private readonly UInt16 _x2;
        private readonly UInt16 _x3;

        public static int Counter => _counter;

        public WithFinalizer(int id)
        {
            _counter++;

            _x1 = (UInt16)(id % 10);
            _x2 = (UInt16)(id % 100);
            _x3 = (UInt16)(id % 1000);
        }

        ~WithFinalizer()
        {
            _counter--;
        }
    }
}
