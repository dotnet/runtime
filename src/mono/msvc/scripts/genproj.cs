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

public static class KnownProject {
	public static readonly KnownProjectInfo
		Genconsts = new KnownProjectInfo {
			Name = "genconsts",
			Path = @"$(SolutionDir)\msvc\scripts\genconsts.csproj",
			Guid = "{702AE2C0-71DD-4112-9A06-E4FABCA59986}"
		},
		Stringreplacer = new KnownProjectInfo {
			Name = "cil-stringreplacer",
			Path = @"$(SolutionDir)\mcs\tools\cil-stringreplacer\cil-stringreplacer.csproj",
			Guid = "{53c50ffa-8b39-4c70-8ba8-caa70c41a47b}"
		},
		Jay = new KnownProjectInfo {
			Name = "jay",
			Path = @"$(SolutionDir)\mcs\jay\jay.vcxproj",
			Guid = "{5d485d32-3b9f-4287-ab24-c8da5b89f537}"
		},
		Culevel = new KnownProjectInfo {
			Name = "culevel",
			Path = @"$(SolutionDir)\mcs\tools\culevel\culevel.csproj",
			Guid = "{E8E246BD-CD0C-4734-A3C2-7F44796EC47B}"
		};		
}

public class KnownProjectInfo {
	public string Path;
	public string Guid;
	public string Name;
}

public class SlnGenerator {
	public static readonly string NewLine = Environment.NewLine;
	public SlnGenerator (string slnVersion)
	{
		Console.Error.WriteLine("// Requested sln version is {0}", slnVersion);
		this.header = MakeHeader ("12.00", "15", "15.0.0.0");
	}

	const string project_start = "Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\""; // Note: No need to double up on {} around {2}
	const string project_end = "EndProject";

	public static readonly List<string> profiles = new List<string> {
		"net_4_x",
		"monodroid",
		"monotouch",
		"monotouch_tv",
		"monotouch_watch",
		"orbis",
		"unreal",
		"wasm",
		"winaot",
		"xammac",
	};

	public static readonly HashSet<string> observedProfiles = new HashSet<string> {
		"net_4_x"
	};

	public const string csproj_type_guid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
	public const string vcxproj_type_guid = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";

	public static Dictionary<string, HashSet<string>> profilesByGuid = new Dictionary<string, HashSet<string>> ();
	public List<MsbuildGenerator.VsCsproj> libraries = new List<MsbuildGenerator.VsCsproj> ();
	string header;

	string MakeHeader (string formatVersion, string yearTag, string minimumVersion)
	{
		return string.Format (
			"Microsoft Visual Studio Solution File, Format Version {0}" + NewLine + 
			"# Visual Studio {1}" + NewLine + 
			"MinimumVisualStudioVersion = {2}", 
			formatVersion, yearTag,
			minimumVersion
		);
	}

	public void Add (MsbuildGenerator.VsCsproj vsproj)
	{
		try {
			libraries.Add (vsproj);
		} catch (Exception ex) {
			Console.Error.WriteLine ($"// Error while adding library: {ex.Message}");
		}
	}

	private void WriteProjectReference (StreamWriter sln, string prefixGuid, string library, string relativePath, string projectGuid, string[] dependencyGuids)
	{
		// HACK
		library = library.Replace("-net_4_x", "");
		sln.WriteLine (project_start, prefixGuid, library, relativePath, projectGuid);

		if (dependencyGuids != null && dependencyGuids.Length > 0) {
			sln.WriteLine ("\tProjectSection(ProjectDependencies) = postProject");
			foreach (var guid in dependencyGuids)
	    		sln.WriteLine ("\t\t{0} = {0}", guid);
			sln.WriteLine ("\tEndProjectSection");
		}

		sln.WriteLine (project_end);
	}

	private void WriteProjectReference (StreamWriter sln, string slnFullPath, MsbuildGenerator.VsCsproj proj)
	{
		var unixProjFile = proj.csProjFilename.Replace ("\\", "/");
		var fullProjPath = Path.GetFullPath (unixProjFile).Replace ("\\", "/");
		var relativePath = MsbuildGenerator.GetRelativePath (slnFullPath, fullProjPath);
		string[] dependencyGuids = null;

		WriteProjectReference(sln, csproj_type_guid, proj.library, relativePath, proj.projectGuid, dependencyGuids);
	}

