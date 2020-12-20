// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Speech.Internal.GrammarBuilding
{
    /// <summary>
    /// 
    /// </summary>
    internal class IdentifierCollection
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        internal IdentifierCollection()
        {
            _identifiers = new List<string>();
            CreateNewIdentifier("_");
        }

        #endregion


        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal string CreateNewIdentifier(string id)
        {
            if (!_identifiers.Contains(id))
            {
                _identifiers.Add(id);
                return id;
            }
            else
            {
                string newId;
                int i = 1;
                do
                {
                    newId = id + i;
                    i++;
                } while (_identifiers.Contains(newId));
                _identifiers.Add(newId);
                return newId;
            }
        }

        #endregion


        //*******************************************************************
        //
        // Protected Fields
        //
        //*******************************************************************

        #region Protected Fields

        /// <summary>
        /// 
        /// </summary>
        protected List<string> _identifiers;

        #endregion
    }
}
