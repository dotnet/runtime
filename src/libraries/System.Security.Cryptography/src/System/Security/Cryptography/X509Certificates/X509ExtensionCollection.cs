// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509ExtensionCollection : ICollection, IEnumerable<X509Extension>
    {
        public X509ExtensionCollection()
        {
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
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
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);

                return _list[index];
            }
        }

        public X509Extension? this[string oid]
        {
            get
            {
                string? oidValue = new Oid(oid).Value;
                foreach (X509Extension extension in _list)
                {
                    if (string.Equals(extension.Oid!.Value, oidValue, StringComparison.OrdinalIgnoreCase))
                        return extension;
                }
                return null;
            }
        }

        public int Add(X509Extension extension)
        {
            ArgumentNullException.ThrowIfNull(extension);

            _list.Add(extension);
            return _list.Count - 1;
        }

        public void CopyTo(X509Extension[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
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

        IEnumerator<X509Extension> IEnumerable<X509Extension>.GetEnumerator() => GetEnumerator();

        private readonly List<X509Extension> _list = new List<X509Extension>();
    }
}
