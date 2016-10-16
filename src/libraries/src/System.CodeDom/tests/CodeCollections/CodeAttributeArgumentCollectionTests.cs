// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Tests
{
    public class CodeAttributeArgumentCollectionTests : CodeCollectionTestBase<CodeAttributeArgumentCollection, CodeAttributeArgument>
    {
        public override CodeAttributeArgumentCollection Ctor() => new CodeAttributeArgumentCollection();
        public override CodeAttributeArgumentCollection CtorArray(CodeAttributeArgument[] array) => new CodeAttributeArgumentCollection(array);
        public override CodeAttributeArgumentCollection CtorCollection(CodeAttributeArgumentCollection collection) => new CodeAttributeArgumentCollection(collection);

        public override int Count(CodeAttributeArgumentCollection collection) => collection.Count;

        public override CodeAttributeArgument GetItem(CodeAttributeArgumentCollection collection, int index) => collection[index];
        public override void SetItem(CodeAttributeArgumentCollection collection, int index, CodeAttributeArgument value) => collection[index] = value;

        public override void AddRange(CodeAttributeArgumentCollection collection, CodeAttributeArgument[] array) => collection.AddRange(array);
        public override void AddRange(CodeAttributeArgumentCollection collection, CodeAttributeArgumentCollection value) => collection.AddRange(value);

        public override object Add(CodeAttributeArgumentCollection collection, CodeAttributeArgument obj) => collection.Add(obj);

        public override void Insert(CodeAttributeArgumentCollection collection, int index, CodeAttributeArgument value) => collection.Insert(index, value);

        public override void Remove(CodeAttributeArgumentCollection collection, CodeAttributeArgument value) => collection.Remove(value);

        public override int IndexOf(CodeAttributeArgumentCollection collection, CodeAttributeArgument value) => collection.IndexOf(value);
        public override bool Contains(CodeAttributeArgumentCollection collection, CodeAttributeArgument value) => collection.Contains(value);

        public override void CopyTo(CodeAttributeArgumentCollection collection, CodeAttributeArgument[] array, int index) => collection.CopyTo(array, index);
    }
}
