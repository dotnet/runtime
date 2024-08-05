// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Xunit;

public class MightyExceptor
{
	static int Result = 100;

	[Fact]
	public static int TestEntryPoint()
	{
		try
		{
			Console.WriteLine("Throwing ArgumentException..");
			throw new ArgumentException("Invalid Argument", "Paramzi", new Exception());
		}
		catch(ArgumentException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.ParamName);
		}

		try
		{
			Console.WriteLine("Throwing ArgumentOutOfRangeException..");
			throw new ArgumentOutOfRangeException("Arguement Name", 1, "Arguement Shame");
			
		}
		catch(ArgumentOutOfRangeException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.ActualValue);
		}

		try
		{
			Console.WriteLine("Throwing BadImageFormatException..");
			throw new BadImageFormatException("I'm bad, I'm bad..", "YouKnowMe.txt");
			
		}
		catch(BadImageFormatException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing another BadImageFormatException..");
			throw new BadImageFormatException("This is a really bad image..", "BadFile.exe", new Exception());
			
		}
		catch(BadImageFormatException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing more BadImageFormatExceptions..");
			throw new BadImageFormatException("Yup, it's bad alright", new Exception());
			
		}
		catch(BadImageFormatException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing DllNotFoundException..");
			throw new DllNotFoundException("Where is my DLL?");
			
		}
		catch(DllNotFoundException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing another DllNotFoundException..");
			throw new DllNotFoundException("The DLL is unavailable, please try again later.", new Exception());
			
		}
		catch(DllNotFoundException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing DirectoryNotFoundException..");
			throw new DirectoryNotFoundException("You've been had, the folder is gone.", new Exception());
			
		}
		catch(DirectoryNotFoundException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing EndOfStreamException..");
			throw new EndOfStreamException("The Stream is finished.", new Exception());
			
		}
		catch(EndOfStreamException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing FileLoadException..");
			throw new FileLoadException("Zis is a mesage..", "File1.abc", new Exception());
			
		}
		catch(FileLoadException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing another FileLoadException..");
			throw new FileLoadException("Nice try..");
			
		}
		catch(FileLoadException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing yet another FileLoadException..");
			throw new FileLoadException("Keep trying..", new Exception());
			
		}
		catch(FileLoadException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing more FileLoadExceptions..");
			throw new FileLoadException("Zis is a mesage..", "File1.abc");
			
		}
		catch(FileLoadException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing FileNotFoundException..");
			throw new FileNotFoundException("What file are you talking about?", "Windows.exe", new Exception());
			
		}
		catch(FileNotFoundException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing another FileNotFoundException..");
			throw new FileNotFoundException("Raiders of the lost file?", new Exception());
			
		}
		catch(FileNotFoundException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.FileName);
		}

		try
		{
			Console.WriteLine("Throwing PathTooLongException..");
			throw new PathTooLongException("Slow down, boy!", new Exception());
			
		}
		catch(PathTooLongException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing InvalidProgramException..");
			throw new InvalidProgramException("Le Programe est invaleed.", new Exception());
			
		}
		catch(InvalidProgramException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing another InvalidProgramException..");
			throw new InvalidProgramException("This program is invalid, parental guidance is advised.");
			
		}
		catch(InvalidProgramException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing MissingFieldException..");
			throw new MissingFieldException("Where's the field, kid?", new Exception());
			
		}
		catch(MissingFieldException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing MissingMemberException..");
			throw new MissingMemberException("Classy");
			
		}
		catch(MissingMemberException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing NotImplementedException..");
			throw new NotImplementedException("What are you talking about?", new Exception());
			
		}
		catch(NotImplementedException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing ObjectDisposedException..");
			throw new ObjectDisposedException("Bad Object!");
			
		}
		catch(ObjectDisposedException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing another ObjectDisposedException..");
			throw new ObjectDisposedException("");
			
		}
		catch(ObjectDisposedException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing AmbiguousMatchException..");
			throw new AmbiguousMatchException("Humpty Dumpty sat on a wall..", new Exception());
			
		}
		catch(AmbiguousMatchException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing ReflectionTypeLoadException..");
			Type[] Typo = new Type[1];
			Exception[] Excepto = new Exception[1];
			throw new ReflectionTypeLoadException(Typo, Excepto, "Ya Zahrat al-mada'in.");
			
		}
		catch(ReflectionTypeLoadException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.Types);
			Console.WriteLine("Extra Exception Info: {0}", e.LoaderExceptions);
		}

		try
		{
			Console.WriteLine("Throwing TargetParameterCountException..");
			throw new TargetParameterCountException("Then you shall DIE AGAIN!!", new Exception());
			
		}
		catch(TargetParameterCountException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing MissingManifestResourceException..");
			throw new MissingManifestResourceException("No deaders today but walkin' ones, looks like!", new Exception());
			
		}
		catch(MissingManifestResourceException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing COMException..");
			throw new COMException("A Space FOLD??!!", new Exception());
			
		}
		catch(COMException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing InvalidComObjectException..");
			throw new InvalidComObjectException("At this altitude, it's IMPOSSIBLE!!", new Exception());
			
		}
		catch(InvalidComObjectException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing InvalidOleVariantTypeException..");
			throw new InvalidOleVariantTypeException("It may be impossible but they did it!", new Exception());
			
		}
		catch(InvalidOleVariantTypeException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing MarshalDirectiveException..");
			throw new MarshalDirectiveException("You point, I punch!");
			
		}
		catch(MarshalDirectiveException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing another MarshalDirectiveException..");
			throw new MarshalDirectiveException("Minsc and Boo stand ready!", new Exception());
			
		}
		catch(MarshalDirectiveException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing SEHException..");
			throw new SEHException("Full plate and packing steel!");
			
		}
		catch(SEHException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.CanResume());
		}

		try
		{
			Console.WriteLine("Throwing another SEHException..");
			throw new SEHException("A den of STINKIN' EVIL! Cover your nose Boo, we'll leave no crevice untouched!!", new Exception());
			
		}
		catch(SEHException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.CanResume());
		}

		try
		{
			Console.WriteLine("Throwing SafeArrayRankMismatchException..");
			throw new SafeArrayRankMismatchException("Evil around every corner.. Careful not to step in any!");
			
		}
		catch(SafeArrayRankMismatchException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing another SafeArrayRankMismatchException..");
			throw new SafeArrayRankMismatchException("Cities always teem with evil and decay.. Let's give it a good shake and see what falls out!!", new Exception());
			
		}
		catch(SafeArrayRankMismatchException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing SafeArrayTypeMismatchException..");
			throw new SafeArrayTypeMismatchException("Aww, we are all heroes, you and Boo and I, hamsters and rangers everywhere.. REJOICE!!", new Exception());
			
		}
		catch(SafeArrayTypeMismatchException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing VerificationException..");
			throw new VerificationException("Butts will be liberally kicked when I get out!!", new Exception());
			
		}
		catch(VerificationException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
		}

		try
		{
			Console.WriteLine("Throwing TypeInitializationException..");
			throw new TypeInitializationException("TheUnknownType", new Exception());
			
		}
		catch(TypeInitializationException e)
		{
			Console.WriteLine("Caught the exception: {0}", e.Message);
			Console.WriteLine("Extra Exception Info: {0}", e.TypeName);
		}

		return Result;
	}
}
