// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information. 

//
// ContextStackTest.cs - Unit tests for 
//	System.ComponentModel.Design.Serialization.ContextStack
//
// Author:
//	Ivan N. Zlatev <contact@i-nz.net>
//
// Copyright (C) 2007 Ivan N. Zlatev <contact@i-nz.net>
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

using System.ComponentModel.Design.Serialization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ContextStackTest
    {
        [Fact]
        public void IntegrityTest()
        {
            ContextStack stack = new ContextStack();

            string one = "one";
            string two = "two";
            stack.Push(two);
            stack.Push(one);
            Assert.Same(one, stack[typeof(string)]);
            Assert.Same(one, stack[0]);
            Assert.Same(one, stack.Current);

            Assert.Same(one, stack.Pop());

            Assert.Same(two, stack[typeof(string)]);
            Assert.Same(two, stack[0]);
            Assert.Same(two, stack.Current);

            string three = "three";
            stack.Append(three);

            Assert.Same(two, stack[typeof(string)]);
            Assert.Same(two, stack[0]);
            Assert.Same(two, stack.Current);

            Assert.Same(two, stack.Pop());

            Assert.Same(three, stack[typeof(string)]);
            Assert.Same(three, stack[0]);
            Assert.Same(three, stack.Current);
            Assert.Same(three, stack.Pop());

            Assert.Null(stack.Pop());
            Assert.Null(stack.Current);
        }

        [Fact]
        public void Append_Context_Null()
        {
            ContextStack stack = new ContextStack();
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => stack.Append(null));
            Assert.Equal(typeof(ArgumentNullException), ex.GetType());
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("context", ex.ParamName);
        }

        [Fact] // Item (Int32)
        public void Indexer1()
        {
            ContextStack stack = new ContextStack();
            string one = "one";
            string two = "two";

            stack.Push(one);
            stack.Push(two);

            Assert.Same(two, stack[0]);
            Assert.Same(one, stack[1]);
            Assert.Null(stack[2]);
            Assert.Same(two, stack.Pop());
            Assert.Same(one, stack[0]);
            Assert.Null(stack[1]);
            Assert.Same(one, stack.Pop());
            Assert.Null(stack[0]);
            Assert.Null(stack[1]);
        }

        [Fact] // Item (Int32)
        public void Indexer1_Level_Negative()
        {
            ContextStack stack = new ContextStack();
            stack.Push(new Foo());
            ArgumentOutOfRangeException ex;

            ex = Assert.Throws<ArgumentOutOfRangeException>(() => stack[-1]);
            Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            Assert.Null(ex.InnerException);
            Assert.Equal(new ArgumentOutOfRangeException("level").Message, ex.Message);
            Assert.Equal("level", ex.ParamName);

            ex = Assert.Throws<ArgumentOutOfRangeException>(() => stack[-5]);
            Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            Assert.Null(ex.InnerException);
            Assert.Equal(new ArgumentOutOfRangeException("level").Message, ex.Message);
            Assert.Equal("level", ex.ParamName);
        }

        [Fact] // Item (Type)
        public void Indexer2()
        {
            ContextStack stack = new ContextStack();

            Foo foo = new Foo();
            FooBar foobar = new FooBar();

            stack.Push(foobar);
            stack.Push(foo);
            Assert.Same(foo, stack[typeof(Foo)]);
            Assert.Same(foo, stack[typeof(IFoo)]);
            Assert.Same(foo, stack.Pop());
            Assert.Same(foobar, stack[typeof(Foo)]);
            Assert.Same(foobar, stack[typeof(FooBar)]);
            Assert.Same(foobar, stack[typeof(IFoo)]);
            Assert.Null(stack[typeof(string)]);
        }

        [Fact] // Item (Type)
        public void Indexer2_Type_Null()
        {
            ContextStack stack = new ContextStack();
            stack.Push(new Foo());
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => stack[(Type)null]);
            Assert.Equal(typeof(ArgumentNullException), ex.GetType());
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("type", ex.ParamName);
        }

        [Fact]
        public void Push_Context_Null()
        {
            ContextStack stack = new ContextStack();
            ArgumentNullException ex= Assert.Throws<ArgumentNullException>(()=> stack.Push(null));
            Assert.Equal(typeof(ArgumentNullException), ex.GetType());
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("context", ex.ParamName);
        }

        [Fact]
        public void Append_NoItems_Success()
        {
            var stack = new ContextStack();
            stack.Append("value");
            Assert.Equal("value", stack[0]);
        }

        [Fact]
        public void Indexer_GetWithoutItems_ReturnsNull()
        {
            var stack = new ContextStack();
            Assert.Null(stack[1]);
            Assert.Null(stack[typeof(int)]);
        }

        public interface IFoo
        {
        }

        public class Foo : IFoo
        {
        }

        public class FooBar : Foo
        {
        }
    }
}