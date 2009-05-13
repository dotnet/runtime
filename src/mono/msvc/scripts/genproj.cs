using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Xml.Linq;

public enum Target {
	Library, Exe, Module, WinExe
}

public enum LanguageVersion
{
	ISO_1		= 1,
	Default_MCS	= 2,
	ISO_2		= 3,
	LINQ		= 4,
	Future		= 5,
	Default		= LINQ
}

class SlnGenerator {
	const string header = "Microsoft Visual Studio Solution File, Format Version 10.00\n" +
		"# Visual Studio 2008";

	const string project_start = "Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{0}\", \"{1}\", \"{{{2}}}\"";
	const string project_end = "EndProject";

	Dictionary<string, string> libraries = new Dictionary<string, string> ();

	public void Add (string library)
	{
		try {
			libraries.Add (library, Guid.NewGuid ().ToString ().ToUpper ());
		}
		catch (Exception ex) {
			Console.WriteLine (ex);
		}
	}

	public void Write (string filename)
	{
		using (var sln = new StreamWriter (filename)) {
			sln.WriteLine ();
			sln.WriteLine (header);
			foreach (var library in libraries) {
				var library_name = Path.GetFileNameWithoutExtension (library.Key);
				sln.WriteLine (project_start, library_name, library.Key, library.Value);
				sln.WriteLine (project_end);
			}
			sln.WriteLine ("Global");

			sln.WriteLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
			sln.WriteLine ("\t\tDebug|Any CPU = Debug|Any CPU");
			sln.WriteLine ("\t\tRelease|Any CPU = Release|Any CPU");
			sln.WriteLine ("\tEndGlobalSection");
			
			sln.WriteLine ("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
			foreach (var library in libraries) {
				sln.WriteLine ("\t\t{{{0}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", library.Value);
				sln.WriteLine ("\t\t{{{0}}}.Debug|Any CPU.Build.0 = Debug|Any CPU", library.Value);
				sln.WriteLine ("\t\t{{{0}}}.Release|Any CPU.ActiveCfg = Release|Any CPU", library.Value);
				sln.WriteLine ("\t\t{{{0}}}.Release|Any CPU.Build.0 = Release|Any CPU", library.Value);
			}
			sln.WriteLine ("\tEndGlobalSection");
				
			sln.WriteLine ("\tGlobalSection(SolutionProperties) = preSolution");
			sln.WriteLine ("\t\tHideSolutionNode = FALSE");
			sln.WriteLine ("\tEndGlobalSection");

			sln.WriteLine ("EndGlobal");
		}
	}
}

class MsbuildGenerator {
	static void Usage ()
	{
		Console.WriteLine ("Invalid argument");
	}

	static string template;
	static MsbuildGenerator ()
	{
		using (var input = new StreamReader ("csproj.tmpl")){
			template = input.ReadToEnd ();
		}
	}

	// The directory as specified in order.xml
	string dir;

	//
	// Our base directory, this is relative to our exectution point mono/msvc/scripts
	string base_dir;

	string mcs_topdir;

	// Class directory, relative to 
	string class_dir;
	
	public MsbuildGenerator (string dir)
	{
		this.dir = dir;
		
		if (dir == "mcs"){
			mcs_topdir = "..\\";
			class_dir = "..\\class\\";
			base_dir = "..\\..\\..\\mcs\\mcs";
		} else {
			mcs_topdir = "..\\";
			
			foreach (char c in dir){
				if (c == '/')
					mcs_topdir = "..\\" + mcs_topdir;
			}
			class_dir = mcs_topdir.Substring (3);
			
			base_dir = "..\\..\\..\\mcs\\" + dir;
		}
	}
	
	// Currently used
	bool Unsafe = false;
	StringBuilder defines = new StringBuilder ();
	bool StdLib = true;

	// Currently unused
	Target Target = Target.Exe;
	string TargetExt = ".exe";
	string OutputFile;
	bool Optimize = true;
	bool VerifyClsCompliance = true;

	string win32IconFile;
	bool want_debugging_support = false;
	bool Checked = false;
	bool WarningsAreErrors;
	Dictionary<string,string> embedded_resources = new Dictionary<string,string> ();
	List<string> references = new List<string> ();
	List<string> reference_aliases = new List<string> ();
	List<string> warning_as_error = new List<string> ();
	int WarningLevel = 4;
	List<int> ignore_warning = new List<int> ();
	bool load_default_config = true;
	string StrongNameKeyFile;
	string StrongNameKeyContainer;
	bool StrongNameDelaySign = false;
	LanguageVersion Version = LanguageVersion.Default;
	string CodePage;

