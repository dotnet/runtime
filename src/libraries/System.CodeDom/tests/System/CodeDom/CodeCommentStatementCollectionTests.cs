// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom.Tests
{
    public class CodeCommentStatementCollectionTests : CodeCollectionTestBase<CodeCommentStatementCollection, CodeCommentStatement>
    {
        protected override CodeCommentStatementCollection Ctor() => new CodeCommentStatementCollection();
        protected override CodeCommentStatementCollection CtorArray(CodeCommentStatement[] array) => new CodeCommentStatementCollection(array);
        protected override CodeCommentStatementCollection CtorCollection(CodeCommentStatementCollection collection) => new CodeCommentStatementCollection(collection);

        protected override int Count(CodeCommentStatementCollection collection) => collection.Count;

        protected override CodeCommentStatement GetItem(CodeCommentStatementCollection collection, int index) => collection[index];
        protected override void SetItem(CodeCommentStatementCollection collection, int index, CodeCommentStatement value) => collection[index] = value;

        protected override void AddRange(CodeCommentStatementCollection collection, CodeCommentStatement[] array) => collection.AddRange(array);
        protected override void AddRange(CodeCommentStatementCollection collection, CodeCommentStatementCollection value) => collection.AddRange(value);

        protected override object Add(CodeCommentStatementCollection collection, CodeCommentStatement obj) => collection.Add(obj);

        protected override void Insert(CodeCommentStatementCollection collection, int index, CodeCommentStatement value) => collection.Insert(index, value);

        protected override void Remove(CodeCommentStatementCollection collection, CodeCommentStatement value) => collection.Remove(value);

        protected override int IndexOf(CodeCommentStatementCollection collection, CodeCommentStatement value) => collection.IndexOf(value);
        protected override bool Contains(CodeCommentStatementCollection collection, CodeCommentStatement value) => collection.Contains(value);

        protected override void CopyTo(CodeCommentStatementCollection collection, CodeCommentStatement[] array, int index) => collection.CopyTo(array, index);
    }
}
