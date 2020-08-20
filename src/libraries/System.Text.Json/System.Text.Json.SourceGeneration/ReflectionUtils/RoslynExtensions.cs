// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public static class RoslynExtensions
    {
        public static Dictionary<string, Type> PrimitiveTypes = new Dictionary<string, Type>()
        {
            { "Boolean", typeof(bool) },
            { "Int32", typeof(int) },
            { "Int64", typeof(long) },
            { "Double", typeof(double) },
            { "Char", typeof(char) },
            { "String", typeof(string) },
            { "DateTime", typeof(DateTime) },
            { "DateTimeOffset", typeof(DateTimeOffset) },
        };
        public static Type AsType(this ITypeSymbol typeSymbol, MetadataLoadContext metadataLoadContext) => (
            typeSymbol == null ? null : (PrimitiveTypes.ContainsKey(typeSymbol.MetadataName)? PrimitiveTypes[typeSymbol.Name] : new TypeWrapper(typeSymbol, metadataLoadContext)))!;

        public static ParameterInfo AsParameterInfo(this IParameterSymbol parameterSymbol, MetadataLoadContext metadataLoadContext) => (parameterSymbol == null ? null : new ParameterInfoWrapper(parameterSymbol, metadataLoadContext))!;

        public static MethodInfo AsMethodInfo(this IMethodSymbol methodSymbol, MetadataLoadContext metadataLoadContext) => (methodSymbol == null ? null : new MethodInfoWrapper(methodSymbol, metadataLoadContext))!;


        public static IEnumerable<INamedTypeSymbol> BaseTypes(this INamedTypeSymbol typeSymbol)
        {
            var t = typeSymbol;
            while (t != null)
            {
                yield return t;
                t = t.BaseType;
            }
        }
    }
}
