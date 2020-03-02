﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal sealed class SplatCallSite
    {
        // Stored callable Delegate or IDynamicMetaObjectProvider.
        internal readonly object _callable;

        // Can the number of arguments to a given event change each call?
        // If not, we don't need this level of indirection--we could cache a
        // delegate that does the splatting.
        internal CallSite<Func<CallSite, object, object[], object>> _site;

        internal SplatCallSite(object callable)
        {
            Debug.Assert(callable != null);
            _callable = callable;
        }

        internal object Invoke(object[] args)
        {
            Debug.Assert(args != null);

            // If it is a delegate, just let DynamicInvoke do the binding.
            if (_callable is Delegate d)
            {
                return d.DynamicInvoke(args);
            }

            // Otherwise, create a CallSite and invoke it.
            if (_site == null)
            {
                _site = CallSite<Func<CallSite, object, object[], object>>.Create(SplatInvokeBinder.s_instance);
            }

            return _site.Target(_site, _callable, args);
        }
    }
}
