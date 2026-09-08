// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

// Regression test for https://github.com/dotnet/runtime/issues/131537
// (WASM manifestation of https://github.com/dotnet/runtime/issues/66220).
//
// Constructing a non-generic Queue from a List<Nullable<T>> enumerates the list
// through the non-generic IEnumerator, whose Current getter boxes each element
// Nullable<T> -> object. On Mono WASM with an AOT'd corelib and this assembly
// running in the interpreter (the WasmTestOnChrome-MONO-ST configuration), the
// concrete Nullable<T> is never instantiated in the AOT'd corelib's TYPESPEC
// table, so the required gsharedvt out-sig wrapper object(Nullable<T>) is not
// emitted and the boxing call traps with "function signature mismatch".
public class Test_131537
{
    [Fact]
    public static void BoxNullableThroughNonGenericEnumerator()
    {
        RoundTrip(new List<Int128?> { 1, 2, 3 });
        RoundTrip(new List<UInt128?> { 1, 2, 3 });
        RoundTrip(new List<Half?> { (Half)1, (Half)2, (Half)3 });
        RoundTrip(new List<decimal?> { 1m, 2m, 3m });
        RoundTrip(new List<Guid?> { Guid.Empty, Guid.NewGuid() });
        RoundTrip(new List<DateTime?> { DateTime.UnixEpoch, DateTime.MaxValue });
        RoundTrip(new List<DateTimeOffset?> { DateTimeOffset.UnixEpoch, DateTimeOffset.MaxValue });
        RoundTrip(new List<TimeSpan?> { TimeSpan.Zero, TimeSpan.FromSeconds(5) });
    }

    private static void RoundTrip<T>(List<T> source)
    {
        // Queue(ICollection) enumerates via the non-generic IEnumerator, boxing
        // each element T -> object. This is the call that crashes when the
        // object(Nullable<T>) gsharedvt out-sig wrapper is missing.
        var queue = new Queue(source);
        Assert.Equal(source.Count, queue.Count);

        int i = 0;
        foreach (object boxed in queue)
        {
            Assert.Equal(source[i], (T)boxed);
            i++;
        }
    }
}
