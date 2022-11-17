using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupLinkerAction ("copy", "CoreLibEmulator")]
	[SetupCompileBefore ("CoreLibEmulator.dll", new string[] { "Dependencies/CoreLibEmulator.cs" }, null, new string[] { "INCLUDE_CORELIB_IMPL" }, compilerToUse: "csc")]

	// Validate that calls from one overload of Type.Get* to another overload of the same method don't produce warning
	[LogDoesNotContain ("Reflection call 'System.Reflection.ConstructorInfo System.Type::GetConstructor\\([^)]*\\)' inside 'System.Reflection.ConstructorInfo System.Type::GetConstructor\\([^)]*\\)' does not use detectable instance type extraction")]
	[LogDoesNotContain ("Reflection call 'System.Reflection.MethodInfo System.Type::GetMethod\\([^)]*\\)' inside 'System.Reflection.MethodInfo System.Type::GetMethod\\([^)]*\\)' was detected with argument which cannot be analyzed")]
	[LogDoesNotContain ("Reflection call 'System.Reflection.PropertyInfo System.Type::GetProperty\\([^)]*\\)' inside 'System.Reflection.PropertyInfo System.Type::GetProperty\\([^)]*\\)' was detected with argument which cannot be analyzed")]
	[LogDoesNotContain ("Reflection call 'System.Reflection.FieldInfo System.Type::GetField\\([^)]*\\)' inside 'System.Reflection.FieldInfo System.Type::GetField\\([^)]*\\)' was detected with argument which cannot be analyzed")]
	[LogDoesNotContain ("Reflection call 'System.Reflection.EventInfo System.Type::GetEvent\\([^)]*\\)' inside 'System.Reflection.EventInfo System.Type::GetEvent\\([^)]*\\)' was detected with argument which cannot be analyzed")]

	public class CoreLibMessages
	{
		public static void Main ()
		{
			Dependencies.CoreLibEmulator.Test ();
		}
	}
}