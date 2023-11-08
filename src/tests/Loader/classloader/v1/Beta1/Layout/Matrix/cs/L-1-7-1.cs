// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////
// L-1-7-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes using 1-deep nesting in
// the same assembly and module (checking access from a
// class in the same family).
//
// See ReadMe.txt in the same project as this source for
// further details about these tests.
//

#pragma warning disable 414
using System;
using Xunit;

public class L171
{
    [Fact]
    public static int TestEntryPoint()
    {
        int mi_RetCode;
        mi_RetCode = Test();
        
        if(mi_RetCode == 100)
            Console.WriteLine("Pass");
        else
            Console.WriteLine("FAIL");
        
        return mi_RetCode;
    }

    public static int Test()
    {
        int mi_RetCode = 100;
        
        B.ClsB bc = new B.ClsB();
        A.ClsA ac = new A.ClsA();
        B b = new B();
        
        if(Test_Nested(bc) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ac) != 100)
            mi_RetCode = 0;

        //@csharp - C# simply won't compile non-related private/family/protected access
        
        if(Test_Nested(b.ClsAPubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAAsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAFoaInst) != 100)
            mi_RetCode = 0;

        if(Test_Nested(B.ClsAPubStat) != 100)
            mi_RetCode = 0;
                
        if(Test_Nested(b.ClsBPubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBAsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBFoaInst) != 100)
            mi_RetCode = 0;

        if(Test_Nested(B.ClsBPubStat) != 100)
            mi_RetCode = 0;
                
