using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;

namespace ResourceLibrary;

public static class ResourceAccessor
{
    public static void Read(Action<string> testOuput)
    {
        var rm = new ResourceManager("ResourceLibrary.words", typeof(ResourceAccessor).Assembly);
        testOuput("default: " + rm.GetString("hello", CultureInfo.CurrentCulture));
        testOuput("es-ES without satellite: " + rm.GetString("hello", new CultureInfo("es-ES")));
    }
}