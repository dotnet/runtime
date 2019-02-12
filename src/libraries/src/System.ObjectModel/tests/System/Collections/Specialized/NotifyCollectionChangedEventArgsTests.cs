// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NotifyCollectionChangedEventArgsTests
    {
        [Fact]
        public void Ctor_NotifyCollectionChangedAction()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
            Assert.Null(e.NewItems);
            Assert.Equal(-1, e.NewStartingIndex);
            Assert.Null(e.OldItems);
            Assert.Equal(-1, e.OldStartingIndex);
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Add)]
        [InlineData(NotifyCollectionChangedAction.Move)]
        [InlineData(NotifyCollectionChangedAction.Remove)]
        [InlineData(NotifyCollectionChangedAction.Replace)]
        [InlineData(NotifyCollectionChangedAction.Add - 1)]
        [InlineData(NotifyCollectionChangedAction.Reset + 1)]
        public void Ctor_InvalidActionForReset_ThrowsArgumentException(NotifyCollectionChangedAction action)
        {
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action));
        }
        
        public static IEnumerable<object[]> Ctor_NotifyCollectionChangedAction_Object_TestData()
        {
            yield return new object[] { NotifyCollectionChangedAction.Add, "item" };
            yield return new object[] { NotifyCollectionChangedAction.Add, null };
            yield return new object[] { NotifyCollectionChangedAction.Remove, "item" };
            yield return new object[] { NotifyCollectionChangedAction.Remove, null };
            yield return new object[] { NotifyCollectionChangedAction.Reset, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_NotifyCollectionChangedAction_Object_TestData))]
        public void Ctor_NotifyCollectionChangedAction_Object(NotifyCollectionChangedAction action, object changedItem)
        {
            var e = new NotifyCollectionChangedEventArgs(action, changedItem);
            Assert.Equal(action, e.Action);
            Assert.Equal(action == NotifyCollectionChangedAction.Add ? new object[] { changedItem } : null, e.NewItems);
            Assert.Equal(-1, e.NewStartingIndex);
            Assert.Equal(action == NotifyCollectionChangedAction.Remove ? new object[] { changedItem } : null, e.OldItems);
            Assert.Equal(-1, e.OldStartingIndex);
        }
        
        public static IEnumerable<object[]> Ctor_NotifyCollectionChangedAction_Object_Int_TestData()
        {
            yield return new object[] { NotifyCollectionChangedAction.Add, "item", 10 };
            yield return new object[] { NotifyCollectionChangedAction.Add, null, 0 };
            yield return new object[] { NotifyCollectionChangedAction.Add, "item", -1 };
            yield return new object[] { NotifyCollectionChangedAction.Add, "item", -10 };
            yield return new object[] { NotifyCollectionChangedAction.Remove, "item", 10 };
            yield return new object[] { NotifyCollectionChangedAction.Remove, null, 0 };
            yield return new object[] { NotifyCollectionChangedAction.Remove, "item", -1 };
            yield return new object[] { NotifyCollectionChangedAction.Remove, "item", -10 };
            yield return new object[] { NotifyCollectionChangedAction.Reset, null, -1 };
        }

        [Theory]
        [MemberData(nameof(Ctor_NotifyCollectionChangedAction_Object_Int_TestData))]
        public void Ctor_NotifyCollectionChangedAction_Object_Int(NotifyCollectionChangedAction action, object changedItem, int index)
        {
            var e = new NotifyCollectionChangedEventArgs(action, changedItem, index);
            Assert.Equal(action, e.Action);
            Assert.Equal(action == NotifyCollectionChangedAction.Add ? new object[] { changedItem } : null, e.NewItems);
            Assert.Equal(action == NotifyCollectionChangedAction.Add ? index : -1, e.NewStartingIndex);
            Assert.Equal(action == NotifyCollectionChangedAction.Remove ? new object[] { changedItem } : null, e.OldItems);
            Assert.Equal(action == NotifyCollectionChangedAction.Remove ? index : -1, e.OldStartingIndex);
        }

        public static IEnumerable<object[]> Ctor_NotifyCollectionChangedAction_IList_TestData()
        {
            yield return new object[] { NotifyCollectionChangedAction.Add, new object[0] };
            yield return new object[] { NotifyCollectionChangedAction.Add, new object[] { "item" } };
            yield return new object[] { NotifyCollectionChangedAction.Remove, new object[0] };
            yield return new object[] { NotifyCollectionChangedAction.Remove, new object[] { "item" } };
            yield return new object[] { NotifyCollectionChangedAction.Reset, null };
        }

        [Theory]
        [MemberData(nameof(Ctor_NotifyCollectionChangedAction_IList_TestData))]
        public void Ctor_NotifyCollectionChangedAction_IList(NotifyCollectionChangedAction action, IList changedItems)
        {
            var e = new NotifyCollectionChangedEventArgs(action, changedItems);
            Assert.Equal(action, e.Action);
            Assert.Equal(action == NotifyCollectionChangedAction.Add ? changedItems : null, e.NewItems);
            Assert.Equal(-1, e.NewStartingIndex);
            Assert.Equal(action == NotifyCollectionChangedAction.Remove ? changedItems : null, e.OldItems);
            Assert.Equal(-1, e.OldStartingIndex);
        }

        public static IEnumerable<object[]> Ctor_NotifyCollectionChangedAction_IList_Int_TestData()
        {
            yield return new object[] { NotifyCollectionChangedAction.Add, new object[0], 10 };
            yield return new object[] { NotifyCollectionChangedAction.Add, new object[] { "item" }, 10 };
            yield return new object[] { NotifyCollectionChangedAction.Add, new object[] { "item" }, 0 };
            yield return new object[] { NotifyCollectionChangedAction.Add, new object[] { "item" }, -1 };
            yield return new object[] { NotifyCollectionChangedAction.Remove, new object[0], 10 };
            yield return new object[] { NotifyCollectionChangedAction.Remove, new object[] { "item" }, 10 };
            yield return new object[] { NotifyCollectionChangedAction.Remove, new object[] { "item" }, 0 };
            yield return new object[] { NotifyCollectionChangedAction.Remove, new object[] { "item" }, -1 };
            yield return new object[] { NotifyCollectionChangedAction.Reset, null, -1 };
        }

        [Theory]
        [MemberData(nameof(Ctor_NotifyCollectionChangedAction_IList_Int_TestData))]
        public void Ctor_NotifyCollectionChangedAction_IList_Int(NotifyCollectionChangedAction action, IList changedItems, int startingIndex)
        {
            var e = new NotifyCollectionChangedEventArgs(action, changedItems, startingIndex);
            Assert.Equal(action, e.Action);
            Assert.Equal(action == NotifyCollectionChangedAction.Add ? changedItems : null, e.NewItems);
            Assert.Equal(action == NotifyCollectionChangedAction.Add ? startingIndex : -1, e.NewStartingIndex);
            Assert.Equal(action == NotifyCollectionChangedAction.Remove ? changedItems : null, e.OldItems);
            Assert.Equal(action == NotifyCollectionChangedAction.Remove ? startingIndex : -1, e.OldStartingIndex);
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Move)]
        [InlineData(NotifyCollectionChangedAction.Replace)]
        [InlineData(NotifyCollectionChangedAction.Add - 1)]
        [InlineData(NotifyCollectionChangedAction.Reset + 1)]
        public void Ctor_InvalidActionForAddRemove_ThrowsArgumentException(NotifyCollectionChangedAction action)
        {
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, "value"));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, "value", 1));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, new object[0]));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, new object[0], 1));
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Add)]
        [InlineData(NotifyCollectionChangedAction.Remove)]
        public void Ctor_ChangedNullForAddOrRemove_ThrowsArgumentNullException(NotifyCollectionChangedAction action)
        {
            AssertExtensions.Throws<ArgumentNullException>("changedItems", () => new NotifyCollectionChangedEventArgs(action, (IList)null));
            AssertExtensions.Throws<ArgumentNullException>("changedItems", () => new NotifyCollectionChangedEventArgs(action, (IList)null, -1));
        }

        [Fact]
        public void Ctor_ChangedNonNullForReset_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, "item"));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, "item", -1));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, new object[] { "item" }));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, new object[] { "item" }, -1));
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Add, -2)]
        [InlineData(NotifyCollectionChangedAction.Remove, -2)]
        public void Ctor_ChangedIndexLessThanMinusOneForAddOrRemove_ThrowsArgumentException(NotifyCollectionChangedAction action, int startingIndex)
        {
            AssertExtensions.Throws<ArgumentException>("startingIndex", () => new NotifyCollectionChangedEventArgs(action, new object[0], startingIndex));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(0)]
        public void Ctor_ChangedIndexNotMinusOneForReset_ThrowsArgumentException(int startingIndex)
        {
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, (object)null, startingIndex));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, (IList)null, startingIndex));
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Move)]
        [InlineData(NotifyCollectionChangedAction.Replace)]
        [InlineData(NotifyCollectionChangedAction.Add - 1)]
        [InlineData(NotifyCollectionChangedAction.Reset + 1)]
        public void Ctor_InvalidActionForChanged_ThrowsArgumentException(NotifyCollectionChangedAction action)
        {
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, "item"));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, "item", -1));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, new object[] { "item" }));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, new object[] { "item", -1 }));
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Replace, "newItem", "oldItem")]
        [InlineData(NotifyCollectionChangedAction.Replace, "oldItem", "oldItem")]
        [InlineData(NotifyCollectionChangedAction.Replace, null, "item")]
        [InlineData(NotifyCollectionChangedAction.Replace, "item", null)]
        public void Ctor_NotifyCollectionChangedAction_Object_Object(NotifyCollectionChangedAction action, object newItem, object oldItem)
        {
            var e = new NotifyCollectionChangedEventArgs(action, newItem, oldItem);
            Assert.Equal(action, e.Action);
            Assert.Equal(new object[] { newItem }, e.NewItems);
            Assert.Equal(-1, e.NewStartingIndex);
            Assert.Equal(new object[] { oldItem }, e.OldItems);
            Assert.Equal(-1, e.OldStartingIndex);
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Replace, "newItem", "oldItem", 10)]
        [InlineData(NotifyCollectionChangedAction.Replace, "oldItem", "oldItem", 0)]
        [InlineData(NotifyCollectionChangedAction.Replace, null, "item", -10)]
        [InlineData(NotifyCollectionChangedAction.Replace, "item", null, -1)]
        public void Ctor_NotifyCollectionChangedAction_Object_Object_Int(NotifyCollectionChangedAction action, object newItem, object oldItem, int index)
        {
            var e = new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index);
            Assert.Equal(action, e.Action);
            Assert.Equal(new object[] { newItem }, e.NewItems);
            Assert.Equal(index, e.NewStartingIndex);
            Assert.Equal(new object[] { oldItem }, e.OldItems);
            Assert.Equal(index, e.OldStartingIndex);
        }

        public static IEnumerable<object[]> Ctor_NotifyCollectionChangedAction_IList_IList_TestData()
        {
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[] { "newItem" }, new object[] { "oldItem" } };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[] { "newItem" }, new object[0] };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[0], new object[] { "oldItem "} };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[0], new object[0] };
        }

        [Theory]
        [MemberData(nameof(Ctor_NotifyCollectionChangedAction_IList_IList_TestData))]
        public void Ctor_NotifyCollectionChangedAction_IList_IList(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
        {
            var e = new NotifyCollectionChangedEventArgs(action, newItems, oldItems);
            Assert.Equal(action, e.Action);
            Assert.Equal(newItems, e.NewItems);
            Assert.Equal(-1, e.NewStartingIndex);
            Assert.Equal(oldItems, e.OldItems);
            Assert.Equal(-1, e.OldStartingIndex);
        }

        public static IEnumerable<object[]> Ctor_NotifyCollectionChangedAction_IList_IList_Int_TestData()
        {
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[] { "newItem" }, new object[] { "oldItem" }, 10 };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[] { "newItem" }, new object[] { "oldItem" }, 0 };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[] { "newItem" }, new object[] { "oldItem" }, -1 };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[] { "newItem" }, new object[] { "oldItem" }, -2 };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[] { "newItem" }, new object[0], 1 };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[0], new object[] { "oldItem "}, 1 };
            yield return new object[] { NotifyCollectionChangedAction.Replace, new object[0], new object[0], 1 };
        }

        [Theory]
        [MemberData(nameof(Ctor_NotifyCollectionChangedAction_IList_IList_Int_TestData))]
        public void Ctor_NotifyCollectionChangedAction_IList_IList_Int(NotifyCollectionChangedAction action, IList newItems, IList oldItems, int index)
        {
            var e = new NotifyCollectionChangedEventArgs(action, newItems, oldItems, index);
            Assert.Equal(action, e.Action);
            Assert.Equal(newItems, e.NewItems);
            Assert.Equal(index, e.NewStartingIndex);
            Assert.Equal(oldItems, e.OldItems);
            Assert.Equal(index, e.OldStartingIndex);
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Add)]
        [InlineData(NotifyCollectionChangedAction.Remove)]
        [InlineData(NotifyCollectionChangedAction.Move)]
        [InlineData(NotifyCollectionChangedAction.Reset)]
        [InlineData(NotifyCollectionChangedAction.Add - 1)]
        [InlineData(NotifyCollectionChangedAction.Reset + 1)]
        public void Ctor_InvalidActionForReplace_ThrowsArgumentException(NotifyCollectionChangedAction action)
        {
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, "newItem", "oldItem"));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, "newItem", "oldItem", -1));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, new object[] { "newItem" }, new object[] { "oldItem" }));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, new object[] { "newItem" }, new object[] { "oldItem" }, -1));
        }

        [Fact]
        public void Ctor_NullNewItemsForReplace_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("newItems", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, null, new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("newItems", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, null, new object[0], -1));
        }

        [Fact]
        public void Ctor_NullOldItemsForReplace_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("oldItems", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[0], null));
            AssertExtensions.Throws<ArgumentNullException>("oldItems", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[0], null, -1));
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Move, "item", 0, -1)]
        [InlineData(NotifyCollectionChangedAction.Move, "item", 1, 1)]
        [InlineData(NotifyCollectionChangedAction.Move, "item", 1, -2)]
        [InlineData(NotifyCollectionChangedAction.Move, null, 1, 2)]
        public void Ctor_NotifyCollectionChangedAction_Object_Int_Int(NotifyCollectionChangedAction action, object changedItem, int index, int oldIndex)
        {
            var e = new NotifyCollectionChangedEventArgs(action, changedItem, index, oldIndex);
            Assert.Equal(action, e.Action);
            Assert.Equal(new object[] { changedItem }, e.NewItems);
            Assert.Equal(index, e.NewStartingIndex);
            Assert.Equal(new object[] { changedItem }, e.OldItems);
            Assert.Equal(oldIndex, e.OldStartingIndex);
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Move, null, 0, -1)]
        [InlineData(NotifyCollectionChangedAction.Move, new object[] { "item" }, 1, 1)]
        [InlineData(NotifyCollectionChangedAction.Move, new object[0], 1, -2)]
        [InlineData(NotifyCollectionChangedAction.Move, null, 1, 2)]
        public void Ctor_NotifyCollectionChangedAction_IList_Int_Int(NotifyCollectionChangedAction action, IList changedItems, int index, int oldIndex)
        {
            var e = new NotifyCollectionChangedEventArgs(action, changedItems, index, oldIndex);
            Assert.Equal(action, e.Action);
            Assert.Equal(changedItems, e.NewItems);
            Assert.Equal(index, e.NewStartingIndex);
            Assert.Equal(changedItems, e.OldItems);
            Assert.Equal(oldIndex, e.OldStartingIndex);
        }

        [Theory]
        [InlineData(NotifyCollectionChangedAction.Add)]
        [InlineData(NotifyCollectionChangedAction.Remove)]
        [InlineData(NotifyCollectionChangedAction.Replace)]
        [InlineData(NotifyCollectionChangedAction.Reset)]
        [InlineData(NotifyCollectionChangedAction.Add - 1)]
        [InlineData(NotifyCollectionChangedAction.Reset + 1)]
        public void Ctor_InvalidActionForMove_ThrowsArgumentException(NotifyCollectionChangedAction action)
        {
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, "item", 1, 1));
            AssertExtensions.Throws<ArgumentException>("action", () => new NotifyCollectionChangedEventArgs(action, new object[] { "item" }, 1, 1));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        public void Ctor_LessThanZeroIndexForMove_ThrowsArgumentException(int index)
        {
            AssertExtensions.Throws<ArgumentException>("index", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, "item", index, 1));
            AssertExtensions.Throws<ArgumentException>("index", () => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, new object[] { "item" }, index, 1));
        }

        [Fact]
        public void List_GetProperties_ReturnsExpected()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item1", "item2" }, new object[0]);
            IList list = e.NewItems;
            Assert.Equal(2, list.Count);
            Assert.True(list.IsFixedSize);
            Assert.True(list.IsReadOnly);
            Assert.False(list.IsSynchronized);
            Assert.NotNull(list.SyncRoot);
        }

        [Fact]
        public void List_Add_ThrowsNotSupportedException()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Throws<NotSupportedException>(() => list.Add(1));
        }

        [Fact]
        public void List_ItemGet_ReturnsExpected()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Equal("item", list[0]);
        }

        [Fact]
        public void List_ItemSet_ThrowsNotSupportedException()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Throws<NotSupportedException>(() => list[0] = 1);
        }

        [Fact]
        public void List_Clear_ThrowsNotSupportedException()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Throws<NotSupportedException>(() => list.Clear());
        }

        [Theory]
        [InlineData("item", true)]
        [InlineData(1, false)]
        [InlineData(null, false)]
        public void List_Contains_ReturnsExpected(object value, bool expected)
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Equal(expected, list.Contains(value));
        }

        [Fact]
        public void List_CopyToInvoke_Success()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            object[] array = new object[] { 1, 2, 3 };
            list.CopyTo(array, 1);
            Assert.Equal(new object[] { 1, "item", 3 }, array);
        }

        [Theory]
        [InlineData("item", 0)]
        [InlineData(1, -1)]
        [InlineData(null, -1)]
        public void List_IndexOf_ReturnsExpected(object value, int expected)
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Equal(expected, list.IndexOf(value));
        }

        [Fact]
        public void List_Insert_ThrowsNotSupportedException()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Throws<NotSupportedException>(() => list.Insert(0, 1));
        }

        [Fact]
        public void List_Remove_ThrowsNotSupportedException()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Throws<NotSupportedException>(() => list.Remove(1));
        }

        [Fact]
        public void List_RemoveAt_ThrowsNotSupportedException()
        {
            var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new object[] { "item" }, new object[0]);
            IList list = e.NewItems;
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
        }
    }
}
