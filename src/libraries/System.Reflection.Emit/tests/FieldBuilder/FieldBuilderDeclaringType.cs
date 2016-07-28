// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderDeclaringType
    {
        private static ModuleBuilder s_module = Helpers.DynamicModule();
        private static TypeBuilder s_type1 = s_module.DefineType("Type1", TypeAttributes.Abstract);
        private static TypeBuilder s_type2 = s_module.DefineType("Type2", TypeAttributes.Abstract);

        public static IEnumerable<object[]> DeclaringType_TestData()
        {
            yield return new object[] { s_type1, typeof(object), FieldAttributes.Public };
            yield return new object[] { s_type1, typeof(int), FieldAttributes.Public };
            yield return new object[] { s_type1, typeof(string), FieldAttributes.Public };
            yield return new object[] { s_type1, typeof(FieldBuilderDeclaringType), FieldAttributes.Public };
            yield return new object[] { s_type1, s_type1.AsType(), FieldAttributes.Public };

            yield return new object[] { s_type2, typeof(object), FieldAttributes.Public };
            yield return new object[] { s_type2, typeof(int), FieldAttributes.Public };
            yield return new object[] { s_type2, typeof(string), FieldAttributes.Public };
            yield return new object[] { s_type2, typeof(FieldBuilderDeclaringType), FieldAttributes.Public };
            yield return new object[] { s_type2, s_type2.AsType(), FieldAttributes.Public };
        }

        [Theory]
        [MemberData(nameof(DeclaringType_TestData))]
        public void DeclaringType(TypeBuilder type, Type fieldType, FieldAttributes attributes)
        {
            FieldBuilder field = type.DefineField(fieldType.ToString(), fieldType, attributes);
            Assert.Equal(type.AsType(), field.DeclaringType);
        }
    }
}
