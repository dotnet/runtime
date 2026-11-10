// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the RuntimeTypeSystem and Loader contracts.
/// Loads types with inheritance, generics, and arrays, then crashes.
/// </summary>
internal static class Program
{
    // Base class hierarchy
    public class Animal
    {
        public virtual string Name => "Animal";
    }

    public class Dog : Animal
    {
        public override string Name => "Dog";
        public string Breed { get; set; } = "Unknown";
    }

    public class GuideDog : Dog
    {
        public string Handler { get; set; } = "None";
    }

    // Generic types
    public class Container<T>
    {
        public T? Value { get; set; }
    }

    public class Pair<TKey, TValue>
    {
        public TKey? Key { get; set; }
        public TValue? Value { get; set; }
    }

    // Interface hierarchy
    public interface IIdentifiable
    {
        int Id { get; }
    }

    public class IdentifiableAnimal : Animal, IIdentifiable
    {
        public int Id { get; set; }
    }

    private static void Main()
    {
        // Create instances so the runtime loads and lays out these types
        var dog = new Dog { Breed = "Labrador" };
        var guideDog = new GuideDog { Handler = "John", Breed = "Shepherd" };
        var container = new Container<int> { Value = 42 };
        var pair = new Pair<string, Dog> { Key = "Rex", Value = dog };
        var idAnimal = new IdentifiableAnimal { Id = 1 };

        // Arrays of various types
        int[] intArray = new[] { 1, 2, 3, 4, 5 };
        string[] stringArray = new[] { "hello", "world" };
        Dog[] dogArray = new[] { dog, guideDog };

        // Multi-dimensional array
        int[,] matrix = new int[3, 3];

        // Generic collections
        var list = new List<Animal> { dog, guideDog, idAnimal };
        var dict = new Dictionary<string, Animal>
        {
            ["dog"] = dog,
            ["guide"] = guideDog,
        };

        // Keep references alive
        GC.KeepAlive(dog);
        GC.KeepAlive(guideDog);
        GC.KeepAlive(container);
        GC.KeepAlive(pair);
        GC.KeepAlive(idAnimal);
        GC.KeepAlive(intArray);
        GC.KeepAlive(stringArray);
        GC.KeepAlive(dogArray);
        GC.KeepAlive(matrix);
        GC.KeepAlive(list);
        GC.KeepAlive(dict);

        Environment.FailFast("cDAC dump test: TypeHierarchy debuggee intentional crash");
    }
}
