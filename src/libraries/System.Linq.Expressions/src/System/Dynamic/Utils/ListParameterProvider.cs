// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;

namespace System.Dynamic.Utils
{
    /// <summary>
    /// See <see cref="ListArgumentProvider"/> for design considerations.
    /// </summary>
    internal sealed class ListParameterProvider : ListProvider<ParameterExpression>
    {
        private readonly IParameterProvider _provider;
        private readonly ParameterExpression _arg0;

        internal ListParameterProvider(IParameterProvider provider, ParameterExpression arg0)
        {
            _provider = provider;
            _arg0 = arg0;
        }

        protected override ParameterExpression First => _arg0;
        protected override int ElementCount => _provider.ParameterCount;
        protected override ParameterExpression GetElement(int index) => _provider.GetParameter(index);
    }
}
