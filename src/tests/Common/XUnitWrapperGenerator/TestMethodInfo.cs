// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace XUnitWrapperGenerator;

interface ITestMethodInfo
{
    string ExecutionStatement { get; }
}

sealed class StaticFactMethod : ITestMethodInfo
{
    public StaticFactMethod(IMethodSymbol method)
    {
        ExecutionStatement = $"{method.ContainingType.ToDisplayString()}.{method.Name}()";
    }

    public string ExecutionStatement { get; }

    public override bool Equals(object obj)
    {
        return obj is StaticFactMethod other && ExecutionStatement == other.ExecutionStatement;
    }
}

sealed class InstanceFactMethod : ITestMethodInfo
{
    public InstanceFactMethod(IMethodSymbol method)
    {
        ExecutionStatement = $"using ({method.ContainingType.ToDisplayString()} obj = new()) obj.{method.Name}();";
    }

    public string ExecutionStatement { get; }

    public override bool Equals(object obj)
    {
        return obj is InstanceFactMethod other && ExecutionStatement == other.ExecutionStatement;
    }
}
