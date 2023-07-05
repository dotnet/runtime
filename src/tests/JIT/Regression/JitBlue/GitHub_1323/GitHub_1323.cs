// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Program
{
    static ushort SkillLevel;

    [Fact]
    public static int TestEntryPoint()
    {
        SkillLevel = 0x2121;
        SkillLevel = (ushort)((byte)SkillLevel ^ 0x21);
        if (SkillLevel != 0)
        {
            Console.WriteLine("Fail");
            return -1;
        }
        else
        {
            Console.WriteLine("Pass");
            return 100;
        }
    }
}
