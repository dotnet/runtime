// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
    public class CodeStatementCollectionTests : CodeCollectionTestBase<CodeStatementCollection, CodeStatement>
    {
        protected override CodeStatementCollection Ctor() => new CodeStatementCollection();
        protected override CodeStatementCollection CtorArray(CodeStatement[] array) => new CodeStatementCollection(array);
        protected override CodeStatementCollection CtorCollection(CodeStatementCollection collection) => new CodeStatementCollection(collection);

        protected override int Count(CodeStatementCollection collection) => collection.Count;

        protected override CodeStatement GetItem(CodeStatementCollection collection, int index) => collection[index];
        protected override void SetItem(CodeStatementCollection collection, int index, CodeStatement value) => collection[index] = value;

        protected override void AddRange(CodeStatementCollection collection, CodeStatement[] array) => collection.AddRange(array);
        protected override void AddRange(CodeStatementCollection collection, CodeStatementCollection value) => collection.AddRange(value);

        protected override object Add(CodeStatementCollection collection, CodeStatement obj) => collection.Add(obj);

        protected override void Insert(CodeStatementCollection collection, int index, CodeStatement value) => collection.Insert(index, value);

        protected override void Remove(CodeStatementCollection collection, CodeStatement value) => collection.Remove(value);

        protected override int IndexOf(CodeStatementCollection collection, CodeStatement value) => collection.IndexOf(value);
        protected override bool Contains(CodeStatementCollection collection, CodeStatement value) => collection.Contains(value);

        protected override void CopyTo(CodeStatementCollection collection, CodeStatement[] array, int index) => collection.CopyTo(array, index);

        public static IEnumerable<object[]> Add_CodeExpression_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CodePrimitiveExpression("Value") };
        }

        [Theory]
        [MemberData(nameof(Add_CodeExpression_TestData))]
        public void Add_CodeExpression(CodeExpression expression)
        {
            var collection = new CodeStatementCollection();
            Assert.Equal(0, collection.Add(expression));
            Assert.Equal(expression, ((CodeExpressionStatement)collection[0]).Expression);
        }
    }
}
