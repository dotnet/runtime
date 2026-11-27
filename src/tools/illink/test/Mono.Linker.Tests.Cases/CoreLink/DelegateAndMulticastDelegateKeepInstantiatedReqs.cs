
using System;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
    [TestCaseRequirements(TestRunCharacteristics.TargetingNetCore, "Only for .NET Core")]
    /// <summary>
    /// Delegate and is created from
    /// </summary>
    [SetupLinkerTrimMode("link")]

    // Check requirements for runtime-instantiated delegate types.
    [KeptBaseOnTypeInAssembly(PlatformAssemblies.CoreLib, typeof(MulticastDelegate), PlatformAssemblies.CoreLib, typeof(Delegate))]
    [KeptInterfaceOnTypeInAssembly(PlatformAssemblies.CoreLib, typeof(MulticastDelegate), PlatformAssemblies.CoreLib, typeof(ISerializable))]
    [KeptMemberInAssembly(PlatformAssemblies.CoreLib, typeof(Delegate), "GetHashCode()")]
    [KeptMemberInAssembly(PlatformAssemblies.CoreLib, typeof(Delegate), "Equals(System.Object)")]
    [KeptInterfaceOnTypeInAssembly(PlatformAssemblies.CoreLib, typeof(Delegate), PlatformAssemblies.CoreLib, typeof(ICloneable))]
    [KeptInterfaceOnTypeInAssembly(PlatformAssemblies.CoreLib, typeof(Delegate), PlatformAssemblies.CoreLib, typeof(ISerializable))]
    public class DelegateAndMulticastDelegateKeepInstantiatedReqs
    {
        public static void Main()
        {
            typeof(MulticastDelegate).ToString();

            // Cause the interfaces to be marked in order to eliminate the possibility of them being removed
            // due to no code path marking the interface type
            typeof(ISerializable).ToString();
            typeof(ICloneable).ToString();
        }
    }
}
