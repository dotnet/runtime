// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Moq;
using Moq.Protected;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeDescriptionProviderTests
    {
        public static IEnumerable<object[]> CreateInstance_WithoutParent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Mock<IServiceProvider>(MockBehavior.Strict).Object };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_WithoutParent_TestData))]
        public void CreateInstance_InvokeWithoutParent_ReturnsExpected(IServiceProvider serviceProvider)
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.Equal("aa", provider.CreateInstance(serviceProvider, typeof(string), new Type[] { typeof(char), typeof(int) }, new object[] { 'a', 2 }));

            // Call again.
            Assert.Equal("aa", provider.CreateInstance(serviceProvider, typeof(string), new Type[] { typeof(char), typeof(int) }, new object[] { 'a', 2 }));
        }

        public static IEnumerable<object[]> CreateInstance_WithParent_TestData()
        {
            foreach (object result in new object[] { null, new object() })
            {
                yield return new object[] { null, null, null, null, result };
                yield return new object[] { new Mock<IServiceProvider>(MockBehavior.Strict).Object, typeof(string), new Type[] { typeof(char), typeof(int) }, new object[] { 'a', 2 }, result };
            }
        }

        [Theory]
        [MemberData(nameof(CreateInstance_WithParent_TestData))]
        public void CreateInstance_InvokeWithParent_ReturnsExpected(IServiceProvider serviceProvider, Type objectType, Type[] argTypes, object[] args, object result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.CreateInstance(serviceProvider, objectType, argTypes, args))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.CreateInstance(serviceProvider, objectType, argTypes, args));
            mockParentProvider.Verify(p => p.CreateInstance(serviceProvider, objectType, argTypes, args), Times.Once());

            // Call again.
            Assert.Same(result, provider.CreateInstance(serviceProvider, objectType, argTypes, args));
            mockParentProvider.Verify(p => p.CreateInstance(serviceProvider, objectType, argTypes, args), Times.Exactly(2));
        }

        [Fact]
        public void CreateInstance_NullObjectType_ThrowsArgumentNullException()
        {
            var provider = new SubTypeDescriptionProvider();
            AssertExtensions.Throws<ArgumentNullException>("objectType", () => provider.CreateInstance(null, null, null, null));
        }

        public static IEnumerable<object[]> GetCache_WithoutParent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new object() };
        }

        [Theory]
        [MemberData(nameof(GetCache_WithoutParent_TestData))]
        public void GetCache_InvokeWithoutParent_ReturnsNull(object instance)
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.Null(provider.GetCache(instance));

            // Call again.
            Assert.Null(provider.GetCache(instance));
        }

        public static IEnumerable<object[]> GetCache_WithParent_TestData()
        {
            foreach (IDictionary result in new IDictionary[] { null, new Dictionary<int, string>() })
            {
                yield return new object[] { null, result };
                yield return new object[] { new object(), result };
            }
        }

        [Theory]
        [MemberData(nameof(GetCache_WithParent_TestData))]
        public void GetCache_InvokeWithParent_ReturnsExpected(object instance, IDictionary result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetCache(instance))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetCache(instance));
            mockParentProvider.Verify(p => p.GetCache(instance), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetCache(instance));
            mockParentProvider.Verify(p => p.GetCache(instance), Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetExtendedTypeDescriptor_WithoutParent_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new object() };
        }

        [Theory]
        [MemberData(nameof(GetExtendedTypeDescriptor_WithoutParent_TestData))]
        public void GetExtendedTypeDescriptor_InvokeWithoutParent_ReturnsExpected(object instance)
        {
            var provider = new SubTypeDescriptionProvider();
            CustomTypeDescriptor result1 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetExtendedTypeDescriptor(instance));
            Assert.Empty(result1.GetProperties());

            // Call again.
            CustomTypeDescriptor result2 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetExtendedTypeDescriptor(instance));
            Assert.Same(result1, result2);
        }

        public static IEnumerable<object[]> GetExtendedTypeDescriptor_WithParent_TestData()
        {
            foreach (ICustomTypeDescriptor result in new ICustomTypeDescriptor[] { null, new Mock<ICustomTypeDescriptor>(MockBehavior.Strict).Object })
            {
                yield return new object[] { null, result };
                yield return new object[] { new object(), result };
            }
        }

        [Theory]
        [MemberData(nameof(GetExtendedTypeDescriptor_WithParent_TestData))]
        public void GetExtendedTypeDescriptor_InvokeWithParent_ReturnsExpected(object instance, ICustomTypeDescriptor result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetExtendedTypeDescriptor(instance))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetExtendedTypeDescriptor(instance));
            mockParentProvider.Verify(p => p.GetExtendedTypeDescriptor(instance), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetExtendedTypeDescriptor(instance));
            mockParentProvider.Verify(p => p.GetExtendedTypeDescriptor(instance), Times.Exactly(2));
        }

        [Fact]
        public void GetExtenderProviders_InvokeWithoutParent_ReturnsEmpty()
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.Empty(provider.GetExtenderProviders(new object()));

            // Call again.
            Assert.Empty(provider.GetExtenderProviders(new object()));
        }

        public static IEnumerable<object[]> GetExtenderProviders_WithParent_TestData()
        {
            foreach (IExtenderProvider[] result in new IExtenderProvider[][] { null, new IExtenderProvider[] { new Mock<IExtenderProvider>(MockBehavior.Strict).Object } })
            {
                yield return new object[] { null, result };
                yield return new object[] { new object(), result };
            }
        }

        [Theory]
        [MemberData(nameof(GetExtenderProviders_WithParent_TestData))]
        public void GetExtenderProviders_InvokeWithParent_ReturnsExpected(object instance, IExtenderProvider[] result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Protected()
                .Setup<IExtenderProvider[]>("GetExtenderProviders", instance ?? ItExpr.IsNull<object>())
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetExtenderProviders(instance));
            mockParentProvider.Protected().Verify("GetExtenderProviders", Times.Once(), instance ?? ItExpr.IsNull<object>());

            // Call again.
            Assert.Same(result, provider.GetExtenderProviders(instance));
            mockParentProvider.Protected().Verify("GetExtenderProviders", Times.Exactly(2), instance ?? ItExpr.IsNull<object>());
        }

        [Fact]
        public void GetExtenderProviders_NullInstance_ThrowsArgumentNullException()
        {
            var provider = new SubTypeDescriptionProvider();
            AssertExtensions.Throws<ArgumentNullException>("instance", () => provider.GetExtenderProviders(null));
        }

        public static IEnumerable<object[]> GetFullComponentName_WithoutParent_TestData()
        {
            yield return new object[] { new object() };
            yield return new object[] { new Component() };

            var mockSite = new Mock<ISite>(MockBehavior.Strict);
            mockSite
                .Setup(s => s.Container)
                .Returns<IContainer>(null);
            mockSite
                .Setup(s => s.Name)
                .Returns("name");
            yield return new object[] { new Component { Site = mockSite.Object } };
        }

        [Theory]
        [MemberData(nameof(GetFullComponentName_WithoutParent_TestData))]
        public void GetFullComponentName_InvokeWithoutParent_ReturnsNull(object component)
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.Null(provider.GetFullComponentName(component));

            // Call again.
            Assert.Null(provider.GetFullComponentName(component));
        }

        public static IEnumerable<object[]> GetFullComponentName_InvokeWithCustomTypeDescriptor_TestData()
        {
            foreach (string result in new string[] { null, "name" })
            {
                yield return new object[] { new object(), result };
                yield return new object[] { new Component(), result };

                var mockSite = new Mock<ISite>(MockBehavior.Strict);
                mockSite
                    .Setup(s => s.Container)
                    .Returns<IContainer>(null);
                mockSite
                    .Setup(s => s.Name)
                    .Returns("name");
                yield return new object[] { new Component { Site = mockSite.Object }, result };
            }
        }

        [Theory]
        [MemberData(nameof(GetFullComponentName_InvokeWithCustomTypeDescriptor_TestData))]
        public void GetFullComponentName_InvokeWithCustomTypeDescriptor_ReturnsExpected(object component, string result)
        {
            var mockCustomTypeDescriptor = new Mock<ICustomTypeDescriptor>(MockBehavior.Strict);
            mockCustomTypeDescriptor
                .Setup(d => d.GetComponentName())
                .Returns(result)
                .Verifiable();
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider
                .Setup(p => p.GetTypeDescriptor(component.GetType(), component))
                .Returns(mockCustomTypeDescriptor.Object)
                .Verifiable();
            mockProvider
                .Setup(p => p.GetFullComponentName(component))
                .CallBase();
            TypeDescriptionProvider provider = mockProvider.Object;
            Assert.Same(result, provider.GetFullComponentName(component));
            mockProvider.Verify(p => p.GetTypeDescriptor(component.GetType(), component), Times.Once());
            mockCustomTypeDescriptor.Verify(d => d.GetComponentName(), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetFullComponentName(component));
            mockProvider.Verify(p => p.GetTypeDescriptor(component.GetType(), component), Times.Exactly(2));
            mockCustomTypeDescriptor.Verify(d => d.GetComponentName(), Times.Exactly(2));
        }

        [Theory]
        [MemberData(nameof(GetFullComponentName_WithoutParent_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws NullReferenceException")]
        public void GetFullComponentName_InvokeWithNullTypeDescriptor_ReturnsExpected(object component)
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider
                .Setup(p => p.GetTypeDescriptor(component.GetType(), component))
                .Returns<ICustomTypeDescriptor>(null)
                .Verifiable();
            mockProvider
                .Setup(p => p.GetFullComponentName(component))
                .CallBase();
            TypeDescriptionProvider provider = mockProvider.Object;
            Assert.Null(provider.GetFullComponentName(component));
            mockProvider.Verify(p => p.GetTypeDescriptor(component.GetType(), component), Times.Once());

            // Call again.
            Assert.Null(provider.GetFullComponentName(component));
            mockProvider.Verify(p => p.GetTypeDescriptor(component.GetType(), component), Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetFullComponentName_WithParent_TestData()
        {
            foreach (string result in new string[] { null, "name" })
            {
                yield return new object[] { null, result };
                yield return new object[] { new object(), result };
            }
        }

        [Theory]
        [MemberData(nameof(GetFullComponentName_WithParent_TestData))]
        public void GetFullComponentName_InvokeWithParent_ReturnsExpected(object component, string result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetFullComponentName(component))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetFullComponentName(component));
            mockParentProvider.Verify(p => p.GetFullComponentName(component), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetFullComponentName(component));
            mockParentProvider.Verify(p => p.GetFullComponentName(component), Times.Exactly(2));
        }

        [Fact]
        public void GetFullComponentName_NullComponent_ReturnsNull()
        {
            var provider = new SubTypeDescriptionProvider();
            AssertExtensions.Throws<ArgumentNullException>("instance", () => provider.GetFullComponentName(null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetReflectionType_InvokeTypeWithoutParent_ReturnsExpected(Type objectType)
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.Same(objectType, provider.GetReflectionType(objectType));

            // Call again.
            Assert.Same(objectType, provider.GetReflectionType(objectType));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetReflectionType_InvokeTypeWithoutParent_CallsTypeObjectOverload_ByType(Type objectType)
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider
                .Setup(p => p.GetReflectionType(objectType, null))
                .CallBase();
            TypeDescriptionProvider provider = mockProvider.Object;
            Assert.Same(objectType, provider.GetReflectionType(objectType));
            mockProvider.Verify(p => p.GetReflectionType(objectType, null), Times.Once());

            // Call again.
            Assert.Same(objectType, provider.GetReflectionType(objectType));
            mockProvider.Verify(p => p.GetReflectionType(objectType, null), Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetReflectionType_TypeWithParent_TestData()
        {
            foreach (Type result in new Type[] { null, typeof(object) })
            {
                yield return new object[] { null, result };
                yield return new object[] { typeof(object), result };
            }
        }

        [Theory]
        [MemberData(nameof(GetReflectionType_TypeWithParent_TestData))]
        public void GetReflectionType_InvokeTypeWithParent_ReturnsExpected(Type objectType, Type result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetReflectionType(objectType, null))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetReflectionType(objectType));
            mockParentProvider.Verify(p => p.GetReflectionType(objectType, null), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetReflectionType(objectType));
            mockParentProvider.Verify(p => p.GetReflectionType(objectType, null), Times.Exactly(2));
        }

        [Theory]
        [InlineData(1, typeof(int))]
        public void GetReflectionType_InvokeObjectWithoutParent_ReturnsExpected(object instance, Type expected)
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.Same(expected, provider.GetReflectionType(instance));

            // Call again.
            Assert.Same(expected, provider.GetReflectionType(instance));
        }

        [Theory]
        [InlineData(1, typeof(int))]
        public void GetReflectionType_InvokeTypeWithoutParent_CallsTypeObjectOverload_ByObjectAndType(object instance, Type expected)
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider
                .Setup(p => p.GetReflectionType(instance.GetType(), instance))
                .CallBase();
            TypeDescriptionProvider provider = mockProvider.Object;
            Assert.Same(expected, provider.GetReflectionType(instance));
            mockProvider.Verify(p => p.GetReflectionType(instance.GetType(), instance), Times.Once());

            // Call again.
            Assert.Same(expected, provider.GetReflectionType(instance));
            mockProvider.Verify(p => p.GetReflectionType(instance.GetType(), instance), Times.Exactly(2));
        }

        [Theory]
        [InlineData(1, null)]
        [InlineData(1, typeof(object))]
        public void GetReflectionType_InvokeObjectWithParent_ReturnsExpected(object instance, Type result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetReflectionType(instance.GetType(), instance))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetReflectionType(instance));
            mockParentProvider.Verify(p => p.GetReflectionType(instance.GetType(), instance), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetReflectionType(instance));
            mockParentProvider.Verify(p => p.GetReflectionType(instance.GetType(), instance), Times.Exactly(2));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, 1)]
        [InlineData(typeof(object), null)]
        [InlineData(typeof(object), 1)]
        public void GetReflectionType_InvokeTypeObjectWithoutParent_ReturnsExpected(Type objectType, object instance)
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.Same(objectType, provider.GetReflectionType(objectType, instance));

            // Call again.
            Assert.Same(objectType, provider.GetReflectionType(objectType, instance));
        }

        public static IEnumerable<object[]> GetReflectionType_TypeObjectWithParent_TestData()
        {
            foreach (Type result in new Type[] { null, typeof(object) })
            {
                yield return new object[] { null, null, result };
                yield return new object[] { typeof(int), null, result };
                yield return new object[] { null, 1, result };
                yield return new object[] { typeof(int), 1, result };
            }
        }

        [Theory]
        [MemberData(nameof(GetReflectionType_TypeObjectWithParent_TestData))]
        public void GetReflectionType_InvokeTypeObjectWithParent_ReturnsExpected(Type objectType, object instance, Type result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetReflectionType(objectType, instance))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetReflectionType(objectType, instance));
            mockParentProvider.Verify(p => p.GetReflectionType(objectType, instance), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetReflectionType(objectType, instance));
            mockParentProvider.Verify(p => p.GetReflectionType(objectType, instance), Times.Exactly(2));
        }

        [Fact]
        public void GetReflectionType_NullInstance_ThrowsArgumentNullException()
        {
            var provider = new SubTypeDescriptionProvider();
            AssertExtensions.Throws<ArgumentNullException>("instance", () => provider.GetReflectionType((object)null));
        }

        [Fact]
        public void GetReflectionType_NullInstanceWithParent_ThrowsArgumentNullException()
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            AssertExtensions.Throws<ArgumentNullException>("instance", () => provider.GetReflectionType((object)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(TypeDescriptionProviderTests))]
        public void GetRuntimeType_InvokeWithoutParentSystemDefinedType_ReturnsSame(Type reflectionType)
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.Same(reflectionType, provider.GetRuntimeType(reflectionType));

            // Call again.
            Assert.Same(reflectionType, provider.GetRuntimeType(reflectionType));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetRuntimeType_InvokeWithoutParentWithUserDefinedType_RetunsUnderlyingSystemType(Type result)
        {
            var mockType = new Mock<Type>(MockBehavior.Strict);
            mockType
                .Setup(t => t.UnderlyingSystemType)
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider();
            Assert.Same(result, provider.GetRuntimeType(mockType.Object));
            mockType.Verify(t => t.UnderlyingSystemType, Times.Once());

            // Call again.
            Assert.Same(result, provider.GetRuntimeType(mockType.Object));
            mockType.Verify(t => t.UnderlyingSystemType, Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetRuntimeType_WithParent_TestData()
        {
            foreach (Type result in new Type[] { null, typeof(int) })
            {
                yield return new object[] { null, result };
                yield return new object[] { typeof(object), result };
            }
        }

        [Theory]
        [MemberData(nameof(GetRuntimeType_WithParent_TestData))]
        public void GetRuntimeType_InvokeWithParent_ReturnsExpected(Type reflectionType, Type result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetRuntimeType(reflectionType))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetRuntimeType(reflectionType));
            mockParentProvider.Verify(p => p.GetRuntimeType(reflectionType), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetRuntimeType(reflectionType));
            mockParentProvider.Verify(p => p.GetRuntimeType(reflectionType), Times.Exactly(2));
        }

        [Fact]
        public void GetRuntimeType_NullReflectionType_ThrowsArgumentNullException()
        {
            var provider = new SubTypeDescriptionProvider();
            AssertExtensions.Throws<ArgumentNullException>("reflectionType", () => provider.GetRuntimeType(null));
        }


        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetTypeDescriptor_InvokeTypeWithoutParent_ReturnsExpected(Type objectType)
        {
            var provider = new SubTypeDescriptionProvider();
            CustomTypeDescriptor result1 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(objectType));
            Assert.Empty(result1.GetProperties());

            // Call again.
            CustomTypeDescriptor result2 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(objectType));
            Assert.Same(result1, result2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetTypeDescriptor_InvokeTypeWithoutParent_CallsTypeObjectOverload_Type(Type objectType)
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider
                .Setup(p => p.GetTypeDescriptor(objectType, null))
                .CallBase();
            TypeDescriptionProvider provider = mockProvider.Object;
            CustomTypeDescriptor result1 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(objectType));
            Assert.Empty(result1.GetProperties());
            mockProvider.Verify(p => p.GetTypeDescriptor(objectType, null), Times.Once());

            // Call again.
            CustomTypeDescriptor result2 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(objectType));
            Assert.Same(result1, result2);
            mockProvider.Verify(p => p.GetTypeDescriptor(objectType, null), Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetTypeDescriptor_TypeWithParent_TestData()
        {
            foreach (ICustomTypeDescriptor result in new ICustomTypeDescriptor[] { null, new Mock<ICustomTypeDescriptor>(MockBehavior.Strict).Object })
            {
                yield return new object[] { null, result };
                yield return new object[] { typeof(object), result };
            }
        }

        [Theory]
        [MemberData(nameof(GetTypeDescriptor_TypeWithParent_TestData))]
        public void GetTypeDescriptor_InvokeTypeWithParent_ReturnsExpected(Type objectType, ICustomTypeDescriptor result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetTypeDescriptor(objectType, null))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetTypeDescriptor(objectType));
            mockParentProvider.Verify(p => p.GetTypeDescriptor(objectType, null), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetTypeDescriptor(objectType));
            mockParentProvider.Verify(p => p.GetTypeDescriptor(objectType, null), Times.Exactly(2));
        }

        [Theory]
        [InlineData(1)]
        public void GetTypeDescriptor_InvokeObjectWithoutParent_ReturnsExpected(object instance)
        {
            var provider = new SubTypeDescriptionProvider();
            CustomTypeDescriptor result1 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(instance));
            Assert.Empty(result1.GetProperties());

            // Call again.
            CustomTypeDescriptor result2 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(instance));
            Assert.Same(result1, result2);
        }

        [Theory]
        [InlineData(1)]
        public void GetTypeDescriptor_InvokeTypeWithoutParent_CallsTypeObjectOverload_Object(object instance)
        {
            var mockProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockProvider
                .Setup(p => p.GetTypeDescriptor(instance.GetType(), instance))
                .CallBase();
            TypeDescriptionProvider provider = mockProvider.Object;
            CustomTypeDescriptor result1 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(instance));
            Assert.Empty(result1.GetProperties());
            mockProvider.Verify(p => p.GetTypeDescriptor(instance.GetType(), instance), Times.Once());

            // Call again.
            CustomTypeDescriptor result2 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(instance));
            Assert.Same(result1, result2);
            mockProvider.Verify(p => p.GetTypeDescriptor(instance.GetType(), instance), Times.Exactly(2));
        }

        public static IEnumerable<object[]> GetTypeDescriptor_ObjectWithParent_TestData()
        {
            foreach (ICustomTypeDescriptor result in new ICustomTypeDescriptor[] { null, new Mock<ICustomTypeDescriptor>(MockBehavior.Strict).Object })
            {
                yield return new object[] { 1, result };
            }
        }

        [Theory]
        [MemberData(nameof(GetTypeDescriptor_ObjectWithParent_TestData))]
        public void GetTypeDescriptor_InvokeObjectWithParent_ReturnsExpected(object instance, ICustomTypeDescriptor result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetTypeDescriptor(instance.GetType(), instance))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetTypeDescriptor(instance));
            mockParentProvider.Verify(p => p.GetTypeDescriptor(instance.GetType(), instance), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetTypeDescriptor(instance));
            mockParentProvider.Verify(p => p.GetTypeDescriptor(instance.GetType(), instance), Times.Exactly(2));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, 1)]
        [InlineData(typeof(object), null)]
        [InlineData(typeof(object), 1)]
        public void GetTypeDescriptor_InvokeTypeObjectWithoutParent_ReturnsExpected(Type objectType, object instance)
        {
            var provider = new SubTypeDescriptionProvider();
            CustomTypeDescriptor result1 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(objectType, instance));
            Assert.Empty(result1.GetProperties());

            // Call again.
            CustomTypeDescriptor result2 = Assert.IsAssignableFrom<CustomTypeDescriptor>(provider.GetTypeDescriptor(objectType, instance));
            Assert.Same(result1, result2);
        }

        public static IEnumerable<object[]> GetTypeDescriptor_TypeObjectWithParent_TestData()
        {
            foreach (ICustomTypeDescriptor result in new ICustomTypeDescriptor[] { null, new Mock<ICustomTypeDescriptor>(MockBehavior.Strict).Object })
            {
                yield return new object[] { null, null, result };
                yield return new object[] { typeof(int), null, result };
                yield return new object[] { null, 1, result };
                yield return new object[] { typeof(int), 1, result };
            }
        }

        [Theory]
        [MemberData(nameof(GetTypeDescriptor_TypeObjectWithParent_TestData))]
        public void GetTypeDescriptor_InvokeTypeObjectWithParent_ReturnsExpected(Type objectType, object instance, ICustomTypeDescriptor result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.GetTypeDescriptor(objectType, instance))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Same(result, provider.GetTypeDescriptor(objectType, instance));
            mockParentProvider.Verify(p => p.GetTypeDescriptor(objectType, instance), Times.Once());

            // Call again.
            Assert.Same(result, provider.GetTypeDescriptor(objectType, instance));
            mockParentProvider.Verify(p => p.GetTypeDescriptor(objectType, instance), Times.Exactly(2));
        }

        [Fact]
        public void GetTypeDescriptor_NullInstance_ThrowsArgumentNullException()
        {
            var provider = new SubTypeDescriptionProvider();
            AssertExtensions.Throws<ArgumentNullException>("instance", () => provider.GetTypeDescriptor((object)null));
        }

        [Fact]
        public void GetTypeDescriptor_NullInstanceWithParent_ThrowsArgumentNullException()
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            AssertExtensions.Throws<ArgumentNullException>("instance", () => provider.GetTypeDescriptor((object)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        public void IsSupportedType_InvokeWithoutParent_ReturnsTrue(Type type)
        {
            var provider = new SubTypeDescriptionProvider();
            Assert.True(provider.IsSupportedType(type));

            // Call again.
            Assert.True(provider.IsSupportedType(type));
        }

        [Theory]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(int), false)]
        public void IsSupportedType_InvokeWithParent_ReturnsExpected(Type type, bool result)
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            mockParentProvider
                .Setup(p => p.IsSupportedType(type))
                .Returns(result)
                .Verifiable();
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            Assert.Equal(result, provider.IsSupportedType(type));
            mockParentProvider.Verify(p => p.IsSupportedType(type), Times.Once());

            // Call again.
            Assert.Equal(result, provider.IsSupportedType(type));
            mockParentProvider.Verify(p => p.IsSupportedType(type), Times.Exactly(2));
        }

        [Fact]
        public void IsSupportedType_NullType_ThrowsArgumentNullException()
        {
            var provider = new SubTypeDescriptionProvider();
            AssertExtensions.Throws<ArgumentNullException>("type", () => provider.IsSupportedType(null));
        }

        [Fact]
        public void IsSupportedType_NullTypeWithParent_ThrowsArgumentNullException()
        {
            var mockParentProvider = new Mock<TypeDescriptionProvider>(MockBehavior.Strict);
            var provider = new SubTypeDescriptionProvider(mockParentProvider.Object);
            AssertExtensions.Throws<ArgumentNullException>("type", () => provider.IsSupportedType(null));
        }

        private class SubTypeDescriptionProvider : TypeDescriptionProvider
        {
            public SubTypeDescriptionProvider() : base()
            {
            }

            public SubTypeDescriptionProvider(TypeDescriptionProvider parent) : base(parent)
            {
            }

            public new IExtenderProvider[] GetExtenderProviders(object instance) => base.GetExtenderProviders(instance);
        }
    }
}
