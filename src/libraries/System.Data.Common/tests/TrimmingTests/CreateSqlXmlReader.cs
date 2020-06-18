using System.Data.SqlTypes;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        MemoryStream ms = new MemoryStream();
        var sqlXml = new SqlXml(ms);
        var xmlReader = sqlXml.CreateReader();
        return 100;
    }
}
