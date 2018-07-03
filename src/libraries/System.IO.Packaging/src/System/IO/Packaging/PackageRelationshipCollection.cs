// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.IO.Packaging
{
    /// <summary>
    /// Collection of all the relationships corresponding to a given source PackagePart.
    /// This class is part of the MMCF Packaging Layer. It handles serialization to/from
    /// relationship parts, creation of those parts and offers methods to create, delete 
    /// and enumerate relationships.
    /// </summary>
    public class PackageRelationshipCollection : IEnumerable<PackageRelationship>
    {
        #region IEnumerable

        /// <summary>
        /// Returns an enumerator for all the relationships for a PackagePart
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        /// <summary>
        /// Returns an enumerator over all the relationships for a PackagePart
        /// </summary>
        /// <returns></returns>
        public IEnumerator<PackageRelationship> GetEnumerator()
        {
            List<PackageRelationship>.Enumerator relationshipsEnumerator = _relationships.GetEnumerator();

            if (_filter == null)
                return relationshipsEnumerator;
            else
                return new FilteredEnumerator(relationshipsEnumerator, _filter);
        }
        #endregion
        
        #region Internal Members
        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>For use by PackagePart</remarks>
        internal PackageRelationshipCollection(InternalRelationshipCollection relationships, string filter)
        {
            Debug.Assert(relationships != null, "relationships parameter cannot be null");

            _relationships = relationships;
            _filter = filter;
        }

        #endregion
        
        #region Private Members

        private InternalRelationshipCollection _relationships;
        private string _filter;

        #endregion

        #region Private Class

        #region FilteredEnumerator Class

        /// <summary>
        /// Internal class for the FilteredEnumerator        
        /// </summary>
        private sealed class FilteredEnumerator : IEnumerator<PackageRelationship>
        {
            #region Constructor

            /// <summary>
            /// Constructs a FilteredEnumerator
            /// </summary>
            /// <param name="enumerator"></param>
            /// <param name="filter"></param>
            internal FilteredEnumerator(IEnumerator<PackageRelationship> enumerator, string filter)
            {
                Debug.Assert((enumerator != null), "Enumerator cannot be null");
                Debug.Assert(filter != null, "PackageRelationship filter string cannot be null");

                // Look for empty string or string with just spaces
                Debug.Assert(filter.Trim() != string.Empty,
                    "RelationshipType filter cannot be empty string or a string with just spaces");

                _enumerator = enumerator;
                _filter = filter;
            }

            #endregion Constructor

            #region IEnumerator Methods

            /// <summary>
            /// This method keeps moving the enumerator the next position till
            /// a relationship is found with the matching Name
            /// </summary>
            /// <returns>Bool indicating if the enumerator successfully moved to the next position</returns>
            bool IEnumerator.MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    if (RelationshipTypeMatches())
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Gets the current object in the enumerator
            /// </summary>
            /// <value></value>
            object IEnumerator.Current
            {
                get
                {
                    return _enumerator.Current;
                }
            }

            /// <summary>
            /// Resets the enumerator to the beginning
            /// </summary>
            void IEnumerator.Reset()
            {
                _enumerator.Reset();
            }

            #endregion IEnumerator Methods

            #region IEnumerator<PackageRelationship> Members


            /// <summary>
            /// Gets the current object in the enumerator
            /// </summary>
            /// <value></value>
            public PackageRelationship Current
            {
                get
                {
                    return _enumerator.Current;
                }
            }

            #endregion IEnumerator<PackageRelationship> Members

            #region IDisposable Members

            public void Dispose()
            {
                //Most enumerators have dispose as a no-op, we follow the same pattern. 
                _enumerator.Dispose();
            }

            #endregion IDisposable Members

            #region Private Methods

            private bool RelationshipTypeMatches()
            {
                PackageRelationship r = _enumerator.Current;

                //Case-sensitive comparison
                if (string.CompareOrdinal(r.RelationshipType, _filter) == 0)
                    return true;
                else
                    return false;
            }

            #endregion Private Methods

            #region Private Members

            private IEnumerator<PackageRelationship> _enumerator;
            private string _filter;

            #endregion Private Members
        }

        #endregion FilteredEnumerator Class

        #endregion Private Class
    }
}
