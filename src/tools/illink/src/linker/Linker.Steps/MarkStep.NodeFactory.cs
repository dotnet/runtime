// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public partial class MarkStep
	{
		internal sealed class NodeFactory (MarkStep markStep)
		{
			public MarkStep MarkStep { get; } = markStep;
			readonly NodeCache<TypeDefinition, TypeDefinitionNode> _typeNodes = new (static t => new TypeDefinitionNode(t));
			readonly NodeCache<MethodDefinition, MethodDefinitionNode> _methodNodes = new (static _ => throw new InvalidOperationException ("Creation of node requires more than the key."));
			readonly NodeCache<TypeDefinition, TypeIsRelevantToVariantCastingNode> _typeIsRelevantToVariantCastingNodes = new (static (t) => new TypeIsRelevantToVariantCastingNode (t));

			internal TypeDefinitionNode GetTypeNode (TypeDefinition definition)
			{
				return _typeNodes.GetOrAdd (definition);
			}

			internal MethodDefinitionNode GetMethodDefinitionNode (MethodDefinition method, DependencyInfo reason)
			{
				return _methodNodes.GetOrAdd (method, (k) => new MethodDefinitionNode (k, reason));
			}

			internal TypeIsRelevantToVariantCastingNode GetTypeIsRelevantToVariantCastingNode (TypeDefinition type)
			{
				return _typeIsRelevantToVariantCastingNodes.GetOrAdd (type);
			}

			struct NodeCache<TKey, TValue> where TKey : notnull
			{
				// Change to concurrent dictionary if/when multithreaded marking is enabled
				readonly Dictionary<TKey, TValue> _cache;
				readonly Func<TKey, TValue> _creator;

				public NodeCache (Func<TKey, TValue> creator, IEqualityComparer<TKey> comparer)
				{
					_creator = creator;
					_cache = new (comparer);
				}

				public NodeCache (Func<TKey, TValue> creator)
				{
					_creator = creator;
					_cache = new ();
				}

				public TValue GetOrAdd (TKey key)
				{
					return _cache.GetOrAdd (key, _creator);
				}

				public TValue GetOrAdd (TKey key, Func<TKey, TValue> creator)
				{
					return _cache.GetOrAdd (key, creator);
				}
			}
		}
	}
}
