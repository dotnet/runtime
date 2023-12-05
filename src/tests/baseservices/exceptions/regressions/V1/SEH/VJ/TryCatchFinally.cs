// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;


public class TryCatchFinally{
	[Fact]
	public static int TestEntryPoint() {
			int i = 1;
			String m_str = "Failed";
			String str = "Done";
			
			try {
				throw new ArithmeticException();
			}
			catch ( ArithmeticException ) {
				m_str = "Passed Catch";
				i = 1;
			}
			finally {
				m_str = m_str + " and Passed Finally";
				i = 100;				
			}
		
			Console.WriteLine( "TryCatch Test " + m_str );
			Console.WriteLine(str);
                        return i;
			
	}
}
