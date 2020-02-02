//
// NOTES:
// - If xunit can't laod a trait discoverer assembly, it silently ignores the error.
// - the RemoteTestExecutor code used by corefx only seems to work if
//   the app is executed from the binary dir using dotnet ./<dllname>.
//   If ran using dotnet run, it seems to invoke itself instead of
//   RemoteExecutorConsoleApp.exe.
//   If ran using dotnet bin/.../<dllname>, it fails with:
//   No executable found matching command "dotnet-<dir>/RemoteExecutorConsoleApp.exe"
//

using System;
using System.Runtime.Loader;
using System.Linq;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;
using Xunit.Abstractions;
using Xunit.ConsoleClient;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

class MsgSink : IMessageSink {

	public bool OnMessage (IMessageSinkMessage message) {
		Console.WriteLine (((Xunit.Sdk.DiagnosticMessage)message).Message);
		return true;
	}

	public bool OnMessageWithTypes (IMessageSinkMessage message, HashSet<string> messageTypes) {
		Console.WriteLine ("m2");
		return true;
	}
}

class Program
{
	static int nskipped = 0;

	// The template for the whole progeam
	const string runner_template = @"
using System;
using System.Reflection;

public class RunTests
{
    static int nrun = 0;
    static int nfailed = 0;

    public static void Run()
    {
    }

    public static int Main()
    {
        Run ();
        Console.WriteLine(""RUN: "" + nrun);
        Console.WriteLine(""FAILURES: "" + nfailed);
        Console.WriteLine(""SKIPPED: "" + #NSKIPPED#);
        if (nfailed == 0)
            return 0;
        else
            return 1;
    }
}
";
	// Template for calling 1 testcase instance
	// Only the code block matters
	const string case_template = @"
class Foo { void Run () {
Console.WriteLine(""#MSG#"");
nrun = (nrun + #NRUN#);
try {
unchecked {
            CALL();
}
        }
        catch (System.Exception ex) {
            nfailed = (nfailed + 1);
            Console.WriteLine(""FAILED: "" + ex);
        }
}}
";

	static string GetTypeName (Type t)
	{
		if (t == typeof (void))
			return "void";
		if (t.IsNested)
			return t.DeclaringType.FullName + "." + t.Name;
		else
			return t.FullName;
	}

	static void ComputeTraits (Assembly assembly, XunitTestCase tc)
	{
		// Traits are not set because of some assembly loading problems i.e. Assembly.Load (new AssemblyName ("Microsoft.DotNet.XUnitExtensions")) fails
		// So load them manually
		foreach (ReflectionAttributeInfo attr in ((ReflectionMethodInfo)tc.Method).GetCustomAttributes (typeof (ITraitAttribute))) {
			var discovererAttr = attr.GetCustomAttributes (typeof (TraitDiscovererAttribute)).FirstOrDefault();
			if (discovererAttr != null) {
				var discoverer_args = discovererAttr.GetConstructorArguments().Cast<string>().ToList();
				var disc_assembly = Assembly.LoadFrom (Path.Combine (Path.GetDirectoryName (assembly.Location), discoverer_args [1]) + ".dll");
				var disc_type = disc_assembly.GetType (discoverer_args [0]);
				var disc_obj = (ITraitDiscoverer)Activator.CreateInstance (disc_type);
				foreach (var trait in disc_obj.GetTraits (attr)) {
					if (!tc.Traits.ContainsKey (trait.Key))
						tc.Traits [trait.Key] = new List<string> ();
					tc.Traits [trait.Key].Add (trait.Value);
				}
			}
		}
	}

	class TcCase {
		public object[] Values;
		public MethodInfo MemberDataMethod;
	}

	static ExpressionSyntax EncodeValue (object val, Type expectedType)
	{
		ExpressionSyntax result = null;

		if (val == null)
			return LiteralExpression (SyntaxKind.NullLiteralExpression);

		if (val is Enum) {
			TypeCode typeCode = Convert.GetTypeCode (val);
			ExpressionSyntax lit;

			long l = 0;
			ulong ul = 0;
			if (typeCode == TypeCode.UInt64) {
				ul = UInt64.Parse (((Enum)val).ToString ("D"));
				lit = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal (ul));
			} else {
				l = Int64.Parse (((Enum)val).ToString ("D"));
				if (l < 0)
					//lit = ParenthesizedExpression (PrefixUnaryExpression (SyntaxKind.UnaryMinusExpression, LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal (l))));
					lit = CastExpression (PredefinedType (Token (SyntaxKind.LongKeyword)), LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal ((ulong)l)));
				else
					lit = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal (l));
			}
			result = CastExpression (IdentifierName (GetTypeName (val.GetType ())), lit);
			return result;
		}

