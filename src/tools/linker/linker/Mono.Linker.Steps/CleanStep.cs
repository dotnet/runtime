//
// CleanStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

using System.Collections;

using Mono.Cecil;

namespace Mono.Linker.Steps {

	public class CleanStep : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) == AssemblyAction.Link)
				CleanAssembly (assembly);
		}

		static void CleanAssembly (AssemblyDefinition asm)
		{
			CleanMemberReferences (asm.MainModule);
			foreach (TypeDefinition type in asm.MainModule.Types)
				CleanType (type);
		}

		static void CleanMemberReferences (ModuleDefinition module)
		{
			foreach (MemberReference reference in new ArrayList (module.MemberReferences)) {
				GenericInstanceType git = reference.DeclaringType as GenericInstanceType;
				if (git == null)
					continue;

				foreach (TypeReference arg in git.GenericArguments)
					if (!CheckType (module, arg))
						module.MemberReferences.Remove (reference);
			}
		}

		static bool CheckType (ModuleDefinition module, TypeReference reference)
		{
			TypeSpecification spec = reference as TypeSpecification;
			if (spec != null)
				return CheckType (module, spec.ElementType);

			TypeDefinition type = reference as TypeDefinition;
			if (type == null)
				return true;

			return module.Types.Contains (type);
		}

		static void CleanType (TypeDefinition type)
		{
			CleanNestedTypes (type);
			CleanProperties (type);
			CleanEvents (type);
		}

		static void CleanNestedTypes (TypeDefinition type)
		{
			foreach (TypeDefinition nested in new ArrayList (type.NestedTypes))
				if (!type.Module.Types.Contains (nested))
					type.NestedTypes.Remove (nested);
		}

		static MethodDefinition CheckMethod (TypeDefinition type, MethodDefinition method)
		{
			if (method == null)
				return null;

			return type.Methods.Contains (method) ? method : null;
		}

		static void CleanEvents (TypeDefinition type)
		{
			foreach (EventDefinition evt in new ArrayList (type.Events)) {
				evt.AddMethod = CheckMethod (type, evt.AddMethod);
				evt.InvokeMethod = CheckMethod (type, evt.InvokeMethod);
				evt.RemoveMethod = CheckMethod (type, evt.RemoveMethod);

				if (!IsEventUsed (evt))
					type.Events.Remove (evt);
			}
		}

		static bool IsEventUsed (EventDefinition evt)
		{
			return evt.AddMethod != null || evt.InvokeMethod != null || evt.RemoveMethod != null;
		}

		static void CleanProperties (TypeDefinition type)
		{
			foreach (PropertyDefinition prop in new ArrayList (type.Properties)) {
				prop.GetMethod = CheckMethod (type, prop.GetMethod);
				prop.SetMethod = CheckMethod (type, prop.SetMethod);

				if (!IsPropertyUsed (prop))
					type.Properties.Remove (prop);
			}
		}

		static bool IsPropertyUsed (PropertyDefinition prop)
		{
			return prop.GetMethod != null || prop.SetMethod != null;
		}
	}
}
