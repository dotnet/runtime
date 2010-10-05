//
// RemoveSerialization.cs
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

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class RemoveSerialization : BaseStep {

		static readonly string _Serialization = "System.Runtime.Serialization";
		static readonly string _ISerializable = Concat (_Serialization, "ISerializable");
		static readonly string _IDeserializationCallback = Concat (_Serialization, "IDeserializationCallback");
		static readonly string _SerializationInfo = Concat (_Serialization, "SerializationInfo");
		static readonly string _StreamingContext = Concat (_Serialization, "StreamingContext");

		static readonly string _GetObjectData = "GetObjectData";
		static readonly string _OnDeserialization = "OnDeserialization";

		static string Concat (string lhs, string rhs)
		{
			return string.Concat (lhs, ".", rhs);
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (assembly.Name.Name == "mscorlib")
				return;

			if (Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			foreach (ModuleDefinition module in assembly.Modules)
				foreach (TypeDefinition type in module.Types)
					ProcessType (type);
		}

		static void RemoveInterface (TypeDefinition type, string name)
		{
			for (int i = 0; i < type.Interfaces.Count; i++) {
				TypeReference iface = type.Interfaces [i];
				if (iface.FullName == name) {
					type.Interfaces.RemoveAt (i);
					return;
				}
			}
		}

		static void RemoveSerializableFlag (TypeDefinition type)
		{
			type.Attributes &= ~TypeAttributes.Serializable;
		}

		static void ProcessType (TypeDefinition type)
		{
			RemoveSerializableFlag (type);

			RemoveInterface (type, _ISerializable);
			RemoveMethod (type, ".ctor", _SerializationInfo, _StreamingContext);
			RemoveInterfaceMethod (type, _ISerializable, _GetObjectData, _SerializationInfo, _StreamingContext);

			RemoveInterface (type, _IDeserializationCallback);
			RemoveInterfaceMethod (type, _IDeserializationCallback, _OnDeserialization, "System.Object");

			RemoveField (type);
		}

		static void RemoveField (TypeDefinition type)
		{
			for (int i = 0; i < type.Fields.Count; i++) {
				FieldDefinition field = type.Fields [i];
				if (field.FieldType.FullName == _SerializationInfo) {
					type.Fields.RemoveAt (i);
					break;
				}
			}
		}

		static bool ParametersMatch (IMethodSignature meth, string [] parameters)
		{
			for (int i = 0; i < parameters.Length; i++) {
				ParameterDefinition param = meth.Parameters [i];
				if (param.ParameterType.FullName != parameters [i])
					return false;
			}

			return true;
		}

		static void RemoveInterfaceMethod (TypeDefinition type, string iface, string method, params string [] parameters)
		{
			RemoveMethod (type, method, parameters);
			RemoveMethod (type, Concat (iface, method), parameters);
		}

		static void RemoveMethod (TypeDefinition type, string name, params string [] parameters)
		{
			RemoveMethod (type.Methods, name, parameters);
		}

		static void RemoveMethod (IList container, string name, params string [] parameters)
		{
			for (int i = 0; i < container.Count; i++) {
				MethodDefinition method = (MethodDefinition) container [i];
				if (method.Name != name)
					continue;

				if (method.Parameters.Count != parameters.Length)
					continue;

				if (!ParametersMatch (method, parameters))
					continue;

				container.RemoveAt (i);
				return;
			}
		}
	}
}
