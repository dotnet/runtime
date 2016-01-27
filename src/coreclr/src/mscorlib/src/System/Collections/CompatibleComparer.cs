// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System.Diagnostics.Contracts;

namespace System.Collections {

    [Serializable]
    internal class CompatibleComparer: IEqualityComparer {
        IComparer _comparer;
#pragma warning disable 618
        IHashCodeProvider _hcp;

        internal CompatibleComparer(IComparer comparer, IHashCodeProvider hashCodeProvider) {
            _comparer = comparer;
            _hcp = hashCodeProvider;
        }
#pragma warning restore 618

        public int Compare(Object a, Object b) {
            if (a == b) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            if (_comparer != null)
                return _comparer.Compare(a,b);
            IComparable ia = a as IComparable;
            if (ia != null)
                return ia.CompareTo(b);

            throw new ArgumentException(Environment.GetResourceString("Argument_ImplementIComparable"));
        }

        public new bool Equals(Object a, Object b) {
            return Compare(a, b) == 0;                
        }

        public int GetHashCode(Object obj) {
            if( obj == null) {
                throw new ArgumentNullException("obj");
            }
            Contract.EndContractBlock();

            if (_hcp != null)
                return _hcp.GetHashCode(obj);
            return obj.GetHashCode();
        }

        // These are helpers for the Hashtable to query the IKeyComparer infrastructure.
        internal IComparer Comparer {
            get {
                return _comparer;
            }
        }

        // These are helpers for the Hashtable to query the IKeyComparer infrastructure.
#pragma warning disable 618
        internal IHashCodeProvider HashCodeProvider {
            get {
                return _hcp;
            }
        }
#pragma warning restore 618
    }
}
