// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public abstract class WebRequest
    {
        internal class WebRequestPrefixElement
        {
            public string Prefix;
            public IWebRequestCreate Creator;

            public WebRequestPrefixElement(string P, IWebRequestCreate C)
            {
                Prefix = P;
                Creator = C;
            }
        }

        private static volatile List<WebRequestPrefixElement> s_prefixList;
        private static object s_internalSyncObject = new object();

        // Create a WebRequest.
        //
        // This is the main creation routine. We take a Uri object, look
        // up the Uri in the prefix match table, and invoke the appropriate
        // handler to create the object. We also have a parameter that
        // tells us whether or not to use the whole Uri or just the
        // scheme portion of it.
        //
        // Input:
        //     requestUri - Uri object for request.
        //     useUriBase - True if we're only to look at the scheme portion of the Uri.
        //
        // Returns:
        //     Newly created WebRequest.
        private static WebRequest Create(Uri requestUri, bool useUriBase)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Enter(NetEventSource.ComponentType.Requests, "WebRequest", "Create", requestUri.ToString());
            }

            string LookupUri;
            WebRequestPrefixElement Current = null;
            bool Found = false;

            if (!useUriBase)
            {
                LookupUri = requestUri.AbsoluteUri;
            }
            else
            {
                // schemes are registered as <schemeName>":", so add the separator
                // to the string returned from the Uri object
                LookupUri = requestUri.Scheme + ':';
            }

            int LookupLength = LookupUri.Length;

            // Copy the prefix list so that if it is updated it will
            // not affect us on this thread.

            List<WebRequestPrefixElement> prefixList = PrefixList;

            // Look for the longest matching prefix.

            // Walk down the list of prefixes. The prefixes are kept longest
            // first. When we find a prefix that is shorter or the same size
            // as this Uri, we'll do a compare to see if they match. If they
            // do we'll break out of the loop and call the creator.

            for (int i = 0; i < prefixList.Count; i++)
            {
                Current = prefixList[i];

                // See if this prefix is short enough.

                if (LookupLength >= Current.Prefix.Length)
                {
                    // It is. See if these match.
                    if (String.Compare(Current.Prefix,
                                       0,
                                       LookupUri,
                                       0,
                                       Current.Prefix.Length,
                                       StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // These match. Remember that we found it and break
                        // out.
                        Found = true;
                        break;
                    }
                }
            }

            WebRequest webRequest = null;

            if (Found)
            {
                // We found a match, so just call the creator and return what it does.
                webRequest = Current.Creator.Create(requestUri);
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Exit(NetEventSource.ComponentType.Requests, "WebRequest", "Create", webRequest);
                }
                return webRequest;
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Exit(NetEventSource.ComponentType.Requests, "WebRequest", "Create", null);
            }

            // Otherwise no match, throw an exception.
            throw new NotSupportedException(SR.net_unknown_prefix);
        }

        // Create - Create a WebRequest.
        //
        // An overloaded utility version of the real Create that takes a
        // string instead of an Uri object.
        //
        // Input:
        //     RequestString       - Uri string to create.
        //
        // Returns:
        //     Newly created WebRequest.
        public static WebRequest Create(string requestUriString)
        {
            if (requestUriString == null)
            {
                throw new ArgumentNullException(nameof(requestUriString));
            }

            return Create(new Uri(requestUriString), false);
        }

        // Create - Create a WebRequest.
        //
        // Another overloaded version of the Create function that doesn't
        // take the UseUriBase parameter.
        //
        // Input:
        //     requestUri - Uri object for request.
        //
        // Returns:
        //     Newly created WebRequest.
        public static WebRequest Create(Uri requestUri)
        {
            if (requestUri == null)
            {
                throw new ArgumentNullException(nameof(requestUri));
            }

            return Create(requestUri, false);
        }

        // CreateDefault - Create a default WebRequest.
        //
        // This is the creation routine that creates a default WebRequest.
        // We take a Uri object and pass it to the base create routine,
        // setting the useUriBase parameter to true.
        //
        // Input:
        //     RequestUri - Uri object for request.
        //
        // Returns:
        //     Newly created WebRequest.
        internal static WebRequest CreateDefault(Uri requestUri)
        {
            if (requestUri == null)
            {
                throw new ArgumentNullException(nameof(requestUri));
            }

            return Create(requestUri, true);
        }

        public static HttpWebRequest CreateHttp(string requestUriString)
        {
            if (requestUriString == null)
            {
                throw new ArgumentNullException(nameof(requestUriString));
            }
            return CreateHttp(new Uri(requestUriString));
        }

        public static HttpWebRequest CreateHttp(Uri requestUri)
        {
            if (requestUri == null)
            {
                throw new ArgumentNullException(nameof(requestUri));
            }
            if ((requestUri.Scheme != "http") && (requestUri.Scheme != "https"))
            {
                throw new NotSupportedException(SR.net_unknown_prefix);
            }
            return (HttpWebRequest)CreateDefault(requestUri);
        }

        // RegisterPrefix - Register an Uri prefix for creating WebRequests.
        //
        // This function registers a prefix for creating WebRequests. When an
        // user wants to create a WebRequest, we scan a table looking for a
        // longest prefix match for the Uri they're passing. We then invoke
        // the sub creator for that prefix. This function puts entries in
        // that table.
        //
        // We don't allow duplicate entries, so if there is a dup this call
        // will fail.
        //
        // Input:
        //     Prefix  - Represents Uri prefix being registered.
        //     Creator - Interface for sub creator.
        //
        // Returns:
        //     True if the registration worked, false otherwise.
        public static bool RegisterPrefix(string prefix, IWebRequestCreate creator)
        {
            bool Error = false;
            int i;
            WebRequestPrefixElement Current;

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }
            if (creator == null)
            {
                throw new ArgumentNullException(nameof(creator));
            }

            // Lock this object, then walk down PrefixList looking for a place to
            // to insert this prefix.
            lock (s_internalSyncObject)
            {
                // Clone the object and update the clone, thus
                // allowing other threads to still read from the original.
                List<WebRequestPrefixElement> prefixList = new List<WebRequestPrefixElement>(PrefixList);

                // As AbsoluteUri is used later for Create, account for formating changes 
                // like Unicode escaping, default ports, etc.
                Uri tempUri;
                if (Uri.TryCreate(prefix, UriKind.Absolute, out tempUri))
                {
                    String cookedUri = tempUri.AbsoluteUri;

                    // Special case for when a partial host matching is requested, drop the added trailing slash
                    // IE: http://host could match host or host.domain
                    if (!prefix.EndsWith("/", StringComparison.Ordinal)
                        && tempUri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped)
                            .Equals("/"))
                    {
                        cookedUri = cookedUri.Substring(0, cookedUri.Length - 1);
                    }

                    prefix = cookedUri;
                }

                i = 0;

                // The prefix list is sorted with longest entries at the front. We
                // walk down the list until we find a prefix shorter than this
                // one, then we insert in front of it. Along the way we check
                // equal length prefixes to make sure this isn't a dupe.
                while (i < prefixList.Count)
                {
                    Current = prefixList[i];

                    // See if the new one is longer than the one we're looking at.
                    if (prefix.Length > Current.Prefix.Length)
                    {
                        // It is. Break out of the loop here.
                        break;
                    }

                    // If these are of equal length, compare them.
                    if (prefix.Length == Current.Prefix.Length)
                    {
                        // They're the same length.
                        if (string.Equals(Current.Prefix, prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            // ...and the strings are identical. This is an error.
                            Error = true;
                            break;
                        }
                    }
                    i++;
                }

                // When we get here either i contains the index to insert at or
                // we've had an error, in which case Error is true.
                if (!Error)
                {
                    // No error, so insert.
                    prefixList.Insert(i, new WebRequestPrefixElement(prefix, creator));

                    // Assign the clone to the static object. Other threads using it
                    // will have copied the original object already.
                    PrefixList = prefixList;
                }
            }
            return !Error;
        }

        internal class HttpRequestCreator : IWebRequestCreate
        {
            // Create - Create an HttpWebRequest.
            //
            // This is our method to create an HttpWebRequest. We register
            // for HTTP and HTTPS Uris, and this method is called when a request
            // needs to be created for one of those.
            //
            //
            // Input:
            //     uri - Uri for request being created.
            //
            // Returns:
            //     The newly created HttpWebRequest.
            public WebRequest Create(Uri Uri)
            {
                return new HttpWebRequest(Uri);
            }
        }

        // PrefixList - Returns And Initialize our prefix list.
        //
        //
        // This is the method that initializes the prefix list. We create
        // an List for the PrefixList, then an HttpRequestCreator object,
        // and then we register the HTTP and HTTPS prefixes.
        //
        // Returns:
        //     true
        internal static List<WebRequestPrefixElement> PrefixList
        {
            get
            {
                // GetConfig() might use us, so we have a circular dependency issue
                // that causes us to nest here. We grab the lock only if we haven't
                // initialized.
                if (s_prefixList == null)
                {
                    lock (s_internalSyncObject)
                    {
                        if (s_prefixList == null)
                        {
                            List<WebRequestPrefixElement> prefixList = new List<WebRequestPrefixElement>();

                            prefixList.Add(new WebRequestPrefixElement("http:", new HttpRequestCreator()));
                            prefixList.Add(new WebRequestPrefixElement("https:", new HttpRequestCreator()));
                            prefixList.Add(new WebRequestPrefixElement("file:", new HttpRequestCreator()));
                            prefixList.Add(new WebRequestPrefixElement("ftp:", new HttpRequestCreator()));

                            s_prefixList = prefixList;
                        }
                    }
                }

                return s_prefixList;
            }
            set
            {
                s_prefixList = value;
            }
        }

        protected WebRequest()
        {
        }

        public abstract string Method
        {
            get;
            set;
        }

        public abstract Uri RequestUri
        {
            get;
        }

        public abstract WebHeaderCollection Headers
        {
            get;
            set;
        }

        public abstract string ContentType
        {
            get;
            set;
        }

        public virtual ICredentials Credentials
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
            set
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }

        public virtual bool UseDefaultCredentials
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
            set
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }

        public abstract IAsyncResult BeginGetResponse(AsyncCallback callback, object state);

        public abstract WebResponse EndGetResponse(IAsyncResult asyncResult);

        public abstract IAsyncResult BeginGetRequestStream(AsyncCallback callback, Object state);

        public abstract Stream EndGetRequestStream(IAsyncResult asyncResult);

        public virtual Task<Stream> GetRequestStreamAsync()
        {
            // Offload to a different thread to avoid blocking the caller during request submission.
            // We use Task.Run rather than Task.Factory.StartNew even though StartNew would let us pass 'this'
            // as a state argument to avoid the closure to capture 'this' and the associated delegate.
            // This is because the task needs to call FromAsync and marshal the inner Task out, and
            // Task.Run's implementation of this is sufficiently more efficient than what we can do with 
            // Unwrap() that it's worth it to just rely on Task.Run and accept the closure/delegate.
            return Task.Run(() =>
                Task<Stream>.Factory.FromAsync(
                    (callback, state) => ((WebRequest)state).BeginGetRequestStream(callback, state),
                    iar => ((WebRequest)iar.AsyncState).EndGetRequestStream(iar),
                    this));
        }

        public virtual Task<WebResponse> GetResponseAsync()
        {
            // See comment in GetRequestStreamAsync().  Same logic applies here.
            return Task.Run(() =>
                Task<WebResponse>.Factory.FromAsync(
                    (callback, state) => ((WebRequest)state).BeginGetResponse(callback, state),
                    iar => ((WebRequest)iar.AsyncState).EndGetResponse(iar),
                    this));
        }

        public abstract void Abort();

        // Default Web Proxy implementation.
        private static IWebProxy s_DefaultWebProxy;
        private static bool s_DefaultWebProxyInitialized;

        public static IWebProxy DefaultWebProxy
        {
            get
            {
                lock (s_internalSyncObject)
                {
                    if (!s_DefaultWebProxyInitialized)
                    {
                        s_DefaultWebProxy = SystemWebProxy.Get();
                        s_DefaultWebProxyInitialized = true;
                    }

                    return s_DefaultWebProxy;
                }
            }

            set
            {
                lock (s_internalSyncObject)
                {
                    s_DefaultWebProxy = value;
                    s_DefaultWebProxyInitialized = true;
                }
            }
        }

        public virtual IWebProxy Proxy
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
            set
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }
    }
}
