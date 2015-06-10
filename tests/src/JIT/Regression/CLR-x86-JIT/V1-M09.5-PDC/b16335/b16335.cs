// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    using System;

    class BB
    {
        public static bool[] m_static1 = new bool[7];
        public BB[] Method1()
        {
            return new BB[7];
        }
        public bool[] m_field2;
    }

    class DD
    {
        public static BB m_static2 = new BB();

        public static int Main()
        {
            try
            {
                new BB().Method1()[2].m_field2 = BB.m_static1;		//Normally, must throw NullReferenceException
            }
            catch (NullReferenceException)
            {
                return 100;
            }
            return 1;
        }
    }
}
