// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Runtime;
using System.Diagnostics;

namespace GCTest
{
    public class GCTestC
    {
        private static int loid = 0; // to give a "unique" identifier
        private static Stopwatch s_stopWatch = new Stopwatch();
        public static void Usage()
        {
            Console.WriteLine("Usage");
            Console.WriteLine("LowLatency.exe <iterations> <lowlatency|interactive|batch>");
        }

        static void Main(string[] args)
        {
            if (args.Length != 2)
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


        private void LoadData(int count)
        {
            loid++;
            Hashtable aMap = new Hashtable(count);
            byte[] aBuffer = null;
            long maxElapsed = 0;

            string clunieFile = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "clunie_small.xml");
            using (StreamReader reader = new StreamReader(clunieFile))
            {
                aBuffer = new byte[reader.BaseStream.Length];
                reader.BaseStream.Read(aBuffer, 0, (int)reader.BaseStream.Length);
            }

            using (MemoryStream aMemStream = new MemoryStream(aBuffer))
            {
                aBuffer = null;
                s_stopWatch.Restart();
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
                    s_stopWatch.Stop();
                    if (maxElapsed < s_stopWatch.ElapsedMilliseconds)
                    {
                        maxElapsed = s_stopWatch.ElapsedMilliseconds;
                    }
                }

                Console.WriteLine("Maximum of {0}: {1}", count, maxElapsed);
            }

        }

        public void DoTest(int count, GCLatencyMode gcMode)
        {
            GCLatencyMode oldMode = GCSettings.LatencyMode;

            try
            {
                // Load Data
                GCSettings.LatencyMode = gcMode;
                LoadData(count);
            }
            finally
            {
                GCSettings.LatencyMode = oldMode;
            }
        }

    }
}
