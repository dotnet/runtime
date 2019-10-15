// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class CollectionConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new CollectionConverter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid(new CustomCollection(), "(Collection)").WithInvariantRemoteInvokeCulture();
            yield return ConvertTest.Valid(1, "1");

            yield return ConvertTest.CantConvertTo(new CustomCollection(), typeof(CustomCollection));
            yield return ConvertTest.CantConvertTo(new CustomCollection(), typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo(new CustomCollection(), typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.CantConvertFrom(new CustomCollection());
        }

        public static IEnumerable<object[]> GetProperties_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CustomCollection() };
        }

        [Theory]
        [MemberData(nameof(GetProperties_TestData))]
        public void GetProperties_Invoke_ReturnsEmpty(object value)
        {
            PropertyDescriptorCollection properties = Converter.GetProperties(value);
            Assert.Empty(properties);
        }

        [Serializable]
        public class CustomCollection : ICollection
        {
            public void CopyTo(Array array, int index) => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool IsSynchronized => throw new NotImplementedException();

            public object SyncRoot => throw new NotImplementedException();

            public IEnumerator GetEnumerator() => throw new NotImplementedException();
        }
    }
}
