//
// FilterAttributes.cs
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

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class FilterAttributes : BaseStep {

		static Hashtable attributes = new Hashtable ();

		static FilterAttributes ()
		{
			FilterAttribute ("System.Runtime.InteropServices.ComVisibleAttribute");
		}

		static void FilterAttribute (string fullname)
		{
			attributes.Add (fullname, null);
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			Filter (assembly);

			foreach (ModuleDefinition module in assembly.Modules)
				ProcessModule (module);
		}

		static void ProcessModule (ModuleDefinition module)
		{
			Filter (module);

			foreach (TypeDefinition type in module.Types)
				ProcessType (type);
		}

		static void ProcessType (TypeDefinition type)
		{
			if (type.HasFields)
				ProcessFields (type.Fields);

			if (type.HasMethods)
				ProcessMethods (type.Methods);

			if (type.HasEvents)
				ProcessEvents (type.Events);

			if (type.HasProperties)
				ProcessProperties (type.Properties);

			ProcessGenericParameters (type);
		}

		static void ProcessFields (ICollection fields)
		{
			foreach (FieldDefinition field in fields)
				Filter (field);
		}

		static void ProcessMethods (ICollection methods)
		{
			foreach (MethodDefinition method in methods)
				ProcessMethod (method);
		}

		static void ProcessMethod (MethodDefinition method)
		{
			ProcessGenericParameters (method);

			Filter (method.MethodReturnType);

			if (method.HasParameters)
				ProcessParameters (method.Parameters);
		}

		static void ProcessParameters (ICollection parameters)
		{
			foreach (ParameterDefinition parameter in parameters)
				Filter (parameter);
		}

		static void ProcessGenericParameters (IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			foreach (GenericParameter parameter in provider.GenericParameters)
				Filter (parameter);
		}

		static void ProcessEvents (ICollection events)
		{
			foreach (EventDefinition @event in events)
				Filter (@event);
		}

		static void ProcessProperties (ICollection properties)
		{
			foreach (PropertyDefinition property in properties)
				Filter (property);
		}

		static void Filter (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			for (int i = 0; i < provider.CustomAttributes.Count; i++) {
				CustomAttribute attribute = provider.CustomAttributes [i];
				if (!IsFilteredAttribute (attribute))
					continue;

				provider.CustomAttributes.RemoveAt (i--);
			}
		}

		static bool IsFilteredAttribute (CustomAttribute attribute)
		{
			return attributes.Contains (attribute.Constructor.DeclaringType.FullName);
		}
	}
}
