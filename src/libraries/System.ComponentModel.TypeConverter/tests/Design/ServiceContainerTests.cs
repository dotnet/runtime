// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Tests;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class ServiceContainerTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var container = new SubServiceContainer();
            Assert.Equal(new Type[] { typeof(IServiceContainer), typeof(ServiceContainer) }, container.DefaultServices);
            Assert.Same(container.DefaultServices, container.DefaultServices);
        }

        public static IEnumerable<object[]> Ctor_IServiceProvider_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new MockServiceProvider() };
        }

        [Theory]
        [MemberData(nameof(Ctor_IServiceProvider_TestData))]
        public void Ctor_IServiceProvider(IServiceProvider parentProvider)
        {
            var container = new SubServiceContainer(parentProvider);
            Assert.Equal(new Type[] { typeof(IServiceContainer), typeof(ServiceContainer) }, container.DefaultServices);
            Assert.Same(container.DefaultServices, container.DefaultServices);
        }

        public static IEnumerable<object[]> AddService_TypeObject_TestData()
        {
            var nullServiceProvider = new MockServiceProvider();
            nullServiceProvider.Setup(typeof(IServiceContainer), null);
            nullServiceProvider.Setup(typeof(int), null);

            var invalidServiceProvider = new MockServiceProvider();
            invalidServiceProvider.Setup(typeof(IServiceContainer), new object());
            invalidServiceProvider.Setup(typeof(int), null);

            var validServiceProvider = new MockServiceProvider();
            validServiceProvider.Setup(typeof(IServiceContainer), new ServiceContainer());
            validServiceProvider.Setup(typeof(int), null);

            var o = new object();
            foreach (IServiceProvider parentProvider in new object[] { null, nullServiceProvider, invalidServiceProvider, validServiceProvider })
            {
                yield return new object[] { parentProvider, typeof(object), o, o };
                yield return new object[] { parentProvider, typeof(object), "abc", "abc" };
                yield return new object[] { parentProvider, typeof(string), "abc", "abc" };
            }
        }

        [Theory]
        [MemberData(nameof(AddService_TypeObject_TestData))]
        [MemberData(nameof(AddService_TypeServiceCreatorCallback_TestData))]
        public void AddService_InvokeTypeObject_GetServiceReturnsExpected(IServiceProvider parentProvider, Type serviceType, object serviceInstance, object expected)
        {
            var container = new ServiceContainer(parentProvider);
            container.AddService(serviceType, serviceInstance);
            Assert.Same(expected, container.GetService(serviceType));
        }

        public static IEnumerable<object[]> AddService_TypeObjectBool_TestData()
        {
            var nullServiceProvider = new MockServiceProvider();
            nullServiceProvider.Setup(typeof(IServiceContainer), null);
            nullServiceProvider.Setup(typeof(int), null);

            var invalidServiceProvider = new MockServiceProvider();
            invalidServiceProvider.Setup(typeof(IServiceContainer), new object());
            invalidServiceProvider.Setup(typeof(int), null);

            var validServiceProvider = new MockServiceProvider();
            validServiceProvider.Setup(typeof(IServiceContainer), new ServiceContainer());
            validServiceProvider.Setup(typeof(int), null);

            var o = new object();
            foreach (bool promote in new bool[] { true, false })
            {
                foreach (IServiceProvider parentProvider in new object[] { null, nullServiceProvider, invalidServiceProvider, validServiceProvider })
                {
                    if (promote && parentProvider == validServiceProvider)
                    {
                        continue;
                    }

                    yield return new object[] { parentProvider, typeof(object), o, promote, o };
                    yield return new object[] { parentProvider, typeof(object), "abc", promote, "abc" };
                    yield return new object[] { parentProvider, typeof(string), "abc", promote, "abc" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AddService_TypeObjectBool_TestData))]
        [MemberData(nameof(AddService_TypeServiceCreatorCallbackBool_TestData))]
        public void AddService_InvokeTypeObjectBool_GetServiceReturnsExpected(IServiceProvider parentProvider, Type serviceType, object serviceInstance, bool promote, object expected)
        {
            var container = new ServiceContainer(parentProvider);
            container.AddService(serviceType, serviceInstance, promote);
            Assert.Same(expected, container.GetService(serviceType));
        }

        [Fact]
        public void AddService_PromoteServiceInstanceWithValidParentProvider_AddsToParent()
        {
            var serviceInstance = new object();
            var parentContainer = new ServiceContainer();
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            parentProvider.Setup(typeof(object), null);
            var container = new ServiceContainer(parentProvider);

            container.AddService(typeof(object), serviceInstance, promote: true);
            Assert.Null(container.GetService(typeof(object)));
            Assert.Same(serviceInstance, parentContainer.GetService(typeof(object)));
        }

        [Fact]
        public void AddService_PromoteServiceInstanceWithValidGrandParentProvider_AddsToGrandParent()
        {
            var serviceInstance = new object();
            var grandparentContainer = new ServiceContainer();
            var grandparentProvider = new MockServiceProvider();
            grandparentProvider.Setup(typeof(IServiceContainer), grandparentContainer);
            grandparentProvider.Setup(typeof(object), null);
            var parentContainer = new ServiceContainer(grandparentProvider);
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            parentProvider.Setup(typeof(object), null);
            var container = new ServiceContainer(parentProvider);

            container.AddService(typeof(object), serviceInstance, promote: true);
            Assert.Null(container.GetService(typeof(object)));
            Assert.Null(parentContainer.GetService(typeof(object)));
            Assert.Same(serviceInstance, grandparentContainer.GetService(typeof(object)));
        }

        public static IEnumerable<object[]> AddService_PromoteObject_TestData()
        {
            ServiceCreatorCallback callback = (container, serviceType) => "abc";
            yield return new object[] { null, null };
            yield return new object[] { typeof(int), new object() };
            yield return new object[] { typeof(int), callback };
        }

        [Theory]
        [MemberData(nameof(AddService_PromoteObject_TestData))]
        public void AddService_PromoteObject_Success(Type serviceType, object serviceInstance)
        {
            var parentContainer = new CustomServiceContainer();
            int addServiceCallCount = 0;
            parentContainer.AddServiceObjectAction = (callbackServiceType, callbackServiceInstance, callbackPromote) =>
            {
                Assert.Same(serviceType, callbackServiceType);
                Assert.Same(serviceInstance, callbackServiceInstance);
                Assert.True(callbackPromote);
                addServiceCallCount++;
            };
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            var container = new ServiceContainer(parentProvider);

            container.AddService(serviceType, serviceInstance, promote: true);
            Assert.Equal(1, addServiceCallCount);
        }

        public static IEnumerable<object[]> AddService_PromoteCallback_TestData()
        {
            ServiceCreatorCallback callback = (container, serviceType) => "abc";
            yield return new object[] { null, null };
            yield return new object[] { typeof(int), callback };
        }

        [Theory]
        [MemberData(nameof(AddService_PromoteCallback_TestData))]
        public void AddService_PromoteCallback_Success(Type serviceType, ServiceCreatorCallback callback)
        {
            var parentContainer = new CustomServiceContainer();
            int addServiceCallCount = 0;
            parentContainer.AddServiceCallbackAction = (callbackServiceType, callbackCallback, callbackPromote) =>
            {
                Assert.Same(serviceType, callbackServiceType);
                Assert.Same(callback, callbackCallback);
                Assert.True(callbackPromote);
                addServiceCallCount++;
            };
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            var container = new ServiceContainer(parentProvider);

            container.AddService(serviceType, callback, promote: true);
            Assert.Equal(1, addServiceCallCount);
        }

        [Fact]
        public void AddService_NullServiceType_ThrowsArgumentNullException()
        {
            var container = new ServiceContainer();
            ServiceCreatorCallback callback = (container, serviceType) => "abc";
            Assert.Throws<ArgumentNullException>("serviceType", () => container.AddService(null, new object()));
            Assert.Throws<ArgumentNullException>("serviceType", () => container.AddService(null, new object(), true));
            Assert.Throws<ArgumentNullException>("serviceType", () => container.AddService(null, new object(), false));
            Assert.Throws<ArgumentNullException>("serviceType", () => container.AddService(null, callback));
            Assert.Throws<ArgumentNullException>("serviceType", () => container.AddService(null, callback, true));
            Assert.Throws<ArgumentNullException>("serviceType", () => container.AddService(null, callback, false));
        }

        [Fact]
        public void AddService_NullServiceInstance_ThrowsArgumentNullException()
        {
            var container = new ServiceContainer();
            Assert.Throws<ArgumentNullException>("serviceInstance", () => container.AddService(typeof(object), (object)null));
            Assert.Throws<ArgumentNullException>("serviceInstance", () => container.AddService(typeof(object), (object)null, true));
            Assert.Throws<ArgumentNullException>("serviceInstance", () => container.AddService(typeof(object), (object)null, false));
        }

        [Fact]
        public void AddService_ServiceInstanceNotInstanceOfType_ThrowsArgumentException()
        {
            var container = new ServiceContainer();
            AssertExtensions.Throws<ArgumentException>(null, () => container.AddService(typeof(int), new object()));
            AssertExtensions.Throws<ArgumentException>(null, () => container.AddService(typeof(int), new object(), true));
            AssertExtensions.Throws<ArgumentException>(null, () => container.AddService(typeof(int), new object(), false));
        }

        [Fact]
        public void AddService_AlreadyAddedServiceInstance_ThrowsArgumentException()
        {
            var container = new ServiceContainer();
            var serviceInstance = new object();
            ServiceCreatorCallback callback = (container, serviceType) => "abc";
            container.AddService(typeof(object), serviceInstance);
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), new object()));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), new object(), true));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), new object(), false));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), callback));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), callback, true));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), callback, false));
        }

        public static IEnumerable<object[]> AddService_TypeServiceCreatorCallback_TestData()
        {
            var nullServiceProvider = new MockServiceProvider();
            nullServiceProvider.Setup(typeof(IServiceContainer), null);
            nullServiceProvider.Setup(typeof(object), null);
            nullServiceProvider.Setup(typeof(int), null);

            var invalidServiceProvider = new MockServiceProvider();
            invalidServiceProvider.Setup(typeof(IServiceContainer), new object());
            invalidServiceProvider.Setup(typeof(object), null);
            invalidServiceProvider.Setup(typeof(int), null);

            var validServiceProvider = new MockServiceProvider();
            validServiceProvider.Setup(typeof(IServiceContainer), new ServiceContainer());
            validServiceProvider.Setup(typeof(object), null);
            validServiceProvider.Setup(typeof(int), null);

            var o = new object();
            ServiceCreatorCallback callback = (container, serviceType) => "abc";
            ServiceCreatorCallback nullCallback = (container, serviceType) => null;
            foreach (IServiceProvider parentProvider in new object[] { null, nullServiceProvider, invalidServiceProvider, validServiceProvider })
            {
                yield return new object[] { parentProvider, typeof(object), callback, "abc" };
                yield return new object[] { parentProvider, typeof(string), callback, "abc" };
                yield return new object[] { parentProvider, typeof(int), callback, null };

                yield return new object[] { parentProvider, typeof(object), nullCallback, null };
                yield return new object[] { parentProvider, typeof(int), nullCallback, null };
            }

            var customServiceProvider = new MockServiceProvider();
            customServiceProvider.Setup(typeof(int), o);
            yield return new object[] { customServiceProvider, typeof(int), callback, o };
        }

        [Theory]
        [MemberData(nameof(AddService_TypeServiceCreatorCallback_TestData))]
        public void AddService_TypeServiceCreatorCallback_GetServiceReturnsExpected(IServiceProvider parentProvider, Type serviceType, ServiceCreatorCallback callback, object expected)
        {
            var container = new ServiceContainer(parentProvider);
            container.AddService(serviceType, callback);
            Assert.Same(expected, container.GetService(serviceType));
        }

        public static IEnumerable<object[]> AddService_TypeServiceCreatorCallbackBool_TestData()
        {
            var nullServiceProvider = new MockServiceProvider();
            nullServiceProvider.Setup(typeof(IServiceContainer), null);
            nullServiceProvider.Setup(typeof(object), null);
            nullServiceProvider.Setup(typeof(int), null);

            var invalidServiceProvider = new MockServiceProvider();
            invalidServiceProvider.Setup(typeof(IServiceContainer), new object());
            invalidServiceProvider.Setup(typeof(object), null);
            invalidServiceProvider.Setup(typeof(int), null);

            var validServiceProvider = new MockServiceProvider();
            validServiceProvider.Setup(typeof(IServiceContainer), new ServiceContainer());
            validServiceProvider.Setup(typeof(object), null);
            validServiceProvider.Setup(typeof(int), null);

            var o = new object();
            ServiceCreatorCallback callback = (container, serviceType) => "abc";
            ServiceCreatorCallback nullCallback = (container, serviceType) => null;
            foreach (bool promote in new bool[] { true, false })
            {
                foreach (IServiceProvider parentProvider in new object[] { null, nullServiceProvider, invalidServiceProvider, validServiceProvider })
                {
                    if (promote && parentProvider == validServiceProvider)
                    {
                        continue;
                    }

                    yield return new object[] { parentProvider, typeof(object), callback, promote, "abc" };
                    yield return new object[] { parentProvider, typeof(string), callback, promote, "abc" };
                    yield return new object[] { parentProvider, typeof(int), callback, promote, null };

                    yield return new object[] { parentProvider, typeof(object), nullCallback, promote, null };
                    yield return new object[] { parentProvider, typeof(int), nullCallback, promote, null };
                }

                var customServiceProvider = new MockServiceProvider();
                customServiceProvider.Setup(typeof(IServiceContainer), null);
                customServiceProvider.Setup(typeof(int), o);
                yield return new object[] { customServiceProvider, typeof(int), callback, promote, o };
            }
        }

        [Theory]
        [MemberData(nameof(AddService_TypeServiceCreatorCallbackBool_TestData))]
        public void AddService_InvokeTypeServiceCreatorCallbackBool_GetServiceReturnsExpected(IServiceProvider parentProvider, Type serviceType, ServiceCreatorCallback callback, bool promote, object expected)
        {
            var container = new ServiceContainer(parentProvider);
            container.AddService(serviceType, callback, promote);
            Assert.Same(expected, container.GetService(serviceType));
        }

        [Fact]
        public void AddService_PromoteCallbackWithNoParentProvider_AddsToCurrent()
        {
            var serviceInstance = new object();
            var container = new ServiceContainer();
            ServiceCreatorCallback callback = (c, serviceType) =>
            {
                Assert.Same(container, c);
                Assert.Equal(typeof(object), serviceType);
                return serviceInstance;
            };
            container.AddService(typeof(object), callback, promote: true);
            Assert.Same(serviceInstance, container.GetService(typeof(object)));
        }

        [Fact]
        public void AddService_PromoteCallbackWithValidParentProvider_AddsToParent()
        {
            var serviceInstance = new object();
            var parentContainer = new ServiceContainer();
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            parentProvider.Setup(typeof(object), null);
            var container = new ServiceContainer(parentProvider);

            ServiceCreatorCallback callback = (c, serviceType) =>
            {
                Assert.Same(parentContainer, c);
                Assert.Equal(typeof(object), serviceType);
                return serviceInstance;
            };
            container.AddService(typeof(object), callback, promote: true);
            Assert.Null(container.GetService(typeof(object)));
            Assert.Same(serviceInstance, parentContainer.GetService(typeof(object)));
        }

        [Fact]
        public void AddService_PromoteServiceCallbackithValidGrandParentProvider_AddsToGrandParent()
        {
            var serviceInstance = new object();
            var grandparentContainer = new ServiceContainer();
            var grandparentProvider = new MockServiceProvider();
            grandparentProvider.Setup(typeof(IServiceContainer), grandparentContainer);
            grandparentProvider.Setup(typeof(object), null);
            var parentContainer = new ServiceContainer(grandparentProvider);
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            parentProvider.Setup(typeof(object), null);
            var container = new ServiceContainer(parentProvider);

            ServiceCreatorCallback callback = (c, serviceType) =>
            {
                Assert.Same(grandparentContainer, c);
                Assert.Equal(typeof(object), serviceType);
                return serviceInstance;
            };
            container.AddService(typeof(object), callback, promote: true);
            Assert.Null(container.GetService(typeof(object)));
            Assert.Null(parentContainer.GetService(typeof(object)));
            Assert.Same(serviceInstance, grandparentContainer.GetService(typeof(object)));
        }

        [Fact]
        public void AddService_NullCallback_ThrowsArgumentNullException()
        {
            var container = new ServiceContainer();
            Assert.Throws<ArgumentNullException>("callback", () => container.AddService(typeof(object), null));
            Assert.Throws<ArgumentNullException>("callback", () => container.AddService(typeof(object), null, true));
            Assert.Throws<ArgumentNullException>("callback", () => container.AddService(typeof(object), null, false));
        }

        [Fact]
        public void AddService_AlreadyAddedCallback_ThrowsArgumentException()
        {
            var container = new ServiceContainer();
            var serviceInstance = new object();
            ServiceCreatorCallback callback = (container, serviceType) => "abc";
            container.AddService(typeof(object), callback);
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), new object()));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), new object(), true));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), new object(), false));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), callback));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), callback, true));
            Assert.Throws<ArgumentException>("serviceType", () => container.AddService(typeof(object), callback, false));
        }

        [Fact]
        public void Dispose_Invoke_ClearsServices()
        {
            var container = new ServiceContainer();
            container.AddService(typeof(object), new object());
            container.Dispose();
            Assert.Null(container.GetService(typeof(object)));

            // Dispose again.
            container.Dispose();
            Assert.Null(container.GetService(typeof(object)));
        }

        [Fact]
        public void Dispose_InvokeWithDisposableObject_CallsDispose()
        {
            var componentContainer = new Container();
            var component = new Component();
            componentContainer.Add(component);
            Assert.Same(componentContainer, component.Container);

            var container = new ServiceContainer();
            container.AddService(typeof(object), component);
            container.Dispose();
            Assert.Null(component.Container);
            Assert.Null(container.GetService(typeof(object)));

            // Dispose again.
            container.Dispose();
            Assert.Null(component.Container);
            Assert.Null(container.GetService(typeof(object)));
        }

        [Fact]
        public void Dispose_InvokeDisposing_ClearsServices()
        {
            var container = new SubServiceContainer();
            var serviceInstance = new object();
            container.AddService(typeof(object), serviceInstance);

            container.Dispose(disposing: false);
            Assert.Same(serviceInstance, container.GetService(typeof(object)));

            container.Dispose(disposing: true);
            Assert.Null(container.GetService(typeof(object)));

            // Dispose again.
            container.Dispose(disposing: true);
            Assert.Null(container.GetService(typeof(object)));
        }

        [Fact]
        public void Dispose_InvokeDisposingWithDisposableObject_CallsDispose()
        {
            var componentContainer = new Container();
            var component = new Component();
            componentContainer.Add(component);
            Assert.Same(componentContainer, component.Container);

            var container = new SubServiceContainer();
            container.AddService(typeof(object), component);

            container.Dispose(disposing: false);
            Assert.Same(componentContainer, component.Container);
            Assert.Same(component, container.GetService(typeof(object)));

            container.Dispose(disposing: true);
            Assert.Null(component.Container);
            Assert.Null(container.GetService(typeof(object)));

            // Dispose again.
            container.Dispose(disposing: true);
            Assert.Null(component.Container);
            Assert.Null(container.GetService(typeof(object)));
        }

        [Theory]
        [InlineData(typeof(IServiceContainer))]
        [InlineData(typeof(ServiceContainer))]
        public void GetService_DefaultService_ReturnsExpected(Type serviceType)
        {
            var container = new ServiceContainer();
            Assert.Same(container, container.GetService(serviceType));

            // Should return the container even if overridden.
            container.AddService(serviceType, new ServiceContainer());
            Assert.Same(container, container.GetService(serviceType));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetService_NoSuchService_ReturnsNull(Type serviceType)
        {
            var container = new ServiceContainer();
            Assert.Null(container.GetService(serviceType));
        }

        public static IEnumerable<object[]> RemoveService_Type_TestData()
        {
            var nullServiceProvider = new MockServiceProvider();
            nullServiceProvider.Setup(typeof(IServiceContainer), null);
            nullServiceProvider.Setup(typeof(int), null);
            yield return new object[] { nullServiceProvider };

            var invalidServiceProvider = new MockServiceProvider();
            invalidServiceProvider.Setup(typeof(IServiceContainer), new object());
            invalidServiceProvider.Setup(typeof(int), null);
            yield return new object[] { invalidServiceProvider };

            var validServiceProvider = new MockServiceProvider();
            validServiceProvider.Setup(typeof(IServiceContainer), new ServiceContainer());
            validServiceProvider.Setup(typeof(int), null);
            yield return new object[] { validServiceProvider };
        }

        [Theory]
        [MemberData(nameof(RemoveService_Type_TestData))]
        public void RemoveService_InvokeType_GetServiceReturnsNull(IServiceProvider parentProvider)
        {
            var container = new ServiceContainer(parentProvider);
            container.AddService(typeof(int), 1);
            container.RemoveService(typeof(int));
            Assert.Null(container.GetService(typeof(int)));
        }

        public static IEnumerable<object[]> RemoveService_TypeBool_TestData()
        {
            var nullServiceProvider = new MockServiceProvider();
            nullServiceProvider.Setup(typeof(IServiceContainer), null);
            nullServiceProvider.Setup(typeof(int), null);
            yield return new object[] { nullServiceProvider, true };
            yield return new object[] { nullServiceProvider, false };

            var invalidServiceProvider = new MockServiceProvider();
            invalidServiceProvider.Setup(typeof(IServiceContainer), new object());
            invalidServiceProvider.Setup(typeof(int), null);
            yield return new object[] { invalidServiceProvider, true };
            yield return new object[] { invalidServiceProvider, false };

            var validServiceProvider = new MockServiceProvider();
            validServiceProvider.Setup(typeof(IServiceContainer), new ServiceContainer());
            validServiceProvider.Setup(typeof(int), null);
            yield return new object[] { validServiceProvider, false };
        }

        [Theory]
        [MemberData(nameof(RemoveService_TypeBool_TestData))]
        public void RemoveService_InvokeTypeBool_GetServiceReturnsNull(IServiceProvider parentProvider, bool promote)
        {
            var container = new ServiceContainer(parentProvider);
            container.AddService(typeof(int), 1);
            container.RemoveService(typeof(int), promote);
            Assert.Null(container.GetService(typeof(int)));
        }

        [Fact]
        public void RemoveService_PromoteServiceInstanceWithValidParentProvider_RemovesFromParent()
        {
            var serviceInstance = new object();
            var parentContainer = new ServiceContainer();
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            parentProvider.Setup(typeof(object), null);
            var container = new ServiceContainer(parentProvider);

            container.AddService(typeof(object), serviceInstance, promote: true);
            container.RemoveService(typeof(object), promote: true);
            Assert.Null(container.GetService(typeof(object)));
            Assert.Null(parentContainer.GetService(typeof(object)));
        }

        [Fact]
        public void RemoveService_PromoteServiceInstanceWithValidGrandParentProvider_RemovesFromGrandParent()
        {
            var serviceInstance = new object();
            var grandparentContainer = new ServiceContainer();
            var grandparentProvider = new MockServiceProvider();
            grandparentProvider.Setup(typeof(IServiceContainer), grandparentContainer);
            grandparentProvider.Setup(typeof(object), null);
            var parentContainer = new ServiceContainer(grandparentProvider);
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            parentProvider.Setup(typeof(object), null);
            var container = new ServiceContainer(parentProvider);

            container.AddService(typeof(object), serviceInstance, promote: true);
            container.RemoveService(typeof(object), promote: true);
            Assert.Null(container.GetService(typeof(object)));
            Assert.Null(parentContainer.GetService(typeof(object)));
            Assert.Null(grandparentContainer.GetService(typeof(object)));
        }

        public static IEnumerable<object[]> RemoveService_Promote_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { typeof(int) };
        }

        [Theory]
        [MemberData(nameof(RemoveService_Promote_TestData))]
        public void RemoveService_Promote_Success(Type serviceType)
        {
            var parentContainer = new CustomServiceContainer();
            int removeServiceCallCount = 0;
            parentContainer.RemoveServiceAction = (callbackServiceType, callbackPromote) =>
            {
                Assert.Same(serviceType, callbackServiceType);
                Assert.True(callbackPromote);
                removeServiceCallCount++;
            };
            var parentProvider = new MockServiceProvider();
            parentProvider.Setup(typeof(IServiceContainer), parentContainer);
            var container = new ServiceContainer(parentProvider);

            container.RemoveService(serviceType, promote: true);
            Assert.Equal(1, removeServiceCallCount);
        }

        [Fact]
        public void RemoveService_NoSuchService_Nop()
        {
            var container = new ServiceContainer();
            container.RemoveService(typeof(int));
            container.RemoveService(typeof(int), promote: true);
            container.RemoveService(typeof(int), false);
        }

        [Fact]
        public void RemoveService_NullServiceType_ThrowsArgumentNullException()
        {
            var container = new ServiceContainer();
            Assert.Throws<ArgumentNullException>("serviceType", () => container.RemoveService(null));
            Assert.Throws<ArgumentNullException>("serviceType", () => container.RemoveService(null, true));
            Assert.Throws<ArgumentNullException>("serviceType", () => container.RemoveService(null, false));
        }

        private class SubServiceContainer : ServiceContainer
        {
            public SubServiceContainer() : base()
            {
            }

            public SubServiceContainer(IServiceProvider parentProvider) : base(parentProvider)
            {
            }

            public new Type[] DefaultServices => base.DefaultServices;

            public new void Dispose(bool disposing) => base.Dispose(disposing);
        }

        private class CustomServiceContainer : IServiceContainer
        {
            public void AddService(Type serviceType, object serviceInstance)
            {
                throw new NotImplementedException();
            }

            public Action<Type, object, bool> AddServiceObjectAction { get; set; }

            public void AddService(Type serviceType, object serviceInstance, bool promote)
            {
                AddServiceObjectAction(serviceType, serviceInstance, promote);
            }

            public void AddService(Type serviceType, ServiceCreatorCallback callback)
            {
                throw new NotImplementedException();
            }

            public Action<Type, ServiceCreatorCallback, bool> AddServiceCallbackAction { get; set; }

            public void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
            {
                AddServiceCallbackAction(serviceType, callback, promote);
            }

            public object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }

            public void RemoveService(Type serviceType)
            {
                throw new NotImplementedException();
            }

            public Action<Type, bool> RemoveServiceAction { get; set; }

            public void RemoveService(Type serviceType, bool promote)
            {
                RemoveServiceAction(serviceType, promote);
            }
        }
    }
}
