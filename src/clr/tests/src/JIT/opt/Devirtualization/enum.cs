// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

enum MyEnum { One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten };

public class Test
{
    public static int Main()
    {
        // Call to ToString should be devirtualize since boxed enums
        // are sealed classes. This doesn't happen yet.
        string s = (MyEnum.Seven).ToString();

        // Call to Equals will devirtualize since string is sealed
        return (s.Equals((object)"Seven") ? 100 : -1);
    }
}
