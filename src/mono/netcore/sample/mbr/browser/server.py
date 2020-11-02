# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

import sys

if sys.version_info[0] == 2:

    import SimpleHTTPServer
    import SocketServer

    PORT = 8000

    class Handler(SimpleHTTPServer.SimpleHTTPRequestHandler):
        pass

    Handler.extensions_map['.wasm'] = 'application/wasm'

    httpd = SocketServer.TCPServer(("", PORT), Handler)

    print ("python 2 serving at port", PORT)
    httpd.serve_forever()


if sys.version_info[0] == 3:
    
    import http.server
    import socketserver

    PORT = 8000

    Handler = http.server.SimpleHTTPRequestHandler
    Handler.extensions_map['.wasm'] = 'application/wasm'

    with socketserver.TCPServer(("", PORT), Handler) as httpd:
        print("python 3 serving at port", PORT)
        httpd.serve_forever()

