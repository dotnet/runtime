// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeParameterDeclarationExpressionCollectionTests : CodeCollectionTestBase<CodeParameterDeclarationExpressionCollection, CodeParameterDeclarationExpression>
    {
        public override CodeParameterDeclarationExpressionCollection Ctor() => new CodeParameterDeclarationExpressionCollection();
        public override CodeParameterDeclarationExpressionCollection CtorArray(CodeParameterDeclarationExpression[] array) => new CodeParameterDeclarationExpressionCollection(array);
        public override CodeParameterDeclarationExpressionCollection CtorCollection(CodeParameterDeclarationExpressionCollection collection) => new CodeParameterDeclarationExpressionCollection(collection);

        public override int Count(CodeParameterDeclarationExpressionCollection collection) => collection.Count;

        public override CodeParameterDeclarationExpression GetItem(CodeParameterDeclarationExpressionCollection collection, int index) => collection[index];
        public override void SetItem(CodeParameterDeclarationExpressionCollection collection, int index, CodeParameterDeclarationExpression value) => collection[index] = value;

        public override void AddRange(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression[] array) => collection.AddRange(array);
        public override void AddRange(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpressionCollection value) => collection.AddRange(value);

        public override object Add(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression obj) => collection.Add(obj);

        public override void Insert(CodeParameterDeclarationExpressionCollection collection, int index, CodeParameterDeclarationExpression value) => collection.Insert(index, value);

        public override void Remove(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression value) => collection.Remove(value);

        public override int IndexOf(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression value) => collection.IndexOf(value);
        public override bool Contains(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression value) => collection.Contains(value);

        public override void CopyTo(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression[] array, int index) => collection.CopyTo(array, index);
    }
}
