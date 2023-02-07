// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.PortableExecutable;
using ILCompiler.PEWriter;

namespace r2rtarget
{
    internal sealed class Program
    {
        public static int Main(string[] args)
        {
            foreach (string file in args)
            {
                if (!DumpFile(file))
                {
                    return 1;
                }
            }
            return 0;
        }

        private static bool DumpFile(string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (PEReader pe = new PEReader(fs))
            {
                ushort r2rOsArch = (ushort)pe.PEHeaders.CoffHeader.Machine;
                foreach (MachineOSOverride os in Enum.GetValues(typeof(MachineOSOverride)))
                {
                    foreach (Machine architecture in Enum.GetValues(typeof(Machine)))
                    {
                        if (((ushort)architecture ^ (ushort)os) == (ushort)r2rOsArch)
                        {
                            Console.WriteLine("File: {0}", file);
                            Console.WriteLine("OS:   {0}", os);
                            Console.WriteLine("Arch: {0}", architecture);
                            return true;
                        }
                    }
                }
                Console.WriteLine("OS/Architecture code not recognized: {0:X8}", r2rOsArch);
                return false;
            }
        }
    }
}
