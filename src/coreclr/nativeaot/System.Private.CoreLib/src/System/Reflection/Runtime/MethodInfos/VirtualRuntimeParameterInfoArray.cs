// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    // Helper for GetRuntimeParameters() - array mimic that supports an efficient "array.Skip(1).ToArray()" operation.
    internal struct VirtualRuntimeParameterInfoArray
    {
        public VirtualRuntimeParameterInfoArray(int count)
            : this()
        {
            Debug.Assert(count >= 1);
            Remainder = (count == 1) ? Array.Empty<RuntimeParameterInfo>() : new RuntimeParameterInfo[count - 1];
        }

        public RuntimeParameterInfo this[int index]
        {
            get
            {
                return index == 0 ? First : Remainder[index - 1];
            }

            set
            {
                if (index == 0)
                    First = value;
                else
                    Remainder[index - 1] = value;
            }
        }

        public RuntimeParameterInfo First { get; private set; }
        public RuntimeParameterInfo[] Remainder { get; }
    }
}
