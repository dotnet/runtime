namespace Mono.Linker.Tests.Cases.Substitutions.Dependencies
{
	public class ReferencedField
	{
		static ReferencedField ()
		{
			BoolValue = false;
		}
		public static bool BoolValue;
	}
}
