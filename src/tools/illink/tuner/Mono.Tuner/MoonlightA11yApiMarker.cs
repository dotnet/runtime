//
// MoonlightA11yApiMarker.cs
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
using System.Collections;
using System.Xml;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class MoonlightA11yApiMarker : MarkStep {

		bool IsA11yAssembly (AssemblyDefinition assembly)
		{
			return assembly.ToString ().Contains ("DummyEntry") || assembly.ToString ().Contains ("MoonAtkBridge");
		}

		protected override void InitializeAssembly (AssemblyDefinition assembly)
		{
			if (IsA11yAssembly (assembly))
				base.InitializeAssembly (assembly);
		}

		protected override void EnqueueMethod (MethodDefinition method)
		{
			if (IsA11yAssembly (method.DeclaringType.Module.Assembly))
				base.EnqueueMethod (method);
			else
				Annotations.Mark (method);
		}

		protected override bool IgnoreScope (IMetadataScope scope)
		{
			return false;
		}

		protected override TypeDefinition MarkType (TypeReference reference)
		{
			if (reference == null)
				throw new ArgumentNullException ("reference");

			reference = GetOriginalType (reference);

			if (reference is GenericParameter)
				return null;

			TypeDefinition type = reference.Resolve ();

			if (type == null)
				throw new ResolutionException (reference);

			if (CheckProcessed (type))
				return type;

			Annotations.Mark (type);
			return type;
		}
	}
}
