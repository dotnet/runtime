// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class BestFitMappingAttribute : Attribute
    {
        public BestFitMappingAttribute(bool BestFitMapping)
        {
            this.BestFitMapping = BestFitMapping;
        }

        public bool BestFitMapping { get; }

        public bool ThrowOnUnmappableChar;
    }
}
