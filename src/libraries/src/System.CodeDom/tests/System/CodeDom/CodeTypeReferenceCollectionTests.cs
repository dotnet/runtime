// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
    public class CodeTypeReferenceCollectionTests : CodeCollectionTestBase<CodeTypeReferenceCollection, CodeTypeReference>
    {
        protected override CodeTypeReferenceCollection Ctor() => new CodeTypeReferenceCollection();
        protected override CodeTypeReferenceCollection CtorArray(CodeTypeReference[] array) => new CodeTypeReferenceCollection(array);
        protected override CodeTypeReferenceCollection CtorCollection(CodeTypeReferenceCollection collection) => new CodeTypeReferenceCollection(collection);

        protected override int Count(CodeTypeReferenceCollection collection) => collection.Count;

        protected override CodeTypeReference GetItem(CodeTypeReferenceCollection collection, int index) => collection[index];
        protected override void SetItem(CodeTypeReferenceCollection collection, int index, CodeTypeReference value) => collection[index] = value;

        protected override void AddRange(CodeTypeReferenceCollection collection, CodeTypeReference[] array) => collection.AddRange(array);
        protected override void AddRange(CodeTypeReferenceCollection collection, CodeTypeReferenceCollection value) => collection.AddRange(value);

        protected override object Add(CodeTypeReferenceCollection collection, CodeTypeReference obj) => collection.Add(obj);

        protected override void Insert(CodeTypeReferenceCollection collection, int index, CodeTypeReference value) => collection.Insert(index, value);

        protected override void Remove(CodeTypeReferenceCollection collection, CodeTypeReference value) => collection.Remove(value);

        protected override int IndexOf(CodeTypeReferenceCollection collection, CodeTypeReference value) => collection.IndexOf(value);
        protected override bool Contains(CodeTypeReferenceCollection collection, CodeTypeReference value) => collection.Contains(value);

        protected override void CopyTo(CodeTypeReferenceCollection collection, CodeTypeReference[] array, int index) => collection.CopyTo(array, index);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("System.Int32")]
        public void Add_String(string type)
        {
            var collection = new CodeTypeReferenceCollection();
            collection.Add(type);
            Assert.Equal(new CodeTypeReference(type).BaseType, collection[0].BaseType);
        }

        [Theory]
        [InlineData(typeof(int))]
        public void Add_Type(Type type)
        {
            var collection = new CodeTypeReferenceCollection();
            collection.Add(type);
            Assert.Equal(new CodeTypeReference(type).BaseType, collection[0].BaseType);
        }

        [Fact]
        public void Add_NullType_ThrowsArgumentNullException()
        {
            var collection = new CodeTypeReferenceCollection();
            AssertExtensions.Throws<ArgumentNullException>("type", () => collection.Add((Type)null));
        }
    }
}
