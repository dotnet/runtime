// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
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

    public class DD
    {
        static BB m_static2 = new BB();

        [Fact]
        public static int TestEntryPoint()
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
