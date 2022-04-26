// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Misc information of structural properties of a <see cref="SymbolicRegexNode{S}"/> that is computed bottom up.</summary>
    internal readonly struct SymbolicRegexInfo : IEquatable<SymbolicRegexInfo>
    {
        private const uint IsAlwaysNullableMask = 1;
        private const uint StartsWithLineAnchorMask = 2;
        private const uint IsLazyMask = 4;
        private const uint CanBeNullableMask = 8;
        private const uint ContainsSomeAnchorMask = 16;
        private const uint StartsWithSomeAnchorMask = 32;

        private readonly uint _info;

        private SymbolicRegexInfo(uint i) => _info = i;

        internal static SymbolicRegexInfo Create(bool isAlwaysNullable = false, bool canBeNullable = false,
            bool startsWithLineAnchor = false, bool startsWithSomeAnchor = false, bool containsSomeAnchor = false, bool isLazy = true)
        {
            uint i = 0;

            if (canBeNullable || isAlwaysNullable)
            {
                i |= CanBeNullableMask;

                if (isAlwaysNullable)
                {
                    i |= IsAlwaysNullableMask;
                }
            }

            if (containsSomeAnchor || startsWithLineAnchor || startsWithSomeAnchor)
            {
                i |= ContainsSomeAnchorMask;

                if (startsWithLineAnchor)
                {
                    i |= StartsWithLineAnchorMask;
                }

                if (startsWithLineAnchor || startsWithSomeAnchor)
                {
                    i |= StartsWithSomeAnchorMask;
                }
            }

            if (isLazy)
            {
                i |= IsLazyMask;
            }

            return new SymbolicRegexInfo(i);
        }

        public bool IsNullable => (_info & IsAlwaysNullableMask) != 0;

        public bool CanBeNullable => (_info & CanBeNullableMask) != 0;

        public bool StartsWithLineAnchor => (_info & StartsWithLineAnchorMask) != 0;

        public bool StartsWithSomeAnchor => (_info & StartsWithSomeAnchorMask) != 0;

        public bool ContainsSomeAnchor => (_info & ContainsSomeAnchorMask) != 0;

        public bool IsLazy => (_info & IsLazyMask) != 0;

        public static SymbolicRegexInfo Or(params SymbolicRegexInfo[] infos)
        {
            uint isLazy = IsLazyMask;
            uint i = 0;

            for (int j = 0; j < infos.Length; j++)
            {
                // Disjunction is lazy if ALL of its members are lazy
                isLazy &= infos[j]._info;
                i |= infos[j]._info;
            }

            i = (i & ~IsLazyMask) | isLazy;
            return new SymbolicRegexInfo(i);
        }

        public static SymbolicRegexInfo And(params SymbolicRegexInfo[] infos)
        {
            uint isLazy = IsLazyMask;
            uint isNullable = IsAlwaysNullableMask | CanBeNullableMask;
            uint i = 0;

            foreach (SymbolicRegexInfo info in infos)
            {
                //nullability and lazyness are conjunctive while other properties are disjunctive
                isLazy &= info._info;
                isNullable &= info._info;
                i |= info._info;
            }

            i = (i & ~IsLazyMask) | isLazy;
            i = (i & ~(IsAlwaysNullableMask | CanBeNullableMask)) | isNullable;
            return new SymbolicRegexInfo(i);
        }

        public static SymbolicRegexInfo Concat(SymbolicRegexInfo left_info, SymbolicRegexInfo right_info) =>
            Create(
                isAlwaysNullable: left_info.IsNullable && right_info.IsNullable,
                canBeNullable: left_info.CanBeNullable && right_info.CanBeNullable,
                startsWithLineAnchor: left_info.StartsWithLineAnchor || (left_info.CanBeNullable && right_info.StartsWithLineAnchor),
                startsWithSomeAnchor: left_info.StartsWithSomeAnchor || (left_info.CanBeNullable && right_info.StartsWithSomeAnchor),
                containsSomeAnchor: left_info.ContainsSomeAnchor || right_info.ContainsSomeAnchor,
                isLazy: left_info.IsLazy && right_info.IsLazy);

        public static SymbolicRegexInfo Loop(SymbolicRegexInfo body_info, int lowerBound, bool isLazy)
        {
            // Inherit anchor visibility from the loop body
            uint i = body_info._info;

            // The loop is nullable if either the body is nullable or if the lower boud is 0
            if (lowerBound == 0)
            {
                i |= IsAlwaysNullableMask | CanBeNullableMask;
            }

            // The loop is lazy iff it is marked lazy
            if (isLazy)
            {
                i |= IsLazyMask;
            }
            else
            {
                i &= ~IsLazyMask;
            }

            return new SymbolicRegexInfo(i);
        }

        public static SymbolicRegexInfo Not(SymbolicRegexInfo info) =>
            // Nullability is complemented, all other properties remain the same
            // The following rules are used to determine nullability of Not(node):
            // Observe that this is used as an over-approximation, actual nullability is checked dynamically based on given context.
            // - If node is never nullable (for any context, info.CanBeNullable=false) then Not(node) is always nullable
            // - If node is always nullable (info.IsNullable=true) then Not(node) can never be nullable
            // For example \B.CanBeNullable=true and \B.IsNullable=false
            // and ~(\B).CanBeNullable=true and ~(\B).IsNullable=false
            Create(isAlwaysNullable: !info.CanBeNullable,
                canBeNullable: !info.IsNullable,
                startsWithLineAnchor: info.StartsWithLineAnchor,
                containsSomeAnchor: info.ContainsSomeAnchor,
                isLazy: info.IsLazy);

        public override bool Equals(object? obj) => obj is SymbolicRegexInfo i && Equals(i);

        public bool Equals(SymbolicRegexInfo other) => _info == other._info;

        public override int GetHashCode() => _info.GetHashCode();

        public override string ToString() => _info.ToString("X");
    }
}
