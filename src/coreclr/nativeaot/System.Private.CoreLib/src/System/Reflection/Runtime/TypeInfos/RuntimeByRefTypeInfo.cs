// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // The runtime's implementation of TypeInfo's for byref types.
    //
    internal sealed partial class RuntimeByRefTypeInfo : RuntimeHasElementTypeInfo
    {
        private RuntimeByRefTypeInfo(UnificationKey key)
            : base(key)
        {
        }

        public override bool IsByRef => true;

        protected override string Suffix
        {
            get
            {
                return "&";
            }
        }
    }
}
