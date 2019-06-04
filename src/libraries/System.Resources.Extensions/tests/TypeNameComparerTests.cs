// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Resources.Extensions.Tests
{
    public class TypeNameComparerTests
    {
        [Theory]
        [InlineData("System.String", "System.String")]
        [InlineData("System.String", "System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
        [InlineData("System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.String")]
        [InlineData("System.String, mscorlib, Version=4.0.0.0, Culture=bogus, PublicKeyToken=b77a5c561934e089", "System.String")]
        [InlineData("System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=blahblahblah", "System.String")]
        [InlineData("System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.String")]
        [InlineData("System.String, mscorlib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.String")]
        [InlineData("System.String, mscorlib", "System.String")]
        [InlineData("System.String, mscorlib,", "System.String")]
        [InlineData("System.String, mscorlib ,", "System.String")]
        public static void IgnoresMscorlib(string typeName1, string typeName2)
        {
            Assert.Equal(typeName1, typeName2, TypeNameComparer.Instance);

            Assert.Equal(TypeNameComparer.Instance.GetHashCode(typeName1), TypeNameComparer.Instance.GetHashCode(typeName2));
        }

        [Theory]
        [InlineData("System.String", "System.String, , Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
        [InlineData("System.String", "System.String, , Version=4.0.0.0")]
        [InlineData("System.String", "System.String, ,")]
        public static void DoesNotIgnoreMissingSimpleName(string typeName1, string typeName2)
        {
            Assert.NotEqual(typeName1, typeName2, TypeNameComparer.Instance);
        }

        [Theory]
        [InlineData(",MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData(", MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData(",\tMyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData(", \t\r\nMyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData(", MyAssembly\t\r\n , Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        public static void IgnoresWhiteSpaceInAssemblyName(string assemblyNamePortion)
        {
            string typeName = "MyNamespace.MyType";
            string expectedAssemblyName = "MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef";
            string typeName1 = $"{typeName}, {expectedAssemblyName}";
            string typeName2 = $"{typeName}{assemblyNamePortion}";
            Assert.Equal(typeName1, typeName2, TypeNameComparer.Instance);
            Assert.Equal(TypeNameComparer.Instance.GetHashCode(typeName1), TypeNameComparer.Instance.GetHashCode(typeName2));
        }

        [Theory]
        [InlineData(" MyNamespace.MyType")]
        [InlineData("\tMyNamespace.MyType")]
        [InlineData("\rMyNamespace.MyType")]
        [InlineData("\nMyNamespace.MyType")]
        [InlineData("  \n\r\t  \r\n  \t\tMyNamespace.MyType")]
        public static void IgnoresLeadingSpceInTypeName(string typeNamePortion)
        {
            string expectedtypeName = "MyNamespace.MyType";
            string assemblyName = "MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef";
            string typeName1 = $"{expectedtypeName}, {assemblyName}";
            string typeName2 = $"{typeNamePortion}, {assemblyName}";
            Assert.Equal(typeName1, typeName2, TypeNameComparer.Instance);
            Assert.Equal(TypeNameComparer.Instance.GetHashCode(typeName1), TypeNameComparer.Instance.GetHashCode(typeName2));
        }

        [Theory]
        [InlineData("MyNamespace.MyType ")]
        [InlineData("MyNamespace.MyType  ")]
        [InlineData("MyNamespace.MyType\t")]
        [InlineData("MyNamespace.MyType\r")]
        [InlineData("MyNamespace.MyType\n")]
        public static void DoesNotIgnoreTrailingSpceInTypeName(string typeNamePortion)
        {
            string expectedtypeName = "MyNamespace.MyType";
            string assemblyName = "MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef";
            Assert.NotEqual($"{expectedtypeName}, {assemblyName}", $"{typeNamePortion}, {assemblyName}", TypeNameComparer.Instance);
        }

        [Theory]
        [InlineData("MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData("MyAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData("MyAssembly, Version=10.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData("MyAssembly, Version=255.255.255.255, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        public static void IgnoresVersion(string assemblyNamePortion)
        {
            string typeName = "MyNamespace.MyType";
            string expectedAssemblyName = "MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef";
            string typeName1 = $"{typeName}, {expectedAssemblyName}";
            string typeName2 = $"{typeName}, {assemblyNamePortion}";
            Assert.Equal(typeName1, typeName2, TypeNameComparer.Instance);
            Assert.Equal(TypeNameComparer.Instance.GetHashCode(typeName1), TypeNameComparer.Instance.GetHashCode(typeName2));
        }

        [Theory]
        [InlineData("MyAssembly2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData(", Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef")]
        [InlineData("MyAssembly, Version=1.0.0.0, Culture=en, PublicKeyToken=0123456789abcdef")]
        [InlineData("MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456ff9abcdef")]
        public static void ComparesAssemblyName(string assemblyNamePortion)
        {
            string expectedtypeName = "MyNamespace.MyType";
            string assemblyName = "MyAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef";
            Assert.NotEqual($"{expectedtypeName}, {assemblyName}", $"{expectedtypeName}, {assemblyNamePortion}", TypeNameComparer.Instance);
        }

        [Fact]
        public static void HandlesNameWithSpaces()
        {
            Assert.Equal("MyNamespace.MyType, My Assembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef",
                         "MyNamespace.MyType, My Assembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef",
                         TypeNameComparer.Instance);
            // make sure we don't stop comparing at the space
            Assert.NotEqual("MyNamespace.MyType, My Assembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef",
                            "MyNamespace.MyType, My Assembly2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=0123456789abcdef",
                            TypeNameComparer.Instance);
        }
    }
}
