// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
        public class TestReferenceService : IReferenceService
        {
            private Dictionary<string, object> _references = new Dictionary<string, object>();

            public void AddReference(string name, object reference)
            {
                _references[name ?? "(null)"] = reference;
            }

            public void ClearReferences() => _references.Clear();

            public IComponent GetComponent(object reference) => null;

            public string GetName(object reference)
            {
                foreach (KeyValuePair<string, object> entry in _references)
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
                if (!_references.ContainsKey(name ?? "(null)"))
                {
                    return null;
                }

                return _references[name];
            }

            public object[] GetReferences()
            {
                object[] array = new object[_references.Values.Count];
                _references.Values.CopyTo(array, 0);
                return array;
            }

            public object[] GetReferences(Type baseType)
            {
                object[] references = GetReferences();

                var filtered = new List<object>();
                foreach (object reference in references)
                {
                    if (baseType.IsInstanceOfType(reference))
                    {
                        filtered.Add(reference);
                    }
                }

                return filtered.ToArray();
            }
        }

        public class TestTypeDescriptorContext : ITypeDescriptorContext
        {
            private IReferenceService _referenceService = null;

            public TestTypeDescriptorContext() { }

            public TestTypeDescriptorContext(IReferenceService referenceService)
            {
                _referenceService = referenceService;
            }

            public IContainer Container { get; set; }

            public object Instance => null;
            public PropertyDescriptor PropertyDescriptor => null;

            public void OnComponentChanged() { }
            public bool OnComponentChanging() => true;

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IReferenceService))
                {
                    return _referenceService;
                }

                return null;
            }
        }

        private interface ITestInterface { }

        private class TestComponent : Component { }

        public override TypeConverter Converter => new ReferenceConverter(typeof(ITestInterface));

        public override bool StandardValuesExclusive => true;
        public override bool StandardValuesSupported => true;

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            // This does actually succeed despite CanConvertFrom returning false.
            ConvertTest noContext = ConvertTest.Valid("reference name", null).WithContext(null);
            noContext.CanConvert = false;
            yield return noContext;

            // No IReferenceService or IContainer.
            yield return ConvertTest.Valid("", null);
            yield return ConvertTest.Valid("   ", null);
            yield return ConvertTest.Valid("(none)", null).WithInvariantRemoteInvokeCulture();
            yield return ConvertTest.Valid("nothing", null);

            // IReferenceService.
            var component = new TestComponent();
            var referenceService = new TestReferenceService();
            referenceService.AddReference("reference name", component);
            var contextWithReferenceCollection = new TestTypeDescriptorContext(referenceService);

            yield return ConvertTest.Valid("reference name", component).WithContext(contextWithReferenceCollection);
            yield return ConvertTest.Valid("no such reference", null).WithContext(contextWithReferenceCollection);

            // IContainer.
            var container = new Container();
            container.Add(component, "reference name");
            var contextWithContainer = new TestTypeDescriptorContext { Container = container };

            yield return ConvertTest.Valid("reference name", component).WithContext(contextWithContainer);
            yield return ConvertTest.Valid("no such reference", null).WithContext(contextWithContainer);

            yield return ConvertTest.CantConvertFrom(1);
            yield return ConvertTest.CantConvertFrom(new object());
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            var component = new TestComponent();

            // No Context.
            yield return ConvertTest.Valid(null, "(none)").WithInvariantRemoteInvokeCulture();;
            yield return ConvertTest.Valid(null, "(none)").WithContext(null).WithInvariantRemoteInvokeCulture();;
            yield return ConvertTest.Valid(string.Empty, string.Empty).WithContext(null);

            // IReferenceCollection.
            var referenceService = new TestReferenceService();
            referenceService.AddReference("reference name", component);
            var contextWithReferenceService = new TestTypeDescriptorContext(referenceService);

            yield return ConvertTest.Valid(component, "reference name").WithContext(contextWithReferenceService);
            yield return ConvertTest.Valid(new Component(), string.Empty).WithContext(contextWithReferenceService);

            // IContainer.
            var container = new Container();
            container.Add(component, "reference name");
            var contextWithContainer = new TestTypeDescriptorContext { Container = container };

            yield return ConvertTest.Valid(component, "reference name").WithContext(contextWithContainer);
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

            var referenceService = new TestReferenceService();
            referenceService.AddReference("reference name 1", component1);
            referenceService.AddReference("reference name 2", component2);
            referenceService.AddReference("reference name 3", component3);

            var context = new TestTypeDescriptorContext(referenceService);

            TypeConverter.StandardValuesCollection values = converter.GetStandardValues(context);
            Assert.Equal(new object[] { component1, component2, null }, values.Cast<object>());
        }

        [Fact]
        public void GetStandardValues_IReferenceServiceNoValuesAllowed_ReturnsEmpty()
        {
            var converter = new SubReferenceConverter(typeof(TestComponent));

            var component1 = new Component();
            var component2 = new TestComponent();
            var component3 = new TestComponent();

            var referenceService = new TestReferenceService();
            referenceService.AddReference("reference name 1", component1);
            referenceService.AddReference("reference name 2", component2);
            referenceService.AddReference("reference name 3", component3);

            var context = new TestTypeDescriptorContext(referenceService);

            TypeConverter.StandardValuesCollection values = converter.GetStandardValues(context);
            Assert.Equal(new object[] { null }, values.Cast<object>());
            Assert.Equal(new object[] { component2, component3 }, converter.Values);
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
            var context = new TestTypeDescriptorContext { Container = container };

            TypeConverter.StandardValuesCollection values = converter.GetStandardValues(context);
            Assert.Equal(new object[] { component1, component2, null }, values.Cast<object>());
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
            var context = new TestTypeDescriptorContext { Container = container };

            TypeConverter.StandardValuesCollection values = converter.GetStandardValues(context);
            Assert.Equal(new object[] { null }, values.Cast<object>());
            Assert.Equal(new object[] { component2, component3 }, converter.Values);
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
            var context = new TestTypeDescriptorContext();

            TypeConverter.StandardValuesCollection values = converter.GetStandardValues(context);
            Assert.Equal(new object[] { null }, values.Cast<object>());
        }

        private class SubReferenceConverter : ReferenceConverter
        {
            public SubReferenceConverter(Type type) : base(type) { }

            public List<object> Values { get; } = new List<object>();

            protected override bool IsValueAllowed(ITypeDescriptorContext context, object value)
            {
                Values.Add(value);
                return false;
            }
        }
    }
}
