// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeCatchClauseCollectionTests : CodeCollectionTestBase<CodeCatchClauseCollection, CodeCatchClause>
    {
        protected override CodeCatchClauseCollection Ctor() => new CodeCatchClauseCollection();
        protected override CodeCatchClauseCollection CtorArray(CodeCatchClause[] array) => new CodeCatchClauseCollection(array);
        protected override CodeCatchClauseCollection CtorCollection(CodeCatchClauseCollection collection) => new CodeCatchClauseCollection(collection);

        protected override int Count(CodeCatchClauseCollection collection) => collection.Count;

        protected override CodeCatchClause GetItem(CodeCatchClauseCollection collection, int index) => collection[index];
        protected override void SetItem(CodeCatchClauseCollection collection, int index, CodeCatchClause value) => collection[index] = value;

        protected override void AddRange(CodeCatchClauseCollection collection, CodeCatchClause[] array) => collection.AddRange(array);
        protected override void AddRange(CodeCatchClauseCollection collection, CodeCatchClauseCollection value) => collection.AddRange(value);

        protected override object Add(CodeCatchClauseCollection collection, CodeCatchClause obj) => collection.Add(obj);

        protected override void Insert(CodeCatchClauseCollection collection, int index, CodeCatchClause value) => collection.Insert(index, value);

        protected override void Remove(CodeCatchClauseCollection collection, CodeCatchClause value) => collection.Remove(value);

        protected override int IndexOf(CodeCatchClauseCollection collection, CodeCatchClause value) => collection.IndexOf(value);
        protected override bool Contains(CodeCatchClauseCollection collection, CodeCatchClause value) => collection.Contains(value);

        protected override void CopyTo(CodeCatchClauseCollection collection, CodeCatchClause[] array, int index) => collection.CopyTo(array, index);
    }
}
