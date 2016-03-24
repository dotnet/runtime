// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Copyright (c) Andrey Akinshin. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class Program
{
    static ushort SkillLevel;

    static int Main(string[] args)
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
