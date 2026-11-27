// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#if USE_QCALL
if (GC.GetTotalMemory(forceFullCollection: false) <= 0)
    return 1;
#endif

if (Environment.ProcessorCount <= 0)
    return 2;

string value = new string('q', 32);
if (!ReferenceEquals(string.Intern(value), string.Intern(new string('q', 32))))
    return 3;

byte[] buffer = new byte[8192];
buffer[0] = 42;
Buffer.BlockCopy(buffer, 0, buffer, 1, buffer.Length - 1);
if (buffer[1] != 42 || buffer[2] != 0)
    return 4;

Array.Clear(buffer);
foreach (byte item in buffer)
{
    if (item != 0)
        return 5;
}

Console.WriteLine("TestOutput -> QCall trimming sample passed");
return 42;
