// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml.Schema;

namespace System.Xml.Tests
{
    internal class ObservedNameTable : NameTable
    {
        public bool IsAddCalled = false;
        public bool IsGetCalled = false;

        public override string Get(char[] array, int offset, int length)
        {
            IsGetCalled = true;
            return base.Get(array, offset, length);
        }

        public override string Get(string array)
        {
            IsGetCalled = true;
            return base.Get(array);
        }

        public override string Add(char[] array, int offset, int length)
        {
            IsAddCalled = true;
            return base.Add(array, offset, length);
        }

        public override string Add(string array)
        {
            IsAddCalled = true;
            return base.Add(array);
        }
    }

    internal class ObservedNamespaceManager : XmlNamespaceManager
    {
        public ObservedNamespaceManager(XmlNameTable nt)
            : base(nt)
        {
        }

        public bool IsLookupNamespaceCalled = false;

        public override string LookupNamespace(string prefix)
        {
            IsLookupNamespaceCalled = true;
            return base.LookupNamespace(prefix);
        }

        public bool IsLookupPrefixCalled = false;

        public override string LookupPrefix(string uri)
        {
            IsLookupPrefixCalled = true;
            return base.LookupPrefix(uri);
        }

        public bool IsGetNamespacesInScopeCalled = false;

        public override IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            IsGetNamespacesInScopeCalled = true;
            return base.GetNamespacesInScope(scope);
        }
    }

    public enum ResolverEvent
    {
        SetCredentials,         // Set Credentials
        CalledResolveUri,       // Called ResolveUri
        CalledGetEntity,        // Called GetEntity
    };

    // -----------------
    // Event Arguments
    // -----------------
    public abstract class XmlTestResolverEventArgs : EventArgs
    {
        public abstract ResolverEvent EventType { get; }
    }

    internal class CResolverHolder
    {
        public bool IsCalledResolveUri = false;
        public bool IsCalledGetEntity = false;

        public void CallBackResolveUri(object sender, XmlTestResolverEventArgs args)
        {
            IsCalledResolveUri = true;
        }

        public void CallBackGetEntity(object sender, XmlTestResolverEventArgs args)
        {
            IsCalledGetEntity = true;
        }
    }

    internal class CDummyLineInfo : IXmlLineInfo
    {
        private int m_Number, m_Position;

        public CDummyLineInfo(int lineNumber, int linePosition)
        {
            m_Number = lineNumber;
            m_Position = linePosition;
        }

        public bool HasLineInfo()
        {
            return true;
        }

        public int LineNumber
        {
            get { return m_Number; }
        }

        public int LinePosition
        {
            get { return m_Position; }
        }
    }

    internal class CValidationEventHolder
    {
        public object lastObjectSent = null;
        public bool IsCalledA = false;
        public bool IsCalledB = false;
        public int NestingDepth = 0;
        public XmlSeverityType lastSeverity;
        public XmlSchemaValidationException lastException;

        public void CallbackA(object sender, ValidationEventArgs args)
        {
            lastObjectSent = sender;
            IsCalledA = true;
            lastSeverity = args.Severity;
            lastException = args.Exception as XmlSchemaValidationException;
        }

        public void CallbackB(object sender, ValidationEventArgs args)
        {
            lastObjectSent = sender;
            IsCalledB = true;
            lastSeverity = args.Severity;
            lastException = args.Exception as XmlSchemaValidationException;
        }

        public void CallbackNested(object sender, ValidationEventArgs args)
        {
            XmlSchemaInfo info = new XmlSchemaInfo();

            (sender as XmlSchemaValidator).SkipToEndElement(info);
            if (NestingDepth < 3)
            {
                NestingDepth++;
                (sender as XmlSchemaValidator).ValidateElement("bar", "", info);
            }
        }
    }
}