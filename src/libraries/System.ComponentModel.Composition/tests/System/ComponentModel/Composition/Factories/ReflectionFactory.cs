// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.ComponentModel.Composition.Factories
{
    internal static partial class ReflectionFactory
    {
        public static ParameterInfo CreateParameter()
        {
            return CreateParameter((string)null);
        }

        public static ParameterInfo CreateParameter(Type parameterType)
        {
            return new MockParameterInfo(parameterType);
        }

        public static ParameterInfo CreateParameter(string name)
        {
            return new MockParameterInfo(name);
        }
    }
}
