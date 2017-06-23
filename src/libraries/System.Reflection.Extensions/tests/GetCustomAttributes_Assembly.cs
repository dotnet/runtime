// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security;
using Xunit;
using Assembly = System.Reflection.Tests;

[assembly: Assembly.MyAttribute_Single("single"), Assembly.MyAttribute_AllowMultiple("multiple1"), Assembly.MyAttribute_AllowMultiple("multiple2")]

namespace System.Reflection.Tests
{
    public class GetCustomAttributes_Assembly
    {
        [Fact]
        public void GetCustomAttributes_thisAsm()
        {
            Assembly thisAsm = typeof(GetCustomAttributes_Assembly).GetTypeInfo().Assembly;
            IEnumerable<Attribute> attributes = CustomAttributeExtensions.GetCustomAttributes(thisAsm);
            //There are other attributes added as default:

            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_Single single", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple multiple1", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple multiple2", StringComparison.Ordinal)));
        }

        [Fact]
        public void CustomAttributes_Get()
        {
            Assembly thisAsm = typeof(GetCustomAttributes_Assembly).GetTypeInfo().Assembly;
            IEnumerable<CustomAttributeData> attributeData = thisAsm.CustomAttributes;
            IEnumerator<CustomAttributeData> customAttrs = attributeData.GetEnumerator();

            Assert.Equal(1, attributeData.Count(attr => attr.ToString().Contains("MyAttribute_Single")));
            Assert.Equal(2, attributeData.Count(attr => attr.ToString().Contains("MyAttribute_AllowMultiple")));
        }

        [Fact]
        public void GetCustomAttributeExtension()
        {
            Assembly thisAsm = typeof(GetCustomAttributes_Assembly).GetTypeInfo().Assembly;
            Attribute attribute = CustomAttributeExtensions.GetCustomAttribute(thisAsm, typeof(SecurityCriticalAttribute));
            Assert.Null(attribute);

            attribute = CustomAttributeExtensions.GetCustomAttribute(thisAsm, typeof(MyAttribute_Single));
            Assert.Equal("System.Reflection.Tests.MyAttribute_Single single", attribute.ToString());

            Assert.Throws<AmbiguousMatchException>(() =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute(thisAsm, typeof(MyAttribute_AllowMultiple));
            });
        }

        [Fact]
        public void GetCustomAttributesExtension()
        {
            Assembly thisAsm = typeof(GetCustomAttributes_Assembly).GetTypeInfo().Assembly;
            IEnumerable<Attribute> attributes;
            attributes = CustomAttributeExtensions.GetCustomAttributes(thisAsm, typeof(MyAttribute_AllowMultiple));
            Assert.Equal(2, attributes.Count());
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple multiple1", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple multiple1", StringComparison.Ordinal)));

            attributes = CustomAttributeExtensions.GetCustomAttributes(thisAsm, typeof(SecurityCriticalAttribute));
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void GetCustomAttributeOfT()
        {
            Assembly thisAsm = typeof(GetCustomAttributes_Assembly).GetTypeInfo().Assembly;
            Attribute attribute;

            attribute = CustomAttributeExtensions.GetCustomAttribute<SecurityCriticalAttribute>(thisAsm);
            Assert.Null(attribute);

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                CustomAttributeExtensions.GetCustomAttributes(thisAsm, typeof(String));
            });

            attribute = CustomAttributeExtensions.GetCustomAttribute<MyAttribute_Single>(thisAsm);
            Assert.Equal("System.Reflection.Tests.MyAttribute_Single single", attribute.ToString());

            Assert.Throws<AmbiguousMatchException>(() =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute<Attribute>(thisAsm);
            });
        }

        [Fact]
        public void GetCustomAttributesOfT()
        {
            Assembly thisAsm = typeof(GetCustomAttributes_Assembly).GetTypeInfo().Assembly;
            IEnumerable<Attribute> attributes;

            attributes = CustomAttributeExtensions.GetCustomAttributes<MyAttribute_AllowMultiple>(thisAsm);
            Assert.Equal(2, attributes.Count());
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple multiple1", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple multiple2", StringComparison.Ordinal)));

            attributes = CustomAttributeExtensions.GetCustomAttributes<SecurityCriticalAttribute>(thisAsm);
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void IsDefined()
        {
            Assembly thisAsm = typeof(GetCustomAttributes_Assembly).GetTypeInfo().Assembly;
            Assert.True(CustomAttributeExtensions.IsDefined(thisAsm, typeof(MyAttribute_AllowMultiple)));

            Assert.False(CustomAttributeExtensions.IsDefined(thisAsm, typeof(SecurityCriticalAttribute)));
        }
    }

    public class MyAttributeBase : Attribute
    {
        private String _name;
        public MyAttributeBase(String name)
        {
            _name = name;
        }
        public override String ToString() { return this.GetType().ToString() + " " + _name; }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class MyAttribute_Single : MyAttributeBase
    {
        public MyAttribute_Single(String name) : base(name) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class MyAttribute_AllowMultiple : MyAttributeBase
    {
        public MyAttribute_AllowMultiple(String name) : base(name) { }
    }
}
