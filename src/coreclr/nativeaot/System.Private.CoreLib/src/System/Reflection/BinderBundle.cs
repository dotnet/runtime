// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Reflection
{
    // If allocated, indicates the non-default Binder and culture to use for coercing parameters to a dynamic Invoke. Combining
    // these two items in a class makes custom binders more pay-for-play (one less parameter and TLS local for the non-binder case
    // to manage.)
    //
    // This is not an api type but needs to be public as both Reflection.Core and System.Private.Corelib accesses it.
    public sealed class BinderBundle
    {
        public BinderBundle(Binder binder, CultureInfo culture)
        {
            // This is not just performance, it is correctness too. The default binder's ChangeType() method throws a NotSupportedException so you really can't treat it
            // as "just another binder."
            Debug.Assert(binder != null && binder != Type.DefaultBinder, "Not permitted to allocate a BinderBundle for the default Binder. Must pass a null BinderBundle instread.");
            _binder = binder;
            _culture = culture;
        }

        public object ChangeType(object value, Type type)
        {
            return _binder.ChangeType(value, type, _culture);
        }

        private readonly Binder _binder;
        private readonly CultureInfo _culture;
    }
}
