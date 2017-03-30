// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeTypeMemberCollectionTests : CodeCollectionTestBase<CodeTypeMemberCollection, CodeTypeMember>
    {
        public override CodeTypeMemberCollection Ctor() => new CodeTypeMemberCollection();
        public override CodeTypeMemberCollection CtorArray(CodeTypeMember[] array) => new CodeTypeMemberCollection(array);
        public override CodeTypeMemberCollection CtorCollection(CodeTypeMemberCollection collection) => new CodeTypeMemberCollection(collection);

        public override int Count(CodeTypeMemberCollection collection) => collection.Count;

        public override CodeTypeMember GetItem(CodeTypeMemberCollection collection, int index) => collection[index];
        public override void SetItem(CodeTypeMemberCollection collection, int index, CodeTypeMember value) => collection[index] = value;

        public override void AddRange(CodeTypeMemberCollection collection, CodeTypeMember[] array) => collection.AddRange(array);
        public override void AddRange(CodeTypeMemberCollection collection, CodeTypeMemberCollection value) => collection.AddRange(value);

        public override object Add(CodeTypeMemberCollection collection, CodeTypeMember obj) => collection.Add(obj);

        public override void Insert(CodeTypeMemberCollection collection, int index, CodeTypeMember value) => collection.Insert(index, value);

        public override void Remove(CodeTypeMemberCollection collection, CodeTypeMember value) => collection.Remove(value);

        public override int IndexOf(CodeTypeMemberCollection collection, CodeTypeMember value) => collection.IndexOf(value);
        public override bool Contains(CodeTypeMemberCollection collection, CodeTypeMember value) => collection.Contains(value);

        public override void CopyTo(CodeTypeMemberCollection collection, CodeTypeMember[] array, int index) => collection.CopyTo(array, index);
    }
}
