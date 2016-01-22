// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

internal static class Surfaces
{
    // Only works with X-Z plane.
    public static readonly Surface CheckerBoard =
        new Surface(
            delegate (Vector pos)
            {
                return ((Math.Floor(pos.Z) + Math.Floor(pos.X)) % 2 != 0)
             ? new Color(1, 1, 1)
             : new Color(0.02, 0.0, 0.14);
            },
            delegate (Vector pos) { return new Color(1, 1, 1); },
            delegate (Vector pos)
            {
                return ((Math.Floor(pos.Z) + Math.Floor(pos.X)) % 2 != 0)
             ? .1
             : .5;
            },
            150);



    public static readonly Surface Shiny =
        new Surface(
            delegate (Vector pos) { return new Color(1, 1, 1); },
            delegate (Vector pos) { return new Color(.5, .5, .5); },
            delegate (Vector pos) { return .7; },
            250);

    public static readonly Surface MatteShiny =
        new Surface(
            delegate (Vector pos) { return new Color(1, 1, 1); },
            delegate (Vector pos) { return new Color(.25, .25, .25); },
            delegate (Vector pos) { return .7; },
            250);
}