	readonly char[] argument_value_separator = new char [] { ';', ',' };

	//
	// This parses the -arg and /arg options to the compiler, even if the strings
	// in the following text use "/arg" on the strings.
	//
	bool CSCParseOption (string option, ref string [] args)
	{
		int idx = option.IndexOf (':');
		string arg, value;

		if (idx == -1){
			arg = option;
			value = "";
		} else {
			arg = option.Substring (0, idx);

			value = option.Substring (idx + 1);
		}

		switch (arg.ToLower (CultureInfo.InvariantCulture)){
		case "/nologo":
			return true;

		case "/t":
		case "/target":
			switch (value){
			case "exe":
				Target = Target.Exe;
				break;

			case "winexe":
				Target = Target.WinExe;
				break;

			case "library":
				Target = Target.Library;
				TargetExt = ".dll";
				break;

			case "module":
				Target = Target.Module;
				TargetExt = ".netmodule";
				break;

			default:
				return false;
			}
			return true;

		case "/out":
			if (value.Length == 0){
				Usage ();
				Environment.Exit (1);
			}
			OutputFile = value;
			return true;

		case "/o":
		case "/o+":
		case "/optimize":
		case "/optimize+":
			Optimize = true;
			return true;

		case "/o-":
		case "/optimize-":
			Optimize = false;
			return true;

		case "/incremental":
		case "/incremental+":
		case "/incremental-":
			// nothing.
			return true;

		case "/d":
		case "/define": {
			if (value.Length == 0){
				Usage ();
				Environment.Exit (1);
			}

			foreach (string d in value.Split (argument_value_separator)){
				if (defines.Length != 0)
					defines.Append (";");
				defines.Append (d);
			}

			return true;
		}

		case "/bugreport":
			//
			// We should collect data, runtime, etc and store in the file specified
			//
			return true;
		case "/linkres":
		case "/linkresource":
		case "/res":
		case "/resource":
			bool embeded = arg [1] == 'r' || arg [1] == 'R';
			string[] s = value.Split (argument_value_separator);
			switch (s.Length) {
			case 1:
				if (s[0].Length == 0)
					goto default;
				embedded_resources [s[0]] = Path.GetFileName (s[0]);
				break;
			case 2:
				embedded_resources [s [0]] = s [1];
				break;
			case 3:
				Console.WriteLine ("Does not support this method yet: {0}", arg);
				Environment.Exit (1);
				break;
			default:
				Console.WriteLine ("Wrong number of arguments for option `{0}'", option);
				Environment.Exit (1);
				break;
				
			}

			return true;
				
		case "/recurse":
			Console.WriteLine ("/recurse not supported");
			Environment.Exit (1);
			return true;

		case "/r":
		case "/reference": {
			if (value.Length == 0){
				Console.WriteLine ("-reference requires an argument");
				Environment.Exit (1);
			}

			string[] refs = value.Split (argument_value_separator);
			foreach (string r in refs){
				string val = r;
				int index = val.IndexOf ('=');
				if (index > -1) {
					reference_aliases.Add (r);
					continue;
				}

				if (val.Length != 0)
					references.Add (val);
			}
			return true;
		}
		case "/main":
		case "/m":
		case "/addmodule": 
		case "/win32res":
		case "/doc": 
		case "/lib": 
		{
			Console.WriteLine ("{0} = not supported", arg);
			throw new Exception ();
		}
		case "/win32icon": {
			win32IconFile = value;
			return true;
		}
		case "/debug-":
			want_debugging_support = false;
			return true;
				
		case "/debug":
		case "/debug+":
			want_debugging_support = true;
			return true;

		case "/checked":
		case "/checked+":
			Checked = true;
			return true;

		case "/checked-":
			Checked = false;
			return true;

		case "/clscheck":
		case "/clscheck+":
			return true;

		case "/clscheck-":
			VerifyClsCompliance = false;
			return true;

		case "/unsafe":
		case "/unsafe+":
			Unsafe = true;
			return true;

		case "/unsafe-":
			Unsafe = false;
			return true;

		case "/warnaserror":
		case "/warnaserror+":
			if (value.Length == 0) {
				WarningsAreErrors = true;
			} else {
				foreach (string wid in value.Split (argument_value_separator))
					warning_as_error.Add (wid);
			}
			return true;

		case "/warnaserror-":
			if (value.Length == 0) {
				WarningsAreErrors = false;
			} else {
				foreach (string wid in value.Split (argument_value_separator))
					warning_as_error.Remove (wid);
			}
			return true;

		case "/warn":
			WarningLevel = Int32.Parse (value);
			return true;

		case "/nowarn": {
			string [] warns;

			if (value.Length == 0){
				Console.WriteLine ("/nowarn requires an argument");
				Environment.Exit (1);
			}

			warns = value.Split (argument_value_separator);
			foreach (string wc in warns){
				try {
					if (wc.Trim ().Length == 0)
						continue;

					int warn = Int32.Parse (wc);
					if (warn < 1) {
						throw new ArgumentOutOfRangeException("warn");
					}
					ignore_warning.Add (warn);
				} catch {
					Console.WriteLine (String.Format("`{0}' is not a valid warning number", wc));
					Environment.Exit (1);
				}
			}
			return true;
		}

		case "/noconfig":
			load_default_config = false;
			return true;

		case "/nostdlib":
		case "/nostdlib+":
			StdLib = false;
			return true;

		case "/nostdlib-":
			StdLib = true;
			return true;

		case "/fullpaths":
			return true;

		case "/keyfile":
			if (value == String.Empty) {
				Console.WriteLine ("{0} requires an argument", arg);
				Environment.Exit (1);
			}
			StrongNameKeyFile = value;
			return true;
		case "/keycontainer":
			if (value == String.Empty) {
				Console.WriteLine ("{0} requires an argument", arg);
				Environment.Exit (1);
			}
			StrongNameKeyContainer = value;
			return true;
		case "/delaysign+":
			StrongNameDelaySign = true;
			return true;
		case "/delaysign-":
			StrongNameDelaySign = false;
			return true;

		case "/langversion":
			switch (value.ToLower (CultureInfo.InvariantCulture)) {
			case "iso-1":
				Version = LanguageVersion.ISO_1;
				return true;
					
			case "default":
				Version = LanguageVersion.Default;
				return true;
			case "iso-2":
				Version = LanguageVersion.ISO_2;
				return true;
			case "future":
				Version = LanguageVersion.Future;
				return true;
			}
			Console.WriteLine ("Invalid option `{0}' for /langversion. It must be either `ISO-1', `ISO-2' or `Default'", value);
			Environment.Exit (1);
			return true;
			
		case "/codepage":
			CodePage = value;
			return true;
		}

		return false;
	}

