// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    public abstract partial class DesignerOptionService
    {
        [System.ComponentModel.TypeConverter(typeof(DesignerOptionConverter))]
        public sealed partial class DesignerOptionCollection { }
        internal class DesignerOptionConverter { }
    }
}
