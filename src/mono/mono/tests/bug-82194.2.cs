using System;
using System.Collections.Generic;
using System.Reflection;

namespace Runner
{
	public class ObjectContainer<T> where T : class, new () {}
	public class DocumentObject : ObjectContainer<DomainObject> {}
	public class DomainObject : ObjectContainer<DomainObject> {}

	class Program
	{
		[STAThread]
		static int Main (string[] args)
		{
			Type [] ts = typeof(Program).Assembly.GetTypes ();

			foreach (Type t in ts)
				Console.WriteLine (t);

			return 0;
		}
	}
}
