// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeDirectiveCollectionTests : CodeCollectionTestBase<CodeDirectiveCollection, CodeDirective>
    {
        public override CodeDirectiveCollection Ctor() => new CodeDirectiveCollection();
        public override CodeDirectiveCollection CtorArray(CodeDirective[] array) => new CodeDirectiveCollection(array);
        public override CodeDirectiveCollection CtorCollection(CodeDirectiveCollection collection) => new CodeDirectiveCollection(collection);

        public override int Count(CodeDirectiveCollection collection) => collection.Count;

        public override CodeDirective GetItem(CodeDirectiveCollection collection, int index) => collection[index];
        public override void SetItem(CodeDirectiveCollection collection, int index, CodeDirective value) => collection[index] = value;

        public override void AddRange(CodeDirectiveCollection collection, CodeDirective[] array) => collection.AddRange(array);
        public override void AddRange(CodeDirectiveCollection collection, CodeDirectiveCollection value) => collection.AddRange(value);

        public override object Add(CodeDirectiveCollection collection, CodeDirective obj) => collection.Add(obj);

        public override void Insert(CodeDirectiveCollection collection, int index, CodeDirective value) => collection.Insert(index, value);

        public override void Remove(CodeDirectiveCollection collection, CodeDirective value) => collection.Remove(value);

        public override int IndexOf(CodeDirectiveCollection collection, CodeDirective value) => collection.IndexOf(value);
        public override bool Contains(CodeDirectiveCollection collection, CodeDirective value) => collection.Contains(value);

        public override void CopyTo(CodeDirectiveCollection collection, CodeDirective[] array, int index) => collection.CopyTo(array, index);
    }
}
