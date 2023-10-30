// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// DataMisalignment
// NullRef (generic, nullable, object)
// Divide by zero (integral)
// Overflow (integral)
// Stack overflow
// OOM

// Array out of bounds (single, multi, jagged)
// Array null ref (single, multi, jagged)

public class HardwareEh
{
	public const long c_VALUE = 34252;
	public delegate bool TestDelegate();

	[Fact]
	public static int TestEntryPoint()
	{
		HardwareEh e = new HardwareEh();

		TestLibrary.TestFramework.BeginTestCase("Hardware exceptions: handled");

		if (e.RunTests())
		{
			TestLibrary.TestFramework.EndTestCase();
			TestLibrary.TestFramework.LogInformation("PASS");
			return 100;
		}
		else
		{
			TestLibrary.TestFramework.EndTestCase();
			TestLibrary.TestFramework.LogInformation("FAIL");
			return 0;
		}
	}

	public bool RunTests()
	{
		bool retVal = true;

		TestLibrary.TestFramework.LogInformation("[Positive]");
		retVal = PosTest1() && retVal;
		retVal = PosTest2() && retVal;
		retVal = PosTest3() && retVal;
		retVal = PosTest4() && retVal;
		retVal = PosTest5() && retVal;
		retVal = PosTest6() && retVal;
		retVal = PosTest7() && retVal;
		retVal = PosTest8() && retVal;
		retVal = PosTest9() && retVal;
// The current stack overflow behavior is to rip the process
//		retVal = PosTest10() && retVal;
//		retVal = PosTest11() && retVal;
		retVal = PosTest12() && retVal;
		retVal = PosTest13() && retVal;
		retVal = PosTest14() && retVal;
		retVal = PosTest15() && retVal;
		retVal = PosTest16() && retVal;
		retVal = PosTest17() && retVal;
		retVal = PosTest18() && retVal;
		retVal = PosTest19() && retVal;
		retVal = PosTest20() && retVal;

		return retVal;
	}

	public bool PosTest1() { return DataMisalign(1, false); }
	public bool PosTest2() { return DataMisalign(2, true); }
	public bool PosTest3() { return ExceptionTest(3, "NullReference", typeof(NullReferenceException),
								delegate()
								{
									object o = null;
									o.ToString();
									return true;
								} ); }
	public bool PosTest4() { return ExceptionTest(4, "NullReference (generic)", typeof(NullReferenceException),
								delegate()
								{
									List<int> l = null;
									l.ToString();
									return true;
								} ); }
	public bool PosTest5() { return ExceptionTest(5, "NullReference (nullable)", typeof(InvalidOperationException),
								delegate()
								{
									int? i = null;
									i.Value.ToString();
									return true;
								} ); }
	public bool PosTest6() { return ExceptionTest(6, "DivideByZero (int64)", typeof(DivideByZeroException),
								delegate()
								{
									Int64 i = 10;
									Int64 j = 0;
									Int64 k = i / j;
									return true;
								} ); }
	public bool PosTest7() { return ExceptionTest(7, "DivideByZero (int32)", typeof(DivideByZeroException),
								delegate()
								{
									Int32 i = 10;
									Int32 j = 0;
									Int32 k = i / j;
									return true;
								} ); }
	public bool PosTest8() { return ExceptionTest(8, "OverflowException (int64)", typeof(OverflowException), new TestDelegate(ILHelper.Int64Overflow) ); }
	public bool PosTest9() { return ExceptionTest(9, "OverflowException (int32)", typeof(OverflowException), new TestDelegate(ILHelper.Int32Overflow) ); }
//	public bool PosTest10() { return ExceptionTest(10, "StackOverflow", typeof(StackOverflowException),  new TestDelegate( GobbleStack )); }
/*	public bool PosTest11() { return ExceptionTest(11, "OutOfMemory", typeof(OutOfMemoryException),
								delegate()
								{
                                    List<object> list;
									list = new List<object>();
									while(true)
									{
										// allocate memory (86 meg chunks)
										list.Add( new byte[8388608]);
									}
								} ); } */
	public bool PosTest12() { return ExceptionTest(12, "IndexOutOfRange (single dim [less than])", typeof(IndexOutOfRangeException),
								delegate()
								{
									int[] arr = new int[10];
									int   index = -1;
									arr[index] = 0;
									return true;
								} ); }
	public bool PosTest13() { return ExceptionTest(13, "IndexOutOfRange (single dim [greater than])", typeof(IndexOutOfRangeException),
								delegate()
								{
									int[] arr = new int[10];
									int   index = 11;
									arr[index] = 0;
									return true;
								} ); }
	public bool PosTest14() { return ExceptionTest(14, "IndexOutOfRange (multi dim [less than])", typeof(IndexOutOfRangeException),
								delegate()
								{
									int[,] arr = new int[10,10];
									int   index = -1;
									arr[0,index] = 0;
									return true;
								} ); }
	public bool PosTest15() { return ExceptionTest(15, "IndexOutOfRange (multi dim [greater than])", typeof(IndexOutOfRangeException),
								delegate()
								{
									int[,] arr = new int[10,10];
									int   index = 11;
									arr[index,0] = 0;
									return true;
								} ); }
	public bool PosTest16() { return ExceptionTest(16, "IndexOutOfRange (jagged [less than])", typeof(IndexOutOfRangeException),
								delegate()
								{
									int[][] arr = new int[10][];
									int   index = -1;
									arr[0] = new int[10];
									arr[0][index] = 0;
									return true;
								} ); }
	public bool PosTest17() { return ExceptionTest(17, "IndexOutOfRange (jagged [greater than])", typeof(IndexOutOfRangeException),
								delegate()
								{
									int[][] arr = new int[10][];
									int   index = 11;
									arr[index] = new int[10];
									return true;
								} ); }
	public bool PosTest18() { return ExceptionTest(18, "NullReference (single dim)", typeof(NullReferenceException),
								delegate()
								{
									int[] arr = null;
									int   index = 2;
									arr[index] = 0;
									return true;
								} ); }
	public bool PosTest19() { return ExceptionTest(19, "NullReference (multi dim)", typeof(NullReferenceException),
								delegate()
								{
									int[,] arr = null;
									int   index = 2;
									arr[index,0] = 0;
									return true;
								} ); }
	public bool PosTest20() { return ExceptionTest(20, "NullReference (jagged)", typeof(NullReferenceException),
								delegate()
								{
									int[][] arr = new int[10][];
									int   index = 2;
									arr[index][0] = 0;
									return true;
								} ); }

