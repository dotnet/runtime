// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

class WeakReferenceTest
{
    // test variables
    private Object[] m_objectArray;
    private WeakReference[] m_wrArray;
    private const int m_numWRs = 50;
    private long m_numIters;
    private bool m_alive = true;
    
    public static void Usage()
    {
        Console.WriteLine("USAGE: WeakReference.exe <num iterations> <alive|dead> <gettarget|settarget|isalive|alloc|alloctrack>");
    }

    static void Main(string[] args)
    {
        if (args.Length!=3)
		{
			Usage();
			return;
		}
        
        long iterations = 0;
        if (!long.TryParse(args[0], out iterations))
		{
			Usage();
			return;
		}

        bool alive = true;
        if (args[1].ToLower()=="dead")
        {
            alive = false;
            Console.WriteLine("Using dead targets");
        }

        WeakReferenceTest test = new WeakReferenceTest(iterations, alive);
        test.Init(); 

        switch (args[2].ToLower())
        {
            case "gettarget":
                test.GetTargetTest();
                break;
            case "settarget":
                test.SetTargetTest();
                break;
            case "isalive":
                test.IsAliveTest();
                break;
            case "alloc":
                test.AllocTest(false);
                break;
            case "alloctrack":
                test.AllocTest(true);
                break;
            default:
                Usage();
                break;
        }
         
    }
    
    
    public WeakReferenceTest(long numIters, bool alive)
    {
        m_numIters = numIters;
        m_alive = alive;
        
        m_objectArray = new Object[m_numWRs];
        m_wrArray = new WeakReference[m_numWRs];
        
        for (int i = 0; i < m_numWRs; ++i)
        {
            if (m_alive)
            {
                // create a new string object
                String s = "blah" + i;
                m_objectArray[i] = s;
            }
            else
            {
                // set to null
                m_objectArray[i] = null;
            }
            
        }
      
        //GC now to get that out of the way
        GC.Collect();          
    }
    
    
    public void Init()
    {

        for (int i = 0; i < m_numWRs; ++i)
        {            
            m_wrArray[i] = new WeakReference(m_objectArray[i]);
        }    
        GC.Collect();    

    }

    public void AllocTest(bool trackResurrection)
    {
                    
        for (long i = 0; i < m_numIters; i++)
        {                           
            m_wrArray[0] = new WeakReference(m_objectArray[0], trackResurrection);
            m_wrArray[1] = new WeakReference(m_objectArray[1], trackResurrection);
            m_wrArray[2] = new WeakReference(m_objectArray[2], trackResurrection);
            m_wrArray[3] = new WeakReference(m_objectArray[3], trackResurrection);
            m_wrArray[4] = new WeakReference(m_objectArray[4], trackResurrection);
            m_wrArray[5] = new WeakReference(m_objectArray[5], trackResurrection);
            m_wrArray[6] = new WeakReference(m_objectArray[6], trackResurrection);
            m_wrArray[7] = new WeakReference(m_objectArray[7], trackResurrection);
            m_wrArray[8] = new WeakReference(m_objectArray[8], trackResurrection);
            m_wrArray[9] = new WeakReference(m_objectArray[9], trackResurrection);

            m_wrArray[10] = new WeakReference(m_objectArray[10], trackResurrection);
            m_wrArray[11] = new WeakReference(m_objectArray[11], trackResurrection);
            m_wrArray[12] = new WeakReference(m_objectArray[12], trackResurrection);
            m_wrArray[13] = new WeakReference(m_objectArray[13], trackResurrection);
            m_wrArray[14] = new WeakReference(m_objectArray[14], trackResurrection);
            m_wrArray[15] = new WeakReference(m_objectArray[15], trackResurrection);
            m_wrArray[16] = new WeakReference(m_objectArray[16], trackResurrection);
            m_wrArray[17] = new WeakReference(m_objectArray[17], trackResurrection);
            m_wrArray[18] = new WeakReference(m_objectArray[18], trackResurrection);
            m_wrArray[19] = new WeakReference(m_objectArray[19], trackResurrection);

            m_wrArray[20] = new WeakReference(m_objectArray[20], trackResurrection);
            m_wrArray[21] = new WeakReference(m_objectArray[21], trackResurrection);
            m_wrArray[22] = new WeakReference(m_objectArray[22], trackResurrection);
            m_wrArray[23] = new WeakReference(m_objectArray[23], trackResurrection);
            m_wrArray[24] = new WeakReference(m_objectArray[24], trackResurrection);
            m_wrArray[25] = new WeakReference(m_objectArray[25], trackResurrection);
            m_wrArray[26] = new WeakReference(m_objectArray[26], trackResurrection);
            m_wrArray[27] = new WeakReference(m_objectArray[27], trackResurrection);
            m_wrArray[28] = new WeakReference(m_objectArray[28], trackResurrection);
            m_wrArray[29] = new WeakReference(m_objectArray[29], trackResurrection);

            m_wrArray[30] = new WeakReference(m_objectArray[30], trackResurrection);
            m_wrArray[31] = new WeakReference(m_objectArray[31], trackResurrection);
            m_wrArray[32] = new WeakReference(m_objectArray[32], trackResurrection);
            m_wrArray[33] = new WeakReference(m_objectArray[33], trackResurrection);
            m_wrArray[34] = new WeakReference(m_objectArray[34], trackResurrection);
            m_wrArray[35] = new WeakReference(m_objectArray[35], trackResurrection);
            m_wrArray[36] = new WeakReference(m_objectArray[36], trackResurrection);
            m_wrArray[37] = new WeakReference(m_objectArray[37], trackResurrection);
            m_wrArray[38] = new WeakReference(m_objectArray[38], trackResurrection);
            m_wrArray[39] = new WeakReference(m_objectArray[39], trackResurrection);

            m_wrArray[40] = new WeakReference(m_objectArray[40], trackResurrection);
            m_wrArray[41] = new WeakReference(m_objectArray[41], trackResurrection);
            m_wrArray[42] = new WeakReference(m_objectArray[42], trackResurrection);
            m_wrArray[43] = new WeakReference(m_objectArray[43], trackResurrection);
            m_wrArray[44] = new WeakReference(m_objectArray[44], trackResurrection);
            m_wrArray[45] = new WeakReference(m_objectArray[45], trackResurrection);
            m_wrArray[46] = new WeakReference(m_objectArray[46], trackResurrection);
            m_wrArray[47] = new WeakReference(m_objectArray[47], trackResurrection);
            m_wrArray[48] = new WeakReference(m_objectArray[48], trackResurrection);
            m_wrArray[49] = new WeakReference(m_objectArray[49], trackResurrection);
             
            for (int j=0; j< m_wrArray.Length; j++)
            {
                m_wrArray[j] = null;
            }
            
            GC.Collect();
    
        }
        
    }

       
       
