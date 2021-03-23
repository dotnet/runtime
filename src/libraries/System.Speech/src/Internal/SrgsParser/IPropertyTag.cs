// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Internal.SrgsParser
{
    /// <summary>
    /// Interface definition for the IElementTag
    /// </summary>
    internal interface IPropertyTag : IElement
    {
        void NameValue(IElement parent, string name, object value);
    }
}
