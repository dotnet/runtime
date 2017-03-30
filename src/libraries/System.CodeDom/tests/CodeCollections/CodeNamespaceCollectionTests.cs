// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeNamespaceCollectionTests : CodeCollectionTestBase<CodeNamespaceCollection, CodeNamespace>
    {
        public override CodeNamespaceCollection Ctor() => new CodeNamespaceCollection();
        public override CodeNamespaceCollection CtorArray(CodeNamespace[] array) => new CodeNamespaceCollection(array);
        public override CodeNamespaceCollection CtorCollection(CodeNamespaceCollection collection) => new CodeNamespaceCollection(collection);

        public override int Count(CodeNamespaceCollection collection) => collection.Count;

        public override CodeNamespace GetItem(CodeNamespaceCollection collection, int index) => collection[index];
        public override void SetItem(CodeNamespaceCollection collection, int index, CodeNamespace value) => collection[index] = value;

        public override void AddRange(CodeNamespaceCollection collection, CodeNamespace[] array) => collection.AddRange(array);
        public override void AddRange(CodeNamespaceCollection collection, CodeNamespaceCollection value) => collection.AddRange(value);

        public override object Add(CodeNamespaceCollection collection, CodeNamespace obj) => collection.Add(obj);

        public override void Insert(CodeNamespaceCollection collection, int index, CodeNamespace value) => collection.Insert(index, value);

        public override void Remove(CodeNamespaceCollection collection, CodeNamespace value) => collection.Remove(value);

        public override int IndexOf(CodeNamespaceCollection collection, CodeNamespace value) => collection.IndexOf(value);
        public override bool Contains(CodeNamespaceCollection collection, CodeNamespace value) => collection.Contains(value);

        public override void CopyTo(CodeNamespaceCollection collection, CodeNamespace[] array, int index) => collection.CopyTo(array, index);
    }
}
