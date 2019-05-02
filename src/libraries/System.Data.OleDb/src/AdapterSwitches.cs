// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if DEBUG

using System.Diagnostics;

namespace System.Data.Common
{
    internal static class AdapterSwitches
    {
        static private TraceSwitch _dataSchema;

        static internal TraceSwitch DataSchema
        {
            get
            {
                TraceSwitch dataSchema = _dataSchema;
                if (null == dataSchema)
                {
                    _dataSchema = dataSchema = new TraceSwitch("Data.Schema", "Enable tracing for schema actions.");
                }
                return dataSchema;
            }
        }
    }
}
#endif
