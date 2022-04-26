// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MTNAMETABLE
using System;
using System.IO;
using System.Collections;
using System.Threading;

namespace System.Xml {

#if !SPLAY_MTNAMETABLE

    // MTNameTable is a modified version of our normal NameTable
    // that is thread-safe on read & write.  The design is kept
    // simple by using the Entry[] as the atomic update pivot point.
    public class MTNameTable : XmlNameTable {
        //
        // Private types
        //
        class Entry {
            internal string str;
            internal int    hashCode;
            internal Entry  next;

            internal Entry( string str, int hashCode, Entry next ) {
                this.str = str;
                this.hashCode = hashCode;
                this.next = next;
            }
        }

        //
        // Fields
        //
        Entry[] entries;
        int     count;
        int     hashCodeRandomizer;

        //
        // Constructor
        //
        public MTNameTable() {
            entries = new Entry[32];
            hashCodeRandomizer = Environment.TickCount;
        }

        //
        // XmlNameTable public methods
        //
        public override string Add( string key!! ) {
            int len = key.Length;
            if ( len == 0 ) {
                return string.Empty;
            }
            int hashCode = len + hashCodeRandomizer;
            // use key.Length to eliminate the range check
            for ( int i = 0; i < key.Length; i++ ) {
                hashCode += ( hashCode << 7 ) ^ key[i];
            }
            // mix it a bit more
            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;

            Entry[] entries = this.entries;
            for ( Entry e = entries[hashCode & (entries.Length-1)];
                  e != null;
                  e = e.next ) {
                if ( e.hashCode == hashCode && e.str.Equals( key ) ) {
                    return e.str;
                }
            }
            return AddEntry( key, hashCode );
        }

        public override string Add( char[] key, int start, int len ) {
            if ( len == 0 ) {
                return string.Empty;
            }

            int hashCode = len + hashCodeRandomizer;
            hashCode += ( hashCode << 7 ) ^ key[start];   // this will throw IndexOutOfRangeException in case the start index is invalid
            int end = start+len;
            for ( int i = start + 1; i < end; i++) {
                hashCode += ( hashCode << 7 ) ^ key[i];
            }
            // mix it a bit more
            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;

            Entry[] entries = this.entries;
            for ( Entry e = entries[hashCode & (entries.Length-1)];
                  e != null;
                  e = e.next ) {
                if ( e.hashCode == hashCode && TextEquals( e.str, key, start ) ) {
                    return e.str;
                }
            }
            return AddEntry( new string( key, start, len ), hashCode );
        }

        public override string Get( string value!! ) {
            if ( value.Length == 0 ) {
                return string.Empty;
            }

            int len = value.Length + hashCodeRandomizer;
            int hashCode = len;
            // use value.Length to eliminate the range check
            for ( int i = 0; i < value.Length; i++ ) {
                hashCode += ( hashCode << 7 ) ^ value[i];
            }
            // mix it a bit more
            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;

            Entry[] entries = this.entries;
            for ( Entry e = entries[hashCode & (entries.Length-1)];
                  e != null;
                  e = e.next ) {
                if ( e.hashCode == hashCode && e.str.Equals( value ) ) {
                    return e.str;
                }
            }
            return null;
        }

        public override string Get( char[] key, int start, int len ) {
            if ( len == 0 ) {
                return string.Empty;
            }

            int hashCode = len + hashCodeRandomizer;
            hashCode += ( hashCode << 7 ) ^ key[start]; // this will throw IndexOutOfRangeException in case the start index is invalid
            int end = start+len;
            for ( int i = start + 1; i < end; i++) {
                hashCode += ( hashCode << 7 ) ^ key[i];
            }
            // mix it a bit more
            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;

            Entry[] entries = this.entries;
            for ( Entry e = entries[hashCode & (entries.Length-1)];
                  e != null;
                  e = e.next ) {
                if ( e.hashCode == hashCode && TextEquals( e.str, key, start ) ) {
                    return e.str;
                }
            }
            return null;
        }

        //
        // Private methods
        //

        private string AddEntry( string str, int hashCode ) {
            Entry e;
            lock (this) {
                Entry[] entries = this.entries;
                int index = hashCode & entries.Length-1;
                for ( e = entries[index]; e != null; e = e.next ) {
                    if ( e.hashCode == hashCode && e.str.Equals( str ) ) {
                        return e.str;
                    }
                }
                e = new Entry( str, hashCode, entries[index] );
                entries[index] = e;
                if ( count++ == mask ) {
                    Grow();
                }
            }
            return e.str;
        }

