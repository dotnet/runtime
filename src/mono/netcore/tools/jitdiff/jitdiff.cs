using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace JitDiffTools
{
	class Program
	{
		static void Main (string [] args)
		{
			if (args?.Length != 2)
			{
				Console.WriteLine ("usage:\n\tjitdiff folder1 folder2");
				return;
			}

			string [] filesBefore = Directory.GetFiles (args [0], "*.dasm");
			string [] filesAfter = Directory.GetFiles (args [1], "*.dasm");
			var pairs = new List<Tuple<string, string>> ();

			foreach (string fileBefore in filesBefore)
			{
				string fileName = Path.GetFileName (fileBefore);
				string fileAfter = filesAfter.FirstOrDefault (f =>
					Path.GetFileName (f).Equals (fileName, StringComparison.InvariantCultureIgnoreCase));

				if (fileAfter != null)
					pairs.Add (new Tuple<string, string> (fileBefore, fileAfter));
			}

			long totalFileDiff = 0;
			Console.WriteLine ();
			foreach (var pair in pairs)
			{
				long sizeBefore = new FileInfo (pair.Item1).Length;
				long sizeAfter = new FileInfo (pair.Item2).Length;
				long diff = sizeAfter - sizeBefore;
				totalFileDiff += diff;
				if (diff != 0)
					Console.WriteLine ($"Total diff for {Path.GetFileName (pair.Item1)}: {diff} bytes");
			}
			if (totalFileDiff != 0)
				Console.WriteLine ($"Total diff for all files: {totalFileDiff} bytes");

			Console.WriteLine ("\n=====================\n= Per-method diffs (may take a while):\n=====================\n");
			foreach (var pair in pairs)
			{
				PrintDiffs (pair.Item1, pair.Item2);
			}
			Console.WriteLine ("Done.");
		}

		static void PrintDiffs (string fileBefore, string fileAfter)
		{
			List<DiffItem> diff = GetDiffs (fileBefore, fileAfter);

			int totalRegression = 0, totalImprovement = 0;
			int methodRegressed = 0, methodImproved = 0;
			foreach (var diffItem in diff.OrderByDescending (d => d.DiffPercentage))
			{
				if (diffItem.HasChanges)
				{
					Console.WriteLine (diffItem);
					if (diffItem.Diff > 0)
					{
						totalRegression += diffItem.Diff;
						methodRegressed++;
					}
					else
					{
						totalImprovement += diffItem.Diff;
						methodImproved++;
					}
				}
			}

			if (methodRegressed == 0 && methodImproved == 0)
				return;

			Console.WriteLine ("\n");
			Console.WriteLine (Path.GetFileNameWithoutExtension (fileBefore));
			Console.WriteLine ($"Methods \"regressed\": {methodRegressed}");
			Console.WriteLine ($"Methods \"improved\": {methodImproved}");
			Console.WriteLine ($"Total regression: {totalRegression} lines, Total improvement: {totalImprovement} lines.");
			Console.WriteLine ("\n");
		}

		static List<DiffItem> GetDiffs (string file1, string file2)
		{
			List<FunctionInfo> file1Functions = ParseFunctions (file1);
			List<FunctionInfo> file2Functions = ParseFunctions (file2);

			var diffItems = new List<DiffItem> (); // diffs

			foreach (FunctionInfo file1Function in file1Functions)
			{
				// SingleOrDefault to make sure functions are unique
				FunctionInfo file2Function = file2Functions.FirstOrDefault (f => f.Name == file1Function.Name);
				diffItems.Add (new DiffItem (file1Function, file2Function)); // file2Function can be null here - means function was removed in file2
			}

			foreach (FunctionInfo file2Function in file2Functions)
			{
				// SingleOrDefault to make sure functions are unique
				FunctionInfo file1Function = file1Functions.FirstOrDefault (f => f.Name == file2Function.Name);
				if (file1Function == null)
					diffItems.Add (new DiffItem (null, file2Function)); // function was added in file2
			}

			return diffItems;
		}

		static bool TryParseFunctionName (string str, out string name)
		{
			// depends on objdump, let's use the whole line as a name if it ends with `:`
			if (str.EndsWith (':'))
			{
				name = Regex.Replace(str, @"(?i)\b([a-f0-9]+){8,16}\b", m => "0xD1FFAB1E");
				return true;
			}
			name = null;
			return false;
		}

		static List<FunctionInfo> ParseFunctions (string file)
		{
			string [] lines = File.ReadAllLines (file)
				.Select (l => l.Trim (' ', '\t', '\r', '\n'))
				.Where (l => !string.IsNullOrEmpty (l))
				.ToArray ();

			var result = new List<FunctionInfo> ();
			FunctionInfo current = null;
			foreach (string line in lines)
			{
				if (TryParseFunctionName (line, out string name))
				{
					current = new FunctionInfo (name);
					result.Add (current);
				}
				current?.Body.Add (line);
			}
			return result;
		}
	}

	public class FunctionInfo
	{
		public FunctionInfo (string name)
		{
			if (string.IsNullOrWhiteSpace (name))
				throw new ArgumentException ("Function name should not be empty", nameof (name));
			Name = name;
		}

		public string Name { get; }

		public List<string> Body { get; set; } = new List<string>();

		public override string ToString () => $"{Name} (lines:{Body?.Count})";
	}

	public class DiffItem
	{
		public DiffItem (FunctionInfo before, FunctionInfo after)
		{
			if (before == null && after == null)
				throw new ArgumentException ("Both Before and After can not be null at the same time");
			if (before != null && after != null && before.Name != after.Name)
				throw new ArgumentException ("After.Name != Before.Name");
			Before = before;
			After = after;
			Name = before != null ? before.Name : after.Name;
		}

		public string Name { get; }

		public FunctionInfo Before { get; }

		public FunctionInfo After { get; }

		public int Diff => CalculateBytes (After) - CalculateBytes (Before);

		static int CalculateBytes (FunctionInfo info)
		{
			// TODO: calculate bytes
			return info?.Body?.Count ?? 0;
		}

		public bool HasChanges => Diff != 0;

		public double DiffPercentage
		{
			get
			{
				int b = Before?.Body?.Count ?? 0;
				int a = (After?.Body?.Count ?? 0) * 100;
				if (a == 0 && b == 0)
					return 0;
				if (a > 0 && b == 0)
					return -100;
				return -(100 - a / b);
			}
		}

		public override string ToString () => $"Diff for {Name}:  {Diff} lines ({DiffPercentage:F1}%)";
	}
}