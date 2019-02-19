using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink {
	[SetupLinkerCoreAction ("link")]
	[SetupLinkerArgument ("--strip-security", "true")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[Reference ("System.dll")]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SecurityPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (PermissionSetAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (ReflectionPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (RegistryPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (StrongNameIdentityPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (CodeAccessSecurityAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (EnvironmentPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (FileIOPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (HostProtectionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SecurityCriticalAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SecuritySafeCriticalAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SuppressUnmanagedCodeSecurityAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SecurityRulesAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (AllowPartiallyTrustedCallersAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (UnverifiableCodeAttribute))]
	// Fails with `Runtime critical type System.Reflection.CustomAttributeData not found` which is a known short coming
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	[SkipPeVerify ("System.dll")]
	// System.Core.dll is referenced by System.dll in the .NET FW class libraries. Our GetType reflection marking code
	// detects a GetType("SHA256CryptoServiceProvider") in System.dll, which then causes a type in System.Core.dll to be marked.
	// PeVerify fails on the original GAC copy of System.Core.dll so it's expected that it will also fail on the stripped version we output
	[SkipPeVerify ("System.Core.dll")]
	public class NoSecurityPlusOnlyKeepUsedRemovesAllSecurityAttributesFromCoreLibraries {
		public static void Main ()
		{
			// Use something that has security attributes to make this test more meaningful
			var process = new Process ();
		}
	}
}