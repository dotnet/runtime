// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class MessageFormattingTest
    {
        [Fact]
        public void BasicStringMessageFormatter()
        {
            //arrange
            TwoParamMetadata<string, int> metadata = new TwoParamMetadata<string, int>("User {p1} is {p2} years old.");
            Func<(string, int), Exception?, string> formatter = metadata.CreateStringMessageFormatter();

            //act
            string result = formatter(("John", 10), null);

            //assert
            Assert.Equal("User John is 10 years old.", result);
        }

        [Fact]
        public void BasicMessageFormatter()
        {
            //arrange
            TwoParamMetadata<string, int> metadata = new TwoParamMetadata<string, int>("User {p1} is {p2} years old.");
            FormatLogMessage<(string,int)> formatter = metadata.CreateMessageFormatter();
            PooledByteBufferWriter writer = new PooledByteBufferWriter(256);

            //act
            (string, int) state = ("John", 10);
            formatter(ref state, writer);

            //assert
            Assert.Equal("User John is 10 years old.", MemoryMarshal.Cast<byte,char>(writer.WrittenMemory.Span).ToString());
        }

        [Fact]
        public void NullSentinel()
        {
            //arrange
            TwoParamMetadata<string, int> metadata = new TwoParamMetadata<string, int>("User {p1} is {p2} years old.");
            Func<(string, int), Exception?, string> formatter = metadata.CreateStringMessageFormatter();

            //act
            string result = formatter((null, 10), null);

            //assert
            Assert.Equal("User (null) is 10 years old.", result);
        }

        [Fact]
        public void EnumerateArrays()
        {
            //arrange
            TwoParamMetadata<string[], int> metadata = new TwoParamMetadata<string[], int>("Users {p1} are {p2} years old.");
            Func<(string[], int), Exception?, string> formatter = metadata.CreateStringMessageFormatter();

            //act
            string result = formatter((new string[] { "Joe", "Jim", "John" }, 10), null);

            //assert
            Assert.Equal("Users Joe, Jim, John are 10 years old.", result);
        }

        [Fact]
        public void MessagesHandleFormatSpecifiers()
        {
            //arrange
            TwoParamMetadata<string, int> metadata = new TwoParamMetadata<string, int>("User {p1} is 0x{p2:x} years old.");
            Func<(string, int), Exception?, string> formatter = metadata.CreateStringMessageFormatter();

            //act
            string result = formatter(("John", 10), null);

            //assert
            Assert.Equal("User John is 0xa years old.", result);
        }

        [Theory]
        [InlineData("User {p1,-10} is {p2,-10} years old.", "User John       is 10         years old.")]
        [InlineData("User {p1,10} is {p2,10} years old.", "User       John is         10 years old.")]
        [InlineData("User {p1,1} is {p2,1} years old.", "User John is 10 years old.")]
        [InlineData("User {p1,-1} is {p2,-1} years old.", "User John is 10 years old.")]
        [InlineData("User {p1,-5} is {p2,3} years old.", "User John  is  10 years old.")]
        public void MessagesHandleAlignment(string format, string expected)
        {
            //arrange
            TwoParamMetadata<string, int> metadata = new TwoParamMetadata<string, int>(format);
            Func<(string, int), Exception?, string> formatter = metadata.CreateStringMessageFormatter();

            //act
            string result = formatter(("John", 10), null);

            //assert
            Assert.Equal(expected, result);
        }
    }

    class TwoParamMetadata<T1, T2> : ILogMetadata<(T1, T2)>
    {
        public TwoParamMetadata(string format)
        {
            OriginalFormat = format;
        }

        public LogLevel LogLevel => LogLevel.None;

        public EventId EventId => 1;

        public string OriginalFormat { get; set; }

        public int PropertyCount => 2;

        public VisitPropertyListAction<(T1, T2), TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> propertyVisitorFactory)
        {
            VisitPropertyAction<T1, TCookie> visit0 = propertyVisitorFactory.GetPropertyVisitor<T1>();
            VisitPropertyAction<T2, TCookie> visit1 = propertyVisitorFactory.GetPropertyVisitor<T2>();
            return Visit;

            void Visit(ref (T1, T2) value, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                visit0(0, value.Item1, ref spanCookie, ref cookie);
                visit1(1, value.Item2, ref spanCookie, ref cookie);
            }
        }
        public LogPropertyInfo GetPropertyInfo(int index)
        {
            switch (index)
            {
                case 0: return new LogPropertyInfo("p1", null);
                case 1: return new LogPropertyInfo("p2", null);
            }
            throw new IndexOutOfRangeException();
        }
    }
}
