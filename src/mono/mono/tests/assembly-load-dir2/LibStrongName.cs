
using System.Reflection;

[assembly:AssemblyVersion("2.0.0.0")]
[assembly:AssemblyKeyFile("../testing_gac/testkey.snk")]

public class LibClass {
	public int InAllVersions;
	public int OnlyInVersion2;
	public LibClass () {}
}
