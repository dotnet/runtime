// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	// This discovers types attributed with certain serialization attributes, to match the old behavior
	// of xamarin-android. It is not meant to be complete. Unlike xamarin-andorid:
	// - this will only discover attributed types that are marked
	// - this will discover types in non-"link" assemblies as well
	public class DiscoverSerializationHandler : IMarkHandler
	{
		LinkContext? _context;
		LinkContext Context {
			get {
				Debug.Assert (_context != null);
				return _context;
			}
		}

		public void Initialize (LinkContext context, MarkContext markContext)
		{
			_context = context;
			markContext.RegisterMarkTypeAction (ProcessType);
			markContext.RegisterMarkMethodAction (CheckForSerializerActivation);
		}

		void CheckForSerializerActivation (MethodDefinition method)
		{
			var type = method.DeclaringType;

			if (!Context.SerializationMarker.IsActive (SerializerKind.DataContractSerializer) &&
				method.IsConstructor && !method.IsStatic &&
				((type.Namespace == "System.Runtime.Serialization" && type.Name == "DataContractSerializer") ||
				(type.Namespace == "System.Runtime.Serialization.Json" && type.Name == "DataContractJsonSerializer"))) {

				Context.SerializationMarker.Activate (SerializerKind.DataContractSerializer);
			}

			if (!Context.SerializationMarker.IsActive (SerializerKind.XmlSerializer) &&
				method.IsConstructor && !method.IsStatic &&
				type.Namespace == "System.Xml.Serialization" &&
				type.Name == "XmlSerializer") {

				Context.SerializationMarker.Activate (SerializerKind.XmlSerializer);
			}
		}

		void ProcessType (TypeDefinition type)
		{
			ProcessAttributeProvider (type);

			if (type.HasFields) {
				foreach (var field in type.Fields)
					ProcessAttributeProvider (field);
			}

			if (type.HasProperties) {
				foreach (var property in type.Properties)
					ProcessAttributeProvider (property);
			}

			if (type.HasMethods) {
				foreach (var method in type.Methods)
					ProcessAttributeProvider (method);
			}

			if (type.HasEvents) {
				foreach (var @event in type.Events) {
					ProcessAttributeProvider (@event);
				}
			}
		}

		void ProcessAttributeProvider (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			var serializedFor = SerializerKind.None;

			foreach (var attribute in provider.CustomAttributes) {
				if (IsPreservedSerializationAttribute (provider, attribute, out SerializerKind serializerKind))
					serializedFor |= serializerKind;
			}

			if (serializedFor == SerializerKind.None)
				return;

			if (serializedFor.HasFlag (SerializerKind.DataContractSerializer))
				Context.SerializationMarker.TrackForSerialization (provider, SerializerKind.DataContractSerializer);
			if (serializedFor.HasFlag (SerializerKind.XmlSerializer))
				Context.SerializationMarker.TrackForSerialization (provider, SerializerKind.XmlSerializer);
		}

		static bool IsPreservedSerializationAttribute (ICustomAttributeProvider provider, CustomAttribute attribute, out SerializerKind serializerKind)
		{
			TypeReference attributeType = attribute.Constructor.DeclaringType;
			serializerKind = SerializerKind.None;

			switch (attributeType.Namespace) {

			// http://bugzilla.xamarin.com/show_bug.cgi?id=1415
			// http://msdn.microsoft.com/en-us/library/system.runtime.serialization.datamemberattribute.aspx
			case "System.Runtime.Serialization":
				var serialized = false;
				if (provider is PropertyDefinition or FieldDefinition or EventDefinition)
					serialized = attributeType.Name == "DataMemberAttribute";
				else if (provider is TypeDefinition)
					serialized = attributeType.Name == "DataContractAttribute";

				if (serialized) {
					serializerKind = SerializerKind.DataContractSerializer;
					return true;
				}
				break;

			// http://msdn.microsoft.com/en-us/library/83y7df3e.aspx
			case "System.Xml.Serialization":
				var attributeName = attributeType.Name;
				if (attributeName.StartsWith ("Xml", StringComparison.Ordinal)
					&& attributeName.EndsWith ("Attribute", StringComparison.Ordinal)
					&& attributeName != "XmlIgnoreAttribute") {
					serializerKind = SerializerKind.XmlSerializer;
					return true;
				}
				break;

			};

			return false;
		}
	}
}
