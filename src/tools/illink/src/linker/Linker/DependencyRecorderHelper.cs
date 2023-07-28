// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;


namespace Mono.Linker
{
	/// <summary>
	/// Class which implements IDependencyRecorder and writes the dependencies into an DGML file.
	/// </summary>
	public class DependencyRecorderHelper
	{
		static bool IsAssemblyBound (TypeDefinition td)
		{
			do {
				if (td.IsNestedPrivate || td.IsNestedAssembly || td.IsNestedFamilyAndAssembly)
					return true;

				td = td.DeclaringType;
			} while (td != null);

			return false;
		}

		public static string TokenString (LinkContext context, object? o)
		{
			if (o == null)
				return "N:null";

			if (o is TypeReference t) {
				bool addAssembly = true;
				var td = context.TryResolve (t);

				if (td != null) {
					addAssembly = td.IsNotPublic || IsAssemblyBound (td);
					t = td;
				}

				var addition = addAssembly ? $":{t.Module}" : "";

				return $"{((IMetadataTokenProvider) o).MetadataToken.TokenType}:{o}{addition}";
			}

			if (o is IMetadataTokenProvider provider)
				return provider.MetadataToken.TokenType + ":" + o;

			return "Other:" + o;
		}

		static bool WillAssemblyBeModified (LinkContext context, AssemblyDefinition assembly)
		{
			switch (context.Annotations.GetAction (assembly)) {
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
				return true;
			default:
				return false;
			}
		}

		public static bool ShouldRecord (LinkContext context, object? source, object target)
		{
			if (source == null || target == null)
				return false;

			// We use a few hacks to work around MarkStep outputting thousands of edges even
			// with the above ShouldRecord checks. Ideally we would format these into a meaningful format
			// however I don't think that is worth the effort at the moment.

			// Prevent useless logging of attributes like `e="Other:Mono.Cecil.CustomAttribute"`.
			if (source is CustomAttribute || target is CustomAttribute)
				return false;

			// Prevent useless logging of interface implementations like `e="InterfaceImpl:Mono.Cecil.InterfaceImplementation"`.
			if (source is InterfaceImplementation || target is InterfaceImplementation)
				return false;

			if (!ShouldRecord (context, source) && !ShouldRecord (context, target)) {
				return false;
			}
			return true;
		}

		public static bool ShouldRecord (LinkContext context, object? o)
		{
			if (!context.EnableReducedTracing)
				return true;

			if (o is TypeDefinition t)
				return WillAssemblyBeModified (context, t.Module.Assembly);

			if (o is IMemberDefinition m)
				return WillAssemblyBeModified (context, m.DeclaringType.Module.Assembly);

			if (o is TypeReference typeRef) {
				var resolved = context.TryResolve (typeRef);

				// Err on the side of caution if we can't resolve
				if (resolved == null)
					return true;

				return WillAssemblyBeModified (context, resolved.Module.Assembly);
			}

			if (o is MemberReference mRef) {
				var resolved = mRef.Resolve ();

				// Err on the side of caution if we can't resolve
				if (resolved == null)
					return true;

				return WillAssemblyBeModified (context, resolved.DeclaringType.Module.Assembly);
			}

			if (o is ModuleDefinition module)
				return WillAssemblyBeModified (context, module.Assembly);

			if (o is AssemblyDefinition assembly)
				return WillAssemblyBeModified (context, assembly);

			if (o is ParameterDefinition parameter) {
				if (parameter.Method is MethodDefinition parameterMethodDefinition)
					return WillAssemblyBeModified (context, parameterMethodDefinition.DeclaringType.Module.Assembly);
			}

			return true;
		}
	}
}
