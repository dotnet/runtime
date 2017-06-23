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
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Threading;

namespace System.ComponentModel.Tests
{
    public class InstanceDescriptorTest
    {
        private const string url = "http://www.mono-project.com/";
        private ConstructorInfo ci;

        public InstanceDescriptorTest()
        {
            ci = typeof(Uri).GetConstructor(new Type[1] { typeof(string) });
        }

        [Fact]
        public void Constructor0_Arguments_Mismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(ci, null));
            // Length mismatch
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Constructor_MemberInfo_ICollection()
        {
            InstanceDescriptor id = new InstanceDescriptor(ci, new object[] { url });
            Assert.Equal(1, id.Arguments.Count);
            Assert.True(id.IsComplete);
            Assert.Same(ci, id.MemberInfo);
            Uri uri = (Uri)id.Invoke();
            Assert.Equal(url, uri.AbsoluteUri);
        }

        [Fact]
        public void Constructor_MemberInfo_Null_Boolean()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(ci, null, false));
            // mismatch for required parameters
        }

        [Fact]
        public void Constructor_MemberInfo_ICollection_Boolean()
        {
            InstanceDescriptor id = new InstanceDescriptor(ci, new object[] { url }, false);
            Assert.Equal(1, id.Arguments.Count);
            Assert.False(id.IsComplete);
            Assert.Same(ci, id.MemberInfo);
            Uri uri = (Uri)id.Invoke();
            Assert.Equal(url, uri.AbsoluteUri);
        }

        [Fact]
        public void Field_Arguments_Empty()
        {
            FieldInfo fi = typeof(StaticField).GetField(nameof(StaticField.Field));

            InstanceDescriptor id = new InstanceDescriptor(fi, new object[0]);
            Assert.Equal(0, id.Arguments.Count);
            Assert.True(id.IsComplete);
            Assert.Same(fi, id.MemberInfo);
            Assert.NotNull(id.Invoke());
        }

        [Fact]
        public void Field_Arguments_Mismatch()
        {
            FieldInfo fi = typeof(StaticField).GetField(nameof(StaticField.Field));

            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(fi, new object[] { url }));
            // Parameter must be static
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Field_Arguments_Null()
        {
            FieldInfo fi = typeof(StaticField).GetField(nameof(StaticField.Field));

            InstanceDescriptor id = new InstanceDescriptor(fi, null);
            Assert.Equal(0, id.Arguments.Count);
            Assert.True(id.IsComplete);
            Assert.Same(fi, id.MemberInfo);
            Assert.NotNull(id.Invoke());
        }

        [Fact]
        public void Field_MemberInfo_NonStatic()
        {
            FieldInfo fi = typeof(InstanceField).GetField(nameof(InstanceField.Name));

            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(fi, null));
            // Parameter must be static
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Property_Arguments_Mismatch()
        {
            PropertyInfo pi = typeof(StaticProperty).GetProperty(nameof(StaticProperty.Property));

            InstanceDescriptor id = new InstanceDescriptor(pi, new object[] { url });
            Assert.Equal(1, id.Arguments.Count);
            object[] arguments = new object[id.Arguments.Count];
            id.Arguments.CopyTo(arguments, 0);
            Assert.Same(url, arguments[0]);
            Assert.True(id.IsComplete);
            Assert.Same(pi, id.MemberInfo);

            Assert.Throws<TargetParameterCountException>(() => id.Invoke());
        }

        [Fact]
        public void Property_Arguments_Null()
        {
            PropertyInfo pi = typeof(StaticProperty).GetProperty(nameof(StaticProperty.Property));

            InstanceDescriptor id = new InstanceDescriptor(pi, null);
            Assert.Equal(0, id.Arguments.Count);
            Assert.True(id.IsComplete);
            Assert.Same(pi, id.MemberInfo);
            Assert.NotNull(id.Invoke());
        }

        [Fact]
        public void Property_MemberInfo_NonStatic()
        {
            PropertyInfo pi = typeof(InstanceProperty).GetProperty(nameof(InstanceProperty.Property));

            ArgumentException ex;

            ex = AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(pi, null));
            // Parameter must be static
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }

            ex = AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(pi, null, false));
            // Parameter must be static
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Property_MemberInfo_WriteOnly()
        {
            PropertyInfo pi = typeof(WriteOnlyProperty).GetProperty(nameof(WriteOnlyProperty.Name));

            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => new InstanceDescriptor(pi, null));
            // Parameter must be readable
            Assert.Equal(typeof(ArgumentException), ex.GetType());
            Assert.Null(ex.InnerException);
            if (!PlatformDetection.IsNetNative) // .Net Native toolchain optimizes away exception messages and paramnames.
            {
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        private class WriteOnlyProperty
        {
            public static string Name
            {
                set
                {
                }
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
