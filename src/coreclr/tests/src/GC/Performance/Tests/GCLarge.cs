// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

internal class List
{
    const int LOOP = 847;
    public SmallGC dat;
    public List next;

    public static void Main(string[] p_args)
    {
        long iterations = 200;

        //Large Object Collection
        CreateLargeObjects();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        for(long i = 0; i < iterations; i++)
        {
            CreateLargeObjects();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        //Large Object Collection (half array)
        CreateLargeObjectsHalf();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        for(long i = 0; i < iterations; i++)
        {
            CreateLargeObjectsHalf();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        //Promote from Gen1 to Gen2
        SmallGC [] sgc;
        sgc = new SmallGC [LOOP];

        for (int j = 0; j < LOOP; j++)
            sgc[j] = new SmallGC(0);

        GC.Collect();

        for (int j = 0; j < LOOP; j++)
            sgc[j] = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
                    
        for(long i = 0; i < iterations; i++)
        {
            // allocate into gen 0
            sgc = new SmallGC [LOOP];
            for (int j = 0; j < LOOP; j++)
                sgc[j] = new SmallGC(0);

            // promote to gen 1
            while (GC.GetGeneration(sgc[LOOP-1])<1)
            {
                GC.Collect();
            }                
            
            while (GC.GetGeneration(sgc[LOOP-1])<2)
            {
                // promote to gen 2
                GC.Collect();
            }

            for (int j = 0; j < LOOP; j++)
                sgc[j] = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        //Promote from Gen1 to Gen2 (Gen1 ptr updates)
        List node = PopulateList(LOOP);
        GC.Collect();
        GC.WaitForPendingFinalizers();

        if(List.ValidateList(node, LOOP) == 0)
            Console.WriteLine("Pointers after promotion are not valid");

        for(long i = 0; i < iterations; i++)
        {
            // allocate into gen 0
            node = PopulateList(LOOP);

            // promote to gen 1
            while (GC.GetGeneration(node)<1)
            {
                GC.Collect();
            }  

            while (GC.GetGeneration(node)<2)
            {
                //promote to gen 2
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if(ValidateList(node, LOOP) == 0)
                    Console.WriteLine("Pointers after promotion are not valid");
                
            }
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static List PopulateList(int len)
    {
        if (len == 0) return null;
        List Node = new List();
        Node.dat = new SmallGC(1);
        Node.dat.AttachSmallObjects();
        Node.next = null;
        for (int i = len -1; i > 0; i--)
        {
            List cur = new List();
            cur.dat = new SmallGC(1);
            cur.dat.AttachSmallObjects();
            cur.next = Node;
            Node = cur;
        }
        return Node;
    }

    public static int ValidateList(List First, int len)
    {
        List tmp1 = First;
        int i = 0;
        LargeGC tmp2;
        while(tmp1 != null)
        {
            //Check the list have correct small object pointers after collection
            if(tmp1.dat == null) break;
            tmp2 = tmp1.dat.m_pLarge;
            //check the large object has non zero small object pointers
            if (tmp2.m_pSmall == null) break;
            //check the large object has correct small object pointers
            if(tmp2.m_pSmall != tmp1.dat) break;
            tmp1 = tmp1.next;
            i++;
        }
        if (i == len)
            return 1;
        else
            return 0;
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateLargeObjects()
    {
        LargeGC [] lgc;
        lgc = new LargeGC[LOOP];
        for (int i=0; i < LOOP ; i++)
            lgc[i] = new LargeGC();
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateLargeObjectsHalf()
    {
        LargeGC [] lgc;
        lgc = new LargeGC[LOOP];

        for(int i = 0; i < LOOP; i++)
            lgc[i] = new LargeGC();

        for(int i = 0; i < LOOP; i+=2)
            lgc[i] = null;
    }
}

internal class LargeGC
{
    public double [] d;
    public SmallGC m_pSmall;

    public LargeGC()
    {
        d = new double [10625]; //85 KB
        m_pSmall = null;
    }

    public virtual void AttachSmallObjects(SmallGC small)
    {
        m_pSmall = small;
    }
}

internal class SmallGC
{
    public LargeGC m_pLarge;
    public SmallGC(int HasLargeObj)
    {
        if (HasLargeObj == 1)
            m_pLarge = new LargeGC();
        else
            m_pLarge = null;
    }
    public virtual void AttachSmallObjects()
    {
        m_pLarge.AttachSmallObjects(this);
    }
}
