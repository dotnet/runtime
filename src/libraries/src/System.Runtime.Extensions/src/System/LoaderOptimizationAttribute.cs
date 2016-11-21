// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LoaderOptimizationAttribute : Attribute
    {
        private readonly byte _val;
        public LoaderOptimizationAttribute(byte value) 
        { 
            _val = value;
        }
        public LoaderOptimizationAttribute(LoaderOptimization value) 
        {
            _val = (byte) value;
        }
        public LoaderOptimization Value => (LoaderOptimization)_val;
    }
}
