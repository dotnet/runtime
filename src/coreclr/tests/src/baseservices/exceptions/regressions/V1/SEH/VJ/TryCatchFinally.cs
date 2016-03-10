// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;


public class TryCatchFinally{
	public static int Main(String [] args) {
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
