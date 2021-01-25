// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom.Tests
{
    public class CodeTypeDeclarationCollectionTests : CodeCollectionTestBase<CodeTypeDeclarationCollection, CodeTypeDeclaration>
    {
        protected override CodeTypeDeclarationCollection Ctor() => new CodeTypeDeclarationCollection();
        protected override CodeTypeDeclarationCollection CtorArray(CodeTypeDeclaration[] array) => new CodeTypeDeclarationCollection(array);
        protected override CodeTypeDeclarationCollection CtorCollection(CodeTypeDeclarationCollection collection) => new CodeTypeDeclarationCollection(collection);

        protected override int Count(CodeTypeDeclarationCollection collection) => collection.Count;

        protected override CodeTypeDeclaration GetItem(CodeTypeDeclarationCollection collection, int index) => collection[index];
        protected override void SetItem(CodeTypeDeclarationCollection collection, int index, CodeTypeDeclaration value) => collection[index] = value;

        protected override void AddRange(CodeTypeDeclarationCollection collection, CodeTypeDeclaration[] array) => collection.AddRange(array);
        protected override void AddRange(CodeTypeDeclarationCollection collection, CodeTypeDeclarationCollection value) => collection.AddRange(value);

        protected override object Add(CodeTypeDeclarationCollection collection, CodeTypeDeclaration obj) => collection.Add(obj);

        protected override void Insert(CodeTypeDeclarationCollection collection, int index, CodeTypeDeclaration value) => collection.Insert(index, value);

        protected override void Remove(CodeTypeDeclarationCollection collection, CodeTypeDeclaration value) => collection.Remove(value);

        protected override int IndexOf(CodeTypeDeclarationCollection collection, CodeTypeDeclaration value) => collection.IndexOf(value);
        protected override bool Contains(CodeTypeDeclarationCollection collection, CodeTypeDeclaration value) => collection.Contains(value);

        protected override void CopyTo(CodeTypeDeclarationCollection collection, CodeTypeDeclaration[] array, int index) => collection.CopyTo(array, index);
    }
}
