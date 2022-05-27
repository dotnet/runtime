// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal class MidObj
{
    public int i;
    public Object parent;

    public MidObj(int _i, Object obj)
    {
        i = _i;
        parent = obj;
    }
}

internal class MiddlePin
{
    public byte[] smallNonePinned; // this is very small, 3 or 4 ptr size
    public byte[] pinnedObj; // this is very small as well, 3 or 4 ptr size
    public byte[] nonePinned; // this is variable size
}

internal class MyRequest
{
    public GCHandle pin;
    public MidObj[] mObj;
    public byte[] obj;
    private bool _is_pinned;
    private static int s_count;

    // We distinguish between permanent and mid so we can free Mid and
    // influence the demotion effect. If Mid is a lot, we will hit
    // demotion; otherwise they will make into gen2.
    public MyRequest(int sizePermanent, int sizeMid, bool pinned)
    {
        if ((s_count % 32) == 0)
        {
            sizePermanent = 1;
            sizeMid = 1;
        }

        mObj = new MidObj[sizeMid];
        mObj[0] = new MidObj(s_count, this);
        obj = new byte[sizePermanent];
        FillInPin(obj);
        _is_pinned = pinned;
        if (pinned)
        {
            pin = GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        s_count++;
    }

    private void FillInPin(byte[] pinnedObj)
    {
        int len = pinnedObj.Length;
        int lenToFill = 10;

        if (lenToFill > len)
        {
            lenToFill = len - 1;
        }

        for (int i = 0; i < lenToFill; i++)
        {
            obj[len - i - 1] = (byte)(0x11 * i);
        }
    }

    public void Free()
    {
        if (_is_pinned)
        {
            lock (this)
            {
                if (_is_pinned)
                {
                    pin.Free();
                    _is_pinned = false;
                }
            }
        }
    }
}

internal class MemoryAlloc
{
    private MyRequest[] _old = null;
    private MyRequest[] _med = null;
    private int _num_old_data = 2000;
    private int _num_med_data = 200;

    private int _mean_old_alloc_size = 1000;
    private int _mean_med_alloc_size = 300;
    private int _mean_young_alloc_size = 60;

    private int _old_time = 3;
    private int _med_time = 2;
    private int _young_time = 1;

    private int _index = 0;

    private int _iter_count = 0;
    private Rand _rand;

    // pinning 10%.
    private double _pinRatio = 0.1;

    private double _fragRatio = 0.2;

    public MemoryAlloc(int iter, Rand _rand, int i)
    {
        _iter_count = iter;
        if (_iter_count == 0)
            _iter_count = 1000;
        this._rand = _rand;
        _index = i;
    }

    public void RunTest()
    {
        AllocTest();
        SteadyState();
    }

    public void CreateFragmentation()
    {
        int iFreeInterval = (int)((double)1 / _fragRatio);
        //Console.WriteLine("{0}: Freeing approx every {1} object", index, iFreeInterval);

        int iNextFree = _rand.getRand(iFreeInterval) + 1;
        int iFreedObjects = 0;

        int iGen2Objects = 0;

        for (int j = 0; j < _old.Length; j++)
        {
            //int iGen = GC.GetGeneration(old[j].mObj);
            int iGen = _rand.getRand(3);
            //Console.WriteLine("old[{0}] is in gen{1}", j, iGen);

            if (iGen == 2)
            {
                iGen2Objects++;

                if (iGen2Objects == iNextFree)
                {
                    iFreedObjects++;
                    if (_old[j].mObj == null)
                    {
                        //Console.WriteLine("old[{0}].mObj is null", j);
                    }
                    else
                    {
                        int iLen = _old[j].mObj.Length;
                        _old[j].mObj = new MidObj[iLen];
                    }

                    int inc = _rand.getRand(iFreeInterval) + 1;
                    iNextFree += inc;
                }
            }
        }

        //        Console.WriteLine("there are {0} gen2 objects (total {1}), freed {2}", 
        //            iGen2Objects, old.Length, iFreedObjects);
    }

