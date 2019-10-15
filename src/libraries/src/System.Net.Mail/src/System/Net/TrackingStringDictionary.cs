// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;

namespace System.Net
{
    internal sealed class TrackingStringDictionary : StringDictionary
    {
        private readonly bool _isReadOnly;
        private bool _isChanged;

        internal TrackingStringDictionary() : this(false)
        {
        }

        internal TrackingStringDictionary(bool isReadOnly)
        {
            _isReadOnly = isReadOnly;
        }

        internal bool IsChanged { get { return _isChanged; } set { _isChanged = value; } }

        public override void Add(string key, string value)
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.MailCollectionIsReadOnly);
            }

            base.Add(key, value);
            _isChanged = true;
        }

        public override void Clear()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.MailCollectionIsReadOnly);
            }

            base.Clear();
            _isChanged = true;
        }

        public override void Remove(string key)
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.MailCollectionIsReadOnly);
            }

            base.Remove(key);
            _isChanged = true;
        }

        public override string this[string key]
        {
            get { return base[key]; }
            set
            {
                if (_isReadOnly)
                {
                    throw new InvalidOperationException(SR.MailCollectionIsReadOnly);
                }

                base[key] = value;
                _isChanged = true;
            }
        }
    }
}
