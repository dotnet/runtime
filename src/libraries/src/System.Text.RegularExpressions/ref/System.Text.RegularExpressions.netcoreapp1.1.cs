// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Text.RegularExpressions
{
    public partial class Group
    {
        public string Name { get { throw null; } }
    }
    public partial class Regex
    {
        [System.CLSCompliant(false)]
        protected System.Collections.IDictionary Caps { get { throw null; } set { } }
        [System.CLSCompliant(false)]
        protected System.Collections.IDictionary CapNames { get { throw null; } set { } }
    }
    public partial class CaptureCollection : System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>, System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Capture>, System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>, System.Collections.Generic.IReadOnlyCollection<System.Text.RegularExpressions.Capture>, System.Collections.Generic.IReadOnlyList<System.Text.RegularExpressions.Capture>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public void CopyTo(System.Text.RegularExpressions.Capture[] array, int arrayIndex) { }
        System.Collections.Generic.IEnumerator<System.Text.RegularExpressions.Capture> System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Capture>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>.IndexOf(System.Text.RegularExpressions.Capture item) { throw null; }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>.Insert(int index, System.Text.RegularExpressions.Capture item) { }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>.RemoveAt(int index) { }
        System.Text.RegularExpressions.Capture System.Collections.Generic.IList<System.Text.RegularExpressions.Capture>.this[int index] { get { throw null; } set { } }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>.Add(System.Text.RegularExpressions.Capture item) { }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>.Contains(System.Text.RegularExpressions.Capture item) { throw null; }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Capture>.Remove(System.Text.RegularExpressions.Capture item) { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
    }
    public partial class GroupCollection : System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>, System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Group>, System.Collections.Generic.IList<System.Text.RegularExpressions.Group>, System.Collections.Generic.IReadOnlyCollection<System.Text.RegularExpressions.Group>, System.Collections.Generic.IReadOnlyList<System.Text.RegularExpressions.Group>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public void CopyTo(System.Text.RegularExpressions.Group[] array, int arrayIndex) { }
        System.Collections.Generic.IEnumerator<System.Text.RegularExpressions.Group> System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Group>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<System.Text.RegularExpressions.Group>.IndexOf(System.Text.RegularExpressions.Group item) { throw null; }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Group>.Insert(int index, System.Text.RegularExpressions.Group item) { }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Group>.RemoveAt(int index) { }
        System.Text.RegularExpressions.Group System.Collections.Generic.IList<System.Text.RegularExpressions.Group>.this[int index] { get { throw null; } set { } }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>.Add(System.Text.RegularExpressions.Group item) { }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>.Contains(System.Text.RegularExpressions.Group item) { throw null; }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Group>.Remove(System.Text.RegularExpressions.Group item) { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
    }
    public partial class MatchCollection : System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>, System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Match>, System.Collections.Generic.IList<System.Text.RegularExpressions.Match>, System.Collections.Generic.IReadOnlyCollection<System.Text.RegularExpressions.Match>, System.Collections.Generic.IReadOnlyList<System.Text.RegularExpressions.Match>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public void CopyTo(System.Text.RegularExpressions.Match[] array, int arrayIndex) { }
        System.Collections.Generic.IEnumerator<System.Text.RegularExpressions.Match> System.Collections.Generic.IEnumerable<System.Text.RegularExpressions.Match>.GetEnumerator() { throw null; }
        int System.Collections.Generic.IList<System.Text.RegularExpressions.Match>.IndexOf(System.Text.RegularExpressions.Match item) { throw null; }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Match>.Insert(int index, System.Text.RegularExpressions.Match item) { }
        void System.Collections.Generic.IList<System.Text.RegularExpressions.Match>.RemoveAt(int index) { }
        System.Text.RegularExpressions.Match System.Collections.Generic.IList<System.Text.RegularExpressions.Match>.this[int index] { get { throw null; } set { } }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>.Add(System.Text.RegularExpressions.Match item) { }
        void System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>.Contains(System.Text.RegularExpressions.Match item) { throw null; }
        bool System.Collections.Generic.ICollection<System.Text.RegularExpressions.Match>.Remove(System.Text.RegularExpressions.Match item) { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
    }
}
