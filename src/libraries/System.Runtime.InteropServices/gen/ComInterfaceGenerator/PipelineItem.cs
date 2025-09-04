// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal sealed class PipelineItem<T> : IEquatable<PipelineItem<T>>
    {
        private T _item;
        private ImmutableArray<SyntaxNode> _syntaxNodes;
        public SyntaxNode this[int index] => _syntaxNodes[index];
        public PipelineItem(T item, params ImmutableArray<SyntaxNode> nodes)
        {
            _item = item;
            _syntaxNodes = nodes;
        }

        public T Context => _item;

        public PipelineItem<T> WithNode(SyntaxNode node)
        {
            return new PipelineItem<T>(_item, _syntaxNodes.Add(node));
        }

        public bool Equals(PipelineItem<T> other)
        {
            if (other is null) return false;
            if (!other._item.Equals(_item)) return false;
            if (other._syntaxNodes.Length != _syntaxNodes.Length) return false;
            for (int i = 0; i < _syntaxNodes.Length; i++)
            {
                var item = _syntaxNodes[i];
                var otherItem = other._syntaxNodes[i];

                if (SyntaxEquivalentComparer.Instance.Equals(item, otherItem))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PipelineItem<T>);
        }

        public override int GetHashCode()
        {
            return _item.GetHashCode();
        }
    }
}
