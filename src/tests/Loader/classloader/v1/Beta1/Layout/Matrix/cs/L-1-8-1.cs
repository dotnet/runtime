// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////
// L-1-8-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes using 2-deep nesting in
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
        
        B.ClsB.ClsB2 bc = new B.ClsB.ClsB2();
        A.ClsA.ClsA2 ac = new A.ClsA.ClsA2();
        B b = new B();
        
        if(Test_Nested(bc) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(ac) != 100)
            mi_RetCode = 0;
        
        //@csharp - C# simply won't compile non-related private/family/protected access
        
        if(Test_Nested(b.ClsAPubInst.ClsA2PubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAPubInst.ClsA2AsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAPubInst.ClsA2FoaInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAAsmInst.ClsA2PubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAAsmInst.ClsA2AsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAAsmInst.ClsA2FoaInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAFoaInst.ClsA2PubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAFoaInst.ClsA2AsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsAFoaInst.ClsA2FoaInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(B.ClsAPubStat.ClsA2PubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(B.ClsAPubStat.ClsA2AsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(B.ClsAPubStat.ClsA2FoaInst) != 100)
            mi_RetCode = 0;
        
        //----------------------------------------------------
        
        if(Test_Nested(b.ClsBPubInst.ClsB2PubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBPubInst.ClsB2AsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBPubInst.ClsB2FoaInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBAsmInst.ClsB2PubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBAsmInst.ClsB2AsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBAsmInst.ClsB2FoaInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBFoaInst.ClsB2PubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBFoaInst.ClsB2AsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(b.ClsBFoaInst.ClsB2FoaInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(B.ClsBPubStat.ClsB2PubInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(B.ClsBPubStat.ClsB2AsmInst) != 100)
            mi_RetCode = 0;
        
        if(Test_Nested(B.ClsBPubStat.ClsB2FoaInst) != 100)
            mi_RetCode = 0;
        
        return mi_RetCode;
    }
    
    public static int Test_Nested(A.ClsA.ClsA2 ac){
        int mi_RetCode = 100;
        
        /////////////////////////////////
        // Test instance field access
        ac.NestFldA2PubInst = 100;
        if(ac.NestFldA2PubInst != 100)
            mi_RetCode = 0;
        
        ac.NestFldA2AsmInst = 100;
        if(ac.NestFldA2AsmInst != 100)
            mi_RetCode = 0;
        
        ac.NestFldA2FoaInst = 100;
        if(ac.NestFldA2FoaInst != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static field access
        A.ClsA.ClsA2.NestFldA2PubStat = 100;
        if(A.ClsA.ClsA2.NestFldA2PubStat != 100)
            mi_RetCode = 0;
        
        A.ClsA.ClsA2.NestFldA2AsmStat = 100;
        if(A.ClsA.ClsA2.NestFldA2AsmStat != 100)
            mi_RetCode = 0;
        
        A.ClsA.ClsA2.NestFldA2FoaStat = 100;
        if(A.ClsA.ClsA2.NestFldA2FoaStat != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test instance MethA2od access  
        if(ac.NestMethA2PubInst() != 100)
            mi_RetCode = 0;
        
        if(ac.NestMethA2AsmInst() != 100)
            mi_RetCode = 0;
        
        if(ac.NestMethA2FoaInst() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static MethA2od access
        if(A.ClsA.ClsA2.NestMethA2PubStat() != 100)
            mi_RetCode = 0;
        
        if(A.ClsA.ClsA2.NestMethA2AsmStat() != 100)
            mi_RetCode = 0;
        
        if(A.ClsA.ClsA2.NestMethA2FoaStat() != 100)
            mi_RetCode = 0;  
        
        /////////////////////////////////
        // Test virtual MethA2od access
        if(ac.NestMethA2PubVirt() != 100)
            mi_RetCode = 0;
        
        if(ac.NestMethA2AsmVirt() != 100)
            mi_RetCode = 0;
        
        if(ac.NestMethA2FoaVirt() != 100)
            mi_RetCode = 0;  
        
        ////////////////////////////////////////////
        // Test access from within the nested class
        if(ac.Test() != 100)
            mi_RetCode = 0;
        
        return mi_RetCode;
    }
    
    public static int Test_Nested(B.ClsB.ClsB2 bc){
        int mi_RetCode = 100;
        
        /////////////////////////////////
        // Test instance field access
        bc.NestFldB2PubInst = 100;
        if(bc.NestFldB2PubInst != 100)
            mi_RetCode = 0;
        
        bc.NestFldB2AsmInst = 100;
        if(bc.NestFldB2AsmInst != 100)
            mi_RetCode = 0;
        
        bc.NestFldB2FoaInst = 100;
        if(bc.NestFldB2FoaInst != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static field access
        B.ClsB.ClsB2.NestFldB2PubStat = 100;
        if(B.ClsB.ClsB2.NestFldB2PubStat != 100)
            mi_RetCode = 0;
        
        B.ClsB.ClsB2.NestFldB2AsmStat = 100;
        if(B.ClsB.ClsB2.NestFldB2AsmStat != 100)
            mi_RetCode = 0;
        
        B.ClsB.ClsB2.NestFldB2FoaStat = 100;
        if(B.ClsB.ClsB2.NestFldB2FoaStat != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test instance method access  
        if(bc.NestMethB2PubInst() != 100)
            mi_RetCode = 0;
        
        if(bc.NestMethB2AsmInst() != 100)
            mi_RetCode = 0;
        
        if(bc.NestMethB2FoaInst() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static method access
        if(B.ClsB.ClsB2.NestMethB2PubStat() != 100)
            mi_RetCode = 0;
        
        if(B.ClsB.ClsB2.NestMethB2AsmStat() != 100)
            mi_RetCode = 0;
        
        if(B.ClsB.ClsB2.NestMethB2FoaStat() != 100)
            mi_RetCode = 0;  
        
        /////////////////////////////////
        // Test virtual method access
        if(bc.NestMethB2PubVirt() != 100)
            mi_RetCode = 0;
        
        if(bc.NestMethB2AsmVirt() != 100)
            mi_RetCode = 0;
        
        if(bc.NestMethB2FoaVirt() != 100)
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
    public int MethAPubInst(){
        Console.WriteLine("A::MethAPubInst()");
        return 100;
    }
    
    private int MethAPrivInst(){
        Console.WriteLine("A::MethAPrivInst()");
        return 100;
    }
    
    protected int MethAFamInst(){
        Console.WriteLine("A::MethAFamInst()");
        return 100;
    }
    
    internal int MethAAsmInst(){
        Console.WriteLine("A::MethAAsmInst()");
        return 100;
    }
    
    protected internal int MethAFoaInst(){
        Console.WriteLine("A::MethAFoaInst()");
        return 100;
    }
    
    //////////////////////////////
    // Static Methods
    public static int MethAPubStat(){
        Console.WriteLine("A::MethAPubStat()");
        return 100;
    }
    
    private static int MethAPrivStat(){
        Console.WriteLine("A::MethAPrivStat()");
        return 100;
    }
    
    protected static int MethAFamStat(){
        Console.WriteLine("A::MethAFamStat()");
        return 100;
    }
    
    internal static int MethAAsmStat(){
        Console.WriteLine("A::MethAAsmStat()");
        return 100;
    }
    
    protected internal static int MethAFoaStat(){
        Console.WriteLine("A::MethAFoaStat()");
        return 100;
    }
    
    //////////////////////////////
    // Virtual Instance Methods
    public virtual int MethAPubVirt(){
        Console.WriteLine("A::MethAPubVirt()");
        return 100;
    }
    
    //@csharp - Note that C# won't compile an illegal private virtual function
    //So there is no negative testing MethAPrivVirt() here.
    
    protected virtual int MethAFamVirt(){
        Console.WriteLine("A::MethAFamVirt()");
        return 100;
    }
    
    internal virtual int MethAAsmVirt(){
        Console.WriteLine("A::MethAAsmVirt()");
        return 100;
    }
    
    protected internal virtual int MethAFoaVirt(){
        Console.WriteLine("A::MethAFoaVirt()");
        return 100;
    }
    
    public class ClsA
    {
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
        
        //////////////////////////////////////
        // Instance fields for nested classes
        public ClsA2 ClsA2PubInst = new ClsA2();
        private ClsA2 ClsA2PrivInst = new ClsA2();
        protected ClsA2 ClsA2FamInst = new ClsA2();
        internal ClsA2 ClsA2AsmInst = new ClsA2();
        protected internal ClsA2 ClsA2FoaInst = new ClsA2();
        
        /////////////////////////////////////
        // Static fields of nested classes
        public static ClsA2 ClsA2PubStat = new ClsA2();
        private static ClsA2 ClsA2PrivStat = new ClsA2();
        
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
        
        public class ClsA2
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
                NestFldA2PubStat = 100;
                if(NestFldA2PubStat != 100)
                    mi_RetCode = 0;
                
                NestFldA2FamStat = 100;
                if(NestFldA2FamStat != 100)
                    mi_RetCode = 0;
                
                NestFldA2AsmStat = 100;
                if(NestFldA2AsmStat != 100)
                    mi_RetCode = 0;
                
                NestFldA2FoaStat = 100;
                if(NestFldA2FoaStat != 100)
                    mi_RetCode = 0;
                
                /////////////////////////////////
                // Test static method access
                if(NestMethA2PubStat() != 100)
                    mi_RetCode = 0;
                
                if(NestMethA2FamStat() != 100)
                    mi_RetCode = 0;
                
                if(NestMethA2AsmStat() != 100)
                    mi_RetCode = 0;
                
                if(NestMethA2FoaStat() != 100)
                    mi_RetCode = 0;  
                
                return mi_RetCode;
            }
            
            
            //////////////////////////////
            // Instance Fields
            public int NestFldA2PubInst;
            private int NestFldA2PrivInst;
            protected int NestFldA2FamInst;          //Translates to "family"
            internal int NestFldA2AsmInst;           //Translates to "assembly"
            protected internal int NestFldA2FoaInst; //Translates to "famorassem"
            
            //////////////////////////////
            // Static Fields
            public static int NestFldA2PubStat;
            private static int NestFldA2PrivStat;
            protected static int NestFldA2FamStat;   //family
            internal static int NestFldA2AsmStat;    //assembly
            protected internal static int NestFldA2FoaStat; //famorassem
            
            //////////////////////////////
            // Instance NestMethA2ods
            public int NestMethA2PubInst()
            {
                Console.WriteLine("A::NestMethA2PubInst()");
                return 100;
            }
            
            private int NestMethA2PrivInst()
            {
                Console.WriteLine("A::NestMethA2PrivInst()");
                return 100;
            }
            
            protected int NestMethA2FamInst()
            {
                Console.WriteLine("A::NestMethA2FamInst()");
                return 100;
            }
            
            internal int NestMethA2AsmInst()
            {
                Console.WriteLine("A::NestMethA2AsmInst()");
                return 100;
            }
            
            protected internal int NestMethA2FoaInst()
            {
                Console.WriteLine("A::NestMethA2FoaInst()");
                return 100;
            }
            
            //////////////////////////////
            // Static NestMethods
            public static int NestMethA2PubStat()
            {
                Console.WriteLine("A::NestMethA2PubStat()");
                return 100;
            }
            
            private static int NestMethA2PrivStat()
            {
                Console.WriteLine("A::NestMethA2PrivStat()");
                return 100;
            }
            
            protected static int NestMethA2FamStat()
            {
                Console.WriteLine("A::NestMethA2FamStat()");
                return 100;
            }
            
            internal static int NestMethA2AsmStat()
            {
                Console.WriteLine("A::NestMethA2AsmStat()");
                return 100;
            }
            
            protected internal static int NestMethA2FoaStat()
            {
                Console.WriteLine("A::NestMethA2FoaStat()");
                return 100;
            }
            
            //////////////////////////////
            // Virtual Instance NestMethods
            public virtual int NestMethA2PubVirt()
            {
                Console.WriteLine("A::NestMethA2PubVirt()");
                return 100;
            }
            
            //@csharp - Note that C# won't compile an illegal private virtual function
            //So there is no negative testing NestMethA2PrivVirt() here.
            
            protected virtual int NestMethA2FamVirt()
            {
                Console.WriteLine("A::NestMethA2FamVirt()");
                return 100;
            }
            
            internal virtual int NestMethA2AsmVirt()
            {
                Console.WriteLine("A::NestMethA2AsmVirt()");
                return 100;
            }
            
            protected internal virtual int NestMethA2FoaVirt()
            {
                Console.WriteLine("A::NestMethA2FoaVirt()");
                return 100;
            }
            
        }
    }
}

public class B : A
{
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
        
        //////////////////////////////////////
        // Instance fields for nested classes
        public ClsB2 ClsB2PubInst = new ClsB2();
        private ClsB2 ClsB2PrivInst = new ClsB2();
        protected ClsB2 ClsB2FamInst = new ClsB2();
        internal ClsB2 ClsB2AsmInst = new ClsB2();
        protected internal ClsB2 ClsB2FoaInst = new ClsB2();
        
        /////////////////////////////////////
        // Static fields of nested classes
        public static ClsB2 ClsB2PubStat = new ClsB2();
        private static ClsB2 ClsB2PrivStat = new ClsB2();
        
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
        
        public class ClsB2
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
                
                NestFldBPubStat = 100;
                if(NestFldBPubStat != 100)
                    mi_RetCode = 0;
                
                NestFldBFamStat = 100;
                if(NestFldBFamStat != 100)
                    mi_RetCode = 0;
                
                NestFldBAsmStat = 100;
                if(NestFldBAsmStat != 100)
                    mi_RetCode = 0;
                
                NestFldBFoaStat = 100;
                if(NestFldBFoaStat != 100)
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
                
                if(NestMethBPubStat() != 100)
                    mi_RetCode = 0;
                
                if(NestMethBFamStat() != 100)
                    mi_RetCode = 0;
                
                if(NestMethBAsmStat() != 100)
                    mi_RetCode = 0;
                
                if(NestMethBFoaStat() != 100)
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
                //if(ClsAPubStat.ClsA2PubStat.Test() != 100) - @todo - Why won't this work?
                //  mi_RetCode = 0;
                
                return mi_RetCode;
            }
            
            
            //////////////////////////////
            // Instance Fields
            public int NestFldB2PubInst;
            private int NestFldB2PrivInst;
            protected int NestFldB2FamInst;          //Translates to "family"
            internal int NestFldB2AsmInst;           //Translates to "assembly"
            protected internal int NestFldB2FoaInst; //Translates to "famorassem"
            
            //////////////////////////////
            // Static Fields
            public static int NestFldB2PubStat;
            private static int NestFldB2PrivStat;
            protected static int NestFldB2FamStat;   //family
            internal static int NestFldB2AsmStat;    //assembly
            protected internal static int NestFldB2FoaStat; //famorassem
            
            //////////////////////////////
            // Instance NestMethB2ods
            public int NestMethB2PubInst()
            {
                Console.WriteLine("A::NestMethB2PubInst()");
                return 100;
            }
            
            private int NestMethB2PrivInst()
            {
                Console.WriteLine("A::NestMethB2PrivInst()");
                return 100;
            }
            
            protected int NestMethB2FamInst()
            {
                Console.WriteLine("A::NestMethB2FamInst()");
                return 100;
            }
            
            internal int NestMethB2AsmInst()
            {
                Console.WriteLine("A::NestMethB2AsmInst()");
                return 100;
            }
            
            protected internal int NestMethB2FoaInst()
            {
                Console.WriteLine("A::NestMethB2FoaInst()");
                return 100;
            }
            
            //////////////////////////////
            // Static NestMethods
            public static int NestMethB2PubStat()
            {
                Console.WriteLine("A::NestMethB2PubStat()");
                return 100;
            }
            
            private static int NestMethB2PrivStat()
            {
                Console.WriteLine("A::NestMethB2PrivStat()");
                return 100;
            }
            
            protected static int NestMethB2FamStat()
            {
                Console.WriteLine("A::NestMethB2FamStat()");
                return 100;
            }
            
            internal static int NestMethB2AsmStat()
            {
                Console.WriteLine("A::NestMethB2AsmStat()");
                return 100;
            }
            
            protected internal static int NestMethB2FoaStat()
            {
                Console.WriteLine("A::NestMethB2FoaStat()");
                return 100;
            }
            
            //////////////////////////////
            // Virtual Instance NestMethods
            public virtual int NestMethB2PubVirt()
            {
                Console.WriteLine("A::NestMethB2PubVirt()");
                return 100;
            }
            
            //@csharp - Note that C# won't compile an illegal private virtual function
            //So there is no negative testing NestMethB2PrivVirt() here.
            
            protected virtual int NestMethB2FamVirt()
            {
                Console.WriteLine("A::NestMethB2FamVirt()");
                return 100;
            }
            
            internal virtual int NestMethB2AsmVirt()
            {
                Console.WriteLine("A::NestMethB2AsmVirt()");
                return 100;
            }
            
            protected internal virtual int NestMethB2FoaVirt()
            {
                Console.WriteLine("A::NestMethB2FoaVirt()");
                return 100;
            }
            
        }
        
    }
}
