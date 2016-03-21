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
    /// This class is used to get an enumerator for the Parts in a container. 
    /// This is a part of the MMCF Packaging Layer APIs
    /// </summary>   
    public class PackagePartCollection : IEnumerable<PackagePart>
    {
        #region Public Methods

        /// <summary>
        /// Returns an enumerator over all the Parts in the container
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }



        /// <summary>
        /// Returns an enumerator over all the Parts in the container
        /// </summary>
        /// <returns></returns>
        IEnumerator<PackagePart> IEnumerable<PackagePart>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator over all the Parts in the Container
        /// </summary>
        /// <returns></returns>
        public IEnumerator<PackagePart> GetEnumerator()
        {
            //The Dictionary.Values property always returns a collection, even if empty. It never returns a null.
            return _partList.Values.GetEnumerator();
        }
        #endregion Public Methods
        
        #region Internal Constructor

        internal PackagePartCollection(SortedList<PackUriHelper.ValidatedPartUri, PackagePart> partList)
        {
            Debug.Assert(partList != null, "partDictionary parameter cannot be null");
            _partList = partList;
        }

        #endregion Internal Constructor
        
        #region Private Members

        private SortedList<PackUriHelper.ValidatedPartUri, PackagePart> _partList;

        #endregion Private Members
    }
}
