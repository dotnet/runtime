// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

if (args.Length == 2 && args[0] == "console-direct-exit")
{
    ConsoleTest.PrepareExit();
    return int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
}

System.Console.WriteLine("WasmBasicTestApp");
System.Console.Error.WriteLine("WasmBasicTestApp stderr");
return 0;
