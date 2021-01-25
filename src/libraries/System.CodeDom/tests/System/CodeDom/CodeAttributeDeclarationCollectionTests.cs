// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom.Tests
{
    public class CodeAttributeDeclarationCollectionTests : CodeCollectionTestBase<CodeAttributeDeclarationCollection, CodeAttributeDeclaration>
    {
        protected override CodeAttributeDeclarationCollection Ctor() => new CodeAttributeDeclarationCollection();
        protected override CodeAttributeDeclarationCollection CtorArray(CodeAttributeDeclaration[] array) => new CodeAttributeDeclarationCollection(array);
        protected override CodeAttributeDeclarationCollection CtorCollection(CodeAttributeDeclarationCollection collection) => new CodeAttributeDeclarationCollection(collection);

        protected override int Count(CodeAttributeDeclarationCollection collection) => collection.Count;

        protected override CodeAttributeDeclaration GetItem(CodeAttributeDeclarationCollection collection, int index) => collection[index];
        protected override void SetItem(CodeAttributeDeclarationCollection collection, int index, CodeAttributeDeclaration value) => collection[index] = value;

        protected override void AddRange(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration[] array) => collection.AddRange(array);
        protected override void AddRange(CodeAttributeDeclarationCollection collection, CodeAttributeDeclarationCollection value) => collection.AddRange(value);

        protected override object Add(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration obj) => collection.Add(obj);

        protected override void Insert(CodeAttributeDeclarationCollection collection, int index, CodeAttributeDeclaration value) => collection.Insert(index, value);

        protected override void Remove(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration value) => collection.Remove(value);

        protected override int IndexOf(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration value) => collection.IndexOf(value);
        protected override bool Contains(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration value) => collection.Contains(value);

        protected override void CopyTo(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration[] array, int index) => collection.CopyTo(array, index);
    }
}
