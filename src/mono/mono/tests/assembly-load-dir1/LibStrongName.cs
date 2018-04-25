
using System.Reflection;

[assembly:AssemblyVersion("1.0.0.0")]
[assembly:AssemblyKeyFile("../testing_gac/testkey.snk")]

public class LibClass {
	public int OnlyInVersion1;
	public int InAllVersions;
	public LibClass () {}
}
