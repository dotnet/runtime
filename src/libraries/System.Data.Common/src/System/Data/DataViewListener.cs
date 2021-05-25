// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    internal sealed class DataViewListener
    {
        private readonly WeakReference _dvWeak;
        private DataTable? _table;
        private Index? _index;

        /// <summary><see cref="DataView.ObjectID"/></summary>
        internal readonly int _objectID;

        internal DataViewListener(DataView dv)
        {
            _objectID = dv.ObjectID;
            _dvWeak = new WeakReference(dv);
        }

        private void ChildRelationCollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            DataView? dv = (DataView?)_dvWeak.Target;
            if (dv != null)
            {
                dv.ChildRelationCollectionChanged(sender, e);
            }
            else
            {
                CleanUp(true);
            }
        }

        private void ParentRelationCollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            DataView? dv = (DataView?)_dvWeak.Target;
            if (dv != null)
            {
                dv.ParentRelationCollectionChanged(sender, e);
            }
            else
            {
                CleanUp(true);
            }
        }

        private void ColumnCollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            DataView? dv = (DataView?)_dvWeak.Target;
            if (dv != null)
            {
                dv.ColumnCollectionChangedInternal(sender, e);
            }
            else
            {
                CleanUp(true);
            }
        }

        /// <summary>
        /// Maintain the DataView before <see cref="DataView.ListChanged"/> is raised.
        /// </summary>
        internal void MaintainDataView(ListChangedType changedType, DataRow? row, bool trackAddRemove)
        {
            DataView? dv = (DataView?)_dvWeak.Target;
            if (dv != null)
            {
                dv.MaintainDataView(changedType, row, trackAddRemove);
            }
            else
            {
                CleanUp(true);
            }
        }

        internal void IndexListChanged(ListChangedEventArgs e)
        {
            DataView? dv = (DataView?)_dvWeak.Target;
            if (dv != null)
            {
                dv.IndexListChangedInternal(e);
            }
            else
            {
                CleanUp(true);
            }
        }

        internal void RegisterMetaDataEvents(DataTable? table)
        {
            Debug.Assert(null == _table, "DataViewListener already registered table");
            _table = table;
            if (table != null)
            {
                // actively remove listeners without a target
                RegisterListener(table);

                // start listening to events
                var handlerCollection = new CollectionChangeEventHandler(ColumnCollectionChanged);
                table.Columns.ColumnPropertyChanged += handlerCollection;
                table.Columns.CollectionChanged += handlerCollection;

                var handlerChildRelation = new CollectionChangeEventHandler(ChildRelationCollectionChanged);
                ((DataRelationCollection.DataTableRelationCollection)(table.ChildRelations)).RelationPropertyChanged += handlerChildRelation;
                table.ChildRelations.CollectionChanged += handlerChildRelation;

                var handlerParentRelation = new CollectionChangeEventHandler(ParentRelationCollectionChanged);
                ((DataRelationCollection.DataTableRelationCollection)(table.ParentRelations)).RelationPropertyChanged += handlerParentRelation;
                table.ParentRelations.CollectionChanged += handlerParentRelation;
            }
        }

        internal void UnregisterMetaDataEvents() => UnregisterMetaDataEvents(true);

        private void UnregisterMetaDataEvents(bool updateListeners)
        {
            DataTable? table = _table;
            _table = null;

            if (table != null)
            {
                CollectionChangeEventHandler handlerCollection = new CollectionChangeEventHandler(ColumnCollectionChanged);
                table.Columns.ColumnPropertyChanged -= handlerCollection;
                table.Columns.CollectionChanged -= handlerCollection;

                CollectionChangeEventHandler handlerChildRelation = new CollectionChangeEventHandler(ChildRelationCollectionChanged);
                ((DataRelationCollection.DataTableRelationCollection)(table.ChildRelations)).RelationPropertyChanged -= handlerChildRelation;
                table.ChildRelations.CollectionChanged -= handlerChildRelation;

                CollectionChangeEventHandler handlerParentRelation = new CollectionChangeEventHandler(ParentRelationCollectionChanged);
                ((DataRelationCollection.DataTableRelationCollection)(table.ParentRelations)).RelationPropertyChanged -= handlerParentRelation;
                table.ParentRelations.CollectionChanged -= handlerParentRelation;

                if (updateListeners)
                {
                    List<DataViewListener> listeners = table.GetListeners();
                    lock (listeners)
                    {
                        listeners.Remove(this);
                    }
                }
            }
        }

        internal void RegisterListChangedEvent(Index index)
        {
            Debug.Assert(null == _index, "DataviewListener already registered index");
            _index = index;
            if (null != index)
            {
                lock (index)
                {
                    index.AddRef();
                    index.ListChangedAdd(this);
                }
            }
        }

        internal void UnregisterListChangedEvent()
        {
            Index? index = _index;
            _index = null;

            if (index != null)
            {
                lock (index)
                {
                    index.ListChangedRemove(this);
                    if (index.RemoveRef() <= 1)
                    {
                        index.RemoveRef();
                    }
                }
            }
        }

        private void CleanUp(bool updateListeners)
        {
            UnregisterMetaDataEvents(updateListeners);
            UnregisterListChangedEvent();
        }

        private void RegisterListener(DataTable table)
        {
            List<DataViewListener> listeners = table.GetListeners();
            lock (listeners)
            {
                for (int i = listeners.Count - 1; 0 <= i; --i)
                {
                    DataViewListener listener = listeners[i];
                    if (!listener._dvWeak.IsAlive)
                    {
                        listeners.RemoveAt(i);
                        listener.CleanUp(false);
                    }
                }
                listeners.Add(this);
            }
        }
    }
}
