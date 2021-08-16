// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit/EventOnTypeBuilderInst.cs
//
// Author:
//   Rodrigo Kumpera (rkumpera@novell.com)
//
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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

#if MONO_FEATURE_SRE
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    /*
     * This class represents an event of an instantiation of a generic type builder.
     */
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class EventOnTypeBuilderInst : EventInfo
    {
        private TypeBuilderInstantiation instantiation;
        private EventBuilder? event_builder;
        private EventInfo? event_info;

        internal EventOnTypeBuilderInst(TypeBuilderInstantiation instantiation, EventBuilder evt)
        {
            this.instantiation = instantiation;
            this.event_builder = evt;
        }

        internal EventOnTypeBuilderInst(TypeBuilderInstantiation instantiation, EventInfo evt)
        {
            this.instantiation = instantiation;
            this.event_info = evt;
        }

        public override EventAttributes Attributes
        {
            get { return event_builder != null ? event_builder.attrs : event_info!.Attributes; }
        }

        public override MethodInfo? GetAddMethod(bool nonPublic)
        {
            MethodInfo? add = event_builder != null ? event_builder.add_method : event_info!.GetAddMethod(nonPublic);
            if (add == null || (!nonPublic && !add.IsPublic))
                return null;
            return TypeBuilder.GetMethod(instantiation, add);
        }

        public override MethodInfo? GetRaiseMethod(bool nonPublic)
        {
            MethodInfo? raise = event_builder != null ? event_builder.raise_method : event_info!.GetRaiseMethod(nonPublic);
            if (raise == null || (!nonPublic && !raise.IsPublic))
                return null;
            return TypeBuilder.GetMethod(instantiation, raise);
        }

        public override MethodInfo? GetRemoveMethod(bool nonPublic)
        {
            MethodInfo? remove = event_builder != null ? event_builder.remove_method : event_info!.GetRemoveMethod(nonPublic);
            if (remove == null || (!nonPublic && !remove.IsPublic))
                return null;
            return TypeBuilder.GetMethod(instantiation, remove);
        }

        public override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            MethodInfo[]? other = event_builder != null ? event_builder.other_methods : event_info!.GetOtherMethods(nonPublic);
            if (other == null)
                return Array.Empty<MethodInfo>();

            List<MethodInfo> res = new List<MethodInfo>();
            foreach (MethodInfo method in other)
            {
                if (nonPublic || method.IsPublic)
                    res.Add(TypeBuilder.GetMethod(instantiation, method));
            }
            return res.ToArray();
        }

        public override Type DeclaringType
        {
            get { return instantiation; }
        }

        public override string Name
        {
            get { return event_builder != null ? event_builder.name : event_info!.Name; }
        }

        public override Type ReflectedType
        {
            get { return instantiation; }
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }
    }
}
#endif
