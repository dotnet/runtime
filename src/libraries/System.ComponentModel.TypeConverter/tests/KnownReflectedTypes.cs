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
        public static void GetPropertiesAndEventsFromKnownType_NotRegistered_Throws()
        {
            RemoteExecutor.Invoke(() =>
            {
                // This throws even if we aren't trimming since we are calling KnownType APIs.
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetPropertiesFromKnownType(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetEventsFromKnownType(typeof(C1)));
            }).Dispose();
        }

        [Fact]
        public static void GetPropertiesAndEventsFromIntrinsicType_NotRegistered()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(0, TypeDescriptor.GetProperties(typeof(double)).Count);
                Assert.Equal(0, TypeDescriptor.GetPropertiesFromKnownType(typeof(int)).Count);
                Assert.Equal(0, TypeDescriptor.GetEventsFromKnownType(typeof(int)).Count);
            }).Dispose();
        }

        [Fact]
        public static void GetPropertiesAndEvents_NotRegistered_Trimmed_Throws()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetProperties(typeof(C1)));
                Assert.Throws<InvalidOperationException>(() => TypeDescriptor.GetEvents(typeof(C1)));
            }, options).Dispose();
        }

        [Fact]
        public static void GetPropertiesAndEventsFromKnownType_Registered_Trimmed()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[TypeDescriptorIsTrimmableSwitchName] = bool.TrueString;

            RemoteExecutor.Invoke(() =>
            {
                TypeDescriptor.AddKnownReflectedType<C1>();
                TypeDescriptionProvider provider = TypeDescriptor.GetProvider(typeof(C1));
                Assert.Equal(2, TypeDescriptor.GetPropertiesFromKnownType(typeof(C1)).Count);
                Assert.Equal(2, TypeDescriptor.GetEventsFromKnownType(typeof(C1)).Count);
            }, options).Dispose();
        }

        [Fact]
        public static void IntrinsicTypesAreKnownTypes()
        {
            Assert.IsType<ByteConverter>(TypeDescriptor.GetConverterFromKnownType(typeof(byte)));
            Assert.IsType<Int64Converter>(TypeDescriptor.GetConverterFromKnownType(typeof(long)));
            Assert.IsType<UriTypeConverter>(TypeDescriptor.GetConverterFromKnownType(typeof(Uri)));
        }

        private class C1
        {
            public int P1 { get; set; }
            public int P2 { get; set; }

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
    }
}
