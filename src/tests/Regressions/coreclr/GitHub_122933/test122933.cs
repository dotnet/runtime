// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

// Validates that ilasm/ildasm roundtrip does not duplicate self-referential constraints

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
        Type ieqType = typeof(IEq<>);
        Type icompType = typeof(IComp<>);

        Type[] ieqTypeParams = ieqType.GetGenericArguments();
        if (ieqTypeParams.Length != 1)
        {
            Console.WriteLine($"FAIL: IEq should have 1 generic parameter, but has {ieqTypeParams.Length}");
            return -1;
        }
        Type ieqTSelf = ieqTypeParams[0];

        Type[] ieqConstraints = ieqTSelf.GetGenericParameterConstraints();
        
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

        Type[] icompTypeParams = icompType.GetGenericArguments();
        if (icompTypeParams.Length != 1)
        {
            Console.WriteLine($"FAIL: IComp should have 1 generic parameter, but has {icompTypeParams.Length}");
            return -1;
        }
        Type icompTSelf = icompTypeParams[0];

        Type[] icompConstraints = icompTSelf.GetGenericParameterConstraints();
        
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
