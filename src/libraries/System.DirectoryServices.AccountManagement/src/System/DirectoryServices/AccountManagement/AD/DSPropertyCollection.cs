// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class dSPropertyCollection
    {
        private readonly PropertyCollection _pc;
        private readonly ResultPropertyCollection _rp;

        private dSPropertyCollection() { }
        internal dSPropertyCollection(PropertyCollection pc) { _pc = pc; }
        internal dSPropertyCollection(ResultPropertyCollection rp) { _rp = rp; }

        public dSPropertyValueCollection this[string propertyName]
        {
            get
            {
                if (propertyName == null)
                    throw new ArgumentNullException(nameof(propertyName));

                if (null != _pc)
                {
                    return new dSPropertyValueCollection(_pc[propertyName]);
                }
                else
                {
                    return new dSPropertyValueCollection(_rp[propertyName]);
                }
            }
        }
    }

    internal sealed class dSPropertyValueCollection
    {
        private readonly PropertyValueCollection _pc;
        private readonly ResultPropertyValueCollection _rc;

        private dSPropertyValueCollection() { }
        internal dSPropertyValueCollection(PropertyValueCollection pc) { _pc = pc; }
        internal dSPropertyValueCollection(ResultPropertyValueCollection rc) { _rc = rc; }

        public object this[int index]
        {
            get
            {
                if (_pc != null)
                {
                    return _pc[index];
                }
                else
                {
                    return _rc[index];
                }
            }
        }
        public int Count
        {
            get
            {
                return (_pc != null ? _pc.Count : _rc.Count);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return (_pc != null ? _pc.GetEnumerator() : _rc.GetEnumerator());
        }
    }
}
