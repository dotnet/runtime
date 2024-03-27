// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

class ExternalClass {
	ExternalException ee = new ExternalException();
	
	public void ThrowException(){
		throw ee;	
	}
	
}


public class ExternalException : Exception {

        static int retVal = 100;

	[Fact]
	public static int TestEntryPoint() {
		Thread mv_Thread;
		String str = "Done";
		ExternalException ee = new ExternalException();
		for (int i = 0 ; i < 10; i++){
			mv_Thread = new Thread(new ThreadStart(ee.runtest));
			try {
				mv_Thread.Start();
			}
			catch (Exception ){
				Console.WriteLine("Exception was caught in main");
			}
		}
		Console.WriteLine(str);
                return retVal;
	}
		
	public void runtest(){	
		int counter = 0;
		//String m_str = "Failed";
			for (int j = 0; j < 100; j++){
				try {
					if (j % 2 == 0)
						counter = j / (j % 2);
					else
						recurse(0);
				}
				catch ( ArithmeticException ) {
					counter++;
					continue;
				}
				catch (ExternalException ){
					counter--;
					continue;	
				}
				finally {
					counter++;
				}
			}
			if (counter == 100){
				lock(this) {
					Console.WriteLine( "TryCatch Test Passed" );
				}
			}
			else{
				lock(this) {
					Console.WriteLine( "TryCatch Test Failed" );
					Console.WriteLine(counter);
					retVal = -1;
				}
			}
	}
	
	public void recurse(int counter){
		char [] abc  = new char[100];
		if (counter == 100) 
			(new ExternalClass()).ThrowException();
		else
			recurse(++counter);
	
	}
}