        private void Grow() {
            int newMask = mask * 2 + 1;
            Entry[] oldEntries = entries;
            Entry[] newEntries = new Entry[newMask+1];

            // use oldEntries.Length to eliminate the range check
            for ( int i = 0; i < oldEntries.Length; i++ ) {
                Entry e = oldEntries[i];
                while ( e != null ) {
                    int newIndex = e.hashCode & newMask;
                    Entry tmp = e.next;
                    e.next = newEntries[newIndex];
                    newEntries[newIndex] = e;
                    e = tmp;
                }
            }

            entries = newEntries;
            mask = newMask;
        }

        private static bool TextEquals( string array, char[] text, int start ) {
            // use array.Length to eliminate the range check
            for ( int i = 0; i < array.Length; i++ ) {
                if ( array[i] != text[start+i] ) {
                    return false;
                }
            }
            return true;
        }
    }

#else

    // XmlNameTable implemented as a multi-threaded splay tree.
    [Obsolete("MTNameTable has been deprecated and is not supported.")]
    public class MTNameTable : XmlNameTable {
        internal MTNameTableNode rootNode;
        internal ReaderWriterLock rwLock;
        internal int timeout;

        public MTNameTable( bool isThreadSafe, int timeout ) {
            if (isThreadSafe) {
                this.rwLock = new ReaderWriterLock();
                this.timeout = timeout;
            }
        }

        public MTNameTable( bool isThreadSafe ): this( isThreadSafe, Timeout.Infinite ) {
        }

        public MTNameTable(): this( false ) {
        }



        public IEnumerator GetEnumerator() {
            return new MTNameTableEnumerator( this );
        }


        public override String Get( String value!! ) {
            MTNameTableName name = new MTNameTableName(value);
            return Get( ref name );
        }

        public override String Get( char[] key, int start, int len ) {
            ArgumentNullException.ThrowIfNull(key);

            if ((start < 0) || (len < 0) || (start > key.Length - len))
                throw new ArgumentOutOfRangeException();

            MTNameTableName name = new MTNameTableName(key, start, len);
            return Get( ref name );
        }

        private String Get( ref MTNameTableName nn ) {
            String name = null;

            if (rootNode != null) {
                if (rwLock != null)
                    rwLock.AcquireReaderLock(timeout);

                MTNameTableNode currentNode = rootNode;

                while (true) {
                    Int64 d = currentNode.Compare(ref nn);

                    if (d == 0) {
                        Promote( currentNode );
                        name = currentNode.value;
                        break;
                    }
                    else if (d < 0) {
                        if (currentNode.leftNode == null)
                            break;

                        currentNode = currentNode.leftNode;
                    }
                    else {
                        if (currentNode.rightNode == null)
                            break;

                        currentNode = currentNode.rightNode;
                    }
                }

                if (rwLock != null)
                    rwLock.ReleaseReaderLock();
            }

            return name;
        }



        // Find the matching string atom given a string, or
        // insert a new one.
        public override String Add(String value) {
            ArgumentNullException.ThrowIfNull(value);

            MTNameTableName name = new MTNameTableName( value );
            return Add( ref name, rwLock != null ).value;
        }

        public override String Add(char[] key, int start, int len) {
            ArgumentNullException.ThrowIfNull(key);

            if ((start < 0) || (len < 0) || (start > key.Length - len))
                throw new ArgumentOutOfRangeException();

            MTNameTableName name = new MTNameTableName( key, start, len );
            return Add( ref name, rwLock != null ).value;
        }

        private MTNameTableNode Add( ref MTNameTableName name, bool fLock) {
            if (fLock)
                rwLock.AcquireReaderLock(timeout);

            MTNameTableNode currentNode = rootNode;

            while (true) {
                if (currentNode == null) {
                    currentNode = AddRoot( ref name, fLock );
                    break;
                }
                else {
                    Int64 d = currentNode.Compare(ref name);

                    if (d == 0) {
                        Promote( currentNode );
                        break;
                    }
                    else if (d < 0) {
                        if (currentNode.leftNode == null) {
                            currentNode = AddLeft( currentNode, ref name, fLock );
                            break;
                        }
                        else {
                            currentNode = currentNode.leftNode;
                        }
                    }
                    else {
                        if (currentNode.rightNode == null) {
                            currentNode = AddRight( currentNode, ref name, fLock );
                            break;
                        }
                        else {
                            currentNode = currentNode.rightNode;
                        }
                    }
                }
            }

            if (fLock)
                rwLock.ReleaseReaderLock();

            return currentNode;
        }

