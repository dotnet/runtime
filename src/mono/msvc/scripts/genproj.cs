//
// Consumes the order.xml file that contains a list of all the assemblies to build
// and produces a solution and the csproj files for it
//
// Currently this hardcodes a set of assemblies to build, the net-4.x series, but 
// it can be extended to handle the command line tools.
//
// KNOWN ISSUES:
//    * This fails to find matches for "System" and "System.xml" when processing the
//      RabbitMQ executable, likely, because we do not process executables yet
//
//    * Has not been tested in a while with the command line tools
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Linq;
using System.Xml;

public enum Target {
	Library, Exe, Module, WinExe
}

class SlnGenerator {
	public static readonly string NewLine = "\r\n"; //Environment.NewLine; // "\n"; 
	public SlnGenerator (string formatVersion = "2012")
	{
		switch (formatVersion) {
		case "2008":
			this.header = MakeHeader ("10.00", "2008");
			break;
		default:
			this.header = MakeHeader ("12.00", "2012");
			break;
		}
	}

	const string project_start = "Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{0}\", \"{1}\", \"{2}\""; // Note: No need to double up on {} around {2}
	const string project_end = "EndProject";

	public List<MsbuildGenerator.VsCsproj> libraries = new List<MsbuildGenerator.VsCsproj> ();
	string header;

	string MakeHeader (string formatVersion, string yearTag)
	{
		return string.Format ("Microsoft Visual Studio Solution File, Format Version {0}" + NewLine + "# Visual Studio {1}", formatVersion, yearTag);
	}

	public void Add (MsbuildGenerator.VsCsproj vsproj)
	{
		try {
			libraries.Add (vsproj);
		} catch (Exception ex) {
			Console.WriteLine (ex);
		}
	}

	public void Write (string filename)
	{
		var fullPath = Path.GetDirectoryName (filename) + "/";
		
		using (var sln = new StreamWriter (filename)) {
			sln.WriteLine ();
			sln.WriteLine (header);
			foreach (var proj in libraries) {
				var unixProjFile = proj.csProjFilename.Replace ("\\", "/");
				var fullProjPath = Path.GetFullPath (unixProjFile);
				sln.WriteLine (project_start, proj.library, MsbuildGenerator.GetRelativePath (fullPath, fullProjPath), proj.projectGuid);
				sln.WriteLine (project_end);
			}
			sln.WriteLine ("Global");

			sln.WriteLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
			sln.WriteLine ("\t\tDebug|Any CPU = Debug|Any CPU");
			sln.WriteLine ("\t\tRelease|Any CPU = Release|Any CPU");
			sln.WriteLine ("\tEndGlobalSection");

			sln.WriteLine ("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
			foreach (var proj in libraries) {
				var guid = proj.projectGuid;
				sln.WriteLine ("\t\t{0}.Debug|Any CPU.ActiveCfg = Debug|Any CPU", guid);
				sln.WriteLine ("\t\t{0}.Debug|Any CPU.Build.0 = Debug|Any CPU", guid);
				sln.WriteLine ("\t\t{0}.Release|Any CPU.ActiveCfg = Release|Any CPU", guid);
				sln.WriteLine ("\t\t{0}.Release|Any CPU.Build.0 = Release|Any CPU", guid);
			}
			sln.WriteLine ("\tEndGlobalSection");

			sln.WriteLine ("\tGlobalSection(SolutionProperties) = preSolution");
			sln.WriteLine ("\t\tHideSolutionNode = FALSE");
			sln.WriteLine ("\tEndGlobalSection");

			sln.WriteLine ("EndGlobal");
		}
	}

	internal bool ContainsProjectIdentifier (string projId)
	{
		return libraries.FindIndex (x => (x.library == projId)) >= 0;
	}

	public int Count { get { return libraries.Count; } }
}

class MsbuildGenerator {
	static readonly string NewLine = SlnGenerator.NewLine;
	static XmlNamespaceManager xmlns;

	public const string profile_2_0 = "_2_0";
	public const string profile_3_5 = "_3_5";
	public const string profile_4_0 = "_4_0";
	public const string profile_4_x = "_4_x";

	static void Usage ()
	{
		Console.WriteLine ("Invalid argument");
	}

	static string template;
	static MsbuildGenerator ()
	{
		using (var input = new StreamReader ("csproj.tmpl")) {
			template = input.ReadToEnd ();
		}

		xmlns = new XmlNamespaceManager (new NameTable ());
		xmlns.AddNamespace ("x", "http://schemas.microsoft.com/developer/msbuild/2003");
	}

	// The directory as specified in order.xml
	public string dir;
	string library;
	string projectGuid;
	string fx_version;

	XElement xproject;
	public string CsprojFilename;

