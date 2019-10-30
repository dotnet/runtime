// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

class GCHandleTest
{         
    // test variables
    private Object[] m_objectArray;
    private GCHandle[] m_gchArray;
    private const int m_numGCHs = 50;
    private long m_numIters;
    
    static void Main(string[] real_args)
    {
        long iterations    = 200;
    
        GCHandleTest test = new GCHandleTest(iterations);
        
        test.GetTargetTest();           
    
        test.SetTargetTest();                
    
        test.AllocTest(GCHandleType.Normal);       
    
        test.AllocTest(GCHandleType.Weak);                
        
        test.AllocTest(GCHandleType.WeakTrackResurrection);                
                
        test.AllocTest(GCHandleType.Pinned);                
                
        test.FreeTest();
    }
    
    
    public GCHandleTest(long numIters)
    {
        m_numIters = numIters;
        m_objectArray = new Object[m_numGCHs];
        m_gchArray = new GCHandle[m_numGCHs];
        
        for (int i = 0; i < m_numGCHs; ++i)
        {
            // create a new string object
            String s = "blah" + i;
            m_objectArray[i] = s;                       
        }        
        GC.Collect();        
    }
    
    
    public void Init()
    {
                           
        for (int i = 0; i < m_numGCHs; ++i)
        {
            m_gchArray[i] = GCHandle.Alloc(m_objectArray[i]);
        }
      
        //GC now to get that out of the way
        GC.Collect();        
                
    }
    
