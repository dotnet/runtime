// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    // Mutable static comparison in the implementation
    [Collection(nameof(DisableParallelization))]
    public class MemberDescriptorTests
    {
        [Theory]
        [InlineData("  ")]
        [InlineData("name")]
        public void Ctor_String(string name)
        {
            var descriptor = new SubMemberDescriptor(name);
            Assert.Empty(descriptor.Attributes);
            Assert.Same(descriptor.Attributes, descriptor.Attributes);
            Assert.Empty(descriptor.AttributeArray);
            Assert.Same(descriptor.AttributeArray, descriptor.AttributeArray);
            Assert.Equal("Misc", descriptor.Category);
            Assert.Empty(descriptor.Description);
            Assert.False(descriptor.DesignTimeOnly);
            Assert.Same(name, descriptor.DisplayName);
            Assert.True(descriptor.IsBrowsable);
            Assert.Equal(name, descriptor.Name);
            Assert.Equal(name.GetHashCode(), descriptor.NameHashCode);
        }

        public static IEnumerable<object[]> Ctor_String_Attributes_TestData()
        {
            yield return new object[] { "  ", null, new Attribute[0] };
            yield return new object[] { "  ", new Attribute[0], new Attribute[0] };

            Attribute[] attributes1 = new Attribute[] { new MockAttribute1(), new MockAttribute1() };
            yield return new object[] { "name", attributes1, new Attribute[] { attributes1[0] } };

            Attribute[] attributes2 = new Attribute[] { new MockAttribute1(), new MockAttribute2() };
            yield return new object[] { "name", attributes2, attributes2 };

            Attribute[] attributes3 = new Attribute[] { null, new MockAttribute1() };
            yield return new object[] { "name", attributes3, new Attribute[] { attributes3[1] } };

            Attribute[] attributes4 = new Attribute[] { new CustomTypeIdAttribute(1), new CustomTypeIdAttribute(1), new CustomTypeIdAttribute(2), new CustomTypeIdAttribute(null) };
            yield return new object[] { "name", attributes4, new Attribute[] { attributes4[0], attributes4[2] } };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_Attributes_TestData))]
        public void Ctor_String_Attributes(string name, Attribute[] attributes, Attribute[] expected)
        {
            var descriptor = new SubMemberDescriptor(name, attributes);
            Assert.Equal(expected, descriptor.Attributes.Cast<Attribute>());
            Assert.Same(descriptor.Attributes, descriptor.Attributes);
            Assert.Equal(expected, descriptor.AttributeArray);
            Assert.NotSame(attributes, descriptor.AttributeArray);
            Assert.Same(descriptor.AttributeArray, descriptor.AttributeArray);
            Assert.Equal("Misc", descriptor.Category);
            Assert.Empty(descriptor.Description);
            Assert.False(descriptor.DesignTimeOnly);
            Assert.Same(name, descriptor.DisplayName);
            Assert.True(descriptor.IsBrowsable);
            Assert.Same(name, descriptor.Name);
        }

        [Fact]
        public void Ctor_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("name", () => new SubMemberDescriptor((string)null));
            Assert.Throws<ArgumentNullException>("name", () => new SubMemberDescriptor((string)null, new Attribute[0]));
        }

        [Fact]
        public void Ctor_InvalidName_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("name", () => new SubMemberDescriptor(string.Empty));
            AssertExtensions.Throws<ArgumentException>("name", () => new SubMemberDescriptor(string.Empty, new Attribute[0]));
        }

        [Theory]
        [InlineData("  ")]
        [InlineData("name")]
        public void Ctor_MemberDescriptor(string name)
        {
            var attributes = new Attribute[]
            {
                new CategoryAttribute("Category"),
                new DescriptionAttribute("Description"),
                new DesignOnlyAttribute(true),
                new DisplayNameAttribute("DisplayName"),
                new BrowsableAttribute(false)
            };
            var oldMemberDescriptor = new SubMemberDescriptor(name, attributes);
            var descriptor = new SubMemberDescriptor(oldMemberDescriptor);
            Assert.Equal(attributes, descriptor.Attributes.Cast<Attribute>());
            Assert.Same(descriptor.Attributes, descriptor.Attributes);
            Assert.Equal(attributes, descriptor.AttributeArray);
            Assert.NotSame(attributes, descriptor.AttributeArray);
            Assert.Same(descriptor.AttributeArray, descriptor.AttributeArray);
            Assert.Equal("Category", descriptor.Category);
            Assert.Equal("Description", descriptor.Description);
            Assert.True(descriptor.DesignTimeOnly);
            Assert.Equal("DisplayName", descriptor.DisplayName);
            Assert.False(descriptor.IsBrowsable);
            Assert.Same(name, descriptor.Name);
            Assert.Equal(name.GetHashCode(), descriptor.NameHashCode);
        }

        [Fact]
        public void Ctor_MemberDescriptorWithNullName()
        {
            var attributes = new Attribute[]
            {
                new CategoryAttribute("Category"),
                new DescriptionAttribute("Description"),
                new DesignOnlyAttribute(true),
                new DisplayNameAttribute("DisplayName"),
                new BrowsableAttribute(false)
            };
            var oldMemberDescriptor = new CustomNameMemberDescriptor(null, attributes);
            Assert.Null(oldMemberDescriptor.Name);
            var descriptor = new SubMemberDescriptor(oldMemberDescriptor);
            Assert.Equal(attributes, descriptor.Attributes.Cast<Attribute>());
            Assert.Same(descriptor.Attributes, descriptor.Attributes);
            Assert.Equal(attributes, descriptor.AttributeArray);
            Assert.NotSame(attributes, descriptor.AttributeArray);
            Assert.Same(descriptor.AttributeArray, descriptor.AttributeArray);
            Assert.Equal("Category", descriptor.Category);
            Assert.Equal("Description", descriptor.Description);
            Assert.True(descriptor.DesignTimeOnly);
            Assert.Equal("DisplayName", descriptor.DisplayName);
            Assert.False(descriptor.IsBrowsable);
            Assert.Empty(descriptor.Name);
            Assert.Equal(0, descriptor.NameHashCode);
        }

        [Theory]
        [MemberData(nameof(Ctor_String_Attributes_TestData))]
        public void Ctor_MemberDescriptor_Attributes(string name, Attribute[] attributes, Attribute[] expectedAttributes)
        {
            var originalAttributes = new Attribute[]
            {
                new CategoryAttribute("Category"),
                new DescriptionAttribute("Description"),
                new DesignOnlyAttribute(true),
                new DisplayNameAttribute("DisplayName"),
                new BrowsableAttribute(false)
            };
            Attribute[] expectedNewAttributes = originalAttributes.Concat(expectedAttributes).ToArray();
            var oldMemberDescriptor = new SubMemberDescriptor(name, originalAttributes);
            var descriptor = new SubMemberDescriptor(oldMemberDescriptor, attributes);
            Assert.Equal(expectedNewAttributes, descriptor.Attributes.Cast<Attribute>());
            Assert.Same(descriptor.Attributes, descriptor.Attributes);
            Assert.Equal(expectedNewAttributes, descriptor.AttributeArray);
            Assert.NotSame(expectedNewAttributes, descriptor.AttributeArray);
            Assert.Same(descriptor.AttributeArray, descriptor.AttributeArray);
            Assert.Equal("Category", descriptor.Category);
            Assert.Equal("Description", descriptor.Description);
            Assert.True(descriptor.DesignTimeOnly);
            Assert.Equal("DisplayName", descriptor.DisplayName);
            Assert.False(descriptor.IsBrowsable);
            Assert.Same(name, descriptor.Name);
            Assert.Equal(name.GetHashCode(), descriptor.NameHashCode);
        }

        [Fact]
        public void Ctor_NullDescr_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("descr", () => new SubMemberDescriptor((MemberDescriptor)null));
            Assert.Throws<ArgumentNullException>("oldMemberDescriptor", () => new SubMemberDescriptor((MemberDescriptor)null, new Attribute[0]));
        }

        [Fact]
        public void Attributes_GetWithCustomFillAttributes_ReturnsExpected()
        {
            var descriptor = new CustomFillAttributesMemberDescriptor("Name");
            Assert.Equal(new Attribute[] { descriptor.Attribute }, descriptor.Attributes.Cast<Attribute>());
        }

        [Fact]
        public void Attributes_GetWithThrowingFillAttributes_ReturnsExpected()
        {
            var descriptor = new ThrowingFillAttributesMemberDescriptor("Name");
            Assert.Equal(new Attribute[] { descriptor.Attribute }, descriptor.Attributes.Cast<Attribute>());
        }

        [Fact]
        public void Attributes_GetAfterChangingMetadata_Recreates()
        {
            var descriptor = new SubMemberDescriptor("Name");
            AttributeCollection attributes = descriptor.Attributes;
            Assert.Same(attributes, descriptor.Attributes);

            TypeDescriptor.AddAttributes(typeof(int), new Attribute[0]);
            Assert.NotSame(attributes, descriptor.Attributes);
        }

        [Fact]
        public void AttributeArray_GetWithCustomFillAttributes_ReturnsExpected()
        {
            var descriptor = new CustomFillAttributesMemberDescriptor("Name");
            Assert.Equal(new Attribute[] { descriptor.Attribute }, descriptor.AttributeArray);
        }

        [Fact]
        public void AttributeArray_GetWithThrowingFillAttributes_ReturnsExpected()
        {
            var descriptor = new ThrowingFillAttributesMemberDescriptor("Name");
            Assert.Equal(new Attribute[] { descriptor.Attribute }, descriptor.AttributeArray);
        }

        [Fact]
        public void AttributeArray_GetAfterChangingMetadata_DoesNotRecreate()
        {
            var descriptor = new SubMemberDescriptor("Name");
            Attribute[] attributes = descriptor.AttributeArray;
            Assert.Same(attributes, descriptor.AttributeArray);

            TypeDescriptor.AddAttributes(typeof(int), new Attribute[0]);
            Assert.Same(attributes, descriptor.AttributeArray);
        }

        public static IEnumerable<object[]> AttributeArray_Set_TestData()
        {
            yield return new object[] { null, new Attribute[0] };
            yield return new object[] { new Attribute[0], new Attribute[0] };

            Attribute[] attributes1 = new Attribute[] { new MockAttribute1(), new MockAttribute1() };
            yield return new object[] { attributes1, new Attribute[] { attributes1[0] } };

            Attribute[] attributes2 = new Attribute[] { new MockAttribute1(), new MockAttribute2() };
            yield return new object[] { attributes2, attributes2 };

            Attribute[] attributes3 = new Attribute[] { null, new MockAttribute1() };
            yield return new object[] { attributes3, new Attribute[] { attributes3[1] } };

            Attribute[] attributes4 = new Attribute[] { new CustomTypeIdAttribute(1), new CustomTypeIdAttribute(1), new CustomTypeIdAttribute(2), new CustomTypeIdAttribute(null) };
            yield return new object[] { attributes4, new Attribute[] { attributes4[0], attributes4[2] } };
        }

        [Theory]
        [MemberData(nameof(AttributeArray_Set_TestData))]
        public void AttributeArray_Set_GetReturnsExpected(Attribute[] value, Attribute[] expected)
        {
            var descriptor = new SubMemberDescriptor("Name")
            {
                AttributeArray = value
            };
            Assert.Equal(expected, descriptor.AttributeArray);
            Assert.NotSame(value, descriptor.AttributeArray);
            Assert.Same(descriptor.AttributeArray, descriptor.AttributeArray);
            Assert.Equal(expected, descriptor.Attributes.Cast<Attribute>());
            Assert.Same(descriptor.Attributes, descriptor.Attributes);

            // Set same.
            Assert.Equal(expected, descriptor.AttributeArray);
            Assert.NotSame(value, descriptor.AttributeArray);
            Assert.Same(descriptor.AttributeArray, descriptor.AttributeArray);
            Assert.Equal(expected, descriptor.Attributes.Cast<Attribute>());
            Assert.Same(descriptor.Attributes, descriptor.Attributes);
        }

        [Fact]
        public void AttributeArray_SetDontGetAttributeArrayAndModify_DoesNotCopy()
        {
            var attribute1 = new MockAttribute1();
            var attribute2 = new MockAttribute2();
            var attributes = new Attribute[] { attribute1 };
            var descriptor = new SubMemberDescriptor("Name")
            {
                AttributeArray = attributes
            };
            attributes[0] = attribute2;
            Assert.Same(attribute2, descriptor.AttributeArray[0]);
        }

        [Fact]
        public void AttributeArray_SetGetAttributeArrayAndModify_DoesCopy()
        {
            var attribute1 = new MockAttribute1();
            var attribute2 = new MockAttribute2();
            var attributes = new Attribute[] { attribute1 };
            var descriptor = new SubMemberDescriptor("Name")
            {
                AttributeArray = attributes
            };
            Assert.Same(attribute1, descriptor.AttributeArray[0]);
            attributes[0] = attribute2;
            Assert.Same(attribute1, descriptor.AttributeArray[0]);
        }

        [Fact]
        public void AttributeArray_SetDontGetAttributesAndModify_DoesNotCopy()
        {
            var attribute1 = new MockAttribute1();
            var attribute2 = new MockAttribute2();
            var attributes = new Attribute[] { attribute1 };
            var descriptor = new SubMemberDescriptor("Name")
            {
                AttributeArray = attributes
            };
            attributes[0] = attribute2;
            Assert.Same(attribute2, descriptor.Attributes[0]);
        }

        [Fact]
        public void AttributeArray_SetGetAttributesAndModify_DoesCopy()
        {
            var attribute1 = new MockAttribute1();
            var attribute2 = new MockAttribute2();
            var attributes = new Attribute[] { attribute1 };
            var descriptor = new SubMemberDescriptor("Name")
            {
                AttributeArray = attributes
            };
            Assert.Same(attribute1, descriptor.Attributes[0]);
            attributes[0] = attribute2;
            Assert.Same(attribute1, descriptor.Attributes[0]);
        }

        public static IEnumerable<object[]> Category_Get_TestData()
        {
            yield return new object[] { new CategoryAttribute(), "Misc" };
            yield return new object[] { new CategoryAttribute(null), null };
            yield return new object[] { new CategoryAttribute(string.Empty), string.Empty };
            yield return new object[] { new CategoryAttribute("  "), "  " };
            yield return new object[] { new CategoryAttribute("Category"), "Category" };
        }

        [Theory]
        [MemberData(nameof(Category_Get_TestData))]
        public void Category_GetWithCategoryAttribute_ReturnsExpected(CategoryAttribute attribute, string expected)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            Assert.Equal(expected, descriptor.Category);
            Assert.Same(descriptor.Category, descriptor.Category);
        }

        [Theory]
        [InlineData(null, "Category2")]
        [InlineData("", "")]
        [InlineData("Category", "Category")]
        public void Category_GetModifyAttributesAndGet_CachesFirstResult(string originalCategory, string expected)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute(originalCategory) });
            Assert.Equal(originalCategory, descriptor.Category);

            descriptor.AttributeArray = new Attribute[] { new CategoryAttribute("Category2") };
            Assert.Equal(expected, descriptor.Category);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Category")]
        public void Category_DontGetModifyAttributesAndGet_DoesNotCacheFirstResult(string originalCategory)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute(originalCategory) });
            descriptor.AttributeArray = new Attribute[] { new CategoryAttribute("Category2") };
            Assert.Equal("Category2", descriptor.Category);
        }

        [Fact]
        public void Category_GetWithNullAttributes_ThrowsNullReferenceException()
        {
            var descriptor = new NullAttributesMemberDescriptor("Name");
            Assert.Throws<NullReferenceException>(() => descriptor.Category);
        }

        [Fact]
        public void Category_GetWithInvalidAttributes_ThrowsInvalidCastException()
        {
            var descriptor = new InvalidAttributesMemberDescriptor("Name");
            Assert.Throws<InvalidCastException>(() => descriptor.Category);
        }

        public static IEnumerable<object[]> Description_Get_TestData()
        {
            yield return new object[] { new DescriptionAttribute(), string.Empty };
            yield return new object[] { new DescriptionAttribute(null), null };
            yield return new object[] { new DescriptionAttribute(string.Empty), string.Empty };
            yield return new object[] { new DescriptionAttribute("DisplayName"), "DisplayName" };
        }

        [Theory]
        [MemberData(nameof(Description_Get_TestData))]
        public void Description_GetWithDescriptionAttribute_ReturnsExpected(DescriptionAttribute attribute, string expected)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            Assert.Equal(expected, descriptor.Description);
            Assert.Same(descriptor.Description, descriptor.Description);
        }

        [Theory]
        [InlineData(null, "Description2")]
        [InlineData("", "")]
        [InlineData("Description", "Description")]
        public void Description_GetModifyDescriptionAttribute_CachesFirstResult(string originalDescription, string expected)
        {
            var attribute = new ChangingDescriptionAttribute
            {
                DescriptionValue = originalDescription
            };
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            Assert.Same(originalDescription, descriptor.Description);

            attribute.DescriptionValue = "Description2";
            Assert.Equal(expected, descriptor.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Description")]
        public void Description_DontGetModifyDescriptionAttribute_DoesNotCacheFirstResult(string originalDescription)
        {
            var attribute = new ChangingDescriptionAttribute
            {
                DescriptionValue = originalDescription
            };
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            attribute.DescriptionValue = "Description2";
            Assert.Equal("Description2", descriptor.Description);
        }

        [Theory]
        [InlineData(null, "Description2")]
        [InlineData("", "")]
        [InlineData("Description", "Description")]
        public void Description_GetModifyAttributesAndGet_CachesFirstResult(string originalDescription, string expected)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute(originalDescription) });
            Assert.Same(originalDescription, descriptor.Description);

            descriptor.AttributeArray = new Attribute[] { new DescriptionAttribute("Description2") };
            Assert.Equal(expected, descriptor.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Description")]
        public void Description_DontGetModifyAttributesAndGet_DoesNotCacheFirstResult(string originalDescription)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute(originalDescription) });
            descriptor.AttributeArray = new Attribute[] { new DescriptionAttribute("Description2") };
            Assert.Equal("Description2", descriptor.Description);
        }

        [Fact]
        public void Description_GetWithNullAttributes_ThrowsNullReferenceException()
        {
            var descriptor = new NullAttributesMemberDescriptor("Name");
            Assert.Throws<NullReferenceException>(() => descriptor.Description);
        }

        [Fact]
        public void Description_GetWithInvalidAttributes_ThrowsInvalidCastException()
        {
            var descriptor = new InvalidAttributesMemberDescriptor("Name");
            Assert.Throws<InvalidCastException>(() => descriptor.Description);
        }

        public static IEnumerable<object[]> DesignTimeOnly_Get_TestData()
        {
            yield return new object[] { new DesignOnlyAttribute(true), true };
            yield return new object[] { new DesignOnlyAttribute(false), false };
        }

        [Theory]
        [MemberData(nameof(DesignTimeOnly_Get_TestData))]
        public void DesignTimeOnly_GetWithDesignOnlyAttribute_ReturnsExpected(DesignOnlyAttribute attribute, bool expected)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            Assert.Equal(expected, descriptor.DesignTimeOnly);
        }

        [Fact]
        public void DesignTimeOnly_GetModifyAttributesAndGet_DoesNotCacheFirstResult()
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new DesignOnlyAttribute(true) });
            Assert.True(descriptor.DesignTimeOnly);

            descriptor.AttributeArray = new Attribute[] { new DesignOnlyAttribute(false) };
            Assert.False(descriptor.DesignTimeOnly);
        }

        [Fact]
        public void DesignTimeOnly_DontGetModifyAttributesAndGet_DoesNotCacheFirstResult()
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new DesignOnlyAttribute(true) });
            descriptor.AttributeArray = new Attribute[] { new DesignOnlyAttribute(false) };
            Assert.False(descriptor.DesignTimeOnly);
        }

        [Fact]
        public void DesignTimeOnly_GetWithNullAttributes_ThrowsNullReferenceException()
        {
            var descriptor = new NullAttributesMemberDescriptor("Name");
            Assert.Throws<NullReferenceException>(() => descriptor.DesignTimeOnly);
        }

        [Fact]
        public void DesignTimeOnly_GetWithInvalidAttributes_ReturnsExpected()
        {
            var descriptor = new InvalidAttributesMemberDescriptor("Name");
            Assert.False(descriptor.DesignTimeOnly);
        }

        public static IEnumerable<object[]> DisplayName_Get_TestData()
        {
            yield return new object[] { new DisplayNameAttribute(), "Name" };
            yield return new object[] { new DisplayNameAttribute(null), null };
            yield return new object[] { new DisplayNameAttribute(string.Empty), "Name" };
            yield return new object[] { new DisplayNameAttribute("  "), "  " };
            yield return new object[] { new DisplayNameAttribute("DisplayName"), "DisplayName" };
        }

        [Theory]
        [MemberData(nameof(DisplayName_Get_TestData))]
        public void DisplayName_GetWithDisplayNameAttribute_ReturnsExpected(DisplayNameAttribute attribute, string expected)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            Assert.Equal("Name", descriptor.Name);
            Assert.Equal(expected, descriptor.DisplayName);
            Assert.Same(descriptor.DisplayName, descriptor.DisplayName);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "Name")]
        [InlineData("DisplayName", "DisplayName")]
        public void DisplayName_GetModifyDisplayNameAttribute_CachesFirstResult(string originalDisplayName, string expected)
        {
            var attribute = new ChangingDisplayNameAttribute
            {
                DisplayNameValue = originalDisplayName
            };
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            Assert.Equal(expected, descriptor.DisplayName);

            attribute.DisplayNameValue = "DisplayName2";
            Assert.Equal("DisplayName2", descriptor.DisplayName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("DisplayName")]
        public void DisplayName_DontGetModifyDisplayNameAttribute_DoesNotCacheFirstResult(string originalDisplayName)
        {
            var attribute = new ChangingDisplayNameAttribute
            {
                DisplayNameValue = originalDisplayName
            };
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            attribute.DisplayNameValue = "DisplayName2";
            Assert.Equal("DisplayName2", descriptor.DisplayName);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "Name")]
        [InlineData("DisplayName", "DisplayName")]
        public void DisplayName_GetModifyAttributesAndGet_DoesNotCacheFirstResult(string originalDisplayName, string expected)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new DisplayNameAttribute(originalDisplayName) });
            Assert.Equal(expected, descriptor.DisplayName);

            descriptor.AttributeArray = new Attribute[] { new DisplayNameAttribute("DisplayName2") };
            Assert.Equal("DisplayName2", descriptor.DisplayName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("DisplayName")]
        public void DisplayName_DontGetModifyAttributesAndGet_DoesNotCacheFirstResult(string originalDisplayName)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new DisplayNameAttribute(originalDisplayName) });
            descriptor.AttributeArray = new Attribute[] { new DisplayNameAttribute("DisplayName2") };
            Assert.Equal("DisplayName2", descriptor.DisplayName);
        }

        [Fact]
        public void DisplayName_GetWithNullAttributes_ThrowsNullReferenceException()
        {
            var descriptor = new NullAttributesMemberDescriptor("Name");
            Assert.Throws<NullReferenceException>(() => descriptor.DisplayName);
        }

        [Fact]
        public void DisplayName_GetWithInvalidAttributes_ReturnsExpected()
        {
            var descriptor = new InvalidAttributesMemberDescriptor("Name");
            Assert.Equal("Name", descriptor.DisplayName);
        }

        public static IEnumerable<object[]> IsBrowsable_Get_TestData()
        {
            yield return new object[] { new BrowsableAttribute(true), true };
            yield return new object[] { new BrowsableAttribute(false), false };
        }

        [Theory]
        [MemberData(nameof(IsBrowsable_Get_TestData))]
        public void IsBrowsable_GetWithBrowsableAttribute_ReturnsExpected(BrowsableAttribute attribute, bool expected)
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { attribute });
            Assert.Equal(expected, descriptor.IsBrowsable);
        }

        [Fact]
        public void IsBrowsable_GetModifyAttributesAndGet_DoesNotCacheFirstResult()
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new BrowsableAttribute(true) });
            Assert.True(descriptor.IsBrowsable);

            descriptor.AttributeArray = new Attribute[] { new BrowsableAttribute(false) };
            Assert.False(descriptor.IsBrowsable);
        }

        [Fact]
        public void IsBrowsable_DontGetModifyAttributesAndGet_DoesNotCacheFirstResult()
        {
            var descriptor = new SubMemberDescriptor("Name", new Attribute[] { new BrowsableAttribute(true) });
            descriptor.AttributeArray = new Attribute[] { new BrowsableAttribute(false) };
            Assert.False(descriptor.IsBrowsable);
        }

        [Fact]
        public void IsBrowsable_GetWithNullAttributes_ThrowsNullReferenceException()
        {
            var descriptor = new NullAttributesMemberDescriptor("Name");
            Assert.Throws<NullReferenceException>(() => descriptor.IsBrowsable);
        }

        [Fact]
        public void IsBrowsable_GetWithInvalidAttributes_ThrowsInvalidCastException()
        {
            var descriptor = new InvalidAttributesMemberDescriptor("Name");
            Assert.Throws<InvalidCastException>(() => descriptor.IsBrowsable);
        }

        [Fact]
        public void CreateAttributeCollection_Invoke_ReturnsExpected()
        {
            var attributes = new Attribute[] { new MockAttribute1() };
            var descriptor = new SubMemberDescriptor("Name", attributes);
            AttributeCollection result = descriptor.CreateAttributeCollection();
            Assert.Equal(attributes, result.Cast<Attribute>());
            Assert.NotSame(result, descriptor.Attributes);
            Assert.NotSame(result, descriptor.CreateAttributeCollection());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var attribute = new SubMemberDescriptor("Name");
            yield return new object[] { attribute, attribute, true };
            yield return new object[] { attribute, new SubMemberDescriptor("Name"), true };
            yield return new object[] { new CustomNameMemberDescriptor(null), new CustomNameMemberDescriptor(null), true };
            yield return new object[] { new CustomNameMemberDescriptor(null), new CustomNameMemberDescriptor("Name"), true };
            yield return new object[] { new CustomNameMemberDescriptor("Name"), new CustomNameMemberDescriptor(null), true };
            yield return new object[] { new SubMemberDescriptor(new CustomNameMemberDescriptor(null)), new SubMemberDescriptor(new CustomNameMemberDescriptor(null)), true };
            yield return new object[] { new SubMemberDescriptor(new CustomNameMemberDescriptor(null)), new SubMemberDescriptor(new CustomNameMemberDescriptor("Name")), false };
            yield return new object[] { new SubMemberDescriptor(new CustomNameMemberDescriptor("Name")), new SubMemberDescriptor(new CustomNameMemberDescriptor(null)), false };

            var fullDescriptor = new SubMemberDescriptor("Name", new Attribute[]
            {
                new CategoryAttribute("Category"),
                new DescriptionAttribute("Description"),
                new DesignOnlyAttribute(true),
                new DisplayNameAttribute("DisplayName"),
                new BrowsableAttribute(false)
            });
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute("Category"),
                    new DescriptionAttribute("Description"),
                    new DesignOnlyAttribute(true),
                    new DisplayNameAttribute("DisplayName"),
                    new BrowsableAttribute(false)
                }),
                true
            };
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute(null),
                    new DescriptionAttribute("Description"),
                    new DesignOnlyAttribute(true),
                    new DisplayNameAttribute("DisplayName"),
                    new BrowsableAttribute(false)
                }),
                false
            };
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute("Category2"),
                    new DescriptionAttribute("Description"),
                    new DesignOnlyAttribute(true),
                    new DisplayNameAttribute("DisplayName"),
                    new BrowsableAttribute(false)
                }),
                false
            };
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute("Category"),
                    new DescriptionAttribute(null),
                    new DesignOnlyAttribute(true),
                    new DisplayNameAttribute("DisplayName"),
                    new BrowsableAttribute(false)
                }),
                false
            };
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute("Category"),
                    new DescriptionAttribute("Description2"),
                    new DesignOnlyAttribute(true),
                    new DisplayNameAttribute("DisplayName"),
                    new BrowsableAttribute(false)
                }),
                false
            };
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute("Category"),
                    new DescriptionAttribute("Description"),
                    new DesignOnlyAttribute(false),
                    new DisplayNameAttribute("DisplayName"),
                    new BrowsableAttribute(false)
                }),
                false
            };
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute("Category"),
                    new DescriptionAttribute("Description"),
                    new DesignOnlyAttribute(true),
                    new DisplayNameAttribute(null),
                    new BrowsableAttribute(false)
                }),
                false
            };
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute("Category"),
                    new DescriptionAttribute("Description"),
                    new DesignOnlyAttribute(true),
                    new DisplayNameAttribute("DisplayName2"),
                    new BrowsableAttribute(false)
                }),
                false
            };
            yield return new object[]
            {
                fullDescriptor,
                new SubMemberDescriptor("Name", new Attribute[]
                {
                    new CategoryAttribute("Category"),
                    new DescriptionAttribute("Description"),
                    new DesignOnlyAttribute(true),
                    new DisplayNameAttribute("DisplayName"),
                    new BrowsableAttribute(true)
                }),
                false
            };

            var attribute1 = new MockAttribute1();
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                true
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                new SubMemberDescriptor("Name", new Attribute[] { new MockAttribute2() }),
                false
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                new SubMemberDescriptor("Name", new Attribute[] { attribute1, new MockAttribute2() }),
                false
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                new SubMemberDescriptor("Name", new Attribute[] { attribute1, attribute1 }),
                true
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                new SubMemberDescriptor("Name", new Attribute[0]),
                false
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { null }),
                new SubMemberDescriptor("Name", new Attribute[] { null }),
                true
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { null }),
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                false
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                new SubMemberDescriptor("Name", new Attribute[] { null }),
                false
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", null),
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                false
            };
            yield return new object[]
            {
                new SubMemberDescriptor("Name", new Attribute[] { attribute1 }),
                new SubMemberDescriptor("Name", null),
                false
            };

            var attributeWithCategory1 = new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute("Category") });
            Assert.Equal("Category", attributeWithCategory1.Category);

            var attributeWithCategory2 = new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute("Category") });
            Assert.Equal("Category", attributeWithCategory2.Category);

            var attributeWithCategory3 = new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute("Category2") });
            Assert.Equal("Category2", attributeWithCategory3.Category);

            var attributeWithCategory4 = new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute(null) });
            Assert.Null(attributeWithCategory4.Category);

            var attributeWithCategory5 = new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute(null) });
            Assert.Null(attributeWithCategory5.Category);

            yield return new object[] { attributeWithCategory1, attributeWithCategory2, true };
            yield return new object[] { attributeWithCategory1, attributeWithCategory3, false };
            yield return new object[] { attributeWithCategory1, attributeWithCategory4, false };
            yield return new object[] { attributeWithCategory1, new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute("Category") }), true };
            yield return new object[] { attributeWithCategory1, new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute("Category2") }), false };
            yield return new object[] { attributeWithCategory1, new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute(null) }), false };

            yield return new object[] { attributeWithCategory4, attributeWithCategory5, true };
            yield return new object[] { attributeWithCategory4, attributeWithCategory1, false };
            yield return new object[] { attributeWithCategory4, new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute(null) }), true };
            yield return new object[] { attributeWithCategory4, new SubMemberDescriptor("Name", new Attribute[] { new CategoryAttribute("Category") }), false };

            var attributeWithDescription1 = new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute("Description") });
            Assert.Equal("Description", attributeWithDescription1.Description);

            var attributeWithDescription2 = new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute("Description") });
            Assert.Equal("Description", attributeWithDescription2.Description);

            var attributeWithDescription3 = new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute("Description2") });
            Assert.Equal("Description2", attributeWithDescription3.Description);

            var attributeWithDescription4 = new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute(null) });
            Assert.Null(attributeWithDescription4.Description);

            var attributeWithDescription5 = new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute(null) });
            Assert.Null(attributeWithDescription5.Description);

            yield return new object[] { attributeWithDescription1, attributeWithDescription2, true };
            yield return new object[] { attributeWithDescription1, attributeWithDescription3, false };
            yield return new object[] { attributeWithDescription1, attributeWithDescription4, false };
            yield return new object[] { attributeWithDescription1, new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute("Description") }), true };
            yield return new object[] { attributeWithDescription1, new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute("Description2") }), false };
            yield return new object[] { attributeWithDescription1, new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute(null) }), false };

            yield return new object[] { attributeWithDescription4, attributeWithDescription5, true };
            yield return new object[] { attributeWithDescription4, attributeWithDescription1, false };
            yield return new object[] { attributeWithDescription4, new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute(null) }), true };
            yield return new object[] { attributeWithDescription4, new SubMemberDescriptor("Name", new Attribute[] { new DescriptionAttribute("Description") }), false };

            yield return new object[] { new SubMemberDescriptor("Name"), new InvalidAttributesMemberDescriptor("Name"), false };
            yield return new object[] { new SubMemberDescriptor("Name"), new object(), false };
            yield return new object[] { new SubMemberDescriptor("Name"), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(MemberDescriptor descriptor, object other, bool expected)
        {
            Assert.Equal(expected, descriptor.Equals(other));
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var descriptor = new SubMemberDescriptor("Name");
            Assert.Equal("Name".GetHashCode(), descriptor.GetHashCode());
        }

        public static IEnumerable<object[]> FillAttributes_TestData()
        {
            yield return new object[] { new Attribute[0] };
            yield return new object[] { new Attribute[] { null, new MockAttribute1() } };
        }

        [Theory]
        [MemberData(nameof(FillAttributes_TestData))]
        public void FillAttributes_InvokeWithoutAttributes_Success(Attribute[] attributes)
        {
            var descriptor = new SubMemberDescriptor("Name", attributes);
            var attributeList = new List<Attribute>();
            descriptor.FillAttributes(attributeList);
            Assert.Equal(attributes, attributeList);
        }

        [Fact]
        public void FillAttributes_NullOriginalList_Nop()
        {
            var descriptor = new SubMemberDescriptor("Name", null);
            var attributeList = new List<Attribute>();
            descriptor.FillAttributes(attributeList);
            Assert.Empty(attributeList);
        }

        [Fact]
        public void FillAttributes_NullAttributeList_ThrowsArgumentNullException()
        {
            var descriptor = new SubMemberDescriptor("Name");
            Assert.Throws<ArgumentNullException>("attributeList", () => descriptor.FillAttributes(null));
        }

        public static IEnumerable<object[]> GetInvocationTarget_TestData()
        {
            var o = new object();
            yield return new object[] { typeof(object), o, o };

            string association = "abc";
            TypeDescriptor.CreateAssociation(o, association);
            yield return new object[] { typeof(string), o, association };
        }

        [Theory]
        [MemberData(nameof(GetInvocationTarget_TestData))]
        public void GetInvocationTarget_Invoke_ReturnsExpected(Type type, object instance, object expected)
        {
            var descriptor = new SubMemberDescriptor("Name");
            Assert.Same(expected, descriptor.GetInvocationTarget(type, instance));
        }

        [Fact]
        public void GetInvocationTarget_NullType_ThrowsArgumentNullException()
        {
            var descriptor = new SubMemberDescriptor("Name");
            Assert.Throws<ArgumentNullException>("type", () => descriptor.GetInvocationTarget(null, new object()));
        }

        [Fact]
        public void GetInvocationTarget_NullInstance_ThrowsArgumentNullException()
        {
            var descriptor = new SubMemberDescriptor("Name");
            Assert.Throws<ArgumentNullException>("instance", () => descriptor.GetInvocationTarget(typeof(object), null));
        }

#pragma warning disable 0618
        [Theory]
        [MemberData(nameof(GetInvocationTarget_TestData))]
        public void GetInvokee_Invoke_ReturnsExpected(Type componentClass, object component, object expected)
        {
            Assert.Same(expected, SubMemberDescriptor.GetInvokee(componentClass, component));
        }

        [Fact]
        public void GetInvokee_NullComponentClass_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("componentClass", () => SubMemberDescriptor.GetInvokee(null, new object()));
        }

        [Fact]
        public void GetInvokee_NullComponent_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("component", () => SubMemberDescriptor.GetInvokee(typeof(object), null));
        }
