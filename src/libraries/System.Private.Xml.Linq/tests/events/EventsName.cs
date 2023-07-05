// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace CoreXml.Test.XLinq.FunctionalTests.EventsTests
{
    public class EventsXElementName
    {
        public static object[][] ExecuteXElementVariationParams = new object[][] {
            new object[] { new XElement("element"), (XName)"newName" },
            new object[] { new XElement("parent", new XElement("child", "child text")), (XName)"{b}newName" },
        };
        [Theory, MemberData(nameof(ExecuteXElementVariationParams))]
        public void ExecuteXElementVariation(XElement toChange, XName newName)
        {
            XElement original = new XElement(toChange);
            using (UndoManager undo = new UndoManager(toChange))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(toChange))
                {
                    toChange.Name = newName;
                    Assert.True(newName.Namespace == toChange.Name.Namespace, "Namespace did not change");
                    Assert.True(newName.LocalName == toChange.Name.LocalName, "LocalName did not change");
                    eHelper.Verify(XObjectChange.Name, toChange);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(toChange, original), "Undo did not work");
            }
        }

        [Fact]
        public void XProcessingInstructionPIVariation()
        {
            XProcessingInstruction toChange = new XProcessingInstruction("target", "data");
            XProcessingInstruction original = new XProcessingInstruction(toChange);
            using (UndoManager undo = new UndoManager(toChange))
            {
                undo.Group();
                using (EventsHelper eHelper = new EventsHelper(toChange))
                {
                    toChange.Target = "newTarget";
                    Assert.True(toChange.Target.Equals("newTarget"), "Name did not change");
                    eHelper.Verify(XObjectChange.Name, toChange);
                }
                undo.Undo();
                Assert.True(XNode.DeepEquals(toChange, original), "Undo did not work");
            }
        }

        [Fact]
        public void XDocumentTypeDocTypeVariation()
        {
            XDocumentType toChange = new XDocumentType("root", "", "", "");
            XDocumentType original = new XDocumentType(toChange);
            using (EventsHelper eHelper = new EventsHelper(toChange))
            {
                toChange.Name = "newName";
                Assert.True(toChange.Name.Equals("newName"), "Name did not change");
                eHelper.Verify(XObjectChange.Name, toChange);
            }
        }
    }

    public class EventsSpecialCases
    {
        private static void ChangingDelegate(object sender, XObjectChangeEventArgs e) { }
        private static void ChangedDelegate(object sender, XObjectChangeEventArgs e) { }

        [Fact]
        public void AddingRemovingNullListenersXElementRemoveNullEventListener()
        {
            XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
            EventHandler<XObjectChangeEventArgs> d1 = ChangingDelegate;
            EventHandler<XObjectChangeEventArgs> d2 = ChangedDelegate;
            //Add null first, this should add nothing
            xDoc.Changing += null;
            xDoc.Changed += null;
            //Add the actual delegate
            xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(d1);
            xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(d2);
            //Now set it to null
            d1 = null;
            d2 = null;
            xDoc.Root.Add(new XComment("This is a comment"));
            //Remove nulls
            xDoc.Changing -= null;
            xDoc.Changed -= null;
            //Try removing the originally added delegates
            d1 = ChangingDelegate;
            d2 = ChangedDelegate;
            xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
            xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
        }

        [Fact]
        public void RemoveBothEventListenersXElementRemoveBothEventListeners()
        {
            XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
            EventHandler<XObjectChangeEventArgs> d1 = ChangingDelegate;
            EventHandler<XObjectChangeEventArgs> d2 = ChangedDelegate;
            xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(d1);
            xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(d2);
            xDoc.Root.Add(new XComment("This is a comment"));
            xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
            xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
            //Remove it again
            xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
            xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
            //Change the order
            xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(d2);
            xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(d1);
            xDoc.Root.Add(new XComment("This is a comment"));
            xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
            xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
            //Remove it again
            xDoc.Changed -= new EventHandler<XObjectChangeEventArgs>(d2);
            xDoc.Changing -= new EventHandler<XObjectChangeEventArgs>(d1);
        }

        [Fact]
        public void AddChangedListenerInPreEventAddListenerInPreEvent()
        {
            XElement element = XElement.Parse("<root></root>");
            element.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    element.Changed += new EventHandler<XObjectChangeEventArgs>(ChangedDelegate);
                });

            element.Add(new XElement("Add", "Me"));
            element.Verify();
            Assert.True(element.Element("Add") != null, "Did not add the element");
        }

        [Fact]
        public void AddAndRemoveEventListenersXElementAddRemoveEventListeners()
        {
            XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
            EventsHelper docHelper = new EventsHelper(xDoc);
            EventsHelper eHelper = new EventsHelper(xDoc.Root);
            xDoc.Root.Add(new XElement("Add", "Me"));
            docHelper.Verify(XObjectChange.Add);
            eHelper.Verify(XObjectChange.Add);
            eHelper.RemoveListeners();
            xDoc.Root.Add(new XComment("Comment"));
            eHelper.Verify(0);
            docHelper.Verify(XObjectChange.Add);
        }

        [Fact]
        public void AttachListenersAtEachLevelNestedElementsXElementAttachAtEachLevel()
        {
            XDocument xDoc = new XDocument(XElement.Parse(@"<a>a<b>b<c>c<d>c<e>e<f>f</f></e></d></c></b></a>"));
            EventsHelper[] listeners = new EventsHelper[xDoc.Descendants().Count()];

            int i = 0;
            foreach (XElement x in xDoc.Descendants())
                listeners[i++] = new EventsHelper(x);
            // f element
            XElement toChange = xDoc.Descendants().ElementAt(5);
            // Add element
            toChange.Add(new XElement("Add", "Me"));
            foreach (EventsHelper e in listeners)
                e.Verify(XObjectChange.Add);
            // Add xattribute
            toChange.Add(new XAttribute("at", "value"));
            foreach (EventsHelper e in listeners)
                e.Verify(XObjectChange.Add);
        }

        [Fact]
        public void ExceptionInPREEventHandlerXElementPreException()
        {
            XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
            XElement toChange = xDoc.Descendants().ElementAt(5);

            xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    throw new InvalidOperationException("This should be propagated and operation should be aborted");
                });

            xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(
               delegate (object sender, XObjectChangeEventArgs e)
               {
                   // This should never be called
               });

            Assert.Throws<InvalidOperationException>(() => { toChange.Add(new XElement("Add", "Me")); });
            xDoc.Root.Verify();
            Assert.Null(toChange.Element("Add"));
        }

        [Fact]
        public void ExceptionInPOSTEventHandlerXElementPostException()
        {
            XDocument xDoc = new XDocument(InputSpace.GetElement(10, 10));
            XElement toChange = xDoc.Descendants().ElementAt(5);

            xDoc.Changing += new EventHandler<XObjectChangeEventArgs>(
                delegate (object sender, XObjectChangeEventArgs e)
                {
                    // Do nothing
                });

            xDoc.Changed += new EventHandler<XObjectChangeEventArgs>(
               delegate (object sender, XObjectChangeEventArgs e)
               {
                   throw new InvalidOperationException("This should be propagated and operation should perform");
               });

            Assert.Throws<InvalidOperationException>(() => { toChange.Add(new XElement("Add", "Me")); });
            xDoc.Root.Verify();
            Assert.NotNull(toChange.Element("Add"));
        }
    }
}
