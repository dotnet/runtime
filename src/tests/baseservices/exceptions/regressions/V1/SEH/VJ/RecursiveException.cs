// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.IO;
using Xunit;

class UserException : Exception {
	
}

public class RecursiveException {
	[Fact]
	public static int TestEntryPoint() {
		String s = "Done";
		int retVal = 100;
		Thread mv_Thread;
		RecursiveException re = new RecursiveException();
		for (int i = 0 ; i < 10; i++){
			mv_Thread = new Thread(new ThreadStart(re.runtest));
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
		catch (UserException ) {
		    lock(this)
			{
			    Console.WriteLine("The Exception was caught");
			}
		}
	}
	
	public void recurse(int counter) {	
		if (counter == 1000) 
			throw new UserException();
		else
			recurse(++counter);
	}
}
