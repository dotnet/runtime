// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace TestIlasmRoundtrip
{
    public class Program
    {
        [Fact]
        public static void TestConstraintsNotDuplicated()
        {
            // Load the assembly containing the IL types
            Assembly asm = Assembly.Load("repro");
            
            // Get the types from the IL assembly (they are in the global namespace)
            Type ieqType = asm.GetType("IEq`1");
            Type icompType = asm.GetType("IComp`1");
            
            Assert.NotNull(ieqType);
            Assert.NotNull(icompType);

            // Get the generic type parameter for IEq<TSelf>
            Type[] ieqTypeParams = ieqType.GetGenericArguments();
            Assert.Single(ieqTypeParams);
            Type ieqTSelf = ieqTypeParams[0];

            // Get the constraints on IEq's TSelf parameter
            Type[] ieqConstraints = ieqTSelf.GetGenericParameterConstraints();
            
            // There should be exactly one constraint: IEq<TSelf>
            Assert.Single(ieqConstraints);
            Assert.True(ieqConstraints[0].IsGenericType);
            Assert.Equal(ieqType, ieqConstraints[0].GetGenericTypeDefinition());

            // Get the generic type parameter for IComp<TSelf>
            Type[] icompTypeParams = icompType.GetGenericArguments();
            Assert.Single(icompTypeParams);
            Type icompTSelf = icompTypeParams[0];

            // Get the constraints on IComp's TSelf parameter
            Type[] icompConstraints = icompTSelf.GetGenericParameterConstraints();
            
            // There should be exactly one constraint: IComp<TSelf>
            // After ilasm roundtrip, this becomes duplicated - that's the bug
            Assert.Single(icompConstraints);
            Assert.True(icompConstraints[0].IsGenericType);
            Assert.Equal(icompType, icompConstraints[0].GetGenericTypeDefinition());

            // Log the constraints for debugging
            Console.WriteLine($"IEq<TSelf> constraints count: {ieqConstraints.Length}");
            foreach (var constraint in ieqConstraints)
            {
                Console.WriteLine($"  - {constraint}");
            }

            Console.WriteLine($"IComp<TSelf> constraints count: {icompConstraints.Length}");
            foreach (var constraint in icompConstraints)
            {
                Console.WriteLine($"  - {constraint}");
            }
        }
    }
}
