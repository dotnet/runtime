// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Exchange
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static int Main(string[] args)
		{
			int loops = 100;
			int rValue = 0;
			if(args.Length == 1)
				loops = Int32.Parse(args[0]);
			float valuetoadd = 10.12345F;
			Thread[] threads = new Thread[100];
			ThreadSafe tsi = new ThreadSafe(100,valuetoadd);
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(new ThreadStart(tsi.ThreadWorker));
				threads[i].Start();
			}
			
			tsi.Signal();

			for(int i=0;i<threads.Length;i++)
				threads[i].Join();
			float expected = 0.0F;
			for(int i=0;i<threads.Length*loops;i++)
				expected = (float)(expected + valuetoadd);
			if(tsi.Total == expected)
				rValue = 100;
			Console.WriteLine("Expected: "+expected);
			Console.WriteLine("Actual  : "+tsi.Total);
			Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
			return rValue;
		}
	}

	public class ThreadSafe
	{
		ManualResetEvent signal;
		private float totalValue = 0F;		
		private int numberOfIterations;
		private float valueToAdd;
		public ThreadSafe(): this(100,10.12345F) { }
		public ThreadSafe(int loops, float addend)
		{
			signal = new ManualResetEvent(false);
			numberOfIterations = loops;
			valueToAdd = addend;
		}

		public void Signal()
		{
			signal.Set();
		}

		public void ThreadWorker()
		{
			signal.WaitOne();
			for(int i=0;i<numberOfIterations;i++)
				AddToTotal(valueToAdd);

		}
		public float Expected
		{
			get
			{
				return (numberOfIterations * valueToAdd);
			}
		}
		public float Total
		{
			get { return totalValue; }
		}
		private float AddToTotal(float addend)
		{
			float initialValue, computedValue;
			do
			{
				initialValue = totalValue;
				computedValue = (float)(initialValue + addend);
			} while (initialValue != Interlocked.CompareExchange(ref totalValue, computedValue, initialValue));
			return computedValue;
		}
	}	
}
