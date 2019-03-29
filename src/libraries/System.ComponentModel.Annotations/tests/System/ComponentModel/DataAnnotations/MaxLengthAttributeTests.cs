// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class MaxLengthAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new MaxLengthAttribute(10), null);
            yield return new TestCase(new MaxLengthAttribute(15), "UnderMaxLength");
            yield return new TestCase(new MaxLengthAttribute(16), "EqualToMaxLength");
            yield return new TestCase(new MaxLengthAttribute(-1), "SpecifiedMaximumMaxLength");
            yield return new TestCase(new MaxLengthAttribute(-1), new int[20]);
            yield return new TestCase(new MaxLengthAttribute(15), new string[14]);
            yield return new TestCase(new MaxLengthAttribute(16), new string[16]);

            yield return new TestCase(new MaxLengthAttribute(16), new int[4, 4]);
            yield return new TestCase(new MaxLengthAttribute(16), new string[3, 4]);
        }

        public static IEnumerable<object[]> ValidValues_ICollection()
        {
            yield return new object[] { new MaxLengthAttribute(-1), new Collection<int>(new int[20]) };
            yield return new object[] { new MaxLengthAttribute(15), new Collection<string>(new string[14]) };
            yield return new object[] { new MaxLengthAttribute(16), new Collection<string>(new string[16]) };

            yield return new object[] { new MaxLengthAttribute(-1), new List<int>(new int[20]) };
            yield return new object[] { new MaxLengthAttribute(15), new List<string>(new string[14]) };
            yield return new object[] { new MaxLengthAttribute(16), new List<string>(new string[16]) };

            //ICollection<T> but not ICollection
            yield return new object[] { new MaxLengthAttribute(-1), new HashSet<int>(Enumerable.Range(1, 20)) };
            yield return new object[] { new MaxLengthAttribute(15), new HashSet<string>(Enumerable.Range(1, 14).Select(i => i.ToString())) };
            yield return new object[] { new MaxLengthAttribute(16), new HashSet<string>(Enumerable.Range(1, 16).Select(i => i.ToString())) };

            //ICollection but not ICollection<T>
            yield return new object[] { new MaxLengthAttribute(-1), new ArrayList(new int[20]) };
            yield return new object[] { new MaxLengthAttribute(15), new ArrayList(new string[14]) };
            yield return new object[] { new MaxLengthAttribute(16), new ArrayList(new string[16]) };

            //Multi ICollection<T>
            yield return new object[] { new MaxLengthAttribute(1), new MultiCollection() };
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new MaxLengthAttribute(12), "OverMaxLength");
            yield return new TestCase(new MaxLengthAttribute(12), new byte[13]);

            yield return new TestCase(new MaxLengthAttribute(12), new int[4, 4]);
        }

        public static IEnumerable<object> InvalidValues_ICollection()
        {
            yield return new object[] { new MaxLengthAttribute(12), new Collection<byte>(new byte[13]) };
            yield return new object[] { new MaxLengthAttribute(12), new List<byte>(new byte[13]) };
            yield return new object[] { new MaxLengthAttribute(12), new HashSet<int>(Enumerable.Range(1, 13)) };
        }

        [Fact]
        public static void Ctor()
        {
            Assert.Equal(-1, new MaxLengthAttribute().Length);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(10)]
        [InlineData(0)]
        [InlineData(-10)]
        public static void Ctor_Int(int length)
        {
            Assert.Equal(length, new MaxLengthAttribute(length).Length);
        }

        [Theory]
        [MemberData(nameof(ValidValues_ICollection))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "MaxLengthAttribute in the .NET Framework doesn't support ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetCore_Valid(MaxLengthAttribute attribute, object value)
        {
            attribute.Validate(value, new ValidationContext(new object()));
            Assert.True(attribute.IsValid(value));
        }

        [Theory]
        [MemberData(nameof(InvalidValues_ICollection))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "MaxLengthAttribute in the .NET Framework doesn't support ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetCore_Invalid(MaxLengthAttribute attribute, object value)
        {
            Assert.Throws<ValidationException>(() => attribute.Validate(value, new ValidationContext(new object())));
            Assert.False(attribute.IsValid(value));
        }

        [Theory]
        [MemberData(nameof(ValidValues_ICollection))]
        [MemberData(nameof(InvalidValues_ICollection))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "MaxLengthAttribute in the .NET Core supports ICollection.Count. See https://github.com/dotnet/corefx/issues/18361")]
        public void Validate_ICollection_NetFx_ThrowsInvalidCastException(MaxLengthAttribute attribute, object value)
        {
            Assert.Throws<InvalidCastException>(() => attribute.Validate(value, new ValidationContext(new object())));
            Assert.Throws<InvalidCastException>(() => attribute.IsValid(value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public static void GetValidationResult_InvalidLength_ThrowsInvalidOperationException(int length)
        {
            var attribute = new MaxLengthAttribute(length);
            Assert.Throws<InvalidOperationException>(() => attribute.Validate("Twoflower", new ValidationContext(new object())));
        }

        [Fact]
        public static void GetValidationResult_ValueNotStringOrICollection_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MaxLengthAttribute().GetValidationResult(new Random(), new ValidationContext(new object())));
        }

        [Fact]
        public static void GetValidationResult_ValueGenericIEnumerable_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new MaxLengthAttribute().GetValidationResult(new GenericIEnumerableClass(), new ValidationContext(new object())));
        }
    }

    class GenericIEnumerableClass : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator() => Enumerable.Empty<int>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class MultiCollection : Collection<string>, ICollection<int>, ICollection<uint>
    {
        int ICollection<int>.Count => 0;
        int ICollection<uint>.Count => 0;
        bool ICollection<int>.IsReadOnly => throw new NotSupportedException();
        bool ICollection<uint>.IsReadOnly => throw new NotSupportedException();
        void ICollection<int>.Add(int item) => throw new NotSupportedException();
        void ICollection<uint>.Add(uint item) => throw new NotSupportedException();
        void ICollection<int>.Clear() => throw new NotSupportedException();
        void ICollection<uint>.Clear() => throw new NotSupportedException();
        bool ICollection<int>.Contains(int item) => throw new NotSupportedException();
        bool ICollection<uint>.Contains(uint item) => throw new NotSupportedException();
        void ICollection<int>.CopyTo(int[] array, int arrayIndex) => throw new NotSupportedException();
        void ICollection<uint>.CopyTo(uint[] array, int arrayIndex) => throw new NotSupportedException();
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
        IEnumerator<uint> IEnumerable<uint>.GetEnumerator() => throw new NotSupportedException();
        bool ICollection<int>.Remove(int item) => throw new NotSupportedException();
        bool ICollection<uint>.Remove(uint item) => throw new NotSupportedException();
    }
}
