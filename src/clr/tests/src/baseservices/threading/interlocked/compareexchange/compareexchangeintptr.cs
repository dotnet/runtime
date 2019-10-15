// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Collections;

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
			ThreadSafe tsi = new ThreadSafe(threads.Length);
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(new ThreadStart(tsi.ChangeValue));
				threads[i].Name = i.ToString();
				threads[i].Start();
			}
			
			tsi.Signal();

			for(int i=0;i<threads.Length;i++)
				threads[i].Join();
			
			if(tsi.Success)
				rValue = 100;
			Console.WriteLine("Test {0}", rValue == 100 ? "Passed" : "Failed");
			return rValue;
		}
	}

	public class ThreadSafe
	{
		private int threadCount;
		ManualResetEvent signal;
		private IntPtr mValue;
		private int accessCount;
        private static object myLock = new Object();

		public ThreadSafe(int size)
		{
			threadCount = size;
			mValue = IntPtr.Zero;
			signal = new ManualResetEvent(false);
		}

		public void Signal()
		{
			signal.Set();
		}
		public bool Success
		{			
			get { 
				Console.WriteLine("AccessCount {0} should equal threadCount {1}", accessCount, threadCount);
				Console.WriteLine("mValue.ToInt32 {0} should equal threadCount {1}", mValue.ToInt32(), threadCount);
				return (accessCount == threadCount) && (mValue.ToInt32() == threadCount); 
			}
		}
		public void ChangeValue()
		{
			IntPtr initialValue, newValue;

			signal.WaitOne();
			do
			{
                initialValue = mValue;
                lock (myLock)
                {
                    newValue = new IntPtr(mValue.ToInt32() + 1);
                }
			} while (initialValue != Interlocked.CompareExchange(ref mValue, newValue, initialValue));
			Interlocked.Increment(ref accessCount);
		}
	}	
}
