// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Net
{
    // CookieCollection
    //
    // A list of cookies maintained in Sorted order. Only one cookie with matching Name/Domain/Path 
    public class CookieCollection : ICollection
    {
        internal enum Stamp
        {
            Check = 0,
            Set = 1,
            SetToUnused = 2,
            SetToMaxUsed = 3,
        }

        internal int _version;
        private List<Cookie> _list = new List<Cookie>();

        private DateTime _timeStamp = DateTime.MinValue;
        private bool _hasOtherVersions;

        private readonly bool _isReadOnly;

        public CookieCollection()
        {
            _isReadOnly = true;
        }

        internal CookieCollection(bool IsReadOnly)
        {
            _isReadOnly = IsReadOnly;
        }

        public bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }
        }

        public Cookie this[int index]
        {
            get
            {
                if (index < 0 || index >= _list.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                return _list[index];
            }
        }

        public Cookie this[string name]
        {
            get
            {
                foreach (Cookie c in _list)
                {
                    if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }
                return null;
            }
        }

        public void Add(Cookie cookie)
        {
            if (cookie == null)
            {
                throw new ArgumentNullException("cookie");
            }
            ++_version;
            int idx = IndexOf(cookie);
            if (idx == -1)
            {
                _list.Add(cookie);
            }
            else
            {
                _list[idx] = cookie;
            }
        }

        public void Add(CookieCollection cookies)
        {
            if (cookies == null)
            {
                throw new ArgumentNullException("cookies");
            }
            foreach (Cookie cookie in cookies)
            {
                Add(cookie);
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        public void CopyTo(Cookie[] array, int index)
        {
            _list.CopyTo(array, index);
        }

        internal DateTime TimeStamp(Stamp how)
        {
            switch (how)
            {
                case Stamp.Set:
                    _timeStamp = DateTime.Now;
                    break;
                case Stamp.SetToMaxUsed:
                    _timeStamp = DateTime.MaxValue;
                    break;
                case Stamp.SetToUnused:
                    _timeStamp = DateTime.MinValue;
                    break;
                case Stamp.Check:
                default:
                    break;
            }
            return _timeStamp;
        }


        // This is for internal cookie container usage.
        // For others not that _hasOtherVersions gets changed ONLY in InternalAdd
        internal bool IsOtherVersionSeen
        {
            get
            {
                return _hasOtherVersions;
            }
        }

        // If isStrict == false, assumes that incoming cookie is unique.
        // If isStrict == true, replace the cookie if found same with newest Variant.
        // Returns 1 if added, 0 if replaced or rejected.
        internal int InternalAdd(Cookie cookie, bool isStrict)
        {
            int ret = 1;
            if (isStrict)
            {
                CookieComparer comp = CookieComparer.Instance;

                int idx = 0;
                foreach (Cookie c in _list)
                {
                    if (comp.Compare(cookie, c) == 0)
                    {
                        ret = 0; // Will replace or reject

                        // Cookie2 spec requires that new Variant cookie overwrite the old one.
                        if (c.Variant <= cookie.Variant)
                        {
                            _list[idx] = cookie;
                        }
                        break;
                    }
                    ++idx;
                }
                if (idx == _list.Count)
                {
                    _list.Add(cookie);
                }
            }
            else
            {
                _list.Add(cookie);
            }
            if (cookie.Version != Cookie.MaxSupportedVersion)
            {
                _hasOtherVersions = true;
            }
            return ret;
        }

        internal int IndexOf(Cookie cookie)
        {
            CookieComparer comp = CookieComparer.Instance;

            int idx = 0;
            foreach (Cookie c in _list)
            {
                if (comp.Compare(cookie, c) == 0)
                {
                    return idx;
                }
                ++idx;
            }
            return -1;
        }

        internal void RemoveAt(int idx)
        {
            _list.RemoveAt(idx);
        }

        public IEnumerator GetEnumerator()
        {
            return new CookieCollectionEnumerator(this);
        }

#if DEBUG
        internal void Dump()
        {
            GlobalLog.Print("CookieCollection:");
            foreach (Cookie cookie in this)
            {
                cookie.Dump();
            }
        }
#endif

        private class CookieCollectionEnumerator : IEnumerator
        {
            private CookieCollection _cookies;
            private int _count;
            private int _index = -1;
            private int _version;

            internal CookieCollectionEnumerator(CookieCollection cookies)
            {
                _cookies = cookies;
                _count = cookies.Count;
                _version = cookies._version;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_index < 0 || _index >= _count)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
                    if (_version != _cookies._version)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                    }
                    return _cookies[_index];
                }
            }

            bool IEnumerator.MoveNext()
            {
                if (_version != _cookies._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
                if (++_index < _count)
                {
                    return true;
                }
                _index = _count;
                return false;
            }

            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }
    }
}
