// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security;
using Xunit;

namespace System.Reflection.Tests
{
    public class GetCustomAttributes_MemberInfo
    {
        private static readonly Type s_typeWithoutAttr = typeof(TestClassWithoutAttribute);
        private static readonly Type s_typeBaseClass = typeof(TestBaseClass);
        private static readonly Type s_typeTestClass = typeof(TestClass);

        [Fact]
        public void IsDefined_Inherit()
        {
            Assert.False(CustomAttributeExtensions.IsDefined(s_typeTestClass.GetTypeInfo(), typeof(MyAttribute_Single_Inherited), false));
        }

        [Fact]
        public void IsDefined()
        {
            Assert.True(CustomAttributeExtensions.IsDefined(s_typeTestClass.GetTypeInfo(), typeof(MyAttribute_Single_M)));

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                CustomAttributeExtensions.IsDefined(s_typeTestClass.GetTypeInfo(), typeof(String));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeExtensions.IsDefined(s_typeTestClass.GetTypeInfo(), null);
            });
        }

        [Fact]
        public void GetCustomAttributeOfT_Single_NotInherited()
        {
            Attribute attribute = CustomAttributeExtensions.GetCustomAttribute<MyAttribute_Single_Inherited>(s_typeTestClass.GetTypeInfo(), false);
            Assert.Null(attribute);
        }

        [Fact]
        public void GetCustomAttributeOfT_Single()
        {
            Attribute attribute = CustomAttributeExtensions.GetCustomAttribute<MyAttribute_Single_Inherited>(s_typeTestClass.GetTypeInfo());
            Assert.NotNull(attribute);

            Assert.Throws<AmbiguousMatchException>(() =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute<MyAttribute_AllowMultiple_Inherited>(s_typeTestClass.GetTypeInfo());
            });
        }

        [Fact]
        public void GetCustomAttributeT_Multiple_NoInheirit()
        {
            var attributes = CustomAttributeExtensions.GetCustomAttributes<MyAttribute_AllowMultiple_Inherited>(s_typeTestClass.GetTypeInfo(), false);
            Assert.Equal(1, attributes.Count());
        }

        [Fact]
        public void GetCustomAttributeOfT_Multiple()
        {
            IEnumerable<Attribute> attributes = CustomAttributeExtensions.GetCustomAttributes<MyAttribute_Single_Inherited>(s_typeTestClass.GetTypeInfo());
            Assert.Equal(1, attributes.Count());

            attributes = CustomAttributeExtensions.GetCustomAttributes<SecurityCriticalAttribute>(s_typeTestClass.GetTypeInfo());
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void GetCustomAttribute_Multiple_NotInherited()
        {
            var attribute = CustomAttributeExtensions.GetCustomAttribute(s_typeTestClass.GetTypeInfo(), typeof(MyAttribute_AllowMultiple_Inherited), false);
            Assert.NotNull(attribute);

            var attributes = CustomAttributeExtensions.GetCustomAttributes(s_typeTestClass.GetTypeInfo(), typeof(MyAttribute_AllowMultiple_Inherited), false);
            Assert.Equal(1, attributes.Count());
        }

        [Fact]
        public void GetCustomAttributeSingle()
        {
            Attribute attribute = CustomAttributeExtensions.GetCustomAttribute(s_typeTestClass.GetTypeInfo(), typeof(MyAttribute_Single_M));
            Assert.NotNull(attribute);

            Assert.Throws<AmbiguousMatchException>(() =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute(s_typeTestClass.GetTypeInfo(), typeof(MyAttribute_AllowMultiple_Inherited));
            });

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute(s_typeTestClass.GetTypeInfo(), typeof(String));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute(s_typeTestClass.GetTypeInfo(), null);
            });
        }

        [Fact]
        public void GetCustomAttribute4()
        {
            IEnumerable<Attribute> attributes = CustomAttributeExtensions.GetCustomAttributes(s_typeTestClass.GetTypeInfo(), typeof(MyAttribute_AllowMultiple_Inherited));
            Assert.Equal(2, attributes.Count());

            attributes = CustomAttributeExtensions.GetCustomAttributes(s_typeTestClass.GetTypeInfo(), typeof(SecurityCriticalAttribute));
            Assert.Equal(0, attributes.Count());

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                attributes = CustomAttributeExtensions.GetCustomAttributes(s_typeTestClass.GetTypeInfo(), typeof(String));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                attributes = CustomAttributeExtensions.GetCustomAttributes(s_typeTestClass.GetTypeInfo(), null);
            });
        }

        [Fact]
        public void GetCustomAttribute5()
        {
            IEnumerable<Attribute> attributes = CustomAttributeExtensions.GetCustomAttributes(s_typeTestClass.GetTypeInfo(), false);
            Assert.Equal(4, attributes.Count());
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_M single", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_M multiple1", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_M multiple2", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_Inherited multiple", StringComparison.Ordinal)));
        }

        [Fact]
        public void GetCustomAttributeAll()
        {
            IEnumerable<Attribute> attributes = CustomAttributeExtensions.GetCustomAttributes(s_typeWithoutAttr.GetTypeInfo());
            Assert.Equal(0, attributes.Count());

            attributes = CustomAttributeExtensions.GetCustomAttributes(s_typeTestClass.GetTypeInfo(), true);

            IEnumerator<Attribute> customAttrs = attributes.GetEnumerator();

            Assert.Equal(6, attributes.Count());
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_M single", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_M multiple1", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_M multiple2", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_Inherited singleBase", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_Inherited multiple", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_Inherited multipleBase", StringComparison.Ordinal)));

            IEnumerable<CustomAttributeData> attributeData = s_typeTestClass.GetTypeInfo().CustomAttributes;

            IEnumerator<CustomAttributeData> customAttrsdata = attributeData.GetEnumerator();

            Assert.Equal(4, attributeData.Count());
            Assert.Equal(3, attributeData.Count(attr => attr.ToString().Contains("MyAttribute_AllowMultiple")));
            Assert.Equal(1, attributeData.Count(attr => attr.ToString().Contains("MyAttribute_Single_M")));
        }
    }

    public class MyAttributeBase_M : Attribute
    {
        private String _name;
        public MyAttributeBase_M(String name)
        {
            _name = name;
        }
        public override String ToString() { return this.GetType() + " " + _name; }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public class MyAttribute_Single_M : MyAttributeBase_M
    {
        public MyAttribute_Single_M(String name) : base(name) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class MyAttribute_AllowMultiple_M : MyAttributeBase_M
    {
        public MyAttribute_AllowMultiple_M(String name) : base(name) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class MyAttribute_Single_Inherited : MyAttributeBase_M
    {
        public MyAttribute_Single_Inherited(String name) : base(name) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class MyAttribute_AllowMultiple_Inherited : MyAttributeBase_M
    {
        public MyAttribute_AllowMultiple_Inherited(String name) : base(name) { }
    }

    public class TestClassWithoutAttribute
    {
    }

    [MyAttribute_Single_M("singleBase"),
     MyAttribute_AllowMultiple_M("multiple1Base"),
     MyAttribute_AllowMultiple_M("multiple2Base"),
     MyAttribute_Single_Inherited("singleBase"),
     MyAttribute_AllowMultiple_Inherited("multipleBase")]
    public class TestBaseClass
    {
    }

    [MyAttribute_Single_M("single"),
     MyAttribute_AllowMultiple_M("multiple1"),
     MyAttribute_AllowMultiple_M("multiple2"),
     MyAttribute_AllowMultiple_Inherited("multiple")]
    public class TestClass : TestBaseClass
    {
    }
}
