// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using static System.ComponentModel.Tests.TypeDescriptorTests;

namespace System.ComponentModel.Tests
{
    public class RegisteredTypesTests
    {
        private const string TypeDescriptorRequireRegisteredTypesSwitchName = "System.ComponentModel.TypeDescriptor.RequireRegisteredTypes";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void ApplyResourcesToRegisteredType_NotRegistered()
        {
            RemoteExecutor.Invoke(() =>
            {
                ComponentResourceManager resourceManager = new(typeof(global::Resources.TestResx));
                Assert.Throws<InvalidOperationException>(() => resourceManager.ApplyResourcesToRegisteredType(new C1(), "SomeName", null));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void ApplyResourcesToRegisteredType_Registered()
        {
            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                ComponentResourceManager resourceManager = new(typeof(global::Resources.TestResx));
                resourceManager.ApplyResourcesToRegisteredType(new C1(), "SomeName", null);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetMembersWithRegisteredType_NotRegistered()
        {
            RemoteExecutor.Invoke(() =>
            {
                // These throw even if we aren't trimming since we are calling RegisteredType APIs.
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetEventsFromRegisteredType(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetConverterFromRegisteredType(typeof(C1)));

                // Intrinsic types do not need to be registered.
                TypeDescriptor.GetPropertiesFromRegisteredType(typeof(string));
                TypeDescriptor.GetEventsFromRegisteredType(typeof(string));
                Assert.IsType<StringConverter>(TypeDescriptor.GetConverterFromRegisteredType(typeof(string)));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetMembers_NotRegistered_SwitchOn()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetProperties(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetEvents(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetConverter(typeof(C1)));
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetMembers_NotRegistered()
        {
            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(C1));
                Assert.Equal(2, properties.Count);
                Assert.Equal("System.ComponentModel.Int32Converter", properties[0].Converter.ToString());
                Assert.Equal(2, TypeDescriptor.GetEvents(typeof(C1)).Count);
                Assert.IsType<TypeConverter>(TypeDescriptor.GetConverter(typeof(C1)));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetMembers_FromRegisteredType_Registered_FullCoverage()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));

                Test_PropertyDescriptorCollection(TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1)));
                Test_PropertyDescriptorCollection(provider.GetTypeDescriptor(typeof(C1)).GetPropertiesFromRegisteredType());
                Test_PropertyDescriptorCollection(provider.GetTypeDescriptor(new C1()).GetPropertiesFromRegisteredType());
                Test_PropertyDescriptorCollection(provider.GetTypeDescriptor(typeof(C1), new C1()).GetPropertiesFromRegisteredType());

                static void Test_PropertyDescriptorCollection(PropertyDescriptorCollection collection)
                {
                    Assert.Equal(2, collection.Count);
                    Assert.Equal("System.ComponentModel.Int32Converter", collection[0].ConverterFromRegisteredType.ToString());
                }

                Test_EventDescriptorCollection(TypeDescriptor.GetEventsFromRegisteredType(typeof(C1)));
                Test_EventDescriptorCollection(provider.GetTypeDescriptor(typeof(C1)).GetEventsFromRegisteredType());
                Test_EventDescriptorCollection(provider.GetTypeDescriptor(new C1()).GetEventsFromRegisteredType());
                Test_EventDescriptorCollection(provider.GetTypeDescriptor(typeof(C1), new C1()).GetEventsFromRegisteredType());

                static void Test_EventDescriptorCollection(EventDescriptorCollection collection)
                {
                    Assert.Equal(2, collection.Count);
                }

                Test_GetConverter(TypeDescriptor.GetConverterFromRegisteredType(typeof(C1)));
                Test_GetConverter(provider.GetTypeDescriptor(typeof(C1)).GetConverterFromRegisteredType());
                Test_GetConverter(provider.GetTypeDescriptor(new C1()).GetConverterFromRegisteredType());
                Test_GetConverter(provider.GetTypeDescriptor(typeof(C1), new C1()).GetConverterFromRegisteredType());
                static void Test_GetConverter(TypeConverter converter)
                {
                    // The default type converter returns null.
                    Assert.Null(converter.GetProperties(null, new C1()));
                }

                // C2 has a base class; base class properties are included.
                TypeDescriptor.RegisterType<C2>();
                PropertyDescriptorCollection collection = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C2));
                Assert.Equal(2, collection.Count);
                Assert.Equal("Bool", collection[0].Name);
                Assert.Equal("String", collection[1].Name);
                Assert.Equal("Base", collection[1].ComponentType.Name);

                // Since the base class is not explicitly registered, we throw on GetPropertiesFromRegisteredType().
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetPropertiesFromRegisteredType(typeof(Base)));

            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void TypeDescriptionProvider_RegisterType()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));

