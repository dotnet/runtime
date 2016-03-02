// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

namespace AVHelper
{
	public interface ICreateAV
	{
		void SpinThread();
		int ThreadName();
	}
	
	public class CreateAV : ICreateAV
	{
		public String myAV;
		
		public CreateAV(){}

		public void SpinThread()
		{
			myAV = "Created a new String -- Let's get an AV";
			Console.WriteLine("Running SpinThread");
		}

		public int ThreadName()
		{
			Console.WriteLine(myAV);
			return 100;
		}

		~CreateAV()
		{
			Thread.Sleep(Timeout.Infinite);
 		}
	}
}