#pragma warning restore 0618

        public static IEnumerable<object[]> GetSite_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new object(), null };
            yield return new object[] { new Component(), null };

            var component = new Component();
            new Container().Add(component);
            yield return new object[] { component, component.Site };
        }

        [Theory]
        [MemberData(nameof(GetSite_TestData))]
        public void GetSite_Invoke_ReturnsExpected(object component, object expected)
        {
            Assert.Same(expected, SubMemberDescriptor.GetSite(component));
        }

        private class SubMemberDescriptor : MemberDescriptor
        {
            public SubMemberDescriptor(string name) : base(name)
            {
            }

            public SubMemberDescriptor(string name, Attribute[] attributes) : base(name, attributes)
            {
            }

            public SubMemberDescriptor(MemberDescriptor other) : base(other)
            {
            }

            public SubMemberDescriptor(MemberDescriptor other, Attribute[] attributes) : base(other, attributes)
            {
            }

            public new Attribute[] AttributeArray
            {
                get => base.AttributeArray;
                set => base.AttributeArray = value;
            }

            public new int NameHashCode => base.NameHashCode;

            public new AttributeCollection CreateAttributeCollection() => base.CreateAttributeCollection();

            public new void FillAttributes(IList attributeList) => base.FillAttributes(attributeList);

            public new object GetInvocationTarget(Type type, object instance) => base.GetInvocationTarget(type, instance);

#pragma warning disable 0618
            public static new object GetInvokee(Type componentClass, object component) => MemberDescriptor.GetInvokee(componentClass, component);
#pragma warning restore 0618

            public static new object GetSite(object component) => MemberDescriptor.GetSite(component);

            public static new MethodInfo FindMethod(Type componentClass, string name, Type[] args, Type returnType)
            {
                return MemberDescriptor.FindMethod(componentClass, name, args, returnType);
            }

            public static new MethodInfo FindMethod(Type componentClass, string name, Type[] args, Type returnType, bool publicOnly)
            {
                return MemberDescriptor.FindMethod(componentClass, name, args, returnType, publicOnly);
            }
        }

        private class CustomFillAttributesMemberDescriptor : MemberDescriptor
        {
            public CustomFillAttributesMemberDescriptor(string name) : base(name)
            {
            }

            public Attribute Attribute { get; } = new MockAttribute1();

            public new Attribute[] AttributeArray
            {
                get => base.AttributeArray;
                set => base.AttributeArray = value;
            }

            protected override void FillAttributes(IList attributeList)
            {
                attributeList.Add(Attribute);
            }
        }

        private class ThrowingFillAttributesMemberDescriptor : MemberDescriptor
        {
            public ThrowingFillAttributesMemberDescriptor(string name) : base(name)
            {
            }

            public Attribute Attribute { get; } = new MockAttribute1();

            public new Attribute[] AttributeArray
            {
                get => base.AttributeArray;
                set => base.AttributeArray = value;
            }

            protected override void FillAttributes(IList attributeList)
            {
                attributeList.Add(Attribute);
                throw new NotImplementedException();
            }
        }

        private class NullAttributesMemberDescriptor : MemberDescriptor
        {
            public NullAttributesMemberDescriptor(string name) : base(name)
            {
            }

            public override AttributeCollection Attributes => null;
        }

        private class InvalidAttributesMemberDescriptor : MemberDescriptor
        {
            public InvalidAttributesMemberDescriptor(string name) : base(name)
            {
            }

            public override AttributeCollection Attributes => new InvalidAttributeCollection();
        }

        private class InvalidAttributeCollection : AttributeCollection
        {
            public override Attribute this[Type attributeType] => new MockAttribute1();
        }

        private class CustomNameMemberDescriptor : MemberDescriptor
        {
            private string _name;

            public CustomNameMemberDescriptor(string name) : base("Name")
            {
                _name = name;
            }

            public CustomNameMemberDescriptor(string name, Attribute[] attributes) : base("Name", attributes)
            {
                _name = name;
            }

            public override string Name => _name;
        }

        private sealed class MockAttribute1 : Attribute
        {
        }

        private sealed class MockAttribute2 : Attribute
        {
        }

        private class CustomTypeIdAttribute : Attribute
        {
            private object _typeId;

            public CustomTypeIdAttribute(object typeId)
            {
                _typeId = typeId;
            }

            public override object TypeId => _typeId;
        }

        private class ChangingDescriptionAttribute : DescriptionAttribute
        {
            public new string DescriptionValue
            {
                get => base.DescriptionValue;
                set => base.DescriptionValue = value;
            }
        }

        private class ChangingDisplayNameAttribute : DisplayNameAttribute
        {
            public new string DisplayNameValue
            {
                get => base.DisplayNameValue;
                set => base.DisplayNameValue = value;
            }
        }

        public static IEnumerable<object[]> FindMethod_Type_String_TypeArray_Type_Bool_TestData()
        {
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(void), publicOnly, nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid) };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(int), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[] { typeof(int) }, typeof(void), publicOnly, null };

                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(void), publicOnly, nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid) };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int) }, null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(int), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(object) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int), typeof(object) }, typeof(void), publicOnly, null };

                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[0], typeof(int), publicOnly, nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt) };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[0], null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[0], typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[] { typeof(int) }, typeof(void), publicOnly, null };

                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(int), publicOnly, nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt) };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int) }, null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(object) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int), typeof(object) }, typeof(void), publicOnly, null };

                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[0], typeof(void), publicOnly, nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid) };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[0], null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[0], typeof(int), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[0], typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[] { typeof(int) }, typeof(void), publicOnly, null };

                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(void), publicOnly, nameof(ClassWithMethods.PublicMethodParametersReturnVoid) };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int) }, null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(int), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(object) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int), typeof(object) }, typeof(void), publicOnly, null };

                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[0], typeof(int), publicOnly, nameof(ClassWithMethods.PublicMethodParameterlessReturnInt) };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[0], null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[0], typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[] { typeof(int) }, typeof(void), publicOnly, null };

                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(int), publicOnly, nameof(ClassWithMethods.PublicMethodParametersReturnInt) };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int) }, null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(object) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int), typeof(object) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[0], typeof(void), false, "PrivateStaticMethodParameterlessReturnVoid" };
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[0], null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[0], typeof(int), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[0], typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[] { typeof(int) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(void), false, "PrivateStaticMethodParametersReturnVoid" };
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int) }, null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(int), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(object) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int), typeof(object) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[0], typeof(int), false, "PrivateStaticMethodParameterlessReturnInt" };
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[0], null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[0], typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[] { typeof(int) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(int), false, "PrivateStaticMethodParametersReturnInt" };
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int) }, null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(object) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int), typeof(object) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[0], typeof(void), false, "PrivateMethodParameterlessReturnVoid" };
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[0], null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[0], typeof(int), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[0], typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[] { typeof(int) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(void), false, "PrivateMethodParametersReturnVoid" };
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int) }, null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(int), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(object) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int), typeof(object) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[0], typeof(int), false, "PrivateMethodParameterlessReturnInt" };
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[0], null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[0], typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[] { typeof(int) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(int), false, "PrivateMethodParametersReturnInt" };
            foreach (bool publicOnly in new bool[] { true, false })
            {
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int) }, null, publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(object), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[0], typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(object) }, typeof(void), publicOnly, null };
                yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int), typeof(object) }, typeof(void), publicOnly, null };
            }

            yield return new object[] { typeof(ClassWithMethods), string.Empty, new Type[0], typeof(void), false, null };
            yield return new object[] { typeof(ClassWithMethods), "NoSuchMethod", new Type[0], typeof(void), false, null };
        }

        [Theory]
        [MemberData(nameof(FindMethod_Type_String_TypeArray_Type_Bool_TestData))]
        public void FindMethod_InvokeTypeStringTypeArrayTypeBool_ReturnsExpected(Type componentClass, string name, Type[] args, Type returnType, bool publicOnly, string expectedName)
        {
            MethodInfo result = SubMemberDescriptor.FindMethod(componentClass, name, args, returnType, publicOnly);
            if (expectedName == null)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.Equal(expectedName, result.Name);
            }
        }

        public static IEnumerable<object[]> FindMethod_Type_String_TypeArray_Type_TestData()
        {
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(void), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid) };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], null, null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[] { typeof(int) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(void), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid) };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int) }, null, null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(object) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnVoid), new Type[] { typeof(int), typeof(object) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[0], typeof(int), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt) };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[0], null, null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[0], typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnInt), new Type[] { typeof(int) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(int), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt) };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int) }, null, null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(object) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParametersReturnInt), new Type[] { typeof(int), typeof(object) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[0], typeof(void), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid) };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[0], null, null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[0], typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[0], typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnVoid), new Type[] { typeof(int) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(void), nameof(ClassWithMethods.PublicMethodParametersReturnVoid) };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int) }, null, null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int) }, typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(object) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnVoid), new Type[] { typeof(int), typeof(object) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[0], typeof(int), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt) };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[0], null, null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[0], typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParameterlessReturnInt), new Type[] { typeof(int) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(int), nameof(ClassWithMethods.PublicMethodParametersReturnInt) };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int) }, null, null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int) }, typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(object) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), nameof(ClassWithMethods.PublicMethodParametersReturnInt), new Type[] { typeof(int), typeof(object) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[0], null, null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[0], typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[0], typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnVoid", new Type[] { typeof(int) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int) }, null, null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(object) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnVoid", new Type[] { typeof(int), typeof(object) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[0], typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[0], null, null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[0], typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParameterlessReturnInt", new Type[] { typeof(int) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int) }, null, null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(object) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateStaticMethodParametersReturnInt", new Type[] { typeof(int), typeof(object) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[0], null, null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[0], typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[0], typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnVoid", new Type[] { typeof(int) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int) }, null, null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int) }, typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(object) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnVoid", new Type[] { typeof(int), typeof(object) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[0], typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[0], null, null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[0], typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParameterlessReturnInt", new Type[] { typeof(int) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(int), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int) }, null, null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int) }, typeof(object), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(object) }, typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "PrivateMethodParametersReturnInt", new Type[] { typeof(int), typeof(object) }, typeof(void), null };

            yield return new object[] { typeof(ClassWithMethods), string.Empty, new Type[0], typeof(void), null };
            yield return new object[] { typeof(ClassWithMethods), "NoSuchMethod", new Type[0], typeof(void), null };
        }

        [Theory]
        [MemberData(nameof(FindMethod_Type_String_TypeArray_Type_TestData))]
        public void FindMethod_InvokeTypeStringTypeArrayType_ReturnsExpected(Type componentClass, string name, Type[] args, Type returnType, string expectedName)
        {
            MethodInfo result = SubMemberDescriptor.FindMethod(componentClass, name, args, returnType);
            if (expectedName == null)
            {
                Assert.Null(result);
            }
            else
            {
                Assert.Equal(expectedName, result.Name);
            }
        }

        [Fact]
        public void FindMethod_NullComponentClass_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("componentClass", () => SubMemberDescriptor.FindMethod(null, nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(void)));
            Assert.Throws<ArgumentNullException>("componentClass", () => SubMemberDescriptor.FindMethod(null, nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(void), true));
            Assert.Throws<ArgumentNullException>("componentClass", () => SubMemberDescriptor.FindMethod(null, nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), new Type[0], typeof(void), false));
        }

        [Fact]
        public void FindMethod_NullTypes_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("types", () => SubMemberDescriptor.FindMethod(typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), null, typeof(void)));
            Assert.Throws<ArgumentNullException>("types", () => SubMemberDescriptor.FindMethod(typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), null, typeof(void), true));
            Assert.Throws<ArgumentNullException>("types", () => SubMemberDescriptor.FindMethod(typeof(ClassWithMethods), nameof(ClassWithMethods.PublicStaticMethodParameterlessReturnVoid), null, typeof(void), false));
        }

        [Fact]
        public void FindMethod_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("name", () => SubMemberDescriptor.FindMethod(typeof(ClassWithMethods), null, new Type[0], typeof(void)));
            Assert.Throws<ArgumentNullException>("name", () => SubMemberDescriptor.FindMethod(typeof(ClassWithMethods), null, new Type[0], typeof(void), true));
            Assert.Throws<ArgumentNullException>("name", () => SubMemberDescriptor.FindMethod(typeof(ClassWithMethods), null, new Type[0], typeof(void), false));
        }

        private class ClassWithMethods
        {
            public static void PublicStaticMethodParameterlessReturnVoid() => throw new NotImplementedException();
            public static int PublicStaticMethodParameterlessReturnInt() => throw new NotImplementedException();

            public static void PublicStaticMethodParametersReturnVoid(int value) => throw new NotImplementedException();
            public static int PublicStaticMethodParametersReturnInt(int value) => throw new NotImplementedException();

            private static void PrivateStaticMethodParameterlessReturnVoid() => throw new NotImplementedException();
            private static int PrivateStaticMethodParameterlessReturnInt() => throw new NotImplementedException();

            private static void PrivateStaticMethodParametersReturnVoid(int value) => throw new NotImplementedException();
            private static int PrivateStaticMethodParametersReturnInt(int value) => throw new NotImplementedException();

            public void PublicMethodParameterlessReturnVoid() => throw new NotImplementedException();
            public int PublicMethodParameterlessReturnInt() => throw new NotImplementedException();

            public void PublicMethodParametersReturnVoid(int value) => throw new NotImplementedException();
            public int PublicMethodParametersReturnInt(int value) => throw new NotImplementedException();

            private void PrivateMethodParameterlessReturnVoid() => throw new NotImplementedException();
            private int PrivateMethodParameterlessReturnInt() => throw new NotImplementedException();

            private void PrivateMethodParametersReturnVoid(int value) => throw new NotImplementedException();
            private int PrivateMethodParametersReturnInt(int value) => throw new NotImplementedException();
        }
    }
}
