// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

class environment_version
{
    static int Main()
    {
        Version ver = Environment.Version;
        Console.WriteLine($"Environment.Version = {ver}");

        if (ver < new Version("3.0"))
        {
            Console.WriteLine("ERROR: Version less than 3.0.");
            return -1;
        }

        // Verify that we are not returning hardcoded version from .NET Framework.
        if (ver == new Version("4.0.30319.42000"))
        {
            Console.WriteLine("ERROR: Version is hardcoded .NET Framework version.");
            return -1;
        }

        // .NET Core assemblies use 4.6+ as file version. Verify that we have not used
        // the file version as product version by accident.
        if (ver.Major == 4 && (ver.Minor >= 6))
        {
            Console.WriteLine("ERROR: Version is 4.6+.");
            return -1;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
