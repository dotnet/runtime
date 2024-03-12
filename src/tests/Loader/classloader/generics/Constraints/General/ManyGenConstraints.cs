// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test has a generic type with 100 constraints
// we want to make sure we can load such type.

using System;
using Xunit;

public class Test_ManyGenConstraints {
   [Fact]
   public static int TestEntryPoint() 
   {    
	bool pass = true; 
  
	try
	{
		MyClass<I100> obj = new MyClass<I100>();
	}
	catch (Exception e)
	{
		Console.WriteLine("Caught unexpected exception: " + e);
		pass = false;
	}

	try
	{
		//  warning CS0219: The variable 'obj' is assigned but its value is never used
		#pragma warning disable 219
		MyStruct<I100> obj = new MyStruct<I100>();
		#pragma warning restore 219
	}
	catch (Exception e)
	{
		Console.WriteLine("Caught unexpected exception: " + e);
		pass = false;
	}

	
	if (pass)
	{
		Console.WriteLine("PASS");
		return 100;
	}
	else
	{
		Console.WriteLine("FAIL");
		return 101;
	}
   }
}

public class MyClass<T> where T : I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16, I17, I18, I19, I20, I21, I22, I23, I24, I25, I26, I27, I28, I29, I30, I31, I32, I33, I34, I35, I36, I37, I38, I39, I40, I41, I42, I43, I44, I45, I46, I47, I48, I49, I50, I51, I52, I53, I54, I55, I56, I57, I58, I59, I60, I61, I62, I63, I64, I65, I66, I67, I68, I69, I70, I71, I72, I73, I74, I75, I76, I77, I78, I79, I80, I81, I82, I83, I84, I85, I86, I87, I88, I89, I90, I91, I92, I93, I94, I95, I96, I97, I98, I99, I100{}

public struct MyStruct<T> where T : I1, I2, I3, I4, I5, I6, I7, I8, I9, I10, I11, I12, I13, I14, I15, I16, I17, I18, I19, I20, I21, I22, I23, I24, I25, I26, I27, I28, I29, I30, I31, I32, I33, I34, I35, I36, I37, I38, I39, I40, I41, I42, I43, I44, I45, I46, I47, I48, I49, I50, I51, I52, I53, I54, I55, I56, I57, I58, I59, I60, I61, I62, I63, I64, I65, I66, I67, I68, I69, I70, I71, I72, I73, I74, I75, I76, I77, I78, I79, I80, I81, I82, I83, I84, I85, I86, I87, I88, I89, I90, I91, I92, I93, I94, I95, I96, I97, I98, I99, I100{}

public interface I1 {}
public interface I2 : I1{}
public interface I3 : I2{}
public interface I4 : I3{}
public interface I5 : I4{}
public interface I6 : I5{}
public interface I7 : I6{}
public interface I8 : I7{}
public interface I9 : I8{}
public interface I10 : I9{}
public interface I11 : I10{}
public interface I12 : I11{}
public interface I13 : I12{}
public interface I14 : I13{}
public interface I15 : I14{}
public interface I16 : I15{}
public interface I17 : I16{}
public interface I18 : I17{}
public interface I19 : I18{}
public interface I20 : I19{}
public interface I21 : I20{}
public interface I22 : I21{}
public interface I23 : I22{}
public interface I24 : I23{}
public interface I25 : I24{}
public interface I26 : I25{}
public interface I27 : I26{}
public interface I28 : I27{}
public interface I29 : I28{}
public interface I30 : I29{}
public interface I31 : I30{}
public interface I32 : I31{}
public interface I33 : I32{}
public interface I34 : I33{}
public interface I35 : I34{}
public interface I36 : I35{}
public interface I37 : I36{}
public interface I38 : I37{}
public interface I39 : I38{}
public interface I40 : I39{}
public interface I41 : I40{}
public interface I42 : I41{}
public interface I43 : I42{}
public interface I44 : I43{}
public interface I45 : I44{}
public interface I46 : I45{}
public interface I47 : I46{}
public interface I48 : I47{}
public interface I49 : I48{}
public interface I50 : I49{}
public interface I51 : I50{}
public interface I52 : I51{}
public interface I53 : I52{}
public interface I54 : I53{}
public interface I55 : I54{}
public interface I56 : I55{}
public interface I57 : I56{}
public interface I58 : I57{}
public interface I59 : I58{}
public interface I60 : I59{}
public interface I61 : I60{}
public interface I62 : I61{}
public interface I63 : I62{}
public interface I64 : I63{}
public interface I65 : I64{}
public interface I66 : I65{}
public interface I67 : I66{}
public interface I68 : I67{}
public interface I69 : I68{}
public interface I70 : I69{}
public interface I71 : I70{}
public interface I72 : I71{}
public interface I73 : I72{}
public interface I74 : I73{}
public interface I75 : I74{}
public interface I76 : I75{}
public interface I77 : I76{}
public interface I78 : I77{}
public interface I79 : I78{}
public interface I80 : I79{}
public interface I81 : I80{}
public interface I82 : I81{}
public interface I83 : I82{}
public interface I84 : I83{}
public interface I85 : I84{}
public interface I86 : I85{}
public interface I87 : I86{}
public interface I88 : I87{}
public interface I89 : I88{}
public interface I90 : I89{}
public interface I91 : I90{}
public interface I92 : I91{}
public interface I93 : I92{}
public interface I94 : I93{}
public interface I95 : I94{}
public interface I96 : I95{}
public interface I97 : I96{}
public interface I98 : I97{}
public interface I99 : I98{}
public interface I100 : I99{}
