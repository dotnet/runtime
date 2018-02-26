using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.NoSecurity {
	[SetupLinkerCoreAction ("link")]
	[SetupLinkerArgument ("--strip-security", "true")]
	[Reference ("System.dll")]
	// Attributes from System.Security.Permissions
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SecurityPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (PermissionSetAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (ReflectionPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (RegistryPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (StrongNameIdentityPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (CodeAccessSecurityAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (EnvironmentPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (FileIOPermissionAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (HostProtectionAttribute))]
	
	// "Special" attributes from System.Security namespace that we seem to need to remove in order to set HasSecurity = false and not have
	// pe verify complain
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SecurityCriticalAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SecuritySafeCriticalAttribute))]
	[RemovedTypeInAssembly ("mscorlib.dll", typeof (SuppressUnmanagedCodeSecurityAttribute))]
	
	// Fails with `Runtime critical type System.Reflection.CustomAttributeData not found` which is a known short coming
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	[SkipPeVerify ("System.dll")]
	public class CoreLibrarySecurityAttributeTypesAreRemoved {
		public static void Main ()
		{
			// Use something that has security attributes to make this test more meaningful
			var process = new Process ();
		}
	}
}