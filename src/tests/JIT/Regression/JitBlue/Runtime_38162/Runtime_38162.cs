// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Numerics;

namespace Runtime_38162
{
    class Program
    {
        static int Main(string[] args)
        {
            bool directCall     = Vector.IsHardwareAccelerated;
            bool reflectionCall = (bool)typeof(Vector).InvokeMember(nameof(Vector.IsHardwareAccelerated), BindingFlags.GetProperty, null, null, new object[0]);

            if (directCall != reflectionCall)
            {
                Console.WriteLine($"Direct call to Vector.IsHardwareAccelerated returns {directCall}");
                Console.WriteLine($"Reflection call to Vector.IsHardwareAccelerated returns {reflectionCall}");
                return 1;
            }

            return 100;
        }
    }
}