	private void WriteProjectConfigurationPlatforms (StreamWriter sln, string guid, string defaultPlatform, bool forceBuild)
	{
		var fallbackProfileNames = new List<string> ();
		var didBuildAnyProfile = false;

		foreach (var profile in profiles) {
			if (!observedProfiles.Contains (profile) && !forceBuild)
				continue;

			var platformToBuild = profile;
			var isBuildEnabled = true;

			HashSet<string> projectProfiles;
			if (
				!profilesByGuid.TryGetValue (guid, out projectProfiles) ||
				!projectProfiles.Contains (platformToBuild)
			) {
				fallbackProfileNames.Add (platformToBuild);
				platformToBuild = defaultPlatform;
				isBuildEnabled = forceBuild;
			}

			if (isBuildEnabled)
				didBuildAnyProfile = true;

			sln.WriteLine ("\t\t{0}.Debug|{1}.ActiveCfg = Debug|{2}", guid, profile, platformToBuild);
			if (isBuildEnabled)
				sln.WriteLine ("\t\t{0}.Debug|{1}.Build.0 = Debug|{2}", guid, profile, platformToBuild);
			sln.WriteLine ("\t\t{0}.Release|{1}.ActiveCfg = Release|{2}", guid, profile, platformToBuild);
			if (isBuildEnabled)
				sln.WriteLine ("\t\t{0}.Release|{1}.Build.0 = Release|{2}", guid, profile, platformToBuild);
		}

		if (!didBuildAnyProfile)
			Console.Error.WriteLine($"// Project {guid} not set to build in any profile");

		if (fallbackProfileNames.Count > 0)
			Console.Error.WriteLine ($"// Project {guid} does not have profile(s) {string.Join(", ", fallbackProfileNames)} so using {defaultPlatform}");
	}

