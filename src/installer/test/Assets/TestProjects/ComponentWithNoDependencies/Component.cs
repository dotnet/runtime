// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Component
{
    public class Component
    {
        private static int componentCallCount = 0;
        private static int entryPoint1CallCount = 0;
        private static int entryPoint2CallCount = 0;
        private static int unmanagedEntryPoint1CallCount = 0;

        private static void PrintComponentCallLog(string name, IntPtr arg, int size)
        {
            Console.WriteLine($"Called {name}(0x{arg.ToString("x")}, {size}) - component call count: {componentCallCount}");
        }

        public static int ComponentEntryPoint1(IntPtr arg, int size)
        {
            componentCallCount++;
            entryPoint1CallCount++;
            PrintComponentCallLog(nameof(ComponentEntryPoint1), arg, size);
            return entryPoint1CallCount;
        }

        public static int ComponentEntryPoint2(IntPtr arg, int size)
        {
            componentCallCount++;
            entryPoint2CallCount++;
            PrintComponentCallLog(nameof(ComponentEntryPoint2), arg, size);
            return entryPoint2CallCount;
        }

        public static int ThrowException(IntPtr arg, int size)
        {
            componentCallCount++;
            PrintComponentCallLog(nameof(ThrowException), arg, size);
            throw new InvalidOperationException(nameof(ThrowException));
        }

        [UnmanagedCallersOnly]
        public static int UnmanagedComponentEntryPoint1(IntPtr arg, int size)
        {
            componentCallCount++;
            unmanagedEntryPoint1CallCount++;
            PrintComponentCallLog(nameof(UnmanagedComponentEntryPoint1), arg, size);
            return unmanagedEntryPoint1CallCount;
        }
    }
}