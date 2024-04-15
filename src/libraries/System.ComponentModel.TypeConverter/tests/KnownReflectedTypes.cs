// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class KnownReflectedTypes
    {
        private const string TypeDescriptorIsTrimmableSwitchName = "System.ComponentModel.TypeDescriptor.IsTrimmable";

        [Fact]
        public static void GetMembersWithKnownType_NotRegistered()
        {
            RemoteExecutor.Invoke(() =>
            {
                // These throw even if we aren't trimming since we are calling KnownType APIs.
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetPropertiesFromKnownType(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetEventsFromKnownType(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetConverterFromKnownType(typeof(C1)));

                // Intrinsic types do not need to be registered.
                TypeDescriptor.GetPropertiesFromKnownType(typeof(string));
                TypeDescriptor.GetEventsFromKnownType(typeof(string));
                Assert.IsType<StringConverter>(TypeDescriptor.GetConverterFromKnownType(typeof(string)));
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
        public static void GetMembersFromKnownType_Registered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.AddKnownReflectedType<C1>();
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));

                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromKnownType(typeof(C1));
                Assert.Equal(2, properties.Count);
                Assert.Equal("System.ComponentModel.Int32Converter", properties[0].ConverterFromKnownType.ToString());
                Assert.Equal(2, TypeDescriptor.GetEventsFromKnownType(typeof(C1)).Count);
            }, options).Dispose();
        }

        [Fact]
        public static void GetMembersFromKnownType_ChildRegistered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.AddKnownReflectedType<C1>();
                TypeDescriptor.AddKnownReflectedType<C2>();
                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromKnownType(typeof(C1));
                Assert.Equal("Class", properties[1].Name);
                Assert.Equal(2, properties[1].GetChildProperties().Count);
            }, options).Dispose();
        }

        [Fact]
        public static void GetMembersFromKnownType_ChildUnregistered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.AddKnownReflectedType<C1>();
                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromKnownType(typeof(C1));
                Assert.Equal("Class", properties[1].Name);
                Assert.Throws<InvalidOperationException>(() => properties[1].GetChildProperties());
            }, options).Dispose();
        }

        [Fact]
        public static void GetMembersFromKnownType_BaseClassUnregistered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.AddKnownReflectedType<C1>();
                TypeDescriptor.AddKnownReflectedType<C2>();

                PropertyDescriptorCollection properties = TypeDescriptor.GetPropertiesFromKnownType(typeof(C1));
                Assert.Equal("Int32", properties[0].Name);
                Assert.Equal("Class", properties[1].Name);
                Assert.Equal(2, properties[1].GetChildProperties().Count);
                Assert.Equal("Bool", properties[1].GetChildProperties()[0].Name);

                // Even though C1.Class.Base is not registered, we should still be able to get the properties of Base.
                Assert.Equal("String", properties[1].GetChildProperties()[1].Name);
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

    }
}
