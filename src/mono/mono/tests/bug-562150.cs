
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Application
{
	public sealed class FooClass<S> where S : class
	{
		public static FooClass<S> Create(string name, Action<S> action)
		{
			return new FooClass<S>(name, action);
		}

		public static FooClass<S> Create<T>(S source, string name,
			Func<S, T> func, FooClass<T> fooChild)
			where T : class
		{
			return new FooClass<S>(source, name, CreateCallback(func, fooChild));
		}

		private static Action<S> CreateCallback<T>(
			Func<S, T> func, FooClass<T> fooChild)
			where T : class
		{
			return delegate(S source)
			{
			};
		}

		private FooClass(string name, Action<S> action)
		{
			m_name = name;
			m_action = action;
		}

		private FooClass(S source, string name, Action<S> action)
			: this(name, action)
		{
			m_source = source;
		}

		S m_source;
		readonly string m_name;
		Action<S> m_action;
	}

	public class MyClass
	{
		public int Value { get; set; }
		public MyClass Child { get; set; }
	}

	public class VarCompilerTest
	{

		public static void Main(string[] args)
		{
			MyClass obj = new MyClass();
			int nCalls = 0;
			FooClass<MyClass> fooChild = FooClass<MyClass>.Create("Value", delegate { nCalls++; });
			FooClass<MyClass>.Create(obj, "Child", x => x.Child, fooChild);
		}
	}
}
