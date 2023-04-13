// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Runtime.Serialization;
using Xunit;

public class mytest {
	[Fact]
	public static int TestEntryPoint() {
		int                      rValue = 100;
		ThreadStateException     ta     = null;
		
		Console.WriteLine("Test AutoResetEvent for expected NullRef Exceptions");
		Console.WriteLine( );

		try {
			ta.HelpLink = "Hello";
			rValue = 1;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.HelpLink = Hello)");
		}

		try {
			String s = ta.HelpLink;
			rValue = 2;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (string s = ta.HelpLink)");
		}

		try {
			Exception e = ta.InnerException;
			rValue = 3;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.InnerException)");
		}

		try {
			String s = ta.Message;
			rValue = 4;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.Message)");
		}

		try {
			String s = ta.Source;
			rValue = 5;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.Source)");
		}

		try {
			String s = ta.StackTrace;
			rValue = 6;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.StackTrace)");
		}
		
		// try {
		// 	ta.TargetSite.ToString();
		// 	rValue = 7;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (ta.TargetSite))");
		// }
		
		try {
			ta.Equals(new Exception("Hello"));
			rValue = 8;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.Equals(new Exception()))");
		}

		try {
			ta.GetBaseException();
			rValue = 9;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.GetBaseException())");
		}

		try {
			ta.GetHashCode();
			rValue = 10;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.GetHasCode())");
		}

		// try {
		// 	ta.GetObjectData(new SerializationInfo(1.GetType(),new FormatterConverter()),new StreamingContext(StreamingContextStates.All));
		// 	rValue = 11;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (ta.ObjectData(SerializationInfo,StreamingContext))");
		// }		

		try {
			ta.GetType();
			rValue = 12;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.GetType())");
		}

		try {
			ta.ToString();
			rValue = 13;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (ta.ToString())");
		}
		

		Console.WriteLine("Return Code == {0}",rValue);
		return rValue;
	}
}
