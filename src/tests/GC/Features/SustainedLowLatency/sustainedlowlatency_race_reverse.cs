// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime;

namespace SustainedLowLatencyTest
{
    class SLL
    {
        //The test tries to unset SustainedLowLatency when a foreground GC is in progress
        //Regression test for Bug 576224: Race condition using GCSettings.LatencyMode
        static volatile bool setSSLdone = false;
        static Int64 iterations = 2000;
        static bool runForever = false;
        static bool failed = false;
        static GCLatencyMode initialMode;

        static int Main(string[] args)
        {
            if (args.Length > 0)
                iterations = Int64.Parse(args[0]);

            if (iterations == -1)
                runForever = true;
            if (runForever)
                Console.WriteLine("Run until fail");
            else
                Console.WriteLine("Run {0} iterations", iterations);

            initialMode = GCSettings.LatencyMode;
            Console.WriteLine("Initial mode is: " + initialMode);
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            Thread t1 = new Thread(SetSLL);
            Thread t2 = new Thread(Allocate);
            int numThreads = 100;
            Thread[] threadArr = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threadArr[i] = new Thread(AllocateTempObjects);
                threadArr[i].Start();
            }
           
            t1.Start();
            t2.Start();
            
            t1.Join();
            t2.Join();
            for (int i = 0; i < numThreads; i++)
            {
                threadArr[i].Join();
            }

            if (failed)
            {
                Console.WriteLine("Test failed");
                return 1;
            }
            Console.WriteLine("Test passed");
            return 100;

        }

          public static void SetSLL(object threadInfoObj)
          {
              System.Threading.Thread.Sleep(50);
             
             Int64 counter = 0;
              while (!failed &&(runForever || counter<iterations))
              {
                  counter++;
                  GC.Collect(2, GCCollectionMode.Optimized, false);

                  for (int j = 0; j < 100; j++)
                  {
                     
                      GCSettings.LatencyMode = initialMode;
                      GCLatencyMode lmOrig = GCSettings.LatencyMode;
                      if (lmOrig != initialMode)
                      {
                          Console.WriteLine("latency mode is {0}; expected {1}", lmOrig, initialMode);
                          failed = true;
                          break;
                      }
                      GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

                      GCLatencyMode lm = GCSettings.LatencyMode;
                      //   Console.WriteLine(lm);
                      if (lm != GCLatencyMode.SustainedLowLatency)
                      {
                          Console.WriteLine("latency mode is {0}; expected GCLatencyMode.SustainedLowLatency", lm);
                          failed = true;
                          break;
                      }
                  }
             
                  Thread.Sleep(100);
              }
              setSSLdone = true;
          }


          public static void AllocateTempObjects(object threadInfoObj)
          {
              int listSize2 = 1000;
              List<byte[]> tempList = new List<byte[]>();
              while (!setSSLdone)
              {
                  byte[] temp = new byte[20];
                  for (int i = 0; i < listSize2; i++)
                  {
                      tempList.Add(new byte[50]);
                  }
                  tempList.Clear();
              }

          }

          public static void Allocate(object threadInfoObj)
          {
              int ListSize = 300;
              System.Random rnd = new Random(1122);
             
              int listSize2 = 1000;
              List<byte[]> newList = new List<byte[]>(500+1000);
              

              while (!setSSLdone)
              {
                  for (int i = 0; i < ListSize; i++)
                  {
                      newList.Add(new byte[85000]);
                      newList.Add(new byte[200]);
                      Thread.Sleep(10);
                  }
                  for (int i = 0; i < listSize2; i++)
                  {
                      newList.Add(new byte[50]);
                  }
                  newList.Clear();
              }

              
         

          }

     
}

          
      
}
