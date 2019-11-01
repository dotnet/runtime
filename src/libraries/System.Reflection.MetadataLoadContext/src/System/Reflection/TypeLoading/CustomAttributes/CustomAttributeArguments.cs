// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    //
    // This pairs the Fixed and Named argument lists of a CustomAttributeData in a single conceptual unit (and avoids the
    // the horror of a type name like "KeyValuePair<CustomAttributeTypedArgument, CustomAttributeNamedArgument>")
    //
    internal readonly struct CustomAttributeArguments
    {
        public CustomAttributeArguments(IList<CustomAttributeTypedArgument> fixedArguments, IList<CustomAttributeNamedArgument> namedArguments)
        {
            FixedArguments = fixedArguments;
            NamedArguments = namedArguments;
        }

        public IList<CustomAttributeTypedArgument> FixedArguments { get; }
        public IList<CustomAttributeNamedArgument> NamedArguments { get; }
    }
}
