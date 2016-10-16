// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeAttributeDeclarationCollectionTests : CodeCollectionTestBase<CodeAttributeDeclarationCollection, CodeAttributeDeclaration>
    {
        public override CodeAttributeDeclarationCollection Ctor() => new CodeAttributeDeclarationCollection();
        public override CodeAttributeDeclarationCollection CtorArray(CodeAttributeDeclaration[] array) => new CodeAttributeDeclarationCollection(array);
        public override CodeAttributeDeclarationCollection CtorCollection(CodeAttributeDeclarationCollection collection) => new CodeAttributeDeclarationCollection(collection);

        public override int Count(CodeAttributeDeclarationCollection collection) => collection.Count;

        public override CodeAttributeDeclaration GetItem(CodeAttributeDeclarationCollection collection, int index) => collection[index];
        public override void SetItem(CodeAttributeDeclarationCollection collection, int index, CodeAttributeDeclaration value) => collection[index] = value;

        public override void AddRange(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration[] array) => collection.AddRange(array);
        public override void AddRange(CodeAttributeDeclarationCollection collection, CodeAttributeDeclarationCollection value) => collection.AddRange(value);

        public override object Add(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration obj) => collection.Add(obj);

        public override void Insert(CodeAttributeDeclarationCollection collection, int index, CodeAttributeDeclaration value) => collection.Insert(index, value);

        public override void Remove(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration value) => collection.Remove(value);

        public override int IndexOf(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration value) => collection.IndexOf(value);
        public override bool Contains(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration value) => collection.Contains(value);

        public override void CopyTo(CodeAttributeDeclarationCollection collection, CodeAttributeDeclaration[] array, int index) => collection.CopyTo(array, index);
    }
}
