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
            this.TestSuccessfulValueGeneration(char.MinValue);
            this.TestSuccessfulValueGeneration(char.MaxValue);
            this.TestSuccessfulValueGeneration((char)42);

            this.TestSuccessfulValueGeneration(true);
            this.TestSuccessfulValueGeneration(false);

            this.TestSuccessfulValueGeneration(byte.MinValue);
            this.TestSuccessfulValueGeneration(byte.MaxValue);
            this.TestSuccessfulValueGeneration((byte)42);

            this.TestSuccessfulValueGeneration(sbyte.MinValue);
            this.TestSuccessfulValueGeneration(sbyte.MaxValue);
            this.TestSuccessfulValueGeneration((sbyte)42);

            this.TestSuccessfulValueGeneration(short.MinValue);
            this.TestSuccessfulValueGeneration(short.MaxValue);
            this.TestSuccessfulValueGeneration((short)42);

            this.TestSuccessfulValueGeneration(ushort.MinValue);
            this.TestSuccessfulValueGeneration(ushort.MaxValue);
            this.TestSuccessfulValueGeneration((ushort)42);

            this.TestSuccessfulValueGeneration(int.MinValue);
            this.TestSuccessfulValueGeneration(int.MaxValue);
            this.TestSuccessfulValueGeneration((int)42);

            this.TestSuccessfulValueGeneration(uint.MinValue);
            this.TestSuccessfulValueGeneration(uint.MaxValue);
            this.TestSuccessfulValueGeneration((uint)42);

            this.TestSuccessfulValueGeneration(long.MinValue);
            this.TestSuccessfulValueGeneration(long.MaxValue);
            this.TestSuccessfulValueGeneration((long)42);

            this.TestSuccessfulValueGeneration(ulong.MinValue);
            this.TestSuccessfulValueGeneration(ulong.MaxValue);
            this.TestSuccessfulValueGeneration((ulong)42);

            this.TestSuccessfulValueGeneration(float.MinValue);
            this.TestSuccessfulValueGeneration(float.MaxValue);
            this.TestSuccessfulValueGeneration((float)42.42);

            this.TestSuccessfulValueGeneration(double.MinValue);
            this.TestSuccessfulValueGeneration(double.MaxValue);
            this.TestSuccessfulValueGeneration((double)42.42);
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
