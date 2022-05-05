// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// EventsHelper Class: Used for event registration and notification for all event testing.
    /// </summary>
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public class EventsHelper : IDisposable
    {
        private XObject _root;
        private EventItem _pending;
        private Queue<EventItem> _events;
        private IEqualityComparer _nodeComparer = XNode.EqualityComparer;
        private XAttributeEqualityComparer<XAttribute> _attributeComparer = new XAttributeEqualityComparer<XAttribute>();

        public EventsHelper(XObject x)
        {
            _root = x;
            _root.Changing += new EventHandler<XObjectChangeEventArgs>(Changing);
            _root.Changed += new EventHandler<XObjectChangeEventArgs>(Changed);
            _events = new Queue<EventItem>();
        }

        public void RemoveListners()
        {
            _root.Changing -= new EventHandler<XObjectChangeEventArgs>(Changing);
            _root.Changed -= new EventHandler<XObjectChangeEventArgs>(Changed);
        }

        public void Dispose()
        {
            this.RemoveListners();
        }

        public void Changing(object sender, XObjectChangeEventArgs e)
        {
            if (_pending != null)
                throw new NotImplementedException();
            _pending = new EventItem((XObject)sender, e);
        }

        public void Changed(object sender, XObjectChangeEventArgs e)
        {
            TestLog.Compare(_pending.Sender, sender, "Mismatch in changed and changing events");
            TestLog.Compare(_pending.EventArgs, e, "Mismatch in changed and changing events");
            _events.Enqueue(_pending);
            _pending = null;
        }

        // If all you care about is number of expected events and not the type
        public void Verify(int expectedCount)
        {
            TestLog.Compare(_events.Count, expectedCount, "Mismatch in expected number of events");
            _events.Clear();
        }

        // Single event of specified type expected
        public void Verify(XObjectChange expectedEvent)
        {
            Verify(expectedEvent, 1);
        }

        // Number of events of a certain type are expected to be thrown
        public void Verify(XObjectChange expectedEvent, int expectedCount)
        {
            TestLog.Compare(_events.Count, expectedCount, "Mismatch in expected number of events");
            while (_events.Count > 0)
            {
                EventItem item = _events.Dequeue();
                TestLog.Compare(item.EventArgs.ObjectChange, expectedEvent, "Event Type Mismatch");
            }
        }

        // Know exactly what events should be thrown and in what order
        public void Verify(XObjectChange[] expectedEvents)
        {
            TestLog.Compare(_events.Count, expectedEvents.Length, "Mismatch in expected number of events");
            int i = 0;
            while (_events.Count > 0)
            {
                EventItem item = _events.Dequeue();
                TestLog.Compare(item.EventArgs.ObjectChange, expectedEvents[i], "Event Type Mismatch");
                i++;
            }
        }

        // Single event and object
        public void Verify(XObjectChange expectedEvent, object expectedObject)
        {
            TestLog.Compare(_events.Count, 1, "Mismatch in expected number of events");
            EventItem item = _events.Dequeue();
            TestLog.Compare(item.EventArgs.ObjectChange, expectedEvent, "Event Type Mismatch");
            TestLog.Compare(item.Sender, (XObject)expectedObject, "Object Mismatch");
        }

        // Same event for many different objects
        public void Verify(XObjectChange expectedEvent, object[] expectedObjects)
        {
            TestLog.Compare(_events.Count, expectedObjects.Length, "Mismatch in expected number of events");
            int i = 0;
            while (_events.Count > 0)
            {
                EventItem item = _events.Dequeue();
                TestLog.Compare(item.EventArgs.ObjectChange, expectedEvent, "Event Type Mismatch");
                TestLog.Compare(item.Sender, (XObject)expectedObjects[i], "Object Mismatch");
                i++;
            }
        }

        // One event for one object
        public void Verify(XObjectChange expectedEvent, XObject expectedObject)
        {
            TestLog.Compare(_events.Count, 1, "Mismatch in expected number of events");
            EventItem item = _events.Dequeue();
            TestLog.Compare(item.EventArgs.ObjectChange, expectedEvent, "Event Type Mismatch");
            if (item.Sender is XAttribute)
                TestLog.Compare(_attributeComparer.Equals((XAttribute)item.Sender, (XAttribute)expectedObject), "Attribute Mismatch");
            else
                TestLog.Compare(_nodeComparer.Equals((XNode)item.Sender, expectedObject), "Node Mismatch");
        }

        // Same event for many different XNodes
        public void Verify(XObjectChange expectedEvent, XObject[] expectedObjects)
        {
            TestLog.Compare(_events.Count, expectedObjects.Length, "Mismatch in expected number of events");
            int i = 0;
            while (_events.Count > 0)
            {
                EventItem item = _events.Dequeue();
                TestLog.Compare(item.EventArgs.ObjectChange, expectedEvent, "Event Type Mismatch");
                if (item.Sender is XAttribute)
                    TestLog.Compare(_attributeComparer.Equals((XAttribute)item.Sender, (XAttribute)expectedObjects[i]), "Attribute Mismatch");
                else
                    TestLog.Compare(_nodeComparer.Equals((XNode)item.Sender, expectedObjects[i]), "Node Mismatch");
                i++;
            }
        }

        // Different events for different objects
        public void Verify(XObjectChange[] expectedEvents, XObject[] expectedObjects)
        {
            TestLog.Compare(_events.Count, expectedEvents.Length, "Mismatch in expected number of events");
            int i = 0;
            while (_events.Count > 0)
            {
                EventItem item = _events.Dequeue();
                TestLog.Compare(item.EventArgs.ObjectChange, expectedEvents[i], "Event Type Mismatch");
                if (item.Sender is XAttribute)
                    TestLog.Compare(_attributeComparer.Equals((XAttribute)item.Sender, (XAttribute)expectedObjects[i]), "Attribute Mismatch");
                else
                    TestLog.Compare(_nodeComparer.Equals((XNode)item.Sender, expectedObjects[i]), "Node Mismatch");
                i++;
            }
        }
    }

    class EventItem
    {
        private XObject _sender;
        private XObjectChangeEventArgs _eventArgs;

        public EventItem(XObject sender, XObjectChangeEventArgs eventArgs)
        {
            _sender = sender;
            _eventArgs = eventArgs;
        }

        public XObject Sender
        {
            get { return _sender; }
        }

        public XObjectChangeEventArgs EventArgs
        {
            get { return _eventArgs; }
        }
    }


    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// UndoManager provided by the product team
    /// </summary>
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public class UndoManager : IDisposable
    {
        private XObject _root;
        private UndoUnit _pending;
        private Stack<UndoUnit> _undos;
        private Stack<UndoUnit> _redos;
        private bool _undoing;
        private bool _redoing;
        private int _lastGroup;

        public UndoManager(XObject root)
        {
            ArgumentNullException.ThrowIfNull(root);

            _root = root;
            _root.Changing += new EventHandler<XObjectChangeEventArgs>(Changing);
            _root.Changed += new EventHandler<XObjectChangeEventArgs>(Changed);
            _undos = new Stack<UndoUnit>();
            _redos = new Stack<UndoUnit>();
        }

        public void Dispose()
        {
            _root.Changing -= new EventHandler<XObjectChangeEventArgs>(Changing);
            _root.Changed -= new EventHandler<XObjectChangeEventArgs>(Changed);
        }

        public void Group()
        {
            if (!_undoing && !_redoing)
            {
                _redos.Clear();
            }
            _lastGroup++;
        }

        public void Undo()
        {
            try
            {
                _undoing = true;
                if (_undos.Count > 0)
                {
                    Group();
                    UndoUnit unit;
                    do
                    {
                        unit = _undos.Pop();
                        unit.Undo();
                    } while (_undos.Count > 0 && _undos.Peek().Group == unit.Group);
                }
            }
            finally
            {
                _undoing = false;
            }
        }

        public void Redo()
        {
            try
            {
                _redoing = true;
                if (_redos.Count > 0)
                {
                    Group();
                    UndoUnit unit;
                    do
                    {
                        unit = _redos.Pop();
                        unit.Undo();
                    } while (_redos.Count > 0 && _redos.Peek().Group == unit.Group);
                }
            }
            finally
            {
                _redoing = false;
            }
        }

        public override string ToString()
        {
            return "(" + _lastGroup.ToString() + "):\n" + _root.ToString();
        }

        void Changing(object sender, XObjectChangeEventArgs e)
        {
            if (_pending != null) throw new NotImplementedException();
            switch (e.ObjectChange)
            {
                case XObjectChange.Add:
                    _pending = new AddUnit((XObject)sender);
                    break;
                case XObjectChange.Remove:
                    XContainer parent = ((XObject)sender).Parent;
                    if (parent == null)
                    {
                        parent = ((XObject)sender).Document;
                    }
                    XObject next = null;
                    if (sender is XNode)
                    {
                        next = ((XNode)sender).NextNode;
                    }
                    else if (sender is XAttribute)
                    {
                        next = ((XAttribute)sender).NextAttribute;
                    }
                    _pending = new RemoveUnit((XObject)sender, parent, next);
                    break;
                case XObjectChange.Name:
                    object name = null;
                    if (sender is XNode)
                    {
                        switch (((XNode)sender).NodeType)
                        {
                            case XmlNodeType.Element:
                                name = ((XElement)sender).Name;
                                break;
                            case XmlNodeType.ProcessingInstruction:
                                name = ((XProcessingInstruction)sender).Target;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    _pending = new NameUnit((XNode)sender, name);
                    break;
                case XObjectChange.Value:
                    string value = null;
                    if (sender is XNode)
                    {
                        switch (((XNode)sender).NodeType)
                        {
                            case XmlNodeType.Element:
                                value = ((XElement)sender).IsEmpty ? null : string.Empty;
                                break;
                            case XmlNodeType.Text:
                            case XmlNodeType.CDATA:
                                value = ((XText)sender).Value;
                                break;
                            case XmlNodeType.ProcessingInstruction:
                                value = ((XProcessingInstruction)sender).Data;
                                break;
                            case XmlNodeType.Comment:
                                value = ((XComment)sender).Value;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else if (sender is XAttribute)
                    {
                        value = ((XAttribute)sender).Value;
                    }
                    _pending = new ValueUnit((XObject)sender, value);
                    break;
            }
        }

        void Changed(object sender, XObjectChangeEventArgs e)
        {
            _pending.Group = _lastGroup;
            if (_undoing)
            {
                _redos.Push(_pending);
            }
            else
            {
                _undos.Push(_pending);
            }
            _pending = null;
        }
    }

    abstract class UndoUnit
    {
        private int _group;

        public UndoUnit()
        {
        }

        public int Group
        {
            get { return _group; }
            set { _group = value; }
        }

        public abstract void Undo();
    }

    class AddUnit : UndoUnit
    {
        private XObject _sender;

        public AddUnit(XObject sender)
        {
            _sender = sender;
        }

        public override void Undo()
        {
            if (_sender is XNode)
            {
                ((XNode)_sender).Remove();
                return;
            }
            if (_sender is XAttribute)
            {
                ((XAttribute)_sender).Remove();
            }
        }
    }

    class RemoveUnit : UndoUnit
    {
        private XObject _sender;
        private XContainer _parent;
        private XObject _next;

        public RemoveUnit(XObject sender, XContainer parent, XObject next)
        {
            _sender = sender;
            _parent = parent;
            _next = next;
        }

        public override void Undo()
        {
            if (_sender is XNode)
            {
                if (_next is XNode)
                {
                    ((XNode)_next).AddBeforeSelf(((XNode)_sender));
                }
                else
                {
                    _parent.Add((XNode)_sender);
                }
                return;
            }
            if (_sender is XAttribute)
            {
                _parent.Add((XAttribute)_sender);
            }
        }
    }

    class NameUnit : UndoUnit
    {
        private XNode _sender;
        private object _name;

        public NameUnit(XNode sender, object name)
        {
            _sender = sender;
            _name = name;
        }

        public override void Undo()
        {
            switch (_sender.NodeType)
            {
                case XmlNodeType.Element:
                    ((XElement)_sender).Name = (XName)_name;
                    break;
                case XmlNodeType.ProcessingInstruction:
                    ((XProcessingInstruction)_sender).Target = (string)_name;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    class ValueUnit : UndoUnit
    {
        private XObject _sender;
        private string _value;

        public ValueUnit(XObject sender, string value)
        {
            _sender = sender;
            _value = value;
        }

        public override void Undo()
        {
            if (_sender is XNode)
            {
                switch (((XNode)_sender).NodeType)
                {
                    case XmlNodeType.Element:
                        if (_value == null)
                        {
                            ((XElement)_sender).RemoveNodes();
                        }
                        else
                        {
                            ((XElement)_sender).Add(string.Empty);
                        }
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        ((XText)_sender).Value = _value;
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        ((XProcessingInstruction)_sender).Data = _value;
                        break;
                    case XmlNodeType.Comment:
                        ((XComment)_sender).Value = _value;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                return;
            }
            if (_sender is XAttribute)
            {
                ((XAttribute)_sender).Value = (string)_value;
            }
        }
    }
}
