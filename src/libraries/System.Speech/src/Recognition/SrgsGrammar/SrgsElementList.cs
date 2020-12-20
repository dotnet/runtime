// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Speech.Internal;

namespace System.Speech.Recognition.SrgsGrammar
{
    /// <summary>
    /// Summary description for SrgsElementList.
    /// </summary>
    [Serializable]
    internal class SrgsElementList : Collection<SrgsElement>
    {
        //*******************************************************************
        //
        // Interfaces Implementations
        //
        //*******************************************************************

        #region Interfaces Implementations

        protected override void InsertItem(int index, SrgsElement element)
        {
            Helpers.ThrowIfNull(element, "element");

            base.InsertItem(index, element);
        }

        #endregion
    }
}
