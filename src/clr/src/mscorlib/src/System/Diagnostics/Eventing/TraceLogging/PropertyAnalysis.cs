// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Reflection;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: stores the per-property information obtained by
    /// reflecting over a type.
    /// </summary>
    internal sealed class PropertyAnalysis
    {
        internal readonly string name;
        internal readonly MethodInfo getterInfo;
        internal readonly TraceLoggingTypeInfo typeInfo;
        internal readonly EventFieldAttribute fieldAttribute;

        public PropertyAnalysis(
            string name,
            MethodInfo getterInfo,
            TraceLoggingTypeInfo typeInfo,
            EventFieldAttribute fieldAttribute)
        {
            this.name = name;
            this.getterInfo = getterInfo;
            this.typeInfo = typeInfo;
            this.fieldAttribute = fieldAttribute;
        }
    }
}
