// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
    public class CodeTypeParameterCollectionTests : CodeCollectionTestBase<CodeTypeParameterCollection, CodeTypeParameter>
    {
        public override CodeTypeParameterCollection Ctor() => new CodeTypeParameterCollection();
        public override CodeTypeParameterCollection CtorArray(CodeTypeParameter[] array) => new CodeTypeParameterCollection(array);
        public override CodeTypeParameterCollection CtorCollection(CodeTypeParameterCollection collection) => new CodeTypeParameterCollection(collection);

        public override int Count(CodeTypeParameterCollection collection) => collection.Count;

        public override CodeTypeParameter GetItem(CodeTypeParameterCollection collection, int index) => collection[index];
        public override void SetItem(CodeTypeParameterCollection collection, int index, CodeTypeParameter value) => collection[index] = value;

        public override void AddRange(CodeTypeParameterCollection collection, CodeTypeParameter[] array) => collection.AddRange(array);
        public override void AddRange(CodeTypeParameterCollection collection, CodeTypeParameterCollection value) => collection.AddRange(value);

        public override object Add(CodeTypeParameterCollection collection, CodeTypeParameter obj) => collection.Add(obj);

        public override void Insert(CodeTypeParameterCollection collection, int index, CodeTypeParameter value) => collection.Insert(index, value);

        public override void Remove(CodeTypeParameterCollection collection, CodeTypeParameter value) => collection.Remove(value);

        public override int IndexOf(CodeTypeParameterCollection collection, CodeTypeParameter value) => collection.IndexOf(value);
        public override bool Contains(CodeTypeParameterCollection collection, CodeTypeParameter value) => collection.Contains(value);

        public override void CopyTo(CodeTypeParameterCollection collection, CodeTypeParameter[] array, int index) => collection.CopyTo(array, index);

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
