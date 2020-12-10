//
// ResolveFromXmlStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
// (C) 2007 Novell, Inc.
// Copyright 2013 Xamarin Inc.
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
using System.Diagnostics;
using System.Text;
using System.Xml.XPath;

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ResolveFromXmlStep : ProcessLinkerXmlStepBase
	{
		const string NamespaceElementName = "namespace";

		const string _required = "required";
		const string _preserve = "preserve";
		const string _accessors = "accessors";

		static readonly string[] _accessorsAll = new string[] { "all" };
		static readonly char[] _accessorsSep = new char[] { ';' };

		public ResolveFromXmlStep (XPathDocument document, string xmlDocumentLocation)
			: base (document, xmlDocumentLocation)
		{
		}

		public ResolveFromXmlStep (XPathDocument document, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
			: base (document, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

#if !FEATURE_ILLINK
		protected override bool ShouldProcessElement (XPathNavigator nav) => true;
#endif

		protected override void Process ()
		{
			ProcessXml (Context.StripDescriptors, Context.IgnoreDescriptors);
		}

		protected override AllowedAssemblies AllowedAssemblySelector { get => AllowedAssemblies.AnyAssembly; }

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
#if !FEATURE_ILLINK
			if (IsExcluded (nav))
				return;
#endif

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
					Context.LogWarning ($"Could not find any type in namespace '{fullname}'", 2044, _xmlDocumentLocation);
				}
			}
		}

		void MarkAndPreserveAll (TypeDefinition type)
		{
			Annotations.Mark (type, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
			Annotations.SetPreserve (type, TypePreserve.All);

			if (!type.HasNestedTypes)
				return;

			foreach (TypeDefinition nested in type.NestedTypes)
				MarkAndPreserveAll (nested);
		}

		protected override TypeDefinition ProcessExportedType (ExportedType exported, AssemblyDefinition assembly)
		{
			MarkingHelpers.MarkExportedType (exported, assembly.MainModule, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
			return base.ProcessExportedType (exported, assembly);
		}

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));

#if !FEATURE_ILLINK
			if (IsExcluded (nav))
				return;
#endif

			TypePreserve preserve = GetTypePreserve (nav);
			if (preserve != TypePreserve.Nothing)
				Annotations.SetPreserve (type, preserve);

			bool required = IsRequired (nav);
			ProcessTypeChildren (type, nav, required);

			if (!required)
				return;

			if (Annotations.IsMarked (type)) {
				var duplicateLevel = preserve != TypePreserve.Nothing ? preserve : nav.HasChildren ? TypePreserve.Nothing : TypePreserve.All;
			}

			Annotations.Mark (type, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));

			if (type.IsNested) {
				var currentType = type;
				while (currentType.IsNested) {
					var parent = currentType.DeclaringType;
					Context.Annotations.Mark (parent, new DependencyInfo (DependencyKind.DeclaringType, currentType));
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

#if !FEATURE_ILLINK
		protected override void ProcessField (TypeDefinition type, XPathNavigator nav)
		{
			if (IsExcluded (nav))
				return;

			base.ProcessField (type, nav);
		}
#endif

		protected override void ProcessField (TypeDefinition type, FieldDefinition field, XPathNavigator nav)
		{
			if (Annotations.IsMarked (field))
				Context.LogWarning ($"Duplicate preserve of '{field.FullName}'", 2025, _xmlDocumentLocation);

			Context.Annotations.Mark (field, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
		}

#if !FEATURE_ILLINK
		protected override void ProcessMethod (TypeDefinition type, XPathNavigator nav, object customData)
		{
			if (IsExcluded (nav))
				return;

			base.ProcessMethod (type, nav, customData);
		}
#endif

		protected override void ProcessMethod (TypeDefinition type, MethodDefinition method, XPathNavigator nav, object customData)
		{
			if (Annotations.IsMarked (method))
				Context.LogWarning ($"Duplicate preserve of '{method.GetDisplayName ()}'", 2025, _xmlDocumentLocation);

			Annotations.Mark (method, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));
			Annotations.MarkIndirectlyCalledMethod (method);
			Annotations.SetAction (method, MethodAction.Parse);

			if (!(bool) customData)
				Annotations.AddPreservedMethod (type, method);
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

#if !FEATURE_ILLINK
		protected override void ProcessEvent (TypeDefinition type, XPathNavigator nav, object customData)
		{
			if (IsExcluded (nav))
				return;

			base.ProcessEvent (type, nav, customData);
		}
#endif

		protected override void ProcessEvent (TypeDefinition type, EventDefinition @event, XPathNavigator nav, object customData)
		{
			if (Annotations.IsMarked (@event))
				Context.LogWarning ($"Duplicate preserve of '{@event.FullName}'", 2025, _xmlDocumentLocation);

			Annotations.Mark (@event, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));

			ProcessMethod (type, @event.AddMethod, null, customData);
			ProcessMethod (type, @event.RemoveMethod, null, customData);
			ProcessMethodIfNotNull (type, @event.InvokeMethod, customData);
		}

#if !FEATURE_ILLINK
		protected override void ProcessProperty (TypeDefinition type, XPathNavigator nav, object customData)
		{
			if (IsExcluded (nav))
				return;

			base.ProcessProperty (type, nav, customData);
		}
#endif

		protected override void ProcessProperty (TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object customData, bool fromSignature)
		{
			string[] accessors = fromSignature ? GetAccessors (nav) : _accessorsAll;

			if (Annotations.IsMarked (property))
				Context.LogWarning ($"Duplicate preserve of '{property.FullName}'", 2025, _xmlDocumentLocation);

			Annotations.Mark (property, new DependencyInfo (DependencyKind.XmlDescriptor, _xmlDocumentLocation));

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
				Context.LogWarning ($"Could not find the get accessor of property '{property.Name}' on type '{type.FullName}'", 2018, _xmlDocumentLocation);

			if (property.SetMethod != null && Array.IndexOf (accessors, "set") >= 0)
				ProcessMethod (type, property.SetMethod, null, customData);
			else if (property.SetMethod == null)
				Context.LogWarning ($"Could not find the set accessor of property '{property.Name}' in type '{type.FullName}' specified in {_xmlDocumentLocation}", 2019, _xmlDocumentLocation);
		}

		protected override AssemblyDefinition GetAssembly (LinkContext context, AssemblyNameReference assemblyName)
		{
			var assembly = context.Resolve (assemblyName);
			if (assembly != null)
				ProcessReferences (assembly);

			return assembly;
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

#if !FEATURE_ILLINK
		protected virtual bool IsExcluded (XPathNavigator nav)
		{
			var value = GetAttribute (nav, "feature");
			if (string.IsNullOrEmpty (value))
				return false;

			return Context.IsFeatureExcluded (value);
		}
#endif
	}
}
