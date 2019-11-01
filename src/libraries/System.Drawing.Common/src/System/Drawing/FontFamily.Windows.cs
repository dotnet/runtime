// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Abstracts a group of type faces having a similar basic design but having certain variation in styles.
    /// </summary>
    public sealed partial class FontFamily : MarshalByRefObject, IDisposable
    {
        public override bool Equals(object obj)
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

            // We can safely use the ptr to the native GDI+ FontFamily because in windows it is common to
            // all objects of the same family (singleton RO object).
            return otherFamily.NativeFamily == NativeFamily;
        }
    }
}