    public void SetTargetTest()
    {
        Init();
        
        for (long i = 0; i < m_numIters; i++)
        {                           
            m_wrArray[0].Target = m_objectArray[0];
            m_wrArray[1].Target = m_objectArray[1];
            m_wrArray[2].Target = m_objectArray[2];
            m_wrArray[3].Target = m_objectArray[3];
            m_wrArray[4].Target = m_objectArray[4];
            m_wrArray[5].Target = m_objectArray[5];
            m_wrArray[6].Target = m_objectArray[6];
            m_wrArray[7].Target = m_objectArray[7];
            m_wrArray[8].Target = m_objectArray[8];
            m_wrArray[9].Target = m_objectArray[9];

            m_wrArray[10].Target = m_objectArray[10];
            m_wrArray[11].Target = m_objectArray[11];
            m_wrArray[12].Target = m_objectArray[12];
            m_wrArray[13].Target = m_objectArray[13];
            m_wrArray[14].Target = m_objectArray[14];
            m_wrArray[15].Target = m_objectArray[15];
            m_wrArray[16].Target = m_objectArray[16];
            m_wrArray[17].Target = m_objectArray[17];
            m_wrArray[18].Target = m_objectArray[18];
            m_wrArray[19].Target = m_objectArray[19];

            m_wrArray[20].Target = m_objectArray[20];
            m_wrArray[21].Target = m_objectArray[21];
            m_wrArray[22].Target = m_objectArray[22];
            m_wrArray[23].Target = m_objectArray[23];
            m_wrArray[24].Target = m_objectArray[24];
            m_wrArray[25].Target = m_objectArray[25];
            m_wrArray[26].Target = m_objectArray[26];
            m_wrArray[27].Target = m_objectArray[27];
            m_wrArray[28].Target = m_objectArray[28];
            m_wrArray[29].Target = m_objectArray[29];

            m_wrArray[30].Target = m_objectArray[30];
            m_wrArray[31].Target = m_objectArray[31];
            m_wrArray[32].Target = m_objectArray[32];
            m_wrArray[33].Target = m_objectArray[33];
            m_wrArray[34].Target = m_objectArray[34];
            m_wrArray[35].Target = m_objectArray[35];
            m_wrArray[36].Target = m_objectArray[36];
            m_wrArray[37].Target = m_objectArray[37];
            m_wrArray[38].Target = m_objectArray[38];
            m_wrArray[39].Target = m_objectArray[39];

            m_wrArray[40].Target = m_objectArray[40];
            m_wrArray[41].Target = m_objectArray[41];
            m_wrArray[42].Target = m_objectArray[42];
            m_wrArray[43].Target = m_objectArray[43];
            m_wrArray[44].Target = m_objectArray[44];
            m_wrArray[45].Target = m_objectArray[45];
            m_wrArray[46].Target = m_objectArray[46];
            m_wrArray[47].Target = m_objectArray[47];
            m_wrArray[48].Target = m_objectArray[48];
            m_wrArray[49].Target = m_objectArray[49];
             
        }

    }
    
