// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Composition.Hosting.Core
{
    // Update results ensure that providers can query reentrantly for the contract that
    // they are being queried for. The UpdateResult type keeps a list of remaining
    // providers, with providers being removed from the list before querying.
    internal sealed class UpdateResult
    {
        private static readonly ExportDescriptorPromise[] s_noPromises = Array.Empty<ExportDescriptorPromise>();

        private readonly Queue<ExportDescriptorProvider> _remainingProviders;
        private readonly IList<ExportDescriptorPromise> _providedDescriptors = new List<ExportDescriptorPromise>();
        private ExportDescriptorPromise[] _results;

        public UpdateResult(IEnumerable<ExportDescriptorProvider> providers)
        {
            _remainingProviders = new Queue<ExportDescriptorProvider>(providers);
        }

        public bool TryDequeueNextProvider(out ExportDescriptorProvider provider)
        {
            if (_remainingProviders.Count == 0)
            {
                provider = null;
                return false;
            }

            provider = _remainingProviders.Dequeue();
            return true;
        }

        public void AddPromise(ExportDescriptorPromise promise)
        {
            _results = null;
            _providedDescriptors.Add(promise);
        }

        public ExportDescriptorPromise[] GetResults()
        {
            if (_results == null)
            {
                if (_remainingProviders.Count != 0)
                {
                    throw new Exception(SR.Providers_Remain_To_Be_Queried);
                }

                if (_providedDescriptors.Count == 0)
                    _results = s_noPromises;

                _results = _providedDescriptors.ToArray();
            }

            return _results;
        }
    }
}
