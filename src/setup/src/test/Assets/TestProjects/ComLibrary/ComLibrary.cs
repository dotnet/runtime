using System;
using System.Runtime.InteropServices;

namespace ComLibrary
{
    [ComVisible(true)]
    [Guid("438968CE-5950-4FBC-90B0-E64691350DF5")]
    public class Server
    {
        public Server()
        {
            Console.WriteLine($"New instance of {nameof(Server)} created");
        }
    }
}