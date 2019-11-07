using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JitBench
{
    /// <summary>
    /// Diverts console output into a ITestOutputHelper
    /// </summary>
    public class ConsoleRedirector : IDisposable, ITestOutputHelper
    {
        ITestOutputHelper _output;
        TextWriter _originalConsoleOut;
        MemoryStream _bufferedConsoleStream;
        StreamWriter _bufferedConsoleWriter;

        public ConsoleRedirector(ITestOutputHelper output)
        {
            _output = output;
            _originalConsoleOut = Console.Out;
            _bufferedConsoleStream = new MemoryStream();
            Console.SetOut(_bufferedConsoleWriter = new StreamWriter(_bufferedConsoleStream));
        }

        public void Dispose()
        {
            Console.SetOut(_originalConsoleOut);
            if(_output != null)
            {
                _bufferedConsoleWriter.Flush();
                StreamReader reader = new StreamReader(_bufferedConsoleStream);
                _bufferedConsoleStream.Seek(0, SeekOrigin.Begin);
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                        break;
                    _output.WriteLine(line);
                }
            }
        }

        public void WriteLine(string line)
        {
            _bufferedConsoleWriter.WriteLine(line);
        }
    }
}
