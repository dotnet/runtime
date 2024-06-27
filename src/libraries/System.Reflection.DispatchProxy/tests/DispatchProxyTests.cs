// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using Xunit;

namespace DispatchProxyTests
{
    public static class DispatchProxyTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Proxy_Derives_From_DispatchProxy_BaseType(bool useGenericCreate)
        {
            TestType_IHelloService proxy = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);

            Assert.NotNull(proxy);
            Assert.IsAssignableFrom<TestDispatchProxy>(proxy);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Proxy_Implements_All_Interfaces(bool useGenericCreate)
        {
            TestType_IHelloAndGoodbyeService proxy = CreateHelper<TestType_IHelloAndGoodbyeService, TestDispatchProxy>(useGenericCreate);

            Assert.NotNull(proxy);
            Type[] implementedInterfaces = typeof(TestType_IHelloAndGoodbyeService).GetTypeInfo().ImplementedInterfaces.ToArray();
            foreach (Type t in implementedInterfaces)
            {
                Assert.IsAssignableFrom(t, proxy);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Proxy_Internal_Interface(bool useGenericCreate)
        {
            TestType_InternalInterfaceService proxy = CreateHelper<TestType_InternalInterfaceService, TestDispatchProxy>(useGenericCreate);
            Assert.NotNull(proxy);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Proxy_Implements_Internal_Interfaces(bool useGenericCreate)
        {
            TestType_InternalInterfaceService proxy = CreateHelper<TestType_PublicInterfaceService_Implements_Internal, TestDispatchProxy>(useGenericCreate);
            Assert.NotNull(proxy);

            // ensure we emit a valid attribute definition
            Type iactAttributeType = proxy.GetType().Assembly.GetType("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute");
            Assert.NotNull(iactAttributeType);
            ConstructorInfo constructor = iactAttributeType.GetConstructor(new[] { typeof(string) });
            Assert.NotNull(constructor);
            PropertyInfo propertyInfo = iactAttributeType.GetProperty("AssemblyName");
            Assert.NotNull(propertyInfo);
            Assert.NotNull(propertyInfo.GetMethod);

            string name = "anAssemblyName";
            object attributeInstance = constructor.Invoke(new object[] { name });
            Assert.NotNull(attributeInstance);
            object actualName = propertyInfo.GetMethod.Invoke(attributeInstance, null);
            Assert.Equal(name, actualName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Same_Proxy_Type_And_Base_Type_Reuses_Same_Generated_Type(bool useGenericCreate)
        {
            TestType_IHelloService proxy1 = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            TestType_IHelloService proxy2 = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);

            Assert.NotNull(proxy1);
            Assert.NotNull(proxy2);
            Assert.IsType(proxy1.GetType(), proxy2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Proxy_Instances_Of_Same_Proxy_And_Base_Type_Are_Unique(bool useGenericCreate)
        {
            TestType_IHelloService proxy1 = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            TestType_IHelloService proxy2 = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);

            Assert.NotNull(proxy1);
            Assert.NotNull(proxy2);
            Assert.False(object.ReferenceEquals(proxy1, proxy2),
                        string.Format("First and second instance of proxy type {0} were the same instance", proxy1.GetType().ToString()));
        }


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Same_Proxy_Type_With_Different_BaseType_Uses_Different_Generated_Type(bool useGenericCreate)
        {
            TestType_IHelloService proxy1 = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            TestType_IHelloService proxy2 = CreateHelper<TestType_IHelloService, TestDispatchProxy2>(useGenericCreate);

            Assert.NotNull(proxy1);
            Assert.NotNull(proxy2);
            Assert.False(proxy1.GetType() == proxy2.GetType(),
                        string.Format("Proxy generated for base type {0} used same for base type {1}", typeof(TestDispatchProxy).Name, typeof(TestDispatchProxy).Name));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Created_Proxy_With_Different_Proxy_Type_Use_Different_Generated_Type(bool useGenericCreate)
        {
            TestType_IHelloService proxy1 = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            TestType_IGoodbyeService proxy2 = CreateHelper<TestType_IGoodbyeService, TestDispatchProxy>(useGenericCreate);

            Assert.NotNull(proxy1);
            Assert.NotNull(proxy2);
            Assert.False(proxy1.GetType() == proxy2.GetType(),
                        string.Format("Proxy generated for type {0} used same for type {1}", typeof(TestType_IHelloService).Name, typeof(TestType_IGoodbyeService).Name));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_Concrete_Proxy_Type_Throws_ArgumentException(bool useGenericCreate)
        {
            AssertExtensions.Throws<ArgumentException>(useGenericCreate ? "T" : "interfaceType", () => CreateHelper<TestType_ConcreteClass, TestDispatchProxy>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_Sealed_BaseType_Throws_ArgumentException(bool useGenericCreate)
        {
            AssertExtensions.Throws<ArgumentException>(useGenericCreate ? "TProxy" : "proxyType", () => CreateHelper<TestType_IHelloService, Sealed_TestDispatchProxy>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_Abstract_BaseType_Throws_ArgumentException(bool useGenericCreate)
        {
            AssertExtensions.Throws<ArgumentException>(useGenericCreate ? "TProxy" : "proxyType", () => CreateHelper<TestType_IHelloService, Abstract_TestDispatchProxy>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_Abstract_Generic_BaseType_Throws_ArgumentException(bool useGenericCreate)
        {
            AssertExtensions.Throws<ArgumentException>(useGenericCreate ? "TProxy" : "proxyType", () => CreateHelper<TestType_IHelloService, Abstract_GenericDispatchProxy<TestDispatchProxy>>(useGenericCreate));
        }

        [Fact]
        public static void Create_Using__Generic_BaseType_Throws_ArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("proxyType", () => DispatchProxy.Create(typeof(TestType_IHelloService), typeof(TestType_DipatchProxyGenericConstraint<TestDispatchProxy>)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_BaseType_Without_Default_Ctor_Throws_ArgumentException(bool useGenericCreate)
        {
            AssertExtensions.Throws<ArgumentException>(useGenericCreate ? "TProxy" : "proxyType", () => CreateHelper<TestType_IHelloService, NoDefaultCtor_TestDispatchProxy>(useGenericCreate));
        }

        [Fact]
        public static void Non_Generic_Create_With_Null_InterfaceType_Throws_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("interfaceType", () => DispatchProxy.Create(null, typeof(NoDefaultCtor_TestDispatchProxy)));
        }

        [Fact]
        public static void Non_Generic_Create_With_Null_ProxyType_Throws_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("proxyType", () => DispatchProxy.Create(typeof(TestType_IHelloService), null));
        }

        [Fact]
        public static void Non_Generic_Create_With_ProxyType_That_Is_Not_Assignable_To_DispatchProxy_Throws_ArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("proxyType", () => DispatchProxy.Create(typeof(TestType_IHelloService), typeof(object)));
        }

        [Fact]
        public static void Create_Using_PrivateProxy()
        {
            Assert.NotNull(TestType_PrivateProxy.Proxy<TestType_IHelloService>());
        }

        [Fact]
        public static void Create_Using_PrivateProxyAndInternalService()
        {
            Assert.NotNull(TestType_PrivateProxy.Proxy<TestType_InternalInterfaceService>());
        }

        [Fact]
        public static void Create_Using_PrivateProxyAndInternalServiceWithExternalGenericArgument()
        {
            Assert.NotNull(TestType_PrivateProxy.Proxy<TestType_InternalInterfaceWithNonPublicExternalGenericArgument>());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_InternalProxy(bool useGenericCreate)
        {
            Assert.NotNull(CreateHelper<TestType_InternalInterfaceService, InternalInvokeProxy>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_ExternalNonPublicService(bool useGenericCreate)
        {
            Assert.NotNull(CreateHelper<DispatchProxyTestDependency.TestType_IExternalNonPublicHiService, TestDispatchProxy>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_InternalProxyWithExternalNonPublicBaseType(bool useGenericCreate)
        {
            Assert.NotNull(CreateHelper<TestType_IHelloService, TestType_InternalProxyInternalBaseType>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_InternalServiceImplementingNonPublicExternalService(bool useGenericCreate)
        {
            Assert.NotNull(CreateHelper<TestType_InternalInterfaceImplementsNonPublicExternalType, TestDispatchProxy>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_InternalServiceWithGenericArgumentBeingNonPublicExternalService(bool useGenericCreate)
        {
            Assert.NotNull(CreateHelper<TestType_InternalInterfaceWithNonPublicExternalGenericArgument, TestDispatchProxy>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Create_Using_InternalProxyWithBaseTypeImplementingServiceWithgenericArgumentBeingNonPublicExternalService(bool useGenericCreate)
        {
            Assert.NotNull(CreateHelper<TestType_IHelloService, TestType_InternalProxyImplementingInterfaceWithGenericArgumentBeingNonPublicExternalType>(useGenericCreate));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Receives_Correct_MethodInfo_And_Arguments(bool useGenericCreate)
        {
            bool wasInvoked = false;
            StringBuilder errorBuilder = new StringBuilder();

            // This Func is called whenever we call a method on the proxy.
            // This is where we validate it received the correct arguments and methods
            Func<MethodInfo, object[], object> invokeCallback = (method, args) =>
            {
                wasInvoked = true;

                if (method == null)
                {
                    string error = string.Format("Proxy for {0} was called with null method", typeof(TestType_IHelloService).Name);
                    errorBuilder.AppendLine(error);
                    return null;
                }
                else
                {
                    MethodInfo expectedMethod = typeof(TestType_IHelloService).GetTypeInfo().GetDeclaredMethod("Hello");
                    if (expectedMethod != method)
                    {
                        string error = string.Format("Proxy for {0} was called with incorrect method.  Expected = {1}, Actual = {2}",
                                                    typeof(TestType_IHelloService).Name, expectedMethod, method);
                        errorBuilder.AppendLine(error);
                        return null;
                    }
                }

                return "success";
            };

            TestType_IHelloService proxy = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            Assert.NotNull(proxy);

            TestDispatchProxy dispatchProxy = proxy as TestDispatchProxy;
            Assert.NotNull(dispatchProxy);

            // Redirect Invoke to our own Func above
            dispatchProxy.CallOnInvoke = invokeCallback;

            // Calling this method now will invoke the Func above which validates correct method
            proxy.Hello("testInput");

            Assert.True(wasInvoked, "The invoke method was not called");
            Assert.True(errorBuilder.Length == 0, errorBuilder.ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Receives_Correct_MethodInfo(bool useGenericCreate)
        {
            MethodInfo invokedMethod = null;

            TestType_IHelloService proxy = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                invokedMethod = method;
                return string.Empty;
            };

            proxy.Hello("testInput");

            MethodInfo expectedMethod = typeof(TestType_IHelloService).GetTypeInfo().GetDeclaredMethod("Hello");
            Assert.True(invokedMethod != null && expectedMethod == invokedMethod, string.Format("Invoke expected method {0} but actual was {1}", expectedMethod, invokedMethod));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Receives_Correct_Arguments(bool useGenericCreate)
        {
            object[] actualArgs = null;

            TestType_IHelloService proxy = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                actualArgs = args;
                return string.Empty;
            };

            proxy.Hello("testInput");

            object[] expectedArgs = new object[] { "testInput" };
            Assert.True(actualArgs != null && actualArgs.Length == expectedArgs.Length,
                string.Format("Invoked expected object[] of length {0} but actual was {1}",
                                expectedArgs.Length, (actualArgs == null ? "null" : actualArgs.Length.ToString())));
            for (int i = 0; i < expectedArgs.Length; ++i)
            {
                Assert.True(expectedArgs[i].Equals(actualArgs[i]),
                    string.Format("Expected arg[{0}] = '{1}' but actual was '{2}'",
                    i, expectedArgs[i], actualArgs[i]));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Returns_Correct_Value(bool useGenericCreate)
        {
            TestType_IHelloService proxy = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                return "testReturn";
            };

            string expectedResult = "testReturn";
            string actualResult = proxy.Hello(expectedResult);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Multiple_Parameters_Receives_Correct_Arguments(bool useGenericCreate)
        {
            object[] invokedArgs = null;
            object[] expectedArgs = new object[] { (int)42, "testString", (double)5.0 };

            TestType_IMultipleParameterService proxy = CreateHelper<TestType_IMultipleParameterService, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                invokedArgs = args;
                return 0.0;
            };

            proxy.TestMethod((int)expectedArgs[0], (string)expectedArgs[1], (double)expectedArgs[2]);

            Assert.True(invokedArgs != null && invokedArgs.Length == expectedArgs.Length,
                        string.Format("Expected {0} arguments but actual was {1}",
                        expectedArgs.Length, invokedArgs == null ? "null" : invokedArgs.Length.ToString()));

            for (int i = 0; i < expectedArgs.Length; ++i)
            {
                Assert.True(expectedArgs[i].Equals(invokedArgs[i]),
                    string.Format("Expected arg[{0}] = '{1}' but actual was '{2}'",
                    i, expectedArgs[i], invokedArgs[i]));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Multiple_Parameters_Via_Params_Receives_Correct_Arguments(bool useGenericCreate)
        {
            object[] actualArgs = null;
            object[] invokedArgs = null;
            object[] expectedArgs = new object[] { 42, "testString", 5.0 };

            TestType_IMultipleParameterService proxy = CreateHelper<TestType_IMultipleParameterService, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                invokedArgs = args;
                return string.Empty;
            };

            proxy.ParamsMethod((int)expectedArgs[0], (string)expectedArgs[1], (double)expectedArgs[2]);

            // All separate params should have become a single object[1] array
            Assert.True(invokedArgs != null && invokedArgs.Length == 1,
                        string.Format("Expected single element object[] but actual was {0}",
                        invokedArgs == null ? "null" : invokedArgs.Length.ToString()));

            // That object[1] should contain an object[3] containing the args
            actualArgs = invokedArgs[0] as object[];
            Assert.True(actualArgs != null && actualArgs.Length == expectedArgs.Length,
                string.Format("Invoked expected object[] of length {0} but actual was {1}",
                                expectedArgs.Length, (actualArgs == null ? "null" : actualArgs.Length.ToString())));
            for (int i = 0; i < expectedArgs.Length; ++i)
            {
                Assert.True(expectedArgs[i].Equals(actualArgs[i]),
                    string.Format("Expected arg[{0}] = '{1}' but actual was '{2}'",
                    i, expectedArgs[i], actualArgs[i]));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Void_Returning_Method_Accepts_Null_Return(bool useGenericCreate)
        {
            MethodInfo invokedMethod = null;

            TestType_IOneWay proxy = CreateHelper<TestType_IOneWay, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                invokedMethod = method;
                return null;
            };

            proxy.OneWay();

            MethodInfo expectedMethod = typeof(TestType_IOneWay).GetTypeInfo().GetDeclaredMethod("OneWay");
            Assert.True(invokedMethod != null && expectedMethod == invokedMethod, string.Format("Invoke expected method {0} but actual was {1}", expectedMethod, invokedMethod));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Same_Method_Multiple_Interfaces_Calls_Correct_Method(bool useGenericCreate)
        {
            List<MethodInfo> invokedMethods = new List<MethodInfo>();

            TestType_IHelloService1And2 proxy = CreateHelper<TestType_IHelloService1And2, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                invokedMethods.Add(method);
                return null;
            };

            ((TestType_IHelloService)proxy).Hello("calling 1");
            ((TestType_IHelloService2)proxy).Hello("calling 2");

            Assert.True(invokedMethods.Count == 2, string.Format("Expected 2 method invocations but received {0}", invokedMethods.Count));

            MethodInfo expectedMethod = typeof(TestType_IHelloService).GetTypeInfo().GetDeclaredMethod("Hello");
            Assert.True(invokedMethods[0] != null && expectedMethod == invokedMethods[0], string.Format("First invoke should have been TestType_IHelloService.Hello but actual was {0}", invokedMethods[0]));

            expectedMethod = typeof(TestType_IHelloService2).GetTypeInfo().GetDeclaredMethod("Hello");
            Assert.True(invokedMethods[1] != null && expectedMethod == invokedMethods[1], string.Format("Second invoke should have been TestType_IHelloService2.Hello but actual was {0}", invokedMethods[1]));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Thrown_Exception_Rethrown_To_Caller(bool useGenericCreate)
        {
            Exception actualException = null;
            InvalidOperationException expectedException = new InvalidOperationException("testException");

            TestType_IHelloService proxy = CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                throw expectedException;
            };

            try
            {
                proxy.Hello("testCall");
            }
            catch (Exception e)
            {
                actualException = e;
            }

            Assert.Equal(expectedException, actualException);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Property_Setter_And_Getter_Invokes_Correct_Methods(bool useGenericCreate)
        {
            List<MethodInfo> invokedMethods = new List<MethodInfo>();

            TestType_IPropertyService proxy = CreateHelper<TestType_IPropertyService, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                invokedMethods.Add(method);
                return null;
            };


            proxy.ReadWrite = "testValue";
            string actualValue = proxy.ReadWrite;

            Assert.True(invokedMethods.Count == 2, string.Format("Expected 2 method invocations but received {0}", invokedMethods.Count));

            PropertyInfo propertyInfo = typeof(TestType_IPropertyService).GetTypeInfo().GetDeclaredProperty("ReadWrite");
            Assert.NotNull(propertyInfo);

            MethodInfo expectedMethod = propertyInfo.SetMethod;
            Assert.True(invokedMethods[0] != null && expectedMethod == invokedMethods[0], string.Format("First invoke should have been {0} but actual was {1}",
                            expectedMethod.Name, invokedMethods[0]));

            expectedMethod = propertyInfo.GetMethod;
            Assert.True(invokedMethods[1] != null && expectedMethod == invokedMethods[1], string.Format("Second invoke should have been {0} but actual was {1}",
                            expectedMethod.Name, invokedMethods[1]));

            Assert.Null(actualValue);
        }


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Proxy_Declares_Interface_Properties(bool useGenericCreate)
        {
            TestType_IPropertyService proxy = CreateHelper<TestType_IPropertyService, TestDispatchProxy>(useGenericCreate);
            PropertyInfo propertyInfo = proxy.GetType().GetTypeInfo().GetDeclaredProperty("ReadWrite");
            Assert.NotNull(propertyInfo);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Proxy_Declares_Interface_Static_Virtual_Properties(bool useGenericCreate)
        {
            TestType_IStaticVirtualPropertyService proxy = CreateHelper<TestType_IStaticVirtualPropertyService, TestDispatchProxy>(useGenericCreate);
            PropertyInfo? propertyInfo = proxy.GetType().GetTypeInfo().GetDeclaredProperty(nameof(TestType_IStaticVirtualPropertyService.TestProperty));
            Assert.NotNull(propertyInfo);
            Assert.True(propertyInfo.GetMethod!.IsStatic);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Proxy_Declares_Interface_Static_Virtual_Methods(bool useGenericCreate)
        {
            TestType_IStaticVirtualMethodService proxy = CreateHelper<TestType_IStaticVirtualMethodService, TestDispatchProxy>(useGenericCreate);
            MethodInfo? methodInfo = proxy.GetType().GetTypeInfo().GetDeclaredMethod(nameof(TestType_IStaticVirtualMethodService.TestMethod));
            Assert.NotNull(methodInfo);
            Assert.True(methodInfo.IsStatic);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Static_Virtual_Method_Throws_NotSupportedException(bool useGenericCreate)
        {
            TestType_IStaticVirtualMethodService proxy = CreateHelper<TestType_IStaticVirtualMethodService, TestDispatchProxy>(useGenericCreate);
            MethodInfo? methodInfo = proxy.GetType().GetTypeInfo().GetDeclaredMethod(nameof(TestType_IStaticVirtualMethodService.TestMethod));
            Assert.NotNull(methodInfo);
            Assert.Throws<NotSupportedException>(() => methodInfo.Invoke(proxy, BindingFlags.DoNotWrapExceptions, null, null, null));
        }

#if NET
        [Fact]
        public static void Invoke_Event_Add_And_Remove_And_Raise_Invokes_Correct_Methods_Generic_And_Non_Generic_Tests()
        {
            // C# cannot emit raise_Xxx method for the event, so we must use System.Reflection.Emit to generate such event.
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("EventBuilder"), AssemblyBuilderAccess.Run);
            ModuleBuilder modb = ab.DefineDynamicModule("mod");
            TypeBuilder tb = modb.DefineType($"TestType_IEventService", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            EventBuilder eb = tb.DefineEvent("AddRemoveRaise", EventAttributes.None, typeof(EventHandler));
            eb.SetAddOnMethod(tb.DefineMethod("add_AddRemoveRaise", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(EventHandler) }));
            eb.SetRemoveOnMethod(tb.DefineMethod("remove_AddRemoveRaise", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(EventHandler) }));
            eb.SetRaiseMethod(tb.DefineMethod("raise_AddRemoveRaise", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(EventArgs) }));
            TypeInfo ieventServiceTypeInfo = tb.CreateTypeInfo();

            Invoke_Event_Add_And_Remove_And_Raise_Invokes_Correct_Methods(ieventServiceTypeInfo, true);
            Invoke_Event_Add_And_Remove_And_Raise_Invokes_Correct_Methods(ieventServiceTypeInfo, false);
        }

        static void Invoke_Event_Add_And_Remove_And_Raise_Invokes_Correct_Methods(TypeInfo ieventServiceTypeInfo, bool useGenericCreate)
        {
            List<MethodInfo> invokedMethods = new List<MethodInfo>();
            object proxy;
            if (useGenericCreate)
            {
                proxy =
                    typeof(DispatchProxy)
                    .GetRuntimeMethod("Create", Type.EmptyTypes).MakeGenericMethod(ieventServiceTypeInfo.AsType(), typeof(TestDispatchProxy))
                    .Invoke(null, null);
            }
            else
            {
                proxy = typeof(DispatchProxy)
                    .GetRuntimeMethod("Create", new Type[] { typeof(Type), typeof(Type) })!
                    .Invoke(null, new object[] { ieventServiceTypeInfo.AsType(), typeof(TestDispatchProxy) });
            }

            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                invokedMethods.Add(method);
                return null;
            };

            EventHandler handler = new EventHandler((sender, e) => { });

            proxy.GetType().GetRuntimeMethods().Single(m => m.Name == "add_AddRemoveRaise").Invoke(proxy, new object[] { handler });
            proxy.GetType().GetRuntimeMethods().Single(m => m.Name == "raise_AddRemoveRaise").Invoke(proxy, new object[] { EventArgs.Empty });
            proxy.GetType().GetRuntimeMethods().Single(m => m.Name == "remove_AddRemoveRaise").Invoke(proxy, new object[] { handler });

            Assert.True(invokedMethods.Count == 3, String.Format("Expected 3 method invocations but received {0}", invokedMethods.Count));

            EventInfo eventInfo = ieventServiceTypeInfo.GetDeclaredEvent("AddRemoveRaise");
            Assert.NotNull(eventInfo);

            MethodInfo expectedMethod = eventInfo.AddMethod;
            Assert.True(invokedMethods[0] != null && expectedMethod == invokedMethods[0], String.Format("First invoke should have been {0} but actual was {1}",
                            expectedMethod.Name, invokedMethods[0]));

            expectedMethod = eventInfo.RaiseMethod;
            Assert.True(invokedMethods[1] != null && expectedMethod == invokedMethods[1], String.Format("Second invoke should have been {0} but actual was {1}",
                            expectedMethod.Name, invokedMethods[1]));

            expectedMethod = eventInfo.RemoveMethod;
            Assert.True(invokedMethods[2] != null && expectedMethod == invokedMethods[2], String.Format("Third invoke should have been {0} but actual was {1}",
                            expectedMethod.Name, invokedMethods[1]));
        }
#endif

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Proxy_Declares_Interface_Events(bool useGenericCreate)
        {
            TestType_IEventService proxy = CreateHelper<TestType_IEventService, TestDispatchProxy>(useGenericCreate);
            EventInfo eventInfo = proxy.GetType().GetTypeInfo().GetDeclaredEvent("AddRemove");
            Assert.NotNull(eventInfo);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Indexer_Setter_And_Getter_Invokes_Correct_Methods(bool useGenericCreate)
        {
            List<MethodInfo> invokedMethods = new List<MethodInfo>();

            TestType_IIndexerService proxy = CreateHelper<TestType_IIndexerService, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                invokedMethods.Add(method);
                return null;
            };

            proxy["key"] = "testValue";
            string actualValue = proxy["key"];

            Assert.True(invokedMethods.Count == 2, string.Format("Expected 2 method invocations but received {0}", invokedMethods.Count));

            PropertyInfo propertyInfo = typeof(TestType_IIndexerService).GetTypeInfo().GetDeclaredProperty("Item");
            Assert.NotNull(propertyInfo);

            MethodInfo expectedMethod = propertyInfo.SetMethod;
            Assert.True(invokedMethods[0] != null && expectedMethod == invokedMethods[0], string.Format("First invoke should have been {0} but actual was {1}",
                            expectedMethod.Name, invokedMethods[0]));

            expectedMethod = propertyInfo.GetMethod;
            Assert.True(invokedMethods[1] != null && expectedMethod == invokedMethods[1], string.Format("Second invoke should have been {0} but actual was {1}",
                            expectedMethod.Name, invokedMethods[1]));

            Assert.Null(actualValue);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Proxy_Declares_Interface_Indexers(bool useGenericCreate)
        {
            TestType_IIndexerService proxy = CreateHelper<TestType_IIndexerService, TestDispatchProxy>(useGenericCreate);
            PropertyInfo propertyInfo = proxy.GetType().GetTypeInfo().GetDeclaredProperty("Item");
            Assert.NotNull(propertyInfo);
        }

        static void TestGenericMethodRoundTrip<T>(T testValue, bool useGenericCreate)
        {
            var proxy = CreateHelper<TypeType_GenericMethod, TestDispatchProxy>(useGenericCreate);
            ((TestDispatchProxy)proxy).CallOnInvoke = (mi, a) =>
            {
                Assert.True(mi.IsGenericMethod);
                Assert.False(mi.IsGenericMethodDefinition);
                Assert.Equal(1, mi.GetParameters().Length);
                Assert.Equal(typeof(T), mi.GetParameters()[0].ParameterType);
                Assert.Equal(typeof(T), mi.ReturnType);
                return a[0];
            };
            Assert.Equal(proxy.Echo(testValue), testValue);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Generic_Method(bool useGenericCreate)
        {
            //string
            TestGenericMethodRoundTrip("asdf", useGenericCreate);
            //reference type
            TestGenericMethodRoundTrip(new Version(1, 0, 0, 0), useGenericCreate);
            //value type
            TestGenericMethodRoundTrip(42, useGenericCreate);
            //enum type
            TestGenericMethodRoundTrip(DayOfWeek.Monday, useGenericCreate);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Invoke_Ref_Out_In_Method(bool useGenericCreate)
        {
            string value = "Hello";

            TestRefOutInInvocation(p => p.InAttribute(value), "Hello", useGenericCreate);
            TestRefOutInInvocation(p => p.InAttribute_OutAttribute(value), "Hello", useGenericCreate);
            TestRefOutInInvocation(p => p.InAttribute_Ref(ref value), "Hello", useGenericCreate);
            TestRefOutInInvocation(p => p.Out(out _), null, useGenericCreate);
            TestRefOutInInvocation(p => p.OutAttribute(value), "Hello", useGenericCreate);
            TestRefOutInInvocation(p => p.Ref(ref value), "Hello", useGenericCreate);
            TestRefOutInInvocation(p => p.In(in value), "Hello", useGenericCreate);
        }

        private static void TestRefOutInInvocation(Action<TestType_IOut_Ref> invocation, string expected, bool useGenericCreate)
        {
            var proxy = CreateHelper<TestType_IOut_Ref, TestDispatchProxy>(useGenericCreate);

            string result = "Failed";

            ((TestDispatchProxy)proxy).CallOnInvoke = (method, args) =>
            {
                result = args[0] as string;
                return null;
            };

            invocation(proxy);

            Assert.Equal(expected, result);
        }

        private static TestType_IHelloService CreateTestHelloProxy(bool useGenericCreate) =>
            CreateHelper<TestType_IHelloService, TestDispatchProxy>(useGenericCreate);

        [ActiveIssue("https://github.com/dotnet/runtime/issues/62503", TestRuntimes.Mono)]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Test_Unloadability(bool useGenericCreate)
        {
            if (typeof(DispatchProxyTests).Assembly.Location == "")
                return;

            WeakReference wr = CreateProxyInUnloadableAlc(useGenericCreate);

            for (int i = 0; i < 10 && wr.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.False(wr.IsAlive, "The ALC could not be unloaded.");

            [MethodImpl(MethodImplOptions.NoInlining)]
            static WeakReference CreateProxyInUnloadableAlc(bool useGenericCreate)
            {
                var alc = new AssemblyLoadContext(nameof(Test_Unloadability), true);
                alc.LoadFromAssemblyPath(typeof(DispatchProxyTests).Assembly.Location)
                    .GetType(typeof(DispatchProxyTests).FullName, true)
                    .GetMethod(nameof(CreateTestHelloProxy), BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { useGenericCreate });
                return new WeakReference(alc);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Test_Multiple_AssemblyLoadContexts(bool useGenericCreate)
        {
            if (typeof(DispatchProxyTests).Assembly.Location == "")
                return;

            object proxyDefaultAlc = CreateTestDispatchProxy(typeof(TestDispatchProxy), useGenericCreate);
            Assert.True(proxyDefaultAlc.GetType().IsAssignableTo(typeof(TestDispatchProxy)));

            Type proxyCustomAlcType =
                new AssemblyLoadContext(nameof(Test_Multiple_AssemblyLoadContexts))
                    .LoadFromAssemblyPath(typeof(DispatchProxyTests).Assembly.Location)
                    .GetType(typeof(TestDispatchProxy).FullName, true);

            object proxyCustomAlc = CreateTestDispatchProxy(proxyCustomAlcType, useGenericCreate);
            Assert.True(proxyCustomAlc.GetType().IsAssignableTo(proxyCustomAlcType));

            static object CreateTestDispatchProxy(Type type, bool useGenericCreate)
            {
                if (useGenericCreate)
                {
                    return typeof(DispatchProxy)
                           // It has to be a type shared in both ALCs.
                           .GetMethod("Create", Type.EmptyTypes).MakeGenericMethod(typeof(IDisposable), type)
                           .Invoke(null, null);
                }
                else
                {
                    return typeof(DispatchProxy)
                           .GetMethod("Create", new Type[] { typeof(Type), typeof(Type) })!
                           .Invoke(null, new object[] { typeof(IDisposable), type });
                }
            }
        }

        [Fact]
        public static void Test_Multiple_AssemblyLoadContextsWithBadName()
        {
            if (typeof(DispatchProxyTests).Assembly.Location == "")
                return;

            Assembly assembly = Assembly.LoadFile(typeof(DispatchProxyTests).Assembly.Location);
            Type type = assembly.GetType(typeof(DispatchProxyTests).FullName);
            MethodInfo method = type.GetMethod(nameof(Demo), BindingFlags.NonPublic | BindingFlags.Static);
            Assert.True((bool)method.Invoke(null, null));
        }

        internal static bool Demo()
        {
            TestType_IHelloService proxy = DispatchProxy.Create<TestType_IHelloService, InternalInvokeProxy>();
            proxy.Hello("Hello");
            return true;
        }

        private static TInterface CreateHelper<TInterface, TProxy>(bool useGenericCreate) where TProxy : DispatchProxy
        {
            if (useGenericCreate)
            {
                return DispatchProxy.Create<TInterface, TProxy>();
            }

            return (TInterface)DispatchProxy.Create(typeof(TInterface), typeof(TProxy));
        }
    }
}
