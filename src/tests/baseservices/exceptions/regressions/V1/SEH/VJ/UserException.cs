// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class UserException : Exception {
	[Fact]
	public static int TestEntryPoint() {
			int counter = 0;
			String str = "Done";

			for (int j = 0; j < 100; j++){
				try {
					if (j % 2 == 0)
						counter = j / (j % 2);
					else
						throw new UserException();
				}
				catch ( UserException ) {
					counter++;
					continue;
				}
				catch (ArithmeticException ){
					counter--;
					continue;	
				}
				catch (Exception ){}
				finally {
					counter++;
				}
			}
			if (counter == 100){
				Console.WriteLine( "TryCatch Test Passed" );
				return 100;
			}
			else{
				Console.WriteLine( "TryCatch Test Failed" );
				return 1;
			}
			Console.WriteLine(str);
	}
}
