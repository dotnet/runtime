// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Represents nodes in a Binary Decision Diagram (BDD), which compactly represent sets of integers and allows for fast
    /// querying of whether an integer is in the set, and if so, what value it maps to (typically True or False).
    /// </summary>
    /// <remarks>
    /// All non-leaf nodes have an Ordinal, which indicates the position of the bit the node relates to (0 for the least significant
    /// bit), and two children, One and Zero, for the cases of the current bit being 1 or 0, respectively. An integer
    /// belongs to the set represented by the BDD if the path from the root following the branches that correspond to
    /// the bits of the integer leads to the True leaf. This class also supports multi-terminal BDDs (MTBDD), i.e. ones where
    /// the leaves are something other than True or False, which are used for representing classifiers.
    /// </remarks>
    internal sealed class BDD : IComparable<BDD>
    {
        /// <summary>
        /// The ordinal for the True special value.
        /// </summary>
        private const int TrueOrdinal = -2;

        /// <summary>
        /// The ordinal for the False special value.
        /// </summary>
        private const int FalseOrdinal = -1;

        /// <summary>
        /// The unique BDD leaf that represents the full set or true.
        /// </summary>
        public static readonly BDD True = new BDD(TrueOrdinal, null, null);

        /// <summary>
        /// The unique BDD leaf that represents the empty set or false.
        /// </summary>
        public static readonly BDD False = new BDD(FalseOrdinal, null, null);

        /// <summary>
        /// The encoding of the set for lower ordinals for the case when the current bit is 1.
        /// The value is null iff IsLeaf is true.
        /// </summary>
        public readonly BDD? One;

        /// <summary>
        /// The encoding of the set for lower ordinals for the case when the current bit is 0.
        /// The value is null iff IsLeaf is true.
        /// </summary>
        public readonly BDD? Zero;

        /// <summary>
        /// Ordinal of this bit if nonleaf else MTBDD terminal value when nonnegative
        /// </summary>
        public readonly int Ordinal;

        /// <summary>
        /// Preassigned hashcode value that respects equivalence: equivalent BDDs have equal hashcodes
        /// </summary>
        private readonly int _hashcode;

#if DEBUG // used only for serialization, which is debug-only
        /// <summary>
        /// Representation of False for serialization.
        /// </summary>
        private static readonly long[] s_falseRepresentation = new long[] { 0 };

        /// <summary>
        /// Representation of True for serialization.
        /// </summary>
        private static readonly long[] s_trueRepresentation = new long[] { 1 };

        /// <summary>
        /// Representation of False for compact serialization of BDDs.
        /// </summary>
        private static readonly byte[] s_falseRepresentationCompact = new byte[] { 0 };

        /// <summary>
        /// Representation of True for compact serialization of BDDs.
        /// </summary>
        private static readonly byte[] s_trueRepresentationCompact = new byte[] { 1 };
#endif

        internal BDD(int ordinal, BDD? one, BDD? zero)
        {
            One = one;
            Zero = zero;
            Ordinal = ordinal;

            // Precompute a hashchode value that respects BDD equivalence.
            // Two equivalent BDDs will always have the same hashcode
            // that is independent of object id values of the BDD objects.
            _hashcode = HashCode.Combine(ordinal, one, zero);
        }

        /// <summary>
        /// True iff the node is a terminal (One and Zero are both null).
        /// True and False are terminals.
        /// </summary>
        [MemberNotNullWhen(false, nameof(One))]
        [MemberNotNullWhen(false, nameof(Zero))]
        public bool IsLeaf
        {
            get
            {
                if (One is null)
                {
                    Debug.Assert(Zero is null);
                    return true;
                }

                Debug.Assert(Zero is not null);
                return false;
            }
        }

        /// <summary>
        /// True iff the BDD is True.
        /// </summary>
        public bool IsFull => this == True;

        /// <summary>
        /// True iff the BDD is False.
        /// </summary>
        public bool IsEmpty => this == False;

        /// <summary>
        /// Gets the lexicographically minimum bitvector in this BDD as a ulong.
        /// The BDD must be nonempty.
        /// </summary>
        public ulong GetMin()
        {
            BDD set = this;
            Debug.Assert(!set.IsEmpty);

            if (set.IsFull)
                return 0;

            // starting from all 0, bits will be flipped to 1 as necessary
            ulong res = 0;

            // follow the minimum path throught the branches to a True leaf
            while (!set.IsLeaf)
            {
                if (set.Zero.IsEmpty) //the bit must be set to 1
                {
                    // the bit must be set to 1 when the zero branch is False
                    res |= (ulong)1 << set.Ordinal;
                    // if zero is empty then by the way BDDs are constructed one is not
                    set = set.One;
                }
                else
                {
                    // otherwise, leaving the bit as 0 gives the smaller bitvector
                    set = set.Zero;
                }
            }

            return res;
        }

        /// <summary>
        /// O(1) operation that returns the precomputed hashcode.
        /// </summary>
        public override int GetHashCode() => _hashcode;

        /// <summary>
        /// A shallow equality check that holds if ordinals are identical and one's are identical and zero's are identical.
        /// This equality is used in the _bddCache lookup.
        /// </summary>
        public override bool Equals(object? obj) =>
            obj is BDD bdd &&
            (this == bdd || (Ordinal == bdd.Ordinal && One == bdd.One && Zero == bdd.Zero));

        #region Serialization
#if DEBUG // currently used only from the debug-only code that regenerates the embedded serialized BDD data
        /// <summary>
        /// Serialize this BDD in a flat ulong array. The BDD may have at most 2^k ordinals and 2^n nodes, such that k+2n &lt; 64
        /// BDD.False is represented by return value ulong[]{0}.
        /// BDD.True is represented by return value ulong[]{1}.
        /// Serializer uses more compacted representations when fewer bits are needed, which is reflected in the first
        /// two numbers of the return value. MTBDD terminals are represented by negated numbers as -id.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public long[] Serialize()
        {
            if (IsEmpty)
                return s_falseRepresentation;

            if (IsFull)
                return s_trueRepresentation;

            if (IsLeaf)
                return new long[] { 0, 0, -Ordinal };

            BDD[] nodes = TopologicalSort();

            Debug.Assert(nodes[nodes.Length - 1] == this);
            Debug.Assert(nodes.Length <= (1 << 24));

            // As few bits as possible are used to for ordinals and node identifiers for compact serialization.
            // Use at least a nibble (4 bits) to represent the ordinal and count how many are needed.
            int ordinal_bits = 4;
            while (Ordinal >= (1 << ordinal_bits))
            {
                ordinal_bits += 1;
            }

            // Use at least 2 bits to represent the node identifier and count how many are needed
            int node_bits = 2;
            while (nodes.Length >= (1 << node_bits))
            {
                node_bits += 1;
            }

            // Reserve space for all nodes plus 2 extra: index 0 and 1 are reserved for False and True
            long[] res = new long[nodes.Length + 2];
            res[0] = ordinal_bits;
            res[1] = node_bits;

            //use the following bit layout
            BitLayout(ordinal_bits, node_bits, out int zero_node_shift, out int one_node_shift, out int ordinal_shift);

            //here we know that bdd is neither False nor True
            //but it could still be a MTBDD leaf if both children are null
            var idmap = new Dictionary<BDD, long>
            {
                [True] = 1,
                [False] = 0
            };

            // Give all nodes ascending identifiers and produce their serializations into the result
            for (int i = 0; i < nodes.Length; i++)
            {
                BDD node = nodes[i];
                idmap[node] = i + 2;

                if (node.IsLeaf)
                {
                    // This is MTBDD leaf. Negating it should make it less than or equal to zero, as True and False are
                    // excluded here and MTBDD Ordinals are required to be non-negative.
                    res[i + 2] = -node.Ordinal;
                }
                else
                {
                    // Combine ordinal and child identifiers according to the bit layout
                    long v = (((long)node.Ordinal) << ordinal_shift) | (idmap[node.One] << one_node_shift) | (idmap[node.Zero] << zero_node_shift);
                    Debug.Assert(v >= 0);
                    res[i + 2] = v; // children ids are well-defined due to the topological order of nodes
                }
            }
            return res;
        }

        /// <summary>
        /// Returns a topologically sorted array of all the nodes (other than True or False) in this BDD
        /// such that, all MTBDD leaves (other than True or False) appear first in the array
        /// and all nonterminals with smaller ordinal appear before nodes with larger ordinal.
        /// So this BDD itself (if different from True or False) appears last.
        /// In the case of True or False returns the empty array.
        /// </summary>
        private BDD[] TopologicalSort()
        {
            if (IsFull || IsEmpty)
                return Array.Empty<BDD>();

            if (IsLeaf)
                return new BDD[] { this };

            // Order the nodes according to their ordinals into the nonterminals array
            var nonterminals = new List<BDD>[Ordinal + 1];
            var sorted = new List<BDD>();
            var toVisit = new Stack<BDD>();
            var visited = new HashSet<BDD>();

            toVisit.Push(this);

            while (toVisit.Count > 0)
            {
                BDD node = toVisit.Pop();
                // True and False are not included in the result
                if (node.IsFull || node.IsEmpty)
                    continue;

                if (node.IsLeaf)
                {
                    // MTBDD terminals can be directly added to the sorted nodes, since they have no children that
                    // would come first in the topological ordering.
                    sorted.Add(node);
                }
                else
                {
                    // Non-terminals are grouped by their ordinal so that they can be sorted into a topological order.
                    (nonterminals[node.Ordinal] ??= new List<BDD>()).Add(node);

                    if (visited.Add(node.Zero))
                        toVisit.Push(node.Zero);

                    if (visited.Add(node.One))
                        toVisit.Push(node.One);
                }
            }

            // Flush the grouped non-terminals into the sorted nodes from smallest to highest ordinal. The highest
            // ordinal is guaranteed to have only one node, which places the root of the BDD at the end.
            for (int i = 0; i < nonterminals.Length; i++)
            {
                if (nonterminals[i] != null)
                {
                    sorted.AddRange(nonterminals[i]);
                }
            }

            return sorted.ToArray();
        }

        /// <summary>
        /// Serialize this BDD into a byte array.
        /// This method is not valid for MTBDDs where some elements may be negative.
        /// </summary>
        public byte[] SerializeToBytes()
        {
            if (IsEmpty)
                return s_falseRepresentationCompact;

            if (IsFull)
                return s_trueRepresentationCompact;

            // in other cases make use of the general serializer to long[]
            long[] serialized = Serialize();

            // get the maximal element from the array
            long m = 0;
            for (int i = 0; i < serialized.Length; i++)
            {
                // make sure this serialization is not applied to MTBDDs
                Debug.Assert(serialized[i] > 0);
                m = Math.Max(serialized[i], m);
            }

            // k is the number of bytes needed to represent the maximal element
            int k = m <= 0xFFFF ? 2 : (m <= 0xFF_FFFF ? 3 : (m <= 0xFFFF_FFFF ? 4 : (m <= 0xFF_FFFF_FFFF ? 5 : (m <= 0xFFFF_FFFF_FFFF ? 6 : (m <= 0xFF_FFFF_FFFF_FFFF ? 7 : 8)))));

            // the result will contain k as the first element and the number of serialized elements times k
            byte[] result = new byte[(k * serialized.Length) + 1];
            result[0] = (byte)k;
            for (int i=0; i < serialized.Length; i += 1)
            {
                long serialized_i = serialized[i];
                // add the serialized longs as k-byte subsequences
                for (int j = 1; j <= k; j++)
                {
                    result[(i * k) + j] = (byte)serialized_i;
                    serialized_i = serialized_i >> 8;
                }
            }
            return result;
        }
#endif

        /// <summary>
        /// Recreates a BDD from a byte array that has been created using SerializeToBytes.
        /// </summary>
        public static BDD Deserialize(ReadOnlySpan<byte> bytes, BDDAlgebra algebra)
        {
            if (bytes.Length == 1)
            {
                return bytes[0] == 0 ? False : True;
            }

            // here bytes represents an array of longs with k = the number of bytes used per long
            int bytesPerLong = bytes[0];

            // n is the total nr of longs that corresponds also to the total number of BDD nodes needed
            int n = (bytes.Length - 1) / bytesPerLong;

            // make sure the represented nr of longs divides precisely without remainder
            Debug.Assert((bytes.Length - 1) % bytesPerLong == 0);

            // the number of bits used for ordinals and node identifiers are stored in the first two longs
            int ordinal_bits = (int)Get(bytesPerLong, bytes, 0);
            int node_bits = (int)Get(bytesPerLong, bytes, 1);

            // create bit masks for the sizes of ordinals and node identifiers
            long ordinal_mask = (1 << ordinal_bits) - 1;
            long node_mask = (1 << node_bits) - 1;
            BitLayout(ordinal_bits, node_bits, out int zero_node_shift, out int one_node_shift, out int ordinal_shift);

            // store BDD nodes by their id when they are created
            BDD[] nodes = new BDD[n];
            nodes[0] = False;
            nodes[1] = True;

            for (int i = 2; i < n; i++)
            {
                // represents the triple (ordinal, one, zero)
                long arc = Get(bytesPerLong, bytes, i);

                // reconstruct the ordinal and child identifiers for a non-terminal
                int ord = (int)((arc >> ordinal_shift) & ordinal_mask);
                int oneId = (int)((arc >> one_node_shift) & node_mask);
                int zeroId = (int)((arc >> zero_node_shift) & node_mask);

                // the BDD nodes for the children are guaranteed to exist already
                // because of the topological order they were serialized by
                Debug.Assert(oneId < i && zeroId < i);
                nodes[i] = algebra.GetOrCreateBDD(ord, nodes[oneId], nodes[zeroId]);
            }

            //the result is the last BDD in the nodes array
            return nodes[n - 1];

            // Gets the i'th element from the underlying array of longs represented by bytes
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static long Get(int bytesPerLong, ReadOnlySpan<byte> bytes, int i)
            {
                ulong value = 0;
                for (int j = bytesPerLong; j > 0; j--)
                {
                    value = (value << 8) | bytes[(bytesPerLong * i) + j];
                }

                return (long)value;
            }
        }

        /// <summary>
        /// Use this bit layout in the serialization
        /// </summary>
        private static void BitLayout(int ordinal_bits, int node_bits, out int zero_node_shift, out int one_node_shift, out int ordinal_shift)
        {
            //this bit layout seems to work best: zero,one,ord
            zero_node_shift = ordinal_bits + node_bits;
            one_node_shift = ordinal_bits;
            ordinal_shift = 0;
        }
        #endregion

        /// <summary>
        /// Finds the terminal for the input in a Multi-Terminal-BDD.
        /// Bits of the input are used to determine the path in the BDD.
        /// Returns -1 if False is reached and -2 if True is reached,
        /// else returns the MTBDD terminal number that is reached.
        /// If this is a nonterminal, Find does not care about input bits &gt; Ordinal.
        /// </summary>
        public int Find(int input) =>
            IsLeaf ? Ordinal :
            (input & (1 << Ordinal)) == 0 ? Zero.Find(input) :
            One.Find(input);

        /// <summary>
        /// Finds the terminal for the input in a Multi-Terminal-BDD.
        /// Bits of the input are used to determine the path in the BDD.
        /// Returns -1 if False is reached and 0 if True is reached,
        /// else returns the MTBDD terminal number that is reached.
        /// If this is a nonterminal, Find does not care about input bits &gt; Ordinal.
        /// </summary>
        public int Find(ulong input) =>
            IsLeaf ? Ordinal :
            (input & ((ulong)1 << Ordinal)) == 0 ? Zero.Find(input) :
            One.Find(input);

        /// <summary>
        /// Assumes BDD is not MTBDD and returns true iff it contains the input.
        /// (Otherwise use BDD.Find if this is if fact a MTBDD.)
        /// </summary>
        public bool Contains(int input) => Find(input) == TrueOrdinal; //-2 is the Ordinal of BDD.True

        /// <summary>
        /// Returns true if the only other terminal besides False is a MTBDD terminal that is different from True.
        /// If this is the case, outputs that terminal.
        /// </summary>
        public bool IsEssentiallyBoolean([NotNullWhen(true)] out BDD? terminalActingAsTrue)
        {
            if (IsFull || IsEmpty)
            {
                terminalActingAsTrue = null;
                return false;
            }

            if (IsLeaf)
            {
                terminalActingAsTrue = this;
                return true;
            }

            var toVisit = new Stack<BDD>();
            var visited = new HashSet<BDD>();

            toVisit.Push(this);

            // this will hold the unique MTBDD leaf
            BDD? leaf = null;

            while (toVisit.Count > 0)
            {
                BDD node = toVisit.Pop();
                if (node.IsEmpty)
                    continue;

                if (node.IsFull)
                {
                    //contains the True leaf
                    terminalActingAsTrue = null;
                    return false;
                }

                if (node.IsLeaf)
                {
                    if (leaf is null)
                    {
                        // remember the first MTBDD leaf seen
                        leaf = node;
                    }
                    else if (leaf != node)
                    {
                        // found two different MTBDD leaves
                        terminalActingAsTrue = null;
                        return false;
                    }
                }
                else
                {
                    if (visited.Add(node.Zero))
                        toVisit.Push(node.Zero);

                    if (visited.Add(node.One))
                        toVisit.Push(node.One);
                }
            }

            Debug.Assert(leaf is not null, "this should never happen because there must exist another leaf besides False");
            // found an MTBDD leaf and didn't find any other (non-False) leaves
            terminalActingAsTrue = leaf;
            return true;
        }

        /// <summary>
        /// All terminals precede all nonterminals. Compares Ordinals for terminals.
        /// Compare non-terminals by comparing their minimal elements.
        /// If minimal elements are the same, compare Ordinals.
        /// This provides a total order for terminals.
        /// </summary>
        public int CompareTo(BDD? other)
        {
            if (other is null)
            {
                return -1;
            }

            if (IsLeaf)
            {
                return
                    !other.IsLeaf || Ordinal < other.Ordinal ? -1 :
                    Ordinal == other.Ordinal ? 0 :
                    1;
            }

            if (other.IsLeaf)
            {
                return 1;
            }

            ulong min = GetMin();
            ulong bdd_min = other.GetMin();
            return
                min < bdd_min ? -1 :
                bdd_min < min ? 1 :
                Ordinal.CompareTo(other.Ordinal);
        }
    }
}
