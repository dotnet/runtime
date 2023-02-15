using System;
using System.Timers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerTrimMode ("link")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[Reference ("System.dll")]
	// System.Core.dll is referenced by System.dll in the .NET FW class libraries. Our GetType reflection marking code
	// detects a GetType("SHA256CryptoServiceProvider") in System.dll, which then causes a type in System.Core.dll to be marked.
	// PeVerify fails on the original GAC copy of System.Core.dll so it's expected that it will also fail on the stripped version we output
	[SkipPeVerify ("System.Core.dll")]
	// Fails with `Runtime critical type System.Reflection.CustomAttributeData not found`
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
#if !NETCOREAPP
	// .NET Framework System.dll doesn't pass peverify
	[SkipPeVerify ("System.dll")]
#endif

	class CanLinkCoreLibrariesWithOnlyKeepUsedAttributes
	{
		static void Main ()
		{
			// Use something from System so that the entire reference isn't linked away
			var system = new Timer ();
		}
	}
}
