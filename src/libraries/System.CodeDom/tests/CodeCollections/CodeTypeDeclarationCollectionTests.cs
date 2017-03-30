// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeTypeDeclarationCollectionTests : CodeCollectionTestBase<CodeTypeDeclarationCollection, CodeTypeDeclaration>
    {
        public override CodeTypeDeclarationCollection Ctor() => new CodeTypeDeclarationCollection();
        public override CodeTypeDeclarationCollection CtorArray(CodeTypeDeclaration[] array) => new CodeTypeDeclarationCollection(array);
        public override CodeTypeDeclarationCollection CtorCollection(CodeTypeDeclarationCollection collection) => new CodeTypeDeclarationCollection(collection);

        public override int Count(CodeTypeDeclarationCollection collection) => collection.Count;

        public override CodeTypeDeclaration GetItem(CodeTypeDeclarationCollection collection, int index) => collection[index];
        public override void SetItem(CodeTypeDeclarationCollection collection, int index, CodeTypeDeclaration value) => collection[index] = value;

        public override void AddRange(CodeTypeDeclarationCollection collection, CodeTypeDeclaration[] array) => collection.AddRange(array);
        public override void AddRange(CodeTypeDeclarationCollection collection, CodeTypeDeclarationCollection value) => collection.AddRange(value);

        public override object Add(CodeTypeDeclarationCollection collection, CodeTypeDeclaration obj) => collection.Add(obj);

        public override void Insert(CodeTypeDeclarationCollection collection, int index, CodeTypeDeclaration value) => collection.Insert(index, value);

        public override void Remove(CodeTypeDeclarationCollection collection, CodeTypeDeclaration value) => collection.Remove(value);

        public override int IndexOf(CodeTypeDeclarationCollection collection, CodeTypeDeclaration value) => collection.IndexOf(value);
        public override bool Contains(CodeTypeDeclarationCollection collection, CodeTypeDeclaration value) => collection.Contains(value);

        public override void CopyTo(CodeTypeDeclarationCollection collection, CodeTypeDeclaration[] array, int index) => collection.CopyTo(array, index);
    }
}
