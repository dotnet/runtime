// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
			int rValue = 0;
			Thread[] threads = new Thread[100];
			ThreadSafe tsi = new ThreadSafe();
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(new ThreadStart(tsi.ThreadWorker));
				threads[i].Start();
			}
			
			tsi.Signal();

			for(int i=0;i<threads.Length;i++)
				threads[i].Join();

			if(tsi.Total == tsi.Expected * threads.Length)
				rValue = 100;
			Console.WriteLine("Test Expected {0}, but found {1}", tsi.Expected * threads.Length, tsi.Total);
			Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
			return rValue;
		}
	}

	public class ThreadSafe
	{
		ManualResetEvent signal;
		private int totalValue = 0;		
		private int numberOfIterations;
		private int valueToAdd;
		public ThreadSafe(): this(100,100) { }
		public ThreadSafe(int loops, int addend)
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
		public int Expected
		{
			get
			{
				return (numberOfIterations * valueToAdd);
			}
		}
		public int Total
		{
			get { return totalValue; }
		}
		private int AddToTotal(int addend)
		{
			int initialValue, computedValue;
			do
			{
				initialValue = totalValue;
				computedValue = initialValue + addend;
			} while (initialValue != Interlocked.CompareExchange(ref totalValue, computedValue, initialValue));
			return computedValue;
		}
	}	
}
