// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeExpressionCollectionTests : CodeCollectionTestBase<CodeExpressionCollection, CodeExpression>
    {
        protected override CodeExpressionCollection Ctor() => new CodeExpressionCollection();
        protected override CodeExpressionCollection CtorArray(CodeExpression[] array) => new CodeExpressionCollection(array);
        protected override CodeExpressionCollection CtorCollection(CodeExpressionCollection collection) => new CodeExpressionCollection(collection);

        protected override int Count(CodeExpressionCollection collection) => collection.Count;

        protected override CodeExpression GetItem(CodeExpressionCollection collection, int index) => collection[index];
        protected override void SetItem(CodeExpressionCollection collection, int index, CodeExpression value) => collection[index] = value;

        protected override void AddRange(CodeExpressionCollection collection, CodeExpression[] array) => collection.AddRange(array);
        protected override void AddRange(CodeExpressionCollection collection, CodeExpressionCollection value) => collection.AddRange(value);

        protected override object Add(CodeExpressionCollection collection, CodeExpression obj) => collection.Add(obj);

        protected override void Insert(CodeExpressionCollection collection, int index, CodeExpression value) => collection.Insert(index, value);

        protected override void Remove(CodeExpressionCollection collection, CodeExpression value) => collection.Remove(value);

        protected override int IndexOf(CodeExpressionCollection collection, CodeExpression value) => collection.IndexOf(value);
        protected override bool Contains(CodeExpressionCollection collection, CodeExpression value) => collection.Contains(value);

        protected override void CopyTo(CodeExpressionCollection collection, CodeExpression[] array, int index) => collection.CopyTo(array, index);
    }
}
