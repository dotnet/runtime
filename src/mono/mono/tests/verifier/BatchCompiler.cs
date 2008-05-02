//
// AssemblyRunner.cs
//
// Author:
//   Rodrigo Kumpera (rkumpera@novell.com)
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
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

