//
// MarkStep.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps {

	public class MarkStep : IStep {

		protected LinkContext _context;
		protected Queue<MethodDefinition> _methods;
		protected List<MethodDefinition> _virtual_methods;
		protected Dictionary<TypeDefinition, CustomAttribute> _assemblyDebuggerDisplayAttributes;
		protected Dictionary<TypeDefinition, CustomAttribute> _assemblyDebuggerTypeProxyAttributes;

		public AnnotationStore Annotations {
			get { return _context.Annotations; }
		}

		public MarkStep ()
		{
			_methods = new Queue<MethodDefinition> ();
			_virtual_methods = new List<MethodDefinition> ();

			_assemblyDebuggerDisplayAttributes = new Dictionary<TypeDefinition, CustomAttribute> ();
			_assemblyDebuggerTypeProxyAttributes = new Dictionary<TypeDefinition, CustomAttribute> ();
		}

		public virtual void Process (LinkContext context)
		{
			_context = context;

			Initialize ();
			Process ();
		}

		void Initialize ()
		{
			foreach (AssemblyDefinition assembly in _context.GetAssemblies ())
				InitializeAssembly (assembly);
		}

		protected virtual void InitializeAssembly (AssemblyDefinition assembly)
		{
			MarkAssembly (assembly);

			foreach (TypeDefinition type in assembly.MainModule.Types)
				InitializeType (type);
		}

		void InitializeType (TypeDefinition type)
		{
			if (type.HasNestedTypes) {
				foreach (var nested in type.NestedTypes)
					InitializeType (nested);
			}

			if (!Annotations.IsMarked (type))
				return;

			MarkType (type);

			if (type.HasFields)
				InitializeFields (type);
			if (type.HasMethods)
				InitializeMethods (type.Methods);
		}

		void InitializeFields (TypeDefinition type)
		{
			foreach (FieldDefinition field in type.Fields)
				if (Annotations.IsMarked (field))
					MarkField (field);
		}

		void InitializeMethods (Collection<MethodDefinition> methods)
		{
			foreach (MethodDefinition method in methods)
				if (Annotations.IsMarked (method))
					EnqueueMethod (method);
		}

		void Process ()
		{
			if (QueueIsEmpty ())
				throw new InvalidOperationException ("No entry methods");

			while (!QueueIsEmpty ()) {
				ProcessQueue ();
				ProcessVirtualMethods ();
				DoAdditionalProcessing ();
			}

			// deal with [TypeForwardedTo] pseudo-attributes
			foreach (AssemblyDefinition assembly in _context.GetAssemblies ()) {
				if (!assembly.MainModule.HasExportedTypes)
					continue;

				foreach (var exported in assembly.MainModule.ExportedTypes) {
					bool isForwarder = exported.IsForwarder;
					var declaringType = exported.DeclaringType;
					while (!isForwarder && (declaringType != null)) {
						isForwarder = declaringType.IsForwarder;
						declaringType = declaringType.DeclaringType;
					}

					if (!isForwarder)
						continue;
					TypeDefinition type = null;
					try {
						type = exported.Resolve ();
					}
					catch (AssemblyResolutionException) {
						continue;
					}
					if (!Annotations.IsMarked (type))
						continue;
					Annotations.Mark (exported);
					if (_context.KeepTypeForwarderOnlyAssemblies) {
						Annotations.Mark (assembly.MainModule);
					}
				}
			}
		}

		void ProcessQueue ()
		{
			while (!QueueIsEmpty ()) {
				MethodDefinition method = _methods.Dequeue ();
				Annotations.Push (method);
				try {
					ProcessMethod (method);
				} catch (Exception e) {
					throw new MarkException (string.Format ("Error processing method: '{0}' in assembly: '{1}'", method.FullName, method.Module.Name), e, method);
				} finally {
					Annotations.Pop ();
				}
			}
		}

		bool QueueIsEmpty ()
		{
			return _methods.Count == 0;
		}

		protected virtual void EnqueueMethod (MethodDefinition method)
		{
			_methods.Enqueue (method);
		}

		void ProcessVirtualMethods ()
		{
			foreach (MethodDefinition method in _virtual_methods) {
				Annotations.Push (method);
				ProcessVirtualMethod (method);
				Annotations.Pop ();
			}
		}

		void ProcessVirtualMethod (MethodDefinition method)
		{
			var overrides = Annotations.GetOverrides (method);
			if (overrides == null)
				return;

			foreach (MethodDefinition @override in overrides)
				ProcessOverride (@override);
		}

		void ProcessOverride (MethodDefinition method)
		{
			if (!Annotations.IsMarked (method.DeclaringType))
				return;

			if (Annotations.IsProcessed (method))
				return;

			if (Annotations.IsMarked (method))
				return;

			MarkMethod (method);
			ProcessVirtualMethod (method);
		}

		void MarkMarshalSpec (IMarshalInfoProvider spec)
		{
			if (!spec.HasMarshalInfo)
				return;

			var marshaler = spec.MarshalInfo as CustomMarshalInfo;
			if (marshaler == null)
				return;

			MarkType (marshaler.ManagedType);
		}

		void MarkCustomAttributes (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			foreach (CustomAttribute ca in provider.CustomAttributes)
				MarkCustomAttribute (ca);
		}

		protected virtual void MarkCustomAttribute (CustomAttribute ca)
		{
			Annotations.Push (ca);
			try {
				MarkMethod (ca.Constructor);

				MarkCustomAttributeArguments (ca);

				TypeReference constructor_type = ca.Constructor.DeclaringType;
				TypeDefinition type = constructor_type.Resolve ();

				if (type == null) {
					HandleUnresolvedType (constructor_type);
					return;
				}

				MarkCustomAttributeProperties (ca, type);
				MarkCustomAttributeFields (ca, type);
			} finally {
				Annotations.Pop ();
			}
		}

		protected void MarkSecurityDeclarations (ISecurityDeclarationProvider provider)
		{
			// most security declarations are removed (if linked) but user code might still have some
			// and if the attribtues references types then they need to be marked too
			if ((provider == null) || !provider.HasSecurityDeclarations)
				return;

			foreach (var sd in provider.SecurityDeclarations)
				MarkSecurityDeclaration (sd);
		}

		protected virtual void MarkSecurityDeclaration (SecurityDeclaration sd)
		{
			if (!sd.HasSecurityAttributes)
				return;
			
			foreach (var sa in sd.SecurityAttributes)
				MarkSecurityAttribute (sa);
		}

		protected virtual void MarkSecurityAttribute (SecurityAttribute sa)
		{
			TypeReference security_type = sa.AttributeType;
			TypeDefinition type = security_type.Resolve ();
			if (type == null)
				throw new ResolutionException (security_type);
			
			MarkType (security_type);
			MarkSecurityAttributeProperties (sa, type);
			MarkSecurityAttributeFields (sa, type);
		}

		protected void MarkSecurityAttributeProperties (SecurityAttribute sa, TypeDefinition attribute)
		{
			if (!sa.HasProperties)
				return;

			foreach (var named_argument in sa.Properties)
				MarkCustomAttributeProperty (named_argument, attribute);
		}

		protected void MarkSecurityAttributeFields (SecurityAttribute sa, TypeDefinition attribute)
		{
			if (!sa.HasFields)
				return;

			foreach (var named_argument in sa.Fields)
				MarkCustomAttributeField (named_argument, attribute);
		}

		protected void MarkCustomAttributeProperties (CustomAttribute ca, TypeDefinition attribute)
		{
			if (!ca.HasProperties)
				return;

			foreach (var named_argument in ca.Properties)
				MarkCustomAttributeProperty (named_argument, attribute);
		}

		protected void MarkCustomAttributeProperty (CustomAttributeNamedArgument namedArgument, TypeDefinition attribute)
		{
			PropertyDefinition property = GetProperty (attribute, namedArgument.Name);
			Annotations.Push (property);
			if (property != null)
				MarkMethod (property.SetMethod);

			MarkIfType (namedArgument.Argument);
			Annotations.Pop ();
		}

		PropertyDefinition GetProperty (TypeDefinition type, string propertyname)
		{
			while (type != null) {
				PropertyDefinition property = type.Properties.FirstOrDefault (p => p.Name == propertyname);
				if (property != null)
					return property;

				type = type.BaseType != null ? ResolveTypeDefinition (type.BaseType) : null;
			}

			return null;
		}

		protected void MarkCustomAttributeFields (CustomAttribute ca, TypeDefinition attribute)
		{
			if (!ca.HasFields)
				return;

			foreach (var named_argument in ca.Fields)
				MarkCustomAttributeField (named_argument, attribute);
		}

		protected void MarkCustomAttributeField (CustomAttributeNamedArgument namedArgument, TypeDefinition attribute)
		{
			FieldDefinition field = GetField (attribute, namedArgument.Name);
			if (field != null)
				MarkField (field);

			MarkIfType (namedArgument.Argument);
		}

		FieldDefinition GetField (TypeDefinition type, string fieldname)
		{
			while (type != null) {
				FieldDefinition field = type.Fields.FirstOrDefault (f => f.Name == fieldname);
				if (field != null)
					return field;

				type = type.BaseType != null ? ResolveTypeDefinition (type.BaseType) : null;
			}

			return null;
		}

		MethodDefinition GetMethodWithNoParameters (TypeDefinition type, string methodname)
		{
			while (type != null) {
				MethodDefinition method = type.Methods.FirstOrDefault (m => m.Name == methodname && !m.HasParameters);
				if (method != null)
					return method;

				type = type.BaseType != null ? ResolveTypeDefinition (type.BaseType) : null;
			}

			return null;
		}

		void MarkCustomAttributeArguments (CustomAttribute ca)
		{
			if (!ca.HasConstructorArguments)
				return;

			foreach (var argument in ca.ConstructorArguments)
				MarkIfType (argument);
		}

		void MarkIfType (CustomAttributeArgument argument)
		{
			var at = argument.Type;
			if (at.IsArray) {
				var et = at.GetElementType ();
				if (et.Namespace != "System" || et.Name != "Type")
					return;

				MarkType (et);
				if (argument.Value == null)
					return;

				foreach (var cac in (CustomAttributeArgument[]) argument.Value)
					MarkWithResolvedScope ((TypeReference) cac.Value);
			} else if (at.Namespace == "System" && at.Name == "Type") {
				MarkType (argument.Type);
				MarkWithResolvedScope ((TypeReference) argument.Value);
			}
		}

		// custom attributes encoding means it's possible to have a scope that will point into a PCL facade
		// even if we (just before saving) will resolve all type references (bug #26752)
		void MarkWithResolvedScope (TypeReference type)
		{
			if (type == null)
				return;

			// a GenericInstanceType can could contains generic arguments with scope that
			// needs to be updated out of the PCL facade (bug #28823)
			var git = (type as GenericInstanceType);
			if ((git != null) && git.HasGenericArguments) {
				foreach (var ga in git.GenericArguments)
					MarkWithResolvedScope (ga);
			}
			// we cannot set the Scope of a TypeSpecification but it's element type can be set
			// e.g. System.String[] -> System.String
			var ts = (type as TypeSpecification);
			if (ts != null) {
				MarkWithResolvedScope (ts.ElementType);
				return;
			}

			var td = type.Resolve ();
			if (td != null)
				type.Scope = td.Scope;
			MarkType (type);
		}

		protected bool CheckProcessed (IMetadataTokenProvider provider)
		{
			if (Annotations.IsProcessed (provider))
				return true;

			Annotations.Processed (provider);
			return false;
		}

		protected void MarkAssembly (AssemblyDefinition assembly)
		{
			if (CheckProcessed (assembly))
				return;

			ProcessModule (assembly);

			MarkAssemblySpecialCustomAttributes (assembly);

			MarkCustomAttributes (assembly);

			MarkSecurityDeclarations (assembly);

			foreach (ModuleDefinition module in assembly.Modules)
				MarkCustomAttributes (module);
		}

		void ProcessModule (AssemblyDefinition assembly)
		{
			// Pre-mark <Module> if there is any methods as they need to be executed 
			// at assembly load time
			foreach (TypeDefinition type in assembly.MainModule.Types)
			{
				if (type.Name == "<Module>" && type.HasMethods)
				{
					MarkType (type);
					break;
				}
			}
		}

		protected void MarkField (FieldReference reference)
		{
//			if (IgnoreScope (reference.DeclaringType.Scope))
//				return;

			if (reference.DeclaringType is GenericInstanceType)
				MarkType (reference.DeclaringType);

			FieldDefinition field = ResolveFieldDefinition (reference);

			if (field == null)
				throw new ResolutionException (reference);

			if (CheckProcessed (field))
				return;

			MarkType (field.DeclaringType);
			MarkType (field.FieldType);
			MarkCustomAttributes (field);
			MarkMarshalSpec (field);

			Annotations.Mark (field);
		}

		protected virtual bool IgnoreScope (IMetadataScope scope)
		{
			AssemblyDefinition assembly = ResolveAssembly (scope);
			return Annotations.GetAction (assembly) != AssemblyAction.Link;
		}

		FieldDefinition ResolveFieldDefinition (FieldReference field)
		{
			FieldDefinition fd = field as FieldDefinition;
			if (fd == null)
				fd = field.Resolve ();

			return fd;
		}

		void MarkScope (IMetadataScope scope)
		{
			var provider = scope as IMetadataTokenProvider;
			if (provider == null)
				return;

			Annotations.Mark (provider);
		}

		protected virtual void MarkSerializable (TypeDefinition type)
		{
			MarkDefaultConstructor (type);
			MarkMethodsIf (type.Methods, IsSpecialSerializationConstructor);
		}

		protected virtual TypeDefinition MarkType (TypeReference reference)
		{
			if (reference == null)
				return null;

			reference = GetOriginalType (reference);

			if (reference is FunctionPointerType)
				return null;

			if (reference is GenericParameter)
				return null;

//			if (IgnoreScope (reference.Scope))
//				return null;

			TypeDefinition type = ResolveTypeDefinition (reference);

			if (type == null) {
				HandleUnresolvedType (reference);
				return null;
			}

			if (CheckProcessed (type))
				return null;

			Annotations.Push (type);

			MarkScope (type.Scope);
			MarkType (type.BaseType);
			MarkType (type.DeclaringType);
			MarkCustomAttributes (type);
			MarkSecurityDeclarations (type);

			if (IsMulticastDelegate (type)) {
				MarkMethodCollection (type.Methods);
			}

			if (IsSerializable (type))
				MarkSerializable (type);

			if (IsEventSource (type)) {
				MarkEventSource (type);
			}

			MarkTypeSpecialCustomAttributes (type);

			MarkGenericParameterProvider (type);

			// keep fields for value-types and for classes with LayoutKind.Sequential or Explicit
			if (type.IsValueType || !type.IsAutoLayout)
				MarkFields (type, type.IsEnum);

			if (type.HasInterfaces) {
				foreach (var iface in type.Interfaces)
					MarkType (iface.InterfaceType);
			}

			if (type.HasMethods) {
				MarkMethodsIf (type.Methods, IsVirtualAndHasPreservedParent);
				MarkMethodsIf (type.Methods, IsStaticConstructor);
				MarkMethodsIf (type.Methods, HasSerializationAttribute);
			}

			DoAdditionalTypeProcessing (type);

			Annotations.Pop ();

			Annotations.Mark (type);

			ApplyPreserveInfo (type);

			return type;
		}

		// Allow subclassers to mark additional things in the main processing loop
		protected virtual void DoAdditionalProcessing ()
		{
		}

		// Allow subclassers to mark additional things when marking a method
		protected virtual void DoAdditionalTypeProcessing (TypeDefinition method)
		{
		}

		void MarkAssemblySpecialCustomAttributes (AssemblyDefinition assembly)
		{
			if (!assembly.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in assembly.CustomAttributes) {
				string attributeFullName = attribute.Constructor.DeclaringType.FullName;
				switch (attributeFullName) {
				case "System.Diagnostics.DebuggerDisplayAttribute":
					StoreDebuggerTypeTarget (assembly, attribute, _assemblyDebuggerDisplayAttributes);
					break;
				case "System.Diagnostics.DebuggerTypeProxyAttribute":
					StoreDebuggerTypeTarget (assembly, attribute, _assemblyDebuggerTypeProxyAttributes);
					break;
				}
			}
		}

		void StoreDebuggerTypeTarget (AssemblyDefinition assembly, CustomAttribute attribute, Dictionary<TypeDefinition, CustomAttribute> dictionary)
		{
			if (_context.KeepMembersForDebuggerAttributes) {
				TypeReference targetTypeReference = null;
				TypeDefinition targetTypeDefinition = null;
				foreach (var property in attribute.Properties) {
					if (property.Name == "Target") {
						targetTypeReference = (TypeReference) property.Argument.Value;
						break;
					}

					if (property.Name == "TargetTypeName") {
						targetTypeReference = assembly.MainModule.GetType ((string) property.Argument.Value);
						break;
					}
				}

				if (targetTypeReference != null) {
					targetTypeDefinition = ResolveTypeDefinition (targetTypeReference);
					if (targetTypeDefinition != null) {
						dictionary[targetTypeDefinition] = attribute;
					}
				}
			}
		}

		void MarkTypeSpecialCustomAttributes (TypeDefinition type)
		{
			CustomAttribute debuggerAttribute;
			if (_assemblyDebuggerDisplayAttributes.TryGetValue (type, out debuggerAttribute)) {
				MarkTypeWithDebuggerDisplayAttribute (type, debuggerAttribute);
			}

			if (_assemblyDebuggerTypeProxyAttributes.TryGetValue (type, out debuggerAttribute)) {
				MarkTypeWithDebuggerTypeProxyAttribute (type, debuggerAttribute);
			}

			if (!type.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in type.CustomAttributes) {
				switch (attribute.Constructor.DeclaringType.FullName) {
				case "System.Xml.Serialization.XmlSchemaProviderAttribute":
					MarkXmlSchemaProvider (type, attribute);
					break;
				case "System.Diagnostics.DebuggerDisplayAttribute":
					MarkTypeWithDebuggerDisplayAttribute (type, attribute);
					break;
				case "System.Diagnostics.DebuggerTypeProxyAttribute":
					MarkTypeWithDebuggerTypeProxyAttribute (type, attribute);
					break;
				case "System.Diagnostics.Tracing.EventDataAttribute":
					MarkTypeWithEventDataAttribute (type);
					break;
				}
			}
		}

		void MarkMethodSpecialCustomAttributes (MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return;

			foreach (CustomAttribute attribute in method.CustomAttributes) {
				switch (attribute.Constructor.DeclaringType.FullName) {
				case "System.Web.Services.Protocols.SoapHeaderAttribute":
					MarkSoapHeader (method, attribute);
					break;
				}
			}
		}

		void MarkTypeWithEventDataAttribute (TypeDefinition type)
		{
			MarkMethodsIf (type.Methods, IsPublicInstancePropertyMethod);
		}

		void MarkXmlSchemaProvider (TypeDefinition type, CustomAttribute attribute)
		{
			string method_name;
			if (!TryGetStringArgument (attribute, out method_name))
				return;

			MarkNamedMethod (type, method_name);
		}

		void MarkTypeWithDebuggerDisplayAttribute (TypeDefinition type, CustomAttribute attribute)
		{
			if (_context.KeepMembersForDebuggerAttributes) {

				string displayString = (string) attribute.ConstructorArguments[0].Value;

				Regex regex = new Regex ("{[^{}]+}", RegexOptions.Compiled);

				foreach (Match match in regex.Matches (displayString)) {
					// Remove '{' and '}'
					string realMatch = match.Value.Substring (1, match.Value.Length - 2);

					// Remove ",nq" suffix if present
					// (it asks the expression evaluator to remove the quotes when displaying the final value)
					if (Regex.IsMatch(realMatch, @".+,\s*nq")) {
						realMatch = realMatch.Substring (0, realMatch.LastIndexOf (','));
					}

					if (realMatch.EndsWith ("()")) {
						string methodName = realMatch.Substring (0, realMatch.Length - 2);
						MethodDefinition method = GetMethodWithNoParameters (type, methodName);
						if (method != null) {
							MarkMethod (method);
							continue;
						}
					} else {
						FieldDefinition field = GetField (type, realMatch);
						if (field != null) {
							MarkField (field);
							continue;
						}

						PropertyDefinition property = GetProperty (type, realMatch);
						if (property != null) {
							if (property.GetMethod != null) {
								MarkMethod (property.GetMethod);
							}
							if (property.SetMethod != null) {
								MarkMethod (property.SetMethod);
							}
							continue;
						}
					}

					while (type != null) {
						MarkMethods (type);
						MarkFields (type, includeStatic: true);
						type = type.BaseType != null ? ResolveTypeDefinition (type.BaseType) : null;
					}
					return;
				}
			}
		}

		void MarkTypeWithDebuggerTypeProxyAttribute (TypeDefinition type, CustomAttribute attribute)
		{
			if (_context.KeepMembersForDebuggerAttributes) {
				object constructorArgument = attribute.ConstructorArguments[0].Value;
				TypeReference proxyTypeReference = constructorArgument as TypeReference;
				if (proxyTypeReference == null) {
					string proxyTypeReferenceString = constructorArgument as string;
					if (proxyTypeReferenceString != null) {
						proxyTypeReference = type.Module.GetType (proxyTypeReferenceString, runtimeName: true);
					}
				}

				if (proxyTypeReference == null) {
					return;
				}

				MarkType (proxyTypeReference);

				TypeDefinition proxyType = ResolveTypeDefinition (proxyTypeReference);
				if (proxyType != null) {
					MarkMethods (proxyType);
					MarkFields (proxyType, includeStatic: true);
				}
			}
		}

		static bool TryGetStringArgument (CustomAttribute attribute, out string argument)
		{
			argument = null;

			if (attribute.ConstructorArguments.Count < 1)
				return false;

			argument = attribute.ConstructorArguments [0].Value as string;

			return argument != null;
		}

		protected int MarkNamedMethod (TypeDefinition type, string method_name)
		{
			if (!type.HasMethods)
				return 0;

			int count = 0;
			foreach (MethodDefinition method in type.Methods) {
				if (method.Name != method_name)
					continue;

				MarkMethod (method);
				count++;
			}

			return count;
		}

		void MarkSoapHeader (MethodDefinition method, CustomAttribute attribute)
		{
			string member_name;
			if (!TryGetStringArgument (attribute, out member_name))
				return;

			MarkNamedField (method.DeclaringType, member_name);
			MarkNamedProperty (method.DeclaringType, member_name);
		}

		void MarkNamedField (TypeDefinition type, string field_name)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.Name != field_name)
					continue;

				MarkField (field);
			}
		}

		void MarkNamedProperty (TypeDefinition type, string property_name)
		{
			if (!type.HasProperties)
				return;

			foreach (PropertyDefinition property in type.Properties) {
				if (property.Name != property_name)
					continue;

				Annotations.Push (property);
				MarkMethod (property.GetMethod);
				MarkMethod (property.SetMethod);
				Annotations.Pop ();
			}
		}

		void MarkGenericParameterProvider (IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			foreach (GenericParameter parameter in provider.GenericParameters)
				MarkGenericParameter (parameter);
		}

		void MarkGenericParameter (GenericParameter parameter)
		{
			MarkCustomAttributes (parameter);
			foreach (TypeReference constraint in parameter.Constraints)
				MarkType (constraint);
		}

		bool IsVirtualAndHasPreservedParent (MethodDefinition method)
		{
			if (!method.IsVirtual)
				return false;

			var base_list = Annotations.GetBaseMethods (method);
			if (base_list == null)
				return false;

			foreach (MethodDefinition @base in base_list) {
				if (IgnoreScope (@base.DeclaringType.Scope))
					return true;

				if (IsVirtualAndHasPreservedParent (@base))
					return true;
			}

			return false;
		}

		static bool IsSpecialSerializationConstructor (MethodDefinition method)
		{
			if (!IsInstanceConstructor (method))
				return false;

			var parameters = method.Parameters;
			if (parameters.Count != 2)
				return false;

			return parameters [0].ParameterType.Name == "SerializationInfo" &&
				parameters [1].ParameterType.Name == "StreamingContext";
		}

		protected void MarkMethodsIf (Collection<MethodDefinition> methods, Func<MethodDefinition, bool> predicate)
		{
			foreach (MethodDefinition method in methods)
				if (predicate (method)) {
					Annotations.Push (predicate);
					MarkMethod (method);
					Annotations.Pop ();
				}
		}

		static bool IsDefaultConstructor (MethodDefinition method)
		{
			return IsInstanceConstructor (method) && !method.HasParameters;
		}

		protected static bool IsInstanceConstructor (MethodDefinition method)
		{
			return method.IsConstructor && !method.IsStatic;
		}

		protected void MarkDefaultConstructor (TypeDefinition type)
		{
			if ((type == null) || !type.HasMethods)
				return;

			MarkMethodsIf (type.Methods, IsDefaultConstructor);
		}

		static bool IsStaticConstructor (MethodDefinition method)
		{
			return method.IsConstructor && method.IsStatic;
		}

		static bool HasSerializationAttribute (MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return false;
			foreach (var ca in method.CustomAttributes) {
				var cat = ca.AttributeType;
				if (cat.Namespace != "System.Runtime.Serialization")
					continue;
				switch (cat.Name) {
				case "OnDeserializedAttribute":
				case "OnDeserializingAttribute":
				case "OnSerializedAttribute":
				case "OnSerializingAttribute":
					return true;
				}
			}
			return false;
		}

		static bool IsSerializable (TypeDefinition td)
		{
			return (td.Attributes & TypeAttributes.Serializable) != 0;
		}

		static bool IsMulticastDelegate (TypeDefinition td)
		{
			return td.BaseType != null && td.BaseType.FullName == "System.MulticastDelegate";
		}

		bool IsEventSource (TypeDefinition td)
		{
			TypeReference type = td;
			do {
				if (type.FullName == "System.Diagnostics.Tracing.EventSource") {
					return true;
				}

				TypeDefinition typeDef = type.Resolve ();
				if (typeDef == null) {
					HandleUnresolvedType (type);
					return false;
				}
				type = typeDef.BaseType;
			} while (type != null);
			return false;
		}

		void MarkEventSource (TypeDefinition td)
		{
			foreach (var nestedType in td.NestedTypes) {
				if (nestedType.Name == "Keywords" || nestedType.Name == "Tasks" || nestedType.Name == "Opcodes") {
					MarkStaticFields (nestedType);
				}
			}
		}

		protected TypeDefinition ResolveTypeDefinition (TypeReference type)
		{
			TypeDefinition td = type as TypeDefinition;
			if (td == null)
				td = type.Resolve ();

			return td;
		}

		protected TypeReference GetOriginalType (TypeReference type)
		{
			while (type is TypeSpecification) {
				GenericInstanceType git = type as GenericInstanceType;
				if (git != null)
					MarkGenericArguments (git);

				var mod = type as IModifierType;
				if (mod != null)
					MarkModifierType (mod);

				var fnptr = type as FunctionPointerType;
				if (fnptr != null) {
					MarkParameters (fnptr);
					MarkType (fnptr.ReturnType);
					break; // FunctionPointerType is the original type
				}
				else {
					type = ((TypeSpecification) type).ElementType;
				}
			}

			return type;
		}

		void MarkParameters (FunctionPointerType fnptr)
		{
			if (!fnptr.HasParameters)
				return;

			for (int i = 0; i < fnptr.Parameters.Count; i++)
			{
				MarkType (fnptr.Parameters[i].ParameterType);
			}
		}

		void MarkModifierType (IModifierType mod)
		{
			MarkType (mod.ModifierType);
		}

		void MarkGenericArguments (IGenericInstance instance)
		{
			foreach (TypeReference argument in instance.GenericArguments)
				MarkType (argument);

			MarkGenericArgumentConstructors (instance);
		}

		void MarkGenericArgumentConstructors (IGenericInstance instance)
		{
			var arguments = instance.GenericArguments;

			var generic_element = GetGenericProviderFromInstance (instance);
			if (generic_element == null)
				return;

			var parameters = generic_element.GenericParameters;

			if (arguments.Count != parameters.Count)
				return;

			for (int i = 0; i < arguments.Count; i++) {
				var argument = arguments [i];
				var parameter = parameters [i];

				if (!parameter.HasDefaultConstructorConstraint)
					continue;

				var argument_definition = ResolveTypeDefinition (argument);
				if (argument_definition == null)
					continue;

				MarkMethodsIf (argument_definition.Methods, ctor => !ctor.IsStatic && !ctor.HasParameters);
			}
		}

		IGenericParameterProvider GetGenericProviderFromInstance (IGenericInstance instance)
		{
			var method = instance as GenericInstanceMethod;
			if (method != null)
				return ResolveMethodDefinition (method.ElementMethod);

			var type = instance as GenericInstanceType;
			if (type != null)
				return ResolveTypeDefinition (type.ElementType);

			return null;
		}

		void ApplyPreserveInfo (TypeDefinition type)
		{
			ApplyPreserveMethods (type);

			if (!Annotations.IsPreserved (type))
				return;

			switch (Annotations.GetPreserve (type)) {
			case TypePreserve.All:
				MarkFields (type, true);
				MarkMethods (type);
				break;
			case TypePreserve.Fields:
				MarkFields (type, true, true);
				break;
			case TypePreserve.Methods:
				MarkMethods (type);
				break;
			}
		}

		void ApplyPreserveMethods (TypeDefinition type)
		{
			var list = Annotations.GetPreservedMethods (type);
			if (list == null)
				return;

			MarkMethodCollection (list);
		}

		void ApplyPreserveMethods (MethodDefinition method)
		{
			var list = Annotations.GetPreservedMethods (method);
			if (list == null)
				return;

			MarkMethodCollection (list);
		}

		protected void MarkFields (TypeDefinition type, bool includeStatic, bool markBackingFieldsOnlyIfPropertyMarked = false)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (!includeStatic && field.IsStatic)
					continue;

				if (markBackingFieldsOnlyIfPropertyMarked && field.Name.EndsWith (">k__BackingField")) {
					// We can't reliably construct the expected property name from the backing field name for all compilers
					// because csc shortens the name of the backing field in some cases
					// For example:
					// Field Name = <IFoo<int>.Bar>k__BackingField
					// Property Name = IFoo<System.Int32>.Bar
                    //
					// instead we will search the properties and find the one that makes use of the current backing field
					var propertyDefinition = SearchPropertiesForMatchingFieldDefinition (field);
					if (propertyDefinition != null && !Annotations.IsMarked (propertyDefinition))
						continue;
				}
				MarkField (field);
			}
		}

		static PropertyDefinition SearchPropertiesForMatchingFieldDefinition (FieldDefinition field)
		{
			foreach (var property in field.DeclaringType.Properties) {
				foreach (var ins in property.GetMethod.Body.Instructions) {
					if (ins.Operand != null && ins.Operand == field)
						return property;
				}
			}

			return null;
		}

		protected void MarkStaticFields(TypeDefinition type)
		{
			if (!type.HasFields)
				return;

			foreach (FieldDefinition field in type.Fields) {
				if (field.IsStatic)
					MarkField (field);
			}
		}

		protected virtual void MarkMethods (TypeDefinition type)
		{
			if (type.HasMethods)
				MarkMethodCollection (type.Methods);
		}

		void MarkMethodCollection (IList<MethodDefinition> methods)
		{
			foreach (MethodDefinition method in methods)
				MarkMethod (method);
		}

		protected virtual MethodDefinition MarkMethod (MethodReference reference)
		{
			reference = GetOriginalMethod (reference);

			if (reference.DeclaringType is ArrayType)
				return null;

			Annotations.Push (reference);
			if (reference.DeclaringType is GenericInstanceType)
				MarkType (reference.DeclaringType);

//			if (IgnoreScope (reference.DeclaringType.Scope))
//				return;

			MethodDefinition method = ResolveMethodDefinition (reference);

			try {
				if (method == null) {
					HandleUnresolvedMethod (reference);
					return null;
				}

				if (Annotations.GetAction (method) == MethodAction.Nothing)
					Annotations.SetAction (method, MethodAction.Parse);

				EnqueueMethod (method);
			} finally {
				Annotations.Pop ();
			}
			Annotations.AddDependency (method);

			return method;
		}

		AssemblyDefinition ResolveAssembly (IMetadataScope scope)
		{
			AssemblyDefinition assembly = _context.Resolve (scope);
			MarkAssembly (assembly);
			return assembly;
		}

		protected MethodReference GetOriginalMethod (MethodReference method)
		{
			while (method is MethodSpecification) {
				GenericInstanceMethod gim = method as GenericInstanceMethod;
				if (gim != null)
					MarkGenericArguments (gim);

				method = ((MethodSpecification) method).ElementMethod;
			}

			return method;
		}

		MethodDefinition ResolveMethodDefinition (MethodReference method)
		{
			MethodDefinition md = method as MethodDefinition;
			if (md == null)
				md = method.Resolve ();

			return md;
		}

		protected virtual void ProcessMethod (MethodDefinition method)
		{
			if (CheckProcessed (method))
				return;

			Annotations.Push (method);
			MarkType (method.DeclaringType);
			MarkCustomAttributes (method);
			MarkSecurityDeclarations (method);

			MarkGenericParameterProvider (method);

			if (IsPropertyMethod (method))
				MarkProperty (GetProperty (method));
			else if (IsEventMethod (method))
				MarkEvent (GetEvent (method));

			if (method.HasParameters) {
				foreach (ParameterDefinition pd in method.Parameters) {
					MarkType (pd.ParameterType);
					MarkCustomAttributes (pd);
					MarkMarshalSpec (pd);
				}
			}

			if (method.HasOverrides) {
				foreach (MethodReference ov in method.Overrides)
					MarkMethod (ov);
			}

			MarkMethodSpecialCustomAttributes (method);

			if (method.IsVirtual)
				_virtual_methods.Add (method);

			MarkBaseMethods (method);

			MarkType (method.ReturnType);
			MarkCustomAttributes (method.MethodReturnType);
			MarkMarshalSpec (method.MethodReturnType);

			if (method.IsPInvokeImpl || method.IsInternalCall) {
				ProcessInteropMethod (method);
			}

			if (ShouldParseMethodBody (method))
				MarkMethodBody (method.Body);

			DoAdditionalMethodProcessing (method);

			Annotations.Mark (method);

			ApplyPreserveMethods (method);
			Annotations.Pop ();
		}

		// Allow subclassers to mark additional things when marking a method
		protected virtual void DoAdditionalMethodProcessing (MethodDefinition method)
		{
		}

		void MarkBaseMethods (MethodDefinition method)
		{
			var base_methods = Annotations.GetBaseMethods (method);
			if (base_methods == null)
				return;

			foreach (MethodDefinition base_method in base_methods) {
				if (base_method.DeclaringType.IsInterface && !method.DeclaringType.IsInterface)
					continue;

				MarkMethod (base_method);
				MarkBaseMethods (base_method);
			}
		}

		void ProcessInteropMethod(MethodDefinition method)
		{
			TypeDefinition returnTypeDefinition = ResolveTypeDefinition (method.ReturnType);
			const bool includeStaticFields = false;
			if (returnTypeDefinition != null && !returnTypeDefinition.IsImport) {
				MarkDefaultConstructor (returnTypeDefinition);
				MarkFields (returnTypeDefinition, includeStaticFields);
			}

			if (method.HasThis && !method.DeclaringType.IsImport) {
				MarkFields (method.DeclaringType, includeStaticFields);
			}

			foreach (ParameterDefinition pd in method.Parameters) {
				TypeReference paramTypeReference = pd.ParameterType;
				if (paramTypeReference is TypeSpecification) {
					paramTypeReference = (paramTypeReference as TypeSpecification).ElementType;
				}
				TypeDefinition paramTypeDefinition = ResolveTypeDefinition (paramTypeReference);
				if (paramTypeDefinition != null && !paramTypeDefinition.IsImport) {
					MarkFields (paramTypeDefinition, includeStaticFields);
					if (pd.ParameterType.IsByReference) {
						MarkDefaultConstructor (paramTypeDefinition);
					}
				}
			}
		}

		bool ShouldParseMethodBody (MethodDefinition method)
		{
			if (!method.HasBody)
				return false;

			AssemblyDefinition assembly = ResolveAssembly (method.DeclaringType.Scope);
			return (Annotations.GetAction (method) == MethodAction.ForceParse ||
				(Annotations.GetAction (assembly) == AssemblyAction.Link && Annotations.GetAction (method) == MethodAction.Parse));
		}

		static internal bool IsPropertyMethod (MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.Getter) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Setter) != 0;
		}

		static internal bool IsPublicInstancePropertyMethod (MethodDefinition md)
		{
			return md.IsPublic && !md.IsStatic && IsPropertyMethod (md);
		}

		static bool IsEventMethod (MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.AddOn) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Fire) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.RemoveOn) != 0;
		}

		static internal PropertyDefinition GetProperty (MethodDefinition md)
		{
			TypeDefinition declaringType = md.DeclaringType;
			foreach (PropertyDefinition prop in declaringType.Properties)
				if (prop.GetMethod == md || prop.SetMethod == md)
					return prop;

			return null;
		}

		static EventDefinition GetEvent (MethodDefinition md)
		{
			TypeDefinition declaringType = md.DeclaringType;
			foreach (EventDefinition evt in declaringType.Events)
				if (evt.AddMethod == md || evt.InvokeMethod == md || evt.RemoveMethod == md)
					return evt;

			return null;
		}

		protected void MarkProperty (PropertyDefinition prop)
		{
			MarkCustomAttributes (prop);
		}

		protected void MarkEvent (EventDefinition evt)
		{
			MarkCustomAttributes (evt);
			MarkMethodIfNotNull (evt.AddMethod);
			MarkMethodIfNotNull (evt.InvokeMethod);
			MarkMethodIfNotNull (evt.RemoveMethod);
		}

		void MarkMethodIfNotNull (MethodReference method)
		{
			if (method == null)
				return;

			MarkMethod (method);
		}

		protected virtual void MarkMethodBody (MethodBody body)
		{
			foreach (VariableDefinition var in body.Variables)
				MarkType (var.VariableType);

			foreach (ExceptionHandler eh in body.ExceptionHandlers)
				if (eh.HandlerType == ExceptionHandlerType.Catch)
					MarkType (eh.CatchType);

			foreach (Instruction instruction in body.Instructions)
				MarkInstruction (instruction);
		}

		protected virtual void MarkInstruction (Instruction instruction)
		{
			switch (instruction.OpCode.OperandType) {
			case OperandType.InlineField:
				MarkField ((FieldReference) instruction.Operand);
				break;
			case OperandType.InlineMethod:
				MarkMethod ((MethodReference) instruction.Operand);
				break;
			case OperandType.InlineTok:
				object token = instruction.Operand;
				if (token is TypeReference)
					MarkType ((TypeReference) token);
				else if (token is MethodReference)
					MarkMethod ((MethodReference) token);
				else
					MarkField ((FieldReference) token);
				break;
			case OperandType.InlineType:
				MarkType ((TypeReference) instruction.Operand);
				break;
			default:
				break;
			}
		}

		protected virtual void HandleUnresolvedType (TypeReference reference)
		{
			throw new ResolutionException (reference);
		}

		protected virtual void HandleUnresolvedMethod (MethodReference reference)
		{
			throw new ResolutionException (reference);
		}
	}
}
