// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;  
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

class MidObj
{
    public int i;
    public Object parent; 

    public MidObj(int _i, Object obj)
    {
        i = _i;
        parent = obj;
    }
}

class MiddlePin
{
    public byte[] smallNonePinned; // this is very small, 3 or 4 ptr size
    public byte[] pinnedObj; // this is very small as well, 3 or 4 ptr size
    public byte[] nonePinned; // this is variable size
}

class MyRequest
{
    public GCHandle pin;
    public MidObj[] mObj;
    public byte[] obj;
    private bool is_pinned;
    static int count;

    // We distinguish between permanent and mid so we can free Mid and
    // influence the demotion effect. If Mid is a lot, we will hit
    // demotion; otherwise they will make into gen2.
    public MyRequest(int sizePermanent, int sizeMid, bool pinned)
    {
        if ((count % 32) == 0)
        {
            sizePermanent = 1;
            sizeMid = 1;
        }

        mObj = new MidObj[sizeMid];
        mObj[0] = new MidObj(count, this);
        obj = new byte[sizePermanent];
        FillInPin(obj);
        is_pinned = pinned;
        if (pinned)
        {
            pin = GCHandle.Alloc(obj, GCHandleType.Pinned);
        }

        count++;
    }

    void FillInPin(byte[] pinnedObj)
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
        if (is_pinned)
        {
            lock(this)
            {
                if (is_pinned)
                {
                    pin.Free();
                    is_pinned = false;
                }
            }
        }
    }
}

class MemoryAlloc 
{
    private MyRequest[] old = null;
    private MyRequest[] med = null;
    private int num_old_data = 2000;
    private int num_med_data = 200;

    private int mean_old_alloc_size = 1000;
    private int mean_med_alloc_size = 300;
    private int mean_young_alloc_size = 60;

    private int old_time = 3;
    private int med_time = 2;
    private int young_time = 1;

    private int index = 0;

    private int iter_count = 0;
    private Rand rand;

    // pinning 10%.
    private double pinRatio = 0.1;

    private double fragRatio = 0.2;

    public MemoryAlloc (int iter, Rand _rand, int i)
    {
        iter_count = iter;
        if (iter_count == 0)
            iter_count = 1000;
        rand = _rand;
        index = i;
    }

    public void RunTest()
    {
        AllocTest();
        SteadyState();
    }

    public void CreateFragmentation()
    {
        int iFreeInterval = (int)((double)1 / fragRatio);
        //Console.WriteLine("{0}: Freeing approx every {1} object", index, iFreeInterval);

        int iNextFree = rand.getRand(iFreeInterval) + 1;
        int iFreedObjects = 0;

        int iGen2Objects = 0;

        for (int j = 0; j < old.Length; j++)
        {
            //int iGen = GC.GetGeneration(old[j].mObj);
            int iGen = rand.getRand (3);
            //Console.WriteLine("old[{0}] is in gen{1}", j, iGen);
                
            if (iGen == 2)
            {
                iGen2Objects++;

                if (iGen2Objects == iNextFree)
                {
                    iFreedObjects++;
                    if (old[j].mObj == null)
                    {
                        //Console.WriteLine("old[{0}].mObj is null", j);
                    }
                    else
                    {
                        int iLen = old[j].mObj.Length;
                        old[j].mObj = new MidObj[iLen];
                    }

                    int inc = rand.getRand(iFreeInterval) + 1;
                    iNextFree += inc;
                }
            }
        }

//        Console.WriteLine("there are {0} gen2 objects (total {1}), freed {2}", 
//            iGen2Objects, old.Length, iFreedObjects);
    }

