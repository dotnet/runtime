// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.Tests
{
    public class DebuggerVisualizerAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("VisualizerTypeName")]
        public void Ctor_VisualizerTypeName(string visualizerTypeName)
        {
            var attribute = new DebuggerVisualizerAttribute(visualizerTypeName);
            Assert.Equal(visualizerTypeName, attribute.VisualizerTypeName);
            Assert.Null(attribute.VisualizerObjectSourceTypeName);
            Assert.Null(attribute.Description);
            Assert.Null(attribute.Target);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("VisualizerTypeName", "VisualizerObjectSourceTypeName")]
        public void Ctor_VisualizerTypeName_VisualizerObjectSourceTypeName(string visualizerTypeName, string visualizerObjectSourceTypeName)
        {
            var attribute = new DebuggerVisualizerAttribute(visualizerTypeName, visualizerObjectSourceTypeName);
            Assert.Equal(visualizerTypeName, attribute.VisualizerTypeName);
            Assert.Equal(visualizerObjectSourceTypeName, attribute.VisualizerObjectSourceTypeName);
            Assert.Null(attribute.Description);
            Assert.Null(attribute.Target);
        }

        [Theory]
        [InlineData(null, typeof(int))]
        [InlineData("VisualizerTypeName", typeof(DebuggerVisualizerAttributeTests))]
        public void Ctor_VisualizerTypeName_VisualizerObjectSourceType(string visualizerTypeName, Type visualizerObjectSourceType)
        {
            var attribute = new DebuggerVisualizerAttribute(visualizerTypeName, visualizerObjectSourceType);
            Assert.Equal(visualizerTypeName, attribute.VisualizerTypeName);
            Assert.Equal(visualizerObjectSourceType.AssemblyQualifiedName, attribute.VisualizerObjectSourceTypeName);
            Assert.Null(attribute.Description);
            Assert.Null(attribute.Target);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(DebuggerVisualizerAttributeTests))]
        public void Ctor_VisualizerType(Type visualizerType)
        {
            var attribute = new DebuggerVisualizerAttribute(visualizerType);
            Assert.Equal(visualizerType.AssemblyQualifiedName, attribute.VisualizerTypeName);
            Assert.Null(attribute.VisualizerObjectSourceTypeName);
            Assert.Null(attribute.Description);
            Assert.Null(attribute.Target);
        }

        [Theory]
        [InlineData(typeof(string), typeof(int))]
        [InlineData(typeof(DebuggerVisualizerAttributeTests), typeof(DebuggerVisualizerAttributeTests))]
        public void Ctor_VisualizerType_VisualizerObjectSourceType(Type visualizerType, Type visualizerObjectSourceType)
        {
            var attribute = new DebuggerVisualizerAttribute(visualizerType, visualizerObjectSourceType);
            Assert.Equal(visualizerType.AssemblyQualifiedName, attribute.VisualizerTypeName);
            Assert.Equal(visualizerObjectSourceType.AssemblyQualifiedName, attribute.VisualizerObjectSourceTypeName);
            Assert.Null(attribute.Description);
            Assert.Null(attribute.Target);
        }

        [Theory]
        [InlineData(typeof(string), null)]
        [InlineData(typeof(DebuggerVisualizerAttributeTests), "VisualizerObjectSourceTypeName")]
        public void Ctor_VisualizerType_VisualizerObjectSourceTypeName(Type visualizerType, string visualizerObjectSourceTypeName)
        {
            var attribute = new DebuggerVisualizerAttribute(visualizerType, visualizerObjectSourceTypeName);
            Assert.Equal(visualizerType.AssemblyQualifiedName, attribute.VisualizerTypeName);
            Assert.Equal(visualizerObjectSourceTypeName, attribute.VisualizerObjectSourceTypeName);
            Assert.Null(attribute.Description);
            Assert.Null(attribute.Target);
        }

        [Fact]
        public void Ctor_NullVisualizerType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("visualizer", () => new DebuggerVisualizerAttribute((Type)null));
            AssertExtensions.Throws<ArgumentNullException>("visualizer", () => new DebuggerVisualizerAttribute((Type)null, "VisualizerObjectSourceTypeName"));
            AssertExtensions.Throws<ArgumentNullException>("visualizer", () => new DebuggerVisualizerAttribute((Type)null, typeof(int)));
        }

        [Fact]
        public void Ctor_NullVisualizerObjectSourceType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("visualizerObjectSource", () => new DebuggerVisualizerAttribute("VisualizerTypeName", (Type)null));
            AssertExtensions.Throws<ArgumentNullException>("visualizerObjectSource", () => new DebuggerVisualizerAttribute(typeof(string), (Type)null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(DebuggerVisualizerAttributeTests))]
        public void Target_SetValid_GetReturnsExpected(Type target)
        {
            var attribute = new DebuggerVisualizerAttribute("VisualizerTypeName") { Target = target };
            Assert.Equal(target, attribute.Target);
            Assert.Equal(target.AssemblyQualifiedName, attribute.TargetTypeName);
        }

        [Fact]
        public void Target_SetNull_ThrowsArgumentNullException()
        {
            var attribute = new DebuggerVisualizerAttribute("VisualizerTypeName");
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Target = null);
        }
    }
}
