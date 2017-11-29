// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Internal;
using Microsoft.Internal.Collections;
using System.Threading;

namespace System.ComponentModel.Composition
{
    public static class AdaptationConstants
    {
        private const string CompositionNamespace = "System.ComponentModel.Composition";

        public const string AdapterContractName = CompositionNamespace + ".AdapterContract";
        public const string AdapterFromContractMetadataName = "FromContract";
        public const string AdapterToContractMetadataName = "ToContract";

    }
}
