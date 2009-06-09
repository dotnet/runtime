//
// MoonlightA11yProcessor.cs
//
// Author:
//   Andrés G. Aragoneses (aaragoneses@novell.com)
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

using Mono.Cecil;

using Mono.Linker;

namespace Mono.Tuner {
	
	public class MoonlightA11yProcessor : InjectSecurityAttributes {
		
		protected override bool ConditionToProcess ()
		{
			return true;
		}
		
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			_assembly = assembly;

			// remove existing [SecurityCritical] and [SecuritySafeCritical]
			RemoveSecurityAttributes ();

			// add [SecurityCritical]
			AddSecurityAttributes ();
			
			// convert all public members into internal
			MakeApiInternal ();
		}
		
		void MakeApiInternal ()
		{
			foreach (TypeDefinition type in _assembly.MainModule.Types) {
				if (type.IsPublic)
					type.IsPublic = false;

				if (type.HasConstructors)
					foreach (MethodDefinition ctor in type.Constructors)
						if (ctor.IsPublic)
							ctor.IsAssembly = true;

				if (type.HasMethods)
					foreach (MethodDefinition method in type.Methods)
						if (method.IsPublic)
							method.IsAssembly = true;
			}
		}
		
		void AddSecurityAttributes ()
		{
			foreach (TypeDefinition type in _assembly.MainModule.Types) {
				AddCriticalAttribute (type);

				if (type.HasConstructors)
					foreach (MethodDefinition ctor in type.Constructors)
						AddCriticalAttribute (ctor);

				if (type.HasMethods)
					foreach (MethodDefinition method in type.Methods)
						AddCriticalAttribute (method);
			}
		}

	}
}
