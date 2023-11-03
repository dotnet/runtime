// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using GenericVariance = Internal.Runtime.GenericVariance;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Describes variance of a generic type definition.
    /// </summary>
    public class GenericVarianceNode : ObjectNode, ISymbolDefinitionNode
    {
        private GenericVarianceDetails _details;

        internal GenericVarianceNode(GenericVarianceDetails details)
        {
            _details = details;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__GenericVariance");

            for (int i = 0; i < _details.Variance.Length; i++)
            {
                sb.Append('_');
                sb.Append((checked((byte)_details.Variance[i])).ToStringInvariant());
            }
        }

        public int Offset => 0;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            if (factory.Target.IsWindows)
                return ObjectNodeSection.FoldableReadOnlyDataSection;
            else
                return ObjectNodeSection.DataSection;
        }

        public override bool IsShareable => true;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            var builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            foreach (var argVariance in _details.Variance)
                builder.EmitByte(checked((byte)argVariance));

            return builder.ToObjectData();
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int ClassCode => -4687913;
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _details.CompareToImpl(((GenericVarianceNode)other)._details);
        }
    }

    internal struct GenericVarianceDetails : IEquatable<GenericVarianceDetails>
    {
        public readonly GenericVariance[] Variance;

        public bool IsNull => Variance == null;

        public GenericVarianceDetails(TypeDesc typeDefinition)
        {
            Debug.Assert(typeDefinition.IsTypeDefinition);
            Debug.Assert(typeDefinition.HasInstantiation);

            Debug.Assert((byte)Internal.TypeSystem.GenericVariance.Contravariant == (byte)GenericVariance.Contravariant);
            Debug.Assert((byte)Internal.TypeSystem.GenericVariance.Covariant == (byte)GenericVariance.Covariant);

            Variance = new GenericVariance[typeDefinition.Instantiation.Length];
            int i = 0;
            foreach (GenericParameterDesc param in typeDefinition.Instantiation)
            {
                Variance[i++] = (GenericVariance)param.Variance;
            }
        }

        public GenericVarianceDetails(GenericVariance[] variance)
        {
            Variance = variance;
        }

        public bool Equals(GenericVarianceDetails other)
        {
            if (Variance.Length != other.Variance.Length)
                return false;

            for (int i = 0; i < Variance.Length; i++)
            {
                if (Variance[i] != other.Variance[i])
                    return false;
            }

            return true;
        }

        public int CompareToImpl(GenericVarianceDetails other)
        {
            var compare = Variance.Length.CompareTo(other.Variance.Length);
            if (compare != 0)
                return compare;

            for (int i = 0; i < Variance.Length; i++)
            {
                compare = Variance[i].CompareTo(other.Variance[i]);
                if (compare != 0)
                    return compare;
            }

            Debug.Assert(Equals(other));
            return 0;
        }

        public override bool Equals(object obj)
        {
            return obj is GenericVarianceDetails && Equals((GenericVarianceDetails)obj);
        }

        public override int GetHashCode()
        {
            int hashCode = 13;

            foreach (byte element in Variance)
            {
                int value = element * 0x5498341 + 0x832424;
                hashCode = hashCode * 31 + value;
            }

            return hashCode;
        }
    }
}
