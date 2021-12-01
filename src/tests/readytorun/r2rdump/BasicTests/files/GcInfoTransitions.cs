// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace GcInfoTransitions
{
    class GcInfoTransitions
    {
        static void abc(string a)
        {

        }

        static void Main(string[] args)
        {
            abc(new string('1',1));
            abc(new string('2', 1));
            abc(new string('3', 1));
            abc(new string('4', 1));
            abc(new string('5', 1));
            abc(new string('6', 1));
            abc(new string('7', 1));
            abc(new string('8', 1));
        }
    }
}
