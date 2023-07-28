using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

[assembly: TypeForwardedTo (typeof (MyEnum))]

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
    public class UsedToReferenceForwarderAssembly
    {
    }
}