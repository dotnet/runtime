// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// MonoTests.System.ComponentModel.ReferenceConverterTest
//
// Author:
//      Ivan N. Zlatev  <contact@i-nz.net>
//
// Copyright (C) 2008 Ivan N. Zlatev
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ReferenceConverterTests : TypeConverterTestBase
    {

        public override TypeConverter Converter => new ReferenceConverter(typeof(ITestInterface));

        public override bool StandardValuesExclusive => true;
        public override bool StandardValuesSupported => true;
        public override bool CanConvertToString => false;

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            // This does actually succeed despite CanConvertFrom returning false.
            ConvertTest noContext = ConvertTest.Valid("reference name", null).WithContext(null);
            noContext.CanConvert = false;
            yield return noContext;

            // No IReferenceService or IContainer.
            yield return ConvertTest.Valid(string.Empty, null);
            yield return ConvertTest.Valid("   ", null);
            yield return ConvertTest.Valid("(none)", null).WithInvariantRemoteInvokeCulture();
            yield return ConvertTest.Valid("nothing", null);

            // IReferenceService.
            var nullReferenceServiceContext = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return null;
                }
            };
            yield return ConvertTest.Valid("reference name", null).WithContext(nullReferenceServiceContext);

            var invalidReferenceServiceContext = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return new object();
                }
            };
            yield return ConvertTest.Valid("reference name", null).WithContext(invalidReferenceServiceContext);

            var component1 = new TestComponent();
            var component2 = new TestComponent();
            var nonComponent = new object();
            var referenceService = new MockReferenceService();
            referenceService.AddReference("reference name", component1);
            referenceService.AddReference(string.Empty, component2);
            referenceService.AddReference("non component", nonComponent);
            var validReferenceServiceContext = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return referenceService;
                }
            };
            yield return ConvertTest.Valid("reference name", component1).WithContext(validReferenceServiceContext);
            yield return ConvertTest.Valid("  reference name  ", component1).WithContext(validReferenceServiceContext);
            yield return ConvertTest.Valid(string.Empty, component2).WithContext(validReferenceServiceContext);
            yield return ConvertTest.Valid("non component", nonComponent).WithContext(validReferenceServiceContext);
            yield return ConvertTest.Valid("no such reference", null).WithContext(validReferenceServiceContext);

            // IContainer.
            var containerComponent1 = new TestComponent();
            var containerComponent2 = new TestComponent();
            var container = new Container();
            container.Add(containerComponent1, "reference name");
            container.Add(containerComponent2, string.Empty);
            var contextWithContainer = new MockTypeDescriptorContext { Container = container };

            yield return ConvertTest.Valid("reference name", containerComponent1).WithContext(contextWithContainer);
            yield return ConvertTest.Valid(string.Empty, containerComponent2).WithContext(contextWithContainer);
            yield return ConvertTest.Valid("no such reference", null).WithContext(contextWithContainer);

            yield return ConvertTest.CantConvertFrom(1);
            yield return ConvertTest.CantConvertFrom(new object());
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            var component1 = new TestComponent();
            var component2 = new TestComponent();
            var component3 = new TestComponent();
            var nonComponent = new object();

            // No Context.
            yield return ConvertTest.Valid(null, "(none)").WithInvariantRemoteInvokeCulture();
            yield return ConvertTest.Valid(null, "(none)").WithContext(null).WithInvariantRemoteInvokeCulture();
            yield return ConvertTest.Valid(string.Empty, string.Empty).WithContext(null);

            // IReferenceCollection.
            var nullReferenceServiceContext = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return null;
                }
            };
            yield return ConvertTest.Valid(component1, string.Empty).WithContext(nullReferenceServiceContext);
            yield return ConvertTest.Valid(new Component(), string.Empty).WithContext(nullReferenceServiceContext);

            var invalidReferenceServiceContext = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return new object();
                }
            };
            yield return ConvertTest.Valid(component1, string.Empty).WithContext(invalidReferenceServiceContext);
            yield return ConvertTest.Valid(new Component(), string.Empty).WithContext(invalidReferenceServiceContext);

            var referenceService = new MockReferenceService();
            referenceService.AddReference("reference name", component1);
            referenceService.AddReference(string.Empty, component2);
            referenceService.AddReference(null, component3);
            referenceService.AddReference("non component", nonComponent);
            var validReferenceServiceContext = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return referenceService;
                }
            };
            yield return ConvertTest.Valid(component1, "reference name").WithContext(validReferenceServiceContext);
            yield return ConvertTest.Valid(component2, string.Empty).WithContext(validReferenceServiceContext);
            yield return ConvertTest.Valid(component3, string.Empty).WithContext(validReferenceServiceContext);
            yield return ConvertTest.Valid(nonComponent, "non component").WithContext(validReferenceServiceContext);
            yield return ConvertTest.Valid(new Component(), string.Empty).WithContext(validReferenceServiceContext);

            // IContainer.
            var containerComponent1 = new TestComponent();
            var containerComponent2 = new TestComponent();
            var container = new Container();
            container.Add(containerComponent1, "reference name");
            container.Add(containerComponent2, string.Empty);
            var contextWithContainer = new MockTypeDescriptorContext { Container = container };

            yield return ConvertTest.Valid(containerComponent1, "reference name").WithContext(contextWithContainer);
            yield return ConvertTest.Valid(containerComponent2, string.Empty).WithContext(contextWithContainer);
            yield return ConvertTest.Valid(new Component(), string.Empty).WithContext(contextWithContainer);

            yield return ConvertTest.CantConvertTo(1, typeof(int));
        }

        [Fact]
        public void GetStandardValues_IReferenceService_ReturnsExpected()
        {
            var converter = new ReferenceConverter(typeof(TestComponent));

            var component1 = new TestComponent();
            var component2 = new TestComponent();
            var component3 = new Component();

            var referenceService = new MockReferenceService();
            referenceService.AddReference("reference name 1", component1);
            referenceService.AddReference("reference name 2", component2);
            referenceService.AddReference("reference name 3", component3);
            referenceService.AddReference("reference name 4", component2);

            int callCount = 0;
            var context = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    callCount++;
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return referenceService;
                }
            };

            TypeConverter.StandardValuesCollection values1 = converter.GetStandardValues(context);
            Assert.Equal(1, callCount);
            Assert.Equal(new object[] { component1, component2, component2, null }, values1.Cast<object>());

            // Call again to test caching behavior.
            TypeConverter.StandardValuesCollection values2 = converter.GetStandardValues(context);
            Assert.Equal(2, callCount);
            Assert.NotSame(values1, values2);
        }

        [Fact]
        public void GetStandardValues_IReferenceServiceNullType_ReturnsExpected()
        {
            var converter = new ReferenceConverter(null);

            var component1 = new TestComponent();
            var component2 = new TestComponent();
            var component3 = new Component();

            var referenceService = new MockReferenceService();
            referenceService.AddReference("reference name 1", component1);
            referenceService.AddReference("reference name 2", component2);
            referenceService.AddReference("reference name 3", component3);

            int callCount = 0;
            var context = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    callCount++;
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return referenceService;
                }
            };

            TypeConverter.StandardValuesCollection values1 = converter.GetStandardValues(context);
            Assert.Equal(1, callCount);
            Assert.Equal(new object[] { null }, values1.Cast<object>());

            // Call again to test caching behavior.
            TypeConverter.StandardValuesCollection values2 = converter.GetStandardValues(context);
            Assert.Equal(2, callCount);
            Assert.NotSame(values1, values2);
        }

        public static IEnumerable<object[]> GetStandardValues_IReferenceServiceInvalid_TestData()
        {
            yield return new object[] { new MockReferenceService { References = null } };
            yield return new object[] { new object() };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(GetStandardValues_IReferenceServiceInvalid_TestData))]
        public void GetStandardValues_IReferenceServiceInvalid_ReturnsExpected(object referenceService)
        {
            var converter = new ReferenceConverter(typeof(TestComponent));
            int callCount = 0;
            var context = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    callCount++;
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return referenceService;
                }
            };

            TypeConverter.StandardValuesCollection values1 = converter.GetStandardValues(context);
            Assert.Equal(1, callCount);
            Assert.Equal(new object[] { null }, values1.Cast<object>());

            // Call again to test caching behavior.
            TypeConverter.StandardValuesCollection values2 = converter.GetStandardValues(context);
            Assert.Equal(2, callCount);
            Assert.NotSame(values1, values2);
        }

        [Fact]
        public void GetStandardValues_IReferenceServiceNoValuesAllowed_ReturnsEmpty()
        {
            var converter = new SubReferenceConverter(typeof(TestComponent));

            var component1 = new Component();
            var component2 = new TestComponent();
            var component3 = new TestComponent();

            var referenceService = new MockReferenceService();
            referenceService.AddReference("reference name 1", component1);
            referenceService.AddReference("reference name 2", component2);
            referenceService.AddReference("reference name 3", component3);

            int callCount = 0;
            var context = new MockTypeDescriptorContext
            {
                GetServiceAction = (serviceType) =>
                {
                    callCount++;
                    Assert.Equal(typeof(IReferenceService), serviceType);
                    return referenceService;
                }
            };

            TypeConverter.StandardValuesCollection values1 = converter.GetStandardValues(context);
            Assert.Equal(1, callCount);
            Assert.Equal(new object[] { null }, values1.Cast<object>());
            Assert.Equal(new object[] { component2, component3 }, converter.DisallowedValues);

            // Call again to test caching behavior.
            TypeConverter.StandardValuesCollection values2 = converter.GetStandardValues(context);
            Assert.Equal(2, callCount);
            Assert.NotSame(values1, values2);
        }

        [Fact]
        public void GetStandardValues_IContainer_ReturnsExpected()
        {
            var component1 = new TestComponent();
            var component2 = new TestComponent();
            var component3 = new Component();

            var container = new Container();
            container.Add(component1);
            container.Add(component2);
            container.Add(component3);

            var converter = new ReferenceConverter(typeof(TestComponent));
            var context = new MockTypeDescriptorContext { Container = container };

            TypeConverter.StandardValuesCollection values1 = converter.GetStandardValues(context);
            Assert.Equal(new object[] { component1, component2, null }, values1.Cast<object>());

            // Call again to test caching behavior.
            TypeConverter.StandardValuesCollection values2 = converter.GetStandardValues(context);
            Assert.NotSame(values1, values2);
        }

        [Fact]
        public void GetStandardValues_IContainerNullType_ReturnsExpected()
        {
            var component1 = new TestComponent();
            var component2 = new TestComponent();
            var component3 = new Component();

            var container = new Container();
            container.Add(component1);
            container.Add(component2);
            container.Add(component3);

            var converter = new ReferenceConverter(null);
            var context = new MockTypeDescriptorContext { Container = container };

            TypeConverter.StandardValuesCollection values1 = converter.GetStandardValues(context);
            Assert.Equal(new object[] { null }, values1.Cast<object>());

            // Call again to test caching behavior.
            TypeConverter.StandardValuesCollection values2 = converter.GetStandardValues(context);
            Assert.NotSame(values1, values2);
        }

        [Fact]
        public void GetStandardValues_IContainerNoValuesAllowed_ReturnsEmpty()
        {
            var component1 = new Component();
            var component2 = new TestComponent();
            var component3 = new TestComponent();

            var container = new Container();
            container.Add(component1);
            container.Add(component2);
            container.Add(component3);

            var converter = new SubReferenceConverter(typeof(TestComponent));
            var context = new MockTypeDescriptorContext { Container = container };

            TypeConverter.StandardValuesCollection values1 = converter.GetStandardValues(context);
            Assert.Equal(new object[] { null }, values1.Cast<object>());
            Assert.Equal(new object[] { component2, component3 }, converter.DisallowedValues);

            // Call again to test caching behavior.
            TypeConverter.StandardValuesCollection values2 = converter.GetStandardValues(context);
            Assert.NotSame(values1, values2);
        }

        [Fact]
        public void GetStandardValues_NullContext_ReturnsEmpty()
        {
            var converter = new ReferenceConverter(typeof(TestComponent));
            Assert.Empty(converter.GetStandardValues(null));
        }

        [Fact]
        public void GetStandardValues_NoReferenceCollectionOrIContainer_ReturnsEmpty()
        {
            var converter = new ReferenceConverter(typeof(TestComponent));
            var context = new MockTypeDescriptorContext();

            TypeConverter.StandardValuesCollection values1 = converter.GetStandardValues(context);
            Assert.Equal(new object[] { null }, values1.Cast<object>());

            // Call again to test caching behavior.
            TypeConverter.StandardValuesCollection values2 = converter.GetStandardValues(context);
            Assert.NotSame(values1, values2);
        }

        private class SubReferenceConverter : ReferenceConverter
        {
            public SubReferenceConverter(Type type) : base(type) { }

            public List<object> DisallowedValues { get; } = new List<object>();

            protected override bool IsValueAllowed(ITypeDescriptorContext context, object value)
            {
                DisallowedValues.Add(value);
                return false;
            }
        }

        public class MockTypeDescriptorContext : ITypeDescriptorContext
        {
            public Func<Type, object> GetServiceAction { get; set; }

            public IContainer Container { get; set; }
            public object Instance => null;
            public PropertyDescriptor PropertyDescriptor => null;

            public void OnComponentChanged()
            {
            }

            public bool OnComponentChanging() => true;

            public object GetService(Type serviceType) => GetServiceAction?.Invoke(serviceType);
        }

        public class MockReferenceService : IReferenceService
        {
            public Dictionary<string, object> References { get; set; } = new Dictionary<string, object>();

            public void AddReference(string name, object reference)
            {
                References[name ?? "(null)"] = reference;
            }

            public void ClearReferences() => References.Clear();

            public IComponent GetComponent(object reference) => null;

            public string GetName(object reference)
            {
                foreach (KeyValuePair<string, object> entry in References)
                {
                    if (entry.Value == reference)
                    {
                        return entry.Key == "(null)" ? null : entry.Key;
                    }
                }

                return null;
            }

            public object GetReference(string name)
            {
                if (!References.ContainsKey(name ?? "(null)"))
                {
                    return null;
                }

                return References[name];
            }

            public object[] GetReferences()
            {
                object[] array = new object[References.Values.Count];
                References.Values.CopyTo(array, 0);
                return array;
            }

            public object[] GetReferences(Type baseType)
            {
                if (References == null)
                {
                    return null;
                }

                object[] references = GetReferences();

                var filtered = new List<object>();
                foreach (object reference in references)
                {
                    if (baseType != null && baseType.IsInstanceOfType(reference))
                    {
                        filtered.Add(reference);
                    }
                }

                return filtered.ToArray();
            }
        }

        private interface ITestInterface
        {
        }

        private class TestComponent : Component
        {
        }
    }
}
