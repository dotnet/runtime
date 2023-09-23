// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.ComponentModel.Tests
{
    [Collection(nameof(DisableParallelization))] // manipulates cache
    public class ConcurrentTypeDescriptorTests
    {
        private long _error = 0;
        private bool Error
        {
            get => Interlocked.Read(ref _error) == 1;
            set => Interlocked.Exchange(ref _error, value ? 1 : 0);
        }
        private void ConcurrentTest(SomeType instance)
        {
            var properties = TypeDescriptor.GetProperties(instance);
            Thread.Sleep(10);
            if (properties.Count > 0)
            {
                Error = true;
            }
        }

        [Fact]
        public void GetProperties_ReturnsExpected()
        {
            const int Timeout = 60000;
            int concurrentCount = Environment.ProcessorCount * 2;

            using var finished = new CountdownEvent(concurrentCount);

            var instances = new SomeType[concurrentCount];
            for (int i = 0; i < concurrentCount; i++)
            {
                instances[i] = new SomeType();
            }

            for (int i = 0; i < concurrentCount; i++)
            {
                int i2 = i;
                new Thread(() =>
                {
                    ConcurrentTest(instances[i2]);
                    finished.Signal();
                }).Start();
            }

            finished.Wait(Timeout);

            if (finished.CurrentCount != 0)
            {
                Assert.Fail("Timeout. Possible deadlock.");
            }
            else
            {
                Assert.False(Error, "Fallback type descriptor is used.");
            }
        }
        internal class SomeTypeProvider : TypeDescriptionProvider
        {
            public static ThreadLocal<bool> Constructed = new ThreadLocal<bool>();
            public static ThreadLocal<bool> GetPropertiesCalled = new ThreadLocal<bool>();
            private class CTD : ICustomTypeDescriptor
            {
                public AttributeCollection GetAttributes() => AttributeCollection.Empty;
                public string? GetClassName() => null;
                public string? GetComponentName() => null;
                public TypeConverter GetConverter() => new TypeConverter();
                public EventDescriptor? GetDefaultEvent() => null;
                public PropertyDescriptor? GetDefaultProperty() => null;
                public object? GetEditor(Type editorBaseType) => null;
                public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;
                public EventDescriptorCollection GetEvents(Attribute[]? attributes) => EventDescriptorCollection.Empty;

                public PropertyDescriptorCollection GetProperties()
                {
                    GetPropertiesCalled.Value = true;
                    return PropertyDescriptorCollection.Empty;
                }

                public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
                {
                    throw new NotImplementedException();
                }

                public object? GetPropertyOwner(PropertyDescriptor? pd) => null;
            }
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
            {
                Constructed.Value = true;
                return new CTD();
            }
        }

        [TypeDescriptionProvider(typeof(SomeTypeProvider))]
        internal sealed class SomeType
        {
            public int SomeProperty { get; set; }
        }
    }
}
