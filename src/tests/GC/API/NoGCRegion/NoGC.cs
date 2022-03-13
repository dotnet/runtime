using System;
using System.Runtime;

public class Test
{
    private static byte[][] arrByteArray;
    private static int arrByteArrayLen;
    private static Random Rand;

    static void DoAlloc()
    {
        int totalIter = 1000000;
        int iterInterval = totalIter / 1000;
        for (int i = 0; i < totalIter; i++)
        {
            byte[] b = new byte[Rand.Next(2000)];

            if ((i % iterInterval) == 0)
            {
                arrByteArray[Rand.Next(arrByteArrayLen)] = b;
            }
        }
    }

    static void DoAllocWithNoGC(long size, bool fLOH)
    {
        long maxAllocSize = (fLOH ? (200 * 1024) : 1024);
        long minAllocSize = (fLOH ? (90 * 1024) : 1);
        long totalAllocSize = 0;
        bool fSurvived = false;

        while (true)
        {
            long s = Rand.Next((int)minAllocSize, (int)maxAllocSize);
            totalAllocSize += s + IntPtr.Size * 4;

            if (totalAllocSize >= size)
            {
                break;
            }

            byte[] b = new byte[s];

            if (!fSurvived && (totalAllocSize > (size / 2)))
            {
                fSurvived = true;
                arrByteArray[Rand.Next(arrByteArrayLen)] = b;
            }
        }
    }

    static bool TestAllocInNoGCRegion(int sizeMB, int sizeMBLOH, bool disallowFullBlockingGC)
    {
        bool isCurrentTestPassed = false;
        Console.WriteLine("=====allocating {0}mb/{1}mb {2} full blocking GC first=====", 
            sizeMB, sizeMBLOH, (disallowFullBlockingGC ? "disallowing" : "allowing"));
            
        DoAlloc();

        long size = (long)sizeMB * (long)1024 * (long)1024;
        long sizeLOH = ((sizeMBLOH == -1) ? 0 : ((long)sizeMBLOH * (long)1024 * (long)1024));
        try
        {
            Console.WriteLine("\nCalling TryStartNoGCRegion(..) with totalSize = {0:N0} MB",
                size / 1024.0 / 1024.0);

            int countGen2GCBefore = GC.CollectionCount(2);
            bool succeeded = false;
            if (sizeMBLOH == -1)
            {
                succeeded = GC.TryStartNoGCRegion(size, disallowFullBlockingGC);
            }
            else
            {
                succeeded = GC.TryStartNoGCRegion(size, sizeLOH, disallowFullBlockingGC);
            }

            int countGen2GCAfter = GC.CollectionCount(2);
            Console.WriteLine("{0:N0} MB {1}, did {2} gen2 GCs",
                sizeMB, (succeeded ? "SUCCEEDED" : "FAILED"), (countGen2GCAfter - countGen2GCBefore));
            isCurrentTestPassed = succeeded;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine("By Design: {0:N0} MB {1} {2}", sizeMB, ex.GetType().Name, ex.Message);
            isCurrentTestPassed = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("{0:N0} MB {1} {2}", sizeMB, ex.GetType().Name, ex.Message);
            isCurrentTestPassed = false;
        }

        if (GCSettings.LatencyMode == GCLatencyMode.NoGCRegion)
        {
            int countGCBeforeAlloc = GC.CollectionCount(0);

            DoAllocWithNoGC(size - sizeLOH, false);
            if (sizeLOH > 0)
            {
                DoAllocWithNoGC(sizeLOH, true);
            }
            
            int countGCAfterAlloc = GC.CollectionCount(0);

            Console.WriteLine("before GC: {0}, after GC: {1}", countGCBeforeAlloc, countGCAfterAlloc);

            if (countGCAfterAlloc > countGCBeforeAlloc)
            {
                Console.WriteLine("We did GCs in no gc mode!!!");
            }

            try
            {
                Console.WriteLine("ended no gc region");
                GC.EndNoGCRegion();
            }
            catch (Exception ex)
            {
                Console.WriteLine("End NoGC region: {0:N0} MB {1} {2}", sizeMB, ex.GetType().Name, ex.Message);
                isCurrentTestPassed = false;
            }
        }

        DoAlloc();
        Console.WriteLine("current GC count: {0}", GC.CollectionCount(0));
        Console.WriteLine();
        Console.WriteLine("=====allocating {0}mb {1} full blocking GC first {2}=====", 
            sizeMB, (disallowFullBlockingGC ? "disallowing" : "allowing"), isCurrentTestPassed? "Succeeded":"Failed" );

        return isCurrentTestPassed;
    }

    public static int Main(string[] args)
    {
        arrByteArrayLen = 5000;
        arrByteArray = new byte[arrByteArrayLen][];
        Rand = new Random();

        bool isServerGC = GCSettings.IsServerGC;
        int pointerSize = IntPtr.Size;
        int numberProcs =  Environment.ProcessorCount;

        Console.WriteLine("{0} on {1}-bit with {2} procs",
            (isServerGC ? "Server" : "Workstation"),
            ((pointerSize == 8) ? 64 : 32),
            numberProcs);

        int lowMB = 0;
        int midMB = 0;
        int highMB = 0;

        if (isServerGC)
        {
            if (pointerSize == 8)
            {
                lowMB = 200;
                midMB = lowMB * 2;
                highMB = 1024*numberProcs;
            }
            else
            {
                lowMB = 100;
                midMB = 244;
                highMB = 64*numberProcs;
            }
        }
        else
        {
            if (pointerSize == 8)
            {
                lowMB = 100;
                midMB = lowMB * 2;
                highMB = midMB * 2;
            }
            else
            {
                lowMB = 10;
                midMB = 15;
                highMB = 100;
            }
        }

        bool isTestSucceeded = true;

        if(!TestAllocInNoGCRegion(lowMB, -1, false) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(midMB, -1, false) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(midMB, -1, true) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(highMB, -1, false) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(highMB, -1, true) && isTestSucceeded) isTestSucceeded = false;
        // also specifying loh size
        if(!TestAllocInNoGCRegion(lowMB, (lowMB / 2), false) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(midMB, (midMB / 2), false) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(midMB, (midMB / 2), true) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(highMB, (highMB / 2), false) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(highMB, (highMB / 2), true) && isTestSucceeded) isTestSucceeded = false;
        if(!TestAllocInNoGCRegion(highMB, (highMB - 10), false) && isTestSucceeded) isTestSucceeded = false;
        //Return value
        //Test passed:100
        //Test failed: 1
        return isTestSucceeded ? 100 : 1;
    }
}

