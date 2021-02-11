// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom.Tests
{
    public class CodeTypeMemberCollectionTests : CodeCollectionTestBase<CodeTypeMemberCollection, CodeTypeMember>
    {
        protected override CodeTypeMemberCollection Ctor() => new CodeTypeMemberCollection();
        protected override CodeTypeMemberCollection CtorArray(CodeTypeMember[] array) => new CodeTypeMemberCollection(array);
        protected override CodeTypeMemberCollection CtorCollection(CodeTypeMemberCollection collection) => new CodeTypeMemberCollection(collection);

        protected override int Count(CodeTypeMemberCollection collection) => collection.Count;

        protected override CodeTypeMember GetItem(CodeTypeMemberCollection collection, int index) => collection[index];
        protected override void SetItem(CodeTypeMemberCollection collection, int index, CodeTypeMember value) => collection[index] = value;

        protected override void AddRange(CodeTypeMemberCollection collection, CodeTypeMember[] array) => collection.AddRange(array);
        protected override void AddRange(CodeTypeMemberCollection collection, CodeTypeMemberCollection value) => collection.AddRange(value);

        protected override object Add(CodeTypeMemberCollection collection, CodeTypeMember obj) => collection.Add(obj);

        protected override void Insert(CodeTypeMemberCollection collection, int index, CodeTypeMember value) => collection.Insert(index, value);

        protected override void Remove(CodeTypeMemberCollection collection, CodeTypeMember value) => collection.Remove(value);

        protected override int IndexOf(CodeTypeMemberCollection collection, CodeTypeMember value) => collection.IndexOf(value);
        protected override bool Contains(CodeTypeMemberCollection collection, CodeTypeMember value) => collection.Contains(value);

        protected override void CopyTo(CodeTypeMemberCollection collection, CodeTypeMember[] array, int index) => collection.CopyTo(array, index);
    }
}
