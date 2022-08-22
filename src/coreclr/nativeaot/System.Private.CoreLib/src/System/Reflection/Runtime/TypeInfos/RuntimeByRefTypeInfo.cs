// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
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

        protected sealed override bool IsArrayImpl() => false;
        public sealed override bool IsSZArray => false;
        public sealed override bool IsVariableBoundArray => false;
        protected sealed override bool IsByRefImpl() => true;
        protected sealed override bool IsPointerImpl() => false;

        protected sealed override string Suffix
        {
            get
            {
                return "&";
            }
        }
    }
}
