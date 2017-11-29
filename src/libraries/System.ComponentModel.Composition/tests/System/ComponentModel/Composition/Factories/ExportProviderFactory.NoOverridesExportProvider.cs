// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Factories
{
    partial class ExportProviderFactory
    {
        // NOTE: Do not add any more behavior to this class, as ExportProviderTests.cs 
        // uses this to verify default behavior of the base class.
        private class NoOverridesExportProvider : ExportProvider
        {
            public NoOverridesExportProvider()
            {
            }

            protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition context)
            {
                throw new NotImplementedException();
            }
        }
    }

}
