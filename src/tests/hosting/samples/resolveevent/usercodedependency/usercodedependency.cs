// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Text;

namespace UserCodeDependency
{
    public class UserCodeDependencyClass
    {
        static public void InverseClick(int x, int y)
        {
            Console.WriteLine("[Second User Event Handler] Event called with " + x + ":" + y);
        }

    }
}
