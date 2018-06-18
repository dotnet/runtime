// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeCatchClauseCollectionTests : CodeCollectionTestBase<CodeCatchClauseCollection, CodeCatchClause>
    {
        public override CodeCatchClauseCollection Ctor() => new CodeCatchClauseCollection();
        public override CodeCatchClauseCollection CtorArray(CodeCatchClause[] array) => new CodeCatchClauseCollection(array);
        public override CodeCatchClauseCollection CtorCollection(CodeCatchClauseCollection collection) => new CodeCatchClauseCollection(collection);

        public override int Count(CodeCatchClauseCollection collection) => collection.Count;

        public override CodeCatchClause GetItem(CodeCatchClauseCollection collection, int index) => collection[index];
        public override void SetItem(CodeCatchClauseCollection collection, int index, CodeCatchClause value) => collection[index] = value;

        public override void AddRange(CodeCatchClauseCollection collection, CodeCatchClause[] array) => collection.AddRange(array);
        public override void AddRange(CodeCatchClauseCollection collection, CodeCatchClauseCollection value) => collection.AddRange(value);

        public override object Add(CodeCatchClauseCollection collection, CodeCatchClause obj) => collection.Add(obj);

        public override void Insert(CodeCatchClauseCollection collection, int index, CodeCatchClause value) => collection.Insert(index, value);

        public override void Remove(CodeCatchClauseCollection collection, CodeCatchClause value) => collection.Remove(value);

        public override int IndexOf(CodeCatchClauseCollection collection, CodeCatchClause value) => collection.IndexOf(value);
        public override bool Contains(CodeCatchClauseCollection collection, CodeCatchClause value) => collection.Contains(value);

        public override void CopyTo(CodeCatchClauseCollection collection, CodeCatchClause[] array, int index) => collection.CopyTo(array, index);
    }
}
