
using System;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetCore, "Only for .NET Core")]
	/// <summary>
	/// Delegate and is created from 
	/// </summary>
	[SetupLinkerTrimMode ("link")]
	[KeptBaseOnTypeInAssembly (PlatformAssemblies.CoreLib, typeof (MulticastDelegate), PlatformAssemblies.CoreLib, typeof (Delegate))]

	// Check a couple override methods to verify they were not removed
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (MulticastDelegate), "GetHashCode()")]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (MulticastDelegate), "Equals(System.Object)")]

	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (Delegate), "GetHashCode()")]
	[KeptMemberInAssembly (PlatformAssemblies.CoreLib, typeof (Delegate), "Equals(System.Object)")]
	[KeptInterfaceOnTypeInAssembly (PlatformAssemblies.CoreLib, typeof (Delegate), PlatformAssemblies.CoreLib, typeof (ICloneable))]
	[KeptInterfaceOnTypeInAssembly (PlatformAssemblies.CoreLib, typeof (Delegate), PlatformAssemblies.CoreLib, typeof (ISerializable))]

	// Fails due to Runtime critical type System.Reflection.CustomAttributeData not found.
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	public class DelegateAndMulticastDelegateKeepInstantiatedReqs
	{
		public static void Main ()
		{
			typeof (MulticastDelegate).ToString ();

			// Cause the interfaces to be marked in order to eliminate the possibility of them being removed
			// due to no code path marking the interface type
			typeof (ISerializable).ToString ();
			typeof (ICloneable).ToString ();
		}
	}
}