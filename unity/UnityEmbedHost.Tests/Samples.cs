// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace UnityEmbedHost.Tests;

class FooParentAttribute : Attribute
{ }

class FooAttribute : FooParentAttribute
{ }

[Obsolete]
[Foo]
class Bacon
{
    [Obsolete]
    [FooParent]
    public bool applewood = false;
    [Foo]
    public bool hickory = false;

    [Obsolete]
    [FooParent]
    public static void Fry()
    {
    }

    [Foo]
    public static void Smoke()
    {
    }
}

class Mammal : Animal, IMammal
{
    public int EyeCount = 2;
    public void BreathAir()
    {
    }

    public override void AbstractOnAnimal() => throw new NotImplementedException();
    public void InterfaceMethodOnIMammal() => throw new NotImplementedException();

    void IAnimal.ExplicitlyImplementedByMany() => throw new NotImplementedException();
    void IMammal.ExplicitlyImplementedByMany() => throw new NotImplementedException();
}

class Cat : Mammal, ICat
{
    public int EarCount = 0;

    public static int StaticField = 0;

    public void Meow()
    {
    }

    public virtual void VirtualMethodOnCat()
    {
    }

    public void NonVirtualMethodOnCat()
    {
    }

    public override void AbstractOnAnimal() => throw new NotImplementedException();
    void IAnimal.ExplicitlyImplementedByMany() => throw new NotImplementedException();
    void IMammal.ExplicitlyImplementedByMany() => throw new NotImplementedException();
}

class CatOnlyInterface : ICat
{
    public void InterfaceMethodOnIAnimal() => throw new NotImplementedException();
    void IAnimal.InterfaceMethodOnAnimalExplicitlyImplemented() => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(int p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(string p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(int p, int p2) => throw new NotImplementedException();

    void IMammal.ExplicitlyImplementedByMany() => throw new NotImplementedException();

    void IAnimal.ExplicitlyImplementedByMany() => throw new NotImplementedException();

    public void InterfaceMethodOnIMammal() => throw new NotImplementedException();
}

class Rock : IRock
{
}

abstract class Animal : IAnimal
{
    public void NonVirtualMethodOnAnimal()
    {
    }

    public virtual void VirtualOnAnimalNotOverridden() => throw new NotImplementedException();
    public abstract void AbstractOnAnimal();
    public void InterfaceMethodOnIAnimal() => throw new NotImplementedException();
    void IAnimal.InterfaceMethodOnAnimalExplicitlyImplemented() => throw new NotImplementedException();

    void IAnimal.ExplicitlyImplementedByMany() => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(int p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(string p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(int p, int p2) => throw new NotImplementedException();
}

class NoInterfaces
{
}

class ImposterCat
{
    void InterfaceMethodOnIAnimalWithParameters(int p)
    {
    }

    void InterfaceMethodOnIAnimalWithParameters(string p)
    {
    }

    void InterfaceMethodOnIAnimalWithParameters(int p, int p2)
    {
    }
}

class GenericCat<T, K> : GenericAnimal<T, K>
{
    public override void InterfaceMethodOnIGenericAnimal() => throw new NotImplementedException();

    public override void VirtualMethodOnGenericAnimal() => throw new NotImplementedException();
}

class GenericAnimal<T1, T2> : IGenericAnimal<T1>, IGenericAnimal<T1, T2>
{
    public virtual void InterfaceMethodOnIGenericAnimal() => throw new NotImplementedException();

    public virtual void VirtualMethodOnGenericAnimal() => throw new NotImplementedException();

    void IGenericAnimal<T1, T2>.InterfaceMethodOnGenericAnimalExplicitlyImplemented() => throw new NotImplementedException();


    void IGenericAnimal<T1>.InterfaceMethodOnGenericAnimalExplicitlyImplemented() => throw new NotImplementedException();

}

interface IGenericAnimal<T1>
{
    void InterfaceMethodOnIGenericAnimal();

    void InterfaceMethodOnGenericAnimalExplicitlyImplemented();

}

interface IGenericAnimal<T1, T2>
{
    void InterfaceMethodOnIGenericAnimal();

    void InterfaceMethodOnGenericAnimalExplicitlyImplemented();

}

interface IImposterAnimal
{
    void InterfaceMethodOnIAnimal();
}

interface IAnimal
{
    void InterfaceMethodOnIAnimal();

    void InterfaceMethodOnAnimalExplicitlyImplemented();

    void ExplicitlyImplementedByMany();

    void InterfaceMethodOnIAnimalWithParameters(int p);

    void InterfaceMethodOnIAnimalWithParameters(string p);

    void InterfaceMethodOnIAnimalWithParameters(int p, int p2);
}

interface IMammal : IAnimal
{
    void InterfaceMethodOnIMammal();

    new void ExplicitlyImplementedByMany();
}

interface ICat : IMammal
{
}

interface IRock
{
}

struct MyStruct
{

}

struct ValueMammal : IMammal
{
    public void InterfaceMethodOnIAnimal() => throw new NotImplementedException();
    void IAnimal.InterfaceMethodOnAnimalExplicitlyImplemented() => throw new NotImplementedException();

    void IMammal.ExplicitlyImplementedByMany() => throw new NotImplementedException();
    public void InterfaceMethodOnIAnimalWithParameters(int p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(string p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(int p, int p2) => throw new NotImplementedException();

    void IAnimal.ExplicitlyImplementedByMany() => throw new NotImplementedException();

    public void InterfaceMethodOnIMammal() => throw new NotImplementedException();
}

struct ValueCat : ICat
{
    public void InterfaceMethodOnIAnimal() => throw new NotImplementedException();
    void IAnimal.InterfaceMethodOnAnimalExplicitlyImplemented() => throw new NotImplementedException();
    public void InterfaceMethodOnIAnimalWithParameters(int p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(string p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(int p, int p2) => throw new NotImplementedException();

    void IMammal.ExplicitlyImplementedByMany() => throw new NotImplementedException();

    void IAnimal.ExplicitlyImplementedByMany() => throw new NotImplementedException();

    public void InterfaceMethodOnIMammal() => throw new NotImplementedException();

    public override string ToString() => "ValueCat";
}

struct ValueRock : IRock
{
}

struct ValueAnimal : IAnimal
{
    public void InterfaceMethodOnIAnimal() => throw new NotImplementedException();

    void IAnimal.InterfaceMethodOnAnimalExplicitlyImplemented() => throw new NotImplementedException();
    void IAnimal.ExplicitlyImplementedByMany() => throw new NotImplementedException();
    public void InterfaceMethodOnIAnimalWithParameters(int p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(string p) => throw new NotImplementedException();

    public void InterfaceMethodOnIAnimalWithParameters(int p, int p2) => throw new NotImplementedException();
}

struct ValueNoInterfaces
{
}
