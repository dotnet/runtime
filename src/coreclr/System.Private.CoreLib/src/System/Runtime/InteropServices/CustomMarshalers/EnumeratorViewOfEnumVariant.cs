// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices.CustomMarshalers
{
    internal sealed class EnumeratorViewOfEnumVariant : ICustomAdapter, System.Collections.IEnumerator
    {
        private readonly IEnumVARIANT _enumVariantObject;
        private bool _fetchedLastObject;
        private readonly object[] _nextArray = new object[1];
        private object? _current;

        public EnumeratorViewOfEnumVariant(IEnumVARIANT enumVariantObject)
        {
            _enumVariantObject = enumVariantObject;
            _fetchedLastObject = false;
            _current = null;
        }

        public object? Current => _current;

        public unsafe bool MoveNext()
        {
            if (_fetchedLastObject)
            {
                _current = null;
                return false;
            }

            int numFetched = 0;

            if (_enumVariantObject.Next(1, _nextArray, (IntPtr)(&numFetched)) == HResults.S_FALSE)
            {
                _fetchedLastObject = true;

                if (numFetched == 0)
                {
                    _current = null;
                    return false;
                }
            }

            _current = _nextArray[0];

            return true;
        }

        public void Reset()
        {
            int hr = _enumVariantObject.Reset();
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            _fetchedLastObject = false;
            _current = null;
        }

        public object GetUnderlyingObject()
        {
            return _enumVariantObject;
        }
    }
}
