// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// Data structure for use in CloneAndReplace
    /// </summary>
    /// <remarks>Isolates the many QilNode classes from changes in
    /// the underlying data structure.</remarks>
    internal sealed class SubstitutionList
    {
        // BUGBUG Find an efficient collection internal class to support this
        private ArrayList _s;

        public SubstitutionList()
        {
            _s = new ArrayList(4);
        }

        /// <summary>
        /// Add a substitution pair
        /// </summary>
        /// <param name="find">a node to be replaced</param>
        /// <param name="replace">its replacement</param>
        public void AddSubstitutionPair(QilNode find, QilNode replace)
        {
            _s.Add(find);
            _s.Add(replace);
        }

        /// <summary>
        /// Remove the last a substitution pair
        /// </summary>
        public void RemoveLastSubstitutionPair()
        {
            _s.RemoveRange(_s.Count - 2, 2);
        }

        /// <summary>
        /// Find the replacement for a node
        /// </summary>
        /// <param name="n">the node to replace</param>
        /// <returns>null if no replacement is found</returns>
        public QilNode FindReplacement(QilNode n)
        {
            Debug.Assert(_s.Count % 2 == 0);
            for (int i = _s.Count - 2; i >= 0; i -= 2)
                if (_s[i] == n)
                    return (QilNode)_s[i + 1];
            return null;
        }
    }
}
