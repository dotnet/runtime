// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Logging.Abstractions.Internal
{
    public class TypeNameHelperTest
    {
        public static TheoryData<Type, string> FullTypeNameData
        {
            get
            {
                return new TheoryData<Type, string>
                {
                    // Predefined Types
                    { typeof(int), "int" },
                    { typeof(List<int>), "System.Collections.Generic.List" },
                    { typeof(Dictionary<int, string>), "System.Collections.Generic.Dictionary" },
                    { typeof(Dictionary<int, List<string>>), "System.Collections.Generic.Dictionary" },
                    { typeof(List<List<string>>), "System.Collections.Generic.List" },

                    // Classes inside NonGeneric class
                    { typeof(A), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.A" },
                    { typeof(B<int>), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.B" },
                    { typeof(C<int, string>), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.C" },
                    { typeof(C<int, B<string>>), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.C" },
                    { typeof(B<B<string>>), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.B" },

                    // Classes inside Generic class
                    { typeof(Outer<int>.D), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.Outer.D" },
                    { typeof(Outer<int>.E<int>), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.Outer.E" },
                    { typeof(Outer<int>.F<int, string>), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.Outer.F" },
                    { typeof(Outer<int>.F<int, Outer<int>.E<string>>),"Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.Outer.F" },
                    { typeof(Outer<int>.E<Outer<int>.E<string>>), "Microsoft.Extensions.Logging.Abstractions.Internal.TypeNameHelperTest.Outer.E" }
                };
            }
        }

        [Theory]
        [MemberData(nameof(FullTypeNameData))]
        public void Can_PrettyPrint_FullTypeName(Type type, string expectedTypeName)
        {
            // Arrange & Act
            var displayName = TypeNameHelper.GetTypeDisplayName(type);

            // Assert
            Assert.Equal(expectedTypeName, displayName);
        }

        public static TheoryData<Type, string> BuiltInTypesData
        {
            get
            {
                return new TheoryData<Type, string>
                {
                    // Predefined Types
                    { typeof(bool), "bool" },
                    { typeof(byte), "byte" },
                    { typeof(char), "char" },
                    { typeof(decimal), "decimal" },
                    { typeof(double), "double" },
                    { typeof(float), "float" },
                    { typeof(int), "int" },
                    { typeof(long), "long" },
                    { typeof(object), "object" },
                    { typeof(sbyte), "sbyte" },
                    { typeof(short), "short" },
                    { typeof(string), "string" },
                    { typeof(uint), "uint" },
                    { typeof(ulong), "ulong" },
                    { typeof(ushort), "ushort" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(BuiltInTypesData))]
        public void ReturnsCommonName_ForBuiltinTypes(Type type, string expectedTypeName)
        {
            // Arrange & Act
            var displayName = TypeNameHelper.GetTypeDisplayName(type);

            // Assert
            Assert.Equal(expectedTypeName, displayName);
        }

        private class A { }

        private class B<T> { }

        private class C<T1, T2> { }

        private class Outer<T>
        {
            public class D { }

            public class E<T1> { }

            public class F<T1, T2> { }
        }
    }
}