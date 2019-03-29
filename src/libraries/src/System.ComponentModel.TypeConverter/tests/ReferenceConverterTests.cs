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
using System.Diagnostics;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ReferenceConverterTest
    {

        class TestReferenceService : IReferenceService
        {
            private Dictionary<string, object> references;

            public TestReferenceService()
            {
                references = new Dictionary<string, object>();
            }

            public void AddReference(string name, object reference)
            {
                references[name] = reference;
            }

            public void ClearReferences()
            {
                references.Clear();
            }

            public IComponent GetComponent(object reference)
            {
                return null;
            }

            public string GetName(object reference)
            {
                foreach (KeyValuePair<string, object> entry in references)
                {
                    if (entry.Value == reference)
                        return entry.Key;
                }

                return null;
            }

            public object GetReference(string name)
            {
                if (!references.ContainsKey(name))
                    return null;
                return references[name];
            }

            public object[] GetReferences()
            {
                object[] array = new object[references.Values.Count];
                references.Values.CopyTo(array, 0);
                return array;
            }

            public object[] GetReferences(Type baseType)
            {
                object[] references = GetReferences();

                List<object> filtered = new List<object>();
                foreach (object reference in references)
                {
                    if (baseType.IsInstanceOfType(reference))
                        filtered.Add(reference);
                }

                return filtered.ToArray();
            }
        }

        private class TestTypeDescriptorContext : ITypeDescriptorContext
        {
            private IReferenceService reference_service = null;
            private IContainer container = null;

            public TestTypeDescriptorContext()
            {
            }

            public TestTypeDescriptorContext(IReferenceService referenceService)
            {
                reference_service = referenceService;
            }


            public IContainer Container
            {
                get { return container; }
                set { container = value; }
            }

            public object Instance
            {
                get { return null; }
            }

            public PropertyDescriptor PropertyDescriptor
            {
                get { return null; }
            }

            public void OnComponentChanged()
            {
            }

            public bool OnComponentChanging()
            {
                return true;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IReferenceService))
                    return reference_service;
                return null;
            }
        }

        private interface ITestInterface
        {
        }

        private class TestComponent : Component
        {
        }

        [Fact]
        public void CanConvertFrom()
        {
            ReferenceConverter converter = new ReferenceConverter(typeof(ITestInterface));
            // without context
            Assert.False(converter.CanConvertFrom(null, typeof(string)));
            // with context
            Assert.True(converter.CanConvertFrom(new TestTypeDescriptorContext(), typeof(string)));
        }

        [Fact]
        public void ConvertFrom()
        {
            ReferenceConverter converter = new ReferenceConverter(typeof(ITestInterface));
            string referenceName = "reference name";
            // no context
            Assert.Null(converter.ConvertFrom(null, null, referenceName));

            TestComponent component = new TestComponent();

            // context with IReferenceService
            TestReferenceService referenceService = new TestReferenceService();
            referenceService.AddReference(referenceName, component);
            TestTypeDescriptorContext context = new TestTypeDescriptorContext(referenceService);
            Assert.Same(component, converter.ConvertFrom(context, null, referenceName));

            // context with Component without IReferenceService
            Container container = new Container();
            container.Add(component, referenceName);
            context = new TestTypeDescriptorContext();
            context.Container = container;
            Assert.Same(component, converter.ConvertFrom(context, null, referenceName));
        }

        [Fact]
        public void ConvertTo()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

                ReferenceConverter remoteConverter = new ReferenceConverter(typeof(ITestInterface));
                Assert.Equal("(none)", (string)remoteConverter.ConvertTo(null, null, null, typeof(string)));
            }).Dispose();

            ReferenceConverter converter = new ReferenceConverter(typeof(ITestInterface));
            string referenceName = "reference name";
            TestComponent component = new TestComponent();

            // no context
            Assert.Equal(string.Empty, (string)converter.ConvertTo(null, null, component, typeof(string)));

            // context with IReferenceService
            TestReferenceService referenceService = new TestReferenceService();
            referenceService.AddReference(referenceName, component);
            TestTypeDescriptorContext context = new TestTypeDescriptorContext(referenceService);
            Assert.Equal(referenceName, (string)converter.ConvertTo(context, null, component, typeof(string)));

            // context with Component without IReferenceService
            Container container = new Container();
            container.Add(component, referenceName);
            context = new TestTypeDescriptorContext();
            context.Container = container;
            Assert.Equal(referenceName, (string)converter.ConvertTo(context, null, component, typeof(string)));
        }

        [Fact]
        public void CanConvertTo()
        {
            ReferenceConverter converter = new ReferenceConverter(typeof(ITestInterface));
            Assert.True(converter.CanConvertTo(new TestTypeDescriptorContext(), typeof(string)));
        }

        [Fact]
        public void GetStandardValues()
        {
            ReferenceConverter converter = new ReferenceConverter(typeof(TestComponent));

            TestComponent component1 = new TestComponent();
            TestComponent component2 = new TestComponent();
            TestReferenceService referenceService = new TestReferenceService();
            referenceService.AddReference("reference name 1", component1);
            referenceService.AddReference("reference name 2", component2);
            ITypeDescriptorContext context = new TestTypeDescriptorContext(referenceService);

            TypeConverter.StandardValuesCollection values = converter.GetStandardValues(context);
            Assert.NotNull(values);
            // 2 components + 1 null value
            Assert.Equal(3, values.Count);
        }
    }
}
