using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.NoSecurity
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "Not important for .NET Core build")]
	[SetupLinkerTrimMode ("link")]
	[SetupLinkerArgument ("--strip-security", "true")]
	[Reference ("System.dll")]
	// Attributes from System.Security.Permissions
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (SecurityPermissionAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (PermissionSetAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (ReflectionPermissionAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (RegistryPermissionAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (StrongNameIdentityPermissionAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (CodeAccessSecurityAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (EnvironmentPermissionAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (FileIOPermissionAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (HostProtectionAttribute))]

	// "Special" attributes from System.Security namespace that we seem to need to remove in order to set HasSecurity = false and not have
	// pe verify complain
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (SecurityCriticalAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (SecuritySafeCriticalAttribute))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (SuppressUnmanagedCodeSecurityAttribute))]
	public class CoreLibrarySecurityAttributeTypesAreRemoved
	{
		public static void Main ()
		{
			// Use something that has security attributes to make this test more meaningful
			var process = new Process ();
		}
	}
}