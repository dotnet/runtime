// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;

namespace System.ComponentModel.Composition.Factories
{
    internal partial class ExportProviderFactory
    {
        public static ExportProvider Create()
        {
            return new NoOverridesExportProvider();
        }

        public static RecomposableExportProvider CreateRecomposable()
        {
            return new RecomposableExportProvider();
        }
    }
}
