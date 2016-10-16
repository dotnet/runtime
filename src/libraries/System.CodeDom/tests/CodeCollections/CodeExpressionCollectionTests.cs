// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeExpressionCollectionTests : CodeCollectionTestBase<CodeExpressionCollection, CodeExpression>
    {
        public override CodeExpressionCollection Ctor() => new CodeExpressionCollection();
        public override CodeExpressionCollection CtorArray(CodeExpression[] array) => new CodeExpressionCollection(array);
        public override CodeExpressionCollection CtorCollection(CodeExpressionCollection collection) => new CodeExpressionCollection(collection);

        public override int Count(CodeExpressionCollection collection) => collection.Count;

        public override CodeExpression GetItem(CodeExpressionCollection collection, int index) => collection[index];
        public override void SetItem(CodeExpressionCollection collection, int index, CodeExpression value) => collection[index] = value;

        public override void AddRange(CodeExpressionCollection collection, CodeExpression[] array) => collection.AddRange(array);
        public override void AddRange(CodeExpressionCollection collection, CodeExpressionCollection value) => collection.AddRange(value);

        public override object Add(CodeExpressionCollection collection, CodeExpression obj) => collection.Add(obj);

        public override void Insert(CodeExpressionCollection collection, int index, CodeExpression value) => collection.Insert(index, value);

        public override void Remove(CodeExpressionCollection collection, CodeExpression value) => collection.Remove(value);

        public override int IndexOf(CodeExpressionCollection collection, CodeExpression value) => collection.IndexOf(value);
        public override bool Contains(CodeExpressionCollection collection, CodeExpression value) => collection.Contains(value);

        public override void CopyTo(CodeExpressionCollection collection, CodeExpression[] array, int index) => collection.CopyTo(array, index);
    }
}
