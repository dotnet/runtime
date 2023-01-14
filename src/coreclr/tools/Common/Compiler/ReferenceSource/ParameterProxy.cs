// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	internal partial struct ParameterProxy
	{
		public partial ReferenceKind GetReferenceKind ()
		{
			if (IsImplicitThis)
				return Method.Method.DeclaringType.IsValueType ? ReferenceKind.Ref : ReferenceKind.None;
#pragma warning disable RS0030 // MethodReference.Parameters is banned -- this class provides wrappers to use
			var param = Method.Method.Parameters[MetadataIndex];
#pragma warning restore RS0030 // Do not used banned APIs
			if (!param.ParameterType.IsByReference)
				return ReferenceKind.None;
			if (param.IsIn)
				return ReferenceKind.In;
			if (param.IsOut)
				return ReferenceKind.Out;
			return ReferenceKind.Ref;
		}

		public TypeReference ParameterType {
			get {
				if (IsImplicitThis)
					return Method.Method.DeclaringType;
#pragma warning disable RS0030 // MethodReference.Parameters is banned -- this class provides wrappers to use
				return Method.Method.Parameters[MetadataIndex].ParameterType;
#pragma warning restore RS0030 // Do not used banned APIs
			}
		}

#pragma warning disable RS0030 // MethodReference.Parameters is banned -- this class provides wrappers to use
		public partial string GetDisplayName () => IsImplicitThis ? "this"
			: !string.IsNullOrEmpty (Method.Method.Parameters[MetadataIndex].Name) ? Method.Method.Parameters[MetadataIndex].Name
			: $"#{Index}";
#pragma warning restore RS0030 // Do not used banned APIs

		public ICustomAttributeProvider GetCustomAttributeProvider ()
		{
			if (IsImplicitThis)
				return Method.Method;
#pragma warning disable RS0030 // MethodReference.Parameters is banned -- this class provides wrappers to use
			return Method.Method.Parameters[MetadataIndex];
#pragma warning restore RS0030 // Do not used banned APIs
		}

		public partial bool IsTypeOf (string typeName) => ParameterType.IsTypeOf (typeName);

		public bool IsTypeOf (WellKnownType type) => ParameterType.IsTypeOf (type);
	}
}
