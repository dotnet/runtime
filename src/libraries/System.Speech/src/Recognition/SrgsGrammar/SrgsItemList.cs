// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Speech.Internal;

namespace System.Speech.Recognition.SrgsGrammar
{
    /// <summary>
    /// Summary description for SrgsItemList.
    /// </summary>
    [Serializable]
    internal class SrgsItemList : Collection<SrgsItem>
    {
        //*******************************************************************
        //
        // Interfaces Implementations
        //
        //*******************************************************************

        #region Interfaces Implementations

        protected override void InsertItem(int index, SrgsItem item)
        {
            Helpers.ThrowIfNull(item, nameof(item));

            base.InsertItem(index, item);
        }

        #endregion
    }
}