	public void Write (string filename)
	{
		var fullPath = Path.GetDirectoryName (filename) + "/";
		
		using (var sln = new StreamWriter (filename)) {
			sln.WriteLine ();
			sln.WriteLine (header);

			// Manually insert jay's vcxproj. We depend on jay.exe to perform build steps later.
			WriteProjectReference (sln, vcxproj_type_guid, "jay", "mcs/jay/jay.vcxproj", KnownProject.Jay.Guid, null);

			// Manually insert genconsts. This is used to generate Consts.cs.
			WriteProjectReference (sln, csproj_type_guid, "genconsts", "msvc/scripts/genconsts.csproj", KnownProject.Genconsts.Guid, null);

			// Manually insert cil-stringreplacer. We can't trivially do this through the order.xml flow and it has a custom csproj.
			WriteProjectReference (sln, csproj_type_guid, "cil-stringreplacer", "mcs/tools/cil-stringreplacer/cil-stringreplacer.csproj", KnownProject.Stringreplacer.Guid, null);

			foreach (var proj in libraries) {
				WriteProjectReference (sln, fullPath, proj);
			}

			sln.WriteLine ("Global");

			sln.WriteLine ("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
			foreach (var profile in profiles) {
				if (!observedProfiles.Contains (profile))
					continue;

				sln.WriteLine ("\t\tDebug|{0} = Debug|{0}", profile, profile);
				sln.WriteLine ("\t\tRelease|{0} = Release|{0}", profile, profile);
			}
			sln.WriteLine ("\tEndGlobalSection");

			sln.WriteLine ("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

			// Manually insert configurations for the special projects that always build
			WriteProjectConfigurationPlatforms (sln, KnownProject.Jay.Guid, "Win32", true);
			WriteProjectConfigurationPlatforms (sln, KnownProject.Genconsts.Guid, "x86", true);
			WriteProjectConfigurationPlatforms (sln, KnownProject.Stringreplacer.Guid, "AnyCPU", true);

			foreach (var proj in libraries) {
				WriteProjectConfigurationPlatforms (sln, proj.projectGuid, "net_4_x", false);
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

public class MsbuildGenerator {
	static readonly string NewLine = SlnGenerator.NewLine;
	static XmlNamespaceManager xmlns;

	public const string profile_2_0 = "_2_0";
	public const string profile_3_5 = "_3_5";
	public const string profile_4_0 = "_4_0";
	public const string profile_4_x = "_4_x";

	public static readonly (string, string)[] fixed_guids = new [] {
		("tools/culevel/culevel.csproj", KnownProject.Culevel.Guid)
	};

	static void Usage ()
	{
		Console.Error.WriteLine ("// Invalid argument");
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

	static readonly Dictionary<string, string> GuidForCsprojCache = new Dictionary<string, string> ();

	public string LibraryOutput, AbsoluteLibraryOutput;

	public MsbuildGenerator (XElement xproject)
	{
		this.xproject = xproject;
		dir = xproject.Attribute ("dir").Value;
		library = xproject.Attribute ("library").Value;
		// HACK: 
		var profileIndex = library.LastIndexOf("-");
		var libraryWithoutProfile = library.Substring(0, profileIndex);
		CsprojFilename = "..\\..\\mcs\\" + dir + "\\" + libraryWithoutProfile + ".csproj";
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
		string guidKey = Path.GetFullPath (projectFile);

		foreach (var fg in fixed_guids) {
			if (guidKey.EndsWith (fg.Item1)) {
				Console.WriteLine($"Using fixed guid {fg.Item2} for {fg.Item1}");
				return fg.Item2;
			}
		}

		string result;
		GuidForCsprojCache.TryGetValue (projectFile, out result);

		if (String.IsNullOrEmpty(result) && File.Exists (projectFile)){
			try {
				var doc = XDocument.Load (projectFile);
				result = doc.XPathSelectElement ("x:Project/x:PropertyGroup/x:ProjectGuid", xmlns).Value;
			} catch (Exception exc) {
				Console.Error.WriteLine($"// Failed to parse guid from {projectFile}: {exc.Message}");
			}
		}

		if (String.IsNullOrEmpty(result))
			result = "{" + Guid.NewGuid ().ToString ().ToUpper () + "}";

		GuidForCsprojCache[projectFile] = result;
		return result;
	}

	// Currently used
	bool Unsafe = false;
	StringBuilder defines = new StringBuilder ();
	bool Optimize = true;
	bool want_debugging_support = false;
	string main = null;
	SortedDictionary<string, string> embedded_resources = new SortedDictionary<string, string> ();
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
				Console.Error.WriteLine ("// Does not support this method yet: {0}", arg);
				Environment.Exit (1);
				break;
			default:
				Console.Error.WriteLine ("// Wrong number of arguments for option `{0}'", option);
				Environment.Exit (1);
				break;
			}

			return true;

		case "/recurse":
			Console.Error.WriteLine ("// /recurse not supported");
			Environment.Exit (1);
			return true;

		case "/r":
		case "/reference": {
				if (value.Length == 0) {
					Console.Error.WriteLine ("// /reference requires an argument");
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
				Console.Error.WriteLine ("// {0} = not supported", arg);
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
					Console.Error.WriteLine ("// /nowarn requires an argument");
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
						Console.Error.WriteLine ($"// `{wc}' is not a valid warning number");
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
				Console.Error.WriteLine ($"// {arg} requires an argument");
				Environment.Exit (1);
			}
			StrongNameKeyFile = value;
			return true;
		case "/keycontainer":
			if (value == String.Empty) {
				Console.Error.WriteLine ($"// {arg} requires an argument");
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

		case "/sourcelink":
			return true;

		case "/shared":
			return true;
		}

		Console.Error.WriteLine ($"// Failing with : {arg}");
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
		public string preBuildEvent, postBuildEvent;
	}

	public VsCsproj Csproj;

	void AppendResource (StringBuilder resources, string source, string logical)
	{
		source = NativeName (source);
		resources.AppendFormat ("    <EmbeddedResource Include=\"{0}\">" + NewLine, source);
		resources.AppendFormat ("      <LogicalName>{0}</LogicalName>" + NewLine, logical);
		resources.AppendFormat ("    </EmbeddedResource>" + NewLine);
	}

	internal string GetProjectFilename () 
	{
		return NativeName (Csproj.csProjFilename);
	}

	public void EraseExisting () 
	{
		var generatedProjFile = GetProjectFilename();
		if (File.Exists(generatedProjFile))
			File.Delete(generatedProjFile);
	}

	SourcesParser _SourcesParser = null;

	private SourcesParser GetSourcesParser () {
		if (_SourcesParser != null)
			return _SourcesParser;

		var platformsFolder = Path.GetFullPath ("../../mcs/build/platforms");
		var profilesFolder = Path.GetFullPath ("../../mcs/build/profiles");

		SourcesParser.TraceLevel = 0;
		return _SourcesParser = new SourcesParser (platformsFolder, profilesFolder, null);
	}

	private ParseResult ReadSources (string sourcesFileName) {
		var libraryDirectory = Path.GetDirectoryName (GetProjectFilename ());

		// HACK: Sometimes the sources path contains a relative path like ../../x
		if (sourcesFileName.Contains ("/") || sourcesFileName.Contains ("\\")) {
			libraryDirectory = Path.Combine (libraryDirectory, Path.GetDirectoryName (sourcesFileName));
			sourcesFileName = Path.GetFileName (sourcesFileName);
		}

		libraryDirectory = Path.GetFullPath (libraryDirectory);

		// HACK: executable.make generates sources paths containing .sources already
		var libraryName = sourcesFileName.Replace (".sources", "");

		var parser = GetSourcesParser ();
		var result = parser.Parse (libraryDirectory, libraryName);

		if (result.SourcesFiles.Count == 0)
			Console.Error.WriteLine ($"// No sources files found for '{sourcesFileName}', looked in '{libraryDirectory}' for {libraryName}");

		return result;
	}

	private string FixupSourceName (string s) {
		string src = s.Replace ("/", "\\");
		if (src.StartsWith (@"Test\..\"))
			src = src.Substring (8, src.Length - 8);

		return src;
	}

	private bool IsValidProfile (string output_name, string profile) {
		return SlnGenerator.profiles.Contains (profile);
	}

	private void GenerateSourceItems (XmlWriter writer, IEnumerable<string> fileNames, HashSet<string> commonFiles) {
		foreach (var file in fileNames.OrderBy (f => f, StringComparer.Ordinal)) {
			// FIXME: Is this needed?
			if ((commonFiles != null) && commonFiles.Contains (file))
				continue;

			writer.WriteStartElement ("Compile");
			writer.WriteAttributeString ("Include", file);
			writer.WriteEndElement ();
		}	
	}

	private void GenerateProjectDependency (XmlWriter xmlWriter, KnownProjectInfo project) {
		xmlWriter.WriteStartElement ("ProjectReference");
		xmlWriter.WriteAttributeString ("Include", project.Path);
		xmlWriter.WriteElementString ("Name", project.Name);
		xmlWriter.WriteElementString ("Project", project.Guid);
		xmlWriter.WriteElementString ("ReferenceOutputAssembly", "false");
		xmlWriter.WriteElementString ("CopyToOutputDirectory", "Never");
		xmlWriter.WriteElementString ("Private", "false");
		xmlWriter.WriteEndElement();
	}

	private void GenerateProjectDependencies (
		XmlWriter writer,
		HashSet<string> commonFiles,
		string prebuild, string postbuild
	) {
		var prebuild_postbuild = (prebuild + Environment.NewLine + postbuild)
			.Replace ("\\", "/");

		if (commonFiles.Any (f => f.EndsWith("build\\common\\Consts.cs")))
			GenerateProjectDependency (writer, KnownProject.Genconsts);

		if (prebuild_postbuild.Contains ("jay.exe"))
			GenerateProjectDependency (writer, KnownProject.Jay);

		if (prebuild_postbuild.Contains ("culevel.exe"))
			GenerateProjectDependency (writer, KnownProject.Culevel);

		if (prebuild_postbuild.Contains ("cil-stringreplacer.exe"))
			GenerateProjectDependency (writer, KnownProject.Stringreplacer);
	}

	private StringBuilder GenerateSourceItemGroups (
		string output_name, 
		string profile,
		string sources_file_name,
		string groupConditional,
		string prebuild, string postbuild
	) {
		var result = new StringBuilder ();
		var xmlWriterSettings = new XmlWriterSettings () {
			ConformanceLevel = ConformanceLevel.Fragment,
			WriteEndDocumentOnClose = true,
			CheckCharacters = true,
			Encoding = Encoding.UTF8,
			Indent = true,
			IndentChars = "  ",
			NewLineChars = NewLine,
			NewLineHandling = NewLineHandling.Replace,
			NewLineOnAttributes = false,
			OmitXmlDeclaration = true
		};
		var xmlWriter = XmlWriter.Create (result, xmlWriterSettings);
		var parseResult = ReadSources (sources_file_name);

		var hostPlatformNames = GetSourcesParser ().AllHostPlatformNames;

		var nullExclusions = new SourcesFile ("null", true);

		if (parseResult.TargetDictionary.Count == 0)
			return result;

		var targetFileSets = (from target in parseResult.Targets
			where (target.Key.profile == null) || IsValidProfile (output_name, target.Key.profile)
			let matches = parseResult.GetMatches (target)
				.Select (m => FixupSourceName (m.RelativePath))
				.OrderBy (s => s, StringComparer.Ordinal)
				.Distinct ()
			let fileNames = new HashSet<string> (matches)
			orderby target.Key.profile descending, target.Key.hostPlatform descending
			select (key: target.Key, fileNames: fileNames)).ToList ();

		var commonFiles = targetFileSets.Aggregate (
			(HashSet<string>)null,
			(files, targetSet) => {
				if (files == null)
					files = new HashSet<string> (targetSet.fileNames, StringComparer.Ordinal);
				else
					files.IntersectWith (targetSet.fileNames);
				return files;
			}
		);

		xmlWriter.WriteComment ("Common files");
		xmlWriter.WriteStartElement ("ItemGroup");
		GenerateSourceItems (xmlWriter, commonFiles, null);

		GenerateProjectDependencies (xmlWriter, commonFiles, prebuild, postbuild);

  		xmlWriter.WriteEndElement ();
  		xmlWriter.WriteComment ("End of common files");

  		// FIXME: Is this right if the profile/platform pair are not null,null? It probably is
  		if (targetFileSets.Count != 1) {
			var profileGroups = (from tfs in targetFileSets 
				group tfs by tfs.key.profile into sets
				select sets).ToList ();

			xmlWriter.WriteComment ("Per-profile files");
			if (profileGroups.Count > 1)
				xmlWriter.WriteStartElement ("Choose");

			foreach (var profileGroup in profileGroups) {
				if (profileGroups.Count == 1) {
				} else if (profileGroup.Key == null) {
					xmlWriter.WriteStartElement ("Otherwise");
				} else {
					xmlWriter.WriteStartElement ("When");
					xmlWriter.WriteAttributeString ("Condition", $"'$(Platform)' == '{profileGroup.Key}'");
				}

				var hostPlatforms = profileGroup.ToList ();
				if (hostPlatforms.Count == 1) {
					xmlWriter.WriteStartElement ("ItemGroup");
					GenerateSourceItems (xmlWriter, hostPlatforms[0].fileNames, commonFiles);
					xmlWriter.WriteEndElement ();
				} else {
					xmlWriter.WriteComment ("Per-host-platform files");
					xmlWriter.WriteStartElement ("Choose");

					foreach (var set in hostPlatforms) {
						if (set.key.hostPlatform == null) {
							xmlWriter.WriteStartElement ("Otherwise");
						} else {
							xmlWriter.WriteStartElement ("When");
							xmlWriter.WriteAttributeString ("Condition", $"'$(HostPlatform)' == '{set.key.hostPlatform}'");
						}

						xmlWriter.WriteStartElement ("ItemGroup");
						GenerateSourceItems (xmlWriter, set.fileNames, commonFiles);
						xmlWriter.WriteEndElement ();

						xmlWriter.WriteEndElement();
					}

					xmlWriter.WriteEndElement ();
					xmlWriter.WriteComment ("End of per-host-platform files");
				}

				if (profileGroups.Count > 1)
					xmlWriter.WriteEndElement ();
			}

			if (profileGroups.Count > 1)
				xmlWriter.WriteEndElement ();
			xmlWriter.WriteComment ("End of per-profile files");
  		}

		xmlWriter.Close ();

		return result;
	}
	
	public VsCsproj Generate (string library_output, Dictionary<string,MsbuildGenerator> projects, out string profile, bool showWarnings = false)
	{
		var generatedProjFile = GetProjectFilename();
		var updatingExistingProject = File.Exists(generatedProjFile);

		if (!updatingExistingProject)
			Console.WriteLine ($"Generating {generatedProjFile}");

		string boot, flags, output_name, built_sources, response, reskey, sources_file_name;

		boot = xproject.Element ("boot").Value;
		flags = xproject.Element ("flags").Value;
		sources_file_name = xproject.Element ("sources").Value;
		output_name = xproject.Element ("output").Value;
		if (output_name.EndsWith (".exe"))
			Target = Target.Exe;
		built_sources = xproject.Element ("built_sources").Value.Trim ();
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
				fx_version = "4.6.2";
				profile = "net_4_x";
			}
			Console.WriteLine ($"Using response fallback for {output_name}: {response}");
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
						Console.Error.WriteLine ($"// {library_output}: Unable to open response file: {resp_file_full}");
						Environment.Exit (1);
					}

					all_args.Enqueue (extra_args);
					continue;
				}

				if (CSCParseOption (f [i], ref f))
					continue;
				Console.Error.WriteLine ($"// {library_output}: Failure with {f [i]}");
				Environment.Exit (1);
			}
		}

		var groupConditional = $"Condition=\" '$(Platform)' == '{profile}' \"";

		var sources = 
			updatingExistingProject 
				? new StringBuilder ()
				: GenerateSourceItemGroups (
					output_name, profile, 
					sources_file_name, groupConditional, 
					prebuild, postbuild
				);

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

		refs.Append ($"  <ItemGroup {groupConditional}>{NewLine}");

		if (response.Contains ("_test")) {
			refs.Append ($@"    <Reference Include=""nunitlite"">{NewLine}");
			refs.Append ($@"      <HintPath>..\lib\{profile}\nunitlite.dll</HintPath>{NewLine}");
			refs.Append ($@"      <Private>False</Private>{NewLine}");
			refs.Append ($@"    </Reference>{NewLine}");

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
					Console.Error.WriteLine ($"// Found a resource without a filename: {pairs} for {Csproj.csProjFilename}");
					Environment.Exit (1);
				}
				AppendResource (resources, pair.Substring (p+1), pair.Substring (0, p) + ".resources");
			}
		}
		if (resources.Length > 0){
			resources.Insert (0, $"  <ItemGroup {groupConditional}>{NewLine}");
			resources.Append ("  </ItemGroup>" + NewLine);
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
						Console.Error.WriteLine ($"{library}: Could not find a matching project reference for {Path.GetFileName (reference)}");
						Console.Error.WriteLine ("  --> Adding reference with hintpath instead");
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

		refs.Append ("  </ItemGroup>");

		// Possible inputs:
		// ../class/lib/build/tmp/System.Xml.dll  [No longer possible, we should be removing this from order.xml]
		//   /class/lib/basic/System.Core.dll
		// <library_output>mcs.exe</library_output>
		string build_output_dir, intermediate_output_dir;
		if (LibraryOutput.Contains ("/")) {
			build_output_dir = Path.GetDirectoryName (LibraryOutput);
			intermediate_output_dir = build_output_dir.Substring (0, build_output_dir.IndexOf("/class/lib") + 7) + "obj";
		}
		else {
			build_output_dir = "bin\\Debug\\" + library;
			intermediate_output_dir =  "obj\\Debug\\" + library;
		}

		if (build_output_dir.Contains ("-linux") || build_output_dir.Contains ("-macos") || build_output_dir.Contains ("-win32") || build_output_dir.Contains ("-unix"))
			build_output_dir = build_output_dir
				.Replace ("-linux", "-$(HostPlatform)")
				.Replace ("-macos", "-$(HostPlatform)")
				.Replace ("-win32", "-$(HostPlatform)")
				.Replace ("-unix", "-$(HostPlatform)");

		bool basic_or_build = (library.Contains ("-basic") || library.Contains ("-build"));

		// If an EXE is built with nostdlib, it won't work unless run with mono.exe. This stops our build steps
		//  from working in visual studio (because we already replace @MONO@ with '' on Windows.)

		if (Target != Target.Library)
			StdLib = true;

		// We have our target framework set to 4.5 in many places because broken scripts check for files with 4.5
		//  in the path, even though we compile code that uses 4.6 features. So we need to manually fix that here.

		if (fx_version == "4.5")
			fx_version = "4.6.2";

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

		string assemblyName = Path.GetFileNameWithoutExtension (output_name);
		var outputSuffix = Path.GetFileName (build_output_dir);

		string textToUpdate = updatingExistingProject 
			? File.ReadAllText(generatedProjFile)
			: template;

		var properties = new StringBuilder ();
		properties.Append ($"  <PropertyGroup {groupConditional}>{NewLine}");
		properties.Append ($"    <OutputPath>{build_output_dir}</OutputPath>{NewLine}");
  		properties.Append ($"    <IntermediateOutputPath>{intermediate_output_dir}/$(AssemblyName)-{outputSuffix}</IntermediateOutputPath>{NewLine}");
  		properties.Append ($"    <DefineConstants>{defines.ToString ()}</DefineConstants>{NewLine}");
		properties.Append ($"  </PropertyGroup>{NewLine}");

		var prebuild_postbuild = new StringBuilder ();
		if (!String.IsNullOrWhiteSpace(prebuild) || !String.IsNullOrWhiteSpace(postbuild)) {
			prebuild_postbuild.Append ($"  <PropertyGroup>{NewLine}");
			prebuild_postbuild.Append (prebuild);
			prebuild_postbuild.Append (postbuild);
			prebuild_postbuild.Append ($"  </PropertyGroup>{NewLine}");
		}

		var builtSources = new StringBuilder ();
		if (built_sources.Length > 0) {
			builtSources.Append ($"  <ItemGroup Condition=\" '$(Platform)' == '{profile}' \">{NewLine}");
			foreach (var fileName in built_sources.Split ()) {
				var fixedFileName = FixupSourceName (fileName);
				builtSources.Append ($"    <Compile Include=\"{fixedFileName}\" />{NewLine}");
			}
			builtSources.Append ($"  </ItemGroup>{NewLine}");
		}

		Csproj.output = textToUpdate.
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
			Replace ("@FX_VERSION@", fx_version).
			Replace ("@ASSEMBLYNAME@", assemblyName).
			Replace ("@DEBUG@", want_debugging_support ? "true" : "false").
			Replace ("@DEBUGTYPE@", want_debugging_support ? "full" : "pdbonly").
			Replace ("@PREBUILD_POSTBUILD@", prebuild_postbuild.ToString ()).
			Replace ("@STARTUPOBJECT@", main == null ? "" : $"<StartupObject>{main}</StartupObject>").
			//Replace ("@ADDITIONALLIBPATHS@", String.Format ("<AdditionalLibPaths>{0}</AdditionalLibPaths>", string.Join (",", libs.ToArray ()))).
			Replace ("@ADDITIONALLIBPATHS@", String.Empty).
			Replace ("@OPTIMIZE@", Optimize ? "true" : "false").
			Replace ("@METADATAVERSION@", assemblyName == "mscorlib" ? "<RuntimeMetadataVersion>Mono</RuntimeMetadataVersion>" : "");

		var propertiesPlaceholder = "<!-- @ALL_PROFILE_PROPERTIES@ -->";
		var refsPlaceholder = "<!-- @ALL_REFERENCES@ -->";
		var resourcesPlaceholder = "<!-- @ALL_RESOURCES@ -->";
		var sourcesPlaceholder = "<!-- @ALL_SOURCES@ -->";
		var builtSourcesPlaceholder = "<!-- @BUILT_SOURCES@ -->";

		Csproj.output = Csproj.output.
			Replace (propertiesPlaceholder, properties.ToString () + NewLine + propertiesPlaceholder).
			Replace (refsPlaceholder, refs.ToString () + NewLine + refsPlaceholder).
			Replace (resourcesPlaceholder, resources.ToString () + NewLine + resourcesPlaceholder).
			Replace (sourcesPlaceholder, sources.ToString () + NewLine + sourcesPlaceholder).
			Replace (builtSourcesPlaceholder, builtSources.ToString () + NewLine + builtSourcesPlaceholder);

		Csproj.preBuildEvent = prebuild;
		Csproj.postBuildEvent = postbuild;

		//Console.WriteLine ("Generated {0}", ofile.Replace ("\\", "/"));
		// Console.WriteLine("Writing {0}", generatedProjFile);
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

		target_unix = target.Replace ("@MONO@", "mono").Replace ("@CAT@", "cat");
		target_windows = target.Replace ("@MONO@", "").Replace ("@CAT@", "type");

		target_unix = target_unix.Replace ("\\jay\\jay.exe", "\\jay\\jay");

		target_unix = target_unix.Replace ("@COPY@", "cp");
		target_windows = target_windows.Replace ("@COPY@", "copy");

		target_unix = target_unix.Replace ("\r", "");
		const string condition_unix    = "Condition=\" '$(OS)' != 'Windows_NT' \"";
		const string condition_windows = "Condition=\" '$(OS)' == 'Windows_NT' \"";
		
		var result = new StringBuilder ();
		if (!String.IsNullOrWhiteSpace (target_unix))
			result.Append ($"    <{eventKey} {condition_unix}>{target_unix.Trim ()}</{eventKey}>{NewLine}");
		if (!String.IsNullOrWhiteSpace (target_windows))
			result.Append ($"    <{eventKey} {condition_windows}>{target_windows.Trim ()}</{eventKey}>{NewLine}");
		return result.ToString ();
	}
	
	void AddProjectReference (StringBuilder refs, VsCsproj result, MsbuildGenerator match, string r, string alias)
	{
		refs.AppendFormat ("    <ProjectReference Include=\"{0}\"", GetRelativePath (result.csProjFilename, match.CsprojFilename));
		if (alias != null) {
			refs.Append (">" + NewLine);
			refs.Append ("      <Aliases>" + alias + "</Aliases>" + NewLine);
			refs.Append ("    </ProjectReference>" + NewLine);
		}
		else {
			refs.Append (" />" + NewLine);
		}
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
		Console.Error.WriteLine ($"// {library_output}: did not find referenced {dllReferenceName} with libs={ljoined}");

		// FIXME: This is incredibly noisy and generates a billion lines of output
		if (false)
		foreach (var p in projects) {
			Console.Error.WriteLine ("{0}", p.Value.AbsoluteLibraryOutput);
		}

		return null;
	}

}

public static class Driver {
	static IEnumerable<XElement> GetProjects (bool withTests = false)
	{
		XDocument doc = XDocument.Load ("order.xml");
		foreach (XElement project in doc.Root.Elements ()) {
			string dir = project.Attribute ("dir").Value;
			string library = project.Attribute ("library").Value;
			var profile = project.Element ("profile").Value;
			
			//
			// Do not do 2.1, it is not working yet
			// Do not do basic, as there is no point (requires a system mcs to be installed).
			//
			if (library.Contains ("moonlight") || library.Contains ("-basic") || library.EndsWith ("bootstrap")  || library.Contains ("build"))
				continue;

			// The next ones are to make debugging easier for now
			if (profile == "basic")
				continue;
			
			if (library.Contains ("tests") && !withTests)
				continue;

			yield return project;
		}
	}

	public static void Main (string [] args)
	{
		if (!File.Exists ("genproj.cs")) {
			Console.Error.WriteLine ("This command must be executed from mono/msvc/scripts");
			Environment.Exit (1);
		}

		if (args.Length == 1) {
			switch (args[0].ToLower()) {
				case "-h":
				case "--help":
				case "-?":
					Console.Error.WriteLine ("Usage:");
					Console.Error.WriteLine ("genproj.exe [visual_studio_release] [output_full_solutions] [with_tests]");
					Console.Error.WriteLine ("If output_full_solutions is false, only the main System*.dll");
					Console.Error.WriteLine (" assemblies (and dependencies) is included in the solution.");
					Console.Error.WriteLine ("Example:");
					Console.Error.WriteLine (" genproj.exe 2012 false false");
					Console.Error.WriteLine ("genproj.exe with no arguments is equivalent to 'genproj.exe 2012 true false'\n\n");
					Console.Error.WriteLine ("genproj.exe deps");
					Console.Error.WriteLine ("Generates a Makefile dependency file from the projects input");
					Environment.Exit (0);
					break;
			}
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
		Console.Error.WriteLine("// Deleting existing project files");
		foreach (var project in GetProjects (withTests)) {
			var library_output = project.Element ("library_output").Value;

			var gen = new MsbuildGenerator (project);
			projects [library_output] = gen;
			gen.EraseExisting ();
		}
		Console.Error.WriteLine("// Generating project files");
		foreach (var project in GetProjects (withTests)){
			var library_output = project.Element ("library_output").Value;
			// Console.WriteLine ("=== {0} ===", library_output);
			var gen = projects [library_output];
			try {
				string profileName;
				var csproj = gen.Generate (library_output, projects, out profileName);
				var csprojFilename = csproj.csProjFilename;
				if (!sln_gen.ContainsProjectIdentifier (csproj.library)) {
					sln_gen.Add (csproj);
				} else {
					duplicates.Add (csprojFilename);
				}
				
				if (profileName == null) {
					Console.Error.WriteLine ($"// {library_output} has no profile");
				} else {
					HashSet<string> profileNames;
					if (!SlnGenerator.profilesByGuid.TryGetValue (csproj.projectGuid, out profileNames))
						SlnGenerator.profilesByGuid[csproj.projectGuid] = profileNames = new HashSet<string>();

					profileNames.Add (profileName);
					SlnGenerator.observedProfiles.Add (profileName);
				}
			} catch (Exception e) {
				Console.Error.WriteLine ("// Error in {0}\n{1}", project, e);
			}
		}

		Console.WriteLine ("Deduplicating project references");

		foreach (var csprojFile in projects.Values.Select (x => x.GetProjectFilename ()).Distinct ())
		{
			// Console.WriteLine ("Deduplicating: " + csprojFile);
			DeduplicateProjectReferences (csprojFile);
		}

		Func<MsbuildGenerator.VsCsproj, bool> additionalFilter;
		additionalFilter = fullSolutions ? (Func<MsbuildGenerator.VsCsproj, bool>)null : IsCommonLibrary;

		FillSolution (four_five_sln_gen, MsbuildGenerator.profile_4_x, projects.Values, additionalFilter);

		if (duplicates.Count () > 0) {
			var sb = new StringBuilder ();
			sb.AppendLine ("// WARNING: Skipped some project references, apparent duplicates in order.xml:");
			foreach (var item in duplicates) {
				sb.AppendLine ($"// {item}");
			}
			Console.Error.WriteLine (sb.ToString ());
		}

		WriteSolution (four_five_sln_gen, Path.Combine ("..", "..", "bcl.sln"));

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

	static void DeduplicateProjectReferences (string csprojFilename)
	{
		XmlDocument doc = new XmlDocument ();
		doc.Load (csprojFilename);
		XmlNamespaceManager mgr = new XmlNamespaceManager (doc.NameTable);
		mgr.AddNamespace ("x", "http://schemas.microsoft.com/developer/msbuild/2003");

		XmlNode root = doc.DocumentElement;
		var allProjectReferences = new Dictionary<string, List<string>> ();

		ProcessCompileOrProjectReferenceItems (mgr, root,
		(source, platform) => {},
		// grab all project references across all platforms
		(projRef, platform) =>
		{
			if (!allProjectReferences.ContainsKey (platform))
				allProjectReferences[platform] = new List<string> ();
			allProjectReferences[platform].Add (projRef.Attributes["Include"].Value);
		});

		if (allProjectReferences.Count > 1)
		{
			// find the project references which are common across all platforms
			var commonProjectReferences = allProjectReferences.Values.First ();
			foreach (var l in allProjectReferences.Values.Skip (1))
				commonProjectReferences = commonProjectReferences.Intersect (l).ToList ();

			if (commonProjectReferences.Count > 0)
			{
				// remove common project references from the individual platforms
				ProcessCompileOrProjectReferenceItems (mgr, root, null, (projRef, platform) =>
				{
					var parent = projRef.ParentNode;
					if (commonProjectReferences.Contains (projRef.Attributes["Include"].Value))
						parent.RemoveChild (projRef);

					if (!parent.HasChildNodes)
						parent.ParentNode.RemoveChild (parent);
				});

				// add common project references as ItemGroup
				XmlNode commonProjRefsComment = root.SelectSingleNode ("//comment()[. = ' @COMMON_PROJECT_REFERENCES@ ']");
				XmlElement commonProjRefsElement = doc.CreateElement ("ItemGroup", root.NamespaceURI);

				foreach (var s in commonProjectReferences)
				{
					var c = doc.CreateElement ("ProjectReference", root.NamespaceURI);
					var v = doc.CreateAttribute ("Include");
					v.Value = s;
					c.Attributes.Append (v);

					commonProjRefsElement.AppendChild (c);
				}
				root.ReplaceChild (commonProjRefsElement, commonProjRefsComment);
			}
		}

		using (var w = XmlWriter.Create (csprojFilename, new XmlWriterSettings { NewLineChars = SlnGenerator.NewLine, Indent = true }))
			doc.Save (w);
	}

	static void ProcessCompileOrProjectReferenceItems (XmlNamespaceManager mgr, XmlNode x, Action<XmlNode, string> compileAction, Action<XmlNode, string> projRefAction)
	{
		foreach (XmlNode n in x.SelectNodes("//x:ItemGroup[@Condition]", mgr))
		{
			if (n.Attributes.Count == 0)
				continue;

			var platform = n.Attributes["Condition"].Value;

			if (!platform.Contains("$(Platform)"))
				continue;

			var compileItems = n.SelectNodes("./x:Compile[@Include]", mgr);

			if (compileAction != null && compileItems.Count != 0) {
				foreach (XmlNode source in compileItems)
					compileAction(source, platform);
			}

			var projRefItems = n.SelectNodes("./x:ProjectReference[@Include]", mgr);

			if (projRefAction != null && projRefItems.Count != 0) {
				foreach (XmlNode proj in projRefItems) {
					// we don't bother to process ProjectReferences with Aliases
					if (!proj.HasChildNodes)
						projRefAction(proj, platform);
				}
			}
		}
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
		Console.WriteLine (String.Format ("// Writing solution {1}, with {0} projects", sln_gen.Count, slnfilename));
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
