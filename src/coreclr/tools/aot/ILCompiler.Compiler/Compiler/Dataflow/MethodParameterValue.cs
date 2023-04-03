// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILCompiler.Dataflow;
using ILLink.Shared.TypeSystemProxy;
using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{

    /// <summary>
    /// A value that came from a method parameter - such as the result of a ldarg.
    /// </summary>
    internal partial record MethodParameterValue : IValueWithStaticType
    {
        public MethodParameterValue(ParameterProxy param, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes, bool overrideIsThis = false)
        {
            StaticType = param.ParameterType;
            Parameter = param;
            DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
            _overrideIsThis = overrideIsThis;
        }

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public TypeDesc? StaticType { get; }
    }
}
