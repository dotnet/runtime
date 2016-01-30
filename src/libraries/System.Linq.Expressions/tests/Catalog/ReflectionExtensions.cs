// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    static class ReflectionExtensions
    {
        public static ConstructorInfo GetDeclaredConstructor(this TypeInfo type, Type[] parameterTypes)
        {
            return type.DeclaredConstructors.SingleOrDefault(c => c.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes));
        }
    }
}