    public void AllocTest(GCHandleType gcht)
    {                 
        for (long i = 0; i < m_numIters; i++)
        {                           
            m_gchArray[0] = GCHandle.Alloc(m_objectArray[0],gcht);
            m_gchArray[1] = GCHandle.Alloc(m_objectArray[1],gcht);
            m_gchArray[2] = GCHandle.Alloc(m_objectArray[2],gcht);
            m_gchArray[3] = GCHandle.Alloc(m_objectArray[3],gcht);
            m_gchArray[4] = GCHandle.Alloc(m_objectArray[4],gcht);
            m_gchArray[5] = GCHandle.Alloc(m_objectArray[5],gcht);
            m_gchArray[6] = GCHandle.Alloc(m_objectArray[6],gcht);
            m_gchArray[7] = GCHandle.Alloc(m_objectArray[7],gcht);
            m_gchArray[8] = GCHandle.Alloc(m_objectArray[8],gcht);
            m_gchArray[9] = GCHandle.Alloc(m_objectArray[9],gcht);

            m_gchArray[10] = GCHandle.Alloc(m_objectArray[10],gcht);
            m_gchArray[11] = GCHandle.Alloc(m_objectArray[11],gcht);
            m_gchArray[12] = GCHandle.Alloc(m_objectArray[12],gcht);
            m_gchArray[13] = GCHandle.Alloc(m_objectArray[13],gcht);
            m_gchArray[14] = GCHandle.Alloc(m_objectArray[14],gcht);
            m_gchArray[15] = GCHandle.Alloc(m_objectArray[15],gcht);
            m_gchArray[16] = GCHandle.Alloc(m_objectArray[16],gcht);
            m_gchArray[17] = GCHandle.Alloc(m_objectArray[17],gcht);
            m_gchArray[18] = GCHandle.Alloc(m_objectArray[18],gcht);
            m_gchArray[19] = GCHandle.Alloc(m_objectArray[19],gcht);

            m_gchArray[20] = GCHandle.Alloc(m_objectArray[20],gcht);
            m_gchArray[21] = GCHandle.Alloc(m_objectArray[21],gcht);
            m_gchArray[22] = GCHandle.Alloc(m_objectArray[22],gcht);
            m_gchArray[23] = GCHandle.Alloc(m_objectArray[23],gcht);
            m_gchArray[24] = GCHandle.Alloc(m_objectArray[24],gcht);
            m_gchArray[25] = GCHandle.Alloc(m_objectArray[25],gcht);
            m_gchArray[26] = GCHandle.Alloc(m_objectArray[26],gcht);
            m_gchArray[27] = GCHandle.Alloc(m_objectArray[27],gcht);
            m_gchArray[28] = GCHandle.Alloc(m_objectArray[28],gcht);
            m_gchArray[29] = GCHandle.Alloc(m_objectArray[29],gcht);

            m_gchArray[30] = GCHandle.Alloc(m_objectArray[30],gcht);
            m_gchArray[31] = GCHandle.Alloc(m_objectArray[31],gcht);
            m_gchArray[32] = GCHandle.Alloc(m_objectArray[32],gcht);
            m_gchArray[33] = GCHandle.Alloc(m_objectArray[33],gcht);
            m_gchArray[34] = GCHandle.Alloc(m_objectArray[34],gcht);
            m_gchArray[35] = GCHandle.Alloc(m_objectArray[35],gcht);
            m_gchArray[36] = GCHandle.Alloc(m_objectArray[36],gcht);
            m_gchArray[37] = GCHandle.Alloc(m_objectArray[37],gcht);
            m_gchArray[38] = GCHandle.Alloc(m_objectArray[38],gcht);
            m_gchArray[39] = GCHandle.Alloc(m_objectArray[39],gcht);

            m_gchArray[40] = GCHandle.Alloc(m_objectArray[40],gcht);
            m_gchArray[41] = GCHandle.Alloc(m_objectArray[41],gcht);
            m_gchArray[42] = GCHandle.Alloc(m_objectArray[42],gcht);
            m_gchArray[43] = GCHandle.Alloc(m_objectArray[43],gcht);
            m_gchArray[44] = GCHandle.Alloc(m_objectArray[44],gcht);
            m_gchArray[45] = GCHandle.Alloc(m_objectArray[45],gcht);
            m_gchArray[46] = GCHandle.Alloc(m_objectArray[46],gcht);
            m_gchArray[47] = GCHandle.Alloc(m_objectArray[47],gcht);
            m_gchArray[48] = GCHandle.Alloc(m_objectArray[48],gcht);
            m_gchArray[49] = GCHandle.Alloc(m_objectArray[49],gcht);
            
            for (int j=0; j< m_gchArray.Length; j++)
            {
                m_gchArray[j].Free();
            }
            
            GC.Collect();
                       
        }
        
    }
       
       
    public void FreeTest()
    {                 
    
        for (long i = 0; i < m_numIters; i++)
        {
            GC.Collect();
            
            for (int j=0; j< m_gchArray.Length; j++)
            {
                m_gchArray[j] = GCHandle.Alloc(m_objectArray[j]);
            }
            
            m_gchArray[0].Free();
            m_gchArray[1].Free();
            m_gchArray[2].Free();
            m_gchArray[3].Free();
            m_gchArray[4].Free();
            m_gchArray[5].Free();
            m_gchArray[6].Free();
            m_gchArray[7].Free();
            m_gchArray[8].Free();
            m_gchArray[9].Free();

            m_gchArray[10].Free();
            m_gchArray[11].Free();
            m_gchArray[12].Free();
            m_gchArray[13].Free();
            m_gchArray[14].Free();
            m_gchArray[15].Free();
            m_gchArray[16].Free();
            m_gchArray[17].Free();
            m_gchArray[18].Free();
            m_gchArray[19].Free();

            m_gchArray[20].Free();
            m_gchArray[21].Free();
            m_gchArray[22].Free();
            m_gchArray[23].Free();
            m_gchArray[24].Free();
            m_gchArray[25].Free();
            m_gchArray[26].Free();
            m_gchArray[27].Free();
            m_gchArray[28].Free();
            m_gchArray[29].Free();

            m_gchArray[30].Free();
            m_gchArray[31].Free();
            m_gchArray[32].Free();
            m_gchArray[33].Free();
            m_gchArray[34].Free();
            m_gchArray[35].Free();
            m_gchArray[36].Free();
            m_gchArray[37].Free();
            m_gchArray[38].Free();
            m_gchArray[39].Free();

            m_gchArray[40].Free();
            m_gchArray[41].Free();
            m_gchArray[42].Free();
            m_gchArray[43].Free();
            m_gchArray[44].Free();
            m_gchArray[45].Free();
            m_gchArray[46].Free();
            m_gchArray[47].Free();
            m_gchArray[48].Free();
            m_gchArray[49].Free();
                                                         
        }

    }       
       
       
    public void SetTargetTest()
    {
        Init();
        
        for (long i = 0; i < m_numIters; i++)
        {                           
            m_gchArray[0].Target = m_objectArray[0];
            m_gchArray[1].Target = m_objectArray[1];
            m_gchArray[2].Target = m_objectArray[2];
            m_gchArray[3].Target = m_objectArray[3];
            m_gchArray[4].Target = m_objectArray[4];
            m_gchArray[5].Target = m_objectArray[5];
            m_gchArray[6].Target = m_objectArray[6];
            m_gchArray[7].Target = m_objectArray[7];
            m_gchArray[8].Target = m_objectArray[8];
            m_gchArray[9].Target = m_objectArray[9];

            m_gchArray[10].Target = m_objectArray[10];
            m_gchArray[11].Target = m_objectArray[11];
            m_gchArray[12].Target = m_objectArray[12];
            m_gchArray[13].Target = m_objectArray[13];
            m_gchArray[14].Target = m_objectArray[14];
            m_gchArray[15].Target = m_objectArray[15];
            m_gchArray[16].Target = m_objectArray[16];
            m_gchArray[17].Target = m_objectArray[17];
            m_gchArray[18].Target = m_objectArray[18];
            m_gchArray[19].Target = m_objectArray[19];

            m_gchArray[20].Target = m_objectArray[20];
            m_gchArray[21].Target = m_objectArray[21];
            m_gchArray[22].Target = m_objectArray[22];
            m_gchArray[23].Target = m_objectArray[23];
            m_gchArray[24].Target = m_objectArray[24];
            m_gchArray[25].Target = m_objectArray[25];
            m_gchArray[26].Target = m_objectArray[26];
            m_gchArray[27].Target = m_objectArray[27];
            m_gchArray[28].Target = m_objectArray[28];
            m_gchArray[29].Target = m_objectArray[29];

            m_gchArray[30].Target = m_objectArray[30];
            m_gchArray[31].Target = m_objectArray[31];
            m_gchArray[32].Target = m_objectArray[32];
            m_gchArray[33].Target = m_objectArray[33];
            m_gchArray[34].Target = m_objectArray[34];
            m_gchArray[35].Target = m_objectArray[35];
            m_gchArray[36].Target = m_objectArray[36];
            m_gchArray[37].Target = m_objectArray[37];
            m_gchArray[38].Target = m_objectArray[38];
            m_gchArray[39].Target = m_objectArray[39];

            m_gchArray[40].Target = m_objectArray[40];
            m_gchArray[41].Target = m_objectArray[41];
            m_gchArray[42].Target = m_objectArray[42];
            m_gchArray[43].Target = m_objectArray[43];
            m_gchArray[44].Target = m_objectArray[44];
            m_gchArray[45].Target = m_objectArray[45];
            m_gchArray[46].Target = m_objectArray[46];
            m_gchArray[47].Target = m_objectArray[47];
            m_gchArray[48].Target = m_objectArray[48];
            m_gchArray[49].Target = m_objectArray[49];
             
        }
        
    }
    
