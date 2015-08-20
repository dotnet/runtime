// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Xunit;


public class ConditionalWeakTableTests
{
    [Fact]
    public static void AddTest()
    {
        ConditionalWeakTable<object, object> cwt = new ConditionalWeakTable<object, object>();
        object key = new object();
        object value = new object();
        object obj = null;

        cwt.Add(key, value);

        Assert.True(cwt.TryGetValue(key, out value));
        Assert.Equal(value, cwt.GetOrCreateValue(key));
        Assert.Equal(value, cwt.GetValue(key , k => new object()));

        WeakReference<object> wrValue = new WeakReference<object>(value, false);
        WeakReference<object> wrkey = new WeakReference<object>(key, false);
        key = null;
        value = null;

        GC.Collect();

        // key and value must be collected
        Assert.False(wrValue.TryGetTarget(out obj));
        Assert.False(wrkey.TryGetTarget(out obj));
    }

    [Fact]
    public static void GetOrCreateValueTest()
    {
        ConditionalWeakTable<object, object> cwt = new ConditionalWeakTable<object, object>();
        object key = new object();
        object obj = null;

        object value = cwt.GetOrCreateValue(key);

        Assert.True(cwt.TryGetValue(key, out value));
        Assert.Equal(value, cwt.GetValue(key, k => new object()));

        WeakReference<object> wrValue = new WeakReference<object>(value, false);
        WeakReference<object> wrkey = new WeakReference<object>(key, false);
        key = null;
        value = null;

        GC.Collect();

        // key and value must be collected
        Assert.False(wrValue.TryGetTarget(out obj));
        Assert.False(wrkey.TryGetTarget(out obj));
    }

    [Fact]
    public static void GetValueTest()
    {
        ConditionalWeakTable<object, object> cwt = new ConditionalWeakTable<object, object>();
        object key = new object();
        object obj = null;

        object value = cwt.GetValue(key, k => new object());

        Assert.True(cwt.TryGetValue(key, out value));
        Assert.Equal(value, cwt.GetOrCreateValue(key));

        WeakReference<object> wrValue = new WeakReference<object>(value, false);
        WeakReference<object> wrkey = new WeakReference<object>(key, false);
        key = null;
        value = null;

        GC.Collect();

        // key and value must be collected
        Assert.False(wrValue.TryGetTarget(out obj));
        Assert.False(wrkey.TryGetTarget(out obj));
    }
}

