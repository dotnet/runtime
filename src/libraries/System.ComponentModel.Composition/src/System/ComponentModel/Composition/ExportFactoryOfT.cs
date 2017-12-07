// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    public class ExportFactory<T>
    {
        private Func<Tuple<T, Action>> _exportLifetimeContextCreator;

        public ExportFactory(Func<Tuple<T, Action>> exportLifetimeContextCreator)
        {
            if (exportLifetimeContextCreator == null)
            {
                throw new ArgumentNullException("exportLifetimeContextCreator");
            }

            _exportLifetimeContextCreator = exportLifetimeContextCreator;
        }

        public ExportLifetimeContext<T> CreateExport()
        {
            Tuple<T, Action> untypedLifetimeContext = _exportLifetimeContextCreator.Invoke();
            return new ExportLifetimeContext<T>(untypedLifetimeContext.Item1, untypedLifetimeContext.Item2);
        }
    }
}