    // This pins every few objects (defined by pinRatio) in the old array. 
    // med is just created with non pinned.
    public void AllocTest () 
    {
//        Console.WriteLine(index + ": Allocating memory - old: " + num_old_data + "[~" + mean_old_alloc_size + "]; med: "
//            + num_med_data + "[~" + mean_med_alloc_size + "]");

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Reset();
        stopwatch.Start();

        old = new MyRequest[num_old_data];
        med = new MyRequest[num_med_data];
        bool fIsPinned = false;
        int iPinInterval = (int)((double)1 / pinRatio);
        //Console.WriteLine("Pinning approx every {0} object", iPinInterval);

        int iPinnedObject = 0;
        int iPinnedMidSize = 0;

        //int iNextPin = iPinInterval / 5 + rand.getRand(iPinInterval);
        int iNextPin = rand.getRand(iPinInterval * 4 ) + 1;

        //Console.WriteLine("Pin object {0}", iNextPin);
        for (int j = 0; j < old.Length; j++) 
        {
            //old [j] =  new byte [GetSizeRandom (mean_old_alloc_size)];
            if (j == iNextPin)
            {
                fIsPinned = true;
                //iNextPin = j + iPinInterval / 5 + rand.getRand(iPinInterval);
                iNextPin = j + rand.getRand(iPinInterval * 4) + 1;
                //Console.WriteLine("Pin object {0}", iNextPin);
            }
            else
            {
                fIsPinned = false;
            }

            iPinnedMidSize = mean_old_alloc_size * 2;
            if (fIsPinned)
            {
                iPinnedObject++;

                if ((iPinnedObject % 10) == 0)
                {
                    iPinnedMidSize = mean_old_alloc_size * 10;
                    //Console.WriteLine("{0}th pinned object, non pin size is {1}", iPinnedObject, iPinnedMidSize);
                }
                else
                {
                    //Console.WriteLine("{0}th pinned object, non pin size is {1}", iPinnedObject, iPinnedMidSize);
                }
            }

            //Console.WriteLine("item {0}: {1}, pin size: {2}, non pin size: {3}", j, (fIsPinned ? "pinned" : "not pinned"), mean_old_alloc_size, iPinnedMidSize);

            byte[] temp = new byte[mean_med_alloc_size * 3];
            old[j] = new MyRequest(mean_old_alloc_size, iPinnedMidSize, fIsPinned);

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

        for (int j = 0; j < med.Length; j++) 
        {
            //med [j] = new byte [GetSizeRandom (mean_med_alloc_size)];
            med[j] = new MyRequest(mean_med_alloc_size, (mean_med_alloc_size* 2), false);
        }

        stopwatch.Stop();
//        Console.WriteLine ("{0}: startup: {1:d} seconds({2:d} ms. Heap size {3})", 
//            index, (int)stopwatch.Elapsed.TotalSeconds, (int)stopwatch.Elapsed.TotalMilliseconds,
//            GC.GetTotalMemory(false));
    }

    public void SteadyState()
    {
        Console.WriteLine(index + ": replacing old every " + old_time + "; med every " + med_time + ";creating young " + young_time + "times ("
                          + "(size " + mean_young_alloc_size + ")");

        Console.WriteLine("iterating {0} times", iter_count);

        int iter_interval = iter_count / 10;
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Reset();
        stopwatch.Start();
        //int lastGen2Count = 0;
        int lastGen1Count = 0;

        int checkInterval = old.Length / 10;
        int lastChecked = 0;
        int iCheckedEnd = 0;

        int timeoutInterval = 5;

        double pinRatio = 0.1;
        bool fIsPinned = false;
        int iPinInterval = (int)((double)1 / pinRatio);
        Console.WriteLine("Pinning every {0} object", iPinInterval);
        int iNextPin = rand.getRand(iPinInterval * 4) + 1; 

        int iPinnedObject = 0;
        int iPinnedMidSize = mean_old_alloc_size * 2;

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

        for (int j = 0; j < iter_count; j++)
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
            if (iCheckedEnd > old.Length)
            {
                iCheckedEnd = old.Length;
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
                    int iItemIndex = (lastChecked + iCheckIndex * timeoutInterval + iIter) % old.Length;

                   // Console.WriteLine("replacing item {0}", iItemIndex);

                    old[iItemIndex].Free();

                    countObjects++;
                    if ((countObjects % 10) == 0)
                    {
                        byte[] temp = new byte[mean_med_alloc_size * 3];
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
                        iNextPin += rand.getRand(iPinInterval * 4) + 1;
                    }
                    else
                    {
                        fIsPinned = false;
                    }

                    iPinnedMidSize = mean_old_alloc_size * 2;
                    if (fIsPinned)
                    {
                        iPinnedObject++;

                        if ((iPinnedObject % 10) == 0)
                        {
                            iPinnedMidSize = mean_old_alloc_size * 10;
                        }
                    }

                    //Console.WriteLine("perm {0}, mid {1}, {2}", mean_old_alloc_size, iPinnedMidSize, (fIsPinned ? "pinned" : "not pinned"));
                    old[iItemIndex] = new MyRequest(mean_old_alloc_size, iPinnedMidSize, fIsPinned);
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
                int iGen = rand.getRand (3);
                countWithGen[iGen]++;

                if (iGen == 1)
                {
                    //Console.WriteLine("item {0} is in gen1, getting rid of it", iItemIndex);
                    if ((countObjectsGen1 % 5) == 0)
                        old[iItemIndex].mObj = null;
                    countObjectsGen1++;
                }
            }

//            Console.WriteLine("{0} in gen0, {1} in gen1, {2} in gen2",
//                countWithGen[0],
//                countWithGen[1],
//                countWithGen[2]);
//
//            Console.WriteLine("{0} objects out of {1} are in gen1", countObjectsGen1, (iCheckedEnd - lastChecked));

            if (iCheckedEnd == old.Length)
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
                Console.WriteLine("{0}: iter {1}, heap size: {2}", index, j, GC.GetTotalMemory(false));

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
            index, (int)stopwatch.Elapsed.TotalSeconds, (int)stopwatch.Elapsed.TotalMilliseconds,
            GC.GetTotalMemory(false));
    }
}

