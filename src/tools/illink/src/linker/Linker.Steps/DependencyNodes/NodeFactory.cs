// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Steps
{
	public partial class MarkStep
	{
		internal sealed class NodeFactory (MarkStep markStep)
		{
			public MarkStep MarkStep { get; } = markStep;

			public static readonly ImmutableDictionary<string, DependencyKind> StringToDependencyKindMap = Enum.GetValues<DependencyKind> ().ToImmutableDictionary (v => v.ToString ());
			public static readonly ImmutableDictionary<DependencyKind, string> DependencyKindToStringMap = Enum.GetValues<DependencyKind> ().ToImmutableDictionary (v => v, v => v.ToString ());

			readonly NodeCache<TypeDefinition, TypeDefinitionNode> _typeNodes = new (static t => new TypeDefinitionNode (t));

			readonly NodeCache<MethodDefinition, MethodDefinitionNode> _methodNodes = new (static _ => throw new InvalidOperationException ("Creation of node requires more than the key."));

			readonly NodeCache<TypeDefinition, TypeIsRelevantToVariantCastingNode> _typeIsRelevantToVariantCastingNodes = new (static (t) => new TypeIsRelevantToVariantCastingNode (t));

			readonly NodeCache<(ITracingNode, string, object?), RootTracingNode> _rootTracingNodes = new (static (tup) => new RootTracingNode (tup.Item1, tup.Item2, tup.Item3));

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

			internal RootTracingNode GetRootTracingNode (ITracingNode newNode, string reason, object? depender)
			{
				return _rootTracingNodes.GetOrAdd ((newNode, reason, depender));
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