        return mi_RetCode;
    }

    public static int Test_Nested(A.ClsA ac)
    {
        int mi_RetCode = 100;
        
        /////////////////////////////////
        // Test instance field access
        ac.NestFldAPubInst = 100;
        if(ac.NestFldAPubInst != 100)
            mi_RetCode = 0;
        
        ac.NestFldAAsmInst = 100;
        if(ac.NestFldAAsmInst != 100)
            mi_RetCode = 0;
        
        ac.NestFldAFoaInst = 100;
        if(ac.NestFldAFoaInst != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static field access
        A.ClsA.NestFldAPubStat = 100;
        if(A.ClsA.NestFldAPubStat != 100)
            mi_RetCode = 0;
        
        A.ClsA.NestFldAAsmStat = 100;
        if(A.ClsA.NestFldAAsmStat != 100)
            mi_RetCode = 0;
        
        A.ClsA.NestFldAFoaStat = 100;
        if(A.ClsA.NestFldAFoaStat != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test instance MethAod access  
        if(ac.NestMethAPubInst() != 100)
            mi_RetCode = 0;
        
        if(ac.NestMethAAsmInst() != 100)
            mi_RetCode = 0;
        
        if(ac.NestMethAFoaInst() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static MethAod access
        if(A.ClsA.NestMethAPubStat() != 100)
            mi_RetCode = 0;
        
        if(A.ClsA.NestMethAAsmStat() != 100)
            mi_RetCode = 0;
        
        if(A.ClsA.NestMethAFoaStat() != 100)
            mi_RetCode = 0;  
        
        /////////////////////////////////
        // Test virtual MethAod access
        if(ac.NestMethAPubVirt() != 100)
            mi_RetCode = 0;
        
        if(ac.NestMethAAsmVirt() != 100)
            mi_RetCode = 0;
        
        if(ac.NestMethAFoaVirt() != 100)
            mi_RetCode = 0;  
        
        ////////////////////////////////////////////
        // Test access from within the nested class
        if(ac.Test() != 100)
            mi_RetCode = 0;
        
        return mi_RetCode;
    }

    public static int Test_Nested(B.ClsB bc)
    {
        int mi_RetCode = 100;
        
        /////////////////////////////////
        // Test instance field access
        bc.NestFldBPubInst = 100;
        if(bc.NestFldBPubInst != 100)
            mi_RetCode = 0;
        
        bc.NestFldBAsmInst = 100;
        if(bc.NestFldBAsmInst != 100)
            mi_RetCode = 0;
        
        bc.NestFldBFoaInst = 100;
        if(bc.NestFldBFoaInst != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static field access
        B.ClsB.NestFldBPubStat = 100;
        if(B.ClsB.NestFldBPubStat != 100)
            mi_RetCode = 0;
        
        B.ClsB.NestFldBAsmStat = 100;
        if(B.ClsB.NestFldBAsmStat != 100)
            mi_RetCode = 0;
        
        B.ClsB.NestFldBFoaStat = 100;
        if(B.ClsB.NestFldBFoaStat != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test instance method access  
        if(bc.NestMethBPubInst() != 100)
            mi_RetCode = 0;
        
        if(bc.NestMethBAsmInst() != 100)
            mi_RetCode = 0;
        
        if(bc.NestMethBFoaInst() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static method access
        if(B.ClsB.NestMethBPubStat() != 100)
            mi_RetCode = 0;
        
        if(B.ClsB.NestMethBAsmStat() != 100)
            mi_RetCode = 0;
        
        if(B.ClsB.NestMethBFoaStat() != 100)
            mi_RetCode = 0;  
        
        /////////////////////////////////
        // Test virtual method access
        if(bc.NestMethBPubVirt() != 100)
            mi_RetCode = 0;
        
        if(bc.NestMethBAsmVirt() != 100)
            mi_RetCode = 0;
        
        if(bc.NestMethBFoaVirt() != 100)
            mi_RetCode = 0;  
        
        ////////////////////////////////////////////
        // Test access from within the nested class
        if(bc.Test() != 100)
            mi_RetCode = 0;
        
        return mi_RetCode;
    }

}

public class A
{
    //////////////////////////////
    // Instance Fields
    public int FldAPubInst;
    private int FldAPrivInst;
    protected int FldAFamInst;          //Translates to "family"
    internal int FldAAsmInst;           //Translates to "assembly"
    protected internal int FldAFoaInst; //Translates to "famorassem"
    
    //////////////////////////////
    // Static Fields
    public static int FldAPubStat;
    private static int FldAPrivStat;
    protected static int FldAFamStat;   //family
    internal static int FldAAsmStat;    //assembly
    protected internal static int FldAFoaStat; //famorassem
    
    //////////////////////////////////////
    // Instance fields for nested classes
    public ClsA ClsAPubInst = new ClsA();
    private ClsA ClsAPrivInst = new ClsA();
    protected ClsA ClsAFamInst = new ClsA();
    internal ClsA ClsAAsmInst = new ClsA();
    protected internal ClsA ClsAFoaInst = new ClsA();
    
    /////////////////////////////////////
    // Static fields of nested classes
    public static ClsA ClsAPubStat = new ClsA();
    private static ClsA ClsAPrivStat = new ClsA();
    
    //////////////////////////////
    // Instance Methods
    public int MethAPubInst()
    {
        Console.WriteLine("A::MethAPubInst()");
        return 100;
    }
    
    private int MethAPrivInst()
    {
        Console.WriteLine("A::MethAPrivInst()");
        return 100;
    }
    
    protected int MethAFamInst()
    {
        Console.WriteLine("A::MethAFamInst()");
        return 100;
    }
    
    internal int MethAAsmInst()
    {
        Console.WriteLine("A::MethAAsmInst()");
        return 100;
    }
    
    protected internal int MethAFoaInst()
    {
        Console.WriteLine("A::MethAFoaInst()");
        return 100;
    }
    
    //////////////////////////////
    // Static Methods
    public static int MethAPubStat()
    {
        Console.WriteLine("A::MethAPubStat()");
        return 100;
    }
    
    private static int MethAPrivStat()
    {
        Console.WriteLine("A::MethAPrivStat()");
        return 100;
    }
    
    protected static int MethAFamStat()
    {
        Console.WriteLine("A::MethAFamStat()");
        return 100;
    }
    
    internal static int MethAAsmStat()
    {
        Console.WriteLine("A::MethAAsmStat()");
        return 100;
    }
    
    protected internal static int MethAFoaStat()
    {
        Console.WriteLine("A::MethAFoaStat()");
        return 100;
    }
    
    //////////////////////////////
    // Virtual Instance Methods
    public virtual int MethAPubVirt()
    {
        Console.WriteLine("A::MethAPubVirt()");
        return 100;
    }
    
    //@csharp - Note that C# won't compile an illegal private virtual function
    //So there is no negative testing MethAPrivVirt() here.
    
    protected virtual int MethAFamVirt()
    {
        Console.WriteLine("A::MethAFamVirt()");
        return 100;
    }
    
    internal virtual int MethAAsmVirt()
    {
        Console.WriteLine("A::MethAAsmVirt()");
        return 100;
    }
    
    protected internal virtual int MethAFoaVirt()
    {
        Console.WriteLine("A::MethAFoaVirt()");
        return 100;
    }
    
    public class ClsA
    {
        public int Test()
        {
            int mi_RetCode = 100;
            
            /////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////
            // ACCESS ENCLOSING FIELDS/MEMBERS
            /////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////
            
            //@csharp - C# will not allow nested classes to access non-static members of their enclosing classes
            
            /////////////////////////////////
            // Test static field access
            FldAPubStat = 100;
            if(FldAPubStat != 100)
                mi_RetCode = 0;
            
            FldAFamStat = 100;
            if(FldAFamStat != 100)
                mi_RetCode = 0;
            
            FldAAsmStat = 100;
            if(FldAAsmStat != 100)
                mi_RetCode = 0;
            
            FldAFoaStat = 100;
            if(FldAFoaStat != 100)
                mi_RetCode = 0;
            
            /////////////////////////////////
            // Test static method access
            if(MethAPubStat() != 100)
                mi_RetCode = 0;
            
            if(MethAFamStat() != 100)
                mi_RetCode = 0;
            
            if(MethAAsmStat() != 100)
                mi_RetCode = 0;
            
            if(MethAFoaStat() != 100)
                mi_RetCode = 0;  
            
            return mi_RetCode;
        }


        //////////////////////////////
        // Instance Fields
        public int NestFldAPubInst;
        private int NestFldAPrivInst;
        protected int NestFldAFamInst;          //Translates to "family"
        internal int NestFldAAsmInst;           //Translates to "assembly"
        protected internal int NestFldAFoaInst; //Translates to "famorassem"
        
        //////////////////////////////
        // Static Fields
        public static int NestFldAPubStat;
        private static int NestFldAPrivStat;
        protected static int NestFldAFamStat;   //family
        internal static int NestFldAAsmStat;    //assembly
        protected internal static int NestFldAFoaStat; //famorassem
        
        //////////////////////////////
        // Instance NestMethAods
        public int NestMethAPubInst()
        {
            Console.WriteLine("A::NestMethAPubInst()");
            return 100;
        }
        
        private int NestMethAPrivInst()
        {
            Console.WriteLine("A::NestMethAPrivInst()");
            return 100;
        }
        
        protected int NestMethAFamInst()
        {
            Console.WriteLine("A::NestMethAFamInst()");
            return 100;
        }
        
        internal int NestMethAAsmInst()
        {
            Console.WriteLine("A::NestMethAAsmInst()");
            return 100;
        }
        
        protected internal int NestMethAFoaInst()
        {
            Console.WriteLine("A::NestMethAFoaInst()");
            return 100;
        }
        
        //////////////////////////////
        // Static NestMethods
        public static int NestMethAPubStat()
        {
            Console.WriteLine("A::NestMethAPubStat()");
            return 100;
        }
        
        private static int NestMethAPrivStat()
        {
            Console.WriteLine("A::NestMethAPrivStat()");
            return 100;
        }
        
        protected static int NestMethAFamStat()
        {
            Console.WriteLine("A::NestMethAFamStat()");
            return 100;
        }
        
        internal static int NestMethAAsmStat()
        {
            Console.WriteLine("A::NestMethAAsmStat()");
            return 100;
        }
        
        protected internal static int NestMethAFoaStat()
        {
            Console.WriteLine("A::NestMethAFoaStat()");
            return 100;
        }
        
        //////////////////////////////
        // Virtual Instance NestMethods
        public virtual int NestMethAPubVirt()
        {
            Console.WriteLine("A::NestMethAPubVirt()");
            return 100;
        }
        
        //@csharp - Note that C# won't compile an illegal private virtual function
        //So there is no negative testing NestMethAPrivVirt() here.
        
        protected virtual int NestMethAFamVirt()
        {
            Console.WriteLine("A::NestMethAFamVirt()");
            return 100;
        }
        
        internal virtual int NestMethAAsmVirt()
        {
            Console.WriteLine("A::NestMethAAsmVirt()");
            return 100;
        }
        
        protected internal virtual int NestMethAFoaVirt()
        {
            Console.WriteLine("A::NestMethAFoaVirt()");
            return 100;
        }
        
    }
}

public class B : A
{
    public int Test()
    {
        A a = new A();

        int mi_RetCode = 100;
        
        /////////////////////////////////
        // Test nested class access
        if(Test_Nested(ClsAPubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsAFamInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsAAsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsAFoaInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsAPubStat) != 100)
            mi_RetCode = 0;

        if(Test_Nested(ClsBPubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsBPrivInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsBFamInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsBAsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsBFoaInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ClsBPubStat) != 100)
            mi_RetCode = 0;

        if(Test_Nested(ClsBPrivStat) != 100)
            mi_RetCode = 0;

        return mi_RetCode;
    }
    
    public int Test_Nested(ClsA Nested_Cls)
    {
        int mi_RetCode = 100;
        
        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////
        // ACCESS NESTED FIELDS/MEMBERS
        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////
        
        /////////////////////////////////
        // Test instance field access
        Nested_Cls.NestFldAPubInst = 100;
        if(Nested_Cls.NestFldAPubInst != 100)
            mi_RetCode = 0;
        
        Nested_Cls.NestFldAAsmInst = 100;
        if(Nested_Cls.NestFldAAsmInst != 100)
            mi_RetCode = 0;
        
        Nested_Cls.NestFldAFoaInst = 100;
        if(Nested_Cls.NestFldAFoaInst != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static field access
        A.ClsA.NestFldAPubStat = 100;
        if(A.ClsA.NestFldAPubStat != 100)
            mi_RetCode = 0;
        
        A.ClsA.NestFldAAsmStat = 100;
        if(A.ClsA.NestFldAAsmStat != 100)
            mi_RetCode = 0;
        
        A.ClsA.NestFldAFoaStat = 100;
        if(A.ClsA.NestFldAFoaStat != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test instance MethAod access  
        if(Nested_Cls.NestMethAPubInst() != 100)
            mi_RetCode = 0;
        
        if(Nested_Cls.NestMethAAsmInst() != 100)
            mi_RetCode = 0;
        
        if(Nested_Cls.NestMethAFoaInst() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static MethAod access
        if(A.ClsA.NestMethAPubStat() != 100)
            mi_RetCode = 0;
        
        if(A.ClsA.NestMethAAsmStat() != 100)
            mi_RetCode = 0;
        
        if(A.ClsA.NestMethAFoaStat() != 100)
            mi_RetCode = 0;  
        
        /////////////////////////////////
        // Test virtual MethAod access
        if(Nested_Cls.NestMethAPubVirt() != 100)
            mi_RetCode = 0;
        
        if(Nested_Cls.NestMethAAsmVirt() != 100)
            mi_RetCode = 0;
        
        if(Nested_Cls.NestMethAFoaVirt() != 100)
            mi_RetCode = 0;  
        
        ////////////////////////////////////////////
        // Test access from within the nested class
        if(Nested_Cls.Test() != 100)
            mi_RetCode = 0;

        return mi_RetCode;
    }

    public static int Test_Nested(ClsB Nested_Cls)
    {
        int mi_RetCode = 100;
        
        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////
        // ACCESS NESTED FIELDS/MEMBERS
        /////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////
        
        /////////////////////////////////
        // Test instance field access
        Nested_Cls.NestFldBPubInst = 100;
        if(Nested_Cls.NestFldBPubInst != 100)
            mi_RetCode = 0;
        
        Nested_Cls.NestFldBAsmInst = 100;
        if(Nested_Cls.NestFldBAsmInst != 100)
            mi_RetCode = 0;
        
        Nested_Cls.NestFldBFoaInst = 100;
        if(Nested_Cls.NestFldBFoaInst != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static field access
        B.ClsB.NestFldBPubStat = 100;
        if(B.ClsB.NestFldBPubStat != 100)
            mi_RetCode = 0;
        
        B.ClsB.NestFldBAsmStat = 100;
        if(B.ClsB.NestFldBAsmStat != 100)
            mi_RetCode = 0;
        
        B.ClsB.NestFldBFoaStat = 100;
        if(B.ClsB.NestFldBFoaStat != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test instance MethBod access  
        if(Nested_Cls.NestMethBPubInst() != 100)
            mi_RetCode = 0;
        
        if(Nested_Cls.NestMethBAsmInst() != 100)
            mi_RetCode = 0;
        
        if(Nested_Cls.NestMethBFoaInst() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static MethBod access
        if(B.ClsB.NestMethBPubStat() != 100)
            mi_RetCode = 0;
        
        if(B.ClsB.NestMethBAsmStat() != 100)
            mi_RetCode = 0;
        
        if(B.ClsB.NestMethBFoaStat() != 100)
            mi_RetCode = 0;  
        
        /////////////////////////////////
        // Test virtual MethBod access
        if(Nested_Cls.NestMethBPubVirt() != 100)
            mi_RetCode = 0;
        
        if(Nested_Cls.NestMethBAsmVirt() != 100)
            mi_RetCode = 0;
        
        if(Nested_Cls.NestMethBFoaVirt() != 100)
            mi_RetCode = 0;  
        
        ////////////////////////////////////////////
        // Test access from within the nested class
        if(Nested_Cls.Test() != 100)
            mi_RetCode = 0;

        return mi_RetCode;
    }

    //////////////////////////////
    // Instance Fields
    public int FldBPubInst;
    private int FldBPrivInst;
    protected int FldBFamInst;          //Translates to "family"
    internal int FldBAsmInst;           //Translates to "assembly"
    protected internal int FldBFoaInst; //Translates to "famorassem"
    
    //////////////////////////////
    // Static Fields
    public static int FldBPubStat;
    private static int FldBPrivStat;
    protected static int FldBFamStat;   //family
    internal static int FldBAsmStat;    //assembly
    protected internal static int FldBFoaStat; //famorassem
    
    //////////////////////////////////////
    // Instance fields for nested classes
    public ClsB ClsBPubInst = new ClsB();
    private ClsB ClsBPrivInst = new ClsB();
    protected ClsB ClsBFamInst = new ClsB();
    internal ClsB ClsBAsmInst = new ClsB();
    protected internal ClsB ClsBFoaInst = new ClsB();
    
    /////////////////////////////////////
    // Static fields of nested classes
    public static ClsB ClsBPubStat = new ClsB();
    private static ClsB ClsBPrivStat = new ClsB();
    
    //////////////////////////////
    // Instance MethBods
    public int MethBPubInst()
    {
        Console.WriteLine("B::MethBPubInst()");
        return 100;
    }
    
    private int MethBPrivInst()
    {
        Console.WriteLine("B::MethBPrivInst()");
        return 100;
    }
    
    protected int MethBFamInst()
    {
        Console.WriteLine("B::MethBFamInst()");
        return 100;
    }
    
    internal int MethBAsmInst()
    {
        Console.WriteLine("B::MethBAsmInst()");
        return 100;
    }
    
    protected internal int MethBFoaInst()
    {
        Console.WriteLine("B::MethBFoaInst()");
        return 100;
    }
    
    //////////////////////////////
    // Static MethBods
    public static int MethBPubStat()
    {
        Console.WriteLine("B::MethBPubStat()");
        return 100;
    }
    
    private static int MethBPrivStat()
    {
        Console.WriteLine("B::MethBPrivStat()");
        return 100;
    }
    
    protected static int MethBFamStat()
    {
        Console.WriteLine("B::MethBFamStat()");
        return 100;
    }
    
    internal static int MethBAsmStat()
    {
        Console.WriteLine("B::MethBAsmStat()");
        return 100;
    }
    
    protected internal static int MethBFoaStat()
    {
        Console.WriteLine("B::MethBFoaStat()");
        return 100;
    }
    
    //////////////////////////////
    // Virtual Instance MethBods
    public virtual int MethBPubVirt()
    {
        Console.WriteLine("B::MethBPubVirt()");
        return 100;
    }
    
    //@csharp - Note that C# won't compile an illegal private virtual function
    //So there is no negative testing MethBPrivVirt() here.
    
    protected virtual int MethBFamVirt()
    {
        Console.WriteLine("B::MethBFamVirt()");
        return 100;
    }
    
    internal virtual int MethBAsmVirt()
    {
        Console.WriteLine("B::MethBAsmVirt()");
        return 100;
    }
    
    protected internal virtual int MethBFoaVirt()
    {
        Console.WriteLine("B::MethBFoaVirt()");
        return 100;
    }
    
    public class ClsB
    {
        public int Test()
        {
            int mi_RetCode = 100;
            
            /////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////
            // ACCESS ENCLOSING FIELDS/MEMBERS
            /////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////
            
            //@csharp - C# will not allow nested classes to access non-static members of their enclosing classes
            
            /////////////////////////////////
            // Test static field access
            FldBPubStat = 100;
            if(FldBPubStat != 100)
                mi_RetCode = 0;
            
            FldBFamStat = 100;
            if(FldBFamStat != 100)
                mi_RetCode = 0;
            
            FldBAsmStat = 100;
            if(FldBAsmStat != 100)
                mi_RetCode = 0;
            
            FldBFoaStat = 100;
            if(FldBFoaStat != 100)
                mi_RetCode = 0;
            
            /////////////////////////////////
            // Test static method access
            if(MethBPubStat() != 100)
                mi_RetCode = 0;
            
            if(MethBFamStat() != 100)
                mi_RetCode = 0;
            
            if(MethBAsmStat() != 100)
                mi_RetCode = 0;
            
            if(MethBFoaStat() != 100)
                mi_RetCode = 0;  
            
            /////////////////////////////////
            // Test static field access
            FldAPubStat = 100;
            if(FldAPubStat != 100)
                mi_RetCode = 0;
            
            FldAFamStat = 100;
            if(FldAFamStat != 100)
                mi_RetCode = 0;
            
            FldAAsmStat = 100;
            if(FldAAsmStat != 100)
                mi_RetCode = 0;
            
            FldAFoaStat = 100;
            if(FldAFoaStat != 100)
                mi_RetCode = 0;
            
            /////////////////////////////////
            // Test static method access
            if(MethAPubStat() != 100)
                mi_RetCode = 0;
            
            if(MethAFamStat() != 100)
                mi_RetCode = 0;
            
            if(MethAAsmStat() != 100)
                mi_RetCode = 0;
            
            if(MethAFoaStat() != 100)
                mi_RetCode = 0;  
            
            ////////////////////////////////////////////
            // Test access from within the nested class
            if(ClsAPubStat.Test() != 100)
                mi_RetCode = 0;
            
            return mi_RetCode;
        }
        
        //////////////////////////////
        // Instance Fields
        public int NestFldBPubInst;
        private int NestFldBPrivInst;
        protected int NestFldBFamInst;          //Translates to "family"
        internal int NestFldBAsmInst;           //Translates to "assembly"
        protected internal int NestFldBFoaInst; //Translates to "famorassem"
        
        //////////////////////////////
        // Static Fields
        public static int NestFldBPubStat;
        private static int NestFldBPrivStat;
        protected static int NestFldBFamStat;   //family
        internal static int NestFldBAsmStat;    //assembly
        protected internal static int NestFldBFoaStat; //famorassem
        
        //////////////////////////////
        // Instance NestMethods
        public int NestMethBPubInst()
        {
            Console.WriteLine("B::NestMethBPubInst()");
            return 100;
        }
        
        private int NestMethBPrivInst()
        {
            Console.WriteLine("B::NestMethBPrivInst()");
            return 100;
        }
        
        protected int NestMethBFamInst()
        {
            Console.WriteLine("B::NestMethBFamInst()");
            return 100;
        }
        
        internal int NestMethBAsmInst()
        {
            Console.WriteLine("B::NestMethBAsmInst()");
            return 100;
        }
        
        protected internal int NestMethBFoaInst()
        {
            Console.WriteLine("B::NestMethBFoaInst()");
            return 100;
        }
        
        //////////////////////////////
        // Static NestMethBods
        public static int NestMethBPubStat()
        {
            Console.WriteLine("B::NestMethBPubStat()");
            return 100;
        }
        
        private static int NestMethBPrivStat()
        {
            Console.WriteLine("B::NestMethBPrivStat()");
            return 100;
        }
        
        protected static int NestMethBFamStat()
        {
            Console.WriteLine("B::NestMethBFamStat()");
            return 100;
        }
        
        internal static int NestMethBAsmStat()
        {
            Console.WriteLine("B::NestMethBAsmStat()");
            return 100;
        }
        
        protected internal static int NestMethBFoaStat()
        {
            Console.WriteLine("B::NestMethBFoaStat()");
            return 100;
        }
        
        //////////////////////////////
        // Virtual Instance NestMethods
        public virtual int NestMethBPubVirt()
        {
            Console.WriteLine("B::NestMethBPubVirt()");
            return 100;
        }
        
        //@csharp - Note that C# won't compile an illegal private virtual function
        //So there is no negative testing NestMethBPrivVirt() here.
        
        protected virtual int NestMethBFamVirt()
        {
            Console.WriteLine("B::NestMethBFamVirt()");
            return 100;
        }
        
        internal virtual int NestMethBAsmVirt()
        {
            Console.WriteLine("B::NestMethBAsmVirt()");
            return 100;
        }
        
        protected internal virtual int NestMethBFoaVirt()
        {
            Console.WriteLine("B::NestMethBFoaVirt()");
            return 100;
        }
    }
}
