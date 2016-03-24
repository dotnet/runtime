// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*	==========================================================	*\
	Fibonacci
	Fibonacci.Compute		WaitDelegate
	Fibonacci.Check			WaitOrTimerDelegate
	Copyright (c) Microsoft, 1999-2000
\*	==========================================================	*/
using System;
using System.Threading;
using System.Security;

#if WINCORESYS
[assembly: AllowPartiallyTrustedCallers]
#endif
namespace ThreadPool_Test {
	public class Fibonacci {
		private	int		fibnumber;
		public	int		iFlag;
		private int fib(int x)		{return ((x<=1)?1:(fib(x-1)+fib(x-2)));}
		public Fibonacci(int num)	{fibnumber=num;iFlag=0;}
		public void SetNumber(int n){fibnumber=n;}

		//	WaitDelegate
		public void Compute(Object o) {
			int		x	= fib(fibnumber);
			Console.WriteLine("Fibonacci.Compute({0})={1}",fibnumber,x);
		}

		//	WaitOrTimerDelegate
		public void Check(Object o, bool zTimex) {
			iFlag=1;
			Console.WriteLine("Fibonacci, {0}",((zTimex)?"timer expired":"wait signalled"));
			if (!zTimex)
				Compute(o);
		}
	}

	public class Fibonacci2 {
		private int fib(int x)		{return ((x<=1)?1:(fib(x-1)+fib(x-2)));}
		public Fibonacci2( )	{Console.WriteLine("Fibonacci2..ctor(int)");}

		//	WaitDelegate
		public void Compute(Object o) {
			SubFib	sfLocal		= (SubFib) o;
			sfLocal.iRet	= fib(sfLocal.iFibon);
			Console.Write("({0})  ",sfLocal.iFibon);
			Console.WriteLine("Fibonacci2.Compute({0})={1}",sfLocal.iFibon,sfLocal.iRet);
		}

		//	WaitOrTimerDelegate
		public void Check(Object o, bool zTimex) {
			SubFib	sfLocal		= (SubFib) o;
			sfLocal.iCheckFlag=1;
			Console.Write("({0})  ",sfLocal.iFibon);
//			iFlag=1;
			Console.WriteLine("Fibonacci2, {0}",((zTimex)?"timer expired":"wait signalled"));
			if (!zTimex)
				Compute(o);
		}
	}

	public class SubFib {
		public int	iFibon;
		public int	iCheckFlag;
		public int	iRet;
		public SubFib(int num)	{iFibon=num;iCheckFlag=0;Console.WriteLine("SubFib..ctor({0})",num);}
	}
}

