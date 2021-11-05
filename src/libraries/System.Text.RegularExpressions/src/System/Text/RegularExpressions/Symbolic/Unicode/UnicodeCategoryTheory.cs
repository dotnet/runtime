// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic.Unicode
{
    internal sealed class UnicodeCategoryTheory<TPredicate> where TPredicate : class
    {
        internal readonly ICharAlgebra<TPredicate> _solver;
        private readonly TPredicate[] _catConditions = new TPredicate[30];

        private TPredicate? _whiteSpaceCondition;
        private TPredicate? _wordLetterCondition;
        private TPredicate? _wordLetterConditionForAnchors;

        public UnicodeCategoryTheory(ICharAlgebra<TPredicate> solver) => _solver = solver;

        public TPredicate CategoryCondition(int i)
        {
            if (_catConditions[i] is not TPredicate condition)
            {
                BDD bdd = BDD.Deserialize(UnicodeCategoryRanges.AllCategoriesSerializedBDD[i], _solver.CharSetProvider);
                _catConditions[i] = condition = _solver.ConvertFromCharSet(_solver.CharSetProvider, bdd);
            }

            return condition;
        }

        public TPredicate WhiteSpaceCondition
        {
            get
            {
                if (_whiteSpaceCondition is not TPredicate condition)
                {
                    BDD bdd = BDD.Deserialize(UnicodeCategoryRanges.WhitespaceSerializedBDD, _solver.CharSetProvider);
                    _whiteSpaceCondition = condition = _solver.ConvertFromCharSet(_solver.CharSetProvider, bdd);
                }

                return condition;
            }
        }

        public TPredicate WordLetterCondition
        {
            get
            {
                if (_wordLetterCondition is not TPredicate condition)
                {
                    // \w is the union of the 8 categories: 0,1,2,3,4,5,8,18
                    TPredicate[] predicates = new TPredicate[] {
                        CategoryCondition(0), CategoryCondition(1), CategoryCondition(2), CategoryCondition(3),
                        CategoryCondition(4), CategoryCondition(5), CategoryCondition(8), CategoryCondition(18)};
                    _wordLetterCondition = condition = _solver.Or(predicates);
                }

                return condition;
            }
        }

        public TPredicate WordLetterConditionForAnchors
        {
            get
            {
                if (_wordLetterConditionForAnchors is not TPredicate condition)
                {
                    // Create the condition from WordLetterCondition together with the characters
                    // \u200C (zero width non joiner) and \u200D (zero width joiner) that are treated
                    // as if they were word characters in the context of the anchors \b and \B
                    BDD extra_bdd = _solver.CharSetProvider.CreateCharSetFromRange('\u200C', '\u200D');
                    TPredicate extra_pred = _solver.ConvertFromCharSet(_solver.CharSetProvider, extra_bdd);
                    _wordLetterConditionForAnchors = condition = _solver.Or(WordLetterCondition, extra_pred);
                }

                return condition;
            }
        }
    }
}
