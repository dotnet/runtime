// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerators.Tests;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class TypeSystemProxyTests
    {
        private static CSharpCompilation CreateCompilation() => CSharpCompilation.Create("ProxyTests",
            new[] { CSharpSyntaxTree.ParseText("public class C<T> { public void M(T first, int second) { } public void N(T first) { } }") },
            LiveReferencePack.GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        [Fact]
        public void TypeEqualityPreservesNullableAndDefaultSemantics()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol type = compilation.GetTypeByMetadataName("C`1")!;
            Check(Create("TypeProxy", type), Create("TypeProxy", type), equal: true);
            Check(Create("TypeProxy", type), Create("TypeProxy", type.WithNullableAnnotation(NullableAnnotation.Annotated)), equal: true);
            Check(Create("TypeProxy", type), Create("TypeProxy", compilation.GetSpecialType(SpecialType.System_Object)), equal: false);
            Check(Create("TypeProxy"), Create("TypeProxy"), equal: true);
            Check(Create("TypeProxy"), Create("TypeProxy", type), equal: false);
        }

        [Fact]
        public void MethodParameterAndGenericParameterEqualityPreserveIdentity()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol type = compilation.GetTypeByMetadataName("C`1")!;
            IMethodSymbol method = (IMethodSymbol)type.GetMembers("M").Single();
            IMethodSymbol other = (IMethodSymbol)type.GetMembers("N").Single();
            Check(Create("MethodProxy", method), Create("MethodProxy", method), equal: true);
            Check(Create("MethodProxy", method), Create("MethodProxy", other), equal: false);
            Check(Create("MethodProxy"), Create("MethodProxy"), equal: true);
            Check(Create("ParameterProxy", method.Parameters[0], method), Create("ParameterProxy", method.Parameters[0], method), equal: true);
            Check(Create("ParameterProxy", method.Parameters[0], method), Create("ParameterProxy", method.Parameters[1], method), equal: false);
            Check(Create("ParameterProxy", method.Parameters[0], method), Create("ParameterProxy", other.Parameters[0], other), equal: false);
            Check(Create("ParameterProxy"), Create("ParameterProxy"), equal: true);
            Check(Create("GenericParameterProxy", type.TypeParameters[0]), Create("GenericParameterProxy", type.TypeParameters[0]), equal: true);
            Check(Create("GenericParameterProxy"), Create("GenericParameterProxy"), equal: true);
            INamedTypeSymbol differentCompilationType = CreateCompilation().GetTypeByMetadataName("C`1")!;
            Check(Create("TypeProxy", type), Create("TypeProxy", differentCompilationType), equal: false);
            Check(Create("GenericParameterProxy", type.TypeParameters[0]), Create("GenericParameterProxy", differentCompilationType.TypeParameters[0]), equal: false);
        }

        private static object Create(string name, params object[] arguments)
        {
            Type type = typeof(DynamicallyAccessedMembersAnalyzer).Assembly.GetType("ILLink.Shared.TypeSystemProxy." + name, throwOnError: true)!;
            return Activator.CreateInstance(type, arguments)!;
        }

        private static void Check(object left, object right, bool equal)
        {
            // A friend assembly would also expose the analyzer's nullable polyfill attributes.
            typeof(TypeSystemProxyTests).GetMethod(nameof(CheckTyped), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(left.GetType()).Invoke(null, new[] { left, right, equal });
        }

        private static void CheckTyped<T>(T left, T right, bool equal) where T : struct, IEquatable<T>
        {
            Assert.Equal(equal, left.Equals(right));
            Assert.Equal(equal, right.Equals(left));
            Assert.Equal(equal, left.Equals((object)right));
            Assert.Equal(equal, EqualityComparer<T>.Default.Equals(left, right));
            if (equal)
                Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }
    }
}
