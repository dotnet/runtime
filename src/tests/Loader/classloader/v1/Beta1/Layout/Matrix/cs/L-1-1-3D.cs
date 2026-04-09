// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

///////////////////////////////////////
// L-1-1-2D.cs - DLL Component of
// L-1-1-2.cs test.  - RDawson
//

using System;

public class A{

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
protected internal int FldFoaStat; //famorassem

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