    public void GetTargetTest()
    {
    
        Init();
        Object o = null;
                
        for (long i = 0; i < m_numIters; i++)
        {                             
            o = m_wrArray[0].Target;
            o = m_wrArray[1].Target;
            o = m_wrArray[2].Target;
            o = m_wrArray[3].Target;
            o = m_wrArray[4].Target;
            o = m_wrArray[5].Target;
            o = m_wrArray[6].Target;
            o = m_wrArray[7].Target;
            o = m_wrArray[8].Target;
            o = m_wrArray[9].Target;

            o = m_wrArray[10].Target;
            o = m_wrArray[11].Target;
            o = m_wrArray[12].Target;
            o = m_wrArray[13].Target;
            o = m_wrArray[14].Target;
            o = m_wrArray[15].Target;
            o = m_wrArray[16].Target;
            o = m_wrArray[17].Target;
            o = m_wrArray[18].Target;
            o = m_wrArray[19].Target;

            o = m_wrArray[20].Target;
            o = m_wrArray[21].Target;
            o = m_wrArray[22].Target;
            o = m_wrArray[23].Target;
            o = m_wrArray[24].Target;
            o = m_wrArray[25].Target;
            o = m_wrArray[26].Target;
            o = m_wrArray[27].Target;
            o = m_wrArray[28].Target;
            o = m_wrArray[29].Target;

            o = m_wrArray[30].Target;
            o = m_wrArray[31].Target;
            o = m_wrArray[32].Target;
            o = m_wrArray[33].Target;
            o = m_wrArray[34].Target;
            o = m_wrArray[35].Target;
            o = m_wrArray[36].Target;
            o = m_wrArray[37].Target;
            o = m_wrArray[38].Target;
            o = m_wrArray[39].Target;

            o = m_wrArray[40].Target;
            o = m_wrArray[41].Target;
            o = m_wrArray[42].Target;
            o = m_wrArray[43].Target;
            o = m_wrArray[44].Target;
            o = m_wrArray[45].Target;
            o = m_wrArray[46].Target;
            o = m_wrArray[47].Target;
            o = m_wrArray[48].Target;
            o = m_wrArray[49].Target;
              
        }

    }
    
    
    public void IsAliveTest()
    {
        bool b = false;        
        Init();
                
        for (int i = 0; i < m_numIters; i++)
        {              

            b = m_wrArray[0].IsAlive;
            b = m_wrArray[1].IsAlive;
            b = m_wrArray[2].IsAlive;
            b = m_wrArray[3].IsAlive;
            b = m_wrArray[4].IsAlive;
            b = m_wrArray[5].IsAlive;
            b = m_wrArray[6].IsAlive;
            b = m_wrArray[7].IsAlive;
            b = m_wrArray[8].IsAlive;
            b = m_wrArray[9].IsAlive;

            b = m_wrArray[10].IsAlive;
            b = m_wrArray[11].IsAlive;
            b = m_wrArray[12].IsAlive;
            b = m_wrArray[13].IsAlive;
            b = m_wrArray[14].IsAlive;
            b = m_wrArray[15].IsAlive;
            b = m_wrArray[16].IsAlive;
            b = m_wrArray[17].IsAlive;
            b = m_wrArray[18].IsAlive;
            b = m_wrArray[19].IsAlive;

            b = m_wrArray[20].IsAlive;
            b = m_wrArray[21].IsAlive;
            b = m_wrArray[22].IsAlive;
            b = m_wrArray[23].IsAlive;
            b = m_wrArray[24].IsAlive;
            b = m_wrArray[25].IsAlive;
            b = m_wrArray[26].IsAlive;
            b = m_wrArray[27].IsAlive;
            b = m_wrArray[28].IsAlive;
            b = m_wrArray[29].IsAlive;

            b = m_wrArray[30].IsAlive;
            b = m_wrArray[31].IsAlive;
            b = m_wrArray[32].IsAlive;
            b = m_wrArray[33].IsAlive;
            b = m_wrArray[34].IsAlive;
            b = m_wrArray[35].IsAlive;
            b = m_wrArray[36].IsAlive;
            b = m_wrArray[37].IsAlive;
            b = m_wrArray[38].IsAlive;
            b = m_wrArray[39].IsAlive;

            b = m_wrArray[40].IsAlive;
            b = m_wrArray[41].IsAlive;
            b = m_wrArray[42].IsAlive;
            b = m_wrArray[43].IsAlive;
            b = m_wrArray[44].IsAlive;
            b = m_wrArray[45].IsAlive;
            b = m_wrArray[46].IsAlive;
            b = m_wrArray[47].IsAlive;
            b = m_wrArray[48].IsAlive;
            b = m_wrArray[49].IsAlive;
            
        }

    }    

}
