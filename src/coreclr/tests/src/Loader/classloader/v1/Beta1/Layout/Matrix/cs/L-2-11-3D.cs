// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public struct A{
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

  //@csharp - C# Won't compile illegal family access from non-family members

  b.FldAsmInst = 100;
  if(b.FldAsmInst != 100)
    mi_RetCode = 0;

  /////////////////////////////////
  // Test static field access
  B.FldPubStat = 100;
  if(B.FldPubStat != 100)
    mi_RetCode = 0;

  //@csharp - Again, note C# won't do private field access

  //@csharp - C# Won't compile illegal family access from non-family members

  B.FldAsmStat = 100;
  if(B.FldAsmStat != 100)
    mi_RetCode = 0;

  /////////////////////////////////
  // Test instance b.Method access  
  if(b.MethPubInst() != 100)
    mi_RetCode = 0;

  //@csharp - C# won't do private b.Method access

  //@csharp - C# Won't compile illegal family access from non-family members

  if(b.MethAsmInst() != 100)
    mi_RetCode = 0;

  /////////////////////////////////
  // Test static b.Method access
  if(B.MethPubStat() != 100)
    mi_RetCode = 0;

  //@csharp - C# won't do private b.Method access

  //@csharp - C# Won't compile illegal family access from non-family members

  if(B.MethAsmStat() != 100)
    mi_RetCode = 0;

  return mi_RetCode;
}
*/
  //////////////////////////////
  // Instance Fields
public int FldPubInst;
private int FldPrivInst;
internal int FldAsmInst;           //Translates to "assembly"
  
  //////////////////////////////
  // Static Fields
public static int FldPubStat;
private static int FldPrivStat;
internal static int FldAsmStat;    //assembly

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

internal int MethAsmInst(){
  Console.WriteLine("A::MethAsmInst()");
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

internal static int MethAsmStat(){
  Console.WriteLine("A::MethAsmStat()");
  return 100;
}
}
