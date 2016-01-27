// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections;
//using System.Windows.Forms;
using System.IO;
using System.Text;

class Test
{
    public int[,] m_nSourceDestMap;
    public static int m_coSourceLength = 100;
    public static int m_coDestLength = 100;
    static public int Main()
    {
        String testenv = Environment.GetEnvironmentVariable("URTBUILDENV");
        if ((testenv == null) || (testenv.ToUpper() != "FRE"))
        {
            Console.WriteLine("Skip the test since %URTBUILDENV% NEQ 'FRE'");
            return 100;
        }

        Test t = new Test();

        t.EstablishIdentityTransform();

        return 100;
    }

    public void EstablishIdentityTransform()
    {
        //MessageBox.Show("EstablishIdentityTransform() enter");
        int nSourceElements = m_coSourceLength;
        int nDestElements = m_coDestLength;
        int nElements = Math.Max(nSourceElements, nDestElements);
        m_nSourceDestMap = new int[nElements, 2];
        for (int nIndex = 0; nIndex < nElements; nIndex++)
        {
            m_nSourceDestMap[nIndex, 0] = (nIndex > nSourceElements) ? -1 : nIndex;
            m_nSourceDestMap[nIndex, 1] = (nIndex > nDestElements) ? -1 : nIndex;
        }
        //MessageBox.Show("EstablishIdentityTransform() leave");
    }

}
