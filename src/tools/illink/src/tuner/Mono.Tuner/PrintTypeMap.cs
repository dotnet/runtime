//
// PrintTypeMap.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class PrintTypeMap : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			foreach (TypeDefinition type in assembly.MainModule.GetAllTypes ())
				PrintMap (type);
		}

		void PrintMap (TypeDefinition type)
		{
			if (!type.HasMethods)
				return;

			Console.WriteLine ("Type {0} map", type);

			foreach (MethodDefinition method in type.Methods) {
				if (!method.IsVirtual)
					continue;

				Console.WriteLine ("  Method {0} map", method);

				IEnumerable<MethodDefinition> overrides = Annotations.GetOverrides (method);
				foreach (var @override in overrides ?? new MethodDefinition [0])
					Console.WriteLine ("    HasOverride {0}", @override);

				IEnumerable<MethodDefinition> bases = Annotations.GetBaseMethods (method);
				foreach (var @base in bases ?? new MethodDefinition [0])
					Console.WriteLine ("    Base {0}", @base);
			}
		}
	}
}
