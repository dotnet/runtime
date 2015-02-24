// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// the hashtable has a race:
///     A read operation on hashtable has three steps:
///        (1) calculate the hash and find the slot number.
///        (2) compare the hashcode, if equal, go to step 3. Otherwise end.
///        (3) compare the key, if equal, go to step 4. Otherwise end.
///        (4) return the value contained in the bucket.
///     The problem is that after step 3 and before step 4. A writer can kick in a remove the old item and add a new one 
///     in the same bukcet. In order to make this happen easily, I created two long with same hashcode.
/// </summary>
public class ItemThreadSafetyTests
{
    private object _key1;
    private object _key2;
    private object _value1 = "value1";
    private object _value2 = "value2";
    private Hashtable _ht;

    private bool _errorOccured = false;
    private bool _timeExpired = false;

    private const int MAX_TEST_TIME_MS = 10000; // 10 seconds

    [Fact]
    [OuterLoop]
    public void TestGetItemThreadSafety()
    {
        int i1 = 0x10;
        int i2 = 0x100;
        long l1;
        long l2;

        //Setup key1 and key2 so they are different values but have the same hashcode
        //To produce a hashcode long XOR's the first 32bits with the last 32 bits
        l1 = (((long) i1) << 32) + i2;
        l2 = (((long) i2) << 32) + i1;
        _key1 = (object) l1;
        _key2 = (object) l2;

        _ht = new Hashtable(3); //Just one item will be in the hashtable at a time
        int taskCount = 3;
        Task[] readers1 = new Task[taskCount];
        Task[] readers2 = new Task[taskCount];
        Task writer;

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < readers1.Length; i++)
        {
            readers1[i] = Task.Run(new Action(ReaderFunction1));
        }

        for (int i = 0; i < readers2.Length; i++)
        {
            readers2[i] = Task.Run(new Action(ReaderFunction2));
        }

        writer = Task.Run(new Action(WriterFunction));

        SpinWait spin = new SpinWait();
        while (!_errorOccured && !_timeExpired)
        {
            if (MAX_TEST_TIME_MS < stopwatch.ElapsedMilliseconds)
            {
                _timeExpired = true;
            }

            spin.SpinOnce();
        }

        Task.WaitAll(readers1);
        Task.WaitAll(readers2);
        writer.Wait();

        Assert.False(_errorOccured);
    }

    private void ReaderFunction1()
    {
        while (!_timeExpired)
        {
            object value = _ht[_key1];

            if (value != null)
            {
                Assert.False(value.Equals(_value2));
            }
        }
    }

    private void ReaderFunction2()
    {
        while (!_errorOccured && !_timeExpired)
        {
            object value = _ht[_key2];
            if (value != null)
            {
                Assert.False(value.Equals(_value1));
            }
        }
    }

    private void WriterFunction()
    {
        while (!_errorOccured && !_timeExpired)
        {
            _ht.Add(_key1, _value1);
            _ht.Remove(_key1);
            _ht.Add(_key2, _value2);
            _ht.Remove(_key2);
        }
    }
}