        // Sets the root node given a string
        private MTNameTableNode AddRoot( ref MTNameTableName name, bool fLock ) {
            MTNameTableNode newNode = null;

            if (fLock) {
                LockCookie lc = rwLock.UpgradeToWriterLock(timeout);

                // recheck for failsafe against race-condition
                if (rootNode == null) {
                    rootNode = newNode = new MTNameTableNode( ref name );
                }
                else {
                    // try again, with write-lock active
                    newNode = Add( ref name, false );
                }

                rwLock.DowngradeFromWriterLock(ref lc);
            }
            else {
                rootNode = newNode = new MTNameTableNode( ref name );
            }

            return newNode;
        }


        // Adds the a node to the left of the specified node given a string
        private MTNameTableNode AddLeft( MTNameTableNode node, ref MTNameTableName name, bool fLock ) {
            MTNameTableNode newNode = null;

            if (fLock) {
                LockCookie lc = rwLock.UpgradeToWriterLock(timeout);

                // recheck for failsafe against race-condition
                if (node.leftNode == null) {
                    newNode = new MTNameTableNode( ref name );
                    node.leftNode = newNode;
                    newNode.parentNode = node;
                }
                else {
                    // try again, with write-lock active
                    newNode = Add( ref name, false );
                }

                rwLock.DowngradeFromWriterLock(ref lc);
            }
            else {
                newNode = new MTNameTableNode( ref name );
                node.leftNode = newNode;
                newNode.parentNode = node;
            }

            return newNode;
        }


        // Adds the a node to the right of the specified node, given a string.
        private MTNameTableNode AddRight( MTNameTableNode node, ref MTNameTableName name, bool fLock ) {
            MTNameTableNode newNode = null;

            if (fLock) {
                LockCookie lc = rwLock.UpgradeToWriterLock(timeout);

                // recheck for failsafe against race-condition
                if (node.rightNode == null) {
                    newNode = new MTNameTableNode( ref name );
                    node.rightNode = newNode;
                    newNode.parentNode = node;
                }
                else {
                    // try again, with write-lock active
                    newNode = Add( ref name, false );
                }

                rwLock.DowngradeFromWriterLock(ref lc);
            }
            else {
                newNode = new MTNameTableNode( ref name );
                node.rightNode = newNode;
                newNode.parentNode = node;
            }

            return newNode;
        }


        private const int threshhold = 20;

        // Promote the node into the parent's position (1 ply closer to the rootNode)
        private void Promote( MTNameTableNode node ) {
            // count number of times promotion requested
            node.counter++;

            if (node != rootNode &&
                node.counter > threshhold &&
                node.counter > node.parentNode.counter * 2) {
                if (rwLock != null) {
                    LockCookie lc = rwLock.UpgradeToWriterLock(timeout);

                    // recheck for failsafe against race-condition
                    if (node != rootNode &&
                        node.counter > threshhold &&
                        node.counter > node.parentNode.counter * 2) {
                        InternalPromote( node );
                    }

                    rwLock.DowngradeFromWriterLock(ref lc);
                }
                else {
                    InternalPromote( node );
                }
            }
        }

        private void InternalPromote( MTNameTableNode node ) {
            MTNameTableNode parent = node.parentNode;

            if (parent != null) {
                MTNameTableNode grandParent = parent.parentNode;

                if (parent.leftNode == node) {
                    parent.leftNode = node.rightNode;
                    node.rightNode = parent;

                    // update lineage
                    if (parent.leftNode != null)
                        parent.leftNode.parentNode = parent;

                    node.parentNode = grandParent;
                    parent.parentNode = node;
                }
                else {
                    parent.rightNode = node.leftNode;
                    node.leftNode = parent;

                    // update lineage
                    if (parent.rightNode != null)
                        parent.rightNode.parentNode = parent;

                    node.parentNode = grandParent;
                    parent.parentNode = node;
                }

                // fixup pointer to promoted node in grand parent
                if (grandParent == null) {
                    rootNode = node;
                }
                else {
                    if (grandParent.leftNode == parent) {
                        grandParent.leftNode = node;
                    }
                    else {
                        grandParent.rightNode = node;
                    }
                }
            }
        }
    }


