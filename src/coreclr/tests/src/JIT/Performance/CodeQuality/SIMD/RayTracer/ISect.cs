// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

internal class ISect
{
    public SceneObject Thing;
    public Ray Ray;
    public double Dist;

    public ISect(SceneObject thing, Ray ray, double dist) { Thing = thing; Ray = ray; Dist = dist; }

    public static bool IsNull(ISect sect) { return sect == null; }
    public readonly static ISect Null = null;
}
