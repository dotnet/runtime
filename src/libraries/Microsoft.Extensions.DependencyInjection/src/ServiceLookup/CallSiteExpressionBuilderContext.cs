// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CallSiteExpressionBuilderContext
    {
        public ParameterExpression ScopeParameter { get; set; }
        public bool RequiresResolvedServices { get; set; }
    }
}