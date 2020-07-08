// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.IO;
using System.Reflection;

public class CMain{
    public static int Main(String[] args) {
        string tempFileName = Path.GetTempFileName();

        bool isThrown = false;

        try
        {
            AssemblyName.GetAssemblyName(tempFileName);
        }
        catch (BadImageFormatException)
        {
            isThrown = true;
        }

        File.Delete(tempFileName);

        if (isThrown) {
            Console.WriteLine("PASS");

            return 100;
        } else {
            Console.WriteLine("FAIL");

            return 101;
        }
    }
}
