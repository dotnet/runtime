// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information. 

//
// InstanceDescriptorTest.cs - Unit tests for 
//	System.ComponentModel.Design.Serialization.InstanceDescriptor
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

using Xunit;
using System.Reflection;
using System.Linq;

namespace System.ComponentModel.Design.Serialization.Tests
{
    public class InstanceDescriptorTest
    {
        private const string Url = "http://www.mono-project.com/";

        [Fact]
        public void Ctor_ConstructorInfo_ICollection()
        {
            ConstructorInfo ci = typeof(Uri).GetConstructor(new Type[] { typeof(string) });

            InstanceDescriptor id = new InstanceDescriptor(ci, new object[] { Url });
            Assert.Equal(1, id.Arguments.Count);
            Assert.True(id.IsComplete);
            Assert.Same(ci, id.MemberInfo);
            Uri uri = (Uri)id.Invoke();
            Assert.Equal(Url, uri.AbsoluteUri);
        }

        [Fact]
        public void Constructor_ConstructorInfo_ICollection_Boolean()
        {
            ConstructorInfo ci = typeof(Uri).GetConstructor(new Type[] { typeof(string) });

            InstanceDescriptor id = new InstanceDescriptor(ci, new object[] { Url }, false);
            Assert.Equal(1, id.Arguments.Count);
            Assert.False(id.IsComplete);
            Assert.Same(ci, id.MemberInfo);
            Uri uri = (Uri)id.Invoke();
            Assert.Equal(Url, uri.AbsoluteUri);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new object[] { } })]
        public void Ctor_ConstructorInfoArgumentMismatch_ThrowsArgumentException(object[] arguments)
        {
            ConstructorInfo ci = typeof(Uri).GetConstructor(new Type[] { typeof(string) });
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(ci, arguments));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(ci, arguments, false));
        }
        [Fact]
        public void Ctor_StaticConstructor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(StaticConstructor).GetConstructors(BindingFlags.Static | BindingFlags.NonPublic).Single();
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(constructor, null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new object[] { } })]
        public void Ctor_FieldInfo_ICollection(object[] arguments)
        {
            FieldInfo fi = typeof(StaticField).GetField(nameof(StaticField.Field));

            InstanceDescriptor id = new InstanceDescriptor(fi, arguments);
            Assert.Equal(0, id.Arguments.Count);
            Assert.True(id.IsComplete);
            Assert.Same(fi, id.MemberInfo);
            Assert.NotNull(id.Invoke());
        }

        [Fact]
        public void Ctor_FieldInfoArgumentMismatch_ThrowsArgumentException()
        {
            FieldInfo fi = typeof(StaticField).GetField(nameof(StaticField.Field));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(fi, new object[] { Url }));
        }

        [Fact]
        public void Ctor_NonStaticFieldInfo_ThrowsArgumentException()
        {
            FieldInfo fi = typeof(InstanceField).GetField(nameof(InstanceField.Name));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(fi, null));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(fi, null, false));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new object[] { new object[] { } })]
        public void Ctor_PropertyInfo_ICollection(object[] arguments)
        {
            PropertyInfo pi = typeof(StaticProperty).GetProperty(nameof(StaticProperty.Property));

            InstanceDescriptor id = new InstanceDescriptor(pi, arguments);
            Assert.Equal(0, id.Arguments.Count);
            Assert.True(id.IsComplete);
            Assert.Same(pi, id.MemberInfo);
            Assert.NotNull(id.Invoke());
        }

        [Fact]
        public void Invoke_PropertyInfoArgumentMismatch_ThrowsTargetParameterCountException()
        {
            PropertyInfo pi = typeof(StaticProperty).GetProperty(nameof(StaticProperty.Property));

            InstanceDescriptor id = new InstanceDescriptor(pi, new object[] { Url });
            Assert.Equal(1, id.Arguments.Count);
            object[] arguments = new object[id.Arguments.Count];
            id.Arguments.CopyTo(arguments, 0);
            Assert.Same(Url, arguments[0]);
            Assert.True(id.IsComplete);
            Assert.Same(pi, id.MemberInfo);

            Assert.Throws<TargetParameterCountException>(() => id.Invoke());
        }

        [Fact]
        public void Ctor_NonStaticPropertyInfo_ThrowsArgumentException()
        {
            PropertyInfo pi = typeof(InstanceProperty).GetProperty(nameof(InstanceProperty.Property));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(pi, null));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(pi, null, false));
        }

        [Fact]
        public void Ctor_WriteOnlyPropertyInfo_ThrowsArgumentException()
        {
            PropertyInfo pi = typeof(WriteOnlyProperty).GetProperty(nameof(WriteOnlyProperty.Name));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(pi, null));
        }

        [Fact]
        public void Ctor_MethodInfo_ICollection()
        {
            MethodInfo method = typeof(MethodClass).GetMethod(nameof(MethodClass.StaticMethod));
            var arguments = new object[] { 1 };

            var instanceDescriptor = new InstanceDescriptor(method, arguments);
            Assert.Same(method, instanceDescriptor.MemberInfo);
            Assert.Equal(arguments, instanceDescriptor.Arguments);
            Assert.NotSame(arguments, instanceDescriptor.Arguments);
            Assert.True(instanceDescriptor.IsComplete);

            Assert.Equal("1", instanceDescriptor.Invoke());
        }

        [Fact]
        public void Ctor_NonStaticMethod_ThrowsArgumentException()
        {
            MethodInfo method = typeof(MethodClass).GetMethod(nameof(MethodClass.Method));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(method, new object[] { 1 }));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void Ctor_IncorrectMethodArgumentCount_ThrowsArgumentException(int count)
        {
            MethodInfo method = typeof(MethodClass).GetMethod(nameof(MethodClass.StaticMethod));
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(method, new object[count]));
        }

        [Fact]
        public void Ctor_EventInfo_ICollection()
        {
            EventInfo eventInfo = typeof(EventClass).GetEvent(nameof(EventClass.Event));
            var arguments = new object[] { 1 };

            var instanceDescriptor = new InstanceDescriptor(eventInfo, arguments);
            Assert.Same(eventInfo, instanceDescriptor.MemberInfo);
            Assert.Equal(arguments, instanceDescriptor.Arguments);
            Assert.NotSame(arguments, instanceDescriptor.Arguments);
            Assert.True(instanceDescriptor.IsComplete);

            Assert.Null(instanceDescriptor.Invoke());
        }

        [Fact]
        public void Invoke_ArgumentInstanceDescriptor_InvokesArgument()
        {
            MethodInfo argumentMethod = typeof(MethodClass).GetMethod(nameof(MethodClass.IntMethod));
            var argumentInstanceDescriptor = new InstanceDescriptor(argumentMethod, null);

            MethodInfo method = typeof(MethodClass).GetMethod(nameof(MethodClass.StaticMethod));
            var instanceDescriptor = new InstanceDescriptor(method, new object[] { argumentInstanceDescriptor });

            Assert.Equal("1", instanceDescriptor.Invoke());
        }

        [Fact]
        public void Invoke_NullMember_ReturnsNull()
        {
            var instanceDescriptor = new InstanceDescriptor(null, new object[0]);
            Assert.Null(instanceDescriptor.Invoke());
        }

        private class EventClass
        {
            public event EventHandler Event { add { } remove { } }
        }

        private class StaticConstructor
        {
            static StaticConstructor() { }
        }

        private class MethodClass
        {
            public void Method(int i) { }

            public static int IntMethod() => 1;
            public static string StaticMethod(int i) => i.ToString();
        }

        private class WriteOnlyProperty
        {
            public static string Name
            {
                set { }
            }
        }

        public class InstanceField
        {
            public string Name = "FieldValue";
        }

        public class InstanceProperty
        {
            public string Property => "PropertyValue";
        }

        public class StaticField
        {
            public static readonly string Field = "FieldValue";
        }

        public class StaticProperty
        {
            public static string Property => "PropertyValue";
        }
    }
}
