// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Allocate
{
    public class AllocateArraysOfDoubles : IAllocations
    {
        public void Allocate(int count)
        {
            List<double[]> arrays = new List<double[]>(count);

            for (int i = 0; i < count; i++)
            {
                arrays.Add(new double[1] { i });
            }

            Console.WriteLine($"Sum {arrays.Count} arrays of one double = {arrays.Sum(doubles => doubles[0])}");
        }
    }
}
