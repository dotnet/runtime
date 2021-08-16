// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Speech.Internal;

namespace System.Speech.Recognition.SrgsGrammar
{
    [Serializable]
    internal class SrgsElementList : Collection<SrgsElement>
    {
        #region Interfaces Implementations

        protected override void InsertItem(int index, SrgsElement element)
        {
            Helpers.ThrowIfNull(element, nameof(element));

            base.InsertItem(index, element);
        }

        #endregion
    }
}
