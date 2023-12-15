// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;


public class TryCatch{
	[Fact]
	public static int TestEntryPoint() {
                        int retVal = 100;
			int i = 0;
			String m_str = "";
			String str = "Done";

			try {
				throw new ArithmeticException();
			}
			catch ( ArithmeticException ) {
				m_str = m_str + "ArithmeticException\n";
				i++;
			}

			try {
				throw new DivideByZeroException();
			}
			catch ( DivideByZeroException ) {
				m_str = m_str + "DivideByZeroException\n";
				i++;
			}

			try {
				throw new OverflowException();
			}
			catch ( OverflowException ) {
				m_str = m_str + "OverflowException\n";
				i++;
			}

			try {
				throw new ArgumentException();
			}
			catch ( ArgumentException ) {
				m_str = m_str + "ArgumentException\n";
				i++;
			}

			try {
				throw new ArrayTypeMismatchException();
			}
			catch ( ArrayTypeMismatchException ) {
				m_str = m_str + "ArrayTypeMismatchException\n";
				i++;
			}

			try {
				throw new MemberAccessException();
			}
			catch ( MemberAccessException ) {
				m_str = m_str + "AccessException\n";
				i++;
			}

			try {
				throw new FieldAccessException();
			}
			catch ( FieldAccessException ) {
				m_str = m_str + "FieldAccessException\n";
				i++;
			}

			try {
				throw new MissingFieldException();
			}
			catch ( MissingFieldException ) {
				m_str = m_str + "MissingFieldException\n";
				i++;
			}

			try {
				throw new MethodAccessException();
			}
			catch ( MethodAccessException ) {
				m_str = m_str + "MethodAccessException\n";
				i++;
			}

			try {
				throw new MissingMethodException();
			}
			catch ( MissingMethodException ) {
				m_str = m_str + "MissingMethodException\n";
				i++;
			}

			Console.WriteLine( m_str );
			if (i == 10){
				Console.WriteLine("Test Passed");
			}
			else {
				Console.WriteLine("Test Failed");
				retVal = 1;
			}
			Console.WriteLine(str);
                        return retVal;

	}
}
