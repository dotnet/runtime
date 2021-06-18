// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace NoSpecialEntryPointPatternThrows
{
    public class Program
    {
        public static void Main(string[] args)
        {
            throw new Exception("Main just throws");
        }
    }
}
