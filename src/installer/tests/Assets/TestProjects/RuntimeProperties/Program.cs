// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                var propertyValue = (string)System.AppContext.GetData(propertyName);
                if (string.IsNullOrEmpty(propertyValue))
                {
                    Console.WriteLine($"Property '{propertyName}' was not found.");
                    continue;
                }

                Console.WriteLine($"AppContext.GetData({propertyName}) = {propertyValue}");
            }
        }
    }
}
