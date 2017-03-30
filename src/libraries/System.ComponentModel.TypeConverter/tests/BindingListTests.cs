// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information. 

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class BindingListTest
    {
        [Fact]
        public void BindingListDefaults()
        {
            BindingList<string> l = new BindingList<string>();
            IBindingList ibl = (IBindingList)l;

            Assert.True(l.AllowEdit, "1");
            Assert.False(l.AllowNew, "2");
            Assert.True(l.AllowRemove, "3");
            Assert.True(l.RaiseListChangedEvents, "4");

            Assert.False(ibl.IsSorted, "5");
            Assert.Equal(ibl.SortDirection, ListSortDirection.Ascending);
            Assert.True(ibl.SupportsChangeNotification, "7");
            Assert.False(ibl.SupportsSearching, "8");
            Assert.False(ibl.SupportsSorting, "9");
            Assert.False(((IRaiseItemChangedEvents)l).RaisesItemChangedEvents, "10");
        }

        [Fact]
        public void BindingListDefaults_FixedSizeList()
        {
            string[] arr = new string[10];
            BindingList<string> l = new BindingList<string>(arr);
            IBindingList ibl = (IBindingList)l;

            Assert.True(l.AllowEdit, "1");
            Assert.False(l.AllowNew, "2");
            Assert.True(l.AllowRemove, "3");
            Assert.True(l.RaiseListChangedEvents, "4");

            Assert.False(ibl.IsSorted, "5");
            Assert.Equal(ibl.SortDirection, ListSortDirection.Ascending);
            Assert.True(ibl.SupportsChangeNotification, "7");
            Assert.False(ibl.SupportsSearching, "8");
            Assert.False(ibl.SupportsSorting, "9");
            Assert.False(((IRaiseItemChangedEvents)l).RaisesItemChangedEvents, "10");
        }

        [Fact]
        public void BindingListDefaults_NonFixedSizeList()
        {
            List<string> list = new List<string>();
            BindingList<string> l = new BindingList<string>(list);
            IBindingList ibl = (IBindingList)l;

            Assert.True(l.AllowEdit, "1");
            Assert.False(l.AllowNew, "2");
            Assert.True(l.AllowRemove, "3");
            Assert.True(l.RaiseListChangedEvents, "4");

            Assert.False(ibl.IsSorted, "5");
            Assert.Equal(ibl.SortDirection, ListSortDirection.Ascending);
            Assert.True(ibl.SupportsChangeNotification, "7");
            Assert.False(ibl.SupportsSearching, "8");
            Assert.False(ibl.SupportsSorting, "9");
            Assert.False(((IRaiseItemChangedEvents)l).RaisesItemChangedEvents, "10");
        }

        [Fact]
        public void BindingListDefaults_ReadOnlyList()
        {
            List<string> list = new List<string>();
            BindingList<string> l = new BindingList<string>(list);
            IBindingList ibl = (IBindingList)l;

            Assert.True(l.AllowEdit, "1");
            Assert.False(l.AllowNew, "2");
            Assert.True(l.AllowRemove, "3");
            Assert.True(l.RaiseListChangedEvents, "4");

            Assert.False(ibl.IsSorted, "5");
            Assert.Equal(ibl.SortDirection, ListSortDirection.Ascending);
            Assert.True(ibl.SupportsChangeNotification, "7");
            Assert.False(ibl.SupportsSearching, "8");
            Assert.False(ibl.SupportsSorting, "9");
            Assert.False(((IRaiseItemChangedEvents)l).RaisesItemChangedEvents, "10");
        }

        [Fact]
        public void TestAllowNew()
        {
            // Object has a default ctor
            BindingList<object> l1 = new BindingList<object>();
            Assert.True(l1.AllowNew, "1");

            // string has no default ctor
            BindingList<string> l2 = new BindingList<string>();
            Assert.False(l2.AllowNew, "2");

            // adding a delegate to AddingNew fixes that
            l2.AddingNew += delegate (object sender, AddingNewEventArgs e) { };
            Assert.True(l2.AllowNew, "3");

            l1 = new BindingList<object>();

            bool list_changed = false;
            bool expected = false;

            l1.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(expected, l1.AllowNew);
            };

            expected = false;
            l1.AllowNew = false;

            Assert.True(list_changed, "7");

            //the default for T=object is true, so check
			//if we enter the block for raising the event
			//if we explicitly set it to the value it
			//currently has.
            l1 = new BindingList<object>();

            list_changed = false;

            l1.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(expected, l1.AllowNew);
            };

            expected = true;
            l1.AllowNew = true;

            //turns out it doesn't raise the event, so the check must only be for "allow_new == value"
            Assert.False(list_changed, "11");
        }

        [Fact]
        public void TestResetBindings()
        {
            BindingList<object> l = new BindingList<object>();

            bool list_changed = false;

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
            };

            l.ResetBindings();

            Assert.True(list_changed, "3");
        }

        [Fact]
        public void TestResetItem()
        {
            List<object> list = new List<object>();
            list.Add(new object());

            BindingList<object> l = new BindingList<object>(list);

            bool item_changed = false;

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                item_changed = true;
                Assert.Equal(0, e.NewIndex);
                Assert.Equal(ListChangedType.ItemChanged, e.ListChangedType);
            };

            l.ResetItem(0);

            Assert.True(item_changed, "3");
        }

        [Fact]
        public void TestRemoveItem()
        {
            List<object> list = new List<object>();
            list.Add(new object());

            BindingList<object> l = new BindingList<object>(list);

            bool item_deleted = false;

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                item_deleted = true;
                Assert.Equal(0, e.NewIndex);
                Assert.Equal(ListChangedType.ItemDeleted, e.ListChangedType);
                Assert.Equal(0, l.Count); // to show the event is raised after the removal
            };

            l.RemoveAt(0);

            Assert.True(item_deleted, "4");
        }

        [Fact]
        public void TestRemoveItem_AllowRemoveFalse()
        {
            List<object> list = new List<object>();
            list.Add(new object());

            BindingList<object> l = new BindingList<object>(list);

            l.AllowRemove = false;

            Assert.Throws<NotSupportedException>(() => l.RemoveAt(0));

        }

        [Fact]
        public void TestAllowEditEvent()
        {
            BindingList<object> l = new BindingList<object>();

            bool event_raised = false;
            bool expected = false;

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                event_raised = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(expected, l.AllowEdit);
            };

            expected = false;
            l.AllowEdit = false;

            Assert.True(event_raised, "4");

            // check to see if RaiseListChangedEvents affects AllowEdit's event.
            l.RaiseListChangedEvents = false;

            event_raised = false;
            expected = true;
            l.AllowEdit = true;

            Assert.False(event_raised, "5");
        }

        [Fact]
        public void TestAllowRemove()
        {
            BindingList<object> l = new BindingList<object>();

            bool event_raised = false;
            bool expected = false;

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                event_raised = true;
                Assert.Equal(-1, e.NewIndex);
                Assert.Equal(ListChangedType.Reset, e.ListChangedType);
                Assert.Equal(expected, l.AllowRemove);
            };

            expected = false;
            l.AllowRemove = false;

            Assert.True(event_raised, "4");

            // check to see if RaiseListChangedEvents affects AllowRemove's event.
            l.RaiseListChangedEvents = false;

            event_raised = false;
            expected = true;
            l.AllowRemove = true;

            Assert.False(event_raised, "5");
        }

        [Fact]
        public void TestAddNew_SettingArgsNewObject()
        {
            BindingList<object> l = new BindingList<object>();

            bool adding_event_raised = false;
            object o = new object();

            l.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                adding_event_raised = true;
                Assert.Null(e.NewObject);
                e.NewObject = o;
            };

            object rv = l.AddNew();
            Assert.True(adding_event_raised, "2");
            Assert.Same(o, rv);
        }

        [Fact]
        public void TestAddNew()
        {
            BindingList<object> l = new BindingList<object>();

            bool adding_event_raised = false;
            object o = new object();

            l.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                adding_event_raised = true;
                Assert.Null(e.NewObject);
            };

            object rv = l.AddNew();
            Assert.True(adding_event_raised, "2");
            Assert.NotNull(rv);
        }

        [Fact]
        public void TestAddNew_Cancel()
        {
            BindingList<object> l = new BindingList<object>();

            bool adding_event_raised = false;
            object o = new object();

            bool list_changed = false;
            ListChangedType change_type = ListChangedType.Reset;
            int list_changed_index = -1;

            l.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                adding_event_raised = true;
                Assert.Null(e.NewObject);
            };

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                change_type = e.ListChangedType;
                list_changed_index = e.NewIndex;
            };

            object rv = l.AddNew();
            Assert.True(adding_event_raised, "2");
            Assert.NotNull(rv);

            Assert.Equal(1, l.Count);
            Assert.Equal(0, l.IndexOf(rv));
            Assert.True(list_changed, "6");
            Assert.Equal(ListChangedType.ItemAdded, change_type);
            Assert.Equal(0, list_changed_index);

            list_changed = false;

            l.CancelNew(0);

            Assert.Equal(0, l.Count);
            Assert.True(list_changed, "10");
            Assert.Equal(ListChangedType.ItemDeleted, change_type);
            Assert.Equal(0, list_changed_index);
        }

        [Fact]
        public void TestAddNew_CancelDifferentIndex()
        {
            List<object> list = new List<object>();

            list.Add(new object());
            list.Add(new object());

            BindingList<object> l = new BindingList<object>(list);

            bool adding_event_raised = false;
            object o = new object();

            bool list_changed = false;
            ListChangedType change_type = ListChangedType.Reset;
            int list_changed_index = -1;

            l.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                adding_event_raised = true;
                Assert.Null(e.NewObject);
            };

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                change_type = e.ListChangedType;
                list_changed_index = e.NewIndex;
            };

            object rv = l.AddNew();
            Assert.True(adding_event_raised, "2");
            Assert.NotNull(rv);

            Assert.Equal(3, l.Count);
            Assert.Equal(2, l.IndexOf(rv));
            Assert.True(list_changed, "6");
            Assert.Equal(ListChangedType.ItemAdded, change_type);
            Assert.Equal(2, list_changed_index);

            list_changed = false;

            l.CancelNew(0);

            Assert.False(list_changed, "9");
            Assert.Equal(3, l.Count);

            l.CancelNew(2);

            Assert.True(list_changed, "11");
            Assert.Equal(ListChangedType.ItemDeleted, change_type);
            Assert.Equal(2, list_changed_index);
            Assert.Equal(2, l.Count);
        }

        [Fact]
        public void TestAddNew_End()
        {
            BindingList<object> l = new BindingList<object>();

            bool adding_event_raised = false;
            object o = new object();

            bool list_changed = false;
            ListChangedType change_type = ListChangedType.Reset;
            int list_changed_index = -1;

            l.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                adding_event_raised = true;
                Assert.Null(e.NewObject);
            };

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                change_type = e.ListChangedType;
                list_changed_index = e.NewIndex;
            };

            object rv = l.AddNew();
            Assert.True(adding_event_raised, "2");
            Assert.NotNull(rv);

            Assert.Equal(1, l.Count);
            Assert.Equal(0, l.IndexOf(rv));
            Assert.True(list_changed, "6");
            Assert.Equal(ListChangedType.ItemAdded, change_type);
            Assert.Equal(0, list_changed_index);

            list_changed = false;

            l.EndNew(0);

            Assert.Equal(1, l.Count);
            Assert.False(list_changed, "10");
        }

        [Fact]
        public void TestAddNew_CancelDifferentIndexThenEnd()
        {
            BindingList<object> l = new BindingList<object>();

            bool adding_event_raised = false;
            object o = new object();

            bool list_changed = false;
            ListChangedType change_type = ListChangedType.Reset;
            int list_changed_index = -1;

            l.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                adding_event_raised = true;
                Assert.Null(e.NewObject);
            };

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                change_type = e.ListChangedType;
                list_changed_index = e.NewIndex;
            };

            object rv = l.AddNew();
            Assert.True(adding_event_raised, "2");
            Assert.NotNull(rv);

            Assert.Equal(1, l.Count);
            Assert.Equal(0, l.IndexOf(rv));
            Assert.True(list_changed, "6");
            Assert.Equal(ListChangedType.ItemAdded, change_type);
            Assert.Equal(0, list_changed_index);

            list_changed = false;

            l.CancelNew(2);

            Assert.Equal(1, l.Count);
            Assert.False(list_changed, "10");

            l.EndNew(0);

            Assert.Equal(1, l.Count);
            Assert.False(list_changed, "12");
        }

        [Fact]
        public void TestAddNew_EndDifferentIndexThenCancel()
        {
            BindingList<object> l = new BindingList<object>();

            bool adding_event_raised = false;
            object o = new object();

            bool list_changed = false;
            ListChangedType change_type = ListChangedType.Reset;
            int list_changed_index = -1;

            l.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                adding_event_raised = true;
                Assert.Null(e.NewObject);
            };

            l.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                change_type = e.ListChangedType;
                list_changed_index = e.NewIndex;
            };

            object rv = l.AddNew();
            Assert.True(adding_event_raised, "2");
            Assert.NotNull(rv);

            Assert.Equal(1, l.Count);
            Assert.Equal(0, l.IndexOf(rv));
            Assert.True(list_changed, "6");
            Assert.Equal(ListChangedType.ItemAdded, change_type);
            Assert.Equal(0, list_changed_index);

            list_changed = false;

            l.EndNew(2);

            Assert.Equal(1, l.Count);
            Assert.False(list_changed, "10");

            l.CancelNew(0);

            Assert.True(list_changed, "11");
            Assert.Equal(ListChangedType.ItemDeleted, change_type);
            Assert.Equal(0, list_changed_index);
        }

        class BindingListPoker : BindingList<object>
        {
            public object DoAddNewCore()
            {
                return base.AddNewCore();
            }
        }

        // test to make sure that the events are raised in AddNewCore and not in AddNew
        [Fact]
        public void TestAddNewCore_Insert()
        {
            BindingListPoker poker = new BindingListPoker();

            bool adding_event_raised = false;

            bool list_changed = false;
            ListChangedType change_type = ListChangedType.Reset;
            int list_changed_index = -1;

            poker.AddingNew += delegate (object sender, AddingNewEventArgs e)
            {
                adding_event_raised = true;
            };

            poker.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                list_changed = true;
                change_type = e.ListChangedType;
                list_changed_index = e.NewIndex;
            };

            object o = poker.DoAddNewCore();

            Assert.True(adding_event_raised, "1");
            Assert.True(list_changed, "2");
            Assert.Equal(ListChangedType.ItemAdded, change_type);
            Assert.Equal(0, list_changed_index);
            Assert.Equal(1, poker.Count);
        }

        private class Item : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler PropertyChanged;

            string _name;

            public string Name
            {
                get { return _name; }
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        OnPropertyChanged("Name");
                    }
                }
            }

            void OnPropertyChanged(string name)
            {
                var fn = PropertyChanged;
                if (fn != null)
                    fn(this, new PropertyChangedEventArgs(name));
            }
        }

        [Fact]
        public void Test_InsertNull()
        {
            var list = new BindingList<Item>();
            list.Insert(0, null);
            var count = list.Count;

            Assert.Equal(1, count);
        }

        private class Person : INotifyPropertyChanged
        {
            private string _lastName;
            private string _firstName;

            public string FirstName
            {
                get { return _firstName; }
                set
                {
                    _firstName = value;
                    OnPropertyChanged("FirstName"); // string matches property name
                }
            }

            public string LastName
            {
                get { return _lastName; }
                set
                {
                    _lastName = value;
                    OnPropertyChanged("NotTheName"); // string doesn't match property name
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName = null)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
