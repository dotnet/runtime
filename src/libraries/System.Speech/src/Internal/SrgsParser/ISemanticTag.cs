// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Internal.SrgsParser
{
    /// <summary>
    /// Interface definition for the IElementTag
    /// </summary>
    internal interface ISemanticTag : IElement
    {
        void Content(IElement parent, string value, int line);
    }
}
