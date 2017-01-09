// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Net.HttpListenerPrefixCollection.cs
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections;
using System.Collections.Generic;

namespace System.Net
{
    public class HttpListenerPrefixCollection : ICollection<string>
    {
        private List<string> _prefixes = new List<string>();
        private HttpListener _listener;

        internal HttpListenerPrefixCollection(HttpListener listener)
        {
            _listener = listener;
        }

        public int Count
        {
            get { return _prefixes.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public void Add(string uriPrefix)
        {
            _listener.CheckDisposed();
            ListenerPrefix.CheckUri(uriPrefix);
            if (_prefixes.Contains(uriPrefix))
                return;

            _prefixes.Add(uriPrefix);
            if (_listener.IsListening)
                HttpEndPointManager.AddPrefix(uriPrefix, _listener);
        }

        public void Clear()
        {
            _listener.CheckDisposed();
            _prefixes.Clear();
            if (_listener.IsListening)
                HttpEndPointManager.RemoveListener(_listener);
        }

        public bool Contains(string uriPrefix)
        {
            _listener.CheckDisposed();
            return _prefixes.Contains(uriPrefix);
        }

        public void CopyTo(string[] array, int offset)
        {
            _listener.CheckDisposed();
            _prefixes.CopyTo(array, offset);
        }

        public void CopyTo(Array array, int offset)
        {
            _listener.CheckDisposed();
            ((ICollection)_prefixes).CopyTo(array, offset);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _prefixes.GetEnumerator();
        }

        public bool Remove(string uriPrefix)
        {
            _listener.CheckDisposed();
            if (uriPrefix == null)
                throw new ArgumentNullException(nameof(uriPrefix));

            bool result = _prefixes.Remove(uriPrefix);
            if (result && _listener.IsListening)
                HttpEndPointManager.RemovePrefix(uriPrefix, _listener);

            return result;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _prefixes.GetEnumerator();
        }
    }
}
