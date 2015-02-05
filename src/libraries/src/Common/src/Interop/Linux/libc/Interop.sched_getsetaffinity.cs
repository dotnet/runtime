// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using cpu_mask = System.Int64;
using pid_t = System.Int32;
using size_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern unsafe int sched_setaffinity(pid_t pid, size_t cpusetsize, cpu_set_t* mask);

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern unsafe int sched_getaffinity(pid_t pid, size_t cpusetsize, cpu_set_t* mask);

        internal unsafe struct cpu_set_t
        {
            internal fixed cpu_mask bits[CPU_SETSIZE / NCPUBITS];
        }

        internal static unsafe void CPU_SET(int cpu, cpu_set_t* set)
        {
            set->bits[CPUELT(cpu)] |= CPUMASK(cpu);
        }

        internal static unsafe bool CPU_ISSET(int cpu, cpu_set_t* set)
        {
            return (set->bits[CPUELT(cpu)] & CPUMASK(cpu)) != 0;
        }

        private const int CPU_SETSIZE = 1024;
        private const int NCPUBITS = 64; // 8 * sizeof(cpu_set_t)

        private static int CPUELT(int cpu)
        {
            return cpu / NCPUBITS;
        }

        private static cpu_mask CPUMASK(int cpu)
        {
            return (cpu_mask)1 << (cpu % NCPUBITS);
        }
    }
}
