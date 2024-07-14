// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

Console.WriteLine(@"Reference opcodes
This file is presently only for human consumption
This file is generated from opcode.def using the genrops.pl script

Name                     String Name              refop    encode
-----------------------------------------------------------------");

int ret = 0;
var oneByte = new SortedDictionary<int, string>();
var twoByte = new SortedDictionary<int, string>();
var deprecated = new List<string>();
int count = 0;

string? line;
while ((line = Console.ReadLine()) is not null)
{
    // Process only OPDEF(....) lines
    if (line.StartsWith("OPDEF("))
    {
        line = line.TrimEnd(); // Strip off trailing CR
        line = line.Substring("OPDEF(".Length); // Strip off "OP("
        line = line.TrimEnd(')'); // Strip off ")" at end
        line = line.Replace(", ", ","); // Remove whitespace

        // Split the line up into its basic parts
        var parts = line.Split(',');
        string enumname = parts[0];
        string stringname = parts[1].Trim();
        // string pop = parts[2];
        // string push = parts[3];
        // string operand = parts[4];
        // string type = parts[5];
        int size = int.Parse(parts[6]);
        string part7 = parts[7].Trim();
        string part8 = parts[8].Trim();
        int s1 = part7 == "MOOT" ? 0 : Convert.ToInt32(part7, 16);
        int s2 = part8 == "MOOT" ? 0 : Convert.ToInt32(part8, 16);
        // string ctrl = parts[9];

        string outputLine = string.Format("{0,-24} {1,-24} 0x{2:x3}",
                                          enumname, stringname, count);
        if (size == 1)
        {
            outputLine += string.Format("    0x{0:x2}\n", s2);
            if (oneByte.TryGetValue(s2, out string? old))
            {
                Console.WriteLine($"Error opcode 0x{s2:x} already defined!");
                Console.WriteLine("   Old = " + old);
                Console.WriteLine("   New = " + outputLine);
                ret = -1;
            }
            oneByte[s2] = outputLine;
        }
        else if (size == 2)
        {
            if (twoByte.ContainsKey(s2 + 256 * s1))
            {
                Console.WriteLine($"Error opcode 0x{s1:x} 0x{s2:x} already defined!");
                Console.WriteLine("   Old = " + twoByte[s2 + 256 * s1]);
                Console.WriteLine("   New = " + outputLine);
                ret = -1;
            }
            outputLine += string.Format("    0x{0:x2} 0x{1:x2}\n", s1, s2);
            twoByte[s2 + 256 * s1] = outputLine;
        }
        else
        {
            outputLine += "\n";
            deprecated.Add(outputLine);
        }
        count++;
    }
}

int lastOp = -1;
foreach (var kvp in oneByte)
{
    int opcode = kvp.Key;
    if (lastOp + 1 != opcode && lastOp > 0)
    {
        Console.WriteLine($"***** GAP {opcode - lastOp} instrs ****");
    }
    Console.Write(kvp.Value);
    lastOp = opcode;
}

lastOp = -1;
foreach (var kvp in twoByte)
{
    int opcode = kvp.Key;
    if (lastOp + 1 != opcode && lastOp > 0)
    {
        Console.WriteLine($"***** GAP {opcode - lastOp} instrs ****");
    }
    Console.Write(kvp.Value);
    lastOp = opcode;
}

foreach (var lineDep in deprecated)
{
    Console.Write(lineDep);
}

return ret;
