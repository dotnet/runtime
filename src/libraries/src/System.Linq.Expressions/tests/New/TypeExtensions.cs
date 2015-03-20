// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Tests.ExpressionCompiler
{
    public static class TypeExtensions
    {
        // This implementation is simplistic, but sufficient for the tests in this assembly.
        public static ConstructorInfo GetConstructor(this Type _this, Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException("types");
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            }

            foreach (ConstructorInfo candidate in _this.GetTypeInfo().DeclaredConstructors)
            {
                if (!candidate.IsPublic)
                    continue;
                if (candidate.IsStatic)
                    continue;
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != types.Length)
                    continue;
                bool foundMismatch = false;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != types[i])
                    {
                        foundMismatch = true;
                        break;
                    }
                }
                if (foundMismatch)
                    continue;
                return candidate;
            }
            return null;
        }
    }
}
