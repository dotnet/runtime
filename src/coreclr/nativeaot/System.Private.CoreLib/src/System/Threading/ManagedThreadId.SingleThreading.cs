// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal class ManagedThreadId
    {
        public const int IdNone = 0;
        public const int IdMainThread = 1;

        private static int s_threadIdCounter;
        private int _managedThreadId;

        public int Id => _managedThreadId;

        internal static int CurrentManagedThreadIdUnchecked => IdMainThread;

        public static int Current => IdMainThread;

        public static ManagedThreadId GetCurrentThreadId() => new ManagedThreadId(IdMainThread);

        public ManagedThreadId()
        {
            // Unstarted Thread objects still need distinct IDs, separate from the executing thread.
            int id = checked(s_threadIdCounter + IdMainThread + 1);
            s_threadIdCounter++;
            _managedThreadId = id;
        }

        private ManagedThreadId(int id)
        {
            _managedThreadId = id;
        }
    }
}
