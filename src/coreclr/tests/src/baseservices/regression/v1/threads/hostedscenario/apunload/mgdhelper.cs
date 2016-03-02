// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

namespace Helper
{
	public interface ISample
	{
		int SpinThread();
	}
	
	[ClassInterface(ClassInterfaceType.None)]
	public class Sample : ISample
	{

		int aNumber;
		Thread t;

		public Sample()
		{
			aNumber = 500;
			t = null;
		}

		public int SpinThread()
		{
			Console.WriteLine("Inside Sample::SpinThread");
                        t = new Thread(new ThreadStart(WorkerFunc));
			t.Start();
			return aNumber;
		}

		public static void WorkerFunc()
		{			
		     Console.WriteLine("Inside Sample::SpinThread::WorkerFunction");
		     Console.WriteLine("   The following loop will not end...");
		     int i = 0;
		     while (true)
		     {
		       if(i%1000 == 0)
			 Console.WriteLine("Iteration::{0}",i++);
		     }
                     //Console.WriteLine("Exit Sample::SpinThread::WorkerFunction");
		}

		~Sample()
		{
			Console.WriteLine("Inside Sample::Finalize");
 		}
	}
}
