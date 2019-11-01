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
		ArrayList myObjArray;
		ArrayList postObjArray;
		ManualResetEvent signal;
		private Object mValue;
		private int accessCount;
		public ThreadSafe(int arraySize)
		{
			accessCount = 0;
			mValue = null;
			postObjArray = new ArrayList(arraySize + 1);
			myObjArray = new ArrayList(arraySize+1);
			for(int i=0;i<arraySize;i++)
                myObjArray.Add(new Object());
			signal = new ManualResetEvent(false);
		}

		public void Signal()
		{
			signal.Set();
		}
		public bool Success
		{
			get
			{
				for (int i = 0; i < postObjArray.Count; i++)
					for (int j = i+1; j < postObjArray.Count; j++)
						if (postObjArray[i] == postObjArray[j])
						{
							Console.WriteLine("Failure!!!!");
							Console.WriteLine("ValueOne:" + postObjArray[i]);
							Console.WriteLine("ValueTwo:" + postObjArray[j]);
							Console.WriteLine("Position:" + i + "  " + j);
							return false;
						}
				//No dups so check for proper count
				Console.WriteLine("Expect accessCount {0} to equal postObjArray.Count {1}", accessCount, postObjArray.Count);
				return (accessCount == (postObjArray.Count));
			}
		}
		public void ChangeValue()
		{
			Object initialValue, newValue;
			signal.WaitOne();
			do
			{
				initialValue = mValue;
				newValue = myObjArray[Int32.Parse(Thread.CurrentThread.Name)];
			} while (initialValue != Interlocked.CompareExchange(ref mValue, newValue, initialValue));
			lock (this)
			{
				postObjArray.Add(initialValue);
			}
			Interlocked.Increment(ref accessCount);
		}
	}
}
