// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

// Reference the IL types so they are loaded
public interface IEq<TSelf> where TSelf : IEq<TSelf>? { }
public interface IComp<TSelf> : IEq<TSelf> where TSelf : IComp<TSelf>? { }

namespace TestIlasmRoundtrip
{
    public class Program
    {
        [Fact]
        public static void TestConstraintsNotDuplicated()
        {
            // Load the types from the IL assembly
            Type ieqType = typeof(IEq<>);
            Type icompType = typeof(IComp<>);

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
