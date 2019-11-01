using System;

namespace Component
{
    public class Component
    {
        private static int componentCallCount = 0;
        private static int entryPoint1CallCount = 0;
        private static int entryPoint2CallCount = 0;

        public static int ComponentEntryPoint1(IntPtr arg, int size)
        {
            componentCallCount++;
            entryPoint1CallCount++;
            Console.WriteLine($"Called ComponentEntryPoint1(0x{arg.ToString("x")}, {size}) - component call count: {componentCallCount}");
            return entryPoint1CallCount;
        }

        public static int ComponentEntryPoint2(IntPtr arg, int size)
        {
            componentCallCount++;
            entryPoint2CallCount++;
            Console.WriteLine($"Called ComponentEntryPoint2(0x{arg.ToString("x")}, {size}) - component call count: {componentCallCount}");
            return entryPoint2CallCount;
        }
    }
}