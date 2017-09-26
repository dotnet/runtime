// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Schema.Tests
{
    public class DatabaseGeneratedAttributeTests
    {
        [Theory]
        [InlineData(DatabaseGeneratedOption.None)]
        [InlineData(DatabaseGeneratedOption.Identity)]
        [InlineData(DatabaseGeneratedOption.Computed)]
        public static void Ctor_DatabaseGeneratedOption(DatabaseGeneratedOption databaseGeneratedOption)
        {
            DatabaseGeneratedAttribute attribute = new DatabaseGeneratedAttribute(databaseGeneratedOption);
            Assert.Equal(databaseGeneratedOption, attribute.DatabaseGeneratedOption);
        }

        [Theory]
        [InlineData((DatabaseGeneratedOption)(-1))]
        [InlineData((DatabaseGeneratedOption)10)]
        public static void Ctor_DatabaseGeneratedOption_UndefinedOption_ThrowsArgumentOutOfRangeException(DatabaseGeneratedOption databaseGeneratedOption)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("databaseGeneratedOption", () => new DatabaseGeneratedAttribute(databaseGeneratedOption));
        }
    }
}
