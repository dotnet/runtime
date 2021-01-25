// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing
{
    /// <summary>
    /// Abstracts a group of type faces having a similar basic design but having certain variation in styles.
    /// </summary>
    public sealed partial class FontFamily : MarshalByRefObject, IDisposable
    {
        public override bool Equals(object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            // if obj = null then (obj is FontFamily) = false.
            if (!(obj is FontFamily otherFamily))
            {
                return false;
            }

            // In unix FontFamily objects are not singleton so they don't share the same native pointer,
            // the best we have to know if they are the same is FontFamily.Name which gets resolved from the native pointer.
            return Name.Equals(otherFamily.Name, StringComparison.Ordinal);
        }
    }
}
