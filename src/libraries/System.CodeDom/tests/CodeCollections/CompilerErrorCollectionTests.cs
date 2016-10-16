// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CodeDom.Compiler;
using Xunit;

namespace System.CodeDom.Tests
{
    public class CompilerErrorCollectionTests : CodeCollectionTestBase<CompilerErrorCollection, CompilerError>
    {
        public override CompilerErrorCollection Ctor() => new CompilerErrorCollection();
        public override CompilerErrorCollection CtorArray(CompilerError[] array) => new CompilerErrorCollection(array);
        public override CompilerErrorCollection CtorCollection(CompilerErrorCollection collection) => new CompilerErrorCollection(collection);

        public override int Count(CompilerErrorCollection collection) => collection.Count;

        public override CompilerError GetItem(CompilerErrorCollection collection, int index) => collection[index];
        public override void SetItem(CompilerErrorCollection collection, int index, CompilerError value) => collection[index] = value;

        public override void AddRange(CompilerErrorCollection collection, CompilerError[] array) => collection.AddRange(array);
        public override void AddRange(CompilerErrorCollection collection, CompilerErrorCollection value) => collection.AddRange(value);

        public override object Add(CompilerErrorCollection collection, CompilerError obj) => collection.Add(obj);

        public override void Insert(CompilerErrorCollection collection, int index, CompilerError value) => collection.Insert(index, value);

        public override void Remove(CompilerErrorCollection collection, CompilerError value) => collection.Remove(value);

        public override int IndexOf(CompilerErrorCollection collection, CompilerError value) => collection.IndexOf(value);
        public override bool Contains(CompilerErrorCollection collection, CompilerError value) => collection.Contains(value);

        public override void CopyTo(CompilerErrorCollection collection, CompilerError[] array, int index) => collection.CopyTo(array, index);

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
