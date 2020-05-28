// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace RuntimeProperties
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));

            foreach (string propertyName in args)
            {
                Console.WriteLine($"AppContext.GetData({propertyName}) = {System.AppContext.GetData(propertyName)}");
            }
        }
    }
}
