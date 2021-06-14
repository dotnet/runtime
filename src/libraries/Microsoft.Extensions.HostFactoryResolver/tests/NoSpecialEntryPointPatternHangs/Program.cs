// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace NoSpecialEntryPointPatternHangs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Threading.Thread.Sleep(-1);
        }
    }
}
