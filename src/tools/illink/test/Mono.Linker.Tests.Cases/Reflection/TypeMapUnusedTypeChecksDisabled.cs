// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: TypeMap<Mono.Linker.Tests.Cases.Reflection.TypeMapUnusedTypeChecksDisabled.Group>(
    "TypeCheck",
    typeof(Mono.Linker.Tests.Cases.Reflection.TypeMapUnusedTypeChecksDisabled.Target),
    typeof(Mono.Linker.Tests.Cases.Reflection.TypeMapUnusedTypeChecksDisabled.TrimTarget))]
[assembly: KeptAttributeAttribute(
    typeof(TypeMapAttribute<Mono.Linker.Tests.Cases.Reflection.TypeMapUnusedTypeChecksDisabled.Group>),
    "TypeCheck",
    typeof(Mono.Linker.Tests.Cases.Reflection.TypeMapUnusedTypeChecksDisabled.Target),
    typeof(Mono.Linker.Tests.Cases.Reflection.TypeMapUnusedTypeChecksDisabled.TrimTarget))]

namespace Mono.Linker.Tests.Cases.Reflection
{
    [SetupLinkerArgument("--disable-opt", "unusedtypechecks")]
    class TypeMapUnusedTypeChecksDisabled
    {
        static void Main(string[] args)
        {
            object value = GetValue(args);
            if (value is TrimTarget)
            {
                Console.WriteLine(value);
            }

            Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<Group>());
        }

        [Kept]
        static object GetValue(string[] args)
        {
            return args.Length == 0 ? null! : new object();
        }

        [Kept]
        public class Group;

        [Kept]
        public class Target;

        [Kept]
        public class TrimTarget;
    }
}
