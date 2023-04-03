// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
// using System.Runtime.Remoting;
using System.Runtime.Serialization;
using Xunit;

public class mytest {
	[Fact]
	public static int TestEntryPoint() {
		int                           rValue = 100;
		SynchronizationLockException  sle    = null;
		
		Console.WriteLine("Test AutoResetEvent for expected NullRef Exceptions");
		Console.WriteLine( );

		try {
			sle.HelpLink = "Hello";
			rValue = 1;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.HelpLink = Hello)");
		}

		try {
			String s = sle.HelpLink;
			rValue = 2;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (string s = sle.HelpLink)");
		}

		try {
			Exception e = sle.InnerException;
			rValue = 3;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.InnerException)");
		}

		try {
			String s = sle.Message;
			rValue = 4;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.Message)");
		}

		try {
			String s = sle.Source;
			rValue = 5;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.Source)");
		}

		try {
			String s = sle.StackTrace;
			rValue = 6;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.StackTrace)");
		}
		
		// try {
		// 	sle.TargetSite.ToString();
		// 	rValue = 7;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (sle.TargetSite))");
		// }
		
		try {
			sle.Equals(new Exception("Hello"));
			rValue = 8;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.Equals(new Exception()))");
		}

		try {
			sle.GetBaseException();
			rValue = 9;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.GetBaseException())");
		}

		try {
			sle.GetHashCode();
			rValue = 10;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.GetHasCode())");
		}

		// try {
		// 	sle.GetObjectData(new SerializationInfo(1.GetType(),new FormatterConverter()),new StreamingContext(StreamingContextStates.All));
		// 	rValue = 11;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (sle.ObjectData(SerializationInfo,StreamingContext))");
		// }		

		try {
			sle.GetType();
			rValue = 12;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.GetType())");
		}

		try {
			sle.ToString();
			rValue = 13;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (sle.ToString())");
		}
		

		Console.WriteLine("Return Code == {0}",rValue);
		return rValue;
	}
}
