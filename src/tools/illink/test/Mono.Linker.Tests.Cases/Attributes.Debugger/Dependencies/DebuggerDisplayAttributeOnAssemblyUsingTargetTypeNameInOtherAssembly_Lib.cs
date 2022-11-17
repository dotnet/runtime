namespace Mono.Linker.Tests.Cases.Attributes.Debugger.Dependencies
{
	public class DebuggerDisplayAttributeOnAssemblyUsingTargetTypeNameInOtherAssembly_Lib
	{
		public int Property { get; set; }

		public class NestedType
		{
			public int NestedProperty { get; set; }
		}

		public class GenericType<T>
		{
			public T PropertyOnGenericType { get; set; }
		}
	}
}