class FreeListTest
{
    static int iLastGen0Count;
    static int iLastGen1Count;

    static void InducedGen2()
    {
        int iCurrentGen0Count = GC.CollectionCount(0);
        if ((iCurrentGen0Count - iLastGen0Count) > 50)
        {
            //Console.WriteLine("we've done {0} gen0 GCs, inducing a gen2", (iCurrentGen0Count - iLastGen0Count));
            iLastGen0Count = iCurrentGen0Count;
            GC.Collect(2, GCCollectionMode.Forced, false);
            //GC.Collect(2);
        }

        int iCurrentGen1Count = GC.CollectionCount(1);
        if ((iCurrentGen1Count - iLastGen1Count) > 10)
        {
            //Console.WriteLine("we've done {0} gen1 GCs, inducing a gen2", (iCurrentGen1Count - iLastGen1Count));
            iLastGen1Count = iCurrentGen1Count;
            GC.Collect(2, GCCollectionMode.Forced, false);
        }
    }

    public static int Main(String[] args)
    {
        if (GCSettings.IsServerGC == true)
        {
            Console.WriteLine ("we are using server GC");
        }
        int iter_num = 500000;
        if (args.Length >= 1)
        {
            iter_num = int.Parse(args[0]);
            Console.WriteLine ("iterating {0} times", iter_num);
        }

        // ProjectN doesn't support thread! for now just do everything on the main thread.
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

        Console.WriteLine("start with {0} gen1 GCs", iLastGen1Count);

        iLastGen0Count = GC.CollectionCount(0);
        iLastGen1Count = GC.CollectionCount(1);

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
        Console.WriteLine ("Test completed; "+(tEnd-tStart)+"ms");
//        Console.WriteLine("Press any key to exit.");
//        Console.ReadLine();

        return 100;
    }

};

sealed class Rand 
{
  /* Generate Random numbers
   */
  private int x = 0;

  public int getRand() {
	x = (314159269*x+278281) & 0x7FFFFFFF;
	return x;
  }

  // obtain random number in the range 0 .. r-1
  public int getRand(int r) {
	// require r >= 0
	int x = (int)(((long)getRand() * r) >> 31);
	return x;
  }
  public double getFloat () {
	return (double)getRand () / (double)0x7FFFFFFF;
  }
  
};