	static string [] LoadArgs (string file)
	{
		StreamReader f;
		var args = new List<string> ();
		string line;
		try {
			f = new StreamReader (file);
		} catch {
			return null;
		}
		
		StringBuilder sb = new StringBuilder ();
		
		while ((line = f.ReadLine ()) != null){
			int t = line.Length;
			
			for (int i = 0; i < t; i++){
				char c = line [i];
				
				if (c == '"' || c == '\''){
					char end = c;
					
					for (i++; i < t; i++){
						c = line [i];
						
						if (c == end)
							break;
						sb.Append (c);
					}
				} else if (c == ' '){
					if (sb.Length > 0){
						args.Add (sb.ToString ());
						sb.Length = 0;
					}
				} else
					sb.Append (c);
			}
			if (sb.Length > 0){
				args.Add (sb.ToString ());
				sb.Length = 0;
			}
		}
		
		string [] ret_value = new string [args.Count];
		args.CopyTo (ret_value, 0);
		
		return ret_value;
	}

	static string Load (string f)
	{
		if (File.Exists (f)){
			using (var sr = new StreamReader (f)){
				return sr.ReadToEnd ();
			}
		} else
			return "";
	}
	
	public string Generate (XElement xproject)
	{
		string library = xproject.Attribute ("library").Value;
		string boot, mcs, flags, output_name, built_sources, library_output, response;

		boot  = xproject.Element ("boot").Value;
		mcs   = xproject.Element ("mcs").Value;
		flags = xproject.Element ("flags").Value;
		output_name =xproject.Element ("output").Value;
		built_sources = xproject.Element ("built_sources").Value;
		library_output = xproject.Element ("library_output").Value;
		response = xproject.Element ("response").Value;

		//
		// Prebuild code, might be in inputs, check:
		//  inputs/LIBRARY-PROFILE.pre
		//  inputs/LIBRARY.pre
		//
		string prebuild = Load (library + ".pre");

		int q = library.IndexOf ("-");
		if (q != -1)
			prebuild = prebuild + Load (library.Substring (0, q) + ".pre");
			
		var all_args = new Queue<string []> ();
		all_args.Enqueue (flags.Split ());
		while (all_args.Count > 0){
			string [] f = all_args.Dequeue ();
			
			for (int i = 0; i < f.Length; i++){
				if (f [i][0] == '-')
					f [i] = "/" + f [i].Substring (1);
				
				if (f [i][0] == '@') {
					string [] extra_args;
					string response_file = f [i].Substring (1);
					
					extra_args = LoadArgs (base_dir + "\\" + response_file);
					if (extra_args == null) {
						Console.WriteLine ("Unable to open response file: " + response_file);
						Environment.Exit (1);
					}

					all_args.Enqueue (extra_args);
					continue;
				}
				
				if (CSCParseOption (f [i], ref f))
					continue;
				Console.WriteLine ("Failure with {0}", f [i]);
				Environment.Exit (1);
			}
		}
		
		string [] source_files;
		using (var reader = new StreamReader (base_dir + "\\" + response)){
			source_files  = reader.ReadToEnd ().Split ();
		}
		StringBuilder sources = new StringBuilder ();
		foreach (string s in source_files){
			if (s.Length == 0)
				continue;
			sources.Append (String.Format ("   <Compile Include=\"{0}\" />\n", s.Replace ("/", "\\")));
		}
		foreach (string s in built_sources.Split ()){
			if (s.Length == 0)
				continue;
			
			sources.Append (String.Format ("   <Compile Include=\"{0}\" />\n", s.Replace ("/", "\\")));
		}
		
		//
		// Compute the csc command that we need to use
		//
		// The mcs string is formatted like this:
		// MONO_PATH=./../../class/lib/basic: /cvs/mono/runtime/mono-wrapper ./../../class/lib/basic/mcs.exe
		//
		// The first block is a set of MONO_PATHs, the last part is the compiler
		//
		if (mcs.StartsWith ("MONO_PATH="))
			mcs = mcs.Substring (10);
		
		var compiler = mcs.Substring (mcs.LastIndexOf (' ') + 1);
		if (compiler.EndsWith ("class/lib/basic/mcs.exe"))
			compiler = "basic";
		else if (compiler.EndsWith ("class/lib/net_1_1_bootstrap/mcs.exe"))
			compiler = "net_1_1_bootstrap";
		else if (compiler.EndsWith ("class/lib/net_1_1/mcs.exe"))
			compiler = "net_1_1";
		else if (compiler.EndsWith ("class/lib/net_2_0_bootstrap/gmcs.exe"))
			compiler = "net_2_0_bootstrap";
		else if (compiler.EndsWith ("mcs/gmcs.exe"))
			compiler = "gmcs";
		else if (compiler.EndsWith ("class/lib/net_2_1_bootstrap/smcs.exe"))
			compiler = "net_2_1_bootstrap";
		else if (compiler.EndsWith ("class/lib/net_2_1_raw/smcs.exe"))
			compiler = "net_2_1_raw";
		else {
			Console.WriteLine ("Can not determine compiler from {0}", compiler);
			Environment.Exit (1);
		}

		var mono_paths = mcs.Substring (0, mcs.IndexOf (' ')).Split (new char [] {':'});
		for (int i = 0; i < mono_paths.Length; i++){
			int p = mono_paths [i].LastIndexOf ('/');
			if (p != -1)
				mono_paths [i] = mono_paths [i].Substring (p + 1);
		}
		
		var encoded_mono_paths = string.Join ("-", mono_paths).Replace ("--", "-");
		var encoded_mp_compiler = (encoded_mono_paths + "-" + compiler).Replace ("--", "-");
		
		string csc_tool_path = mcs_topdir + "..\\mono\\msvc\\scripts\\" + encoded_mp_compiler;
		if (!Directory.Exists (encoded_mp_compiler)){
			Console.WriteLine ("Created {0}", encoded_mp_compiler);
			Directory.CreateDirectory (encoded_mp_compiler);
		}
		if (!File.Exists (Path.Combine (encoded_mp_compiler, "csc.exe"))){
			File.Copy ("monowrap.exe", Path.Combine (encoded_mp_compiler, "csc.exe"));
			File.Copy ("monowrap.pdb", Path.Combine (encoded_mp_compiler, "csc.pdb"));
		}
		
		var refs = new StringBuilder ();
		//
		// mcs is different that csc in this regard, somehow with -noconfig we still import System and System.XML
		//
		if (dir == "mcs" && !load_default_config){
			references.Add ("System.dll");
			references.Add ("System.Xml.dll");
		}
		
		if (references.Count > 0 || reference_aliases.Count > 0){
			refs.Append ("<ItemGroup>\n");
			string last = mono_paths [0].Substring (mono_paths [0].LastIndexOf ('/') + 1);
			
			string hint_path = class_dir + "\\lib\\" + last;

			foreach (string r in references){
				refs.Append ("    <Reference Include=\"" + r + "\">\n");
				refs.Append ("      <SpecificVersion>False</SpecificVersion>\n");
				refs.Append ("      <HintPath>" + hint_path + "\\" + r + "</HintPath>\n");
				refs.Append ("    </Reference>\n");
			}

			foreach (string r in reference_aliases){
				int index = r.IndexOf ('=');
				string alias = r.Substring (0, index);
				string assembly = r.Substring (index + 1);

				refs.Append ("    <Reference Include=\"" + assembly + "\">\n");
				refs.Append ("      <SpecificVersion>False</SpecificVersion>\n");
				refs.Append ("      <HintPath>" + hint_path + "\\" + r + "</HintPath>\n");
				refs.Append ("      <Aliases>" + alias + "</Aliases>\n");
				refs.Append ("    </Reference>\n");
			}
			
			refs.Append ("  </ItemGroup>\n");
		}

		try {
			Path.GetDirectoryName (library_output);
		} catch {
			Console.WriteLine ("Error in path: {0} while processing {1}", library_output, library);
		}
		
		//
		// Replace the template values
		//
		string output = template.
			Replace ("@DEFINES@", defines.ToString ()).
			Replace ("@NOSTDLIB@", StdLib ? "" : "<NoStdLib>true</NoStdLib>").
			Replace ("@ALLOWUNSAFE@", Unsafe ? "<AllowUnsafeBlocks>true</AllowUnsafeBlocks>" : "").
			Replace ("@ASSEMBLYNAME@", Path.GetFileNameWithoutExtension (output_name)).
			Replace ("@OUTPUTDIR@", Path.GetDirectoryName (library_output)).
			Replace ("@DEFINECONSTANTS@", defines.ToString ()).
			Replace ("@CSCTOOLPATH@", csc_tool_path).
			Replace ("@DEBUG@", want_debugging_support ? "true" : "false").
			Replace ("@DEBUGTYPE@", want_debugging_support ? "full" : "pdbonly").
			Replace ("@REFERENCES@", refs.ToString ()).
			Replace ("@PREBUILD@", prebuild).
			Replace ("@SOURCES@", sources.ToString ());


		string ofile = "..\\..\\..\\mcs\\" + dir + "\\" + library + ".csproj";
		ofile = ofile.Replace ('/', '\\');
		//Console.WriteLine ("Generated {0}", ofile.Replace ("\\", "/"));
		using (var o = new StreamWriter (ofile)){
			o.WriteLine (output);
		}

		return ofile;
	}
	
}

public class Driver {
	
	static void Main (string [] args)
	{
		if (!File.Exists ("genproj.cs") || !File.Exists ("monowrap.cs")){
			Console.WriteLine ("This command should be ran from mono/msvc/scripts");
			Environment.Exit (1);
		}

        var sln_gen = new SlnGenerator ();
		XDocument doc = XDocument.Load ("order.xml");
		foreach (XElement project in doc.Root.Elements ()){
			string dir = project.Attribute ("dir").Value;
			string library = project.Attribute ("library").Value;

			//
			// Do only class libraries for now
			//
			if (!(dir.StartsWith ("class") || dir.StartsWith ("mcs")))
				continue;

			//
			// Do not do 2.1, it is not working yet
			// Do not do basic, as there is no point (requires a system mcs to be installed).
			//
			if (library.Contains ("net_2_1") || library.Contains ("-basic"))
				continue;
			
			var gen = new MsbuildGenerator (dir);
			try {
				sln_gen.Add (gen.Generate (project));
			} catch (Exception e) {
				Console.WriteLine ("Error in {0}\n{1}", dir, e);
			}
		}
        sln_gen.Write ("mcs_full.sln");
    }

}
