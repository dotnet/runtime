// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
