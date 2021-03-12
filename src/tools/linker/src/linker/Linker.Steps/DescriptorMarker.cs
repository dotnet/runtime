// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using System.Xml.XPath;

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class DescriptorMarker : ProcessLinkerXmlBase
	{
		const string NamespaceElementName = "namespace";

		const string _required = "required";
		const string _preserve = "preserve";
		const string _accessors = "accessors";

		static readonly string[] _accessorsAll = new string[] { "all" };
		static readonly char[] _accessorsSep = new char[] { ';' };

		public DescriptorMarker (LinkContext context, XPathDocument document, string xmlDocumentLocation)
			: base (context, document, xmlDocumentLocation)
		{
		}

		public DescriptorMarker (LinkContext context, XPathDocument document, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
			: base (context, document, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

		public void Mark ()
		{
			bool stripDescriptors = _context.IsOptimizationEnabled (CodeOptimizations.RemoveDescriptors, _resourceAssembly);
			ProcessXml (stripDescriptors, _context.IgnoreDescriptors);
		}

		protected override AllowedAssemblies AllowedAssemblySelector { get => AllowedAssemblies.AnyAssembly; }

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			if (GetTypePreserve (nav) == TypePreserve.All) {
				foreach (var type in assembly.MainModule.Types)
					MarkAndPreserveAll (type);
			} else {
				ProcessTypes (assembly, nav, warnOnUnresolvedTypes);
				ProcessNamespaces (assembly, nav);
			}
		}

		void ProcessNamespaces (AssemblyDefinition assembly, XPathNavigator nav)
		{
			var iterator = nav.SelectChildren (NamespaceElementName, XmlNamespace);
			while (iterator.MoveNext ()) {
				if (!ShouldProcessElement (iterator.Current))
					continue;

				string fullname = GetFullName (iterator.Current);
				bool foundMatch = false;
				foreach (TypeDefinition type in assembly.MainModule.Types) {
					if (type.Namespace != fullname)
						continue;

					foundMatch = true;
					MarkAndPreserveAll (type);
				}

				if (!foundMatch) {
					_context.LogWarning ($"Could not find any type in namespace '{fullname}'", 2044, _xmlDocumentLocation);
				}
			}
		}

		void MarkAndPreserveAll (TypeDefinition type)
		{
			_context.Annotations.Mark (type, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
			_context.Annotations.SetPreserve (type, TypePreserve.All);

			if (!type.HasNestedTypes)
				return;

			foreach (TypeDefinition nested in type.NestedTypes)
				MarkAndPreserveAll (nested);
		}

		protected override TypeDefinition ProcessExportedType (ExportedType exported, AssemblyDefinition assembly)
		{
			_context.MarkingHelpers.MarkExportedType (exported, assembly.MainModule, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
			return base.ProcessExportedType (exported, assembly);
		}

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));

			TypePreserve preserve = GetTypePreserve (nav);
			if (preserve != TypePreserve.Nothing)
				_context.Annotations.SetPreserve (type, preserve);

			bool required = IsRequired (nav);
			ProcessTypeChildren (type, nav, required);

			if (!required)
				return;

			_context.Annotations.Mark (type, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));

			if (type.IsNested) {
				var currentType = type;
				while (currentType.IsNested) {
					var parent = currentType.DeclaringType;
					_context.Annotations.Mark (parent, new DependencyInfo (DependencyKind.DeclaringType, currentType));
					currentType = parent;
				}
			}
		}

		static TypePreserve GetTypePreserve (XPathNavigator nav)
		{
			string attribute = GetAttribute (nav, _preserve);
			if (string.IsNullOrEmpty (attribute))
				return nav.HasChildren ? TypePreserve.Nothing : TypePreserve.All;

			if (Enum.TryParse (attribute, true, out TypePreserve result))
				return result;
			return TypePreserve.Nothing;
		}

		protected override void ProcessField (TypeDefinition type, FieldDefinition field, XPathNavigator nav)
		{
			if (_context.Annotations.IsMarked (field))
				_context.LogWarning ($"Duplicate preserve of '{field.FullName}'", 2025, _xmlDocumentLocation);

			_context.Annotations.Mark (field, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
		}

		protected override void ProcessMethod (TypeDefinition type, MethodDefinition method, XPathNavigator nav, object customData)
		{
			if (_context.Annotations.IsMarked (method))
				_context.LogWarning ($"Duplicate preserve of '{method.GetDisplayName ()}'", 2025, _xmlDocumentLocation);

			_context.Annotations.MarkIndirectlyCalledMethod (method);
			_context.Annotations.SetAction (method, MethodAction.Parse);

			if (!(bool) customData) {
				_context.Annotations.AddPreservedMethod (type, method);
			} else {
				_context.Annotations.Mark (method, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
			}
		}

		void ProcessMethodIfNotNull (TypeDefinition type, MethodDefinition method, object customData)
		{
			if (method == null)
				return;

			ProcessMethod (type, method, null, customData);
		}

		protected override MethodDefinition GetMethod (TypeDefinition type, string signature)
		{
			if (type.HasMethods)
				foreach (MethodDefinition meth in type.Methods)
					if (signature == GetMethodSignature (meth, false))
						return meth;

			return null;
		}

		public static string GetMethodSignature (MethodDefinition meth, bool includeGenericParameters)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (meth.ReturnType.FullName);
			sb.Append (" ");
			sb.Append (meth.Name);
			if (includeGenericParameters && meth.HasGenericParameters) {
				sb.Append ("`");
				sb.Append (meth.GenericParameters.Count);
			}

			sb.Append ("(");
			if (meth.HasParameters) {
				for (int i = 0; i < meth.Parameters.Count; i++) {
					if (i > 0)
						sb.Append (",");

					sb.Append (meth.Parameters[i].ParameterType.FullName);
				}
			}
			sb.Append (")");
			return sb.ToString ();
		}

		protected override void ProcessEvent (TypeDefinition type, EventDefinition @event, XPathNavigator nav, object customData)
		{
			if (_context.Annotations.IsMarked (@event))
				_context.LogWarning ($"Duplicate preserve of '{@event.FullName}'", 2025, _xmlDocumentLocation);

			ProcessMethod (type, @event.AddMethod, null, customData);
			ProcessMethod (type, @event.RemoveMethod, null, customData);
			ProcessMethodIfNotNull (type, @event.InvokeMethod, customData);
		}

		protected override void ProcessProperty (TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object customData, bool fromSignature)
		{
			string[] accessors = fromSignature ? GetAccessors (nav) : _accessorsAll;

			if (_context.Annotations.IsMarked (property))
				_context.LogWarning ($"Duplicate preserve of '{property.FullName}'", 2025, _xmlDocumentLocation);

			ProcessPropertyAccessors (type, property, accessors, customData);
		}

		void ProcessPropertyAccessors (TypeDefinition type, PropertyDefinition property, string[] accessors, object customData)
		{
			if (Array.IndexOf (accessors, "all") >= 0) {
				ProcessMethodIfNotNull (type, property.GetMethod, customData);
				ProcessMethodIfNotNull (type, property.SetMethod, customData);
				return;
			}

			if (property.GetMethod != null && Array.IndexOf (accessors, "get") >= 0)
				ProcessMethod (type, property.GetMethod, null, customData);
			else if (property.GetMethod == null)
				_context.LogWarning ($"Could not find the get accessor of property '{property.Name}' on type '{type.FullName}'", 2018, _xmlDocumentLocation);

			if (property.SetMethod != null && Array.IndexOf (accessors, "set") >= 0)
				ProcessMethod (type, property.SetMethod, null, customData);
			else if (property.SetMethod == null)
				_context.LogWarning ($"Could not find the set accessor of property '{property.Name}' in type '{type.FullName}' specified in {_xmlDocumentLocation}", 2019, _xmlDocumentLocation);
		}

		static bool IsRequired (XPathNavigator nav)
		{
			string attribute = GetAttribute (nav, _required);
			if (attribute == null || attribute.Length == 0)
				return true;

			return bool.TryParse (attribute, out bool result) && result;
		}

		protected static string[] GetAccessors (XPathNavigator nav)
		{
			string accessorsValue = GetAttribute (nav, _accessors);

			if (accessorsValue != null) {
				string[] accessors = accessorsValue.Split (
					_accessorsSep, StringSplitOptions.RemoveEmptyEntries);

				if (accessors.Length > 0) {
					for (int i = 0; i < accessors.Length; ++i)
						accessors[i] = accessors[i].ToLower ();

					return accessors;
				}
			}
			return _accessorsAll;
		}
	}
}
