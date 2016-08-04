// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderFieldType
    {
        private static TypeBuilder s_type = Helpers.DynamicType(TypeAttributes.Abstract);

        public static IEnumerable<object[]> FieldType_TestData()
        {
            yield return new object[] { typeof(object), FieldAttributes.Public };
            yield return new object[] { typeof(int), FieldAttributes.Public };
            yield return new object[] { typeof(string), FieldAttributes.Public };
            yield return new object[] { typeof(FieldBuilderFieldType), FieldAttributes.Public };
            yield return new object[] { s_type.AsType(), FieldAttributes.Public };
        }

        [Theory]
        [MemberData(nameof(FieldType_TestData))]
        public void FieldType(Type type, FieldAttributes attributes)
        {
            FieldBuilder field = s_type.DefineField(type.ToString(), type, attributes);
            Assert.Equal(type, field.FieldType);
        }
    }
}
