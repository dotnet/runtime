// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	/// <summary>
	/// Provides dataflow annotations from a JSON file.
	/// </summary>
	class JsonFlowAnnotationSource : IFlowAnnotationSource
	{
		readonly Dictionary<MethodDefinition, AnnotatedMethod> _methods = new Dictionary<MethodDefinition, AnnotatedMethod> ();
		readonly Dictionary<PropertyDefinition, DynamicallyAccessedMemberTypes> _properties = new Dictionary<PropertyDefinition, DynamicallyAccessedMemberTypes> ();
		readonly Dictionary<FieldDefinition, DynamicallyAccessedMemberTypes> _fields = new Dictionary<FieldDefinition, DynamicallyAccessedMemberTypes> ();

		public JsonFlowAnnotationSource (LinkContext context, string jsonFile)
		{
			Initialize (context, jsonFile);
		}

		public DynamicallyAccessedMemberTypes GetFieldAnnotation (FieldDefinition field)
		{
			return _fields.TryGetValue (field, out var ann) ? ann : DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetParameterAnnotation (MethodDefinition method, int index)
		{
			if (_methods.TryGetValue (method, out var ann) && ann.ParameterAnnotations != null) {
				string paramName = method.Parameters[index].Name;

				foreach (var (ParamName, Annotation) in ann.ParameterAnnotations)
					if (ParamName == paramName)
						return Annotation;
			}

			return DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetPropertyAnnotation (PropertyDefinition property)
		{
			return _properties.TryGetValue (property, out var ann) ? ann : DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetReturnParameterAnnotation (MethodDefinition method)
		{
			return _methods.TryGetValue (method, out var ann) ? ann.ReturnAnnotation : DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetThisParameterAnnotation (MethodDefinition method)
		{
			if (_methods.TryGetValue (method, out var ann) && ann.ParameterAnnotations != null) {

				foreach (var (ParamName, Annotation) in ann.ParameterAnnotations)
					if (ParamName == "this")
						return Annotation;
			}

			return DynamicallyAccessedMemberTypes.None;
		}

		static DynamicallyAccessedMemberTypes ParseKinds (JsonElement attributes)
		{
			foreach (var attribute in attributes.EnumerateObject ()) {
				if (attribute.Name == "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers") {
					string value = attribute.Value.GetString ();

					// Enum.Parse accepts a comma as a separator for Flags
					return (DynamicallyAccessedMemberTypes) Enum.Parse (typeof (DynamicallyAccessedMemberTypes), value);
				}
			}

			return DynamicallyAccessedMemberTypes.None;
		}

		void Initialize (LinkContext context, string jsonFile)
		{
			// Need "using" because JsonDocument won't close this as part of Dispose().
			using FileStream jsonFileStream = File.OpenRead (jsonFile);

			// We only support UTF-8
			using JsonDocument jsonDoc = JsonDocument.Parse (jsonFileStream, new JsonDocumentOptions {
				CommentHandling = JsonCommentHandling.Skip
			});

			// TODO: need to also check the document is structurally sound.
			foreach (var assemblyElement in jsonDoc.RootElement.EnumerateObject ()) {

				var assembly = context.Resolve (new AssemblyNameReference (assemblyElement.Name, new Version ()));

				if (assembly == null) {
					context.LogMessage ($"Assembly {assemblyElement.Name} couldn't be resolved");
					continue;
				}

				foreach (var ns in assemblyElement.Value.EnumerateObject ()) {
					string namespaceName = ns.Name;

					foreach (var typeElement in ns.Value.EnumerateObject ()) {
						string typeName = typeElement.Name;

						var type = assembly.MainModule.GetType (namespaceName, typeName);
						if (type == null) {
							context.LogMessage ($"Type {namespaceName}.{typeName} couldn't be resolved");
							continue;
						}

						foreach (var member in typeElement.Value.EnumerateObject ()) {
							string memberName = member.Name;

							// Technically, '(' is a valid character in both method and field names,
							// but the existing PreserveDependencyAttribute parser has a limitation in supporting
							// that anyway, so we will use '(' to distinguish methods from fields/properties.
							if (memberName.Contains ("(")) {
								// This is a method

								// Parser uses same format as PreserveDependencyAttribute
								string[] signature = null;
								memberName = memberName.Replace (" ", "");
								var sign_start = memberName.IndexOf ('(');
								var sign_end = memberName.LastIndexOf (')');
								if (sign_start > 0 && sign_end > sign_start) {
									var parameters = memberName.Substring (sign_start + 1, sign_end - sign_start - 1);
									signature = string.IsNullOrEmpty (parameters) ? Array.Empty<string> () : parameters.Split (',');
									memberName = memberName.Substring (0, sign_start);
								}

								MethodDefinition method = null;
								foreach (var candidate in type.Methods) {
									if (candidate.Name != memberName)
										continue;

									if (signature != null) {
										if (candidate.Parameters.Count != signature.Length)
											continue;

										bool sigMatch = true;
										for (int i = 0; i < candidate.Parameters.Count; i++) {
											if (candidate.Parameters[i].ParameterType.FullName != signature[i].ToCecilName ()) {
												sigMatch = false;
												break;
											}
										}

										if (!sigMatch)
											continue;
									}

									if (method != null) {
										context.LogMessage ($"Multiple matches for method {memberName}");
									}

									method = candidate;
								}

								if (method == null) {
									context.LogMessage ($"No match for {memberName}");
									continue;
								}

								DynamicallyAccessedMemberTypes returnAnnotation = DynamicallyAccessedMemberTypes.None;
								var parameterAnnotations = new ArrayBuilder<(string ParamName, DynamicallyAccessedMemberTypes Annotation)> ();
								foreach (var parameter in member.Value.EnumerateObject ()) {
									if (parameter.Name == "return") {
										returnAnnotation = ParseKinds (parameter.Value);
									} else {
										DynamicallyAccessedMemberTypes paramAnnotation = ParseKinds (parameter.Value);
										if (paramAnnotation != DynamicallyAccessedMemberTypes.None)
											parameterAnnotations.Add ((parameter.Name, paramAnnotation));
									}
								}

								if (returnAnnotation != DynamicallyAccessedMemberTypes.None || parameterAnnotations.Count > 0)
									_methods[method] = new AnnotatedMethod (returnAnnotation, parameterAnnotations.ToArray ());
							} else {
								// This is a field or property
								FieldDefinition field = null;
								foreach (var candidate in type.Fields) {
									if (candidate.Name != memberName)
										continue;

									// IL allows overloaded fields, but not worth adding messages for that...
									field = candidate;
									break;
								}

								if (field != null) {
									DynamicallyAccessedMemberTypes fieldAnnotation = ParseKinds (member.Value);

									if (fieldAnnotation != DynamicallyAccessedMemberTypes.None)
										_fields[field] = fieldAnnotation;
									continue;
								}

								PropertyDefinition property = null;
								foreach (var candidate in type.Properties) {
									if (candidate.Name != memberName)
										continue;

									// IL allows overloaded properties, but not worth adding messages for that...
									property = candidate;
									break;
								}

								if (property != null) {
									DynamicallyAccessedMemberTypes propertyAnnotation = ParseKinds (member.Value);

									if (propertyAnnotation != DynamicallyAccessedMemberTypes.None)
										_properties[property] = propertyAnnotation;
								}

								if (field == null && property == null) {
									context.LogMessage ($"No match for field or property {memberName}");
								}
							}
						}
					}
				}
			}
		}

		struct AnnotatedMethod
		{
			public readonly DynamicallyAccessedMemberTypes ReturnAnnotation;
			public readonly (string ParamName, DynamicallyAccessedMemberTypes Annotation)[] ParameterAnnotations;

			public AnnotatedMethod (DynamicallyAccessedMemberTypes returnAnnotation,
				(string ParamName, DynamicallyAccessedMemberTypes Annotation)[] paramAnnotations)
				=> (ReturnAnnotation, ParameterAnnotations) = (returnAnnotation, paramAnnotations);
		}
	}
}