    // This pins every few objects (defined by pinRatio) in the old array. 
    // med is just created with non pinned.
    public void AllocTest()
    {
        //        Console.WriteLine(index + ": Allocating memory - old: " + num_old_data + "[~" + mean_old_alloc_size + "]; med: "
        //            + num_med_data + "[~" + mean_med_alloc_size + "]");

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Reset();
        stopwatch.Start();

        _old = new MyRequest[_num_old_data];
        _med = new MyRequest[_num_med_data];
        bool fIsPinned = false;
        int iPinInterval = (int)((double)1 / _pinRatio);
        //Console.WriteLine("Pinning approx every {0} object", iPinInterval);

        int iPinnedObject = 0;
        int iPinnedMidSize = 0;

        //int iNextPin = iPinInterval / 5 + rand.getRand(iPinInterval);
        int iNextPin = _rand.getRand(iPinInterval * 4) + 1;

        //Console.WriteLine("Pin object {0}", iNextPin);
        for (int j = 0; j < _old.Length; j++)
        {
            //old [j] =  new byte [GetSizeRandom (mean_old_alloc_size)];
            if (j == iNextPin)
            {
                fIsPinned = true;
                //iNextPin = j + iPinInterval / 5 + rand.getRand(iPinInterval);
                iNextPin = j + _rand.getRand(iPinInterval * 4) + 1;
                //Console.WriteLine("Pin object {0}", iNextPin);
            }
            else
            {
                fIsPinned = false;
            }

            iPinnedMidSize = _mean_old_alloc_size * 2;
            if (fIsPinned)
            {
                iPinnedObject++;

                if ((iPinnedObject % 10) == 0)
                {
                    iPinnedMidSize = _mean_old_alloc_size * 10;
                    //Console.WriteLine("{0}th pinned object, non pin size is {1}", iPinnedObject, iPinnedMidSize);
                }
                else
                {
                    //Console.WriteLine("{0}th pinned object, non pin size is {1}", iPinnedObject, iPinnedMidSize);
                }
            }

            //Console.WriteLine("item {0}: {1}, pin size: {2}, non pin size: {3}", j, (fIsPinned ? "pinned" : "not pinned"), mean_old_alloc_size, iPinnedMidSize);

            byte[] temp = new byte[_mean_med_alloc_size * 3];
            _old[j] = new MyRequest(_mean_old_alloc_size, iPinnedMidSize, fIsPinned);

            //if ((j % (old.Length / 10)) == 0)
            //{
            //    Console.WriteLine("{0}: allocated {1} on old array, {2}ms elapsed, Heap size {3}, gen0: {4}, gen1: {5}, gen2: {6})",
            //        index,
            //        j,
            //        (int)stopwatch.Elapsed.TotalMilliseconds,
            //        GC.GetTotalMemory(false),
            //        GC.CollectionCount(0),
            //        GC.CollectionCount(1),
            //        GC.CollectionCount(2));
            //}
        }

        //Console.WriteLine("pinned {0} objects out of {1}", iPinnedObject, old.Length);

        {
            //            Console.WriteLine("{0}: allocated {1} on old array, {2}ms elapsed, Heap size {3}, gen0: {4}, gen1: {5}, gen2: {6})",
            //                index,
            //                old.Length,
            //                (int)stopwatch.Elapsed.TotalMilliseconds,
            //                GC.GetTotalMemory(false),
            //                GC.CollectionCount(0),
            //                GC.CollectionCount(1),
            //                GC.CollectionCount(2));
        }

        for (int j = 0; j < _med.Length; j++)
        {
            //med [j] = new byte [GetSizeRandom (mean_med_alloc_size)];
            _med[j] = new MyRequest(_mean_med_alloc_size, (_mean_med_alloc_size * 2), false);
        }

        stopwatch.Stop();
        //        Console.WriteLine ("{0}: startup: {1:d} seconds({2:d} ms. Heap size {3})", 
        //            index, (int)stopwatch.Elapsed.TotalSeconds, (int)stopwatch.Elapsed.TotalMilliseconds,
        //            GC.GetTotalMemory(false));
    }

