// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Microsoft.CLR.UnitTesting;

namespace System.ComponentModel.Composition.Factories
{
    partial class ContainerFactory
    {
        // NOTE: Do not add any more behavior to this class, as CompositionContainerTests.cs 
        // uses this to verify default behavior of the base class.
        private class NoOverridesCompositionContainer : CompositionContainer
        {
            public NoOverridesCompositionContainer()
            {                
            }
        }
    }
}
