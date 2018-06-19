// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeCommentStatementCollectionTests : CodeCollectionTestBase<CodeCommentStatementCollection, CodeCommentStatement>
    {
        public override CodeCommentStatementCollection Ctor() => new CodeCommentStatementCollection();
        public override CodeCommentStatementCollection CtorArray(CodeCommentStatement[] array) => new CodeCommentStatementCollection(array);
        public override CodeCommentStatementCollection CtorCollection(CodeCommentStatementCollection collection) => new CodeCommentStatementCollection(collection);

        public override int Count(CodeCommentStatementCollection collection) => collection.Count;

        public override CodeCommentStatement GetItem(CodeCommentStatementCollection collection, int index) => collection[index];
        public override void SetItem(CodeCommentStatementCollection collection, int index, CodeCommentStatement value) => collection[index] = value;

        public override void AddRange(CodeCommentStatementCollection collection, CodeCommentStatement[] array) => collection.AddRange(array);
        public override void AddRange(CodeCommentStatementCollection collection, CodeCommentStatementCollection value) => collection.AddRange(value);

        public override object Add(CodeCommentStatementCollection collection, CodeCommentStatement obj) => collection.Add(obj);

        public override void Insert(CodeCommentStatementCollection collection, int index, CodeCommentStatement value) => collection.Insert(index, value);

        public override void Remove(CodeCommentStatementCollection collection, CodeCommentStatement value) => collection.Remove(value);

        public override int IndexOf(CodeCommentStatementCollection collection, CodeCommentStatement value) => collection.IndexOf(value);
        public override bool Contains(CodeCommentStatementCollection collection, CodeCommentStatement value) => collection.Contains(value);

        public override void CopyTo(CodeCommentStatementCollection collection, CodeCommentStatement[] array, int index) => collection.CopyTo(array, index);
    }
}