    public void SteadyState()
    {
        Console.WriteLine(_index + ": replacing old every " + _old_time + "; med every " + _med_time + ";creating young " + _young_time + "times ("
                          + "(size " + _mean_young_alloc_size + ")");

        Console.WriteLine("iterating {0} times", _iter_count);

        int iter_interval = _iter_count / 10;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Reset();
        stopwatch.Start();
        //int lastGen2Count = 0;
        int lastGen1Count = 0;

        int checkInterval = _old.Length / 10;
        int lastChecked = 0;
        int iCheckedEnd = 0;

        int timeoutInterval = 5;

        double pinRatio = 0.1;
        bool fIsPinned = false;
        int iPinInterval = (int)((double)1 / pinRatio);
        Console.WriteLine("Pinning every {0} object", iPinInterval);
        int iNextPin = _rand.getRand(iPinInterval * 4) + 1;

        int iPinnedObject = 0;
        int iPinnedMidSize = _mean_old_alloc_size * 2;

        int countObjects = 0;
        int countObjectsGen1 = 0;
        int[] countWithGen = new int[3];

        MiddlePin[] steadyPinningArray = new MiddlePin[100];
        GCHandle[] steadyPinningHandles = new GCHandle[100];
        int steadyPinningIndex = 0;
        for (steadyPinningIndex = 0; steadyPinningIndex < steadyPinningArray.Length; steadyPinningIndex++)
        {
            steadyPinningArray[steadyPinningIndex] = new MiddlePin();
            steadyPinningHandles[steadyPinningIndex] = new GCHandle();
        }

        for (int j = 0; j < _iter_count; j++)
        {
            if (steadyPinningIndex >= steadyPinningArray.Length)
            {
                steadyPinningIndex = 0;

                //                Console.WriteLine("steady array wrapped, press enter to continue");
                //                Console.ReadLine();
            }

            byte[] tempObj = new byte[1];
            steadyPinningArray[steadyPinningIndex].smallNonePinned = new byte[8];
            steadyPinningArray[steadyPinningIndex].pinnedObj = new byte[8];
            steadyPinningArray[steadyPinningIndex].nonePinned = new byte[24];
            steadyPinningHandles[steadyPinningIndex] = GCHandle.Alloc(steadyPinningArray[steadyPinningIndex].pinnedObj, GCHandleType.Pinned);
            steadyPinningArray[steadyPinningIndex].smallNonePinned[3] = 0x31;
            steadyPinningArray[steadyPinningIndex].smallNonePinned[5] = 0x51;
            steadyPinningArray[steadyPinningIndex].smallNonePinned[7] = 0x71;
            steadyPinningArray[steadyPinningIndex].pinnedObj[3] = 0x21;
            steadyPinningArray[steadyPinningIndex].pinnedObj[5] = 0x41;
            steadyPinningArray[steadyPinningIndex].pinnedObj[7] = 0x61;
            tempObj = new byte[256];

            steadyPinningIndex++;

            countObjects = 0;
            countObjectsGen1 = 0;

            iCheckedEnd = lastChecked + checkInterval;
            if (iCheckedEnd > _old.Length)
            {
                iCheckedEnd = _old.Length;
            }

            //Console.WriteLine("timing out item {0} to {1}", lastChecked, iCheckedEnd);

            // time out requests in this range.
            // for the range we are looking at time out requests (ie, end them and replace them with new ones).
            // we go from the beginning of the range to the end, time out every Nth one; then time out everyone
            // after that, and so on.
            for (int iIter = 0; iIter < timeoutInterval; iIter++)
            {
                for (int iCheckIndex = 0; iCheckIndex < ((iCheckedEnd - lastChecked) / timeoutInterval); iCheckIndex++)
                {
                    int iItemIndex = (lastChecked + iCheckIndex * timeoutInterval + iIter) % _old.Length;

                    // Console.WriteLine("replacing item {0}", iItemIndex);

                    _old[iItemIndex].Free();

                    countObjects++;
                    if ((countObjects % 10) == 0)
                    {
                        byte[] temp = new byte[_mean_med_alloc_size * 3];
                        temp[0] = (byte)27; // 0x1b
                    }
                    else if ((countObjects % 4) == 0)
                    {
                        byte[] temp = new byte[1];
                        temp[0] = (byte)27; // 0x1b
                    }

                    if (countObjects == iNextPin)
                    {
                        fIsPinned = true;
                        iNextPin += _rand.getRand(iPinInterval * 4) + 1;
                    }
                    else
                    {
                        fIsPinned = false;
                    }

                    iPinnedMidSize = _mean_old_alloc_size * 2;
                    if (fIsPinned)
                    {
                        iPinnedObject++;

                        if ((iPinnedObject % 10) == 0)
                        {
                            iPinnedMidSize = _mean_old_alloc_size * 10;
                        }
                    }

                    //Console.WriteLine("perm {0}, mid {1}, {2}", mean_old_alloc_size, iPinnedMidSize, (fIsPinned ? "pinned" : "not pinned"));
                    _old[iItemIndex] = new MyRequest(_mean_old_alloc_size, iPinnedMidSize, fIsPinned);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                countWithGen[i] = 0;
            }

            //            Console.WriteLine("Checking {0} to {1}", lastChecked, iCheckedEnd);

            for (int iItemIndex = lastChecked; iItemIndex < iCheckedEnd; iItemIndex++)
            {
                //int iGen = GC.GetGeneration(old[iItemIndex].mObj);
                int iGen = _rand.getRand(3);
                countWithGen[iGen]++;

                if (iGen == 1)
                {
                    //Console.WriteLine("item {0} is in gen1, getting rid of it", iItemIndex);
                    if ((countObjectsGen1 % 5) == 0)
                        _old[iItemIndex].mObj = null;
                    countObjectsGen1++;
                }
            }

            //            Console.WriteLine("{0} in gen0, {1} in gen1, {2} in gen2",
            //                countWithGen[0],
            //                countWithGen[1],
            //                countWithGen[2]);
            //
            //            Console.WriteLine("{0} objects out of {1} are in gen1", countObjectsGen1, (iCheckedEnd - lastChecked));

            if (iCheckedEnd == _old.Length)
            {
                lastChecked = 0;
            }
            else
            {
                lastChecked += checkInterval;
            }

            int currentGen1Count = GC.CollectionCount(1);
            if ((currentGen1Count - lastGen1Count) > 30)
            {
                GC.Collect(2, GCCollectionMode.Forced, false);
                Console.WriteLine("{0}: iter {1}, heap size: {2}", _index, j, GC.GetTotalMemory(false));

                lastGen1Count = currentGen1Count;
            }
        }

        for (steadyPinningIndex = 0; steadyPinningIndex < steadyPinningArray.Length; steadyPinningIndex++)
        {
            if (steadyPinningHandles[steadyPinningIndex].IsAllocated)
                steadyPinningHandles[steadyPinningIndex].Free();
        }

        stopwatch.Stop();
        Console.WriteLine("{0}: steady: {1:d} seconds({2:d} ms. Heap size {3})",
            _index, (int)stopwatch.Elapsed.TotalSeconds, (int)stopwatch.Elapsed.TotalMilliseconds,
            GC.GetTotalMemory(false));
    }
}

internal class FreeListTest
{
    private static int s_iLastGen0Count;
    private static int s_iLastGen1Count;

