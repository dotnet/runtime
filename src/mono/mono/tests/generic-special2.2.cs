using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Threading;

internal class GenericType<T> {
	[ThreadStatic]
	internal static object static_var;

	public static void AccessStaticVar ()
	{
		if (static_var != null && static_var.GetType () != typeof (List<T>))
			throw new Exception ("Corrupted static var");
		GenericType<T>.static_var = new List<T> ();
	}
}

public static class Program {
	private static bool stress;

	/* Create a lot of static vars */
	private static void CreateVTables ()
	{
		Type[] nullArgs = new Type[0];
		Assembly ass = Assembly.GetAssembly (typeof (int));
		foreach (Type type in ass.GetTypes ()) {
			try {
				Type inst = typeof (GenericType<>).MakeGenericType (type);
				Activator.CreateInstance (inst);
			} catch {
			}
		}
	}

	private static void StressStaticFieldAddr ()
	{
		while (stress) {
			GenericType<object>.AccessStaticVar ();
		}
	}

	public static void Main (string[] args)
	{
		Thread thread = new Thread (StressStaticFieldAddr);

		stress = true;
		thread.Start ();
		CreateVTables ();
		stress = false;

		thread.Join ();
	}
}
