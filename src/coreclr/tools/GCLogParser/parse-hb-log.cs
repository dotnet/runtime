// parse-hb-log.exe -ia 1 -l gclog.pid.log
// where gclog.pid.log is the log you get from GC with HEAP_BALANCE_LOG. See
// comments for it in gcpriv.h
//
// -ia 1 means to include all timestamp instead of only timestamps where we
// observed samples. This is usually what you want as it makes it really
// obvious when a thread is not active enough.
//
// This prints out the following in the console:
// Samples observed on each thread and thread mapping (from id to index)
// and it tells you the BFR (Budget Fill Ratio) like this:
//
//Printing out proc [0,[28
//Total 1074 GCs, avg 0ms, node 0: BFR %90.83
//Printing out proc [28,[56
//Total 1074 GCs, avg 0ms, node 1: BFR %89.43
//
// and generates 2 logs per numa node -
// pass-zero-pass1-nX-ia1-alloc.txt
// pass-zero-pass1-nX-ia1-thread.txt
//
// thread tells you the thread index running on each proc at each timestamp.
// 4240| 63₉ | 65₉ | 62₉ | 56₁₀| 87₁₀|109₁₀| 59₉ | 70₁₀| 78₉ | 64₉ | 71₁₀|107₁₀|
//
// 4240 is the 4240th ms since we started recording.
// the numbers following are the thread indices and the subscript is the # of samples
// observed during that ms. The tool can do a time unit that's larger than 1ms.
//
// alloc tells you which alloc heap the each proc, for the same timestamp
// 4240| 56  | 57  | 58ⁱ | 59  | 60  | 61  | 62  | 63  | 64  | 65  | 66  | 67  |
// 56 means heap 56. The subscript i means we did a SetIdealProcessor during this
// ms. You may also see
// ᵖ meaning we went through the balancing logic due to the proc for the thread changed
// from the home heap.
// ᵐ meaning while we were going through balancing logic the proc switched.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace parse_hb_log
{
    enum HeapBalanceFlagMask
    {
        MutipleProcs = 0xf,
        EnterDueToProc = 0xf0,
        SetIdeal = 0xf00,
    };
    struct SampleInfo
    {
        // -1 means uninit.
        public int tid;
        public int allocHeap;
        public int countSamples;
        // This is to present m/p/i - each takes 4 bits, mask for them
        // is defined in HeapBalanceFlagMask. Most of the time the flags are
        // 0s.
        public int flags;
        public int idealProcNo;

        public SampleInfo(int _tid, int _allocHeap, int _countSamples, int _flags, int _idealProcNo)
        {
            tid = _tid;
            allocHeap = _allocHeap;
            countSamples = _countSamples;
            flags = _flags;
            idealProcNo = _idealProcNo;
        }
    };

    enum PassOneViewType
    {
        Thread = 0,
        AllocHeap = 1,
        MaxType = 2
    };
    class Program
    {
        static bool fLogging = false;

        static string ParseString(string str, string strStart, string strEnd, out string strRemaining)
        {
            int startIndex = 0;
            if (strStart != null)
            {
                startIndex = str.IndexOf(strStart);
                if (startIndex == -1)
                {
                    Console.WriteLine("couldn't find {0} in \"{1}\"!!!", strStart, str);
                    strRemaining = str;
                    return null;
                }

                startIndex += strStart.Length;
            }

            //string strRest = str.Substring(startIndex + strStart.Length);
            string strRest = str.Substring(startIndex);
            string strRet = null;
            //Console.WriteLine(strRest);
            if (strEnd == null)
            {
                strRet = strRest;
                strRemaining = null;
            }
            else
            {
                int endIndex = strRest.IndexOf(strEnd);
                strRet = strRest.Substring(0, endIndex);
                strRemaining = strRest.Substring(endIndex);
            }

            //Console.WriteLine("string is --{0}--, remaining --{1}--", strRet, strRemaining);
            return strRet;
        }

        static StreamWriter swPassZero = null;
        static string strPassZeroLog = "pass-zero.txt";
        static int totalProcs = 0;
        static int totalNodes = 0;
        static int procsPerNode = 0;
        // this is qpf / 1000 so we calculate ms instead of s.
        static UInt64 qpfAdjusted = 0;
        // we log the current qpc so subtract by this.
        static UInt64 qpcStart = 0;
        static Int32 totalAllocThreads = 0;
        static Dictionary<string, int> threadMapping = new Dictionary<string, int>(112);
        // We do compress the samples, this stores the aggregated sample counts for all procs.
        static int[] aggregatedSampleCount = null;

        static int timeUnitMS = 1;
        // We wanna compress the log by compressing the samples in the same unit of time.
        // We get samples by procs, so on the same proc if we observe the same thread
        // allocating on the same alloc heap in the same time unit we simply count the total
        // samples. However we do want to remember if any of the p/m/i is set.
        static string strLastTID = null;
        static string strTID = null;
        static string strLastThreadAllocHeap = null;
        static string strAllocHeap = null;
        static string strIdealProc = null;
        static int lastTimeUnit = 0;
        static int lastThreadSampleCount = 0;
        static int lastThreadPCount = 0;
        static int lastThreadMCount = 0;
        static int lastThreadICount = 0;
        static int lastProcIndex = -1;
        static int largestTimeInBetweenGCs = 0;

        static void InitSampleInfoPassZero()
        {
            strLastTID = null;
            strTID = null;
            strLastThreadAllocHeap = null;
            strAllocHeap = null;
            lastTimeUnit = 0;
            lastThreadSampleCount = 0;
            lastThreadPCount = 0;
            lastThreadMCount = 0;
            lastThreadICount = 0;
        }

        static void LogPassZeroAggregatedSample(int _lastTimeUnit, int _tid, int _sampleCount, string _strAllocHeap)
        {
            swPassZero.WriteLine("{0}ms,{1}({2}),{3},m:{4},p:{5},i:{6}({7})",
                _lastTimeUnit,
                _tid,
                _sampleCount,
                _strAllocHeap,
                lastThreadMCount, lastThreadPCount, lastThreadICount,
                strIdealProc);

            (aggregatedSampleCount[lastProcIndex])++;
        }

        // Aside from writing out a new log this also prints out some stats for further processing:
        // How many samples are observed on each proc. Because we could use this for many proc analysis,
        // it's impractical to have a wide enough window to display all procs.
        static void PassZero(string strLog)
        {
            swPassZero = new StreamWriter(strPassZeroLog);
            string strTemp, strRemaining;
            using (StreamReader sr = new StreamReader(strLog))
            {
                string s;

                while ((s = sr.ReadLine()) != null)
                {
                    if (s.StartsWith("["))
                    {
                        string strAfterTID = ParseString(s, "]", null, out strRemaining);
                        //Console.WriteLine(s);

                        if (strAfterTID.StartsWith("qpf"))
                        {
                            // [15900]qpf=10000000, start: 17262240813778(1726224081)
                            strTemp = ParseString(s, "qpf=", ",", out strRemaining);
                            qpfAdjusted = UInt64.Parse(strTemp) / 1000;
                            strTemp = ParseString(strRemaining, "start:", "(", out strRemaining);
                            qpcStart = UInt64.Parse(strTemp);
                            Console.WriteLine("QPF adjusted: {0}, init QPC: {1}", qpfAdjusted, qpcStart);
                        }
                        else if (strAfterTID.StartsWith("total: "))
                        {
                            // [15900]total: 112, numa: 2
                            strTemp = ParseString(strAfterTID, "total: ", ",", out strRemaining);
                            totalProcs = Int32.Parse(strTemp);
                            strTemp = ParseString(strRemaining, "numa: ", null, out strRemaining);
                            totalNodes = Int32.Parse(strTemp);
                            Console.WriteLine("total procs: {0}, nodes: {1}", totalProcs, totalNodes);
                            procsPerNode = totalProcs / totalNodes;
                            swPassZero.WriteLine("P: {0}, N: {1}", totalProcs, totalNodes);
                            aggregatedSampleCount = new int[totalProcs];
                        }
                        else if (strAfterTID == "[GC_alloc_mb]")
                        {
                            if (strTID != null)
                            {
                                LogPassZeroAggregatedSample(
                                //swPassZero.WriteLine("last sample before GC: {0}ms,{1}({2}),{3},m:{4},p:{5},i:{6}",
                                    lastTimeUnit,
                                    threadMapping[strTID],
                                    lastThreadSampleCount,
                                    strAllocHeap);
                                InitSampleInfoPassZero();
                            }

                            swPassZero.WriteLine("[GC_alloc_mb]");
                            //[36812][GC_alloc_mb]
                            //[N# 24]0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                            //[N# 24]0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,24,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                            int totalNodesRead = 0;
                            while ((s = sr.ReadLine()) != null)
                            {
                                if (s.StartsWith("[N"))
                                {
                                    //Console.WriteLine(s);
                                    swPassZero.WriteLine(s);
                                    totalNodesRead++;
                                    if (totalNodesRead == totalNodes)
                                        break;
                                }
                            }
                        }
                        else if (strAfterTID.StartsWith("[p"))
                        {
                            if (strTID != null)
                            {
                                //swPassZero.WriteLine("last sample before p: {0}ms,{1}({2}),{3},m:{4},p:{5},i:{6}",
                                LogPassZeroAggregatedSample(
                                    lastTimeUnit,
                                    threadMapping[strTID],
                                    lastThreadSampleCount,
                                    strAllocHeap);
                                InitSampleInfoPassZero();
                            }

                            // [36812][p22]-192-254ms
                            //Console.WriteLine(strAfterTID);
                            swPassZero.WriteLine(strAfterTID);
                            strTemp = ParseString(strAfterTID, "p", "]", out strRemaining);
                            lastProcIndex = Int32.Parse(strTemp);
                        }
                        else if (strAfterTID.StartsWith("[GCA#"))
                        {
                            // 270 is the time elapsed in ms since the last GC end.
                            // 18280955362126/18280957621306 are the min/max timestamps observed
                            // on all procs for this period.
                            // [39652][GCA#1 270-18280955362126-18280957621306]
                            //Console.WriteLine(strAfterTID);
                            // convert the raw ts to relative ms.
                            strTemp = ParseString(strAfterTID, "-", "-", out strRemaining);
                            //Console.WriteLine("min ts is {0}", strTemp);
                            UInt64 minTimestampMS = UInt64.Parse(strTemp);
                            minTimestampMS /= qpfAdjusted;
                            strTemp = ParseString(strRemaining, "-", "]", out strRemaining);
                            //Console.WriteLine("max ts is {0}", strTemp);
                            UInt64 maxTimestampMS = UInt64.Parse(strTemp);
                            maxTimestampMS /= qpfAdjusted;
                            strTemp = ParseString(strAfterTID, null, "-", out strRemaining);
                            swPassZero.WriteLine("{0}-{1}-{2}", strTemp, minTimestampMS, maxTimestampMS);

                            largestTimeInBetweenGCs = Math.Max(largestTimeInBetweenGCs, (int)(maxTimestampMS - minTimestampMS));
                        }
                        else if (strAfterTID.StartsWith("[GC#"))
                        {
                            // 12 is GC duration in ms
                            // [15628][GC#1-12]
                            //Console.WriteLine(strAfterTID);
                            swPassZero.WriteLine(strAfterTID);
                        }
                        else if (strAfterTID.StartsWith("TEMP"))
                        {
                            // If I want to only log something temporarily I prefix it with TEMP so we know to just
                            // write it out as is.
                            swPassZero.WriteLine(strAfterTID);
                        }
                        else
                        {
                            // This is the majority of the log -
                            // qpc,tid,ideal_proc,alloc_heap
                            // |m means we observed the thread changed the proc it was running on
                            // during the iterations we do in balance_heaps since we check at various
                            // points for the current proc.
                            // |p means we detected proc != home_heap proc
                            // |i means we did SetIdeal
                            //[38328]12791000,80,24540|m,101|p
                            //[38328]12792000,80,24540,101
                            //[38328]12801000,90,33356,111|p
                            //[38328]12802000,90,33356,111
                            //
                            // this will be converted to
                            //1279ms,2(2),80,101,m:1,p:1,i:0
                            //1280ms,3(2),90,111,m:0,p:1,i:0
                            //
                            // the format is:
                            // relative time from start in ms, thread index(sample count during the unit of time),
                            // count for m/p/i observed during this unit of time.
                            //
                            strTemp = ParseString(s, "]", ",", out strRemaining);
                            //UInt64 currentQPC = UInt64.Parse(strTemp);
                            UInt64 currentQPC = 0;
                            if (!UInt64.TryParse(strTemp, out currentQPC))
                            {
                                continue;
                            }
                            if (fLogging)
                            {
                                //Console.WriteLine("ts: {0}-{1}={2}", currentQPC, qpcStart, (currentQPC - qpcStart));
                                Console.WriteLine();
                                Console.WriteLine(strAfterTID);
                            }
                            //double timestamp = (double)(currentQPC - qpcStart) / qpfAdjusted;
                            double timestamp = (double)currentQPC / qpfAdjusted;

                            strTID = ParseString(strRemaining, ",", ",", out strRemaining);
                            if (strTID == "15580|m")
                            {
                                Console.WriteLine(s);
                            }
                            if (!threadMapping.ContainsKey(strTID))
                            {
                                threadMapping.Add(strTID, totalAllocThreads);
                                if (fLogging)
                                {
                                    Console.WriteLine("Adding {0} as T#{1}", strTID, totalAllocThreads);
                                }
                                totalAllocThreads++;
                            }

                            strIdealProc = ParseString(strRemaining, ",", ",", out strRemaining);

                            bool multiple_procs_p = false;
                            bool alloc_count_p = true;
                            bool set_ideal_p = false;

                            if (strRemaining.Contains("|"))
                            {
                                strAllocHeap = ParseString(strRemaining, ",", "|", out strRemaining);
                                if (strRemaining.Contains("m"))
                                    multiple_procs_p = true;
                                if (strRemaining.Contains("p"))
                                    alloc_count_p = false;
                                if (strRemaining.Contains("i"))
                                    set_ideal_p = true;
                            }
                            else
                            {
                                strAllocHeap = ParseString(strRemaining, ",", null, out strRemaining);
                            }

                            if (fLogging)
                            {
                                Console.WriteLine("{0:0.00}ms, tid: {1}/{2}, alloc heap: {3}, m: {4}, p: {5}, i: {6}, ideal {7}",
                                    timestamp, strTID, threadMapping[strTID],
                                    strAllocHeap, multiple_procs_p, !alloc_count_p, set_ideal_p,
                                    strIdealProc);
                            }
                            //swPassZero.WriteLine("{0:0.00}ms,{1},{2},m:{3},p:{4},i:{5}",
                            //    timestamp,
                            //    threadMapping[strTID],
                            //    strAllocHeap,
                            //    (multiple_procs_p ? "T" : ""),
                            //    (!alloc_count_p ? "T" : ""),
                            //    (set_ideal_p? "T" : ""));

                            if ((strTID == strLastTID) && (strLastThreadAllocHeap == strAllocHeap))
                            {
                                if (((int)timestamp - lastTimeUnit) >= timeUnitMS)
                                {
                                    if (fLogging)
                                    {
                                        Console.WriteLine("time is now {7}>{0}+{8},{1}({2}),{3},m:{4},p:{5},i:{6}",
                                            lastTimeUnit,
                                            threadMapping[strTID],
                                            lastThreadSampleCount,
                                            strAllocHeap,
                                            lastThreadMCount, lastThreadPCount, lastThreadICount,
                                            (int)timestamp, timeUnitMS);
                                    }
                                    LogPassZeroAggregatedSample(
                                        lastTimeUnit,
                                        threadMapping[strTID],
                                        lastThreadSampleCount,
                                        strAllocHeap);
                                    if (fLogging)
                                    {
                                        Console.WriteLine("Set last time to {0} for thread {1}, alloc {2}",
                                            (int)timestamp, strLastTID, strLastThreadAllocHeap);
                                    }
                                    lastTimeUnit = (int)timestamp;
                                    lastThreadSampleCount = 0;
                                    lastThreadMCount = lastThreadPCount = lastThreadICount = 0;
                                }
                            }
                            else
                            {
                                if (strLastTID != null)
                                {
                                    // If we detect the thread or alloc heap changed, we always want to
                                    // log.
                                    if (fLogging)
                                    {
                                        Console.WriteLine("{0},{1}({2}),{3},m:{4},p:{5},i:{6} -> {7}/{8}",
                                            lastTimeUnit,
                                            threadMapping[strLastTID],
                                            lastThreadSampleCount,
                                            strLastThreadAllocHeap,
                                            lastThreadMCount, lastThreadPCount, lastThreadICount,
                                            strTID, strAllocHeap);
                                    }
                                    LogPassZeroAggregatedSample(
                                        lastTimeUnit,
                                        threadMapping[strLastTID],
                                        lastThreadSampleCount,
                                        strLastThreadAllocHeap);
                                }

                                if (fLogging)
                                {
                                    Console.WriteLine("last tid {0}, last alloc heap {1}, diff, set last time to {2}",
                                        strLastTID, strLastThreadAllocHeap, (int)timestamp);
                                }
                                strLastTID = strTID;
                                strLastThreadAllocHeap = strAllocHeap;
                                lastTimeUnit = (int)timestamp;
                                lastThreadSampleCount = 0;
                                lastThreadPCount = 0;
                                lastThreadMCount = 0;
                                lastThreadICount = 0;
                            }

                            lastThreadSampleCount++;
                            if (multiple_procs_p) lastThreadMCount++;
                            if (!alloc_count_p) lastThreadPCount++;
                            if (set_ideal_p) lastThreadICount++;

                            //if ((lastThreadMCount > 1) || (lastThreadPCount > 1) || (lastThreadICount > 1))
                            //{
                            //    swPassZero.WriteLine("DETECTED: m: {0}, p: {1}, i: {2}",
                            //        lastThreadMCount, lastThreadPCount, lastThreadICount);
                            //}

                            if (fLogging)
                            {
                                Console.WriteLine("timeunit {0}, sample->{1}, MCount->{2}, PCount->{3}, ICount->{4}",
                                    lastTimeUnit, lastThreadSampleCount,
                                    lastThreadMCount, lastThreadPCount, lastThreadICount);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("{0,-3} | {1,-10}", "p", "samples");
            for (int procIndex = 0; procIndex < totalProcs; procIndex++)
            {
                if (aggregatedSampleCount[procIndex] != 0)
                {
                    Console.WriteLine("{0,-3} | {1,-10} ", procIndex, aggregatedSampleCount[procIndex]);
                }
            }

            Console.WriteLine("---Threading mapping---");
            foreach (var t in threadMapping)
            {
                Console.WriteLine("t#{0,-3} - {1}", t.Key, t.Value);
            }

            Console.WriteLine("Largest time span inbetween GCs: {0}ms", largestTimeInBetweenGCs);
            swPassZero.Close();
        }

        //=========================================================================
        // Pass one. TODO: should separate this from pass zero.
        //=========================================================================
        static StreamWriter[] swPassOneFiles = null;
        // It's difficult to print all of the procs on one line so we only print per node.
        static int nodeIndexToPrint = -1;
        static bool fIncludeAllTime = false;
        static bool fPrintThreadInfoPerTimeUnit = false;

        // This represents the samples for all the procs inbetween GCs so it doesn't grow very large.
        // Cleared every GC.
        static SampleInfo[][] samples;
        // This is the min/max time for inbetween each GC.
        static int startTimeMS = 0;
        static int endTimeMS = 0;
        static int currentProcIndex = -1;
        static int lastTimeIndex = -1;

        // This respresents the threads we see in samples so we can get info such as when threads
        // start running and how active they are.
        // Cleared every GC.
        static List<int> threadsSeenPerTimeUnit = new List<int>(56);
        static Dictionary<int, int> threadsSeen = new Dictionary<int, int>(56);
        static Dictionary<int, int> threadsSeenTotal = new Dictionary<int, int>(56);
        static List<int> threadsToPrint = new List<int>(8);
        static string strThreadIndices = null;
        static int gcIndexToPrintStart = -1;
        static int gcIndexToPrintEnd = 1000000;
        static string strGCRange = null;
        static int totalGCCount = 0;
        static int totalGCDurationMS = 0;
        static int totalAllocMB = 0;
        static int totalBudgetMB = 0;

        // This represents allocated MB on each heap; parse from the lines following [GC_alloc_mb]
        static int[] AllocMB;
        static int budgetMB = 0;
        // These are subscript chars for 0-9.
        static char[] unicodeChars = { '\x2080', '\x2081', '\x2082', '\x2083', '\x2084', '\x2085', '\x2086', '\x2087', '\x2088', '\x2089' };
        static string[] passOneFileTypes = { "thread", "alloc" };

        static void PrintToAllPassOneFiles(string strLine)
        {
            for (int fileIndex = 0; fileIndex < (int)PassOneViewType.MaxType; fileIndex++)
            {
                swPassOneFiles[fileIndex].WriteLine(strLine);
            }
        }

        static void PrintAllocToAllPassOneFiles()
        {
            string strAlloc = string.Format("{0,6}", nodeIndexToPrint);
            int procStart = procsPerNode * nodeIndexToPrint;
            int procEnd = procsPerNode * (nodeIndexToPrint + 1);

            int currentGCAllocMBAllHeaps = 0;
            int currentGCAllocMB = 0;
            for (int procIndex = procStart; procIndex < procEnd; procIndex++)
            {
                currentGCAllocMB += AllocMB[procIndex];
                //Console.WriteLine("P#{0,2} alloc {1,3}mb, total is now {2,3}mb",
                //    procIndex, AllocMB[procIndex],
                //    currentGCAllocMB);
            }

            currentGCAllocMBAllHeaps = currentGCAllocMB;
            currentGCAllocMB /= procsPerNode;
            totalAllocMB += currentGCAllocMB;
            totalBudgetMB += budgetMB;

            //Console.WriteLine("N#{0} GC#{1,2} alloc {2,3}mb, budget {3,3}mb, total alloc {4,3}mb, total budget {5,3}mb",
            //    nodeIndexToPrint, totalGCCount, currentGCAllocMB, budgetMB, totalAllocMB, totalBudgetMB);

            //Console.WriteLine("{0,3}mb - {1,3}mb (%{2:f2} - {3}ms ({4:f2}mb/ms)",
            //    budgetMB, currentGCAllocMB, ((currentGCAllocMB * 100.0) / budgetMB),
            //    (endTimeMS - startTimeMS),
            //    ((double)currentGCAllocMBAllHeaps / (endTimeMS - startTimeMS)));

            int procIndexBase = nodeIndexToPrint * procsPerNode;
            for (int procIndex = procIndexBase; procIndex < (procIndexBase + procsPerNode); procIndex++)
            {
                strAlloc += string.Format("|{0,5}", AllocMB[procIndex]);
            }
            strAlloc += string.Format("|");
            PrintToAllPassOneFiles(strAlloc);
        }
        static void CloseAllPassOneFiles()
        {
            Console.WriteLine("Total {0} GCs, avg {1}ms, node {2}: BFR %{3:f2}",
                totalGCCount,
                (totalGCDurationMS / totalGCCount),
                nodeIndexToPrint,
                (totalAllocMB * 100.0 / totalBudgetMB));

            for (int fileIndex = 0; fileIndex < (int)PassOneViewType.MaxType; fileIndex++)
            {
                swPassOneFiles[fileIndex].Close();
            }
        }

        static void PrintHeader()
        {
            // TEMP..
            //procsPerNode = 16;
            int procStart = procsPerNode * nodeIndexToPrint;
            int procEnd = procsPerNode * (nodeIndexToPrint + 1);

            Console.WriteLine("Printing out proc [{0},[{1}", procStart, procEnd);

            swPassOneFiles[(int)PassOneViewType.Thread].WriteLine("##########Thread view - indices of threads currently running on each proc in between GCs\n");
            swPassOneFiles[(int)PassOneViewType.AllocHeap].WriteLine("##########Alloc heap view - alloc heaps for threads currently running on each proc in between GCs\n");
            string strHeader = string.Format("{0,6}", "ms");
            for (int procIndex = procStart; procIndex < procEnd; procIndex++)
            {
                strHeader += string.Format("|{0,5}", procIndex);
            }
            strHeader += string.Format("|");
            PrintToAllPassOneFiles(strHeader);
        }

        // Note that I'm only allocating 2 chars for count and we don't expect to have more than 99 of them.
        static string FormatCount(int count)
        {
            if (count > 99)
            {
                Console.WriteLine("actually have {0} samples in {1}ms!!!!", count, timeUnitMS);
                count = 99;
            }

            string strFormattedCount = null;

            if (count >= 10)
            {
                strFormattedCount += unicodeChars[count / 10];
            }

            strFormattedCount += unicodeChars[count % 10];
            return strFormattedCount;
        }

        static string FormatFlags(int flags)
        {
            string strFormattedFlags = "";
            if ((flags & (int)HeapBalanceFlagMask.MutipleProcs) != 0)
            {
                strFormattedFlags += "ᵐ";
            }
            if ((flags & (int)HeapBalanceFlagMask.EnterDueToProc) != 0)
            {
                strFormattedFlags += "ᵖ";
            }
            if ((flags & (int)HeapBalanceFlagMask.SetIdeal) != 0)
            {
                strFormattedFlags += "ⁱ";
            }

            return strFormattedFlags;
        }

        // We display the following -
        // Each row represents a time unit, each column represents a proc.
        // Each element represents a piece of info on that proc for that time.
        // This info could be the thread index, or the alloc heap, along with
        // things like m/p/i or count of samples for that time period for that
        // thread and alloc heap.
        //
        // There could be multiple entries for the same time on a proc because
        // we happen to observe multiple threads running during that time.
        // But we only store one piece of info. Here are the rules to decide
        // what we store -
        // We take the last one on the same time unless we see one with interesting
        // info (ie, one of m/p/i isn't 0); or one with higher sample count.
        //
        static void PrintProcActivityOnNode()
        {
            // TEMP..
            //procsPerNode = 16;
            int totalTimeUnits = (endTimeMS - startTimeMS) / timeUnitMS + 1;
            int procStart = procsPerNode * nodeIndexToPrint;
            int procEnd = procsPerNode * (nodeIndexToPrint + 1);

            bool fCheckThreadIndex = (threadsToPrint.Count != 0);

            threadsSeen.Clear();

            for (int timeIndex = 0; timeIndex < totalTimeUnits; timeIndex++)
            {
                bool fPrint = fIncludeAllTime;

                if (!fPrint)
                {
                    // Only write something if there's samples observed on at least one proc.
                    for (int procIndex = procStart; procIndex < procEnd; procIndex++)
                    {
                        if (samples[timeIndex][procIndex].tid != -1)
                        {
                            fPrint = true;
                            break;
                        }
                    }
                }

                // see https://en.wikipedia.org/wiki/Unicode_subscripts_and_superscripts
                // for subscript characters
                // ᵐ,ᵖ,ⁱ
                if (fPrint)
                {
                    int procsHadSamples = 0;
                    if (fPrintThreadInfoPerTimeUnit)
                        threadsSeenPerTimeUnit.Clear();

                    string strCellFormat = "|{0,3}{1,-2}";
                    string strThread = string.Format("{0,6}", (timeIndex + startTimeMS));
                    string strAllocHeap = string.Format("{0,6}", (timeIndex + startTimeMS));

                    for (int procIndex = procStart; procIndex < procEnd; procIndex++)
                    {
                        SampleInfo currentSample = samples[timeIndex][procIndex];
                        int tid = currentSample.tid;
                        bool fIncludeThisSample = true;

                        if (fCheckThreadIndex)
                        {
                            if (!threadsToPrint.Contains(tid))
                            {
                                fIncludeThisSample = false;
                            }
                        }

                        if ((tid == -1) || !fIncludeThisSample)
                        {
                            strThread += string.Format(strCellFormat, "", "");
                            strAllocHeap += string.Format(strCellFormat, "", "");
                        }
                        else
                        {
                            if (fPrintThreadInfoPerTimeUnit)
                            {
                                if (!threadsSeenPerTimeUnit.Contains(tid))
                                    threadsSeenPerTimeUnit.Add(tid);
                                procsHadSamples++;
                            }

                            if (!threadsSeen.ContainsKey(tid))
                                threadsSeen.Add(tid, currentSample.countSamples);
                            else
                                threadsSeen[tid] += currentSample.countSamples;

                            string strCount = ((currentSample.countSamples > 0) ? FormatCount(currentSample.countSamples) : "");
                            strThread += string.Format(strCellFormat, tid.ToString(), strCount);

                            string strFlags = FormatFlags(currentSample.flags);
                            strAllocHeap += string.Format(strCellFormat, currentSample.allocHeap.ToString(), strFlags);
                        }
                    }
                    //swPassOne.WriteLine("|");
                    strThread += "|";
                    swPassOneFiles[(int)PassOneViewType.Thread].WriteLine(strThread);

                    if (fPrintThreadInfoPerTimeUnit)
                    {
                        swPassOneFiles[(int)PassOneViewType.Thread].WriteLine("----{0,3} threads on {1,3} procs----",
                            threadsSeenPerTimeUnit.Count, procsHadSamples);
                    }

                    strAllocHeap += "|";
                    swPassOneFiles[(int)PassOneViewType.AllocHeap].WriteLine(strAllocHeap);
                }
            }

            //var threadsSeenOrdered = threadsSeen.OrderByDescending(i => i.Value);
            //foreach (var item in threadsSeenOrdered)
            //{
            //    int k = item.Key;
            //    int v = item.Value;
            //    swPassOneFiles[(int)PassOneViewType.Thread].WriteLine("tid: {0,5}-{1,-5}", k, v);
            //    if (!threadsSeenTotal.ContainsKey(k))
            //        threadsSeenTotal.Add(k, v);
            //    else
            //        threadsSeenTotal[k] += v;
            //}
        }

        static int GetAdjustedProcIndex(int pIndex)
        {
            return (pIndex % procsPerNode);
        }

        static void ParseThreadIndices(string _strThreadIndices)
        {
            strThreadIndices = _strThreadIndices;
            string[] fields = strThreadIndices.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < fields.Length; i++)
            {
                threadsToPrint.Add(Int32.Parse(fields[i]));
            }

            int len = threadsToPrint.Count;
            Console.WriteLine("getting info for {0} threads", len);
            for (int i = 0; i < len; i++)
            {
                Console.WriteLine(threadsToPrint[i]);
            }
        }

        static void ParseGCRange(string _strGCRange)
        {
            strGCRange = _strGCRange;
            int dashIndex= strGCRange.IndexOf('-');
            if (dashIndex== -1)
            {
                Console.WriteLine("Invalid GC range {0}", strGCRange);
                return;
            }

            string strStart = strGCRange.Substring(0, dashIndex);
            string strEnd = strGCRange.Substring(dashIndex + 1);

            if (strStart != "start")
                gcIndexToPrintStart = Int32.Parse(strStart);
            if (strEnd != "end")
                gcIndexToPrintEnd = Int32.Parse(strEnd);

            Console.WriteLine("printing GC {0}->{1}", gcIndexToPrintStart, gcIndexToPrintEnd);
        }

        static void PassOne(string strPassZeroLog)
        {
            // This generates 2 files - thread view and alloc heap view. This is just so you could
            // compare them SxS.
            string strLogNameWithoutExtension = Path.GetFileNameWithoutExtension(strPassZeroLog);
            string strIncludeAll = (fIncludeAllTime ? "-ia1-" : "-ia0-");
            string strTI = ((strThreadIndices == null) ? "" : strThreadIndices);
            string strPassOneLog = strLogNameWithoutExtension + "-pass1-n" + nodeIndexToPrint + strIncludeAll + strTI;

            if (strGCRange != null)
                strPassOneLog += "-" + strGCRange + "-";

            swPassOneFiles = new StreamWriter[(int)PassOneViewType.MaxType];
            for (int fileIndex = 0; fileIndex < (int)PassOneViewType.MaxType; fileIndex++)
            {
                swPassOneFiles[fileIndex] = new StreamWriter(strPassOneLog + passOneFileTypes[fileIndex] + ".txt");
            }

            string strTemp, strRemaining;
            using (StreamReader sr = new StreamReader(strPassZeroLog))
            {
                string s;
                bool fSkip = false;

                while ((s = sr.ReadLine()) != null)
                {
                    if (s.StartsWith("P:"))
                    {
                        //P: 112, N: 2
                        strTemp = ParseString(s, "P: ", ",", out strRemaining);
                        totalProcs = Int32.Parse(strTemp);
                        strTemp = ParseString(strRemaining, "N: ", null, out strRemaining);
                        totalNodes = Int32.Parse(strTemp);
                        procsPerNode = totalProcs / totalNodes;
                        PrintHeader();

                        int timeUnits = largestTimeInBetweenGCs / timeUnitMS + 1;
                        //Console.WriteLine("allocating {0} columns, {1} rows", totalProcs, timeUnits);
                        samples = new SampleInfo[timeUnits][];
                        for (int timeIndex = 0; timeIndex < timeUnits; timeIndex++)
                        {
                            samples[timeIndex] = new SampleInfo[totalProcs];
                            for (int procIndex = 0; procIndex < totalProcs; procIndex++)
                            {
                                samples[timeIndex][procIndex].tid = -1;
                            }
                        }

                        // Note - I'm allocating totalProcs elements but there are not necessarily that many
                        // heaps. But there can't be more heaps than procs so we just waste some space here which
                        // is ok.
                        AllocMB = new int[totalProcs];

                        totalGCCount = 0;
                        totalGCDurationMS = 0;
                        totalAllocMB = 0;
                        totalBudgetMB = 0;
                    }
                    else if (s.StartsWith("TEMP"))
                    {
                        // If I want to only log something temporarily I prefix it with TEMP so we know to just
                        // write it out as is.
                        //PrintToAllPassOneFiles(s);
                    }
                    else if (s.StartsWith("[GCA#"))
                    {
                        // Clear what we recorded for the last GC.
                        int usedEntryCount = (endTimeMS - startTimeMS) / timeUnitMS + 1;
                        //Console.WriteLine("Clearing {0} time entries", usedEntryCount);
                        for (int timeIndex = 0; timeIndex < usedEntryCount; timeIndex++)
                        {
                            for (int procIndex = 0; procIndex < totalProcs; procIndex++)
                            {
                                samples[timeIndex][procIndex].tid = -1;
                            }
                        }

                        // GCA#gc_index time_since_last_gc-min_time-max_time
                        //[GCA#1 270-42-268
                        strTemp = ParseString(s, "A#", " ", out strRemaining);
                        int gcIndex = Int32.Parse(strTemp);

                        fSkip = !((gcIndex >= gcIndexToPrintStart) && (gcIndex <= gcIndexToPrintEnd));

                        if (fSkip)
                        {
                            //Console.WriteLine("GC#{0} is skipped", gcIndex);
                            continue;
                        }
                        else
                        {
                            //Console.WriteLine("GC#{0} is NOT skipped", gcIndex);
                        }
                        //swPassOne.WriteLine("[GC#{0}-GC#{1}]", (gcIndex - 1), gcIndex);
                        strTemp = ParseString(s, "-", "-", out strRemaining);
                        startTimeMS = Int32.Parse(strTemp);
                        strTemp = ParseString(strRemaining, "-", null, out strRemaining);
                        endTimeMS = Int32.Parse(strTemp);
                        //Console.WriteLine("Before GC#{0} time {1} {2}, {3} entries",
                        //    gcIndex, startTimeMS, endTimeMS, ((endTimeMS - startTimeMS) / timeUnitMS));
                    }
                    else if (s.StartsWith("[p"))
                    {
                        if (fSkip)
                            continue;

                        //[p78]-192-225ms
                        strTemp = ParseString(s, "[p", "]", out strRemaining);
                        currentProcIndex = Int32.Parse(strTemp);
                        lastTimeIndex = -1;
                        //currentProcIndex = GetAdjustedProcIndex(currentProcIndex);
                    }
                    else if (s.StartsWith("[GC_"))
                    {
                        if (fSkip)
                            continue;

                        //[GC_alloc_mb]
                        //[N# 24]17,16,16,15,16,16,17,16,16,15,17,16,15,16,17,16,17,15,16,16,16,16,17,16,16,16,16,16,16,16,16,16,17,15,16,16,16,16,16,16,16,15,16,16,16,16,16,16,16,16,16,15,16,16,16,16,
                        //[N# 24]21,22,22,23,22,22,21,22,22,22,23,21,22,22,22,22,22,22,22,21,21,24,4,22,21,22,22,22,22,22,22,22,22,23,21,21,22,23,23,22,21,22,23,21,22,22,22,22,22,22,22,21,21,22,21,22,
                        int totalNodesRead = 0;
                        while ((s = sr.ReadLine()) != null)
                        {
                            if (s.StartsWith("[N"))
                            {
                                //Console.WriteLine(s);
                                int procIndexBase = totalNodesRead * procsPerNode;
                                strTemp = ParseString(s, "[N#", "]", out strRemaining);
                                budgetMB = Int32.Parse(strTemp);
                                string strAllocLine = s.Substring(7);
                                //Console.WriteLine("spliting {0}", strAllocLine);
                                string[] fieldsAlloc = strAllocLine.Split(new Char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                for (int fieldIndex = 0; fieldIndex < procsPerNode; fieldIndex++)
                                {
                                    AllocMB[procIndexBase + fieldIndex] = Int32.Parse(fieldsAlloc[fieldIndex]);
                                }
                                totalNodesRead++;
                                if (totalNodesRead == totalNodes)
                                    break;
                            }
                        }
                    }
                    else if (s.StartsWith("[GC#"))
                    {
                        if (fSkip)
                            continue;

                        //Console.WriteLine(s);
                        PrintProcActivityOnNode();
                        PrintAllocToAllPassOneFiles();

                        // gc index-duration
                        //[GC#1-15-2911711]
                        strTemp = ParseString(s, "-", "-", out strRemaining);
                        totalGCCount++;
                        totalGCDurationMS += Int32.Parse(strTemp);

                        //Console.WriteLine(s);
                        //Console.WriteLine("GC#{0} {1}ms ->total {2}ms", totalGCCount, Int32.Parse(strTemp), totalGCDurationMS);

                        PrintToAllPassOneFiles(s);
                    }
                    else if (s.StartsWith("[N#"))
                    {
                        if (fSkip)
                            continue;
                    }
                    else
                    {
                        if (fSkip)
                            continue;

                        // Majority of the log, (83) is the ideal proc for that thread.
                        //1051ms,95(1),83,m:0,p:0,i:0(83)
                        strTemp = ParseString(s, null, "ms", out strRemaining);
                        int currentTimeIndex = Int32.Parse(strTemp);

                        if (currentTimeIndex < lastTimeIndex)
                        {
                            //Console.WriteLine("!!bad time stamp at p{0} -{1}-", currentProcIndex, s);
                            continue;
                        }

                        lastTimeIndex = currentTimeIndex;

                        currentTimeIndex -= startTimeMS;
                        currentTimeIndex /= timeUnitMS;
                        strTemp = ParseString(strRemaining, ",", "(", out strRemaining);
                        int tid = Int32.Parse(strTemp);
                        strTemp = ParseString(strRemaining, "(", ")", out strRemaining);
                        int countSamples = Int32.Parse(strTemp);
                        strTemp = ParseString(strRemaining, ",", ",", out strRemaining);
                        int allocHeap = Int32.Parse(strTemp);
                        strTemp = ParseString(strRemaining, ",m:", ",", out strRemaining);
                        int flags = Int32.Parse(strTemp);
                        strTemp = ParseString(strRemaining, ",p:", ",", out strRemaining);
                        flags |= Int32.Parse(strTemp) << 4;
                        strTemp = ParseString(strRemaining, ",i:", "(", out strRemaining);
                        flags |= Int32.Parse(strTemp) << 8;
                        strTemp = ParseString(strRemaining, "(", ")", out strRemaining);
                        int idealProcNo = Int32.Parse(strTemp);
                        //Console.WriteLine("ADDING time {0}ms, entry {1}, thread #{2}, ah {3}, ideal {4}",
                        //    currentTimeIndex, currentProcIndex, tid, allocHeap, idealProcNo);
                        samples[currentTimeIndex][currentProcIndex] = new SampleInfo(tid, allocHeap, countSamples, flags, idealProcNo);
                    }
                }
            }

            var threadsSeenTotalOrdered = threadsSeenTotal.OrderByDescending(i => i.Value);
            swPassOneFiles[(int)PassOneViewType.Thread].WriteLine("\n-----------Total samples per thread-----------");
            foreach (var item in threadsSeenTotalOrdered)
            {
                int k = item.Key;
                int v = item.Value;
                swPassOneFiles[(int)PassOneViewType.Thread].WriteLine("tid: {0,5}-{1,-5}", k, v);
            }

            CloseAllPassOneFiles();
        }

        // TODO: in pass zero there's merit in assigning thread indices based on the first CPU they appear on,
        // instead of just assigning one as we come across them.
        static void Main(string[] args)
        {
            int len = args.Length;
            string strLog = null;

            for (int i = 0; i < args.Length; ++i)
            {
                string currentArg = args[i];
                string currentArgValue;
                if (currentArg.Equals("-l") || currentArg.Equals("-Log"))
                {
                    strLog = args[++i];
                }
                else if (currentArg.Equals("-ia") || currentArg.Equals("-IncludeAll"))
                {
                    currentArgValue = args[++i];
                    fIncludeAllTime = (Int32.Parse(currentArgValue) == 1);
                }
                else if (currentArg.Equals("-ti") || currentArg.Equals("-ThreadIndices"))
                {
                    // This is a comma separated indices
                    // -ti 88,110
                    currentArgValue = args[++i];
                    ParseThreadIndices(currentArgValue);
                }
                else if (currentArg.Equals("-gr") || currentArg.Equals("-GCRange"))
                {
                    // This specifies a range of GCs, inclusive
                    //10-20
                    //10-end
                    //start-10
                    currentArgValue = args[++i];
                    ParseGCRange(currentArgValue);
                }
                else if (currentArg.Equals("-pti") || currentArg.Equals("-PrintThreadInfo"))
                {
                    // prints the # of threads we see for each unit of time we display and how many
                    // total procs they ran on during that time. This shows us how volatile threads
                    // are jumping between procs.
                    currentArgValue = args[++i];
                    if (Int32.Parse(currentArgValue) == 1)
                        fPrintThreadInfoPerTimeUnit = true;
                }
            }

            Console.WriteLine("Processing {0}, {1}", strLog, (fIncludeAllTime ? "full time view" : "compressed time view"));
            PassZero(strLog);
            // Feel free to convert PassOne to print out all node's activity at once.
            // I used pass in the node # to print so did this the lazy way.
            for (int i = 0; i < totalNodes; i++)
            {
                nodeIndexToPrint = i;
                PassOne(strPassZeroLog);
            }
        }
    }
}
