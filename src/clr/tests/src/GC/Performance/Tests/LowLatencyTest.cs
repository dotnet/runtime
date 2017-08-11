// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Runtime;

namespace GCTest
{

    public class GCTestC
    {

        static int loid=0; // to give a "unique" identifier

        public static void Usage()
        {
            Console.WriteLine("Usage");
            Console.WriteLine("LowLatency.exe <iterations> <lowlatency|interactive|batch>");
        }


        static void Main(string[] args)
        {
            if (args.Length!=2)
            {
                Usage();
                return;
            }

            int iters = 0;
            if (!Int32.TryParse(args[0], out iters))
            {
                Usage();
                return;
            }

            GCLatencyMode gcMode;

            switch (args[1].ToLower())
            {
                case "lowlatency":
                    gcMode = GCLatencyMode.LowLatency;
                    break;
                case "interactive":
                    gcMode = GCLatencyMode.Interactive;
                    break;
                case "batch":
                    gcMode = GCLatencyMode.Batch;
                    break;
                default:
                    Usage();
                    return;
            }

            GCTestC test = new GCTestC();
            test.DoTest(iters, gcMode);
        }


        private void LoadData( int count, ref long[] values )
        {

            loid++;
            Hashtable aMap = new Hashtable(count);
            byte[] aBuffer = null;

            using (StreamReader reader = new StreamReader("clunie_small.xml"))
            {
                aBuffer = new byte[reader.BaseStream.Length];
                reader.BaseStream.Read(aBuffer, 0, (int)reader.BaseStream.Length);
            }

            using (MemoryStream aMemStream = new MemoryStream(aBuffer))
            {
                aBuffer = null;

                values[0] = PerformanceCounter.QueryCounter;
                for (int i = 0; i < count; i++)
                {
                    Thread.Sleep(0); // simulate waiting on arrival of new data...

                    // create XmlDocuments
                    XmlDocument aXmlDoc = new XmlDocument();
                    aXmlDoc.Load(aMemStream);
                    aMap.Add("1.2.3" + loid.ToString() + i.ToString(), aXmlDoc);
                    aXmlDoc = null;

                    // reset the position in the memory stream to the beginning
                    aMemStream.Seek(0, SeekOrigin.Begin);

                    values[i + 1] = PerformanceCounter.QueryCounter;
                }

            }

        }


        private void PrintStat(long[] values)
        {
            int count=values.Length-1;
            long max = values[1]-values[0];

            for (int i=0;i<count;i++)
            {
                max = Math.Max(max, (values[i+1]-values[i]));
            }

            //Console.WriteLine("Maximum of {0}: {1}", count, PerformanceCounter.PrintTime(PerformanceCounter.Seconds(max)));

        }


        public void DoTest(int count, GCLatencyMode gcMode)
        {

            GCLatencyMode oldMode = GCSettings.LatencyMode;

            long[] values = new long[count+1]; // reuse this array for every test ...

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                // Load Data
                GCSettings.LatencyMode = gcMode;
                LoadData(count, ref values);
            }
            finally
            {
                GCSettings.LatencyMode = oldMode;
            }

            PrintStat(values);
        }


        public static class PerformanceCounter
        {
            [DllImport("kernel32.dll", CallingConvention=CallingConvention.Winapi, EntryPoint="QueryPerformanceFrequency")]
            private static extern int QueryPerformanceFrequency_int(ref long count);
            [DllImport("kernel32.dll", CallingConvention=CallingConvention.Winapi, EntryPoint="QueryPerformanceCounter")]
            private static extern int QueryPerformanceCounter_int(ref long count);

            public static long QueryCounter
            {
                get
                {
                    long var = 0;
                    QueryPerformanceCounter_int(ref var);
                    return var;
                }
            }

            public static long Frequency
            {
                get
                {
                    long var = 0;
                    QueryPerformanceFrequency_int(ref var);
                    return var;
                }
            }

            public static double Seconds(long v)
            {
                return (double)v/Frequency;
            }


            public static string PrintTime(double time)
            {
                return (time*1000).ToString("0.000ms");
            }
        }

    }
}
