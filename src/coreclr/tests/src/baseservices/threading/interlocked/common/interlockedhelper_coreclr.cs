// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace InterlockedTest
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	///
	class LongTest
	{
		long sharedValue;
		int iterations;
		ManualResetEvent signal;
		Thread[] threads;
		public LongTest() { }
		public LongTest(int LoopValue, int NumberOfThreads)
		{
			threads = new Thread[NumberOfThreads];
			iterations = LoopValue;
			signal = new ManualResetEvent(false);
		}
		public int Inc()
		{
			sharedValue = 0;
			long expectedValue = iterations * threads.Length;

			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(new ThreadStart(this.SimpleIncrement));
				threads[i].Start();
			}

			Thread.Sleep(100);
			signal.Set();
			for (int i = 0; i < threads.Length; i++)
				threads[i].Join();
			if (sharedValue == expectedValue)
				return 100;

			return -1;
		}
		public int Dec()
		{
			sharedValue = iterations * threads.Length;

			int expectedValue = 0;

			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(new ThreadStart(this.SimpleDecrement));
				threads[i].Start();
			}

			Thread.Sleep(100);
			signal.Set();
			for (int i = 0; i < threads.Length; i++)
				threads[i].Join();

			if (sharedValue == expectedValue)
				return 100;

			return -1;
		}
		private void SimpleIncrement()
		{
			signal.WaitOne();
			for (int i = 0; i < iterations; i++)
				Interlocked.Increment(ref sharedValue);
		}
		private void SimpleDecrement()
		{
			signal.WaitOne();
			for (int i = 0; i < iterations; i++)
				Interlocked.Decrement(ref sharedValue);
		}
		public int CheckIncReturn()
		{
			long sharedValue;
			int rValue = 0;

			foreach (long val in LongVals)
			{
				sharedValue = val;
				
				Console.WriteLine("Test for Inc with value {0}", val);
				Console.WriteLine("Expected : {0}", val + 1);
				long returnV = Interlocked.Increment(ref sharedValue);
				if (val + 1 != returnV )
				{
					Console.WriteLine("Failed: Return value is wrong: {0}", returnV);
					rValue = -1;
				}
				if (val + 1 != sharedValue)
				{
					Console.WriteLine("Failed: refence value is wrong: {0}", sharedValue);
					rValue = -2;
				}
			}

			if (rValue == 0)
				rValue = 100;

			return rValue;
		}
		public int CheckDecReturn()
		{
			long sharedValue;
			int rValue =0;
			foreach (long val in LongVals)
			{
				sharedValue = val;
				
				Console.WriteLine("Test for Dec with value {0}", val);
				Console.WriteLine("Expected : {0}", val - 1);
				long returnV = Interlocked.Decrement(ref sharedValue);
				if (val - 1 != returnV)
				{
					Console.WriteLine("Failed: Return value is wrong: {0}", returnV);
					rValue = -1;
				}
				if (val -1 != sharedValue)
				{
					Console.WriteLine("Failed: refence value is wrong: {0}", sharedValue);
					rValue = -2;
				}
			}
			if(rValue == 0)
				rValue = 100;
			return rValue;
		}
        long[] LongVals = new long[5]
		{   
			Int64.MinValue,
			Int64.MaxValue,
			0,
			-1,
			1
		};
	}



	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	///
	class IntTest
	{
		int sharedValue;
		int iterations;
		ManualResetEvent signal;
		Thread[] threads;
		public IntTest(int LoopValue, int NumberOfThreads)
		{
			threads = new Thread[NumberOfThreads];
			iterations = LoopValue;
			signal = new ManualResetEvent(false);
		}
		public int Inc()
		{
			sharedValue = 0;

			int expectedValue = iterations * threads.Length;

			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(new ThreadStart(this.SimpleIncrement));
				threads[i].Start();
			}

			Thread.Sleep(100);
			signal.Set();
			for (int i = 0; i < threads.Length; i++)
				threads[i].Join();

			if (sharedValue == expectedValue)
				return 100;

			return -1;
		}
		public int Dec()
		{
			sharedValue = iterations * threads.Length;

			int expectedValue = 0;

			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(new ThreadStart(this.SimpleDecrement));
				threads[i].Start();
			}

			Thread.Sleep(100);
			signal.Set();
			for (int i = 0; i < threads.Length; i++)
				threads[i].Join();

			if (sharedValue == expectedValue)
				return 100;

			return -1;
		}
		private void SimpleIncrement()
		{
			signal.WaitOne();
			for (int i = 0; i < iterations; i++)
				Interlocked.Increment(ref sharedValue);
		}
		private void SimpleDecrement()
		{
			signal.WaitOne();
			for (int i = 0; i < iterations; i++)
				Interlocked.Decrement(ref sharedValue);
		}
		public int CheckIncReturn()
		{
			int sharedValue;
			int rValue = 0;

			foreach (int val in IntVals)
			{
				sharedValue = val;
				Console.WriteLine("Test for Inc with value {0}", val);
				Console.WriteLine("Expected : {0}", val - 1);
				int returnV = Interlocked.Increment(ref sharedValue);
				if (val + 1 != returnV )
				{
					Console.WriteLine("Failed: Return value is wrong: {0}", returnV);
					rValue = -1;
				}
				if (val + 1 != sharedValue)
				{
					Console.WriteLine("Failed: refence value is wrong: {0}", sharedValue);
					rValue = -2;
				}
			}

			if (rValue == 0)
				rValue = 100;

			return rValue;
		}
		public int CheckDecReturn()
		{
			int sharedValue;
			int rValue =0;
			foreach (int val in IntVals)
			{
				sharedValue = val;
				Console.WriteLine("Test for Dec with value {0}", val);
				Console.WriteLine("Expected : {0}", val - 1);
				int returnV = Interlocked.Decrement(ref sharedValue);
				if (val - 1 != returnV)
				{
					Console.WriteLine("Failed: Return value is wrong: {0}", returnV);
					rValue = -1;
				}
				if (val -1 != sharedValue)
				{
					Console.WriteLine("Failed: refence value is wrong: {0}", sharedValue);
					rValue = -2;
				}
			}
			if(rValue == 0)
				rValue = 100;
			return rValue;
		}
        int[] IntVals = new int[5]
		{
			Int32.MinValue,
			Int32.MaxValue,
			0,
			-1,
			1
		};
	}
}
