// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeNamespaceCollectionTests : CodeCollectionTestBase<CodeNamespaceCollection, CodeNamespace>
    {
        protected override CodeNamespaceCollection Ctor() => new CodeNamespaceCollection();
        protected override CodeNamespaceCollection CtorArray(CodeNamespace[] array) => new CodeNamespaceCollection(array);
        protected override CodeNamespaceCollection CtorCollection(CodeNamespaceCollection collection) => new CodeNamespaceCollection(collection);

        protected override int Count(CodeNamespaceCollection collection) => collection.Count;

        protected override CodeNamespace GetItem(CodeNamespaceCollection collection, int index) => collection[index];
        protected override void SetItem(CodeNamespaceCollection collection, int index, CodeNamespace value) => collection[index] = value;

        protected override void AddRange(CodeNamespaceCollection collection, CodeNamespace[] array) => collection.AddRange(array);
        protected override void AddRange(CodeNamespaceCollection collection, CodeNamespaceCollection value) => collection.AddRange(value);

        protected override object Add(CodeNamespaceCollection collection, CodeNamespace obj) => collection.Add(obj);

        protected override void Insert(CodeNamespaceCollection collection, int index, CodeNamespace value) => collection.Insert(index, value);

        protected override void Remove(CodeNamespaceCollection collection, CodeNamespace value) => collection.Remove(value);

        protected override int IndexOf(CodeNamespaceCollection collection, CodeNamespace value) => collection.IndexOf(value);
        protected override bool Contains(CodeNamespaceCollection collection, CodeNamespace value) => collection.Contains(value);

        protected override void CopyTo(CodeNamespaceCollection collection, CodeNamespace[] array, int index) => collection.CopyTo(array, index);
    }
}