    internal struct MTNameTableName {
        internal String str;
        internal char[] array;
        internal int start;
        internal int len;
        internal Int64 hash;

        public MTNameTableName( string str ) {
            this.str = str;
            this.hash = Hash(str);
            this.array = null;
            this.start = 0;
            this.len = 0;
        }

        public MTNameTableName( char[] array, int start, int len ) {
            this.array = array;
            this.start = start;
            this.len = len;
            this.str = null;
            this.hash = Hash(array, start, len);
        }

        private static Int64 Hash(String value) {
            Int64 hash = 0;
            int len = value.Length;

            if (len > 0)
                hash = (((Int64)value[0]) & 0xFF) << 48;

            if (len > 1)
                hash = hash | ((((Int64)value[1]) & 0xFF) << 32);

            if (len > 2)
                hash = hash | ((((Int64)value[2]) & 0xFF) << 16);

            if (len > 3)
                hash = hash | (((Int64)value[3]) & 0xFF);

            return hash;
        }

        private static Int64 Hash(char[] key, int start, int len) {
            Int64 hash = 0;

            if (len > 0)
                hash = (((Int64)key[start]) & 0xFF) << 48;

            if (len > 1)
                hash = hash | ((((Int64)key[start+1]) & 0xFF) << 32);

            if (len > 2)
                hash = hash | ((((Int64)key[start+2]) & 0xFF) << 16);

            if (len > 3)
                hash = hash | (((Int64)key[start+3]) & 0xFF);

            return hash;
        }
    }


    // A MTNameTable node.
    internal sealed class MTNameTableNode {
        internal String value;
        internal Int64 hash;
        internal Int64 counter;
        internal MTNameTableNode leftNode;
        internal MTNameTableNode rightNode;
        internal MTNameTableNode parentNode;

        internal MTNameTableNode(ref MTNameTableName name ) {
            if (name.str != null) {
                value = name.str;
            }
            else {
                value = new String(name.array, name.start, name.len);
            }

            this.hash = name.hash;
        }

        internal Int64 Compare( ref MTNameTableName name ) {
            if (name.array != null) {
                return Compare( name.hash, name.array, name.start, name.len );
            }
            else {
                return Compare( name.hash, name.str );
            }
        }

        private Int64 Compare(Int64 hash, string value) {
            Int64 d = hash - this.hash;

            if (d == 0) {
                int valueLength = this.value.Length;
                d = value.Length - valueLength;

                // if length is not equal, break
                if (d != 0)
                    return(Int64)d;

                for (int ii = 4; ii < valueLength; ii++) {
                    int dd = value[ii] - this.value[ii];
                    if (dd != 0) {
                        d = dd;
                        break;
                    }
                }
            }

            return(Int64)d;
        }

        private Int64 Compare(Int64 hash, char[] key, int start, int len) {
            Int64 d = hash - this.hash;

            if (d == 0) {
                int valueLength = this.value.Length;
                d = len - valueLength;

                // if length is not equal, break
                if (d != 0)
                    return(Int64)d;

                for (int ii = 4; ii < valueLength; ii++) {
                    int dd = key[start + ii] - this.value[ii];
                    if (dd != 0) {
                        d = dd;
                        break;
                    }
                }
            }

            return(Int64)d;
        }
    }


    // Enumerates all the names (strings) of a MTNameTable
    internal sealed class MTNameTableEnumerator: IEnumerator {
        private ArrayList names;
        private int iName;

        internal MTNameTableEnumerator( MTNameTable nt ) {
            if (nt.rwLock != null)
                nt.rwLock.AcquireReaderLock(nt.timeout);

            names = new ArrayList();
            Walk( nt.rootNode );
            iName = -1;

            if (nt.rwLock != null)
                nt.rwLock.ReleaseReaderLock();
        }

        internal void Walk( MTNameTableNode node ) {
            if (node != null) {
                if (node.leftNode != null)
                    Walk( node.leftNode );

                names.Add( node.value );

                if (node.rightNode != null)
                    Walk( node.rightNode );
            }
        }

        public void Reset() {
            iName = -1;
        }

        public bool MoveNext() {
            iName++;
            return iName < names.Count;
        }

        public object Current {
            get {
                if (iName < names.Count)
                    return names[iName];
                return null;
            }
        }
    }
#endif
}

#endif
