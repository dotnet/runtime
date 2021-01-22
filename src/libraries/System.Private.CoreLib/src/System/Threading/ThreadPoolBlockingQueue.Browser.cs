// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public static partial class ThreadPool
    {
        internal static void RecordBlockingCallsite() {}
    }

    internal class ThreadPoolBlockingQueue
    {
        public static bool IsEnabled => false;
        public static object Enqueue(object workItem) => workItem;
        public static bool RequiresMitigation(object? workItem) => false;
        public static void RegisterForBlockingDetection(object? workItem) { }
        public static void ClearRegistration() { }
        public static void Enable() { }
    }
}
