// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ExtensionCollection : ICollection, IEnumerable
    {
        public X509ExtensionCollection()
        {
        }

        public int Count
        {
            get { return _list.Count; }
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return this; }
        }

        public X509Extension this[int index]
        {
            get
            {
                if (index < 0)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                if (index >= _list.Count)
                    throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);

                return _list[index];
            }
        }

        public X509Extension this[String oid]
        {
            get
            {
                String oidValue = new Oid(oid).Value;
                foreach (X509Extension extension in _list)
                {
                    if (String.Equals(extension.Oid.Value, oidValue, StringComparison.OrdinalIgnoreCase))
                        return extension;
                }
                return null;
            }
        }

        public int Add(X509Extension extension)
        {
            if (extension == null)
                throw new ArgumentNullException("extension");
            _list.Add(extension);
            return _list.Count - 1;
        }

        public void CopyTo(X509Extension[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
            if (index + Count > array.Length)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        public X509ExtensionEnumerator GetEnumerator()
        {
            return new X509ExtensionEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new X509ExtensionEnumerator(this);
        }

        private readonly List<X509Extension> _list = new List<X509Extension>();
    }
}

