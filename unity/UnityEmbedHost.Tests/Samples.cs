// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace UnityEmbedHost.Tests;

[Obsolete]
class Bacon
{
    [Obsolete]
    public static void Fry()
    {
    }

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
}

class Cat : Mammal, ICat
{
    public int EarCount = 0;

    public static int StaticField = 0;

    public void Meow()
    {
    }
}

class CatOnlyInterface : ICat
{
}

class Rock : IRock
{
}

class Animal : IAnimal
{
}

class NoInterfaces
{
}

interface IAnimal
{
}

interface IMammal : IAnimal
{
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
}

struct ValueCat : ICat
{
}

struct ValueRock : IRock
{
}

struct ValueAnimal : IAnimal
{
}

struct ValueNoInterfaces
{
}
