// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;
#nullable disable

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshalerSignature
    {
        internal unsafe JSMarshalerSignatureHeader* Header;
        internal unsafe JSMarshalerSig* Sigs;
        internal IntPtr JSHandle;
        internal JavaScriptMarshalerBase[] CustomMarshalers;
        internal Type[] Types;

        internal unsafe int ArgumentCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Header[0].ArgumentCount;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Header[0].ArgumentCount = value;
            }
        }

        internal unsafe int ExtraBufferLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Header[0].ExtraBufferLength;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Header[0].ExtraBufferLength = value;
            }
        }

        internal unsafe int ArgumentsBufferLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return sizeof(JSMarshalerArgumentsHeader) + ArgumentCount * sizeof(JSMarshalerArg);
            }
        }

        internal unsafe JSMarshalerSig Result
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Header[0].Result;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Header[0].Result = value;
            }
        }

        internal unsafe JSMarshalerSig Exception
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Header[0].Exception;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Header[0].Exception = value;
            }
        }

        // one based position of args, not exception, not result
        internal unsafe JSMarshalerSig this[int position]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Sigs[position - 1];
            }
        }
    }
}
