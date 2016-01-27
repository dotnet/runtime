// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    class DD
    {
        public float[] Method1()
        {
            return new float[7];
        }
        public static int Main()
        {
            new DD().Method1();
            return 100;
        }
    }
}