	public bool DataMisalign(int id, bool getter)
	{
		bool     retVal = true;
		long     misAlignedField = 0;
		MyStruct m;

		TestLibrary.TestFramework.BeginScenario("PosTest"+id+": "+ (getter?"Get":"Set") +" misaligned field expect DataMisalignment Exception (IA64 only)");

		try
		{
			m = new MyStruct();

			if (getter)
			{
				misAlignedField = m.MisalignedField;
			}
			else
			{
				m.MisalignedField = c_VALUE;
			}

			if (IsIA64())
			{
				TestLibrary.TestFramework.LogError("002", "DataMisalignedException expected");
				retVal = false;
			}

			// need to get it to validate that it is right
			if (!getter) misAlignedField = m.MisalignedField;

			if (c_VALUE != misAlignedField)
			{
				TestLibrary.TestFramework.LogError("001", "Incorrect value: Expected("+c_VALUE+") Actual("+misAlignedField+")");
				retVal = false;
			}
		}
		catch (DataMisalignedException e)
		{
			// expected on IA64
			if (IsIA64())
			{
				TestLibrary.TestFramework.LogInformation("Catch DataMisalignedException as expected");
			}
			else
			{
				TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
				retVal = false;
			}
		}
		catch (Exception e)
		{
			TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
			retVal = false;
		}

		return retVal;
	}

	public bool ExceptionTest(int id, string msg, Type ehType, TestDelegate d)
	{
		bool     retVal = true;

		TestLibrary.TestFramework.BeginScenario("PosTest"+id+": " + msg);

		try
		{
			retVal = d();

			TestLibrary.TestFramework.LogError("10" + id, "Function should have thrown: " + ehType);
			retVal = false;
		}
		catch (Exception e)
		{
			if (ehType != e.GetType())
			{
				TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
				retVal = false;
			}
		}

		return retVal;
	}

	public bool IsIA64()
	{
            return false;
	}

	public volatile static int volatileReadWrite = 0;

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static bool GobbleStack()
	{
		#pragma warning disable 0168
		MyStruct s1;
		MyStruct s2;
		#pragma warning restore 0168

		return GobbleStack();

		#pragma warning disable 0162
		// avoid tail call optimizations
		volatileReadWrite++;
		#pragma warning restore 0162
	}
}

[StructLayout(LayoutKind.Explicit)]
public class MyStruct
{
	[FieldOffset(1)]
	public long MisalignedField = HardwareEh.c_VALUE;
}

#pragma warning disable 0169
public struct MyLargeStruct
{
	long l0,l1,l2,l3,l4,l5,l6,l7,l8,l9;
	long l10,l11,l12,l13,l14,l15,l16,l17,l18,l19;
	long l20,l21,l22,l23,l24,l25,l26,l27,l28,l29;
	long l30,l31,l32,l33,l34,l35,l36,l37,l38,l39;
	long l40,l41,l42,l43,l44,l45,l46,l47,l48,l49;
	double d0,d1,d2,d3,d4,d5,d6,d7,d8,d9;
	double d10,d11,d12,d13,d14,d15,d16,d17,d18,d19;
	double d20,d21,d22,d23,d24,d25,d26,d27,d28,d29;
	double d30,d31,d32,d33,d34,d35,d36,d37,d38,d39;
	double d40,d41,d42,d43,d44,d45,d46,d47,d48,d49;
}
#pragma warning restore 0169
