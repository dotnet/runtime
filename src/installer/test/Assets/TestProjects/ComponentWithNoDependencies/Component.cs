// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Component
{
    // All component exports should have a version marked with UnmanagedCallersOnlyAttribute.
    // Both versions are implicitly run in the nativehost runner. For methods marked with
    // UnmanagedCallersOnlyAttribute, a prefix of "Unmanaged" is assumed by the nativehost runner.
    public class Component
    {
        private static int componentCallCount = 0;
        private static int entryPoint1CallCount = 0;
        private static int entryPoint2CallCount = 0;

        public static int ComponentEntryPoint1(IntPtr arg, int size)
        {
            componentCallCount++;
            entryPoint1CallCount++;
            Console.WriteLine($"Called {nameof(ComponentEntryPoint1)}(0x{arg.ToString("x")}, {size}) - component call count: {componentCallCount}");
            return entryPoint1CallCount;
        }

        public static int ComponentEntryPoint2(IntPtr arg, int size)
        {
            componentCallCount++;
            entryPoint2CallCount++;
            Console.WriteLine($"Called {nameof(ComponentEntryPoint2)}(0x{arg.ToString("x")}, {size}) - component call count: {componentCallCount}");
            return entryPoint2CallCount;
        }

        public static int ThrowException(IntPtr arg, int size)
        {
            componentCallCount++;
            Console.WriteLine($"Called {nameof(ThrowException)}(0x{arg.ToString("x")}, {size}) - component call count: {componentCallCount}");
            throw new InvalidOperationException(nameof(ThrowException));
        }

        [UnmanagedCallersOnly]
        public static int UnmanagedComponentEntryPoint1(IntPtr arg, int size)
        {
            return ComponentEntryPoint1(arg, size);
        }

        [UnmanagedCallersOnly]
        public static int UnmanagedComponentEntryPoint2(IntPtr arg, int size)
        {
            return ComponentEntryPoint2(arg, size);
        }

        [UnmanagedCallersOnly]
        public static int UnmanagedThrowException(IntPtr arg, int size)
        {
            return ThrowException(arg, size);
        }
    }
}