// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Composition
{
    public class ExportFactory<T, TMetadata> : ExportFactory<T>
    {
        private readonly TMetadata _metadata;

        public ExportFactory(Func<Tuple<T, Action>> exportLifetimeContextCreator, TMetadata metadata)
            : base(exportLifetimeContextCreator)
        {
            _metadata = metadata;
        }

        public TMetadata Metadata
        {
            get { return _metadata; }
        }
    }
}
