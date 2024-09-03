// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TestLibrary;

using Xunit;

// This test shares its logic with the managed type system test suite, and seeks to ensure the runtime agrees with it
namespace istypeequivalent
{
    public class Test
    {
        private static IEnumerable<Type> GetAllTypesInNamespace(Module module, string @namespace)
        {
            foreach (var type in module.GetTypes())
            {
                if (type.Namespace == @namespace)
                {
                    Console.WriteLine($"Found {type}");
                    yield return type;
                }
            }
        }

        private static string GetTypeIdentiferFromType(Type type)
        {
            foreach (var ca in type.GetCustomAttributes())
            {
                if (ca is TypeIdentifierAttribute typeId)
                {
                    return $"{typeId.Scope}_{typeId.Identifier}";
                }
            }

            return null;
        }

        private static Dictionary<string, Type> GetTypeIdentifierAssociatedTypesInNamespace(Module module, string @namespace)
        {
            Dictionary<string, Type> result = new Dictionary<string, Type>();
            foreach (var typeDef in GetAllTypesInNamespace(module, @namespace))
            {
                string typeId = GetTypeIdentiferFromType(typeDef);
                if (typeId != null)
                {
                    result.Add(typeId, typeDef);
                }
            }
            return result;
        }

        private static IEnumerable<ValueTuple<Type, Type>> GetTypesWhichClaimMatchingTypeIdentifiersInNamespace(string @namespace)
        {
            var module1Types = GetTypeIdentifierAssociatedTypesInNamespace(typeof(TypeEquivalenceAssembly1).Module, @namespace);
            var module2Types = GetTypeIdentifierAssociatedTypesInNamespace(typeof(TypeEquivalenceAssembly2).Module, @namespace);

            foreach (var data in module1Types)
            {
                if (module2Types.TryGetValue(data.Key, out var typeDef2))
                {
                    yield return (data.Value, typeDef2);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public static void TestTypesWhichShouldMatch()
        {
            foreach (var typePair in GetTypesWhichClaimMatchingTypeIdentifiersInNamespace("TypesWhichMatch"))
            {
                Console.WriteLine($"Comparing {typePair.Item1} to {typePair.Item2}");
                Assert.NotEqual(typePair.Item1, typePair.Item2);
                Assert.True(typePair.Item1.IsEquivalentTo(typePair.Item2));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public static void TestTypesWhichShouldNotMatch()
        {
            foreach (var typePair in GetTypesWhichClaimMatchingTypeIdentifiersInNamespace("TypesWhichDoNotMatch"))
            {
                Console.WriteLine($"Comparing {typePair.Item1} to {typePair.Item2}");
                Assert.False(typePair.Item1.IsEquivalentTo(typePair.Item2));
            }
        }
    }
}
