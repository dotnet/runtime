// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Composition.Registration
{
    // This class exists to enable configuration of PartBuilder<T>
    public class ParameterImportBuilder
    {
        public T Import<T>()
        {
            return default;
        }

        public T Import<T>(Action<ImportBuilder> configure)
        {
            return default;
        }
    }
}
