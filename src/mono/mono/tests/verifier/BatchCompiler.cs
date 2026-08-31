//
// AssemblyRunner.cs
//
// Author:
//   Rodrigo Kumpera (rkumpera@novell.com)
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Reflection;


namespace Verifier {
	public class BatchCompiler : MarshalByRefObject {
		static AppDomain current;

		static BatchCompiler NewBatchCompiler () {
			if (current != null)
				AppDomain.Unload (current);

			current = AppDomain.CreateDomain ("test");
			BatchCompiler compiler = (BatchCompiler) current.CreateInstanceAndUnwrap(
				Assembly.GetExecutingAssembly().FullName,
				"Verifier.BatchCompiler");
			return compiler;
		}

		public static void Main (String[] args) {
			int total = 0;
			BatchCompiler bc = NewBatchCompiler ();

			foreach (string src in Directory.GetFiles (".", "*.il")) {
				if (bc.Compile (src)) 
					++total;
				bc = NewBatchCompiler ();
			}
			Console.WriteLine ("Total compiled successfully {0}", total);
		}

		public bool Compile (String src) {
			try {
				Mono.ILASM.Driver.Main (new string[] { src });
				string binary = src.Substring (0, src.Length - 3) + ".exe";
				return File.Exists (binary);
			} catch (Exception e) {
				Console.WriteLine ("Error compiling {0}", e);
				return false;
			}
		}
	}
}

