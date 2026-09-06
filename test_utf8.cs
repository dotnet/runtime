using System;
using System.Text;

class Program
{
    static void Main()
    {
        var utf8 = new UTF8Encoding(false, true);
        try
        {
            utf8.GetCharCount(new byte[] { 0xFF });
            Console.WriteLine("Success");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Thrown: {ex.GetType().Name}");
        }
    }
}
