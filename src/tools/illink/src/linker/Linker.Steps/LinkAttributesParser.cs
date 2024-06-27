// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class LinkAttributesParser : ProcessLinkerXmlBase
	{
		AttributeInfo? _attributeInfo;

		public LinkAttributesParser (LinkContext context, Stream documentStream, string xmlDocumentLocation)
			: base (context, documentStream, xmlDocumentLocation)
		{
		}

		public LinkAttributesParser (LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
			: base (context, documentStream, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

		public void Parse (AttributeInfo xmlInfo)
		{
			_attributeInfo = xmlInfo;
			bool stripLinkAttributes = _context.IsOptimizationEnabled (CodeOptimizations.RemoveLinkAttributes, _resource?.Assembly);
			ProcessXml (stripLinkAttributes, _context.IgnoreLinkAttributes);
		}

		static bool IsRemoveAttributeInstances (string attributeName) => attributeName == "RemoveAttributeInstances" || attributeName == "RemoveAttributeInstancesAttribute";

		(CustomAttribute[]? customAttributes, MessageOrigin[]? origins) ProcessAttributes (XPathNavigator nav, ICustomAttributeProvider provider)
		{
			ArrayBuilder<CustomAttribute> customAttributesBuilder = default;
			ArrayBuilder<MessageOrigin> originsBuilder = default;
			foreach (XPathNavigator attributeNav in nav.SelectChildren ("attribute", string.Empty)) {
				if (!ShouldProcessElement (attributeNav))
					continue;

				TypeDefinition? attributeType;
				string internalAttribute = GetAttribute (attributeNav, "internal");
				if (!string.IsNullOrEmpty (internalAttribute)) {
					if (!IsRemoveAttributeInstances (internalAttribute)) {
						LogWarning (attributeNav, DiagnosticId.UnrecognizedInternalAttribute, internalAttribute);
						continue;
					}
					if (provider is not TypeDefinition) {
						LogWarning (attributeNav, DiagnosticId.XmlRemoveAttributeInstancesCanOnlyBeUsedOnType, nameof (RemoveAttributeInstancesAttribute));
						continue;
					}

					attributeType = GenerateRemoveAttributeInstancesAttribute ();
					if (attributeType == null)
						continue;
				} else {
					string attributeFullName = GetFullName (attributeNav);
					if (string.IsNullOrEmpty (attributeFullName)) {
						LogWarning (attributeNav, DiagnosticId.XmlElementDoesNotContainRequiredAttributeFullname);
						continue;
					}

					if (!GetAttributeType (attributeNav, attributeFullName, out attributeType))
						continue;
				}

				CustomAttribute? customAttribute = CreateCustomAttribute (attributeNav, attributeType, provider);
				if (customAttribute != null) {
					_context.LogMessage ($"Assigning external custom attribute '{FormatCustomAttribute (customAttribute)}' instance to '{provider}'.");
					customAttributesBuilder.Add (customAttribute);
					originsBuilder.Add (GetMessageOriginForPosition (attributeNav));
				}
			}

			return (customAttributesBuilder.ToArray (), originsBuilder.ToArray ());

			static string FormatCustomAttribute (CustomAttribute ca)
			{
				StringBuilder sb = new StringBuilder ();
				sb.Append (ca.Constructor.GetDisplayName ());
				sb.Append (" { args: ");
				for (int i = 0; i < ca.ConstructorArguments.Count; ++i) {
					if (i > 0)
						sb.Append (", ");

					var caa = ca.ConstructorArguments[i];
					sb.Append ($"{caa.Type.GetDisplayName ()} {caa.Value}");
				}
				sb.Append (" }");

				return sb.ToString ();
			}
		}
		TypeDefinition? GenerateRemoveAttributeInstancesAttribute ()
		{
			TypeDefinition? td = null;

			if (_context.MarkedKnownMembers.RemoveAttributeInstancesAttributeDefinition is TypeDefinition knownTypeDef) {
				return knownTypeDef;
			}

			var voidType = BCL.FindPredefinedType (WellKnownType.System_Void, _context);
			if (voidType == null)
				return null;

			var attributeType = BCL.FindPredefinedType (WellKnownType.System_Attribute, _context);
			if (attributeType == null)
				return null;

			var objectType = BCL.FindPredefinedType (WellKnownType.System_Object, _context);
			if (objectType == null)
				return null;
			var objectArrayType = new ArrayType (objectType);
			if (objectArrayType == null)
				return null;

			//
			// Generates metadata information for internal type
			//
			// public sealed class RemoveAttributeInstancesAttribute : Attribute
			// {
			//  public RemoveAttributeInstancesAttribute () {}
			//  public RemoveAttributeInstancesAttribute (object values) {} // For legacy uses
			//  public RemoveAttributeInstancesAttribute (params object[] values) {}
			// }
			//
			const MethodAttributes ctorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Final;

			td = new TypeDefinition ("", nameof (RemoveAttributeInstancesAttribute), TypeAttributes.Public);
			td.BaseType = attributeType;

			var ctor = new MethodDefinition (".ctor", ctorAttributes, voidType);
			td.Methods.Add (ctor);
			var ctor1 = new MethodDefinition (".ctor", ctorAttributes, voidType);
			var param = new ParameterDefinition (objectType);
			td.Methods.Add (ctor1);

			var ctorN = new MethodDefinition (".ctor", ctorAttributes, voidType);
			var paramN = new ParameterDefinition (objectArrayType);
#pragma warning disable RS0030 // MethodReference.Parameters is banned. It's necessary to build the method definition here, though.
			ctorN.Parameters.Add (paramN);
#pragma warning restore RS0030
			td.Methods.Add (ctorN);

			return _context.MarkedKnownMembers.RemoveAttributeInstancesAttributeDefinition = td;
		}

		CustomAttribute? CreateCustomAttribute (XPathNavigator nav, TypeDefinition attributeType, ICustomAttributeProvider provider)
		{
			CustomAttributeArgument[] arguments = ReadCustomAttributeArguments (nav, provider);

			MethodDefinition? constructor = FindBestMatchingConstructor (attributeType, arguments);
			if (constructor == null) {
				LogWarning (nav, DiagnosticId.XmlCouldNotFindMatchingConstructorForCustomAttribute, attributeType.GetDisplayName ());
				return null;
			}

			CustomAttribute customAttribute = new CustomAttribute (constructor);
			foreach (var argument in arguments)
				customAttribute.ConstructorArguments.Add (argument);

			ReadCustomAttributeProperties (nav, attributeType, customAttribute);

			return customAttribute;
		}

		MethodDefinition? FindBestMatchingConstructor (TypeDefinition attributeType, CustomAttributeArgument[] args)
		{
			var methods = attributeType.Methods;
			for (int i = 0; i < attributeType.Methods.Count; ++i) {
				var method = methods[i];
				if (!method.IsInstanceConstructor ())
					continue;

				if (args.Length != method.GetMetadataParametersCount ())
					continue;

				bool match = true;
				foreach (var p in method.GetMetadataParameters ()) {
					//
					// No candidates betterness, only exact matches are supported
					//
					var parameterType = _context.TryResolve (p.ParameterType);
					if (parameterType == null || parameterType != _context.TryResolve (args[p.MetadataIndex].Type))
						match = false;
				}

				if (match)
					return method;
			}

			return null;
		}

		void ReadCustomAttributeProperties (XPathNavigator nav, TypeDefinition attributeType, CustomAttribute customAttribute)
		{
			foreach (XPathNavigator propertyNav in nav.SelectChildren ("property", string.Empty)) {
				string propertyName = GetName (propertyNav);
				if (string.IsNullOrEmpty (propertyName)) {
					LogWarning (propertyNav, DiagnosticId.XmlPropertyDoesNotContainAttributeName);
					continue;
				}

				PropertyDefinition? property = attributeType.Properties.Where (prop => prop.Name == propertyName).FirstOrDefault ();
				if (property == null) {
					LogWarning (propertyNav, DiagnosticId.XmlCouldNotFindProperty, propertyName);
					continue;
				}

				var caa = ReadCustomAttributeArgument (propertyNav, property);
				if (caa is null)
					continue;

				customAttribute.Properties.Add (new CustomAttributeNamedArgument (property.Name, caa.Value));
			}
		}

		CustomAttributeArgument[] ReadCustomAttributeArguments (XPathNavigator nav, ICustomAttributeProvider provider)
		{
			ArrayBuilder<CustomAttributeArgument> args = default;

			foreach (XPathNavigator argumentNav in nav.SelectChildren ("argument", string.Empty)) {
				CustomAttributeArgument? caa = ReadCustomAttributeArgument (argumentNav, provider);
				if (caa is not null)
					args.Add (caa.Value);
			}

			return args.ToArray () ?? Array.Empty<CustomAttributeArgument> ();
		}

		CustomAttributeArgument? ReadCustomAttributeArgument (XPathNavigator nav, ICustomAttributeProvider provider)
		{
			TypeReference? typeref = ResolveArgumentType (nav, provider);
			if (typeref is null)
				return null;

			string svalue = nav.Value;

			//
			// Builds CustomAttributeArgument in the same way as it would be
			// represented in the metadata if encoded there. This simplifies
			// any custom attributes handling in ILLink by using same attributes
			// value extraction or mathing logic.
			//
			switch (typeref.MetadataType) {
			case MetadataType.Object:
				var argumentIterator = nav.SelectChildren ("argument", string.Empty);
				if (argumentIterator?.MoveNext () != true) {
					_context.LogError (null, DiagnosticId.CustomAttributeArgumentForTypeRequiresNestedNode, "System.Object", "argument");
					return null;
				}

				var typedef = _context.TryResolve (typeref);
				if (typedef == null)
					return null;

				var boxedValue = ReadCustomAttributeArgument (argumentIterator.Current!, typedef);
				if (boxedValue is null)
					return null;

				return new CustomAttributeArgument (typeref, boxedValue);

			case MetadataType.Char:
			case MetadataType.Byte:
			case MetadataType.SByte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.UInt64:
			case MetadataType.Int64:
			case MetadataType.String:
				return new CustomAttributeArgument (typeref, ConvertStringValue (svalue, typeref));

			case MetadataType.ValueType:
				var enumType = _context.Resolve (typeref);
				if (enumType?.IsEnum != true)
					goto default;

				var enumField = enumType.Fields.Where (f => f.IsStatic && f.Name == svalue).FirstOrDefault ();
				object evalue = enumField?.Constant ?? svalue;

				typeref = enumType.GetEnumUnderlyingType ();
				return new CustomAttributeArgument (enumType, ConvertStringValue (evalue, typeref));

			case MetadataType.Class:
				if (!typeref.IsTypeOf (WellKnownType.System_Type))
					goto default;

				var diagnosticContext = new DiagnosticContext (new MessageOrigin (provider), diagnosticsEnabled: true, _context);
				if (!_context.TypeNameResolver.TryResolveTypeName (svalue, diagnosticContext, out TypeReference? type, out _, needsAssemblyName: false)) {
					_context.LogError (GetMessageOriginForPosition (nav), DiagnosticId.CouldNotResolveCustomAttributeTypeValue, svalue);
					return null;
				}

				return new CustomAttributeArgument (typeref, type);
			case MetadataType.Array:
				if (typeref is ArrayType arrayTypeRef) {
					var elementType = arrayTypeRef.ElementType;
					var arrayArgumentIterator = nav.SelectChildren ("argument", string.Empty);
					ArrayBuilder<CustomAttributeArgument> elements = default;
					foreach (XPathNavigator elementNav in arrayArgumentIterator) {
						if (ReadCustomAttributeArgument (elementNav, provider) is CustomAttributeArgument arg) {
							// To match Cecil, elements of a list that are subclasses of the list type must be boxed in the base type
							// e.g. object[] { 73 } translates to Cecil.CAA { Type: object[] : Value: CAA{ Type: object, Value: CAA{ Type: int, Value: 73} } }
							if (arg.Type == elementType) {
								elements.Add (arg);
							}
							// This check allows the xml to be less verbose by allowing subtypes to not be boxed in the Array's element type
							// e.g. here string doesn't need to be boxed in an "object" argument
							// <argument type="System.Object[]">
							//   <argument type="System.String">hello</argument>
							// </argument>
							//
							else if (arg.Type.IsSubclassOf (elementType.Namespace, elementType.Name, _context)) {
								elements.Add (new CustomAttributeArgument (elementType, arg));
							} else {
								_context.LogError (GetMessageOriginForPosition (nav), DiagnosticId.UnexpectedAttributeArgumentType, typeref.GetDisplayName ());
							}
						} else {
							return null;
						}
					}
					return new CustomAttributeArgument (arrayTypeRef, elements.ToArray ());
				}
				goto default;
			default:
				// No support for null, consider adding - dotnet/linker/issues/1957
				_context.LogError (GetMessageOriginForPosition (nav), DiagnosticId.UnexpectedAttributeArgumentType, typeref.GetDisplayName ());
				return null;
			}

			TypeReference? ResolveArgumentType (XPathNavigator nav, ICustomAttributeProvider provider)
			{
				string typeName = GetAttribute (nav, "type");
				if (string.IsNullOrEmpty (typeName))
					typeName = "System.String";

				var diagnosticContext = new DiagnosticContext (new MessageOrigin (provider), diagnosticsEnabled: true, _context);
				if (!_context.TypeNameResolver.TryResolveTypeName (typeName, diagnosticContext, out TypeReference? typeref, out _, needsAssemblyName: false)) {
					_context.LogError (GetMessageOriginForPosition (nav), DiagnosticId.TypeUsedWithAttributeValueCouldNotBeFound, typeName, nav.Value);
					return null;
				}

				return typeref;
			}
		}

		object? ConvertStringValue (object value, TypeReference targetType)
		{
			TypeCode typeCode;
			switch (targetType.MetadataType) {
			case MetadataType.String:
				typeCode = TypeCode.String;
				break;
			case MetadataType.Char:
				typeCode = TypeCode.Char;
				break;
			case MetadataType.Byte:
				typeCode = TypeCode.Byte;
				break;
			case MetadataType.SByte:
				typeCode = TypeCode.SByte;
				break;
			case MetadataType.Int16:
				typeCode = TypeCode.Int16;
				break;
			case MetadataType.UInt16:
				typeCode = TypeCode.UInt16;
				break;
			case MetadataType.Int32:
				typeCode = TypeCode.Int32;
				break;
			case MetadataType.UInt32:
				typeCode = TypeCode.UInt32;
				break;
			case MetadataType.UInt64:
				typeCode = TypeCode.UInt64;
				break;
			case MetadataType.Int64:
				typeCode = TypeCode.Int64;
				break;
			case MetadataType.Boolean:
				typeCode = TypeCode.Boolean;
				break;
			case MetadataType.Single:
				typeCode = TypeCode.Single;
				break;
			case MetadataType.Double:
				typeCode = TypeCode.Double;
				break;
			default:
				throw new NotSupportedException (targetType.ToString ());
			}

			try {
				return Convert.ChangeType (value, typeCode);
			} catch {
				_context.LogError (null, DiagnosticId.CannotConverValueToType, value.ToString () ?? "", targetType.GetDisplayName ());
				return null;
			}
		}

		bool GetAttributeType (XPathNavigator nav, string attributeFullName, [NotNullWhen (true)] out TypeDefinition? attributeType)
		{
			string assemblyName = GetAttribute (nav, "assembly");
			if (string.IsNullOrEmpty (assemblyName)) {
				attributeType = _context.GetType (attributeFullName);
			} else {
				AssemblyDefinition? assembly;
				try {
					assembly = _context.TryResolve (AssemblyNameReference.Parse (assemblyName));
					if (assembly == null) {
						LogWarning (nav, DiagnosticId.XmlCouldNotResolveAssemblyForAttribute, assemblyName, attributeFullName);

						attributeType = default;
						return false;
					}
				} catch (Exception) {
					LogWarning (nav, DiagnosticId.XmlCouldNotResolveAssemblyForAttribute, assemblyName, attributeFullName);
					attributeType = default;
					return false;
				}

				attributeType = _context.TryResolve (assembly, attributeFullName);
			}

			if (attributeType == null) {
				LogWarning (nav, DiagnosticId.XmlAttributeTypeCouldNotBeFound, attributeFullName);
				return false;
			}

			return true;
		}

		protected override AllowedAssemblies AllowedAssemblySelector {
			get {
				if (_resource?.Assembly == null)
					return AllowedAssemblies.AllAssemblies;

				// Corelib XML may contain assembly wildcard to support compiler-injected attribute types
				if (_resource?.Assembly.Name.Name == _context.SystemModuleName)
					return AllowedAssemblies.AllAssemblies;

				return AllowedAssemblies.ContainingAssembly;
			}
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			PopulateAttributeInfo (assembly, nav);
			ProcessTypes (assembly, nav, warnOnUnresolvedTypes);
		}

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));

			PopulateAttributeInfo (type, nav);
			ProcessTypeChildren (type, nav);

			if (!type.HasNestedTypes)
				return;

			foreach (XPathNavigator nestedTypeNav in nav.SelectChildren ("type", string.Empty)) {
				foreach (TypeDefinition nested in type.NestedTypes) {
					if (nested.Name == GetAttribute (nestedTypeNav, "name") && ShouldProcessElement (nestedTypeNav))
						ProcessType (nested, nestedTypeNav);
				}
			}
		}

		protected override void ProcessField (TypeDefinition type, FieldDefinition field, XPathNavigator nav)
		{
			PopulateAttributeInfo (field, nav);
		}

		protected override void ProcessMethod (TypeDefinition type, MethodDefinition method, XPathNavigator nav, object? customData)
		{
			PopulateAttributeInfo (method, nav);
			ProcessReturnParameters (method, nav);
			ProcessParameters (method, nav);
		}

		void ProcessParameters (MethodDefinition method, XPathNavigator nav)
		{
			Debug.Assert (_attributeInfo != null);
			foreach (XPathNavigator parameterNav in nav.SelectChildren ("parameter", string.Empty)) {
				var (attributes, origins) = ProcessAttributes (parameterNav, method);
				if (attributes != null && origins != null) {
					string paramName = GetAttribute (parameterNav, "name");
#pragma warning disable RS0030 // MethodReference.Parameters is banned. It's easiest to leave existing code as is
					foreach (ParameterDefinition parameter in method.Parameters) {
						if (paramName == parameter.Name) {
							if (parameter.HasCustomAttributes || _attributeInfo.CustomAttributes.ContainsKey (parameter))
								LogWarning (parameterNav, DiagnosticId.XmlMoreThanOneValueForParameterOfMethod, paramName, method.GetDisplayName ());
							_attributeInfo.AddCustomAttributes (parameter, attributes, origins);
							break;
						}
					}
#pragma warning restore RS0030
				}
			}
		}

		void ProcessReturnParameters (MethodDefinition method, XPathNavigator nav)
		{
			Debug.Assert (_attributeInfo != null);
			bool firstAppearance = true;
			foreach (XPathNavigator returnNav in nav.SelectChildren ("return", string.Empty)) {
				if (firstAppearance) {
					firstAppearance = false;
					var (attributes, origins) = ProcessAttributes (returnNav, method);
					if (attributes != null && origins != null) {
						_attributeInfo.AddCustomAttributes (method.MethodReturnType, attributes, origins);
					}
				} else {
					LogWarning (returnNav, DiagnosticId.XmlMoreThanOneReturnElementForMethod, method.GetDisplayName ());
				}
			}
		}

		protected override MethodDefinition? GetMethod (TypeDefinition type, string signature)
		{
			if (type.HasMethods)
				foreach (MethodDefinition method in type.Methods)
					if (signature.Replace (" ", "") == GetMethodSignature (method) || signature.Replace (" ", "") == GetMethodSignature (method, true))
						return method;

			return null;
		}

