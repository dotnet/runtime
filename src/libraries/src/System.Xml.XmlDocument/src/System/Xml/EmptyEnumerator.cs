// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace System.Xml
{
    internal sealed class EmptyEnumerator : IEnumerator
    {
        bool IEnumerator.MoveNext()
        {
            return false;
        }

        void IEnumerator.Reset()
        {
        }

        object IEnumerator.Current
        {
            get
            {
                throw new InvalidOperationException(SR.Xml_InvalidOperation);
            }
        }
    }
}
