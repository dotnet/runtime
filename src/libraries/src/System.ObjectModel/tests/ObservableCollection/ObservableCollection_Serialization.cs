// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public partial class ObservableCollection_Serialization
    {
        public static IEnumerable<object[]> SerializeDeserialize_Roundtrips_MemberData()
        {
            yield return new object[] { new ObservableCollection<int>() };
            yield return new object[] { new ObservableCollection<int>() { 42 } };
            yield return new object[] { new ObservableCollection<int>() { 1, 5, 3, 4, 2 } };
        }

        [Theory]
        [MemberData(nameof(SerializeDeserialize_Roundtrips_MemberData))]
        public void SerializeDeserialize_Roundtrips(ObservableCollection<int> c)
        {
            ObservableCollection<int> clone = BinaryFormatterHelpers.Clone(c);
            Assert.NotSame(c, clone);
            Assert.Equal(c, clone);
        }

        [Fact]
        public void OnDeserialized_MonitorNotInitialized_ExpectSuccess()
        {
            var observableCollection = new ObservableCollection<int>();
            MethodInfo onDeserializedMethodInfo = observableCollection.GetType().GetMethod("OnDeserialized",
                BindingFlags.Instance | Reflection.BindingFlags.NonPublic);

            Assert.NotNull(onDeserializedMethodInfo);
            onDeserializedMethodInfo.Invoke(observableCollection, new object[] { null });
        }
    }
}
