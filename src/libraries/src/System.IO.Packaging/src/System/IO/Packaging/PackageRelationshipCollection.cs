// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
//
// Description:
//  This is a class for representing a PackageRelationshipCollection. This is a part of the 
//  MMCF Packaging Layer. 
//
// Details:
//   This class handles serialization to/from relationship parts, creation of those parts
//   and offers methods to create, delete and enumerate relationships.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;               // For Debug.Assert

namespace System.IO.Packaging
{
    /// <summary>
    /// Collection of all the relationships corresponding to a given source PackagePart
    /// </summary>
    public class PackageRelationshipCollection : IEnumerable<PackageRelationship>
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
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

        //------------------------------------------------------
        //
        //  Internal Members
        //
        //------------------------------------------------------
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

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        //  None
        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
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
                Debug.Assert(filter.Trim() != String.Empty,
                    "RelationshipType filter cannot be empty string or a string with just spaces");

                _enumerator = enumerator;
                _filter = filter;
            }

            #endregion Constructor

            #region IEnumerator Methods

            /// <summary>
            /// This method keeps moving the enumerator the the next position till
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
            Object IEnumerator.Current
            {
                get
                {
                    return _enumerator.Current;
                }
            }

            /// <summary>
            /// Resets the enumerator to the begining
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
                if (String.CompareOrdinal(r.RelationshipType, _filter) == 0)
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
