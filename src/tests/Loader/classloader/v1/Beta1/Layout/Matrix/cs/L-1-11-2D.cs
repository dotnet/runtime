// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class A{
/*public int Test(B b){
  int mi_RetCode = 100;

  /////////////////////////////////
  // Test instance field access
  b.FldPubInst = 100;
  if(b.FldPubInst != 100)
    mi_RetCode = 0;

  //@csharp - Note that C# will not compile an illegal access of b.FldPrivInst
  //So there is no negative test here, it should be covered elsewhere and
  //should throw a FielAccessException within the runtime.  (IL sources is
  //the most logical, only?, choice)

  //@csharp - C# Won't compile illegial family access from non-family members

  b.FldAsmInst = 100;
  if(b.FldAsmInst != 100)
    mi_RetCode = 0;

  b.FldFoaInst = 100;
  if(b.FldFoaInst != 100)
    mi_RetCode = 0;

  /////////////////////////////////
  // Test static field access
  B.FldPubStat = 100;
  if(B.FldPubStat != 100)
    mi_RetCode = 0;

  //@csharp - Again, note C# won't do private field access

  //@csharp - C# Won't compile illegial family access from non-family members

  B.FldAsmStat = 100;
  if(B.FldAsmStat != 100)
    mi_RetCode = 0;

  B.FldFoaStat = 100;
  if(B.FldFoaStat != 100)
    mi_RetCode = 0;

  /////////////////////////////////
  // Test instance b.Method access  
  if(b.MethPubInst() != 100)
    mi_RetCode = 0;

  //@csharp - C# won't do private b.Method access

  //@csharp - C# Won't compile illegial family access from non-family members

  if(b.MethAsmInst() != 100)
    mi_RetCode = 0;

  if(b.MethFoaInst() != 100)
    mi_RetCode = 0;

  /////////////////////////////////
  // Test static b.Method access
  if(B.MethPubStat() != 100)
    mi_RetCode = 0;

  //@csharp - C# won't do private b.Method access

  //@csharp - C# Won't compile illegial family access from non-family members

  if(B.MethAsmStat() != 100)
    mi_RetCode = 0;

  if(B.MethFoaStat() != 100)
    mi_RetCode = 0;  

  /////////////////////////////////
  // Test virtual b.Method access
  if(b.MethPubVirt() != 100)
    mi_RetCode = 0;

  //@csharp - C# won't do private b.Method access

  //@csharp - C# Won't compile illegial family access from non-family members

  if(b.MethAsmVirt() != 100)
    mi_RetCode = 0;

  if(b.MethFoaVirt() != 100)
    mi_RetCode = 0;  

  return mi_RetCode;
}
*/
  //////////////////////////////
  // Instance Fields
public int FldPubInst;
private int FldPrivInst;
protected int FldFamInst;          //Translates to "family"
internal int FldAsmInst;           //Translates to "assembly"
protected internal int FldFoaInst; //Translates to "famorassem"
  
  //////////////////////////////
  // Static Fields
public static int FldPubStat;
private static int FldPrivStat;
protected static int FldFamStat;   //family
internal static int FldAsmStat;    //assembly
protected internal static int FldFoaStat; //famorassem

  //////////////////////////////
  // Instance Methods
public int MethPubInst(){
  Console.WriteLine("A::MethPubInst()");
  return 100;
}

private int MethPrivInst(){
  Console.WriteLine("A::MethPrivInst()");
  return 100;
}

protected int MethFamInst(){
  Console.WriteLine("A::MethFamInst()");
  return 100;
}

internal int MethAsmInst(){
  Console.WriteLine("A::MethAsmInst()");
  return 100;
}

protected internal int MethFoaInst(){
  Console.WriteLine("A::MethFoaInst()");
  return 100;
}

  //////////////////////////////
  // Static Methods
public static int MethPubStat(){
  Console.WriteLine("A::MethPubStat()");
  return 100;
}

private static int MethPrivStat(){
  Console.WriteLine("A::MethPrivStat()");
  return 100;
}

protected static int MethFamStat(){
  Console.WriteLine("A::MethFamStat()");
  return 100;
}

internal static int MethAsmStat(){
  Console.WriteLine("A::MethAsmStat()");
  return 100;
}

protected internal static int MethFoaStat(){
  Console.WriteLine("A::MethFoaStat()");
  return 100;
}

  //////////////////////////////
  // Virtual Instance Methods
public virtual int MethPubVirt(){
  Console.WriteLine("A::MethPubVirt()");
  return 100;
}

  //@csharp - Note that C# won't compile an illegal private virtual function
  //So there is no negative testing MethPrivVirt() here.

protected virtual int MethFamVirt(){
  Console.WriteLine("A::MethFamVirt()");
  return 100;
}

internal virtual int MethAsmVirt(){
  Console.WriteLine("A::MethAsmVirt()");
  return 100;
}

protected internal virtual int MethFoaVirt(){
  Console.WriteLine("A::MethFoaVirt()");
  return 100;
}
}
