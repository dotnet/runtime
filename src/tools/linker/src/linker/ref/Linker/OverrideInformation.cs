using Mono.Cecil;

namespace Mono.Linker
{
	public class OverrideInformation
	{
		internal OverrideInformation ()
		{
		}

		public MethodDefinition Base { get { throw null; } }
		public MethodDefinition Override { get { throw null; } }
		public InterfaceImplementation MatchingInterfaceImplementation { get { throw null; } }
		public TypeDefinition InterfaceType { get { throw null; } }
	}
}
