// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
[Serializable]
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class IUnknownConstantAttribute : CustomConstantAttribute
    {
        public IUnknownConstantAttribute()
        {
        }

        public override Object Value
        {
            get 
            {
                return new UnknownWrapper(null);
            }
        }

    }
}