	//
	// Our base directory, this is relative to our exectution point mono/msvc/scripts
	string base_dir;
	string mcs_topdir;

	public string LibraryOutput, AbsoluteLibraryOutput;

	public MsbuildGenerator (XElement xproject)
	{
		this.xproject = xproject;
		dir = xproject.Attribute ("dir").Value;
		library = xproject.Attribute ("library").Value;
		CsprojFilename = "..\\..\\mcs\\" + dir + "\\" + library + ".csproj";
		LibraryOutput = xproject.Element ("library_output").Value;

		projectGuid = LookupOrGenerateGuid ();
		fx_version = xproject.Element ("fx_version").Value;
		Csproj = new VsCsproj () {
			csProjFilename = this.CsprojFilename,
			projectGuid = this.projectGuid,
			library_output = this.LibraryOutput,
			fx_version = double.Parse (fx_version),
			library = this.library,
			MsbuildGenerator = this
		};

		if (dir == "mcs") {
			mcs_topdir = "../";
			class_dir = "../class/";
			base_dir = "../../mcs/mcs";
		} else {
			mcs_topdir = "../";

			foreach (char c in dir) {
				if (c == '/')
					mcs_topdir = "..//" + mcs_topdir;
			}
			class_dir = mcs_topdir.Substring (3);

			base_dir = Path.Combine ("..", "..", "mcs", dir);
		}
		AbsoluteLibraryOutput = Path.GetFullPath (Path.Combine (base_dir, LibraryOutput));
	}

	string LookupOrGenerateGuid ()
	{
		var projectFile = NativeName (CsprojFilename);
		if (File.Exists (projectFile)){
			var doc = XDocument.Load (projectFile);
			return doc.XPathSelectElement ("x:Project/x:PropertyGroup/x:ProjectGuid", xmlns).Value;
		}
		return "{" + Guid.NewGuid ().ToString ().ToUpper () + "}";
	}

	// Currently used
	bool Unsafe = false;
	StringBuilder defines = new StringBuilder ();
	bool Optimize = true;
	bool want_debugging_support = false;
	string main = null;
	Dictionary<string, string> embedded_resources = new Dictionary<string, string> ();
	List<string> warning_as_error = new List<string> ();
	List<int> ignore_warning = new List<int> ();
	bool load_default_config = true;
	bool StdLib = true;
	List<string> references = new List<string> ();
	List<string> libs = new List<string> ();
	List<string> reference_aliases = new List<string> ();
	bool showWarnings = true;

	// Currently unused
#pragma warning disable 0219, 0414
	int WarningLevel = 4;

	bool Checked = false;
	bool WarningsAreErrors;
	bool VerifyClsCompliance = true;
	string win32IconFile;
	string StrongNameKeyFile;
	bool copyLocal = true;
	Target Target = Target.Library;
	string TargetExt = ".exe";
	string OutputFile;
	string StrongNameKeyContainer;
	bool StrongNameDelaySign = false;
	string LangVersion = "default";
	string CodePage;

	// Class directory, relative to 
	string class_dir;
#pragma warning restore 0219,414

	readonly char [] argument_value_separator = new char [] { ';', ',' };

