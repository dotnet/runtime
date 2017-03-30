// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Accumulates a list of sort keys and stores them in an array.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct XmlSortKeyAccumulator
    {
        private XmlSortKey[] _keys;
        private int _pos;

#if DEBUG
        private const int DefaultSortKeyCount = 4;
#else
        private const int DefaultSortKeyCount = 64;
#endif

        /// <summary>
        /// Initialize the XmlSortKeyAccumulator.
        /// </summary>
        public void Create()
        {
            if (_keys == null)
                _keys = new XmlSortKey[DefaultSortKeyCount];

            _pos = 0;
            _keys[0] = null;
        }

        /// <summary>
        /// Create a new sort key and append it to the current run of sort keys.
        /// </summary>
        public void AddStringSortKey(XmlCollation collation, string value)
        {
            AppendSortKey(collation.CreateSortKey(value));
        }

        public void AddDecimalSortKey(XmlCollation collation, decimal value)
        {
            AppendSortKey(new XmlDecimalSortKey(value, collation));
        }

        public void AddIntegerSortKey(XmlCollation collation, long value)
        {
            AppendSortKey(new XmlIntegerSortKey(value, collation));
        }

        public void AddIntSortKey(XmlCollation collation, int value)
        {
            AppendSortKey(new XmlIntSortKey(value, collation));
        }

        public void AddDoubleSortKey(XmlCollation collation, double value)
        {
            AppendSortKey(new XmlDoubleSortKey(value, collation));
        }

        public void AddDateTimeSortKey(XmlCollation collation, DateTime value)
        {
            AppendSortKey(new XmlDateTimeSortKey(value, collation));
        }

        public void AddEmptySortKey(XmlCollation collation)
        {
            AppendSortKey(new XmlEmptySortKey(collation));
        }

        /// <summary>
        /// Finish creating the current run of sort keys and begin a new run.
        /// </summary>
        public void FinishSortKeys()
        {
            _pos++;
            if (_pos >= _keys.Length)
            {
                XmlSortKey[] keysNew = new XmlSortKey[_pos * 2];
                Array.Copy(_keys, 0, keysNew, 0, _keys.Length);
                _keys = keysNew;
            }
            _keys[_pos] = null;
        }

        /// <summary>
        /// Append new sort key to the current run of sort keys.
        /// </summary>
        private void AppendSortKey(XmlSortKey key)
        {
            // Ensure that sort will be stable by setting index of key
            key.Priority = _pos;

            if (_keys[_pos] == null)
                _keys[_pos] = key;
            else
                _keys[_pos].AddSortKey(key);
        }

        /// <summary>
        /// Get array of sort keys that was constructed by this internal class.
        /// </summary>
        public Array Keys
        {
            get { return _keys; }
        }
    }
}
