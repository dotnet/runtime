// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class BindingListTest
    {
        [Fact]
        public void Ctor_Default()
        {
            var list = new BindingList<string>();
            IBindingList iBindingList = list;

            Assert.True(list.AllowEdit);
            Assert.False(list.AllowNew);
            Assert.True(list.AllowRemove);
            Assert.True(list.RaiseListChangedEvents);

            Assert.True(iBindingList.AllowEdit);
            Assert.False(iBindingList.AllowNew);
            Assert.True(iBindingList.AllowRemove);
            Assert.Equal(ListSortDirection.Ascending, iBindingList.SortDirection);
            Assert.True(iBindingList.SupportsChangeNotification);
            Assert.False(iBindingList.SupportsSearching);
            Assert.False(iBindingList.SupportsSorting);
            Assert.False(((IRaiseItemChangedEvents)list).RaisesItemChangedEvents);
        }

        [Fact]
        public void Ctor_FixedSizeIList()
        {
            var array = new string[10];
            var bindingList = new BindingList<string>(array);
            IBindingList iBindingList = bindingList;

            Assert.True(bindingList.AllowEdit);
            Assert.False(bindingList.AllowNew);
            Assert.True(bindingList.AllowRemove);
            Assert.True(bindingList.RaiseListChangedEvents);

            Assert.True(iBindingList.AllowEdit);
            Assert.False(iBindingList.AllowNew);
            Assert.True(iBindingList.AllowRemove);
            Assert.False(iBindingList.IsSorted);
            Assert.Equal(ListSortDirection.Ascending, iBindingList.SortDirection);
            Assert.True(iBindingList.SupportsChangeNotification);
            Assert.False(iBindingList.SupportsSearching);
            Assert.False(iBindingList.SupportsSorting);
            Assert.False(((IRaiseItemChangedEvents)bindingList).RaisesItemChangedEvents);
        }

        [Fact]
        public void Ctor_NonFixedSizeIList()
        {
            var list = new List<string>();
            var bindingList = new BindingList<string>(list);
            IBindingList iBindingList = bindingList;

            Assert.True(bindingList.AllowEdit);
            Assert.False(bindingList.AllowNew);
            Assert.True(bindingList.AllowRemove);
            Assert.True(bindingList.RaiseListChangedEvents);

            Assert.True(iBindingList.AllowEdit);
            Assert.False(iBindingList.AllowNew);
            Assert.True(iBindingList.AllowRemove);
            Assert.False(iBindingList.IsSorted);
            Assert.Equal(ListSortDirection.Ascending, iBindingList.SortDirection);
            Assert.True(iBindingList.SupportsChangeNotification);
            Assert.False(iBindingList.SupportsSearching);
            Assert.False(iBindingList.SupportsSorting);
            Assert.False(((IRaiseItemChangedEvents)bindingList).RaisesItemChangedEvents);
        }

        [Fact]
        public void Ctor_IReadOnlyList()
        {
            var list = new List<string>();
            var bindingList = new BindingList<string>(list);
            IBindingList iBindingList = bindingList;

            Assert.True(bindingList.AllowEdit);
            Assert.False(bindingList.AllowNew);
            Assert.True(bindingList.AllowRemove);
            Assert.True(bindingList.RaiseListChangedEvents);

            Assert.True(iBindingList.AllowEdit);
            Assert.False(iBindingList.AllowNew);
            Assert.True(iBindingList.AllowRemove);
            Assert.False(iBindingList.IsSorted);
            Assert.Equal(ListSortDirection.Ascending, iBindingList.SortDirection);
            Assert.True(iBindingList.SupportsChangeNotification);
            Assert.False(iBindingList.SupportsSearching);
            Assert.False(iBindingList.SupportsSorting);
            Assert.False(((IRaiseItemChangedEvents)bindingList).RaisesItemChangedEvents);
        }

        [Fact]
        public void AllowNew_GetWithDefaultCtor_ReturnsTrue()
        {
            var bindingList = new BindingList<object>();
            Assert.True(bindingList.AllowNew);
        }

        [Fact]
        public void AllowNew_Primitive_ReturnsTrue()
        {
            var bindingList = new BindingList<int>();
            Assert.True(bindingList.AllowNew);
        }

        [Fact]
        public void AllowNew_NoDefaultCtor_ReturnsExpected()
        {
            var bindingList = new BindingList<string>();
            Assert.False(bindingList.AllowNew);

            // Binding an AddingNew delegate allows new. 
            bindingList.AddingNew += (object sender, AddingNewEventArgs e) => { };
            Assert.True(bindingList.AllowNew);
        }

        [Fact]
        public void AllowNew_SetFalse_CallsListChanged()
        {
            var bindingList = new BindingList<object>();

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.False(bindingList.AllowNew);
            };
            bindingList.AllowNew = false;

            Assert.True(calledListChanged);
        }

        [Fact]
        public void AllowNew_SetTrue_DoesNotCallListChanged()
        {
            // The default for T=object is true, so check if we enter the
            // block for raising the event if we explicitly set it to the value
            // it currently has.
            var bindingList = new BindingList<object>();

            bool calledListChanged = false;
            bindingList.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                calledListChanged = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.True(bindingList.AllowNew);
            };
            bindingList.AllowNew = true;

            // It doesn't raise the event.
            Assert.False(calledListChanged);
        }

        [Fact]
        public void ResetBindings_Invoke_CallsListChanged()
        {
            var bindingList = new BindingList<object>();

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
            };
            bindingList.ResetBindings();

            Assert.True(calledListChanged);
        }

        [Fact]
        public void ResetItem_Invoke_CallsListChanged()
        {
            var list = new List<object> { new object() };
            var bindingList = new BindingList<object>(list);

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(0, e.NewIndex);
                Assert.Equal(ListChangedType.ItemChanged, e.ListChangedType);
            };
            bindingList.ResetItem(0);

            Assert.True(calledListChanged);
        }

        [Fact]
        public void RemoveAt_Invoke_CallsListChanged()
        {
            var list = new List<object> { new object() };
            var bindingList = new BindingList<object>(list);

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(0, e.NewIndex);
                Assert.Equal(ListChangedType.ItemDeleted, e.ListChangedType);

                // The event is raised after the removal.
                Assert.Equal(0, bindingList.Count);
            };
            bindingList.RemoveAt(0);

            Assert.True(calledListChanged);
        }

        [Fact]
        public void RemoteAt_AllowRemoveFalse_ThrowsNotSupportedException()
        {
            var list = new List<object> { new object() };
            var bindingList = new BindingList<object>(list) { AllowRemove = false };

            Assert.Throws<NotSupportedException>(() => bindingList.RemoveAt(0));
        }

        [Fact]
        public void AllowEdit_Set_InvokesListChanged()
        {
            var bindingList = new BindingList<object>();

            bool calledListChanged = false;
            bool expectedAllowEdit = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(expectedAllowEdit, bindingList.AllowEdit);
            };

            bindingList.AllowEdit = false;
            Assert.True(calledListChanged);

            // ListChanged is not called if RaiseListChangedEvents is false.
            bindingList.RaiseListChangedEvents = false;
            bindingList.RaiseListChangedEvents = false;

            calledListChanged = false;
            bindingList.AllowEdit = true;
            Assert.False(calledListChanged);
        }

        [Fact]
        public void AllowRemove_Set_InvokesListChanged()
        {
            var bindingList = new BindingList<object>();

            bool calledListChanged = false;
            bool expectedAllowRemove = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(expectedAllowRemove, bindingList.AllowRemove);
            };
            
            bindingList.AllowRemove = false;
            Assert.True(calledListChanged);

            // ListChanged is not called if RaiseListChangedEvents is false.
            bindingList.RaiseListChangedEvents = false;

            calledListChanged = false;
            bindingList.AllowRemove = true;
            Assert.False(calledListChanged);
        }

        [Fact]
        public void AddNew_SetArgsNewObject_ReturnsNewObject()
        {
            BindingList<object> bindingList = new BindingList<object>();

            bool calledAddingNew = false;
            var newObject = new object();

            bindingList.AddingNew += (object sender, AddingNewEventArgs e) =>
            {
                calledAddingNew = true;
                Assert.Null(e.NewObject);
                e.NewObject = newObject;
            };

            Assert.Same(newObject, bindingList.AddNew());
            Assert.True(calledAddingNew);
        }

        [Fact]
        public void AddNew_NullArgsNewObject_ReturnsNotNul()
        {
            var bindingList = new BindingList<object>();

            bool calledAddingNew = false;
            bindingList.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                calledAddingNew = true;
                Assert.Null(e.NewObject);
            };

            Assert.NotNull(bindingList.AddNew());
            Assert.True(calledAddingNew);
        }

        [Fact]
        public void AddNew_CancelNew_Success()
        {
            var bindingList = new BindingList<object>();

            bool calledAddingNew = false;
            bool calledListChanged = false;
            ListChangedType listChangedType = ListChangedType.Reset;
            int listChangedIndex = -1;

            bindingList.AddingNew += (object sender, AddingNewEventArgs e) =>
            {
                calledAddingNew = true;
                Assert.Null(e.NewObject);
            };
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                listChangedType = e.ListChangedType;
                listChangedIndex = e.NewIndex;
            };

            object newValue = bindingList.AddNew();
            Assert.True(calledAddingNew);
            Assert.NotNull(newValue);

            Assert.Equal(1, bindingList.Count);
            Assert.Equal(0, bindingList.IndexOf(newValue));
            Assert.True(calledListChanged);
            Assert.Equal(ListChangedType.ItemAdded, listChangedType);
            Assert.Equal(0, listChangedIndex);

            calledListChanged = false;
            bindingList.CancelNew(0);
            Assert.Equal(0, bindingList.Count);
            Assert.True(calledListChanged);
            Assert.Equal(ListChangedType.ItemDeleted, listChangedType);
            Assert.Equal(0, listChangedIndex);
        }

        [Fact]
        public void AddNew_CancelNewMultipleIndices_RemovesAddNewIndex()
        {
            var list = new List<object> { new object(), new object() };
            var bindingList = new BindingList<object>(list);

            bool calledAddingNew = false;
            bool calledListChanged = false;
            ListChangedType listChangedType = ListChangedType.Reset;
            int listChangedIndex = -1;

            bindingList.AddingNew += (object sender, AddingNewEventArgs e) =>
            {
                calledAddingNew = true;
                Assert.Null(e.NewObject);
            };
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                listChangedType = e.ListChangedType;
                listChangedIndex = e.NewIndex;
            };

            object newValue = bindingList.AddNew();
            Assert.True(calledAddingNew);
            Assert.NotNull(newValue);

            Assert.Equal(3, bindingList.Count);
            Assert.Equal(2, bindingList.IndexOf(newValue));
            Assert.True(calledListChanged);
            Assert.Equal(ListChangedType.ItemAdded, listChangedType);
            Assert.Equal(2, listChangedIndex);

            // Cancelling index 0 does not change the list.
            calledListChanged = false;
            bindingList.CancelNew(0);

            Assert.False(calledListChanged);
            Assert.Equal(3, bindingList.Count);

            // Cancelling index 2 changes the list.
            bindingList.CancelNew(2);

            Assert.True(calledListChanged);
            Assert.Equal(ListChangedType.ItemDeleted, listChangedType);
            Assert.Equal(2, listChangedIndex);
            Assert.Equal(2, bindingList.Count);
        }

        [Fact]
        public void AddNew_EndNew_Success()
        {
            var bindingList = new BindingList<object>();

            bool calledAddNew = false;
            bool calledListChanged = false;
            ListChangedType listChangedType = ListChangedType.Reset;
            int listChangedIndex = -1;

            bindingList.AddingNew += (object sender, AddingNewEventArgs e) =>
            {
                calledAddNew = true;
                Assert.Null(e.NewObject);
            };
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                listChangedType = e.ListChangedType;
                listChangedIndex = e.NewIndex;
            };

            // Make sure the item was added.
            object newValue = bindingList.AddNew();
            Assert.True(calledAddNew);
            Assert.NotNull(newValue);

            Assert.Equal(1, bindingList.Count);
            Assert.Equal(0, bindingList.IndexOf(newValue));
            Assert.True(calledListChanged);
            Assert.Equal(ListChangedType.ItemAdded, listChangedType);
            Assert.Equal(0, listChangedIndex);

            // EndNew does not change the list.
            calledListChanged = false;
            bindingList.EndNew(0);
            Assert.Equal(1, bindingList.Count);
            Assert.False(calledListChanged);
        }

        [Fact]
        public void AddNew_CancelDifferentIndexThenEnd_Success()
        {
            var list = new BindingList<object>();

            bool calledAddingNew = false;
            bool calledListChanged = false;
            ListChangedType listChangedType = ListChangedType.Reset;
            int listChangedIndex = -1;

            list.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                calledAddingNew = true;
                Assert.Null(e.NewObject);
            };
            list.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                calledListChanged = true;
                listChangedType = e.ListChangedType;
                listChangedIndex = e.NewIndex;
            };

            // Make sure AddNew changed the list.
            object newValue = list.AddNew();
            Assert.True(calledAddingNew);
            Assert.NotNull(newValue);

            Assert.Equal(1, list.Count);
            Assert.Equal(0, list.IndexOf(newValue));
            Assert.True(calledListChanged);
            Assert.Equal(ListChangedType.ItemAdded, listChangedType);
            Assert.Equal(0, listChangedIndex);

            // Calling CancelNew on an invalid index does not change the list.
            calledListChanged = false;
            list.CancelNew(2);
            Assert.Equal(1, list.Count);
            Assert.False(calledListChanged);

            // Calling EndNew does not change the list.
            list.EndNew(0);
            Assert.Equal(1, list.Count);
            Assert.False(calledListChanged);
        }

        [Fact]
        public void AddNew_EndDifferenceIndexThanCancel_Success()
        {
            var bindingList = new BindingList<object>();

            bool calledAddingNew = false;
            bool calledListChanged = false;
            ListChangedType listChangedType = ListChangedType.Reset;
            int listChangedIndex = -1;

            bindingList.AddingNew += (object sender, AddingNewEventArgs e) =>
            {
                calledAddingNew = true;
                Assert.Null(e.NewObject);
            };
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                listChangedType = e.ListChangedType;
                listChangedIndex = e.NewIndex;
            };

            // Make sure AddNew changed the list.
            object newValue = bindingList.AddNew();
            Assert.True(calledAddingNew);
            Assert.NotNull(newValue);

            Assert.Equal(1, bindingList.Count);
            Assert.Equal(0, bindingList.IndexOf(newValue));
            Assert.True(calledListChanged);
            Assert.Equal(ListChangedType.ItemAdded, listChangedType);
            Assert.Equal(0, listChangedIndex);

            // EndNew with an invalid index does not change the list.
            calledListChanged = false;
            bindingList.EndNew(2);
            Assert.Equal(1, bindingList.Count);
            Assert.False(calledListChanged);

            // CancelNew with a valid index changes the list.
            bindingList.CancelNew(0);
            Assert.True(calledListChanged);
            Assert.Equal(ListChangedType.ItemDeleted, listChangedType);
            Assert.Equal(0, listChangedIndex);
        }

        [Fact]
        public void AddingNew_RemoveWithAllowNewByDefault_Success()
        {
            var bindingList = new BindingList<int>();

            bool calledAddingNew = false;
            AddingNewEventHandler handler = (object sender, AddingNewEventArgs e) => calledAddingNew = true;
            bindingList.AddingNew += handler;

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) => calledListChanged = true;

            // Make sure removing the handler was successful.
            bindingList.AddingNew -= handler;
            Assert.False(calledListChanged);

            bindingList.AddNew();
            Assert.False(calledAddingNew);

            // Make sure removing multiple times is a nop.
            bindingList.AddingNew -= handler;
        }

        [Fact]
        public void AddingNew_RemoveWithNotAllowNewByDefault_CallsListChanged()
        {
            var bindingList = new BindingList<string>();

            bool calledAddingNew = false;
            AddingNewEventHandler handler = (object sender, AddingNewEventArgs e) => calledAddingNew = true;
            bindingList.AddingNew += handler;

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(-1, e.NewIndex);
            };

            // Make sure removing the handler was successful.
            bindingList.AddingNew -= handler;
            Assert.True(calledListChanged);

            Assert.Throws<MissingMethodException>(() => bindingList.AddNew());
            Assert.False(calledAddingNew);
        }

        [Fact]
        public void ListChanged_AddRemove_Success()
        {
            var bindingList = new BindingList<int>();

            bool calledListChanged = false;
            ListChangedEventHandler handler = (object sender, ListChangedEventArgs e) => calledListChanged = true;
            bindingList.ListChanged += handler;

            // Make sure removing the handler was successful.
            bindingList.ListChanged -= handler;
            bindingList.AddNew();
            Assert.False(calledListChanged);
        }

        [Fact]
        public void RaiseListChangedEvents_Set_GetReturnsExpected()
        {
            var bindingList = new BindingList<object> { RaiseListChangedEvents = false };
            Assert.False(bindingList.RaiseListChangedEvents);

            bindingList.RaiseListChangedEvents = false;
            Assert.False(bindingList.RaiseListChangedEvents);
        }

        [Fact]
        public void AllowNew_Set_GetReturnsExpected()
        {
            var bindingList = new BindingList<int> { AllowNew = false };
            Assert.False(bindingList.AllowNew);

            bindingList.AllowNew = false;
            Assert.False(bindingList.AllowNew);
        }

        [Fact]
        public void AllowEdit_Set_GetReturnsExpected()
        {
            var bindingList = new BindingList<int> { AllowEdit = false };
            Assert.False(bindingList.AllowEdit);

            bindingList.AllowEdit = false;
            Assert.False(bindingList.AllowEdit);
        }

        [Fact]
        public void AllowRemove_Set_GetReturnsExpected()
        {
            var bindingList = new BindingList<int> { AllowRemove = false };
            Assert.False(bindingList.AllowRemove);

            bindingList.AllowRemove = false;
            Assert.False(bindingList.AllowRemove);
        }

        [Fact]
        public void Clear_Invoke_Success()
        {
            var bindingList = new BindingList<object> { new object(), new object() };

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(-1, e.NewIndex);
            };

            bindingList.Clear();
            Assert.True(calledListChanged);
            Assert.Empty(bindingList);
        }

        [Fact]
        public void Clear_INotifyPropertyChangedItems_RemovesPropertyChangedEventHandlers()
        {
            var item1 = new Item();
            var item2 = new Item();
            var list = new List<Item> { item1, item2, null };
            var bindingList = new BindingList<Item>(list);
            Assert.Equal(1, item1.InvocationList.Length);
            Assert.Equal(1, item2.InvocationList.Length);

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(-1, e.NewIndex);
            };

            bindingList.Clear();
            Assert.True(calledListChanged);
            Assert.Empty(bindingList);

            Assert.Null(item1.InvocationList);
            Assert.Null(item2.InvocationList);
        }

        [Fact]
        public void RemoveAt_INotifyPropertyChangedItems_RemovesPropertyChangedEventHandlers()
        {
            var item = new Item();
            var bindingList = new BindingList<Item> { item };
            Assert.Equal(1, item.InvocationList.Length);

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.ItemDeleted, e.ListChangedType);
                Assert.Equal(0, e.NewIndex);
            };

            bindingList.RemoveAt(0);
            Assert.True(calledListChanged);
            Assert.Empty(bindingList);
            Assert.Null(item.InvocationList);
        }

        [Fact]
        public void ItemSet_Invoke_CallsListChanged()
        {
            var bindingList = new BindingList<int> { 1 };

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.ItemChanged, e.ListChangedType);
                Assert.Equal(0, e.NewIndex);
            };

            bindingList[0] = 2;
            Assert.True(calledListChanged);
            Assert.Equal(2, bindingList[0]);
        }

        [Fact]
        public void ItemSet_INotifyPropertyChangedItem_RemovesPropertyChangedEventHandlers()
        {
            var item1 = new Item();
            var item2 = new Item();
            var bindingList = new BindingList<Item> { item1 };
            Assert.Equal(1, item1.InvocationList.Length);

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.ItemChanged, e.ListChangedType);
                Assert.Equal(0, e.NewIndex);
            };

            bindingList[0] = item2;
            Assert.True(calledListChanged);
            Assert.Equal(item2, bindingList[0]);
            Assert.Null(item1.InvocationList);
            Assert.Equal(1, item2.InvocationList.Length);
        }

        [Fact]
        public void SortProperty_Get_ReturnsNull()
        {
            IBindingList bindingList = new BindingList<object>();
            Assert.Null(bindingList.SortProperty);
        }

        [Fact]
        public void ApplySort_Invoke_ThrowsNotSupportedException()
        {
            IBindingList bindingList = new BindingList<object>();
            Assert.Throws<NotSupportedException>(() => bindingList.ApplySort(null, ListSortDirection.Descending));
        }

        [Fact]
        public void RemoveSort_Invoke_ThrowsNotSupportedException()
        {
            IBindingList bindingList = new BindingList<object>();
            Assert.Throws<NotSupportedException>(() => bindingList.RemoveSort());
        }

        [Fact]
        public void Find_Invoke_ThrowsNotSupportedException()
        {
            IBindingList bindingList = new BindingList<object>();
            Assert.Throws<NotSupportedException>(() => bindingList.Find(null, null));
        }

        [Fact]
        public void AddIndex_RemoveIndex_Nop()
        {
            IBindingList bindingList = new BindingList<object>();
            bindingList.AddIndex(null);
            bindingList.RemoveIndex(null);
        }

        [Fact]
        public void ItemPropertyChanged_RaiseListChangedEventsFalse_InvokesItemChanged()
        {
            var item = new Item();
            var bindingList = new BindingList<Item> { item };

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.ItemChanged, e.ListChangedType);
                Assert.Equal(0, e.NewIndex);
                Assert.Equal("Name", e.PropertyDescriptor.Name);
                Assert.Equal(typeof(string), e.PropertyDescriptor.PropertyType);
            };

            // Invoke once
            item.Name = "name";
            Assert.True(calledListChanged);

            // Invoke twice.
            calledListChanged = false;
            item.Name = "name2";
            Assert.True(calledListChanged);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("sender")]
        public void ItemPropertyChanged_InvalidSender_InvokesReset(object invokeSender)
        {
            var item = new Item();
            var bindingList = new BindingList<Item> { item };

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(-1, e.NewIndex);
            };

            item.InvokePropertyChanged(invokeSender, new PropertyChangedEventArgs("Name"));
            Assert.True(calledListChanged);
        }

        public static IEnumerable<object[]> InvalidEventArgs_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new PropertyChangedEventArgs(null) };
            yield return new object[] { new PropertyChangedEventArgs(string.Empty) };
        }

        [Theory]
        [MemberData(nameof(InvalidEventArgs_TestData))]
        public void ItemPropertyChanged_InvalidEventArgs_InvokesReset(PropertyChangedEventArgs eventArgs)
        {
            var item = new Item();
            var bindingList = new BindingList<Item> { item };

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(-1, e.NewIndex);
            };

            item.InvokePropertyChanged(item, eventArgs);
            Assert.True(calledListChanged);
        }

        [Fact]
        public void InvokePropertyChanged_NoSuchObjectAnymore_InvokesReset()
        {
            var item1 = new Item();
            var item2 = new Item();
            var bindingList = new BindingList<Item> { item1 };

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(-1, e.NewIndex);
            };

            item1.InvokePropertyChanged(item2, new PropertyChangedEventArgs("Name"));
            Assert.True(calledListChanged);
        }

        [Fact]
        public void ItemPropertyChanged_RaiseListChangedEventsFalse_DoesNotInvokeListChanged()
        {
            var item = new Item();
            var bindingList = new BindingList<Item> { item };
            bindingList.RaiseListChangedEvents = false;

            bool calledListChanged = false;
            bindingList.ListChanged += (object sender, ListChangedEventArgs e) => calledListChanged = true;

            item.Name = "name";
            Assert.False(calledListChanged);
        }

        [Fact]
        public void AddingNewCore_ReturnsNull_Success()
        {
            var bindingList = new BindingListWithNullAddCore();
            Assert.Null(bindingList.AddNew());
        }

        private class BindingListWithNullAddCore : BindingList<object>
        {
            protected override object AddNewCore() => null;
        }

        private class BindingListPoker : BindingList<object>
        {
            public object DoAddNewCore() => base.AddNewCore();
        }

        [Fact]
        public void AddNewCore_Invoke_CallsAddingNewAndListChanged()
        {
            var poker = new BindingListPoker();

            bool calledAddingNew = false;
            bool calledListChanged = false;
            ListChangedType listChangedType = ListChangedType.Reset;
            int listChangedIndex = -1;

            poker.AddingNew += (object sender, AddingNewEventArgs e) =>
            {
                calledAddingNew = true;
            };
            poker.ListChanged += (object sender, ListChangedEventArgs e) =>
            {
                calledListChanged = true;
                listChangedType = e.ListChangedType;
                listChangedIndex = e.NewIndex;
            };

            object newValue = poker.DoAddNewCore();
            Assert.True(calledAddingNew);
            Assert.True(calledListChanged);

            Assert.Equal(ListChangedType.ItemAdded, listChangedType);
            Assert.Equal(0, listChangedIndex);
            Assert.Equal(1, poker.Count);
        }

        private class Item : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private string _name;
            public string Name
            {
                get => _name;
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        OnPropertyChanged();
                    }
                }
            }

            public Delegate[] InvocationList => PropertyChanged?.GetInvocationList();

            public void InvokePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                PropertyChanged?.Invoke(sender, e);
            }

            private void OnPropertyChanged()
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        [Fact]
        public void Insert_Null_Success()
        {
            var list = new BindingList<Item>();
            list.Insert(0, null);

            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void ApplySort_ApplySortCoreOverriden_DoesNotThrow()
        {
            IBindingList bindingList = new SubBindingList();
            bindingList.ApplySort(null, ListSortDirection.Descending);
        }

        [Fact]
        public void RemoveSort_RemoveSortCoreOverriden_DoesNotThrow()
        {
            IBindingList bindingList = new SubBindingList();
            bindingList.RemoveSort();
        }

        [Fact]
        public void Find_FindCoreOverriden_DoesNotThrow()
        {
            IBindingList bindingList = new SubBindingList();
            Assert.Equal(200, bindingList.Find(null, null));
        }

        private class SubBindingList : BindingList<object>
        {
            protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction) { }
            protected override void RemoveSortCore() { }
            protected override int FindCore(PropertyDescriptor prop, object key) => 200;
        }
    }
}
