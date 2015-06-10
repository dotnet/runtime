// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
