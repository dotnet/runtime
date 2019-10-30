// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderGetValue
    {
        [Fact]
        public void GetValue_ThrowsNotSupportedException()
        {
            FieldBuilder field = Helpers.DynamicType(TypeAttributes.Abstract).DefineField("TestField", typeof(int), FieldAttributes.Public);
            Assert.Throws<NotSupportedException>(() => field.GetValue(null));
        }
    }
}
