// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/131537
// (WASM manifestation of https://github.com/dotnet/runtime/issues/66220).
//
// Constructing a non-generic Queue from a List<Nullable<T>> enumerates the list
// through the non-generic IEnumerator, whose Current getter boxes each element
// Nullable<T> -> object. With corelib AOT'd and this assembly running in the
// interpreter (see _AOT_InternalForceInterpretAssemblies in the csproj), the
// concrete Nullable<T> is instantiated only in the interpreter, so the AOT
// compiler never emits the required gsharedvt out-sig wrapper object(Nullable<T>)
// and the boxing call traps with "function signature mismatch" (assertion in
// mini-generic-sharing.c). The 16-byte payload types (Int128?/Guid?/decimal?)
// are the ones that regress.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;

namespace Sample
{
    public partial class Test
    {
        public static void Main()
        {
            Console.WriteLine("Test_131537 loaded");
        }

        [JSExport]
        public static int TestMeaning()
        {
            RoundTrip(new List<Int128?> { 1, 2, 3 });
            RoundTrip(new List<UInt128?> { 1, 2, 3 });
            RoundTrip(new List<Half?> { (Half)1, (Half)2, (Half)3 });
            RoundTrip(new List<decimal?> { 1m, 2m, 3m });
            RoundTrip(new List<Guid?> { Guid.Empty, Guid.NewGuid() });
            RoundTrip(new List<DateTime?> { DateTime.UnixEpoch, DateTime.MaxValue });
            RoundTrip(new List<DateTimeOffset?> { DateTimeOffset.UnixEpoch, DateTimeOffset.MaxValue });
            RoundTrip(new List<TimeSpan?> { TimeSpan.Zero, TimeSpan.FromSeconds(5) });

            // 42 == pass, per the WebAssembly functional test convention.
            return 42;
        }

        private static void RoundTrip<T>(List<T> source)
        {
            // Queue(ICollection) enumerates via the non-generic IEnumerator, boxing
            // each element T -> object. This is the call that crashes when the
            // object(Nullable<T>) gsharedvt out-sig wrapper is missing.
            var queue = new Queue(source);
            if (queue.Count != source.Count)
            {
                throw new Exception($"count mismatch: {queue.Count} != {source.Count}");
            }

            int i = 0;
            foreach (object boxed in queue)
            {
                if (!Equals(source[i], (T)boxed))
                {
                    throw new Exception($"element {i} mismatch");
                }
                i++;
            }
        }
    }
}
