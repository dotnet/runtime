// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
    public class CodeTypeParameterCollectionTests : CodeCollectionTestBase<CodeTypeParameterCollection, CodeTypeParameter>
    {
        protected override CodeTypeParameterCollection Ctor() => new CodeTypeParameterCollection();
        protected override CodeTypeParameterCollection CtorArray(CodeTypeParameter[] array) => new CodeTypeParameterCollection(array);
        protected override CodeTypeParameterCollection CtorCollection(CodeTypeParameterCollection collection) => new CodeTypeParameterCollection(collection);

        protected override int Count(CodeTypeParameterCollection collection) => collection.Count;

        protected override CodeTypeParameter GetItem(CodeTypeParameterCollection collection, int index) => collection[index];
        protected override void SetItem(CodeTypeParameterCollection collection, int index, CodeTypeParameter value) => collection[index] = value;

        protected override void AddRange(CodeTypeParameterCollection collection, CodeTypeParameter[] array) => collection.AddRange(array);
        protected override void AddRange(CodeTypeParameterCollection collection, CodeTypeParameterCollection value) => collection.AddRange(value);

        protected override object Add(CodeTypeParameterCollection collection, CodeTypeParameter obj) => collection.Add(obj);

        protected override void Insert(CodeTypeParameterCollection collection, int index, CodeTypeParameter value) => collection.Insert(index, value);

        protected override void Remove(CodeTypeParameterCollection collection, CodeTypeParameter value) => collection.Remove(value);

        protected override int IndexOf(CodeTypeParameterCollection collection, CodeTypeParameter value) => collection.IndexOf(value);
        protected override bool Contains(CodeTypeParameterCollection collection, CodeTypeParameter value) => collection.Contains(value);

        protected override void CopyTo(CodeTypeParameterCollection collection, CodeTypeParameter[] array, int index) => collection.CopyTo(array, index);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Name")]
        public void Add_String(string name)
        {
            var collection = new CodeTypeParameterCollection();
            collection.Add(name);
            Assert.Equal(new CodeTypeParameter(name).Name, collection[0].Name);
        }
    }
}
