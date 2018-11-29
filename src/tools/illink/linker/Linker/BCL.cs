using Mono.Cecil;

namespace Mono.Linker
{
	static class BCL
	{
		public static class EventTracingForWindows
		{
			public static bool IsEventSourceImplementation (TypeDefinition type, LinkContext context = null)
			{
				if (!type.IsClass)
					return false;

				while (type.BaseType != null) {
					var bt = type.BaseType.Resolve ();

					if (bt == null) {
						if (context != null && !context.IgnoreUnresolved)
							throw new ResolutionException (type.BaseType);

						break;
					}

					if (IsEventSourceType (bt))
						return true;

					type = bt;
				}

				return false;
			}

			public static bool IsEventSourceType (TypeReference type)
			{
				return type.Namespace == "System.Diagnostics.Tracing" && type.Name == "EventSource";
			}

			public static bool IsNonEventAtribute (TypeReference type)
			{
				return type.Namespace == "System.Diagnostics.Tracing" && type.Name == "NonEventAttribute";
			}

			public static bool IsProviderName (string name)
			{
				return name == "Keywords" || name == "Tasks" || name == "Opcodes";
			}
		}

		public static bool IsIDisposableImplementation (MethodDefinition method)
		{
			if (method.Name != "Dispose" || method.ReturnType.MetadataType != MetadataType.Void)
				return false;

			if (method.HasParameters || method.HasGenericParameters || method.IsStatic)
				return false;

			if (!method.IsFinal)
				return false;

			return true;
		}

		public static TypeDefinition FindPredefinedType (string ns, string name, LinkContext context)
		{
			var cache = context.Resolver.AssemblyCache;

			AssemblyDefinition corlib;
			if (cache.TryGetValue ("mscorlib", out corlib))
				return corlib.MainModule.GetType (ns, name);
				
			if (cache.TryGetValue ("System.Private.CoreLib", out corlib))
				return corlib.MainModule.GetType (ns, name);

			return null;
		}
	}
}
