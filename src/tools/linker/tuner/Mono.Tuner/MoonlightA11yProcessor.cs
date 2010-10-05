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
using System.Linq;

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

				if (type.HasMethods && !type.Name.EndsWith ("Adapter"))
					foreach (MethodDefinition ctor in type.Methods.Where (m => m.IsConstructor))
						if (ctor.IsPublic)
							ctor.IsAssembly = true;

				if (type.HasMethods)
					foreach (MethodDefinition method in type.Methods.Where (m => !m.IsConstructor))
						if (method.IsPublic)
							method.IsAssembly = true;
			}
		}

		void AddSecurityAttributes ()
		{
			foreach (TypeDefinition type in _assembly.MainModule.Types) {
				AddCriticalAttribute (type);

				if (type.HasMethods)
					foreach (MethodDefinition ctor in type.Methods.Where (m => m.IsConstructor))
						AddCriticalAttribute (ctor);

				if (type.HasMethods)
					foreach (MethodDefinition method in type.Methods.Where (m => !m.IsConstructor)) {
						MethodDefinition parent = null;

						//TODO: take in account generic params
						if (!method.HasGenericParameters) {

							/*
							 * we need to scan base methods because the CoreCLR complains about SC attribs added
							 * to overriden methods whose base (virtual or interface) method is not marked as SC
							 * with TypeLoadExceptions
							 */
							parent = GetBaseMethod (type, method);
						}

						//if there's no base method
						if (parent == null ||

						//if it's our bridge assembly, we're sure it will (finally, at the end of the linking process) have the SC attrib
						    _assembly.MainModule.Types.Contains (parent.DeclaringType) ||

						//if the type is in the moonlight assemblies, check if it has the SC attrib
						    HasSecurityAttribute (parent, AttributeType.Critical))

							AddCriticalAttribute (method);
				}

			}
		}

		MethodDefinition GetBaseMethod (TypeDefinition finalType, MethodDefinition final)
		{
			// both GetOverridenMethod and GetInterfaceMethod return null if there is no base method
			return GetOverridenMethod (finalType, final) ?? GetInterfaceMethod (finalType, final);
		}

		//note: will not return abstract methods
		MethodDefinition GetOverridenMethod (TypeDefinition finalType, MethodDefinition final)
		{
			TypeReference baseType = finalType.BaseType;
			while (baseType != null && baseType.Resolve () != null) {
				foreach (MethodDefinition method in baseType.Resolve ().Methods) {
					if (!method.IsVirtual || method.Name != final.Name)
						continue;

					//TODO: should we discard them?
					if (method.IsAbstract)
						continue;

					if (HasSameSignature (method, final))
						return method;
				}
				baseType = baseType.Resolve().BaseType;
			}
			return null;
		}

		MethodDefinition GetInterfaceMethod (TypeDefinition finalType, MethodDefinition final)
		{
			TypeDefinition baseType = finalType;
			while (baseType != null) {
				if (baseType.HasInterfaces)
					foreach (TypeReference @interface in baseType.Interfaces)
						foreach (MethodDefinition method in @interface.Resolve ().Methods)
							if (method.Name == final.Name && HasSameSignature (method, final))
								return method;

				baseType = baseType.BaseType == null ? null : baseType.BaseType.Resolve ();
			}
			return null;
		}

		bool HasSameSignature (MethodDefinition method1, MethodDefinition method2)
		{
			if (method1.ReturnType.FullName != method2.ReturnType.FullName)
				return false;

			if (method1.Parameters.Count != method2.Parameters.Count)
				return false;

			for (int i = 0; i < method1.Parameters.Count; i++) {
				if (method1.Parameters [i].ParameterType.FullName !=
				    method2.Parameters [i].ParameterType.FullName)
					return false;
			}

			return true;
		}
	}
}
