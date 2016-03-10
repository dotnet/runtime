// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.IO;

public class UserExceptionThread : Exception {
        static int retVal = 100;

	public static int Main(String []args) {
		Thread mv_Thread;
		String str = "Done";
		UserExceptionThread ue = new UserExceptionThread();
		for (int i = 0 ; i < 10; i++){
			mv_Thread = new Thread(new ThreadStart(ue.runtest));
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
			for (int j = 0; j < 100; j++){
				try {
					if (j % 2 == 0)
						counter = j / (j % 2);
					else
						throw new UserExceptionThread();
				}
				catch ( UserExceptionThread ) {
					counter++;
					continue;
				}
				catch (ArithmeticException ){
					counter--;
					continue;	
				}
				finally {
					counter++;
				}
			}
			if (counter == 100){
				lock(this){
					Console.WriteLine( "TryCatch Test Passed" );
				}
			}
			else{
				Console.WriteLine( "TryCatch Test Failed" );
				retVal = 1;
			}
	}
}
