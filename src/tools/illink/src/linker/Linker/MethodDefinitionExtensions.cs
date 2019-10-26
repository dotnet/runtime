using Mono.Cecil;

namespace Mono.Linker
{
	public static class MethodDefinitionExtensions 
	{
		public static bool IsDefaultConstructor (this MethodDefinition method)
		{
			return IsInstanceConstructor (method) && !method.HasParameters;
		}

		public static bool IsInstanceConstructor (this MethodDefinition method)
		{
			return method.IsConstructor && !method.IsStatic;
		}

		public static bool IsIntrinsic (this MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return false;

			foreach (var ca in method.CustomAttributes) {
				var caType = ca.AttributeType;
				if (caType.Name == "IntrinsicAttribute" && caType.Namespace == "System.Runtime.CompilerServices")
					return true;
			}

			return false;
		}

		public static bool IsStaticConstructor (this MethodDefinition method)
		{
			return method.IsConstructor && method.IsStatic;
		}

		public static bool IsFinalizer (this MethodDefinition method)
		{
			if (method.Name != "Finalize" || method.ReturnType.MetadataType != MetadataType.Void)
				return false;

			if (method.HasParameters || method.HasGenericParameters || method.IsStatic)
				return false;

			return true;
		}
		
		public static void ClearDebugInformation (this MethodDefinition method)
		{
			// TODO: This always allocates, update when Cecil catches up
			var di = method.DebugInformation;
			di.SequencePoints.Clear ();
			if (di.Scope != null) {
				di.Scope.Variables.Clear ();
				di.Scope.Constants.Clear ();
				di.Scope = null;
			}
		}
	}
}
