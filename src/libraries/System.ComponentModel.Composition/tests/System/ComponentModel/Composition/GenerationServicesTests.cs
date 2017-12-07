// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.UnitTesting;
using Xunit;

namespace Microsoft.Internal
{
    public class GenerationServicesTests
    {
        // Has to be public, otherwise the dynamic method doesn't see it
        public enum TestEnum
        {
            First = 1,
            Second = 2
        }

        public static class DelegateTestClass
        {
            public static int Method(int i)
            {
                return i;
            }
        }

        private Func<T> CreateValueGenerator<T>(T value)
        {
            DynamicMethod methodBuilder = new DynamicMethod(TestServices.GenerateRandomString(), typeof(T), Type.EmptyTypes);
            // Generate the method body that simply returns the dictionary
            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            GenerationServices.LoadValue(ilGenerator, value);
            ilGenerator.Emit(OpCodes.Ret);
            return (Func<T>)methodBuilder.CreateDelegate(typeof(Func<T>));
        }

        private void TestSuccessfulValueGeneration<T>(T value)
        {
            Func<T> result = this.CreateValueGenerator<T>(value);
            T generatedValue = result.Invoke();
            Assert.Equal(value, generatedValue);
        }

        private void TestSuccessfulDictionaryGeneration(IDictionary<string, object> dictionary)
        {
            Func<IDictionary<string, object>> result = this.CreateValueGenerator<IDictionary<string, object>>(dictionary);
            IDictionary<string, object> generatedDictionary = result.Invoke();
            Assert.Equal(dictionary, generatedDictionary);
        }

        private void TestSuccessfulEnumerableGeneration<T>(IEnumerable enumerable)
        {
            Func<IEnumerable> result = this.CreateValueGenerator<IEnumerable>(enumerable);
            IEnumerable generatedEnumerable = result.Invoke();
            Assert.True(generatedEnumerable.Cast<T>().SequenceEqual(enumerable.Cast<T>()));
        }

        [Fact]
        public void PrimitiveTypes()
        {
            this.TestSuccessfulValueGeneration(Char.MinValue);
            this.TestSuccessfulValueGeneration(Char.MaxValue);
            this.TestSuccessfulValueGeneration((Char)42);

            this.TestSuccessfulValueGeneration(true);
            this.TestSuccessfulValueGeneration(false);

            this.TestSuccessfulValueGeneration(Byte.MinValue);
            this.TestSuccessfulValueGeneration(Byte.MaxValue);
            this.TestSuccessfulValueGeneration((Byte)42);

            this.TestSuccessfulValueGeneration(SByte.MinValue);
            this.TestSuccessfulValueGeneration(SByte.MaxValue);
            this.TestSuccessfulValueGeneration((SByte)42);

            this.TestSuccessfulValueGeneration(Int16.MinValue);
            this.TestSuccessfulValueGeneration(Int16.MaxValue);
            this.TestSuccessfulValueGeneration((Int16)42);

            this.TestSuccessfulValueGeneration(UInt16.MinValue);
            this.TestSuccessfulValueGeneration(UInt16.MaxValue);
            this.TestSuccessfulValueGeneration((UInt16)42);

            this.TestSuccessfulValueGeneration(Int32.MinValue);
            this.TestSuccessfulValueGeneration(Int32.MaxValue);
            this.TestSuccessfulValueGeneration((Int32)42);

            this.TestSuccessfulValueGeneration(UInt32.MinValue);
            this.TestSuccessfulValueGeneration(UInt32.MaxValue);
            this.TestSuccessfulValueGeneration((UInt32)42);

            this.TestSuccessfulValueGeneration(Int64.MinValue);
            this.TestSuccessfulValueGeneration(Int64.MaxValue);
            this.TestSuccessfulValueGeneration((Int64)42);

            this.TestSuccessfulValueGeneration(UInt64.MinValue);
            this.TestSuccessfulValueGeneration(UInt64.MaxValue);
            this.TestSuccessfulValueGeneration((UInt64)42);

            this.TestSuccessfulValueGeneration(Single.MinValue);
            this.TestSuccessfulValueGeneration(Single.MaxValue);
            this.TestSuccessfulValueGeneration((Single)42.42);

            this.TestSuccessfulValueGeneration(Double.MinValue);
            this.TestSuccessfulValueGeneration(Double.MaxValue);
            this.TestSuccessfulValueGeneration((Double)42.42);
        }

        [Fact]
        public void StringType()
        {
            this.TestSuccessfulValueGeneration("42");
        }

        [Fact]
        public void EnumType()
        {
            this.TestSuccessfulValueGeneration(TestEnum.Second);
        }

        [Fact]
        public void TypeType()
        {
            this.TestSuccessfulValueGeneration(typeof(TestEnum));
        }

        [Fact]
        public void PrimitiveTypeEnumerable()
        {
            int[] enumerable = new int[] { 1, 2, 3, 4, 5 };
            this.TestSuccessfulEnumerableGeneration<int>(enumerable);
        }

        [Fact]
        public void StringTypeEnumerable()
        {
            string[] enumerable = new string[] { "1", "2", "3", "4", "5" };
            this.TestSuccessfulEnumerableGeneration<string>(enumerable);
        }

        [Fact]
        [ActiveIssue(507696)]
        public void EnumTypeEnumerable()
        {
            TestEnum[] enumerable = new TestEnum[] { TestEnum.First, TestEnum.Second };
            this.TestSuccessfulEnumerableGeneration<string>(enumerable);
        }

        [Fact]
        public void MixedEnumerable()
        {
            List<object> list = new List<object>();
            list.Add(42);
            list.Add("42");
            list.Add(typeof(TestEnum));
            list.Add(TestEnum.Second);
            list.Add(null);

            this.TestSuccessfulEnumerableGeneration<object>(list);
        }
    }
}
