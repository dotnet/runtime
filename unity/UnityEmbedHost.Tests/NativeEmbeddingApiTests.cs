// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

#if !TESTING_UNITY_CORECLR
[Ignore("This suite can only be ran against unity coreclr")]
#endif
[TestFixture]
public class NativeEmbeddingApiTests
{
    [Test]
    public void GCHandleNewAndGetTarget()
    {
        var obj = new object();
        var handle1 = CoreCLRHostNativeWrappers.gchandle_new_v2(obj, false);
        Assert.That(handle1, Is.Not.EqualTo(0));
        var result = CoreCLRHostNativeWrappers.gchandle_get_target_v2(handle1);
        Assert.That(obj, Is.EqualTo(result));

        var obj2 = new object();
        var handle2 = CoreCLRHostNativeWrappers.gchandle_new_v2(obj2, true);
        Assert.That(handle2, Is.Not.EqualTo(0));
        var result2 = CoreCLRHostNativeWrappers.gchandle_get_target_v2(handle2);
        Assert.That(obj2, Is.EqualTo(result2));

        Assert.That(handle1, Is.Not.EqualTo(handle2));

        GCHandle.FromIntPtr(handle1).Free();
        GCHandle.FromIntPtr(handle2).Free();
    }

    // Classes and classes
    [TestCase(typeof(object), typeof(object), true)]
    [TestCase(typeof(Mammal), typeof(Mammal), true)]
    [TestCase(typeof(Mammal), typeof(Anaimal), true)]
    [TestCase(typeof(Cat), typeof(Anaimal), true)]
    [TestCase(typeof(Rock), typeof(Anaimal), false)]

    // Classes and interfaces
    [TestCase(typeof(Mammal), typeof(IMammal), true)]
    [TestCase(typeof(Mammal), typeof(IAnimal), true)]
    [TestCase(typeof(Cat), typeof(IAnimal), true)]
    [TestCase(typeof(CatOnlyInterface), typeof(IAnimal), true)]

    [TestCase(typeof(Rock), typeof(IAnimal), false)]
    [TestCase(typeof(NoInterfaces), typeof(IAnimal), false)]
    [TestCase(typeof(object), typeof(IRock), false)]

    // Structs and ValueType
    [TestCase(typeof(ValueMammal), typeof(ValueType), true)]

    // Structs and interfaces
    [TestCase(typeof(ValueMammal), typeof(IMammal), true)]
    [TestCase(typeof(ValueMammal), typeof(IAnimal), true)]
    [TestCase(typeof(ValueCat), typeof(IAnimal), true)]

    [TestCase(typeof(ValueRock), typeof(IAnimal), false)]
    [TestCase(typeof(ValueNoInterfaces), typeof(IAnimal), false)]
    public void IsInst(Type obj, Type type, bool shouldBeInstanceOfType)
    {
        var instance = Activator.CreateInstance(obj)!;

        CheckObjectIsInstance(obj, type, shouldBeInstanceOfType, instance);
    }

    [TestCase(typeof(Mammal), 1, typeof(Mammal), 1, true)]
    [TestCase(typeof(Mammal), 1, typeof(Anaimal), 1, true)]

    [TestCase(typeof(Mammal), 1, typeof(Mammal), 3, false)]

    [TestCase(typeof(ValueMammal), 1, typeof(ValueMammal), 1, true)]
    [TestCase(typeof(ValueMammal), 1, typeof(IMammal), 1, false)]

    [TestCase(typeof(ValueMammal), 1, typeof(ValueMammal), 3, false)]

    [TestCase(typeof(Mammal), 1, typeof(IMammal), 1, true)]
    [TestCase(typeof(Mammal), 1, typeof(IAnimal), 1, true)]
    [TestCase(typeof(Mammal), 1, typeof(IMammal), 2, false)]
    public void IsInstArrays(Type obj, int rank, Type checkType, int checkRank, bool shouldBeInstanceOfType)
    {
        var instance = Array.CreateInstance(obj, new int[rank]);
        Type arrayType = Array.CreateInstance(checkType, new int[checkRank]).GetType();
        CheckObjectIsInstance(instance.GetType(), arrayType, shouldBeInstanceOfType, instance);
    }

    [TestCase(typeof(Mammal), typeof(Mammal), 1, false)]
    [TestCase(typeof(Mammal), typeof(Anaimal), 1, false)]

    [TestCase(typeof(Mammal), typeof(Mammal), 2, false)]

    [TestCase(typeof(ValueMammal),typeof(ValueMammal), 1, false)]
    [TestCase(typeof(ValueMammal), typeof(IMammal), 1, false)]

    [TestCase(typeof(ValueMammal), typeof(ValueMammal), 2, false)]

    [TestCase(typeof(Mammal), typeof(IMammal), 1, false)]
    [TestCase(typeof(Mammal), typeof(IAnimal), 1, false)]
    [TestCase(typeof(Mammal), typeof(IMammal), 2, false)]
    public void IsInstNoneArrayToArrays(Type obj, Type checkType, int checkRank, bool shouldBeInstanceOfType)
    {
        var instance = Activator.CreateInstance(obj)!;

        CheckObjectIsInstance(instance.GetType(), Array.CreateInstance(checkType, new int[checkRank]).GetType(), shouldBeInstanceOfType, instance);
    }

    [TestCase(typeof(Mammal), 1, typeof(Mammal), false)]
    [TestCase(typeof(Mammal), 1, typeof(Anaimal), false)]

    [TestCase(typeof(Mammal), 1, typeof(Mammal), false)]

    [TestCase(typeof(ValueMammal), 1, typeof(ValueMammal), false)]
    [TestCase(typeof(ValueMammal), 1, typeof(IMammal), false)]

    [TestCase(typeof(ValueMammal), 1, typeof(ValueMammal), false)]

    [TestCase(typeof(Mammal), 1, typeof(IMammal), false)]
    [TestCase(typeof(Mammal), 1, typeof(IAnimal), false)]
    [TestCase(typeof(Mammal), 1, typeof(IMammal),  false)]
    public void IsInstArrayToNoneArray(Type obj, int rank, Type checkType, bool shouldBeInstanceOfType)
    {
        var instance = Array.CreateInstance(obj, new int[rank]);

        CheckObjectIsInstance(instance.GetType(), checkType, shouldBeInstanceOfType, instance);
    }

    private static void CheckObjectIsInstance(Type obj, Type type, bool shouldBeInstanceOfType, object instance)
    {
        var result = CoreCLRHostNativeWrappers.object_isinst(instance, type);

        if (shouldBeInstanceOfType)
        {
            if (result == null)
                Assert.Fail($"Expected {obj} to be of type {type}, but {nameof(CoreCLRHost.object_isinst)} claimed it wasn't");

            Assert.That(result, Is.EqualTo(instance));
        }
        else
        {
            if (result != null)
                Assert.Fail($"Expected {obj} to NOT be of type {type}, but {nameof(CoreCLRHost.object_isinst)} claimed it was");

            Assert.That(result, Is.Null);
        }
    }

    [TestCase(typeof(Anaimal))]
    [TestCase(typeof(ValueAnimal))]
    public void GetClass(Type type)
    {
        Assert.That(CoreCLRHostNativeWrappers.object_get_class(Activator.CreateInstance(type)!), Is.EqualTo(type));
    }
}
