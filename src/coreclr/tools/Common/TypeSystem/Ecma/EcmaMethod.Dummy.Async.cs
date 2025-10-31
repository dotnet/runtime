// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Internal.TypeSystem.Ecma
{
    public sealed partial class EcmaMethod : MethodDesc, EcmaModule.IEntityHandleObject
    {
        public override MethodSignature Signature
        {
            get
            {
                if (_metadataSignature == null)
                    InitializeSignature();
                return _metadataSignature;
            }
        }
    }
}
