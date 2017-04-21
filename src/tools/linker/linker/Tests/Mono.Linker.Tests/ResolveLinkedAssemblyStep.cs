using Mono.Linker.Steps;
using Mono.Cecil;
using System.Linq;
using Mono.Collections.Generic;

namespace Mono.Linker.Tests
{
	class ResolveLinkedAssemblyStep : ResolveFromAssemblyStep
	{
		string testCase;

		public ResolveLinkedAssemblyStep (string testCase, string assembly)
			: base (assembly)
		{
			this.testCase = testCase;
		}

		protected override void ProcessLibrary (AssemblyDefinition assembly)
		{
			SetAction (Context, assembly, AssemblyAction.Link);

			Annotations.Push (assembly);

			foreach (TypeDefinition type in assembly.MainModule.Types)
				MarkType (type);

			Annotations.Pop ();
		}

		static bool MarkMember (ICustomAttributeProvider member)
		{
			if (!member.HasCustomAttributes)
				return false;

			return member.CustomAttributes.Any (l => l.AttributeType.FullName == "TestCases.MarkAttribute");
		}

		void MarkType (TypeDefinition type)
		{
			if (type.Namespace != testCase)
				return;

			if (MarkMember (type))
				Annotations.Mark (type);

			Annotations.Push (type);

			if (type.HasFields)
				MarkFields (type.Fields);
			if (type.HasMethods)
				MarkMethods (type.Methods);
			if (type.HasNestedTypes)
				foreach (var nested in type.NestedTypes)
					MarkType (nested);

			Annotations.Pop ();
		}

		void MarkFields (Collection<FieldDefinition> fields)
		{
			foreach (var field in fields) {
				if (MarkMember (field)) {
					Annotations.Mark (field.DeclaringType);
					Annotations.Mark (field);
				}
			}
		}

		void MarkMethods (Collection<MethodDefinition> methods)
		{
			foreach (MethodDefinition method in methods) {
				if (MarkMember(method)) {
					Annotations.Mark (method.DeclaringType);
					Annotations.Mark (method);
					Annotations.SetAction (method, MethodAction.Parse);
				}
			}
		}
	}
}