                // Ensure RegisterType() forwards to the reflection provider (through TypeDescriptionNode and then DelegatingTypeDescriptionProvider)
                provider.RegisterType<C1>();

                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1));
                Assert.Equal(2, properties.Count);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetMembersFromRegisteredType_ChildRegistered_SwitchOn()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                TypeDescriptor.RegisterType<C2>();
                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1));
                Assert.Equal("Class", properties[1].Name);
                Assert.Equal(2, properties[1].GetChildProperties().Count);
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetMembersFromRegisteredType_ChildUnregistered_SwitchOn()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1));
                Assert.Equal("Class", properties[1].Name);
                Assert.Throws<InvalidOperationException>(() => properties[1].GetChildProperties());
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetMembersFromRegisteredType_BaseClassUnregistered_SwitchOn()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                TypeDescriptor.RegisterType<C2>();

                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1));
                Assert.Equal("Int32", properties[0].Name);
                Assert.Equal("Class", properties[1].Name);
                Assert.Equal(2, properties[1].GetChildProperties().Count);
                Assert.Equal("Bool", properties[1].GetChildProperties()[0].Name);

                // Even though C1.Class.Base is not explictely registered, we should still be able to get the properties of Base.
                Assert.Equal("String", properties[1].GetChildProperties()[1].Name);
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetPropertiesFromRegisteredTypeInstance()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                TypeDescriptor.RegisterType<C2>();

                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(new C1());
                Assert.Equal("Int32", properties[0].Name);
                Assert.Equal("Class", properties[1].Name);
                Assert.Equal(2, properties[1].GetChildProperties().Count);
                Assert.Equal("Bool", properties[1].GetChildProperties()[0].Name);

                // Even though C1.Class.Base is not explictely registered, we should still be able to get the properties of Base.
                Assert.Equal("String", properties[1].GetChildProperties()[1].Name);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetPropertiesFromRegisteredTypeInstance_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                GetPropertiesFromRegisteredTypeInstance();
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetPropertiesFromRegisteredTypeInstance_Unregistered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetPropertiesFromRegisteredType(new C1()));
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void LegacyProviders_ThrowOnFeatureSwitch_ICustomTypeDescriptor()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                EmptyPropertiesTypeProvider provider = new();

                // This uses the ICustomTypeDescriptor DIM implementation.
                Test(provider.GetTypeDescriptor(typeof(C1)));

                // This uses the CustomTypeDescriptor implementation.
                Test(new EmptyCustomTypeDescriptor());

                void Test(ICustomTypeDescriptor ictd)
                {
                    Exception ex;

                    ex = Assert.Throws<NotImplementedException>(() => ictd.GetPropertiesFromRegisteredType());
                    Assert.Contains("GetPropertiesFromRegisteredType", ex.Message);

                    ex = Assert.Throws<NotImplementedException>(() => ictd.GetEventsFromRegisteredType());
                    Assert.Contains("GetEventsFromRegisteredType", ex.Message);

                    ex = Assert.Throws<NotImplementedException>(() => ictd.GetConverterFromRegisteredType());
                    Assert.Contains("GetConverterFromRegisteredType", ex.Message);
                }
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void LegacyProviders_DotNotThrowWhenNoFeatureSwitch()
        {
            RemoteExecutor.Invoke(() =>
            {
                EmptyPropertiesTypeProvider provider = new();

                // This uses the ICustomTypeDescriptor DIM implementation.
                Test(provider.GetTypeDescriptor(typeof(C1)));

                // This uses the CustomTypeDescriptor implementation.
                Test(new EmptyCustomTypeDescriptor());

                void Test(ICustomTypeDescriptor ictd)
                {
                    ictd.GetPropertiesFromRegisteredType();
                    ictd.GetEventsFromRegisteredType();
                    ictd.GetConverterFromRegisteredType();
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetProviderWithInstance_Registered()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                C1 obj = new C1();
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(obj);
                Assert.NotNull(provider);

                ICustomTypeDescriptor instanceDescriptor = provider.GetExtendedTypeDescriptor(obj);
                Assert.Equal(0, instanceDescriptor.GetProperties().Count);
                Assert.Throws<NotImplementedException>(() => instanceDescriptor.GetPropertiesFromRegisteredType().Count);
                Assert.Equal(0, instanceDescriptor.GetEvents().Count);
                Assert.Throws<NotImplementedException>(() => instanceDescriptor.GetEventsFromRegisteredType().Count);
                Assert.NotNull(instanceDescriptor.GetConverterFromRegisteredType());

                instanceDescriptor = provider.GetTypeDescriptor(obj);
                Assert.Equal(2, instanceDescriptor.GetProperties().Count);
                Assert.Equal(2, instanceDescriptor.GetPropertiesFromRegisteredType().Count);
                Assert.Equal(2, instanceDescriptor.GetEvents().Count);
                Assert.Equal(2, instanceDescriptor.GetEventsFromRegisteredType().Count);
                Assert.NotNull(instanceDescriptor.GetConverterFromRegisteredType());
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void VerifyNullableUnderlying_Registered()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<ClassWithGenericProperty>();
                TypeDescriptor.RegisterType<MyStruct>();

                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(GetType_ClassWithGenericProperty());
                Assert.Equal(1, properties.Count);
                Assert.Equal("NullableStruct", properties[0].Name);
                Assert.NotNull(properties[0].Converter);

                static Type GetType_ClassWithGenericProperty() => typeof(ClassWithGenericProperty);
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void VerifyNullableUnderlying_NotRegistered()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorRequireRegisteredTypesSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<ClassWithGenericProperty>();

                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(GetType_ClassWithGenericProperty());
                Assert.Equal(1, properties.Count);
                Assert.Equal("NullableStruct", properties[0].Name);

                Assert.Throws<InvalidOperationException>(() => properties[0].Converter);

                static Type GetType_ClassWithGenericProperty() => typeof(ClassWithGenericProperty);
            }, options).Dispose();
        }

        internal class MyStruct
        {
        }

        class ClassWithGenericProperty
        {
            public MyStruct? NullableStruct { get; set; }
        }

        private sealed class EmptyCustomTypeDescriptor : CustomTypeDescriptor { }

        private class C1
        {
            public int Int32 { get; set; }
            public C2 Class { get; set; }

            public event EventHandler E1
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }

            public event EventHandler E2
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }
        }

        private class C2 : Base
        {
            public bool Bool { get; set; }
        }

        private class Base
        {
            public string String { get; set; }
        }
    }
}
