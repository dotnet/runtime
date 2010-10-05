//
// AdjustVisibilityStep.cs
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
using System.Collections.Generic;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class AdjustVisibility : BaseStep {

		static readonly object internalized_key = new object ();

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			ProcessTypes (assembly.MainModule.Types);
		}

		void ProcessTypes (ICollection types)
		{
			foreach (TypeDefinition type in types)
				ProcessType (type);
		}

		void ProcessType (TypeDefinition type)
		{
			if (!IsPublic (type))
				return;

			if (!IsMarkedAsPublic (type)) {
				SetInternalVisibility (type);
				return;
			}

			if (type.IsEnum)
				return;

			ProcessFields (type.Fields);
			ProcessMethods (type.Methods);
		}

		static bool IsPublic (TypeDefinition type)
		{
			return type.DeclaringType == null ? type.IsPublic : type.IsNestedPublic;
		}

		void SetInternalVisibility (TypeDefinition type)
		{
			type.Attributes &= ~TypeAttributes.VisibilityMask;
			if (type.DeclaringType == null)
				type.Attributes |= TypeAttributes.NotPublic;
			else
				type.Attributes |= TypeAttributes.NestedAssembly;

			MarkInternalized (type);
		}

		void ProcessMethods (ICollection methods)
		{
			foreach (MethodDefinition method in methods)
				ProcessMethod (method);
		}

		void ProcessMethod (MethodDefinition method)
		{
			if (IsMarkedAsPublic (method))
				return;

			if (method.IsPublic)
				SetInternalVisibility (method);
			else if (method.IsFamily || method.IsFamilyOrAssembly)
				SetProtectedAndInternalVisibility (method);
		}

		void SetInternalVisibility (MethodDefinition method)
		{
			method.Attributes &= ~MethodAttributes.MemberAccessMask;
			method.Attributes |= MethodAttributes.Assembly;

			MarkInternalized (method);
		}

		void SetProtectedAndInternalVisibility (MethodDefinition method)
		{
			method.Attributes &= ~MethodAttributes.MemberAccessMask;
			method.Attributes |= MethodAttributes.FamANDAssem;

			MarkInternalized (method);
		}

		bool IsMarkedAsPublic (IMetadataTokenProvider provider)
		{
			return Annotations.IsPublic (provider);
		}

		void ProcessFields (IEnumerable<FieldDefinition> fields)
		{
			foreach (FieldDefinition field in fields)
				ProcessField (field);
		}

		void ProcessField (FieldDefinition field)
		{
			if (IsMarkedAsPublic (field))
				return;

			if (field.IsPublic)
				SetInternalVisibility (field);
			else if (field.IsFamily || field.IsFamilyOrAssembly)
				SetProtectedAndInternalVisibility (field);
		}

		void SetInternalVisibility (FieldDefinition field)
		{
			field.Attributes &= ~FieldAttributes.FieldAccessMask;
			field.Attributes |= FieldAttributes.Assembly;

			MarkInternalized (field);
		}

		void SetProtectedAndInternalVisibility (FieldDefinition field)
		{
			field.Attributes &= ~FieldAttributes.FieldAccessMask;
			field.Attributes |= FieldAttributes.FamANDAssem;

			MarkInternalized (field);
		}

		void MarkInternalized (IMetadataTokenProvider provider)
		{
			TunerAnnotations.Internalized (Context, provider);
		}
	}
}
