// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//-----------------------------------------------------------------------------
//
// Description:
//  This class represents a PackageRelationshipSelector. 
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;                  // for Debug.Assert

namespace System.IO.Packaging
{
    /// <summary>
    /// This class is used to represent a PackageRelationship selector. PackageRelationships can be
    /// selected based on their Type or ID. This class will specify what the selection is based on and
    /// what the actual criteria is. </summary>
    public sealed class PackageRelationshipSelector
    {
        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Public Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sourceUri">Source Uri of the PackagePart or PackageRoot ("/") that owns the relationship</param>
        /// <param name="selectorType">PackageRelationshipSelectorType enum representing the type of the selectionCriteria</param>
        /// <param name="selectionCriteria">The actual string that is used to select the relationships</param>
        /// <exception cref="ArgumentNullException">If sourceUri is null</exception>
        /// <exception cref="ArgumentNullException">If selectionCriteria is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If selectorType Enumeration does not have a valid value</exception>
        /// <exception cref="System.Xml.XmlException">If PackageRelationshipSelectorType.Id and selection criteria is not valid Xsd Id</exception>
        /// <exception cref="ArgumentException">If PackageRelationshipSelectorType.Type and selection criteria is not valid relationship type</exception>
        /// <exception cref="ArgumentException">If sourceUri is not "/" to indicate the PackageRoot, then it must conform to the 
        /// valid PartUri syntax</exception>
        public PackageRelationshipSelector(Uri sourceUri, PackageRelationshipSelectorType selectorType, string selectionCriteria)
        {
            if (sourceUri == null)
                throw new ArgumentNullException("sourceUri");

            if (selectionCriteria == null)
                throw new ArgumentNullException("selectionCriteria");

            //If the sourceUri is not equal to "/", it must be a valid part name.
            if (Uri.Compare(sourceUri, PackUriHelper.PackageRootUri, UriComponents.SerializationInfoString, UriFormat.UriEscaped, StringComparison.Ordinal) != 0)
                sourceUri = PackUriHelper.ValidatePartUri(sourceUri);

            //selectionCriteria is tested here as per the value of the selectorType.
            //If selectionCriteria is empty string we will throw the appropriate error message.
            if (selectorType == PackageRelationshipSelectorType.Type)
                InternalRelationshipCollection.ThrowIfInvalidRelationshipType(selectionCriteria);
            else
            if (selectorType == PackageRelationshipSelectorType.Id)
                InternalRelationshipCollection.ThrowIfInvalidXsdId(selectionCriteria);
            else
                throw new ArgumentOutOfRangeException("selectorType");

            _sourceUri = sourceUri;
            _selectionCriteria = selectionCriteria;
            _selectorType = selectorType;
        }

        #endregion Public Constructor
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// This is a uri to the parent PackagePart to which this relationship belongs.
        /// </summary>
        /// <value>PackagePart</value>
        public Uri SourceUri
        {
            get
            {
                return _sourceUri;
            }
        }


        /// <summary>
        /// Enumeration value indicating the interpretations of the SelectionCriteria.
        /// </summary>
        /// <value></value>
        public PackageRelationshipSelectorType SelectorType
        {
            get
            {
                return _selectorType;
            }
        }


        /// <summary>
        /// Selection Criteria - actual value (could be ID or type) on which the selection is based
        /// </summary>
        /// <value></value>
        public string SelectionCriteria
        {
            get
            {
                return _selectionCriteria;
            }
        }


        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// This method returns the list of selected PackageRelationships as per the
        /// given criteria, from a part in the Package provided
        /// </summary>
        /// <param name="package">Package object from which we get the relationsips</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If package parameter is null</exception>
        public List<PackageRelationship> Select(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            List<PackageRelationship> relationships = new List<PackageRelationship>(0);

            switch (SelectorType)
            {
                case PackageRelationshipSelectorType.Id:
                    if (SourceUri.Equals(PackUriHelper.PackageRootUri))
                    {
                        if (package.RelationshipExists(SelectionCriteria))
                            relationships.Add(package.GetRelationship(SelectionCriteria));
                    }
                    else
                    {
                        if (package.PartExists(SourceUri))
                        {
                            PackagePart part = package.GetPart(SourceUri);
                            if (part.RelationshipExists(SelectionCriteria))
                                relationships.Add(part.GetRelationship(SelectionCriteria));
                        }
                    }
                    break;

                case PackageRelationshipSelectorType.Type:
                    if (SourceUri.Equals(PackUriHelper.PackageRootUri))
                    {
                        foreach (PackageRelationship r in package.GetRelationshipsByType(SelectionCriteria))
                            relationships.Add(r);
                    }
                    else
                    {
                        if (package.PartExists(SourceUri))
                        {
                            foreach (PackageRelationship r in package.GetPart(SourceUri).GetRelationshipsByType(SelectionCriteria))
                                relationships.Add(r);
                        }
                    }
                    break;

                default:
                    //Debug.Assert is fine here since the parameters have already been validated. And all the properties are 
                    //readonly
                    Debug.Assert(false, "This option should never be called");
                    break;
            }

            return relationships;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Members

        private Uri _sourceUri;
        private string _selectionCriteria;
        private PackageRelationshipSelectorType _selectorType;

        #endregion Private Members
    }
}
