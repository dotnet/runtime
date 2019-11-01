// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.DirectoryServices.Tests
{
    public class DirectoryVirtualListViewTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var listView = new DirectoryVirtualListView();
            Assert.Equal(0, listView.AfterCount);
            Assert.Equal(0, listView.ApproximateTotal);
            Assert.Equal(0, listView.BeforeCount);
            Assert.Null(listView.DirectoryVirtualListViewContext);
            Assert.Equal(0, listView.Offset);
            Assert.Equal(string.Empty, listView.Target);
            Assert.Equal(0, listView.TargetPercentage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_BeforeCount(int afterCount)
        {
            var listView = new DirectoryVirtualListView(afterCount);
            Assert.Equal(afterCount, listView.AfterCount);
            Assert.Equal(0, listView.ApproximateTotal);
            Assert.Equal(0, listView.BeforeCount);
            Assert.Null(listView.DirectoryVirtualListViewContext);
            Assert.Equal(0, listView.Offset);
            Assert.Equal(string.Empty, listView.Target);
            Assert.Equal(0, listView.TargetPercentage);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 2, 3)]
        public void Ctor_BeforeCount_AfterCount_Offset(int beforeCount, int afterCount, int offset)
        {
            var listView = new DirectoryVirtualListView(beforeCount, afterCount, offset);
            Assert.Equal(afterCount, listView.AfterCount);
            Assert.Equal(0, listView.ApproximateTotal);
            Assert.Equal(beforeCount, listView.BeforeCount);
            Assert.Null(listView.DirectoryVirtualListViewContext);
            Assert.Equal(offset, listView.Offset);
            Assert.Equal(string.Empty, listView.Target);
            Assert.Equal(0, listView.TargetPercentage);
        }

        [Theory]
        [InlineData(0, 0, null)]
        [InlineData(1, 2, "")]
        [InlineData(1, 2, "target")]
        public void Ctor_BeforeCount_AfterCount_Target(int beforeCount, int afterCount, string target)
        {
            var listView = new DirectoryVirtualListView(beforeCount, afterCount, target);
            Assert.Equal(afterCount, listView.AfterCount);
            Assert.Equal(0, listView.ApproximateTotal);
            Assert.Equal(beforeCount, listView.BeforeCount);
            Assert.Null(listView.DirectoryVirtualListViewContext);
            Assert.Equal(0, listView.Offset);
            Assert.Equal(target ?? string.Empty, listView.Target);
            Assert.Equal(0, listView.TargetPercentage);
        }

        public static IEnumerable<object[]> Ctor_BeforeCount_AfterCount_Offset_Context_TestData()
        {
            yield return new object[] { 0, 0, 0, null };
            yield return new object[] { 1, 2, 3, new DirectoryVirtualListViewContext() };
        }

        [Theory]
        [MemberData(nameof(Ctor_BeforeCount_AfterCount_Offset_Context_TestData))]
        public void Ctor_BeforeCount_AfterCount_Offset_Context(int beforeCount, int afterCount, int offset, DirectoryVirtualListViewContext context)
        {
            var listView = new DirectoryVirtualListView(beforeCount, afterCount, offset, context);
            Assert.Equal(afterCount, listView.AfterCount);
            Assert.Equal(0, listView.ApproximateTotal);
            Assert.Equal(beforeCount, listView.BeforeCount);
            Assert.Equal(context, listView.DirectoryVirtualListViewContext);
            Assert.Equal(offset, listView.Offset);
            Assert.Equal(string.Empty, listView.Target);
            Assert.Equal(0, listView.TargetPercentage);
        }

        public static IEnumerable<object[]> Ctor_BeforeCount_AfterCount_Target_Context_TestData()
        {
            yield return new object[] { 0, 0, null, null };
            yield return new object[] { 1, 2, "", new DirectoryVirtualListViewContext() };
            yield return new object[] { 1, 2, "target", new DirectoryVirtualListViewContext() };
        }

        [Theory]
        [MemberData(nameof(Ctor_BeforeCount_AfterCount_Target_Context_TestData))]
        public void Ctor_BeforeCount_AfterCount_Target_Context(int beforeCount, int afterCount, string target, DirectoryVirtualListViewContext context)
        {
            var listView = new DirectoryVirtualListView(beforeCount, afterCount, target, context);
            Assert.Equal(afterCount, listView.AfterCount);
            Assert.Equal(0, listView.ApproximateTotal);
            Assert.Equal(beforeCount, listView.BeforeCount);
            Assert.Equal(context, listView.DirectoryVirtualListViewContext);
            Assert.Equal(0, listView.Offset);
            Assert.Equal(target ?? string.Empty, listView.Target);
            Assert.Equal(0, listView.TargetPercentage);
        }

        [Fact]
        public void Ctor_NegativeBeforeCount_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(0, -1, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(0, -1, "target"));
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(0, -1, 0, new DirectoryVirtualListViewContext()));
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(0, -1, "target", new DirectoryVirtualListViewContext()));
        }

        [Fact]
        public void Ctor_NegativeAfterCount_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(-1));
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(-1, 0, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(-1, 0, "target"));
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(-1, 0, 0, new DirectoryVirtualListViewContext()));
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(-1, 0, "target", new DirectoryVirtualListViewContext()));
        }

        [Fact]
        public void Ctor_NegativeOffset_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(0, 0, -1));
            AssertExtensions.Throws<ArgumentException>(null, () => new DirectoryVirtualListView(0, 0, -1, new DirectoryVirtualListViewContext()));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void ApproximateTotal_SetValid_GetReturnsExpected(int value)
        {
            var listView = new DirectoryVirtualListView { ApproximateTotal = value };
            Assert.Equal(value, listView.ApproximateTotal);
        }

        [Fact]
        public void ApproximateTotal_SetNegative_GetReturnsExpected()
        {
            var listView = new DirectoryVirtualListView();
            AssertExtensions.Throws<ArgumentException>(null, () => listView.ApproximateTotal = -1);
        }

        [Fact]
        public void DirectoryVirtualListViewContext_Set_GetReturnsExpected()
        {
            var context = new DirectoryVirtualListViewContext();
            var listView = new DirectoryVirtualListView { DirectoryVirtualListViewContext = context };
            Assert.Equal(context, listView.DirectoryVirtualListViewContext);
        }

        [Fact]
        public void Offset_SetWithApproximateTotal_SetsTargetPercentage()
        {
            var listView = new DirectoryVirtualListView
            {
                ApproximateTotal = 200,
                Offset = 50
            };
            Assert.Equal(50, listView.Offset);
            Assert.Equal(25, listView.TargetPercentage);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(200, 10, 20)]
        [InlineData(10, 100, 10)]
        public void TargetPercentage_SetValid_GetReturnsExpected(int approximateTotal, int targetPercentage, int expectedOffset)
        {
            var listView = new DirectoryVirtualListView
            {
                ApproximateTotal = approximateTotal,
                TargetPercentage = targetPercentage
            };
            Assert.Equal(targetPercentage, listView.TargetPercentage);
            Assert.Equal(expectedOffset, listView.Offset);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void TargetPercentage_SetInvalid_GetReturnsExpected(int value)
        {
            var listView = new DirectoryVirtualListView();
            AssertExtensions.Throws<ArgumentException>(null, () => listView.TargetPercentage = value);
        }
    }
}
