// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Misc information of structural properties of a <see cref="SymbolicRegexNode{S}"/> that is computed bottom up.</summary>
    internal readonly struct SymbolicRegexInfo
    {
        private const uint IsAlwaysNullableMask = 1;
        private const uint StartsWithLineAnchorMask = 2;
        private const uint IsLazyMask = 4;
        private const uint CanBeNullableMask = 8;
        private const uint ContainsSomeAnchorMask = 16;
        private const uint ContainsLineAnchorMask = 32;
        private const uint ContainsSomeCharacterMask = 64;
        private const uint StartsWithBoundaryAnchorMask = 128;

        private readonly uint _info;

        private SymbolicRegexInfo(uint i) => _info = i;

        /// <summary>Optimized lookup array for most common combinations.</summary>
        /// <remarks>Most common cases will be 0 (no anchors and not nullable) and 1 (no anchors and nullable)</remarks>
        private static readonly SymbolicRegexInfo[] s_infos = CreateSymbolicRegexInfos();

        private static SymbolicRegexInfo[] CreateSymbolicRegexInfos()
        {
            var infos = new SymbolicRegexInfo[128];
            for (uint i = 0; i < infos.Length; i++)
            {
                infos[i] = new SymbolicRegexInfo(i);
            }
            return infos;
        }

        private static SymbolicRegexInfo Mk(uint i)
        {
            SymbolicRegexInfo[] infos = s_infos;
            return i < infos.Length ?
                infos[i] :
                new SymbolicRegexInfo(i);
        }

        internal static SymbolicRegexInfo Mk(bool isAlwaysNullable = false, bool canBeNullable = false, bool startsWithLineAnchor = false,
            bool startsWithBoundaryAnchor = false, bool containsSomeAnchor = false,
            bool containsLineAnchor = false, bool containsSomeCharacter = false, bool isLazy = true)
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

            if (startsWithLineAnchor || containsLineAnchor || startsWithBoundaryAnchor || containsSomeAnchor)
            {
                i |= ContainsSomeAnchorMask;

                if (startsWithLineAnchor || containsLineAnchor)
                {
                    i |= ContainsLineAnchorMask;

                    if (startsWithLineAnchor)
                    {
                        i |= StartsWithLineAnchorMask;
                    }
                }

                if (startsWithBoundaryAnchor)
                {
                    i |= StartsWithBoundaryAnchorMask;
                }
            }

            if (containsSomeCharacter)
            {
                i |= ContainsSomeCharacterMask;
            }

            if (isLazy)
            {
                i |= IsLazyMask;
            }

            return Mk(i);
        }

        public bool IsNullable => (_info & IsAlwaysNullableMask) != 0;

        public bool CanBeNullable => (_info & CanBeNullableMask) != 0;

        public bool StartsWithSomeAnchor => (_info & (StartsWithLineAnchorMask | StartsWithBoundaryAnchorMask)) != 0;

        public bool StartsWithLineAnchor => (_info & StartsWithLineAnchorMask) != 0;

        public bool StartsWithBoundaryAnchor => (_info & StartsWithBoundaryAnchorMask) != 0;

        public bool ContainsSomeAnchor => (_info & ContainsSomeAnchorMask) != 0;

        public bool ContainsLineAnchor => (_info & ContainsLineAnchorMask) != 0;

        public bool ContainsSomeCharacter => (_info & ContainsSomeCharacterMask) != 0;

        public bool IsLazy => (_info & IsLazyMask) != 0;

        public static SymbolicRegexInfo Or(SymbolicRegexInfo[] infos)
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
            return Mk(i);
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
            return Mk(i);
        }

        public static SymbolicRegexInfo Concat(SymbolicRegexInfo left_info, SymbolicRegexInfo right_info)
        {
            bool isNullable = left_info.IsNullable && right_info.IsNullable;
            bool canBeNullable = left_info.CanBeNullable && right_info.CanBeNullable;
            bool isLazy = left_info.IsLazy && right_info.IsLazy;

            bool startsWithLineAnchor = left_info.StartsWithLineAnchor || (left_info.CanBeNullable && right_info.StartsWithLineAnchor);
            bool startsWithBoundaryAnchor = left_info.StartsWithBoundaryAnchor || (left_info.CanBeNullable && right_info.StartsWithBoundaryAnchor);
            bool containsSomeAnchor = left_info.ContainsSomeAnchor || right_info.ContainsSomeAnchor;
            bool containsLineAnchor = left_info.ContainsLineAnchor || right_info.ContainsLineAnchor;
            bool containsSomeCharacter = left_info.ContainsSomeCharacter || right_info.ContainsSomeCharacter;

            return Mk(isNullable, canBeNullable, startsWithLineAnchor, startsWithBoundaryAnchor, containsSomeAnchor, containsLineAnchor, containsSomeCharacter, isLazy);
        }

        public static SymbolicRegexInfo Loop(SymbolicRegexInfo body_info, int lowerBound, bool isLazy)
        {
            // Inherit anchor visibility from the loop body
            uint i = body_info._info;

            // The loop is nullable if either the body is nullable or if the lower boud is 0
            i |= lowerBound == 0 ? (IsAlwaysNullableMask | CanBeNullableMask) : 0;

            // The loop is lazy iff it is marked lazy
            if (isLazy)
            {
                i |= IsLazyMask;
            }
            else
            {
                i &= ~IsLazyMask;
            }

            return Mk(i);
        }

        public static SymbolicRegexInfo Not(SymbolicRegexInfo info) =>
            // Nullability is complemented, all other properties remain the same
            // The following rules are used to determine nullability of Not(node):
            // Observe that this is used as an over-approximation, actual nullability is checked dynamically based on given context.
            // - If node is never nullable (for any context, info.CanBeNullable=false) then Not(node) is always nullable
            // - If node is always nullable (info.IsNullable=true) then Not(node) can never be nullable
            // For example \B.CanBeNullable=true and \B.IsNullable=false
            // and ~(\B).CanBeNullable=true and ~(\B).IsNullable=false
            Mk(isAlwaysNullable: !info.CanBeNullable,
                canBeNullable: !info.IsNullable,
                startsWithLineAnchor: info.StartsWithLineAnchor,
                startsWithBoundaryAnchor: info.StartsWithBoundaryAnchor,
                containsSomeAnchor: info.ContainsSomeAnchor,
                containsLineAnchor: info.ContainsLineAnchor,
                containsSomeCharacter: info.ContainsSomeCharacter,
                isLazy: info.IsLazy);

        public override bool Equals(object? obj) => obj is SymbolicRegexInfo i && i._info == _info;

        public override int GetHashCode() => _info.GetHashCode();

        public override string ToString() => _info.ToString("X");
    }
}
