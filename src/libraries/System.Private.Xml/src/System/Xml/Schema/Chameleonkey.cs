// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Xml.Serialization;

    // Case insensitive file name key for use in a hashtable.

    internal class ChameleonKey
    {
        internal string targetNS;
        internal Uri chameleonLocation;
        // Original schema (used for reference equality only)
        //   stored only when the chameleonLocation is an empty URI in which case the location
        //   is not a good enough identification of the schema
        internal XmlSchema originalSchema;
        private int _hashCode;

        /// <summary>
        /// Creates a new chameleon key - an identification for a chameleon schema instance
        /// </summary>
        /// <param name="ns">The target namespace of the instance of the chameleon schema</param>
        /// <param name="originalSchema">The original (chameleon) schema (the one without the target namespace).
        ///   This is used to get the location (base uri) and to identify the schema.</param>
        public ChameleonKey(string ns, XmlSchema originalSchema)
        {
            targetNS = ns;
            chameleonLocation = originalSchema.BaseUri;
            if (chameleonLocation.OriginalString.Length == 0)
            {
                // Only store the original schema when the location is empty URI
                //   by doing this we effectively allow multiple chameleon schemas for the same target namespace
                //   and URI, but that only makes sense for empty URI (not specified)
                this.originalSchema = originalSchema;
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                _hashCode = unchecked(targetNS.GetHashCode() + chameleonLocation.GetHashCode() +
                    (originalSchema == null ? 0 : originalSchema.GetHashCode()));
            }
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            if (Ref.ReferenceEquals(this, obj))
            {
                return true;
            }
            ChameleonKey cKey = obj as ChameleonKey;
            if (cKey != null)
            {
                // We want to compare the target NS and the schema location. 
                // If the location is empty (but only then) we also want to compare the original schema instance.
                // As noted above the originalSchema is null if the chameleonLocation is non-empty. As a result we
                // can simply compare the reference to the original schema always (regardless of the schemalocation).
                Debug.Assert((chameleonLocation.OriginalString.Length == 0 && originalSchema != null)
                    || (chameleonLocation.OriginalString.Length != 0 && originalSchema == null));
                return this.targetNS.Equals(cKey.targetNS) && this.chameleonLocation.Equals(cKey.chameleonLocation) &&
                    Ref.ReferenceEquals(originalSchema, cKey.originalSchema);
            }
            return false;
        }
    }
}
