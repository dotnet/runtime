// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/51912", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
    public static class NullableMetadataTests
    {
        private const string NullableAttributeFullName = "System.Runtime.CompilerServices.NullableAttribute";
        private const string NullableContextAttributeFullName = "System.Runtime.CompilerServices.NullableContextAttribute";
        private const string NullablePublicOnlyAttributeFullName = "System.Runtime.CompilerServices.NullablePublicOnlyAttribute";

        private static IEnumerable<CustomAttributeData> GetNullableAttributes(this IEnumerable<CustomAttributeData> attributes) =>
            attributes.Where(attribute => attribute.AttributeType.FullName.Equals(NullableAttributeFullName) ||
                                          attribute.AttributeType.FullName.Equals(NullableContextAttributeFullName));

        private static CustomAttributeData GetNullablePublicOnlyAttribute(this IEnumerable<CustomAttributeData> attributes) =>
            attributes.Where(attribute => attribute.AttributeType.FullName.Equals(NullablePublicOnlyAttributeFullName)).FirstOrDefault();

        private static bool IsProtected(this MemberInfo info)
        {
            if (info is MethodBase methodBase)
            {
                return methodBase.IsFamily || methodBase.IsFamilyOrAssembly;
            }
            else if (info is FieldInfo fieldInfo)
            {
                return fieldInfo.IsFamily || fieldInfo.IsFamilyOrAssembly;
            }
            else if (info is PropertyInfo propertyInfo)
            {
                return (propertyInfo.GetMethod != null && (propertyInfo.GetMethod.IsFamily || propertyInfo.GetMethod.IsFamilyOrAssembly)) ||
                       (propertyInfo.SetMethod != null && (propertyInfo.SetMethod.IsFamily || propertyInfo.SetMethod.IsFamilyOrAssembly));
            }
            else if (info is TypeInfo typeInfo)
            {
                return typeInfo.IsNestedFamily || typeInfo.IsNestedFamORAssem;
            }

            return false;
        }

        public static IEnumerable<object[]> NullableMetadataTypesTestData()
        {
            yield return new object[] { typeof(string) };
            yield return new object[] { typeof(Dictionary<,>) };
            yield return new object[] { typeof(Uri) };
            yield return new object[] { typeof(ConcurrentDictionary<,>) };
            yield return new object[] { typeof(ArrayPool<>) };
        }

        [Theory]
        [MemberData(nameof(NullableMetadataTypesTestData))]
        public static void NullableAttributesOnPublicApiOnly(Type type)
        {
            MemberInfo[] internalMembers = type.GetMembers(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            foreach (MemberInfo internalMember in internalMembers)
            {
                // When using BindingFlags.NonPublic protected members are included and those are expected
                // to have Nullable attributes.
                if (internalMember.IsProtected() || internalMember is PropertyInfo)
                    continue;

                Assert.Empty(internalMember.CustomAttributes.GetNullableAttributes());

                if (internalMember is MethodInfo methodInfo)
                {
                    Assert.Empty(methodInfo.ReturnParameter.CustomAttributes.GetNullableAttributes());

                    foreach (ParameterInfo param in methodInfo.GetParameters())
                    {
                        Assert.Empty(param.CustomAttributes.GetNullableAttributes());
                    }
                }
            }

            Assert.True(type.CustomAttributes.GetNullableAttributes().Any());

            bool foundAtLeastOneNullableAttribute = type.CustomAttributes.Where(a => a.AttributeType.Name.Equals(NullableContextAttributeFullName)).Any();

            // If there is a NullableContextAttribute there is no guarantee that its members will have
            // nullable attributes, if a class declare all reference types with the same nullability
            // none will contain an attribute and will take the type's NullableContextAttribute value.
            if (!foundAtLeastOneNullableAttribute)
            {
                MemberInfo[] publicMembers = type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                foreach (MemberInfo publicMember in publicMembers)
                {
                    if (publicMember.CustomAttributes.GetNullableAttributes().Any())
                    {
                        foundAtLeastOneNullableAttribute = true;
                        break;
                    }

                    if (publicMember is MethodInfo methodInfo)
                    {
                        if (methodInfo.ReturnParameter.CustomAttributes.GetNullableAttributes().Any())
                        {
                            foundAtLeastOneNullableAttribute = true;
                            break;
                        }
                    }

                    if (publicMember is MethodBase methodBase)
                    {
                        foreach (ParameterInfo param in methodBase.GetParameters())
                        {
                            if (param.CustomAttributes.GetNullableAttributes().Any())
                            {
                                foundAtLeastOneNullableAttribute = true;
                                break;
                            }
                        }

                        if (foundAtLeastOneNullableAttribute)
                            break;
                    }
                }
            }

            Assert.True(foundAtLeastOneNullableAttribute);
        }

        [Theory]
        [InlineData("mscorlib")]
        [InlineData("System.Threading.Overlapped")]
        public static void ShimsHaveOnlyTypeForwards(string assemblyName)
        {
            Assembly assembly = Assembly.Load(assemblyName);

            Assert.Empty(assembly.GetTypes());
            Assert.Empty(assembly.GetManifestResourceNames());
            Assert.NotEmpty(assembly.GetForwardedTypes());
        }

        [Fact]
        public static void ShimsDontHaveNullablePublicOnlyAttribute()
        {
            Assembly assembly = Assembly.Load("mscorlib");
            Module module = assembly.Modules.First();
            Assert.Null(module.CustomAttributes.GetNullablePublicOnlyAttribute());
        }

        [Theory]
        [MemberData(nameof(NullableMetadataTypesTestData))]
        public static void NullablePublicOnlyAttributePresent(Type type)
        {
            CustomAttributeData nullablePublicOnlyAttribute = type.Module.CustomAttributes.GetNullablePublicOnlyAttribute();
            Assert.NotNull(nullablePublicOnlyAttribute);

            Assert.False((bool)nullablePublicOnlyAttribute.ConstructorArguments.First().Value);
        }
    }
}
