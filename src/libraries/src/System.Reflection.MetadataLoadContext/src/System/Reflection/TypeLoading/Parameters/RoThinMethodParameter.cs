// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all RoParameter's returned by MethodBase.GetParameters() that don't have an entry in the Param table.
    /// (in practice, these are return value "parameters.") These parameters have no name, custom attributes or default values.
    /// </summary>
    internal sealed class RoThinMethodParameter : RoMethodParameter
    {
        internal RoThinMethodParameter(IRoMethodBase roMethodBase, int position, Type parameterType) 
            : base(roMethodBase, position, parameterType)
        {
            Debug.Assert(roMethodBase != null);
            Debug.Assert(parameterType != null);
        }

        public sealed override string Name => null;
        public sealed override ParameterAttributes Attributes => ParameterAttributes.None;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => Array.Empty<CustomAttributeData>();

        public sealed override int MetadataToken => 0x08000000; // nil ParamDef token

        public sealed override bool HasDefaultValue => true; // Compat: returning "true" makes no sense but this is how it's always been.

        // Returning "null" matches the desktop behavior, though this is inconsistent with the DBNull/Missing values
        // returned by fat ParameterInfo's without default values. 
        public sealed override object RawDefaultValue => null; 
    }
}
