// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom.Tests
{
    public class CodeParameterDeclarationExpressionCollectionTests : CodeCollectionTestBase<CodeParameterDeclarationExpressionCollection, CodeParameterDeclarationExpression>
    {
        protected override CodeParameterDeclarationExpressionCollection Ctor() => new CodeParameterDeclarationExpressionCollection();
        protected override CodeParameterDeclarationExpressionCollection CtorArray(CodeParameterDeclarationExpression[] array) => new CodeParameterDeclarationExpressionCollection(array);
        protected override CodeParameterDeclarationExpressionCollection CtorCollection(CodeParameterDeclarationExpressionCollection collection) => new CodeParameterDeclarationExpressionCollection(collection);

        protected override int Count(CodeParameterDeclarationExpressionCollection collection) => collection.Count;

        protected override CodeParameterDeclarationExpression GetItem(CodeParameterDeclarationExpressionCollection collection, int index) => collection[index];
        protected override void SetItem(CodeParameterDeclarationExpressionCollection collection, int index, CodeParameterDeclarationExpression value) => collection[index] = value;

        protected override void AddRange(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression[] array) => collection.AddRange(array);
        protected override void AddRange(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpressionCollection value) => collection.AddRange(value);

        protected override object Add(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression obj) => collection.Add(obj);

        protected override void Insert(CodeParameterDeclarationExpressionCollection collection, int index, CodeParameterDeclarationExpression value) => collection.Insert(index, value);

        protected override void Remove(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression value) => collection.Remove(value);

        protected override int IndexOf(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression value) => collection.IndexOf(value);
        protected override bool Contains(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression value) => collection.Contains(value);

        protected override void CopyTo(CodeParameterDeclarationExpressionCollection collection, CodeParameterDeclarationExpression[] array, int index) => collection.CopyTo(array, index);
    }
}
