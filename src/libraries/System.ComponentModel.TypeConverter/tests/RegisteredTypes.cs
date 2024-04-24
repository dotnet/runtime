// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class RegisteredTypes
    {
        private const string TypeDescriptorIsTrimmableSwitchName = "System.ComponentModel.TypeDescriptor.IsTrimmable";

        [Fact]
        public static void NullableGetConverterUnderlyingType()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<ClassWithGenericProperty>();
                TypeDescriptor.RegisterType<MyStruct>();
                TypeDescriptor.RegisterType<MyStructWithCustomConverter>();

                // Intrinsic type
                NullableConverter nullableConverter = (NullableConverter)TypeDescriptor.GetConverter(typeof(byte?));
                Assert.IsType<ByteConverter>(nullableConverter.UnderlyingTypeConverter);
                Assert.Equal(typeof(byte), nullableConverter.UnderlyingType);

                // Custom type
                TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(ClassWithGenericProperty));
                Assert.IsType<TypeConverter>(typeConverter);
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(ClassWithGenericProperty));
                // Ensure the inner type is not trimmed.
                Assert.Equal("NullableStruct", properties[0].Name);
                typeConverter = properties[0].Converter;
                Assert.IsType<NullableConverter>(typeConverter);
                Assert.True(typeConverter.CanConvertTo(typeof(MyStruct)));
                // Ensure the inner type is not trimmed. Todo: add test where the inner type has its own provider
                Assert.Equal("NullableStructWithCustomConverter", properties[1].Name);
                typeConverter = properties[1].Converter;
                Assert.IsType<NullableConverter>(typeConverter);
                Assert.True(typeConverter.CanConvertTo(typeof(MyStructWithCustomConverter)));

            }, options).Dispose();
        }

        [Fact]
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

        [Fact]
        public static void GetMembers_NotRegistered_Trimmed_Throws()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetProperties(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetEvents(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetConverter(typeof(C1)));
            }, options).Dispose();
        }

        [Fact]
        public static void GetMembersFromRegisteredType_Registered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));

                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1));
                Assert.Equal(2, properties.Count);
                Assert.Equal("System.ComponentModel.Int32Converter", properties[0].ConverterFromRegisteredType.ToString());
                Assert.Equal(2, TypeDescriptor.GetEventsFromRegisteredType(typeof(C1)).Count);
            }, options).Dispose();
        }

        [Fact]
        public static void GetMembersFromRegisteredType_ChildRegistered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                TypeDescriptor.RegisterType<C2>();
                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1));
                Assert.Equal("Class", properties[1].Name);
                Assert.Equal(2, properties[1].GetChildProperties().Count);
            }, options).Dispose();
        }

        [Fact]
        public static void GetMembersFromRegisteredType_ChildUnregistered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1));
                Assert.Equal("Class", properties[1].Name);
                Assert.Throws<InvalidOperationException>(() => properties[1].GetChildProperties());
            }, options).Dispose();
        }

        [Fact]
        public static void GetMembersFromRegisteredType_BaseClassUnregistered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.RegisterType<C1>();
                TypeDescriptor.RegisterType<C2>();

                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(typeof(C1));
                Assert.Equal("Int32", properties[0].Name);
                Assert.Equal("Class", properties[1].Name);
                Assert.Equal(2, properties[1].GetChildProperties().Count);
                Assert.Equal("Bool", properties[1].GetChildProperties()[0].Name);

                // Even though C1.Class.Base is not registered, we should still be able to get the properties of Base.
                Assert.Equal("String", properties[1].GetChildProperties()[1].Name);
            }, options).Dispose();
        }

        [Fact]
        public static void GetPropertiesFromRegisteredTypeInstance()
        {
            TypeDescriptor.RegisterType<C1>();
            TypeDescriptor.RegisterType<C2>();

            PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromRegisteredType(new C1());
            Assert.Equal("Int32", properties[0].Name);
            Assert.Equal("Class", properties[1].Name);
            Assert.Equal(2, properties[1].GetChildProperties().Count);
            Assert.Equal("Bool", properties[1].GetChildProperties()[0].Name);

            // Even though C1.Class.Base is not registered, we should still be able to get the properties of Base.
            Assert.Equal("String", properties[1].GetChildProperties()[1].Name);
        }

        [Fact]
        public static void GetPropertiesFromRegisteredTypeInstance_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                GetPropertiesFromRegisteredTypeInstance();
            }, options).Dispose();
        }

        [Fact]
        public static void GetPropertiesFromRegisteredTypeInstance_Unregistered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetPropertiesFromRegisteredType(new C1()));
            }, options).Dispose();
        }

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

        private class ClassWithGenericProperty
        {
            public MyStruct? NullableStruct { get; set; }
            public MyStructWithCustomConverter? NullableStructWithCustomConverter { get; set; }
        }

        private struct MyStruct
        {
            public int Int32 { get; set; }
        }

        [TypeConverter(typeof(MyStructConverter))]
        private struct MyStructWithCustomConverter
        {
            public int Int32 { get; set; }
        }

        internal class MyStructConverter : TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(MyStructWithCustomConverter);
        }
    }
}