		if (val is Type) {
			Type t = (Type)val;
			if (t.IsGenericType)
				return null;
			return TypeOfExpression (IdentifierName (GetTypeName ((Type)val)));
		}

		ExpressionSyntax LiteralWithCast (string type, SyntaxToken literal) => 
			CastExpression(IdentifierName (type), LiteralExpression (SyntaxKind.NumericLiteralExpression, literal));

		switch (Type.GetTypeCode (val.GetType ())) {
		case TypeCode.Boolean:
			if ((bool)val)
				result = LiteralExpression (SyntaxKind.TrueLiteralExpression);
			else
				result = LiteralExpression (SyntaxKind.FalseLiteralExpression);
			break;
		case TypeCode.Char:
			result = LiteralWithCast ("char", Literal ((char)val));
			break;
		case TypeCode.SByte:
			result = LiteralWithCast ("sbyte", Literal ((sbyte)val));
			break;
		case TypeCode.Byte:
			result = LiteralWithCast ("byte", Literal ((byte)val));
			break;
		case TypeCode.Int16:
			result = LiteralWithCast ("short", Literal ((short)val));
			break;
		case TypeCode.UInt16:
			result = LiteralWithCast ("ushort", Literal ((ushort)val));
			break;
		case TypeCode.Int32:
			result = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal ((int)val));
			break;
		case TypeCode.UInt32:
			result = LiteralWithCast ("uint", Literal ((uint)val));
			break;
		case TypeCode.Int64:
			result = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal ((long)val));
			break;
		case TypeCode.UInt64:
			result = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal ((ulong)val));
			break;
		case TypeCode.Decimal:
			result = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal ((decimal)val));
			break;
		case TypeCode.Single:
			result = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal ((float)val));
			break;
		case TypeCode.Double:
			result = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal ((double)val));
			break;
		case TypeCode.String:
			result = LiteralExpression (SyntaxKind.NumericLiteralExpression, Literal ((string)val));
			break;
		}

		if (val is Array) {
			var arr = (IEnumerable)val;

			var etype = val.GetType ().GetElementType ();
			var type_node = ArrayType (IdentifierName (etype.FullName))
                                                    .WithRankSpecifiers(
                                                        SingletonList<ArrayRankSpecifierSyntax>(
                                                            ArrayRankSpecifier(
                                                                SingletonSeparatedList<ExpressionSyntax>(
																										 OmittedArraySizeExpression()))));
			var elems = new List<ExpressionSyntax> ();
			foreach (var elem in arr) {
				var encoded = EncodeValue (elem, null);
				if (encoded == null)
					return null;
				elems.Add (encoded);
			}
			result = ArrayCreationExpression (type_node).WithInitializer (InitializerExpression (SyntaxKind.ArrayInitializerExpression, SeparatedList<ExpressionSyntax> (elems.ToArray ())));
			return result;
		}

		if (val is Guid guid) {
			var argumentList = SeparatedList (new[] { Argument(LiteralExpression (SyntaxKind.StringLiteralExpression, Literal (guid.ToString ()))) });
			var guidParse = MemberAccessExpression (SyntaxKind.SimpleMemberAccessExpression, IdentifierName ("Guid"), IdentifierName ("Parse"));
			result = InvocationExpression (guidParse, ArgumentList (argumentList));
		}

		if (val is IntPtr ptr) {
			result = LiteralWithCast (nameof (IntPtr), Literal ((ulong) ptr));
		}

		if (val is UIntPtr uptr) {
			result = LiteralWithCast (nameof (UIntPtr), Literal ((ulong) uptr));
		}

		if (result == null) {
			Console.WriteLine ($"Unhandled value: {val} ({val?.GetType()})");
			return null;
		}

		if (val != null && expectedType != null && Nullable.GetUnderlyingType(expectedType) == null && val.GetType () != expectedType && !expectedType.IsGenericParameter)
			result = CastExpression (IdentifierName (GetTypeName (expectedType)), ParenthesizedExpression (result));
		return result;
	}

	static int Main (string[] args)
	{
		if (args.Length < 3) {
			Console.WriteLine ("Usage: <out-dir> <corefx dir> <test assembly filename> <xunit console options>");
			return 1;
		}

		var testAssemblyName = Path.GetFileNameWithoutExtension (args [2]);
		var testAssemblyFull = Path.GetFullPath (args[2]);
		var outdir_name = Path.Combine (args [0], testAssemblyName);
		var sdkdir = args [1] + "/artifacts/bin/runtime/netcoreapp-OSX-Debug-x64";
		args = args.Skip (2).ToArray ();
		// Response file support
		var extra_args = new List<string> ();
		for (int i = 0; i < args.Length; ++i) {
			var arg = args [i];
			if (arg [0] == '@') {
				foreach (var line in File.ReadAllLines (arg.Substring (1))) {
					if (line.Length == 0 || line [0] == '#')
						continue;
					extra_args.AddRange (line.Split (' '));
				}
				args [i] = "";
			}
		}
		args = args.Where (s => s != String.Empty).Concat (extra_args).ToArray ();

		// Despite a lot of effort, couldn't get dotnet to load these assemblies from the sdk dir, so copy them to our binary dir
//		File.Copy ($"{sdkdir}/Microsoft.DotNet.PlatformAbstractions.dll", AppContext.BaseDirectory, true);
		File.Copy ($"{sdkdir}/CoreFx.Private.TestUtilities.dll", AppContext.BaseDirectory, true);
		File.Copy ($"{sdkdir}/Microsoft.DotNet.XUnitExtensions.dll", AppContext.BaseDirectory, true);

		var cmdline = CommandLine.Parse (args);

		// Ditto
		File.Copy (cmdline.Project.Assemblies.First ().AssemblyFilename, AppContext.BaseDirectory, true);

		var assembly = Assembly.LoadFrom (Path.Combine (AppContext.BaseDirectory, Path.GetFileName (cmdline.Project.Assemblies.First ().AssemblyFilename)));

		var msg_sink = new MsgSink ();
		var xunit2 = new Xunit2Discoverer (AppDomainSupport.Denied, new NullSourceInformationProvider (), new ReflectionAssemblyInfo (assembly), null, null, msg_sink);
		var sink = new TestDiscoverySink ();
		var config = new TestAssemblyConfiguration () { DiagnosticMessages = true, InternalDiagnosticMessages = true, PreEnumerateTheories = false };
		xunit2.Find (false, sink, TestFrameworkOptions.ForDiscovery (config));
		sink.Finished.WaitOne ();

		foreach (XunitTestCase tc in sink.TestCases)
			ComputeTraits (assembly, tc);

		// Compute testcase data
		var tc_data = new Dictionary<XunitTestCase, List<TcCase>> ();
		foreach (XunitTestCase tc in sink.TestCases) {
			var m = ((ReflectionMethodInfo)tc.Method).MethodInfo;
			var t = m.ReflectedType;

			var cases = new List<TcCase> ();

			if (m.GetParameters ().Length > 0) {
				foreach (var cattr in m.GetCustomAttributes (true))
					if (cattr is InlineDataAttribute) {
						var data = ((InlineDataAttribute)cattr).GetData (null).First ();
						if (data == null)
							data = new object [m.GetParameters ().Length];
						if (data.Length != m.GetParameters ().Length)
							throw new Exception ();

						bool unhandled = false;
						foreach (var val in data) {
							if (val is float || val is double)
								unhandled = true;
							if (val is Type) {
								var type = val as Type;
								if (!type.IsVisible)
									unhandled = true;
							}
							if (val is Enum) {
								if (!val.GetType ().IsPublic)
									unhandled = true;
							}
						}
						if (!unhandled)
							cases.Add (new TcCase () { Values = data });
					} else if (cattr is MemberDataAttribute memberData && memberData.Parameters.Length == 0) {
						MethodInfo testDataMethod = m.DeclaringType.GetMethod (memberData.MemberName);
						if (testDataMethod == null)
							continue;
						cases.Add (new TcCase { MemberDataMethod = testDataMethod });
					}
			} else {
				cases.Add (new TcCase ());
			}
			tc_data [tc] = cases;
		}

		var filters = cmdline.Project.Filters;

		//
		// Generate code using the roslyn syntax apis
		// Creating syntax nodes one-by-one is very cumbersome, so use
		// CSharpSyntaxTree.ParseText () to create them and ReplaceNode () to insert them into syntax trees.
		//
		var blocks = new List<BlockSyntax> ();
		foreach (XunitTestCase tc in sink.TestCases) {
			var m = ((ReflectionMethodInfo)tc.Method).MethodInfo;
			//Console.WriteLine ("" + m.ReflectedType + " " + m + " " + (tc.TestMethodArguments == null));
			var t = m.ReflectedType;
			if (t.IsGenericType) {
				nskipped ++;
				continue;
			}
			if (!filters.Filter (tc)) {
				nskipped ++;
				continue;
			}

			var cases = tc_data [tc];

			int caseindex = 0;
			foreach (var test in cases) {
				string typename = GetTypeName (t);
				string msg;

				if (cases.Count > 1)
					msg = $"{typename}.{m.Name}[{caseindex}] ...";
				else
					msg = $"{typename}.{m.Name} ...";
				caseindex ++;

				var block = ParseText<BlockSyntax> (case_template
					.Replace ("#MSG#", msg)
					.Replace ("#NRUN#", test.MemberDataMethod == null ? "1" : ((IEnumerable) test.MemberDataMethod.Invoke (null, null)).Cast<object> ().Count ().ToString ()));
				// Obtain the node for the CALL () line
				var try_body_node = block.DescendantNodes ().OfType<ExpressionStatementSyntax> ().Skip (2).First ().Parent;
				// Replace with the generated call code
				var stmts = GenerateTcCall (t, test, m);
				blocks.Add (block.ReplaceNode (try_body_node, Block (stmts.ToArray ())));
			}
		}

		var cu = CSharpSyntaxTree.ParseText (runner_template.Replace ("#NSKIPPED#", nskipped.ToString ())).GetRoot ();

		// Replace the body of the Run () method with the generated body
		var run_body = cu.DescendantNodes ().OfType<MethodDeclarationSyntax> ().First ().DescendantNodes ().OfType<BlockSyntax> ().First ();
		cu = cu.ReplaceNode (run_body, Block (blocks.ToArray ()));

		// Create runner.cs
		Directory.CreateDirectory (outdir_name);
		var outfile_name = Path.Combine (outdir_name, "runner.cs");
		File.WriteAllText (outfile_name, cu.NormalizeWhitespace ().ToString ());

		// Generate csproj file
		var csproj_template = File.ReadAllText ("gen-test.csproj.template");
		csproj_template = csproj_template.Replace ("#XUNIT_LOCATION#", sdkdir);
		csproj_template = csproj_template.Replace ("#TEST_ASSEMBLY#", testAssemblyName);
		csproj_template = csproj_template.Replace ("#TEST_ASSEMBLY_LOCATION#", testAssemblyFull);
		
		File.WriteAllText (Path.Combine (outdir_name, testAssemblyName + "-runner.csproj"), csproj_template);

		return 0;
	}

	static TSyntax ParseText<TSyntax> (string code)
	{
		return CSharpSyntaxTree.ParseText (code).GetRoot().DescendantNodes().OfType<TSyntax> ().First ();
    }

	static List<StatementSyntax> GenerateTcCall (Type t, TcCase test, MethodInfo m) {
		var stmts = new List<StatementSyntax> ();
		if (!m.IsStatic) {
			var newobj_template = @"class Foo { void Run () { var o = new #CLASS# (); }}";

			// FIXME: Disposable
			stmts.Add (ParseText<LocalDeclarationStatementSyntax> (newobj_template.Replace ("#CLASS#", t.FullName)));
		}
		if (!m.IsPublic) {
			// FIXME:
			nskipped ++;
			return stmts;
		}
		string callstr;
		StatementSyntax node = null;
		
		var tname = GetTypeName (t);
		if (test.MemberDataMethod != null) {
			callstr = $"class Foo {{ void Run () {{ foreach (var row in {tname}.{test.MemberDataMethod.Name} ()) typeof ({tname}).GetMethod (\"{m.Name}\").Invoke ({(m.IsStatic ? "null" : "o")}, (object []) row); }}";
			node = ParseText<StatementSyntax> (callstr);
		} else {
			callstr = $"class Foo {{ void Run () {{ {(m.IsStatic ? tname : "o")}.{m.Name} (); }}";
			node = ParseText<ExpressionStatementSyntax> (callstr);
		}
		
		if (test.Values != null && test.MemberDataMethod == null) {
			var parameters = m.GetParameters ();
			var arg_nodes = new List<ArgumentSyntax> ();
			for (var index = 0; index < test.Values.Length; index++) {
				var val_node = EncodeValue (test.Values [index], parameters [index].ParameterType);
				if (val_node == null) {
					nskipped ++;
					return stmts;
				}
				arg_nodes.Add (Argument (val_node));
			}
			var args_node = node.DescendantNodes ().OfType<ArgumentListSyntax> ().First ();
			node = node.ReplaceNode (args_node, ArgumentList (SeparatedList (arg_nodes.ToArray ())));
		}
		
		stmts.Add (node);
		return stmts;
	}
}
