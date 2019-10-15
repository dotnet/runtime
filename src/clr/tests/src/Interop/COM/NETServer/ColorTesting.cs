// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Runtime.InteropServices;

[ComVisible(true)]
[Guid(Server.Contract.Guids.ColorTesting)]
public class ColorTesting : Server.Contract.IColorTesting
{
    public bool AreColorsEqual(Color managed, int native) => ColorTranslator.ToOle(managed) == native;

    public Color GetRed() => Color.Red;
}
