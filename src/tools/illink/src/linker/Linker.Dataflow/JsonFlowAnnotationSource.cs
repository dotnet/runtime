using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Mono.Linker.Dataflow
{
	/// <summary>
	/// Provides dataflow annotations from a JSON file.
	/// </summary>
	class JsonFlowAnnotationSource : IFlowAnnotationSource
	{
		readonly Dictionary<MethodDefinition, AnnotatedMethod> _methods = new Dictionary<MethodDefinition, AnnotatedMethod>();
		readonly Dictionary<PropertyDefinition, DynamicallyAccessedMemberKinds> _properties = new Dictionary<PropertyDefinition, DynamicallyAccessedMemberKinds>();
		readonly Dictionary<FieldDefinition, DynamicallyAccessedMemberKinds> _fields = new Dictionary<FieldDefinition, DynamicallyAccessedMemberKinds> ();

		public JsonFlowAnnotationSource(LinkContext context, string jsonFile)
		{
			Initialize (context, jsonFile);
		}

		public DynamicallyAccessedMemberKinds GetFieldAnnotation (FieldDefinition field)
		{
			return _fields.TryGetValue (field, out var ann) ? ann : 0;
		}

		public DynamicallyAccessedMemberKinds GetParameterAnnotation (MethodDefinition method, int index)
		{
			if (_methods.TryGetValue(method, out var ann) && ann.ParameterAnnotations != null) {
				string paramName = method.Parameters [index].Name;

				foreach (var annotatedParam in ann.ParameterAnnotations)
					if (annotatedParam.ParamName == paramName)
						return annotatedParam.Annotation;
			}

			return 0;
		}

		public DynamicallyAccessedMemberKinds GetPropertyAnnotation (PropertyDefinition property)
		{
			return _properties.TryGetValue (property, out var ann) ? ann : 0;
		}

		public DynamicallyAccessedMemberKinds GetReturnParameterAnnotation (MethodDefinition method)
		{
			return _methods.TryGetValue (method, out var ann) ? ann.ReturnAnnotation : 0;
		}

		static DynamicallyAccessedMemberKinds ParseKinds(string s)
		{
			// Enum.Parse accepts a comma as a separator for Flags
			return (DynamicallyAccessedMemberKinds)Enum.Parse (typeof (DynamicallyAccessedMemberKinds), s);
		}

		private void Initialize(LinkContext context, string jsonFile)
		{
			// Need "using" because JsonDocument won't close this as part of Dispose().
			using FileStream jsonFileStream = File.OpenRead (jsonFile);

			// We only support UTF-8
			using JsonDocument jsonDoc = JsonDocument.Parse (jsonFileStream);

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

							if (member.Value.ValueKind == JsonValueKind.Object) {
								// This is a method

								// Parser uses same format as PreserveDependencyAttribute
								string [] signature = null;
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
											if (candidate.Parameters [i].ParameterType.FullName != signature [i].ToCecilName ()) {
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

								DynamicallyAccessedMemberKinds returnAnnotation = 0;
								var parameterAnnotations = new ArrayBuilder<(string ParamName, DynamicallyAccessedMemberKinds Annotation)> ();
								foreach (var parameter in member.Value.EnumerateObject ()) {
									if (parameter.Name == "return")
										returnAnnotation = ParseKinds (parameter.Value.GetString ());
									else
										parameterAnnotations.Add ((parameter.Name, ParseKinds (parameter.Value.GetString ())));
								}

								_methods [method] = new AnnotatedMethod (returnAnnotation, parameterAnnotations.ToArray ());
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
									_fields [field] = ParseKinds (member.Value.GetString ());
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
									_properties [property] = ParseKinds (member.Value.GetString ());
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

		struct ArrayBuilder<T>
		{
			private List<T> _list;

			public void Add (T value) => (_list ?? (_list = new List<T> ())).Add (value);

			public T [] ToArray () => _list?.ToArray ();
		}

		private struct AnnotatedMethod
		{
			public readonly DynamicallyAccessedMemberKinds ReturnAnnotation;
			public readonly (string ParamName, DynamicallyAccessedMemberKinds Annotation) [] ParameterAnnotations;

			public AnnotatedMethod (DynamicallyAccessedMemberKinds returnAnnotation,
				(string ParamName, DynamicallyAccessedMemberKinds Annotation) [] paramAnnotations)
				=> (ReturnAnnotation, ParameterAnnotations) = (returnAnnotation, paramAnnotations);
		}
	}
}
