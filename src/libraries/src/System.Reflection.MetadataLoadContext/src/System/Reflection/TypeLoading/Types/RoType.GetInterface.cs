// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all Type and TypeInfo objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoType
    {
        public sealed override Type GetInterface(string name, bool ignoreCase)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            name.SplitTypeName(out string ns, out string simpleName);

            Type match = null;
            foreach (Type ifc in ImplementedInterfaces)
            {
                string ifcSimpleName = ifc.Name;
                bool simpleNameMatches = ignoreCase
                    ? simpleName.Equals(ifcSimpleName, StringComparison.OrdinalIgnoreCase)
                    : simpleName.Equals(ifcSimpleName);
                if (!simpleNameMatches)
                    continue;

                // This check exists for desktop compat: 
                //   (1) caller can optionally omit namespace part of name in pattern- we'll still match. 
                //   (2) ignoreCase:true does not apply to the namespace portion.
                if (ns.Length != 0 && !ns.Equals(ifc.Namespace))
                    continue;
                if (match != null)
                    throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
                match = ifc;
            }
            return match;
        }
    }
}
