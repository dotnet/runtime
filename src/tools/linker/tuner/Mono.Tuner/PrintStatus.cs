//
// PrintStatus.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
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

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class PrintStatus : BaseStep {

		static string display_internalized = "display_internalized";

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			Console.WriteLine ("Assembly `{0}' ({1}) tuned", assembly.Name, assembly.MainModule.FullyQualifiedName);

			if (!DisplayInternalized ())
				return;

			foreach (TypeDefinition type in assembly.MainModule.Types)
				ProcessType (type);
		}

		bool DisplayInternalized ()
		{
			try {
				return bool.Parse (Context.GetParameter (display_internalized));
			} catch {
				return false;
			}
		}

		void ProcessType (TypeDefinition type)
		{
			ProcessCollection (type.Fields);
			ProcessCollection (type.Methods);
		}

		void ProcessCollection (ICollection collection)
		{
			foreach (IMetadataTokenProvider provider in collection)
				ProcessProvider (provider);
		}

		void ProcessProvider (IMetadataTokenProvider provider)
		{
			if (!TunerAnnotations.IsInternalized (Context, provider))
				return;

			Console.WriteLine ("[internalized] {0}", provider);
		}
	}
}