#pragma warning disable RS0030 // MethdReference.Parameters is banned. It's easiest to leave existing code as is.
		static string GetMethodSignature (MethodDefinition method, bool includeReturnType = false)
		{
			StringBuilder sb = new StringBuilder ();
			if (includeReturnType) {
				sb.Append (method.ReturnType.FullName);
			}
			sb.Append (method.Name);
			if (method.HasGenericParameters) {
				sb.Append ('<');
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					if (i > 0)
						sb.Append (',');

					sb.Append (method.GenericParameters[i].Name);
				}
				sb.Append ('>');
			}
			sb.Append ('(');
			if (method.HasMetadataParameters ()) {
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (i > 0)
						sb.Append (',');

					sb.Append (method.Parameters[i].ParameterType.FullName);
				}
			}
			sb.Append (')');
			return sb.ToString ();
		}
#pragma warning restore RS0030

		protected override void ProcessProperty (TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object? customData, bool fromSignature)
		{
			PopulateAttributeInfo (property, nav);
		}

		protected override void ProcessEvent (TypeDefinition type, EventDefinition @event, XPathNavigator nav, object? customData)
		{
			PopulateAttributeInfo (@event, nav);
		}

		void PopulateAttributeInfo (ICustomAttributeProvider provider, XPathNavigator nav)
		{
			Debug.Assert (_attributeInfo != null);
			var (attributes, origins) = ProcessAttributes (nav, provider);
			if (attributes != null && origins != null)
				_attributeInfo.AddCustomAttributes (provider, attributes, origins);
		}
	}
}
