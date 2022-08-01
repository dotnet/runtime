// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

//
// System.Reflection.Emit/EventBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class EventBuilder
    {
#region Sync with MonoReflectionEventBuilder in object-internals.h
        internal string name;
        private Type type;
        private TypeBuilder typeb;
        private CustomAttributeBuilder[]? cattrs;
        internal MethodBuilder? add_method;
        internal MethodBuilder? remove_method;
        internal MethodBuilder? raise_method;
        internal MethodBuilder[]? other_methods;
        internal EventAttributes attrs;
        private int table_idx;
#endregion

        [DynamicDependency(nameof(table_idx))]  // Automatically keeps all previous fields too due to StructLayout
        internal EventBuilder(TypeBuilder tb, string eventName, EventAttributes eventAttrs, Type eventType)
        {
            name = eventName;
            attrs = eventAttrs;
            type = eventType;
            typeb = tb;
            table_idx = get_next_table_index(0x14, 1);
        }

        internal int get_next_table_index(int table, int count)
        {
            return typeb.get_next_table_index(table, count);
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            RejectIfCreated();
            if (other_methods != null)
            {
                MethodBuilder[] newv = new MethodBuilder[other_methods.Length + 1];
                other_methods.CopyTo(newv, 0);
                other_methods = newv;
            }
            else
            {
                other_methods = new MethodBuilder[1];
            }
            other_methods[other_methods.Length - 1] = mdBuilder;
        }

        public void SetAddOnMethod(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            RejectIfCreated();
            add_method = mdBuilder;
        }
        public void SetRaiseMethod(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            RejectIfCreated();
            raise_method = mdBuilder;
        }
        public void SetRemoveOnMethod(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            RejectIfCreated();
            remove_method = mdBuilder;
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);
            RejectIfCreated();
            string? attrname = customBuilder.Ctor.ReflectedType!.FullName;
            if (attrname == "System.Runtime.CompilerServices.SpecialNameAttribute")
            {
                attrs |= EventAttributes.SpecialName;
                return;
            }
            if (cattrs != null)
            {
                CustomAttributeBuilder[] new_array = new CustomAttributeBuilder[cattrs.Length + 1];
                cattrs.CopyTo(new_array, 0);
                new_array[cattrs.Length] = customBuilder;
                cattrs = new_array;
            }
            else
            {
                cattrs = new CustomAttributeBuilder[1];
                cattrs[0] = customBuilder;
            }
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        private void RejectIfCreated()
        {
            if (typeb.is_created)
                throw new InvalidOperationException("Type definition of the method is complete.");
        }
    }
}

#endif