    public void GetTargetTest()
    {
        Init();
        Object o = null;
        
                
        for (long i = 0; i < m_numIters; i++)
        {                             
            o = m_gchArray[0].Target;
            o = m_gchArray[1].Target;
            o = m_gchArray[2].Target;
            o = m_gchArray[3].Target;
            o = m_gchArray[4].Target;
            o = m_gchArray[5].Target;
            o = m_gchArray[6].Target;
            o = m_gchArray[7].Target;
            o = m_gchArray[8].Target;
            o = m_gchArray[9].Target;

            o = m_gchArray[10].Target;
            o = m_gchArray[11].Target;
            o = m_gchArray[12].Target;
            o = m_gchArray[13].Target;
            o = m_gchArray[14].Target;
            o = m_gchArray[15].Target;
            o = m_gchArray[16].Target;
            o = m_gchArray[17].Target;
            o = m_gchArray[18].Target;
            o = m_gchArray[19].Target;

            o = m_gchArray[20].Target;
            o = m_gchArray[21].Target;
            o = m_gchArray[22].Target;
            o = m_gchArray[23].Target;
            o = m_gchArray[24].Target;
            o = m_gchArray[25].Target;
            o = m_gchArray[26].Target;
            o = m_gchArray[27].Target;
            o = m_gchArray[28].Target;
            o = m_gchArray[29].Target;

            o = m_gchArray[30].Target;
            o = m_gchArray[31].Target;
            o = m_gchArray[32].Target;
            o = m_gchArray[33].Target;
            o = m_gchArray[34].Target;
            o = m_gchArray[35].Target;
            o = m_gchArray[36].Target;
            o = m_gchArray[37].Target;
            o = m_gchArray[38].Target;
            o = m_gchArray[39].Target;

            o = m_gchArray[40].Target;
            o = m_gchArray[41].Target;
            o = m_gchArray[42].Target;
            o = m_gchArray[43].Target;
            o = m_gchArray[44].Target;
            o = m_gchArray[45].Target;
            o = m_gchArray[46].Target;
            o = m_gchArray[47].Target;
            o = m_gchArray[48].Target;
            o = m_gchArray[49].Target;
              
        }
        
        GC.KeepAlive(o);
    }
    
}
