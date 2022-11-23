// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Antlr4.Runtime;
namespace ILAssembler;

internal sealed class TypeDefs
{
    private readonly Dictionary<string, ParserRuleContext> _typeDefs = new();

    public bool HasTypeDefOfType<TContext>(string name) => _typeDefs.TryGetValue(name, out var context) && context is TContext;

    public TContext GetTypeDefResult<TContext>(string name)
        where TContext: ParserRuleContext
    {
        return (TContext)_typeDefs[name];
    }

    public void Add(string name, ParserRuleContext target)
    {
        _typeDefs.Add(name, target);
    }
}
