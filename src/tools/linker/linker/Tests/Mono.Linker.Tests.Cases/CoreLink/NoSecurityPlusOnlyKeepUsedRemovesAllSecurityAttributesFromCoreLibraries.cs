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
	public class NoSecurityPlusOnlyKeepUsedRemovesAllSecurityAttributesFromCoreLibraries {
		public static void Main ()
		{
			// Use something that has security attributes to make this test more meaningful
			var process = new Process ();
		}
	}
}