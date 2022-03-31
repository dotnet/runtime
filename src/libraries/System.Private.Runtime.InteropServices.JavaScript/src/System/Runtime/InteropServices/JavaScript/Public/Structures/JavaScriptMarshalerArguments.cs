// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;

namespace System.Runtime.InteropServices.JavaScript
{
    [CLSCompliant(false)]
    public partial struct JavaScriptMarshalerArguments
    {
        // intentionaly opaque internal structure

        public unsafe JavaScriptMarshalerArg Result
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var buf = (JSMarshalerArgumentsHeader*)Buffer;
                return new JavaScriptMarshalerArg
                {
                    Value = &buf[0].Result
                };
            }
        }

        public unsafe JavaScriptMarshalerArg Exception
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var buf = (JSMarshalerArgumentsHeader*)Buffer;
                return new JavaScriptMarshalerArg
                {
                    Value = &buf[0].Exception
                };
            }
        }

        /// <param name="position">One based argument position</param>
        public unsafe JavaScriptMarshalerArg this[int position]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var offset = sizeof(JSMarshalerArgumentsHeader) + (position - 1) * sizeof(JSMarshalerArg);
                var buf = (IntPtr)Buffer;
                return new JavaScriptMarshalerArg
                {
                    Value = (JSMarshalerArg*)(buf + offset)
                };
            }
        }
    }
}
