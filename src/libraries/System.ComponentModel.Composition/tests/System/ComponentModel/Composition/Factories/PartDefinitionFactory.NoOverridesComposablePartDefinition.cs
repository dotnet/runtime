// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Factories
{
    partial class PartDefinitionFactory
    {
        // NOTE: Do not add any more behavior to this class, as ComposablePartDefinitionTests.cs 
        // uses this to verify default behavior of the base class.
        private class NoOverridesComposablePartDefinition : ComposablePartDefinition
        {
            public override IEnumerable<ExportDefinition> ExportDefinitions
            {
                get { return Enumerable.Empty<ExportDefinition>(); }
            }

            public override IEnumerable<ImportDefinition> ImportDefinitions
            {
                get { return Enumerable.Empty<ImportDefinition>(); }
            }

            public override ComposablePart CreatePart()
            {
                throw new NotImplementedException();
            }
        }
    }
}
