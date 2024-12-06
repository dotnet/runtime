using System;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
{
	public class RequireHelper
	{
		public static Type RequireType ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string type) {
			return Type.GetType (type);
		}
	}

	public class TypeDefinedInSameAssemblyAsGetType {}
}
