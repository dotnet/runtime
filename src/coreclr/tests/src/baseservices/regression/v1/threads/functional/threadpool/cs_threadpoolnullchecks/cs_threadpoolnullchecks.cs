// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Threading;

#if WINCORESYS
[assembly:AllowPartiallyTrustedCallers]
#endif

public class CS_ThreadPoolNullChecks {
	public static void EventSig(Object state, bool wasSignalled) {
		Console.WriteLine("In EventSig: {0}.", Thread.CurrentThread.GetHashCode());
		Console.WriteLine("wasSignalled: " + wasSignalled);
	}
	public static int Main(String [] args) {
		Console.WriteLine("CS_ThreadPoolNullChecks ...");
		int					ret	= 100;
		ManualResetEvent	j	= new ManualResetEvent(false);
		try {
			ThreadPool.RegisterWaitForSingleObject(null,new WaitOrTimerCallback(EventSig),null,20,false);
			ret = 0;	//	Fail
		}
		catch (ArgumentNullException) {
		}
		try {
			ThreadPool.RegisterWaitForSingleObject(null,null,null,20,false);
			ret = 0;	//	Fail
		}
		catch (ArgumentNullException) {
		}
		try {
			ThreadPool.RegisterWaitForSingleObject(j,null,null,20,false);
			ret = 0;	//	Fail
		}
		catch (ArgumentNullException) {
		}
		try {
			ThreadPool.QueueUserWorkItem(null);
			Console.WriteLine("ThreadPool.QueueUserWorkItem(null) should have thrown an Exception!");
			ret = 0;	//	Fail
		}
		catch (ArgumentNullException) {
		}
		Console.WriteLine(" ... CS_ThreadPoolNullChecks   (ret == {0})",ret);
		return ret;
	}
}