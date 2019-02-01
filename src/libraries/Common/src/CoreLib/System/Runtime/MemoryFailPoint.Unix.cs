// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime
{
    public sealed partial class MemoryFailPoint
    {
        private static ulong GetTopOfMemory()
        {
            // These values are optimistic assumptions. In reality the value will
            // often be lower.
            return IntPtr.Size == 4 ? uint.MaxValue : ulong.MaxValue;
        }

        private static bool CheckForAvailableMemory(out ulong availPageFile, out ulong totalAddressSpaceFree)
        {
            // TODO: Implement
            availPageFile = 0;
            totalAddressSpaceFree = 0;
            return false;
        }

        // Based on the shouldThrow parameter, this will throw an exception, or 
        // returns whether there is enough space.  In all cases, we update
        // our last known free address space, hopefully avoiding needing to 
        // probe again.
        private static bool CheckForFreeAddressSpace(ulong size, bool shouldThrow)
        {
            // Unreachable until CheckForAvailableMemory is implemented
            return false;
        }

        // Allocate a specified number of bytes, commit them and free them. This should enlarge
        // page file if necessary and possible.
        private static void GrowPageFileIfNecessaryAndPossible(UIntPtr numBytes)
        {
            // Unreachable until CheckForAvailableMemory is implemented
        }
    }
}
