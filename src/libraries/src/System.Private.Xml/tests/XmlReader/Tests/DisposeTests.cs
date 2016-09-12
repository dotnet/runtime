// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Xunit;

namespace System.Xml.Tests
{
    public class MyXmlReader : XmlReader
    {
        public MyXmlReader() { IsDisposed = false; }
        public bool IsDisposed { get; private set; }
        protected override void Dispose(bool disposing) { IsDisposed = true; }

        // Implementation of the abstract class
        public override int AttributeCount { get { return default(int); } }
        public override string BaseURI { get { return default(string); } }
        public override int Depth { get { return default(int); } }
        public override bool EOF { get { return default(bool); } }
        public override string GetAttribute(int i) { return default(string); }
        public override string GetAttribute(string name, string namespaceURI) { return default(string); }
        public override string GetAttribute(string name) { return default(string); }
        public override bool IsEmptyElement { get { return default(bool); } }
        public override string LocalName { get { return default(string); } }
        public override string LookupNamespace(string prefix) { return default(string); }
        public override bool MoveToAttribute(string name, string ns) { return default(bool); }
        public override bool MoveToAttribute(string name) { return default(bool); }
        public override bool MoveToElement() { return default(bool); }
        public override bool MoveToFirstAttribute() { return default(bool); }
        public override bool MoveToNextAttribute() { return default(bool); }
        public override XmlNameTable NameTable { get { return default(XmlNameTable); } }
        public override string NamespaceURI { get { return default(string); } }
        public override XmlNodeType NodeType { get { return default(XmlNodeType); } }
        public override string Prefix { get { return default(string); } }
        public override bool Read() { return default(bool); }
        public override bool ReadAttributeValue() { return default(bool); }
        public override ReadState ReadState { get { return default(ReadState); } }
        public override void ResolveEntity() { }
        public override string Value { get { return default(string); } }
    }

    public static class XmlReaderDisposeTests
    {
        public static Stream CreateXmlStream()
        {
            const string xml = @"<?xml version=""1.0""?>
<test>
   <asd id=""testid0"">
      <a>test test</author>
      <b>test'test Test</title>
      <c>12.3</genre>
   </asd>
   <asd id=""testid0"">
      <a>test1 test2</author>
      <b>test3'test4 Test5</title>
      <c>98.7</genre>
   </asd>
</test>";
            MemoryStream ms = new MemoryStream();
            byte[] buffer = Encoding.UTF8.GetBytes(xml);
            ms.Write(buffer, 0, buffer.Length);
            ms.Position = 0;
            return ms;
        }

        [Fact]
        public static void DisposeDisposesInputStream()
        {
            bool[] asyncValues = { false, true };
            bool[] closeInputValues = { false, true };

            foreach (var async in asyncValues)
                foreach (var closeInput in closeInputValues)
                {
                    using (Stream s = CreateXmlStream())
                    {
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.Async = async;
                        settings.CloseInput = closeInput;

                        XmlReader reader = XmlReader.Create(s, settings);
                        if (async)
                        {
                            // Underlying Stream is not being disposed when using async and not reading anything
                            // async is delaying initialization until you start to read (allegedly to not block on IO when creating reader)
                            reader.Read();
                        }
                        reader.Dispose();
                        if (closeInput)
                        {
                            Assert.Throws<ObjectDisposedException>(() =>
                            {
                                s.Position = 0;
                                s.ReadByte();
                            });
                        }
                        else
                        {
                            s.Position = 0;
                            s.ReadByte();
                            // does not throw ObjectDisposedException
                        }

                        // should not throw
                        reader.Dispose();
                    }
                }
        }

        [Fact]
        public static void XmlReaderDisposeWorksWithDerivingClasses()
        {
            MyXmlReader myreader = new MyXmlReader();
            Assert.False(myreader.IsDisposed);
            myreader.Dispose();
            Assert.True(myreader.IsDisposed);
        }
    }
}
