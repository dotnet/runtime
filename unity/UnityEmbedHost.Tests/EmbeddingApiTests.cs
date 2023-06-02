using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

[TestFixture]
public class EmbeddingApiTests : BaseEmbeddingApiTests
{
    internal override ICoreCLRHostWrapper ClrHost { get; } = new CoreCLRHostWrappers();

    /// <summary>
    /// Only run these during the direct call tests rather than the round trip through native code.  This is necessary because they are testing throw cases
    /// and if we go through native code, the exception will result in an Environment.Exit
    /// </summary>
    /// <param name="objType"></param>
    /// <param name="baseType"></param>
    /// <param name="methodName"></param>
    // Base Classes - Generic
    [TestCase(typeof(GenericCat<int,string>), typeof(GenericAnimal<int,string>), nameof(GenericAnimal<int, string>.VirtualMethodOnGenericAnimal))]
    // Interfaces - Generic
    [TestCase(typeof(GenericCat<int,string>), typeof(IGenericAnimal<int,string>), nameof(IGenericAnimal<int, string>.InterfaceMethodOnIGenericAnimal))]
    [TestCase(typeof(GenericCat<int,string>), typeof(IGenericAnimal<int, string>), nameof(IGenericAnimal<int, string>.InterfaceMethodOnGenericAnimalExplicitlyImplemented))]
    [TestCase(typeof(GenericCat<int,string>), typeof(IGenericAnimal<int>), nameof(IGenericAnimal<int>.InterfaceMethodOnIGenericAnimal))]
    [TestCase(typeof(GenericCat<int,string>), typeof(IGenericAnimal<int>), nameof(IGenericAnimal<int>.InterfaceMethodOnGenericAnimalExplicitlyImplemented))]
    public void ObjectGetVirtualMethodThrowsForGenericMethod(Type objType, Type baseType, string methodName)
    {
        var obj = Activator.CreateInstance(objType);
        var baseMethodInfo = baseType.FindInstanceMethodByName(methodName, Array.Empty<Type>());
        Assert.Throws<ArgumentException>(() => ClrHost.object_get_virtual_method(obj, baseMethodInfo.MethodHandle));
    }
}
