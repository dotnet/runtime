// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics
{
    public unsafe struct TestTable<T, TIndex> : IDisposable where T : struct where TIndex : struct
    {
        public T[] InArray;
        public T[] OutArray;

        public void* InArrayPtr => _inHandle.AddrOfPinnedObject().ToPointer();
        public void* OutArrayPtr => _outHandle.AddrOfPinnedObject().ToPointer();

        private GCHandle _inHandle;
        private GCHandle _outHandle;

        public TestTable(T[] a, T[] b)
        {
            InArray = a;
            OutArray = b;

            _inHandle = GCHandle.Alloc(InArray, GCHandleType.Pinned);
            _outHandle = GCHandle.Alloc(OutArray, GCHandleType.Pinned);
        }
        public bool CheckResult(Func<T, T, bool> check, TIndex[] indexArray)
        {
            int length = Math.Min(indexArray.Length, OutArray.Length);
            for (int i = 0; i < length; i++)
            {
                if (!check(InArray[Convert.ToInt32(indexArray[i])], OutArray[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public void Dispose()
        {
            _inHandle.Free();
            _outHandle.Free();
        }
    }
}
