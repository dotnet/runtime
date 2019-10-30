// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Tests
{
    // CalledResolveUri Event Args
    public sealed class CalledResolveUriEventArgs : XmlTestResolverEventArgs
    {
        private Uri _baseUri;
        private string _relativeUri;

        internal CalledResolveUriEventArgs(Uri baseUri, string relativeUri)
            : base()
        {
            _baseUri = baseUri;
            _relativeUri = relativeUri;
        }

        public override ResolverEvent EventType
        {
            get { return ResolverEvent.CalledResolveUri; }
        }

        public Uri BaseUri
        {
            get { return _baseUri; }
        }

        public string RelativeUri
        {
            get { return _relativeUri; }
        }
    }

    // CalledGetEntity Event Args
    public sealed class CalledGetEntityEventArgs : XmlTestResolverEventArgs
    {
        private Uri _absoluteUri;
        private string _role;
        private Type _ofObjectToReturn;

        internal CalledGetEntityEventArgs(Uri absoluteUri, string role, Type ofObjectToReturn)
            : base()
        {
            _absoluteUri = absoluteUri;
            _role = role;
            _ofObjectToReturn = ofObjectToReturn;
        }

        public override ResolverEvent EventType
        {
            get { return ResolverEvent.CalledGetEntity; }
        }

        public Uri AbsoluteUri
        {
            get { return _absoluteUri; }
        }

        public string Role
        {
            get { return _role; }
        }

        public Type OfObjectToReturn
        {
            get { return _ofObjectToReturn; }
        }
    }

    // -----------------
    // Delegates for events
    // -----------------
    public delegate void XmlTestResolverEventHandler(object sender, XmlTestResolverEventArgs args);

    public class CXmlTestResolver : XmlResolver
    {
        private XmlResolver _resolver;

        // -----------------
        // Constructors
        // -----------------
        public CXmlTestResolver()
            : base()
        {
            _resolver = new XmlUrlResolver();
        }

        public CXmlTestResolver(string securityUri)
            : base()
        {
            _resolver = new XmlSecureResolver(new XmlUrlResolver(), securityUri);
        }

        // -----------------
        // Events
        // -----------------
        public event XmlTestResolverEventHandler CalledResolveUri;

        public event XmlTestResolverEventHandler CalledGetEntity;

        // -----------------
        // Resolve URI
        // -----------------
        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            // Fire the CalledResolveUri event
            CalledResolveUri(this, new CalledResolveUriEventArgs(baseUri, relativeUri));

            return _resolver.ResolveUri(baseUri, relativeUri);
        }

        // -----------------
        // Get Entity
        // -----------------
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            // Fire the CalledGetEntity event
            CalledGetEntity(this, new CalledGetEntityEventArgs(absoluteUri, role, ofObjectToReturn));

            return _resolver.GetEntity(absoluteUri, role, ofObjectToReturn);
        }
    }
}