	//
	// This parses the -arg and /arg options to the compiler, even if the strings
	// in the following text use "/arg" on the strings.
	//
	bool CSCParseOption (string option, ref string [] args)
	{
		int idx = option.IndexOf (':');
		string arg, value;

		if (idx == -1) {
			arg = option;
			value = "";
		} else {
			arg = option.Substring (0, idx);

			value = option.Substring (idx + 1);
		}

		switch (arg.ToLower (CultureInfo.InvariantCulture)) {
		case "/nologo":
			return true;

		case "/t":
		case "/target":
			switch (value) {
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
			if (value.Length == 0) {
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
				if (value.Length == 0) {
					Usage ();
					Environment.Exit (1);
				}

				foreach (string d in value.Split (argument_value_separator)) {
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
			string [] s = value.Split (argument_value_separator);
			switch (s.Length) {
			case 1:
				if (s [0].Length == 0)
					goto default;
				embedded_resources [s [0]] = Path.GetFileName (s [0]);
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
				if (value.Length == 0) {
					Console.WriteLine ("-reference requires an argument");
					Environment.Exit (1);
				}

				string [] refs = value.Split (argument_value_separator);
				foreach (string r in refs) {
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
			main = value;
			return true;

		case "/m":
		case "/addmodule":
		case "/win32res":
		case "/doc": 
			if (showWarnings)
				Console.WriteLine ("{0} = not supported", arg);
			return true;
			
		case "/lib": {
				libs.Add (value);
				return true;
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

		case "/-runtime":
			// Console.WriteLine ("Warning ignoring /runtime:v4");
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

				if (value.Length == 0) {
					Console.WriteLine ("/nowarn requires an argument");
					Environment.Exit (1);
				}

				warns = value.Split (argument_value_separator);
				foreach (string wc in warns) {
					try {
						if (wc.Trim ().Length == 0)
							continue;

						int warn = Int32.Parse (wc);
						if (warn < 1) {
							throw new ArgumentOutOfRangeException ("warn");
						}
						ignore_warning.Add (warn);
					} catch {
						Console.WriteLine (String.Format ("`{0}' is not a valid warning number", wc));
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
		case "/delaysign":
			StrongNameDelaySign = true;
			return true;
		case "/delaysign-":
			StrongNameDelaySign = false;
			return true;

		case "/langversion":
			LangVersion = value;
			return true;

		case "/codepage":
			CodePage = value;
			return true;

		case "/publicsign":
			return true;

		case "/deterministic":
			return true;

		case "/runtimemetadataversion":
			return true;

		case "/-getresourcestrings":
			return true;

		case "/features":
			return true;
		}

		Console.WriteLine ("Failing with : {0}", arg);
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

		while ((line = f.ReadLine ()) != null) {
			int t = line.Length;

			for (int i = 0; i < t; i++) {
				char c = line [i];

				if (c == '"' || c == '\'') {
					char end = c;

					for (i++; i < t; i++) {
						c = line [i];

						if (c == end)
							break;
						sb.Append (c);
					}
				} else if (c == ' ') {
					if (sb.Length > 0) {
						args.Add (sb.ToString ());
						sb.Length = 0;
					}
				} else
					sb.Append (c);
			}
			if (sb.Length > 0) {
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
		var native = NativeName (f);

		if (File.Exists (native)) {
			using (var sr = new StreamReader (native)) {
				return sr.ReadToEnd ();
			}
		} else
			return "";
	}

	public static string NativeName (string path)
	{
		if (System.IO.Path.DirectorySeparatorChar == '/')
			return path.Replace ("\\", "/");
		else
			return path.Replace ("/", "\\");
	}

	public class VsCsproj {
		public string projectGuid;
		public string output;
		public string library_output;
		public string csProjFilename;
		public double fx_version;
		public List<VsCsproj> projReferences = new List<VsCsproj> ();
		public string library;
		public MsbuildGenerator MsbuildGenerator;
	}

	public VsCsproj Csproj;

	void AppendResource (StringBuilder resources, string source, string logical)
	{
		resources.AppendFormat ("    <EmbeddedResource Include=\"{0}\">" + NewLine, source);
		resources.AppendFormat ("      <LogicalName>{0}</LogicalName>" + NewLine, logical);
		resources.AppendFormat ("    </EmbeddedResource>" + NewLine);
	}
	
	public VsCsproj Generate (string library_output, Dictionary<string,MsbuildGenerator> projects, bool showWarnings = false)
	{
		var generatedProjFile = NativeName (Csproj.csProjFilename);
		//Console.WriteLine ("Generating: {0}", generatedProjFile);

		string boot, flags, output_name, built_sources, response, profile, reskey;

		boot = xproject.Element ("boot").Value;
		flags = xproject.Element ("flags").Value;
		output_name = xproject.Element ("output").Value;
		if (output_name.EndsWith (".exe"))
			Target = Target.Exe;
		built_sources = xproject.Element ("built_sources").Value;
		response = xproject.Element ("response").Value;
		reskey = xproject.Element ("resources").Value;

		profile = xproject.Element ("profile").Value;
		if (string.IsNullOrEmpty (response)) {
			// Address the issue where entries are missing the fx_version
			// Should be fixed in the Makefile or elsewhere; this is a workaround
			//<fx_version>basic</fx_version>
			//<profile>./../build/deps/mcs.exe.sources.response</profile>
			//<response></response>
			response = profile;
			profile = fx_version;
			if (response.Contains ("build") || response.Contains ("basic") || response.Contains (profile_2_0)) {
				fx_version = "2.0";
				if (response.Contains (profile_2_0)) profile = "net_2_0";
			} if (response.Contains ("build") || response.Contains ("basic") || response.Contains (profile_2_0)) {
				fx_version = "2.0";
			} else if (response.Contains (profile_3_5)) {
				fx_version = "3.5";
				profile = "net_3_5";
			} else if (response.Contains (profile_4_0)) {
				fx_version = "4.0";
				profile = "net_4_0";
			} else if (response.Contains (profile_4_x)) {
				fx_version = "4.5";
				profile = "net_4_x";
			}
		}
		//
		// Prebuild code, might be in inputs, check:
		//  inputs/LIBRARY.pre
		//
		string prebuild = GenerateStep (library, ".pre", "PreBuildEvent");
		string postbuild = GenerateStep (library, ".post", "PostBuildEvent");

		var all_args = new Queue<string []> ();
		all_args.Enqueue (flags.Split ());
		while (all_args.Count > 0) {
			string [] f = all_args.Dequeue ();

			for (int i = 0; i < f.Length; i++) {
				if (f [i].Length > 0 && f [i][0] == '-')
					f [i] = "/" + f [i].Substring (1);
				
				if (f [i] [0] == '@') {
					string [] extra_args;
					string response_file = f [i].Substring (1);

					var resp_file_full = Path.Combine (base_dir, response_file);
					extra_args = LoadArgs (resp_file_full);
					if (extra_args == null) {
						Console.WriteLine ($"{library_output}: Unable to open response file: {resp_file_full}");
						Environment.Exit (1);
					}

					all_args.Enqueue (extra_args);
					continue;
				}

				if (CSCParseOption (f [i], ref f))
					continue;
				Console.WriteLine ("{library_output}: Failure with {0}", f [i]);
				Environment.Exit (1);
			}
		}

		string [] source_files;
		using (var reader = new StreamReader (NativeName (base_dir + "\\" + response))) {
			source_files = reader.ReadToEnd ().Split ();
		}

		Array.Sort (source_files);

		StringBuilder sources = new StringBuilder ();
		foreach (string s in source_files) {
			if (s.Length == 0)
				continue;

			string src = s.Replace ("/", "\\");
			if (src.StartsWith (@"Test\..\"))
				src = src.Substring (8, src.Length - 8);

			sources.AppendFormat ("    <Compile Include=\"{0}\" />" + NewLine, src);
		}

		source_files = built_sources.Split ();
		Array.Sort (source_files);

		foreach (string s in source_files) {
			if (s.Length == 0)
				continue;

			string src = s.Replace ("/", "\\");
			if (src.StartsWith (@"Test\..\"))
				src = src.Substring (8, src.Length - 8);

			sources.AppendFormat ("    <Compile Include=\"{0}\" />" + NewLine, src);
		}
		if (sources.Length > 1)
			sources.Remove (sources.Length - 1, 1);

		//if (library == "corlib-build") // otherwise, does not compile on fx_version == 4.0
		//{
		//    references.Add("System.dll");
		//    references.Add("System.Xml.dll");
		//}

		//if (library == "System.Core-build") // otherwise, slow compile. May be a transient need.
		//{
		//    this.ignore_warning.Add(1685);
		//    this.ignore_warning.Add(0436);
		//}

		var refs = new StringBuilder ();

		if (response.Contains ("_test")) {
			refs.Append ($@"    <Reference Include=""..\lib\{profile}\nunitlite.dll"" />{NewLine}");
		}

		//
		// Generate resource referenced from the command line
		//
		var resources = new StringBuilder ();
		if (embedded_resources.Count > 0) {
			foreach (var dk in embedded_resources) {
				var source = dk.Key;
				if (source.EndsWith (".resources"))
					source = source.Replace (".resources", ".resx");
				
				// try to find a pre-built resource, and use that instead of trying to build it
				if (source.EndsWith (".resx")) {
					var probe_prebuilt = Path.Combine (base_dir, source.Replace (".resx", ".resources.prebuilt"));
					if (File.Exists (probe_prebuilt)) {
						
						source = GetRelativePath (base_dir + "/", probe_prebuilt);
					}
				}
				AppendResource (resources, source, dk.Value);
			}
		}
		//
		// Generate resources that were part of the explicit <resource> node
		//
		if (reskey != null && reskey != ""){
			var pairs = reskey.Split (' ', '\n', '\t');
			foreach (var pair in pairs){
				var p = pair.IndexOf (",");
				if (p == -1){
					Console.Error.WriteLine ($"Found a resource without a filename: {pairs} for {Csproj.csProjFilename}");
					Environment.Exit (1);
				}
				AppendResource (resources, pair.Substring (p+1), pair.Substring (0, p) + ".resources");
			}
		}
		if (resources.Length > 0){
			resources.Insert (0, "  <ItemGroup>" + NewLine);
			resources.AppendFormat ("  </ItemGroup>" + NewLine);
		}

		if (references.Count > 0 || reference_aliases.Count > 0) {
			// -r:mscorlib.dll -r:System.dll
			//<ProjectReference Include="..\corlib\corlib-basic.csproj">
			//  <Project>{155aef28-c81f-405d-9072-9d52780e3e70}</Project>
			//  <Name>corlib-basic</Name>
			//</ProjectReference>
			//<ProjectReference Include="..\System\System-basic.csproj">
			//  <Project>{2094e859-db2f-481f-9630-f89d31d9ed48}</Project>
			//  <Name>System-basic</Name>
			//</ProjectReference>
			var refdistinct = references.Distinct ();
			foreach (string reference in refdistinct) {
				
				var match = GetMatchingCsproj (library_output, reference, projects);
				if (match != null) {
					AddProjectReference (refs, Csproj, match, reference, null);
				} else {
					if (showWarnings){
						Console.WriteLine ("{0}: Could not find a matching project reference for {1}", library, Path.GetFileName (reference));
						Console.WriteLine ("  --> Adding reference with hintpath instead");
					}
					refs.Append ("    <Reference Include=\"" + reference + "\">" + NewLine);
					refs.Append ("      <SpecificVersion>False</SpecificVersion>" + NewLine);
					refs.Append ("      <HintPath>" + reference + "</HintPath>" + NewLine);
					refs.Append ("      <Private>False</Private>" + NewLine);
					refs.Append ("    </Reference>" + NewLine);
				}
			}

			foreach (string r in reference_aliases) {
				int index = r.IndexOf ('=');
				string alias = r.Substring (0, index);
				string assembly = r.Substring (index + 1);
				var match = GetMatchingCsproj (library_output, assembly, projects, explicitPath: true);
				if (match != null) {
					AddProjectReference (refs, Csproj, match, r, alias);
				} else {
					throw new NotSupportedException (string.Format ("From {0}, could not find a matching project reference for {1}", library, r));
					refs.Append ("    <Reference Include=\"" + assembly + "\">" + NewLine);
					refs.Append ("      <SpecificVersion>False</SpecificVersion>" + NewLine);
					refs.Append ("      <HintPath>" + r + "</HintPath>" + NewLine);
					refs.Append ("      <Aliases>" + alias + "</Aliases>" + NewLine);
					refs.Append ("    </Reference>" + NewLine);

				}
			}
		}

		// Possible inputs:
		// ../class/lib/build/tmp/System.Xml.dll  [No longer possible, we should be removing this from order.xml]
		//   /class/lib/basic/System.Core.dll
		// <library_output>mcs.exe</library_output>
		string build_output_dir;
		if (LibraryOutput.Contains ("/"))
			build_output_dir = Path.GetDirectoryName (LibraryOutput);
		else
			build_output_dir = "bin\\Debug\\" + library;

		if (build_output_dir.Contains ("-linux") || build_output_dir.Contains ("-darwin") || build_output_dir.Contains ("-win32"))
			build_output_dir = build_output_dir
				.Replace ("-linux", "-$(HostPlatform)")
				.Replace ("-darwin", "-$(HostPlatform)")
				.Replace ("-win32", "-$(HostPlatform)");

		bool basic_or_build = (library.Contains ("-basic") || library.Contains ("-build"));

		//
		// Replace the template values
		//

		string strongNameSection = "";
		if (StrongNameKeyFile != null){
			strongNameSection = String.Format (
				"    <SignAssembly>true</SignAssembly>" + NewLine +
				"{1}" +
				"    <AssemblyOriginatorKeyFile>{0}</AssemblyOriginatorKeyFile>",
				StrongNameKeyFile, StrongNameDelaySign ? "    <DelaySign>true</DelaySign>" + NewLine : "");
		}
		Csproj.output = template.
			Replace ("@OUTPUTTYPE@", Target == Target.Library ? "Library" : "Exe").
			Replace ("@SIGNATURE@", strongNameSection).
			Replace ("@PROJECTGUID@", Csproj.projectGuid).
			Replace ("@DEFINES@", defines.ToString ()).
			Replace ("@DISABLEDWARNINGS@", string.Join (",", (from i in ignore_warning select i.ToString ()).ToArray ())).
			Replace ("@LANGVERSION@", LangVersion).
			//Replace("@NOSTDLIB@", (basic_or_build || (!StdLib)) ? "<NoStdLib>true</NoStdLib>" : string.Empty).
			Replace ("@NOSTDLIB@", "<NoStdLib>" + (!StdLib).ToString () + "</NoStdLib>").
			Replace ("@NOCONFIG@", "<NoConfig>" + (!load_default_config).ToString () + "</NoConfig>").
			Replace ("@ALLOWUNSAFE@", Unsafe ? "<AllowUnsafeBlocks>true</AllowUnsafeBlocks>" : "").
			Replace ("@FX_VERSION", fx_version).
			Replace ("@ASSEMBLYNAME@", Path.GetFileNameWithoutExtension (output_name)).
			Replace ("@OUTPUTDIR@", build_output_dir).
			Replace ("@OUTPUTSUFFIX@", Path.GetFileName (build_output_dir)).
			Replace ("@DEFINECONSTANTS@", defines.ToString ()).
			Replace ("@DEBUG@", want_debugging_support ? "true" : "false").
			Replace ("@DEBUGTYPE@", want_debugging_support ? "full" : "pdbonly").
			Replace ("@REFERENCES@", refs.ToString ()).
			Replace ("@PREBUILD@", prebuild).
			Replace ("@POSTBUILD@", postbuild).
			Replace ("@STARTUPOBJECT@", main == null ? "" : $"<StartupObject>{main}</StartupObject>").
			//Replace ("@ADDITIONALLIBPATHS@", String.Format ("<AdditionalLibPaths>{0}</AdditionalLibPaths>", string.Join (",", libs.ToArray ()))).
			Replace ("@ADDITIONALLIBPATHS@", String.Empty).
			Replace ("@RESOURCES@", resources.ToString ()).
			Replace ("@OPTIMIZE@", Optimize ? "true" : "false").
			Replace ("@SOURCES@", sources.ToString ());

		//Console.WriteLine ("Generated {0}", ofile.Replace ("\\", "/"));
		using (var o = new StreamWriter (generatedProjFile)) {
			o.WriteLine (Csproj.output);
		}

		return Csproj;
	}

	string GenerateStep (string library, string suffix, string eventKey)
	{
		string target = Load (library + suffix);
		string target_windows, target_unix;

		int q = library.IndexOf ("-");
		if (q != -1)
			target = target + Load (library.Substring (0, q) + suffix);

		if (target.IndexOf ("@MONO@") != -1){
			target_unix = target.Replace ("@MONO@", "mono").Replace ("@CAT@", "cat");
			target_windows = target.Replace ("@MONO@", "").Replace ("@CAT@", "type");
		} else {
			target_unix = target.Replace ("jay.exe", "jay");
			target_windows = target;
		}
		target_unix = target_unix.Replace ("@COPY@", "cp");
		target_windows = target_unix.Replace ("@COPY@", "copy");

		target_unix = target_unix.Replace ("\r", "");
		const string condition_unix    = "Condition=\" '$(OS)' != 'Windows_NT' \"";
		const string condition_windows = "Condition=\" '$(OS)' == 'Windows_NT' \"";
		var result =
			$"    <{eventKey} {condition_unix}>\n{target_unix}\n    </{eventKey}>{NewLine}" +
			$"    <{eventKey} {condition_windows}>{NewLine}{target_windows}{NewLine}    </{eventKey}>";
		return result;
	}
	
	void AddProjectReference (StringBuilder refs, VsCsproj result, MsbuildGenerator match, string r, string alias)
	{
		refs.AppendFormat ("    <ProjectReference Include=\"{0}\">{1}", GetRelativePath (result.csProjFilename, match.CsprojFilename), NewLine);
		refs.Append ("      <Project>" + match.projectGuid + "</Project>" + NewLine);
		refs.Append ("      <Name>" + Path.GetFileNameWithoutExtension (match.CsprojFilename.Replace ('\\', Path.DirectorySeparatorChar)) + "</Name>" + NewLine);
		if (alias != null)
			refs.Append ("      <Aliases>" + alias + "</Aliases>");
		refs.Append ("    </ProjectReference>" + NewLine);
		if (!result.projReferences.Contains (match.Csproj))
			result.projReferences.Add (match.Csproj);
	}

	public static string GetRelativePath (string from, string to)
	{
		from = from.Replace ("\\", "/");
		to = to.Replace ("\\", "/");
		var fromUri = new Uri (Path.GetFullPath (from));
		var toUri = new Uri (Path.GetFullPath (to));

		var ret =  fromUri.MakeRelativeUri (toUri).ToString ().Replace ("%5C", "\x5c");
		return ret;
	}

	MsbuildGenerator GetMatchingCsproj (string library_output, string dllReferenceName, Dictionary<string,MsbuildGenerator> projects, bool explicitPath = false)
	{
		// libDir would be "./../../class/lib/net_4_x for example
		// project 
		if (!dllReferenceName.EndsWith (".dll") && !dllReferenceName.EndsWith (".exe"))
			dllReferenceName += ".dll";

		var probe = Path.GetFullPath (Path.Combine (base_dir, dllReferenceName));
		foreach (var project in projects){
			if (probe == project.Value.AbsoluteLibraryOutput)
				return project.Value;
		}

		// not explicit, search for the library in the lib path order specified

		foreach (var libDir in libs) {
			var abs = Path.GetFullPath (Path.Combine (base_dir, libDir));
			foreach (var project in projects){
				probe = Path.Combine (abs, dllReferenceName);

				if (probe == project.Value.AbsoluteLibraryOutput)
					return project.Value;
			}
		}

		// Last attempt, try to find the library in all the projects
		foreach (var project in projects) {
			if (project.Value.AbsoluteLibraryOutput.EndsWith (dllReferenceName))
				return project.Value;

		}
		var ljoined = String.Join (", ", libs);
		Console.WriteLine ($"{library_output}: did not find referenced {dllReferenceName} with libs={ljoined}");
		foreach (var p in projects) {
			Console.WriteLine ("{0}", p.Value.AbsoluteLibraryOutput);
		}
		return null;
	}

}

public class Driver {

	static IEnumerable<XElement> GetProjects (bool withTests = false)
	{
		XDocument doc = XDocument.Load ("order.xml");
		foreach (XElement project in doc.Root.Elements ()) {
			string dir = project.Attribute ("dir").Value;
			string library = project.Attribute ("library").Value;
			var profile = project.Element ("profile").Value;

#if false
			// Skip facades for now, the tool doesn't know how to deal with them yet.
			if (dir.Contains ("Facades"))
				continue;

			// These are currently broken, skip until they're fixed.
			if (dir.StartsWith ("mcs") || dir.Contains ("apigen"))
				continue;

			//
			// Do only class libraries for now
			//
			if (!(dir.StartsWith ("class") || dir.StartsWith ("mcs") || dir.StartsWith ("basic")))
				continue;
#endif
			//
			// Do not do 2.1, it is not working yet
			// Do not do basic, as there is no point (requires a system mcs to be installed).
			//
			if (library.Contains ("moonlight") || library.Contains ("-basic") || library.EndsWith ("bootstrap")  || library.Contains ("build"))
				continue;

			// The next ones are to make debugging easier for now
			if (profile == "basic")
				continue;

			// For now -- problem is, our resolver currently only considers the assembly name, and we ahve
			// conflicing 2.0 and 2.4 versions so for now, we just skip the nunit20 versions
			if (dir.Contains ("nunit20"))
				continue;
			
			if (library.Contains ("tests") && !withTests)
				continue;

			if (profile != "net_4_x")
				continue;

			yield return project;
		}
	}

	static void Main (string [] args)
	{
		if (!File.Exists ("genproj.cs")) {
			Console.WriteLine ("This command must be executed from mono/msvc/scripts");
			Environment.Exit (1);
		}

		if (args.Length == 1 && args [0].ToLower ().Contains ("-h")) {
			Console.WriteLine ("Usage:");
			Console.WriteLine ("genproj.exe [visual_studio_release] [output_full_solutions] [with_tests]");
			Console.WriteLine ("If output_full_solutions is false, only the main System*.dll");
			Console.WriteLine (" assemblies (and dependencies) is included in the solution.");
			Console.WriteLine ("Example:");
			Console.WriteLine ("genproj.exe 2012 false false");
			Console.WriteLine ("genproj.exe with no arguments is equivalent to 'genproj.exe 2012 true false'\n\n");
			Console.WriteLine ("genproj.exe deps");
			Console.WriteLine ("Generates a Makefile dependency file from the projects input");
			Environment.Exit (0);
		}

		var slnVersion = (args.Length > 0) ? args [0] : "2012";
		bool fullSolutions = (args.Length > 1) ? bool.Parse (args [1]) : true;
		bool withTests = (args.Length > 2) ? bool.Parse (args [2]) : false;

		// To generate makefile depenedencies
		var makefileDeps =  (args.Length > 0 && args [0] == "deps");

		var sln_gen = new SlnGenerator (slnVersion);
		var four_five_sln_gen = new SlnGenerator (slnVersion);
		var projects = new Dictionary<string,MsbuildGenerator> ();

		var duplicates = new List<string> ();
		foreach (var project in GetProjects (withTests)) {
			var library_output = project.Element ("library_output").Value;
			projects [library_output] = new MsbuildGenerator (project);
		}
		foreach (var project in GetProjects (withTests)){
			var library_output = project.Element ("library_output").Value;
			//Console.WriteLine ("=== {0} ===", library_output);
			var gen = projects [library_output];
			try {
				var csproj = gen.Generate (library_output, projects);
				var csprojFilename = csproj.csProjFilename;
				if (!sln_gen.ContainsProjectIdentifier (csproj.library)) {
					sln_gen.Add (csproj);
				} else {
					duplicates.Add (csprojFilename);
				}
				
			} catch (Exception e) {
				Console.WriteLine ("Error in {0}\n{1}", project, e);
			}
		}

		Func<MsbuildGenerator.VsCsproj, bool> additionalFilter;
		additionalFilter = fullSolutions ? (Func<MsbuildGenerator.VsCsproj, bool>)null : IsCommonLibrary;

		FillSolution (four_five_sln_gen, MsbuildGenerator.profile_4_x, projects.Values, additionalFilter);

		if (duplicates.Count () > 0) {
			var sb = new StringBuilder ();
			sb.AppendLine ("WARNING: Skipped some project references, apparent duplicates in order.xml:");
			foreach (var item in duplicates) {
				sb.AppendLine (item);
			}
			Console.WriteLine (sb.ToString ());
		}

		WriteSolution (four_five_sln_gen, Path.Combine ("..", "..", MakeSolutionName (MsbuildGenerator.profile_4_x)));

		if (makefileDeps){
			const string classDirPrefix = "./../../";
			Console.WriteLine ("here {0}", sln_gen.libraries.Count);
			foreach (var p in sln_gen.libraries){
				string rebasedOutput = RebaseToClassDirectory (MsbuildGenerator.GetRelativePath ("../../mcs/class", p.library_output));
				
				Console.Write ("{0}: ", rebasedOutput);
				foreach (var r in p.projReferences){
					var lo = r.library_output;
					if (lo.StartsWith (classDirPrefix))
						lo = lo.Substring (classDirPrefix.Length);
					else
						lo = "<<ERROR-dependency is not a class library>>";
					Console.Write ("{0} ", lo);
				}
				Console.Write ("\n\t(cd {0}; make {1})", p.MsbuildGenerator.dir, p.library_output);
				Console.WriteLine ("\n");
			}
		}
		
		// A few other optional solutions
		// Solutions with 'everything' and the most common libraries used in development may be of interest
		//WriteSolution (sln_gen, "./mcs_full.sln");
		//WriteSolution (small_full_sln_gen, "small_full.sln");
		// The following may be useful if lacking visual studio or MonoDevelop, to bootstrap mono compiler self-hosting
		//WriteSolution (basic_sln_gen, "mcs_basic.sln");
		//WriteSolution (build_sln_gen, "mcs_build.sln");
	}

	// Rebases a path, assuming that execution is taking place in the "class" subdirectory,
	// so it strips ../class/ from a path, which is a no-op
	static string RebaseToClassDirectory (string path)
	{
		const string prefix = "../class/";
		int p = path.IndexOf (prefix);
		if (p == -1)
			return path;
		return path.Substring (0, p) + path.Substring (p+prefix.Length);
		return path;
	}
	
	static string MakeSolutionName (string profileTag)
	{
		return "net" + profileTag + ".sln";
	}

	static void FillSolution (SlnGenerator solution, string profileString, IEnumerable<MsbuildGenerator> projects, Func<MsbuildGenerator.VsCsproj, bool> additionalFilter = null)
	{
		foreach (var generator in projects) {
			var vsCsproj = generator.Csproj;
			if (!vsCsproj.library.Contains (profileString))
				continue;
			if (additionalFilter != null && !additionalFilter (vsCsproj))
				continue;
			var csprojFilename = vsCsproj.csProjFilename;
			if (!solution.ContainsProjectIdentifier (vsCsproj.library)) {
				solution.Add (vsCsproj);
				RecursiveAddProj (solution, vsCsproj);
			}
		}
	}

	static void RecursiveAddProj (SlnGenerator solution, MsbuildGenerator.VsCsproj vsCsproj, int recursiveDepth = 1)
	{
		const int max_recursive = 16;
		if (recursiveDepth > max_recursive) throw new Exception (string.Format ("Reached {0} levels of project dependency", max_recursive));
		foreach (var projRef in vsCsproj.projReferences) {
			if (!solution.ContainsProjectIdentifier (projRef.library)) {
				solution.Add (projRef);
				RecursiveAddProj (solution, projRef, recursiveDepth + 1);
			}
		}
	}

	static void WriteSolution (SlnGenerator sln_gen, string slnfilename)
	{
		Console.WriteLine (String.Format ("Writing solution {1}, with {0} projects", sln_gen.Count, slnfilename));
		sln_gen.Write (slnfilename);
	}

	static bool IsCommonLibrary (MsbuildGenerator.VsCsproj proj)
	{
		var library = proj.library;
		//if (library.Contains ("-basic"))
		//	return true;
		//if (library.Contains ("-build"))
		//	return true;
		//if (library.StartsWith ("corlib"))
		//	return true;
		if (library.StartsWith ("System-"))
			return true;
		if (library.StartsWith ("System.Xml"))
			return true;
		if (library.StartsWith ("System.Secu"))
			return true;
		if (library.StartsWith ("System.Configuration"))
			return true;
		if (library.StartsWith ("System.Core"))
			return true;
		//if (library.StartsWith ("Mono."))
		//	return true;

		return false;
	}
}
