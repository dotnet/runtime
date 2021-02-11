using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

class Program
{
        int[] _testArray = { 3, 49, 40, 58, 32 };

        static void Main(string[] args)
        {
            var arr = new int[5];
            var type = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.Name.Contains("<PrivateImplementationDetails>")).Single();
            var fieldInfo = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Single();
            RuntimeHelpers.InitializeArray(arr, fieldInfo.FieldHandle);
        }
}
