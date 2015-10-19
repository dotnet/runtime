/*	==========================================================	*\
	Class:    Foo
	Copyright (c) Microsoft, 1999
\*	==========================================================	*/
using System;
using System.Threading;

namespace ThreadPool_Test {
	public class Foo {
		private ManualResetEvent	e;
		public Foo(ManualResetEvent ev)	{e = ev;}
		public void f2(Object o)		{Console.WriteLine("f2");}
		public void f3(Object o)		{Console.WriteLine("f3");}
		public void f4(Object o)		{Console.WriteLine("f4");}
		public void f1(Object o)		{Console.WriteLine("f1");e.Set( );}
	}
}
