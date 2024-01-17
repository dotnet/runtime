// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.IO;
using Xunit;

class UserException : Exception {
	internal int ExceptionId;
	
	public UserException(int id){
		ExceptionId = id;	
	}
}

public class MultipleException {
	private int ThreadId;

	public MultipleException(int id){
		ThreadId = id;
	}
		
	
	[Fact]
	public static int TestEntryPoint() {
		int retVal = 100;
		String s = "Done";
		Thread mv_Thread;
		MultipleException [] me = new MultipleException[10];
		for (int i = 0 ; i < 10; i++){
			me[i] = new MultipleException(i);
			mv_Thread = new Thread(new ThreadStart(me[i].runtest));
			try {
				mv_Thread.Start();
			}
			catch (Exception ){
				Console.WriteLine("Exception was caught in main");
				retVal = 0;
			}
		}
		Console.WriteLine(s);
		return retVal;
	}
		
	public void runtest(){
		try {
			recurse(0);
		}
		catch (UserException e) {
			lock(this){
				Console.WriteLine("The Exception  " + e.ExceptionId + " was caught");
			}
		}
	}
	
	public void recurse(int counter) {	
		if (counter == 1000) 
			throw new UserException( ThreadId );
		else
			recurse(++counter);
	}
}
