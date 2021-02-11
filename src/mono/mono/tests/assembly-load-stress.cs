using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.IO;

public class Tests
{
	static int nloops = 1;
	static int nthreads = 10;

	public static void Main (String[] args) {
		if (args.Length > 0)
			nloops = int.Parse (args [0]);
		if (args.Length > 1)
			nthreads = int.Parse (args [1]);

		for (int li = 0; li < nloops; ++li) {
			Thread[] threads = new Thread [nthreads];
			for (int i = 0; i < nthreads; ++i) {
				threads [i] = new Thread (delegate () {
						foreach (string s in Directory.GetFiles ("/usr/local/lib/mono/4.5", "*.dll")) {
							AssemblyName.GetAssemblyName (s);
						}
					});
			}
			for (int i = 0; i < 10; ++i)
				threads [i].Start ();
		}
	}
}
