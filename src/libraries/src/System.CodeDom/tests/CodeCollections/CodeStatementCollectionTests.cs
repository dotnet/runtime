// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
    public class CodeStatementCollectionTests : CodeCollectionTestBase<CodeStatementCollection, CodeStatement>
    {
        public override CodeStatementCollection Ctor() => new CodeStatementCollection();
        public override CodeStatementCollection CtorArray(CodeStatement[] array) => new CodeStatementCollection(array);
        public override CodeStatementCollection CtorCollection(CodeStatementCollection collection) => new CodeStatementCollection(collection);

        public override int Count(CodeStatementCollection collection) => collection.Count;

        public override CodeStatement GetItem(CodeStatementCollection collection, int index) => collection[index];
        public override void SetItem(CodeStatementCollection collection, int index, CodeStatement value) => collection[index] = value;
        
        public override void AddRange(CodeStatementCollection collection, CodeStatement[] array) => collection.AddRange(array);
        public override void AddRange(CodeStatementCollection collection, CodeStatementCollection value) => collection.AddRange(value);

        public override object Add(CodeStatementCollection collection, CodeStatement obj) => collection.Add(obj);

        public override void Insert(CodeStatementCollection collection, int index, CodeStatement value) => collection.Insert(index, value);

        public override void Remove(CodeStatementCollection collection, CodeStatement value) => collection.Remove(value);

        public override int IndexOf(CodeStatementCollection collection, CodeStatement value) => collection.IndexOf(value);
        public override bool Contains(CodeStatementCollection collection, CodeStatement value) => collection.Contains(value);

        public override void CopyTo(CodeStatementCollection collection, CodeStatement[] array, int index) => collection.CopyTo(array, index);

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