    private static void InducedGen2()
    {
        int iCurrentGen0Count = GC.CollectionCount(0);
        if ((iCurrentGen0Count - s_iLastGen0Count) > 50)
        {
            //Console.WriteLine("we've done {0} gen0 GCs, inducing a gen2", (iCurrentGen0Count - iLastGen0Count));
            s_iLastGen0Count = iCurrentGen0Count;
            GC.Collect(2, GCCollectionMode.Forced, false);
            //GC.Collect(2);
        }

        int iCurrentGen1Count = GC.CollectionCount(1);
        if ((iCurrentGen1Count - s_iLastGen1Count) > 10)
        {
            //Console.WriteLine("we've done {0} gen1 GCs, inducing a gen2", (iCurrentGen1Count - iLastGen1Count));
            s_iLastGen1Count = iCurrentGen1Count;
            GC.Collect(2, GCCollectionMode.Forced, false);
        }
    }

    public static int Main(string[] args)
    {
        if (GCSettings.IsServerGC == true)
        {
            Console.WriteLine("we are using server GC");
        }
        int iter_num = 500000;
        if (args.Length >= 1)
        {
            iter_num = int.Parse(args[0]);
            Console.WriteLine("iterating {0} times", iter_num);
        }

        // For now just do everything on the main thread.
        //int threadCount = 8;
        //        int threadCount = 1;
        //        if (args.Length >= 2)
        //        {
        //            threadCount = int.Parse(args[1]);
        //            Console.WriteLine ("creating {0} threads", threadCount);
        //        }

        long tStart, tEnd;
        tStart = Environment.TickCount;
        //        MyThread t;
        //        ThreadStart ts;
        //        Thread[] threads = new Thread[threadCount];
        //
        //        for (int i = 0; i < threadCount; i++)
        //        {
        //            t = new MyThread(i, iter_num, old, med);
        //            ts = new ThreadStart(t.TimeTest);
        //            threads[i] = new Thread( ts );
        //            threads[i].Start();
        //        }
        //
        //        for (int i = 0; i < threadCount; i++)
        //        {
        //            threads[i].Join();
        //        }
        //

        Console.WriteLine("start with {0} gen1 GCs", s_iLastGen1Count);

        s_iLastGen0Count = GC.CollectionCount(0);
        s_iLastGen1Count = GC.CollectionCount(1);

        for (int iter = 0; iter < 1; iter++)
        {
            MemoryAlloc[] maArr = new MemoryAlloc[16];

            Rand rand = new Rand();
            for (int i = 0; i < maArr.Length; i++)
            {
                maArr[i] = new MemoryAlloc(rand.getRand(500), rand, i);
                maArr[i].AllocTest();
                //Console.WriteLine("{0} allocated", i);
                InducedGen2();

                for (int iAllocated = 0; iAllocated < i; iAllocated++)
                {
                    //Console.WriteLine("creating fragmentation in obj {0}", iAllocated);
                    maArr[iAllocated].CreateFragmentation();
                    InducedGen2();
                }
            }

            for (int i = 0; i < maArr.Length; i++)
            {
                InducedGen2();
                Console.WriteLine("steady state for " + i);
                maArr[i].SteadyState();
                Console.WriteLine("DONE: steady state for " + i);
            }
        }

        tEnd = Environment.TickCount;
        Console.WriteLine("Test completed; " + (tEnd - tStart) + "ms");
        //        Console.WriteLine("Press any key to exit.");
        //        Console.ReadLine();

        return 100;
    }
};

internal sealed class Rand
{
    /* Generate Random numbers
     */
    private int _x = 0;

    public int getRand()
    {
        _x = (314159269 * _x + 278281) & 0x7FFFFFFF;
        return _x;
    }

    // obtain random number in the range 0 .. r-1
    public int getRand(int r)
    {
        // require r >= 0
        int x = (int)(((long)getRand() * r) >> 31);
        return x;
    }
    public double getFloat()
    {
        return (double)getRand() / (double)0x7FFFFFFF;
    }
};
