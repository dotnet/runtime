// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

namespace interoptest
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	//[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISample
	{
		int GetANumber();
		[return:MarshalAs(UnmanagedType.IUnknown)]
		object GetAnObject();
		HelperClass GetAnHelperClass();
	}

	public interface IHelper
	{
		string GetName();
	}
	
	[ClassInterface(ClassInterfaceType.None)]
	public class Sample : ISample
	{

		int aNumber;
		HelperClass myObject;
		Thread t;

		public Sample()
		{
			aNumber = 19;
			myObject = new HelperClass("ferit");
			t = null;
		}

		public int GetZero()
		{
			return 0;
		}

		public int GetANumber()
		{
			ThreadStart worker = new ThreadStart(WorkerThreadMethod);
            Console.WriteLine("Inside Sample::GetANumber");

			t = new Thread(worker);
			t.Start();

			return aNumber;
		}

		public static void WorkerThreadMethod()
		{
			Console.WriteLine("inside Worker Thread");
		}

		public object GetAnObject()
		{
			return myObject;
		}

		public HelperClass GetAnHelperClass()
		{
			return myObject;
		}

		~Sample()
		{
			Console.WriteLine("Inside Sample::Finalize");
 		}
	}

	[ClassInterface(ClassInterfaceType.None)]
	public class HelperClass : IHelper
	{
		string theName;

		public HelperClass(string name)
		{
			theName = name;
		}

		public string GetName()
		{
			return theName;
		}
	}
}
