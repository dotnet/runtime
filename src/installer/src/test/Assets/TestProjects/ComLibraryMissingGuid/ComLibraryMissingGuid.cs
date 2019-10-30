using System;
using System.Runtime.InteropServices;

namespace ComLibrary
{
    [ComVisible(true)]
    public class Server
    {
        public Server()
        {
            Console.WriteLine($"New instance of {nameof(Server)} created");
        }
    }
}