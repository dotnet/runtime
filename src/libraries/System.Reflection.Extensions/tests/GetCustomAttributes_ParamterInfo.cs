// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Assembly = System.Reflection.Tests;

[module: Assembly.MyAttribute_Single_P("single"), Assembly.MyAttribute_AllowMultiple_P("multiple1"), Assembly.MyAttribute_AllowMultiple_P("multiple2")]

namespace System.Reflection.Tests
{
    public class GetCustomAttributes_ParameterInfo
    {
        [Fact]
        public void IsDefined_Inherit()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            Assert.False(CustomAttributeExtensions.IsDefined(piWithAttributes,
            typeof(CLSCompliantAttribute), false));
        }

        [Fact]
        public void IsDefined()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            Assert.True(CustomAttributeExtensions.IsDefined(piWithAttributes, typeof(MyAttribute_Single_P)));

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                CustomAttributeExtensions.IsDefined(piWithAttributes, typeof(string));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeExtensions.IsDefined(piWithAttributes, null);
            });
        }

        [Fact]
        public void GetCustomAttributeOfT_Single_NoInherit()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            Attribute attribute = CustomAttributeExtensions.GetCustomAttribute<MyAttribute_Single_P>(piWithAttributes, false);
            Assert.NotNull(attribute);
        }

        [Fact]
        public void GetCustomAttributeOfT_Single()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            Attribute attribute = CustomAttributeExtensions.GetCustomAttribute<MyAttribute_Single_P>(piWithAttributes);
            Assert.NotNull(attribute);

            Assert.Throws<AmbiguousMatchException>(() =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute<MyAttribute_AllowMultiple_P>(piWithAttributes);
            });
        }

        [Fact]
        public void GetCustomAttributeOfT_Multiple_NoInherit()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            IEnumerable<Attribute> attributes;


            attributes = CustomAttributeExtensions.GetCustomAttributes<MyAttribute_AllowMultiple_P>(piWithAttributes, false);
            Assert.Equal(2, attributes.Count());
        }

        [Fact]
        public void GetCustomAttributeOfT()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            IEnumerable<Attribute> attributes;


            attributes = CustomAttributeExtensions.GetCustomAttributes<MyAttribute_Single_P>(piWithAttributes);
            Assert.Equal(1, attributes.Count());

            attributes = CustomAttributeExtensions.GetCustomAttributes<CLSCompliantAttribute>(piWithAttributes);
            Assert.Equal(0, attributes.Count());
        }

        [Fact]
        public void GetCustomAttribute_Single_NoInherit()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            Attribute attribute = CustomAttributeExtensions.GetCustomAttribute(piWithAttributes, typeof(MyAttribute_Single_P), false);
            Assert.NotNull(attribute);
        }

        [Fact]
        public void GetCustomAttribute_Single()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            Attribute attribute = CustomAttributeExtensions.GetCustomAttribute(piWithAttributes, typeof(MyAttribute_Single_P));
            Assert.NotNull(attribute);

            Assert.Throws<AmbiguousMatchException>(() =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute(piWithAttributes, typeof(MyAttribute_AllowMultiple_P));
            });

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute(piWithAttributes, typeof(string));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                attribute = CustomAttributeExtensions.GetCustomAttribute(piWithAttributes, null);
            });
        }

        [Fact]
        public void GetCustomAttribute_Multiple_NoInherit()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            IEnumerable<Attribute> attributes;

            attributes = CustomAttributeExtensions.GetCustomAttributes(piWithAttributes, typeof(MyAttribute_AllowMultiple_P), false);
            Assert.Equal(2, attributes.Count());
        }

        [Fact]
        public void GetCusomAttribute_Multiple()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            IEnumerable<Attribute> attributes;


            attributes = CustomAttributeExtensions.GetCustomAttributes(piWithAttributes, typeof(MyAttribute_AllowMultiple_P));
            Assert.Equal(2, attributes.Count());

            attributes = CustomAttributeExtensions.GetCustomAttributes(piWithAttributes, typeof(CLSCompliantAttribute));
            Assert.Equal(0, attributes.Count());

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                attributes = CustomAttributeExtensions.GetCustomAttributes(piWithAttributes, typeof(string));
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                attributes = CustomAttributeExtensions.GetCustomAttributes(piWithAttributes, null);
            });
        }

        [Fact]
        public void GetCustomAttribute_General_NoInherit()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithoutAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithoutAttribute");
            ParameterInfo piWithoutAttributes = miWithoutAttributes.GetParameters()[0];

            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            IEnumerable<Attribute> attributes;


            attributes = CustomAttributeExtensions.GetCustomAttributes(piWithoutAttributes, false);
            Assert.Equal(0, attributes.Count());

            attributes = CustomAttributeExtensions.GetCustomAttributes(piWithAttributes, false);
            Assert.Equal(5, attributes.Count());
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_P single", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_P multiple1", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_P multiple2", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_Inherited_P single", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_Inherited_P multiple", StringComparison.Ordinal)));
        }

        [Fact]
        public void GetCustom_Attribute_General()
        {
            Type type = typeof(TestClass_P);
            MethodInfo miWithAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithAttribute");
            ParameterInfo piWithAttributes = miWithAttributes.GetParameters()[0];

            MethodInfo miWithoutAttributes = type.GetTypeInfo().GetDeclaredMethod("methodWithoutAttribute");
            ParameterInfo piWithoutAttributes = miWithoutAttributes.GetParameters()[0];


            IEnumerable<Attribute> attributes = CustomAttributeExtensions.GetCustomAttributes(piWithoutAttributes);
            Assert.Equal(0, attributes.Count());

            IEnumerable<CustomAttributeData> attributeData = piWithoutAttributes.CustomAttributes;
            Assert.Equal(0, attributes.Count());

            attributes = CustomAttributeExtensions.GetCustomAttributes(piWithAttributes);
            Assert.Equal(5, attributes.Count());
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_P single", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_P multiple1", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_P multiple2", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_Inherited_P single", StringComparison.Ordinal)));
            Assert.Equal(1, attributes.Count(attr => attr.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_Inherited_P multiple", StringComparison.Ordinal)));

            attributeData = piWithAttributes.CustomAttributes;
            Assert.Equal(5, attributeData.Count());

            Assert.Equal(2, attributeData.Count(attr => attr.AttributeType.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_P", StringComparison.Ordinal)));
            Assert.Equal(1, attributeData.Count(attr => attr.AttributeType.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_P", StringComparison.Ordinal)));
            Assert.Equal(1, attributeData.Count(attr => attr.AttributeType.ToString().Equals("System.Reflection.Tests.MyAttribute_Single_Inherited_P", StringComparison.Ordinal)));
            Assert.Equal(1, attributeData.Count(attr => attr.AttributeType.ToString().Equals("System.Reflection.Tests.MyAttribute_AllowMultiple_Inherited_P", StringComparison.Ordinal)));
        }

        [Fact]
        [ActiveIssue(@"https://github.com/dotnet/coreclr/issues/6600")]
        public void GetCustom_Attribute_On_Return_Parameter_On_Parent_Method()
        {
            Type type = typeof(TestClass_P_Derived);
            MethodInfo miWithReturnAttribute = type.GetTypeInfo().GetDeclaredMethod("methodWithReturnAttribute");
            ParameterInfo returnParameter = miWithReturnAttribute.ReturnParameter;
            MyAttribute_Single_P attribute = CustomAttributeExtensions.GetCustomAttribute<MyAttribute_Single_P>(returnParameter, inherit: true);
            Assert.NotNull(attribute);
        }
    }

    public class ParameterInfoAttributeBase : Attribute
    {
        private string _name;
        public ParameterInfoAttributeBase(string name)
        {
            _name = name;
        }

        public override string ToString() { return this.GetType() + " " + _name; }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public class MyAttribute_Single_P : ParameterInfoAttributeBase
    {
        public MyAttribute_Single_P(string name) : base(name) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class MyAttribute_AllowMultiple_P : ParameterInfoAttributeBase
    {
        public MyAttribute_AllowMultiple_P(string name) : base(name) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class MyAttribute_Single_Inherited_P : ParameterInfoAttributeBase
    {
        public MyAttribute_Single_Inherited_P(string name) : base(name) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class MyAttribute_AllowMultiple_Inherited_P : ParameterInfoAttributeBase
    {
        public MyAttribute_AllowMultiple_Inherited_P(string name) : base(name) { }
    }

    public class TestClass_P
    {
        public void methodWithoutAttribute(int param) { }
        public void methodWithAttribute([MyAttribute_Single_P("single"),
                                      MyAttribute_AllowMultiple_P("multiple1"),
                                      MyAttribute_AllowMultiple_P("multiple2"),
                                      MyAttribute_Single_Inherited_P("single"),
                                      MyAttribute_AllowMultiple_Inherited_P("multiple")] int param)
        { }

        [return: MyAttribute_Single_P("single")]
        public virtual byte methodWithReturnAttribute() { return 0; }
    }

    public class TestClass_P_Derived : TestClass_P
    {
        public override byte methodWithReturnAttribute() { return 1; }
    }
}
