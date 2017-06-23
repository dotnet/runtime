// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.Tests
{
    public class ExportFactoryTests
    {
        [Fact]
        public void CreateExport_ValidCreator_ReturnsExpected()
        {
            var factory = new ExportFactory<int>(ExportCreator);
            Assert.Equal(0, ExportCreatorCalled);
            Assert.Equal(0, ExportActionCalled);

            Export<int> export = factory.CreateExport();
            Assert.Equal(1, export.Value);
            Assert.Equal(1, ExportCreatorCalled);
            Assert.Equal(0, ExportActionCalled);

            export.Dispose();
            Assert.Equal(1, ExportCreatorCalled);
            Assert.Equal(1, ExportActionCalled);
        }

        [Fact]
        public void CreateExport_MultipleTimes_ReturnsDifferentExports()
        {
            var factory = new ExportFactory<int>(ExportCreator);
            Export<int> export1 = factory.CreateExport();
            Export<int> export2 = factory.CreateExport();
            Assert.Equal(2, ExportCreatorCalled);
            Assert.NotSame(export1, export2);
        }

        [Fact]
        public void Ctor_NullExport_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("exportCreator", () => new ExportFactory<int>(null));
        }

        private int ExportCreatorCalled { get; set; }
        private int ExportActionCalled { get; set; }

        private Tuple<int, Action> ExportCreator()
        {
            ExportCreatorCalled++;
            return Tuple.Create<int, Action>(1, () => ExportActionCalled++);
        }
    }
}
