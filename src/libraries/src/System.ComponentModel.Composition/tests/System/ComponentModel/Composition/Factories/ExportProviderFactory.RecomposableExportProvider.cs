// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Factories
{
    partial class ExportProviderFactory
    {
        public class RecomposableExportProvider : ExportProvider
        {
            public static Dictionary<string, object> EmptyMetadataDictionary = new Dictionary<string, object>();
            public List<Export> _exports = new List<Export>();

            public void AddExport(string contractName, object value)
            {
                Export export = CreateExport(contractName, value);
                var exports = this._exports.ToList();

                exports.Add(export);
                ChangeExports(exports);
            }

            public void RemoveExport(string contractName)
            {
                int index = FindExport(contractName);
                Assert.True(index >= 0);

                var exports = this._exports.ToList();

                exports.RemoveAt(index);

                ChangeExports(exports);
            }

            public void ReplaceExportValue(string contractName, object newValue)
            {
                int index = FindExport(contractName);
                Assert.True(index >= 0);

                var exports = this._exports.ToList();

                exports.RemoveAt(index);
                exports.Add(CreateExport(contractName, newValue));

                ChangeExports(exports);
            }

            private void ChangeExports(List<Export> newExports)
            {
                using (var atomicComposition = new AtomicComposition())
                {
                    atomicComposition.AddCompleteAction(() => this._exports = newExports);
                    atomicComposition.SetValue(this, newExports);

                    var addedExports = newExports.Except(this._exports).Select(export => export.Definition);
                    var removedExports = this._exports.Except(newExports).Select(export => export.Definition);

                    this.OnExportsChanging(new ExportsChangeEventArgs(addedExports, removedExports, atomicComposition));

                    atomicComposition.AddCompleteAction(() => this.OnExportsChanged(
                        new ExportsChangeEventArgs(addedExports, removedExports, null)));

                    atomicComposition.Complete();
                }
            }

            private int FindExport(string contractName)
            {
                for (int i = 0; i < _exports.Count; i++)
                {
                    if (_exports[i].Definition.ContractName == contractName)
                    {
                        return i;
                    }
                }
                return -1;
            }

            private Export CreateExport(string contractName, object value)
            {
                return new Export(new ExportDefinition(contractName, EmptyMetadataDictionary), () => value);
            }

            protected override IEnumerable<Export> GetExportsCore(ImportDefinition importDefinition, AtomicComposition context)
            {
                IEnumerable<Export> contextExports;

                if (context == null || !context.TryGetValue(this, out contextExports))
                {
                    contextExports = this._exports;
                }

                List<Export> exports = new List<Export>();
                var func = importDefinition.Constraint.Compile();
                foreach (Export export in contextExports)
                {
                    if (func(export.Definition))
                    {
                        exports.Add(export);
                    }
                }
                return exports;
            }
        }
    }
}
