// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace UnityEmbedHost.Tests;

class Mammal : Animal, IMammal
{
    public void BreathAir()
    {
    }
}

class Cat : Mammal, ICat
{
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
