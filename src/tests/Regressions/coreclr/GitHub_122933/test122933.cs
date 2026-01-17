// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

// Small repro for ilasm/ildasm roundtrip issue with duplicated constraints
// This demonstrates the issue where constraints get duplicated during ilasm roundtrip
public interface IComp<TSelf>
    : IEq<TSelf>
        where TSelf : IComp<TSelf>?
{
}

public interface IEq<TSelf>
        where TSelf : IEq<TSelf>?
{
}

public class Test122933
{
    [Fact]
    public static int TestEntryPoint()
    {
        // Get the types
        Type ieqType = typeof(IEq<>);
        Type icompType = typeof(IComp<>);

        // Get the generic type parameter for IEq<TSelf>
        Type[] ieqTypeParams = ieqType.GetGenericArguments();
        if (ieqTypeParams.Length != 1)
        {
            Console.WriteLine($"FAIL: IEq should have 1 generic parameter, but has {ieqTypeParams.Length}");
            return -1;
        }
        Type ieqTSelf = ieqTypeParams[0];

        // Get the constraints on IEq's TSelf parameter
        Type[] ieqConstraints = ieqTSelf.GetGenericParameterConstraints();
        
        // There should be exactly one constraint: IEq<TSelf>
        if (ieqConstraints.Length != 1)
        {
            Console.WriteLine($"FAIL: IEq<TSelf> should have 1 constraint, but has {ieqConstraints.Length}");
            return -1;
        }

        if (ieqConstraints[0].GetGenericTypeDefinition() != ieqType)
        {
            Console.WriteLine($"FAIL: IEq<TSelf> constraint should be IEq<>, but is {ieqConstraints[0].GetGenericTypeDefinition()}");
            return -1;
        }

        // Get the generic type parameter for IComp<TSelf>
        Type[] icompTypeParams = icompType.GetGenericArguments();
        if (icompTypeParams.Length != 1)
        {
            Console.WriteLine($"FAIL: IComp should have 1 generic parameter, but has {icompTypeParams.Length}");
            return -1;
        }
        Type icompTSelf = icompTypeParams[0];

        // Get the constraints on IComp's TSelf parameter
        Type[] icompConstraints = icompTSelf.GetGenericParameterConstraints();
        
        // There should be exactly one constraint: IComp<TSelf>
        // After ilasm roundtrip, this becomes duplicated - that's the bug
        if (icompConstraints.Length != 1)
        {
            Console.WriteLine($"FAIL: IComp<TSelf> should have 1 constraint, but has {icompConstraints.Length}");
            return -1;
        }

        if (icompConstraints[0].GetGenericTypeDefinition() != icompType)
        {
            Console.WriteLine($"FAIL: IComp<TSelf> constraint should be IComp<>, but is {icompConstraints[0].GetGenericTypeDefinition()}");
            return -1;
        }

        return 100;
    }
}
