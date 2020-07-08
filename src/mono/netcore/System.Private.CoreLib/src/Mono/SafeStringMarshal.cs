// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Safe wrapper for a string and its UTF8 encoding
//
// Authors:
//   Aleksey Kliger <aleksey@xamarin.com>
//   Rodrigo Kumpera <kumpera@xamarin.com>
//
//

using System;
using System.Runtime.CompilerServices;

namespace Mono
{
    internal struct SafeStringMarshal : IDisposable
    {
        private readonly string? str;
        private IntPtr marshaled_string;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr StringToUtf8_icall(ref string str);

        public static IntPtr StringToUtf8(string str)
        {
            return StringToUtf8_icall(ref str);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void GFree(IntPtr ptr);

        public SafeStringMarshal(string? str)
        {
            this.str = str;
            this.marshaled_string = IntPtr.Zero;
        }

        public IntPtr Value
        {
            get
            {
                if (marshaled_string == IntPtr.Zero && str != null)
                    marshaled_string = StringToUtf8(str);
                return marshaled_string;
            }
        }

        public void Dispose()
        {
            if (marshaled_string != IntPtr.Zero)
            {
                GFree(marshaled_string);
                marshaled_string = IntPtr.Zero;
            }
        }
    }
}
