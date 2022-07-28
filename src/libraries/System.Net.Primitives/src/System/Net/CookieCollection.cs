// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Net
{
    // CookieCollection
    //
    // A list of cookies maintained in Sorted order. Only one cookie with matching Name/Domain/Path
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class CookieCollection : ICollection<Cookie>, IReadOnlyCollection<Cookie>, ICollection
    {
        internal enum Stamp
        {
            Check = 0,
            Set = 1,
            SetToUnused = 2,
            SetToMaxUsed = 3,
        }

        private readonly ArrayList m_list = new ArrayList();

        private int m_version; // Do not rename (binary serialization). This field only exists for .NET Framework serialization compatibility.
        private DateTime m_TimeStamp = DateTime.MinValue; // Do not rename (binary serialization)
        private bool m_has_other_versions; // Do not rename (binary serialization)

        public CookieCollection()
        {
        }

        public Cookie this[int index]
        {
            get
            {
                if (index < 0 || index >= m_list.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return (m_list[index] as Cookie)!;
            }
        }

        public Cookie? this[string name]
        {
            get
            {
                foreach (Cookie? c in m_list)
                {
                    if (string.Equals(c!.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }
                return null;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            m_version = m_list.Count;
        }

        public void Add(Cookie cookie)
        {
            ArgumentNullException.ThrowIfNull(cookie);

            int idx = IndexOf(cookie);
            if (idx == -1)
            {
                m_list.Add(cookie);
            }
            else
            {
                m_list[idx] = cookie;
            }
        }

        public void Add(CookieCollection cookies)
        {
            ArgumentNullException.ThrowIfNull(cookies);

            foreach (Cookie? cookie in cookies.m_list)
            {
                Add(cookie!);
            }
        }

        public void Clear()
        {
            m_list.Clear();
        }

        public bool Contains(Cookie cookie)
        {
            return IndexOf(cookie) >= 0;
        }

        public bool Remove(Cookie cookie)
        {
            int idx = IndexOf(cookie);
            if (idx == -1)
            {
                return false;
            }
            m_list.RemoveAt(idx);
            return true;
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                return m_list.Count;
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
            ((ICollection)m_list).CopyTo(array, index);
        }

        public void CopyTo(Cookie[] array, int index)
        {
            m_list.CopyTo(array, index);
        }

        internal DateTime TimeStamp(Stamp how)
        {
            switch (how)
            {
                case Stamp.Set:
                    m_TimeStamp = DateTime.Now;
                    break;
                case Stamp.SetToMaxUsed:
                    m_TimeStamp = DateTime.MaxValue;
                    break;
                case Stamp.SetToUnused:
                    m_TimeStamp = DateTime.MinValue;
                    break;
                case Stamp.Check:
                default:
                    break;
            }
            return m_TimeStamp;
        }


        // This is for internal cookie container usage.
        // For others not that _hasOtherVersions gets changed ONLY in InternalAdd
        internal bool IsOtherVersionSeen
        {
            get
            {
                return m_has_other_versions;
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
                int idx = 0;
                int listCount = m_list.Count;
                for (int i = 0; i < listCount; i++)
                {
                    Cookie c = (Cookie)m_list[i]!;
                    if (CookieComparer.Equals(cookie, c))
                    {
                        ret = 0; // Will replace or reject

                        // Cookie2 spec requires that new Variant cookie overwrite the old one.
                        if (c!.Variant <= cookie.Variant)
                        {
                            m_list[idx] = cookie;
                        }
                        break;
                    }
                    ++idx;
                }
                if (idx == m_list.Count)
                {
                    m_list.Add(cookie);
                }
            }
            else
            {
                m_list.Add(cookie);
            }
            if (cookie.Version != Cookie.MaxSupportedVersion)
            {
                m_has_other_versions = true;
            }
            return ret;
        }

        internal int IndexOf(Cookie cookie)
        {
            int idx = 0;
            foreach (Cookie? c in m_list)
            {
                if (CookieComparer.Equals(cookie, c!))
                {
                    return idx;
                }
                ++idx;
            }
            return -1;
        }

        internal void RemoveAt(int idx)
        {
            m_list.RemoveAt(idx);
        }

        IEnumerator<Cookie> IEnumerable<Cookie>.GetEnumerator()
        {
            foreach (Cookie? cookie in m_list)
            {
                yield return cookie!;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return m_list.GetEnumerator();
        }
    }
}
