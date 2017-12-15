// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Factories
{
    partial class PartFactory
    {
        // NOTE: Do not add any more behavior to this class, as ComposablePartTests.cs 
        // uses this to verify default behavior of the base class.
        private class DisposableComposablePart : ComposablePart, IDisposable
        {
            private readonly Action<bool> _disposeCallback;

            public DisposableComposablePart(Action<bool> disposeCallback)
            {
                Assert.NotNull(disposeCallback);

                _disposeCallback = disposeCallback;
            }

            public void Dispose()
            {
                this.Dispose(true);
            }

            ~DisposableComposablePart()
            {
                Dispose(false);
            }

            protected virtual void Dispose(bool disposing)
            {
                _disposeCallback(disposing);
            }

            public override IEnumerable<ImportDefinition> ImportDefinitions
            {
                get { return Enumerable.Empty<ImportDefinition>(); }
            }

            public override IEnumerable<ExportDefinition> ExportDefinitions
            {
                get { return Enumerable.Empty<ExportDefinition>(); }
            }

            public override object GetExportedValue(ExportDefinition definition)
            {
                throw new NotImplementedException();
            }

            public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
            {
                throw new NotImplementedException();
            }

            public override IDictionary<string, object> Metadata
            {
                get { return new Dictionary<string, object>(); }
            }
        }
    }
}
