// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices.CustomMarshalers
{
    internal class EnumVariantViewOfEnumerator : IEnumVARIANT, ICustomAdapter
    {
        public EnumVariantViewOfEnumerator(IEnumerator enumerator)
        {
            if (enumerator is null)
            {
                throw new ArgumentNullException(nameof(enumerator));
            }

            Enumerator = enumerator;
        }

        public IEnumerator Enumerator { get; }

        public IEnumVARIANT Clone()
        {
            if (Enumerator is ICloneable clonable)
            {
                return new EnumVariantViewOfEnumerator((IEnumerator)clonable.Clone());
            }
            else
            {
                throw new COMException(SR.Arg_EnumNotCloneable, HResults.E_FAIL);
            }
        }

        public int Next(int celt, object?[] rgVar, IntPtr pceltFetched)
        {
            int numElements = 0;

            try
            {
                if (celt > 0 && rgVar == null)
                {
                    return HResults.E_INVALIDARG;
                }

                while ((numElements < celt) && Enumerator.MoveNext())
                {
                    rgVar[numElements++] = Enumerator.Current;
                }

                if (pceltFetched != IntPtr.Zero)
                {
                    Marshal.WriteInt32(pceltFetched, numElements);
                }
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return numElements == celt ? HResults.S_OK : HResults.S_FALSE;
        }

        public int Reset()
        {
            try
            {
                Enumerator.Reset();
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return HResults.S_OK;
        }

        public int Skip(int celt)
        {
            try
            {
                while (celt > 0 && Enumerator.MoveNext())
                {
                    celt--;
                }
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return celt == 0 ? HResults.S_OK : HResults.S_FALSE;
        }

        public object GetUnderlyingObject()
        {
            return Enumerator;
        }
    }
}
