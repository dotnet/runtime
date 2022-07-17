// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesignerOptionServiceTests
    {
        [Fact]
        public void CreateOptionCollection_CreateMultipleTimes_ReturnsExpected()
        {
            var service = new TestDesignerOptionService();

            DesignerOptionService.DesignerOptionCollection options1 = service.DoCreateOptionCollection(service.Options, "name", "value");
            Assert.Equal(0, options1.Count);
            Assert.NotEmpty(options1.Properties);
            Assert.Same(options1.Properties, options1.Properties);
            Assert.Same(service.Options, options1.Parent);

            DesignerOptionService.DesignerOptionCollection options2 = service.DoCreateOptionCollection(service.Options, "name", "value");
            Assert.Equal(2, service.Options.Count);
            Assert.Equal(0, options2.Count);
            Assert.NotEmpty(options2.Properties);
            Assert.Same(options2.Properties, options2.Properties);
            Assert.Same(service.Options, options2.Parent);

            Assert.Equal(new DesignerOptionService.DesignerOptionCollection[] { options1, options2 }, service.Options.Cast<object>());
        }

        [Fact]
        public void CreateOptionCollection_NullParent_ThrowsArgumentNullException()
        {
            var service = new TestDesignerOptionService();
            AssertExtensions.Throws<ArgumentNullException>("parent", () => service.DoCreateOptionCollection(null, "name", "value"));
        }

        [Fact]
        public void CreateOptionCollection_NullName_ThrowsArgumentNullException()
        {
            var service = new TestDesignerOptionService();
            AssertExtensions.Throws<ArgumentNullException>("name", () => service.DoCreateOptionCollection(service.Options, null, "value"));
        }

        [Fact]
        public void CreateOptionCollection_EmptyName_ThrowsArgumentException()
        {
            var service = new TestDesignerOptionService();
            AssertExtensions.Throws<ArgumentException>("name", () => service.DoCreateOptionCollection(service.Options, string.Empty, "value"));
        }

        [Fact]
        public void Options_Get_ReturnsExpected()
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.Options;
            Assert.Same(options, service.Options);

            Assert.Equal(0, options.Count);
            Assert.Empty(options.Properties);
            Assert.Same(options.Properties, options.Properties);
            Assert.Null(options.Parent);
        }

        [Fact]
        public void Options_IListProperties_ReturnsExpected()
        {
            var service = new TestDesignerOptionService();
            IList options = service.Options;
            Assert.Same(options, service.Options);

            Assert.Equal(0, options.Count);
            Assert.True(options.IsFixedSize);
            Assert.True(options.IsReadOnly);
            Assert.False(options.IsSynchronized);
            Assert.Same(options, options.SyncRoot);
        }

        [Fact]
        public void IList_Modification_ThrowsNotSupportedException()
        {
            var service = new TestDesignerOptionService();
            IList options = service.Options;

            Assert.Throws<NotSupportedException>(() => options[0] = null);
            Assert.Throws<NotSupportedException>(() => options.Add(null));
            Assert.Throws<NotSupportedException>(() => options.Insert(0, null));
            Assert.Throws<NotSupportedException>(() => options.Remove(null));
            Assert.Throws<NotSupportedException>(() => options.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => options.Clear());
        }

        [Fact]
        public void IndexOf_ValueExists_ReturnsExpected()
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.Equal(0, service.Options.IndexOf(options));
            Assert.Equal(0, ((IList)service.Options).IndexOf(options));
        }

        [Fact]
        public void IndexOf_NoSuchOptions_ReturnsNegativeOne()
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.Equal(-1, service.Options.IndexOf(service.Options));
            Assert.Equal(-1, service.Options.IndexOf(null));
            Assert.Equal(-1, ((IList)service.Options).IndexOf(service.Options));
        }

        [Fact]
        public void Contains_ValueExists_ReturnsExpected()
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.Equal(0, service.Options.IndexOf(options));
            Assert.Equal(0, ((IList)service.Options).IndexOf(options));
        }

        [Fact]
        public void Contains_NoSuchOptions_ReturnsFalse()
        {
            var service = new TestDesignerOptionService();
            IList options = service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.False(options.Contains(service.Options));
            Assert.False(options.Contains(null));
        }

        [Theory]
        [InlineData("Name")]
        [InlineData("name")]
        public void Indexer_ValidName_ReturnsExpected(string name)
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.Same(options, service.Options[name]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("NoSuchName")]
        public void Indexer_InvalidName_ReturnsNull(string name)
        {
            var service = new TestDesignerOptionService();
            service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.Null(service.Options[name]);
        }

        [Fact]
        public void Indexer_ValidIndex_ReturnsExpected()
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.Same(options, service.Options[0]);
            Assert.Same(options, ((IList)service.Options)[0]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void Indexer_InvalidIndex_ThrowsIndexOutOfRangeException(int index)
        {
            var service = new TestDesignerOptionService();
            service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.Throws<IndexOutOfRangeException>(() => service.Options[index]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList)service.Options)[index]);
        }

        [Fact]
        public void Properties_GetBeforeAddingChild_ReturnsNonEmpty()
        {
            var service = new TestDesignerOptionService();
            Assert.Empty(service.Options.Properties);

            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "name", "value");
            Assert.NotEmpty(service.Options.Properties);
        }

        [Fact]
        public void Properties_GetAfterAddingChild_ReturnsNonEmpty()
        {
            var service = new TestDesignerOptionService();

            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "name", "value");
            Assert.NotEmpty(service.Options.Properties);
        }

        [Fact]
        public void Properties_PropertyDescriptorAttributes_Success()
        {
            var value = new TestClass { Value = "Value" };

            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", value);
            PropertyDescriptor propertyDescriptor = Assert.IsAssignableFrom<PropertyDescriptor>(Assert.Single(options.Properties));
            PropertyDescriptor actualProperty = Assert.IsAssignableFrom<PropertyDescriptor>(Assert.Single(TypeDescriptor.GetProperties(value)));

            Assert.Equal(actualProperty.Attributes, propertyDescriptor.Attributes);
            Assert.Equal(actualProperty.ComponentType, propertyDescriptor.ComponentType);
            Assert.Equal(actualProperty.IsReadOnly, propertyDescriptor.IsReadOnly);
            Assert.Equal(actualProperty.PropertyType, propertyDescriptor.PropertyType);
            Assert.Equal(actualProperty.CanResetValue(value), propertyDescriptor.CanResetValue("InvalidComponent"));
            Assert.Equal(actualProperty.ShouldSerializeValue(value), propertyDescriptor.ShouldSerializeValue("InvalidComponent"));

            propertyDescriptor.ResetValue(null);
            Assert.Equal("Value", value.Value);
        }

        [Fact]
        public void DesignerOptionConverterGetPropertiesSupported_Invoke_ReturnsTrue()
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(DesignerOptionService.DesignerOptionCollection));
            Assert.True(converter.GetPropertiesSupported());
        }

        [Fact]
        public void DesignerOptionConverterGetProperties_ValidValue_ReturnsExpected()
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(DesignerOptionService.DesignerOptionCollection));
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", new TestClass());
            service.DoCreateOptionCollection(options, "Name", null);

            PropertyDescriptorCollection properties = converter.GetProperties(options);
            Assert.Equal(2, properties.Count);

            PropertyDescriptor optionsDescriptor = properties[0];
            Assert.Equal(typeof(DesignerOptionService.DesignerOptionCollection), optionsDescriptor.ComponentType);
            Assert.Equal(typeof(DesignerOptionService.DesignerOptionCollection), optionsDescriptor.PropertyType);
            Assert.Same(options[0], optionsDescriptor.GetValue(null));

            Assert.True(optionsDescriptor.IsReadOnly);
            Assert.False(optionsDescriptor.CanResetValue(null));
            Assert.False(optionsDescriptor.ShouldSerializeValue(null));

            optionsDescriptor.ResetValue(null);
            optionsDescriptor.SetValue(null, null);

            PropertyDescriptor propertyDescriptor = properties[1];
            Assert.Same(options.Properties[0], propertyDescriptor);
        }

        [Theory]
        [InlineData("Value")]
        [InlineData(null)]
        public void DesignerOptionConverterGetProperties_InvalidValue_ReturnsEmpty(object value)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(DesignerOptionService.DesignerOptionCollection));
            Assert.Empty(converter.GetProperties(value));
        }

        [Fact]
        public void DesignerOptionConverter_ConvertToString_ReturnsExpected()
        {
            using (new ThreadCultureChange(null, CultureInfo.InvariantCulture))
            {
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(DesignerOptionService.DesignerOptionCollection));
                Assert.Equal("(Collection)", converter.ConvertToString(null));
            }
        }

        [Fact]
        public void DesignerOptionConverter_ConvertToNonString_ThrowsNotSupportedException()
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(DesignerOptionService.DesignerOptionCollection));
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, typeof(object)));
        }

        [Fact]
        public void CopyTo_ValidRange_Success()
        {
            var service = new TestDesignerOptionService();

            DesignerOptionService.DesignerOptionCollection options1 = service.DoCreateOptionCollection(service.Options, "name", "value");
            DesignerOptionService.DesignerOptionCollection options2 = service.DoCreateOptionCollection(service.Options, "name", "value");

            var destination = new object[3];
            service.Options.CopyTo(destination, 1);

            Assert.Equal(new object[] { null, options1, options2 }, destination);
        }

        [Fact]
        public void Properties_GetWhenPopulateOptionCollectionOverridden_ReturnsExpected()
        {
            var service = new PopulatingDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.Options;
            Assert.Single(options);
            Assert.Equal("Name", options[0].Name);
        }

        private class PopulatingDesignerOptionService : DesignerOptionService
        {
            protected override void PopulateOptionCollection(DesignerOptionCollection options)
            {
                CreateOptionCollection(options, "Name", "Value");
            }
        }

        [Fact]
        public void ShowDialog_Invoke_ReturnsFalseByDefault()
        {
            var service = new TestDesignerOptionService();
            Assert.False(service.DoShowDialog(null, null));
        }

        [Fact]
        public void GetOptionValue_NotNested_ReturnsExpected()
        {
            var value = new TestClass { Value = "StringValue" };

            var service = new TestDesignerOptionService();
            IDesignerOptionService iService = service;
            service.DoCreateOptionCollection(service.Options, "Name", value);

            Assert.Equal("StringValue", iService.GetOptionValue("Name", "Value"));
        }

        [Fact]
        public void GetOptionValue_Nested_ReturnsExpected()
        {
            var value = new TestClass { Value = "StringValue" };

            var service = new TestDesignerOptionService();
            IDesignerOptionService iService = service;
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", null);
            service.DoCreateOptionCollection(options, "SubName", value);

            Assert.Equal("StringValue", iService.GetOptionValue("Name\\SubName", "Value"));
        }

        [Theory]
        [InlineData("NoSuchParentName\\SubName", "Value")]
        [InlineData("Name\\NoSuchSubName", "Value")]
        [InlineData("Name\\SubName", "NoSuchValue")]
        public void GetOptionValue_NoSuchValue_ReturnsNull(string pageName, string valueName)
        {
            var value = new TestClass { Value = "StringValue" };

            var service = new TestDesignerOptionService();
            IDesignerOptionService iService = service;
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", null);
            service.DoCreateOptionCollection(options, "SubName", value);

            Assert.Null(iService.GetOptionValue(pageName, valueName));
        }

        [Fact]
        public void GetOptionValue_NullPageName_ThrowsArgumentNullException()
        {
            IDesignerOptionService service = new TestDesignerOptionService();
            AssertExtensions.Throws<ArgumentNullException>("pageName", () => service.GetOptionValue(null, "ValueName"));
        }

        [Fact]
        public void GetOptionValue_NullValueName_ThrowsArgumentNullException()
        {
            IDesignerOptionService service = new TestDesignerOptionService();
            AssertExtensions.Throws<ArgumentNullException>("valueName", () => service.GetOptionValue("PageName", null));
        }

        [Fact]
        public void SetOptionValue_NotNested_ReturnsExpected()
        {
            var value = new TestClass { Value = "StringValue" };

            var service = new TestDesignerOptionService();
            IDesignerOptionService iService = service;
            service.DoCreateOptionCollection(service.Options, "Name", value);

            iService.SetOptionValue("Name", "Value", "abc");
            Assert.Equal("abc", value.Value);
        }

        [Fact]
        public void SetOptionValue_Nested_ReturnsExpected()
        {
            var value = new TestClass { Value = "StringValue" };

            var service = new TestDesignerOptionService();
            IDesignerOptionService iService = service;
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", null);
            service.DoCreateOptionCollection(options, "SubName", value);

            iService.SetOptionValue("Name\\SubName", "Value", "abc");
            Assert.Equal("abc", value.Value);
        }

        [Theory]
        [InlineData("NoSuchParentName\\SubName", "Value")]
        [InlineData("Name\\NoSuchSubName", "Value")]
        [InlineData("Name\\SubName", "NoSuchValue")]
        public void SetOptionValue_NoSuchValue_Nop(string pageName, string valueName)
        {
            var value = new TestClass { Value = "StringValue" };

            var service = new TestDesignerOptionService();
            IDesignerOptionService iService = service;
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", null);
            service.DoCreateOptionCollection(options, "SubName", value);

            iService.SetOptionValue(pageName, valueName, "value");
        }

        [Fact]
        public void SetOptionValue_NullPageName_ThrowsArgumentNullException()
        {
            IDesignerOptionService service = new TestDesignerOptionService();
            AssertExtensions.Throws<ArgumentNullException>("pageName", () => service.SetOptionValue(null, "ValueName", "value"));
        }

        [Fact]
        public void SetOptionValue_NullValueName_ThrowsArgumentNullException()
        {
            IDesignerOptionService service = new TestDesignerOptionService();
            AssertExtensions.Throws<ArgumentNullException>("valueName", () => service.SetOptionValue("PageName", null, "value"));
        }

        [Fact]
        public void ShowDialog_NonNestedValue_Success()
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", "Value");

            Assert.True(options.ShowDialog());
            Assert.Equal("Value", service.ShowDialogValue);
        }

        [Fact]
        public void ShowDialog_NestedValue_Success()
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", null);
            service.DoCreateOptionCollection(options, "Name", "Value");

            Assert.True(options.ShowDialog());
            Assert.Equal("Value", service.ShowDialogValue);
        }

        [Fact]
        public void ShowDialog_NullValue_Success()
        {
            var service = new TestDesignerOptionService();
            DesignerOptionService.DesignerOptionCollection options = service.DoCreateOptionCollection(service.Options, "Name", null);
            service.DoCreateOptionCollection(options, "Name", null);

            Assert.False(options.ShowDialog());
            Assert.Equal("Default", service.ShowDialogValue);
        }

        private class TestDesignerOptionService : DesignerOptionService
        {
            public object ShowDialogValue { get; set; } = "Default";

            public DesignerOptionCollection DoCreateOptionCollection(DesignerOptionCollection parent, string name, object value)
            {
                return CreateOptionCollection(parent, name, value);
            }

            protected override bool ShowDialog(DesignerOptionCollection options, object optionObject)
            {
                ShowDialogValue = optionObject;
                return true;
            }

            public bool DoShowDialog(DesignerOptionCollection options, object optionObject)
            {
                return base.ShowDialog(options, optionObject);
            }
        }

        private class TestClass
        {
            public string Value { get; set; }
        }
    }
}
