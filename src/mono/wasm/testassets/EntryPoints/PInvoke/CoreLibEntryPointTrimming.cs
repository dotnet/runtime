// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading;

#if USE_ENTRYPOINTS
if (GC.GetTotalMemory(forceFullCollection: false) <= 0 || Math.Asinh(Environment.ProcessorCount) <= 0)
    return 1;
#endif

if (Environment.ProcessorCount <= 0)
    return 2;

char[] characters = ['q', '\0'];
if (new string(characters) != "q\0" || new string(characters, 0, 1) != "q"
    || new string('q', 2) != "qq" || new string(new ReadOnlySpan<char>(characters, 0, 1)) != "q")
    return 3;

unsafe
{
    fixed (char* pointer = characters)
    {
        if (new string(pointer) != "q" || new string(pointer, 0, 1) != "q")
            return 4;
    }

    sbyte[] bytes = [113, 0];
    fixed (sbyte* pointer = bytes)
    {
        if (new string(pointer) != "q" || new string(pointer, 0, 1) != "q"
            || new string(pointer, 0, 1, Encoding.UTF8) != "q")
            return 5;
    }
}

Func<string, string> intern = string.Intern;
if (!ReferenceEquals(intern(new string('q', 32)), intern(new string('q', 32))))
    return 6;

int value = 3;
if (Interlocked.CompareExchange(ref value, 9, 3) != 3 || value != 9)
    return 7;

byte[] buffer = new byte[8192];
buffer[0] = 42;
Buffer.BlockCopy(buffer, 0, buffer, 1, buffer.Length - 1);
if (buffer[1] != 42 || buffer[2] != 0)
    return 8;

Array.Clear(buffer);
foreach (byte item in buffer)
{
    if (item != 0)
        return 9;
}

Console.WriteLine("TestOutput -> CoreLib entrypoint trimming sample passed");
return 42;
