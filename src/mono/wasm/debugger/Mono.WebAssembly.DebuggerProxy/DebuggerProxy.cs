using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebAssembly.Net.Debugging {

	// This type is the public entrypoint that allows external code to attach the debugger proxy
	// to a given websocket listener. Everything else in this package can be internal.

	public class DebuggerProxy {
		private readonly MonoProxy proxy;

		public DebuggerProxy (ILoggerFactory loggerFactory) {
			proxy = new MonoProxy(loggerFactory);
		}

		public Task Run (Uri browserUri, WebSocket ideSocket) {
			return proxy.Run (browserUri, ideSocket);
		}
	}
}
