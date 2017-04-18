// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
    public class CodeTypeReferenceCollectionTests : CodeCollectionTestBase<CodeTypeReferenceCollection, CodeTypeReference>
    {
        public override CodeTypeReferenceCollection Ctor() => new CodeTypeReferenceCollection();
        public override CodeTypeReferenceCollection CtorArray(CodeTypeReference[] array) => new CodeTypeReferenceCollection(array);
        public override CodeTypeReferenceCollection CtorCollection(CodeTypeReferenceCollection collection) => new CodeTypeReferenceCollection(collection);

        public override int Count(CodeTypeReferenceCollection collection) => collection.Count;

        public override CodeTypeReference GetItem(CodeTypeReferenceCollection collection, int index) => collection[index];
        public override void SetItem(CodeTypeReferenceCollection collection, int index, CodeTypeReference value) => collection[index] = value;

        public override void AddRange(CodeTypeReferenceCollection collection, CodeTypeReference[] array) => collection.AddRange(array);
        public override void AddRange(CodeTypeReferenceCollection collection, CodeTypeReferenceCollection value) => collection.AddRange(value);

        public override object Add(CodeTypeReferenceCollection collection, CodeTypeReference obj) => collection.Add(obj);

        public override void Insert(CodeTypeReferenceCollection collection, int index, CodeTypeReference value) => collection.Insert(index, value);

        public override void Remove(CodeTypeReferenceCollection collection, CodeTypeReference value) => collection.Remove(value);

        public override int IndexOf(CodeTypeReferenceCollection collection, CodeTypeReference value) => collection.IndexOf(value);
        public override bool Contains(CodeTypeReferenceCollection collection, CodeTypeReference value) => collection.Contains(value);

        public override void CopyTo(CodeTypeReferenceCollection collection, CodeTypeReference[] array, int index) => collection.CopyTo(array, index);

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
