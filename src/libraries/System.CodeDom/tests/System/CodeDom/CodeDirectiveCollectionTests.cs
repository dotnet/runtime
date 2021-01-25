// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom.Tests
{
    public class CodeDirectiveCollectionTests : CodeCollectionTestBase<CodeDirectiveCollection, CodeDirective>
    {
        protected override CodeDirectiveCollection Ctor() => new CodeDirectiveCollection();
        protected override CodeDirectiveCollection CtorArray(CodeDirective[] array) => new CodeDirectiveCollection(array);
        protected override CodeDirectiveCollection CtorCollection(CodeDirectiveCollection collection) => new CodeDirectiveCollection(collection);

        protected override int Count(CodeDirectiveCollection collection) => collection.Count;

        protected override CodeDirective GetItem(CodeDirectiveCollection collection, int index) => collection[index];
        protected override void SetItem(CodeDirectiveCollection collection, int index, CodeDirective value) => collection[index] = value;

        protected override void AddRange(CodeDirectiveCollection collection, CodeDirective[] array) => collection.AddRange(array);
        protected override void AddRange(CodeDirectiveCollection collection, CodeDirectiveCollection value) => collection.AddRange(value);

        protected override object Add(CodeDirectiveCollection collection, CodeDirective obj) => collection.Add(obj);

        protected override void Insert(CodeDirectiveCollection collection, int index, CodeDirective value) => collection.Insert(index, value);

        protected override void Remove(CodeDirectiveCollection collection, CodeDirective value) => collection.Remove(value);

        protected override int IndexOf(CodeDirectiveCollection collection, CodeDirective value) => collection.IndexOf(value);
        protected override bool Contains(CodeDirectiveCollection collection, CodeDirective value) => collection.Contains(value);

        protected override void CopyTo(CodeDirectiveCollection collection, CodeDirective[] array, int index) => collection.CopyTo(array, index);
    }
}
