// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Tests;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CompilerErrorCollectionTests : CodeCollectionTestBase<CompilerErrorCollection, CompilerError>
    {
        protected override CompilerErrorCollection Ctor() => new CompilerErrorCollection();
        protected override CompilerErrorCollection CtorArray(CompilerError[] array) => new CompilerErrorCollection(array);
        protected override CompilerErrorCollection CtorCollection(CompilerErrorCollection collection) => new CompilerErrorCollection(collection);

        protected override int Count(CompilerErrorCollection collection) => collection.Count;

        protected override CompilerError GetItem(CompilerErrorCollection collection, int index) => collection[index];
        protected override void SetItem(CompilerErrorCollection collection, int index, CompilerError value) => collection[index] = value;

        protected override void AddRange(CompilerErrorCollection collection, CompilerError[] array) => collection.AddRange(array);
        protected override void AddRange(CompilerErrorCollection collection, CompilerErrorCollection value) => collection.AddRange(value);

        protected override object Add(CompilerErrorCollection collection, CompilerError obj) => collection.Add(obj);

        protected override void Insert(CompilerErrorCollection collection, int index, CompilerError value) => collection.Insert(index, value);

        protected override void Remove(CompilerErrorCollection collection, CompilerError value) => collection.Remove(value);

        protected override int IndexOf(CompilerErrorCollection collection, CompilerError value) => collection.IndexOf(value);
        protected override bool Contains(CompilerErrorCollection collection, CompilerError value) => collection.Contains(value);

        protected override void CopyTo(CompilerErrorCollection collection, CompilerError[] array, int index) => collection.CopyTo(array, index);

        [Fact]
        public void HasWarnings_Empty_ReturnsFalse()
        {
            var collection = new CompilerErrorCollection();
            Assert.False(collection.HasWarnings);
        }

        [Fact]
        public void HasWarnings_OnlyErrors_ReturnsFalse()
        {
            var collection = new CompilerErrorCollection();
            collection.Add(new CompilerError() { IsWarning = false });
            Assert.False(collection.HasWarnings);
        }

        [Fact]
        public void HasWarnings_OnlyWarnings_ReturnsTrue()
        {
            var collection = new CompilerErrorCollection();
            collection.Add(new CompilerError() { IsWarning = true });
            Assert.True(collection.HasWarnings);
        }

        [Fact]
        public void HasWarnings_WarningsAndErrors_ReturnsTrue()
        {
            var collection = new CompilerErrorCollection();
            collection.Add(new CompilerError() { IsWarning = false });
            collection.Add(new CompilerError() { IsWarning = true });
            Assert.True(collection.HasWarnings);
        }

        [Fact]
        public void HasErrors_Empty_ReturnsFalse()
        {
            var collection = new CompilerErrorCollection();
            Assert.False(collection.HasErrors);
        }

        [Fact]
        public void HasErrors_OnlyErrors_ReturnsTrue()
        {
            var collection = new CompilerErrorCollection();
            collection.Add(new CompilerError() { IsWarning = false });
            Assert.True(collection.HasErrors);
        }

        [Fact]
        public void HasErrors_OnlyWarnings_ReturnsFalse()
        {
            var collection = new CompilerErrorCollection();
            collection.Add(new CompilerError() { IsWarning = true });
            Assert.False(collection.HasErrors);
        }

        [Fact]
        public void HasErrors_WarningsAndErrors_ReturnsTrue()
        {
            var collection = new CompilerErrorCollection();
            collection.Add(new CompilerError() { IsWarning = false });
            collection.Add(new CompilerError() { IsWarning = true });
            Assert.True(collection.HasErrors);
        }
    }
}
