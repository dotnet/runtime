
using System.Net;
using System.Net.Sockets;
using System.IO;
using System;
using System.Text;
using System.Collections;

namespace T {
	public class T {

		private static String docroot="/home/dick/mono/install/html";
		//private static String docroot="./";

		private static Hashtable mime_types = new Hashtable();

		private static Socket NetSetup() {
			Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			s.Bind(new IPEndPoint(IPAddress.Any, 8000));

			Console.WriteLine("Listening on " + s.LocalEndPoint.ToString());

			s.Listen(5);

			return(s);
		}

		private static String NetRead(Socket sock) {
			byte[] buf=new byte[256];

			int count=sock.Receive(buf);

			// Supply the length because otherwise I get a
			// string of 260-odd chars instead of 30 for some reason
			String req=new String(Encoding.UTF8.GetChars(buf), 0, count);
			return(req);
		}

		private static void NetWrite(Socket sock, String data) {
			byte[] buf=new UTF8Encoding().GetBytes(data);

			sock.Send(buf);
		}

		private static void ReplyHeaders(Socket sock, int code,
						 String detail,
						 String content_type,
						 String content_opt,
						 long content_length) {
			NetWrite(sock, "HTTP/1.0 " + code + " " + detail + "\r\n");
			NetWrite(sock, "Date: Sat, 12 Jan 2002 01:52:56 GMT\r\n");
			NetWrite(sock, "Server: MIS\r\n");
			NetWrite(sock, "Last-Modified: Sat, 12 Jan 2002 01:52:56 GMT\r\n");
			NetWrite(sock, "Connection: close\r\n");
			if(content_length>0) {
				NetWrite(sock, "Content-Length: " + content_length + "\r\n");
			}
			NetWrite(sock, "Content-type: " + content_type);
			if(content_opt!=null) {
				NetWrite(sock, "; " + content_opt);
			}
			NetWrite(sock, "\r\n");
			NetWrite(sock, "\r\n");
		}

		private static void NotFound(Socket sock) {
			ReplyHeaders(sock, 404, "Not Found", "text/html",
				     "charset=iso-8859-1", 0);
			NetWrite(sock, "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">\r\n");
			NetWrite(sock, "<HTML><HEAD>\r\n");
			NetWrite(sock, "<TITLE>404 Not Found</TITLE>\r\n");
			NetWrite(sock, "</HEAD><BODY>\r\n");
			NetWrite(sock, "<H1>Not Found</H1>\r\n");
			NetWrite(sock, "</BODY></HTML>\r\n");
		}

		static void GetHeaders(out String req, out String[] headers,
				       String data, Socket sock) {
			// First, find the \r\n denoting the end of the
			// request line
			int pos=data.IndexOf("\r\n");
			while(pos==-1) {
				Console.WriteLine("Couldn't isolate request");
				data=data+NetRead(sock);
				pos=data.IndexOf("\r\n");
			}

			req=data.Remove(pos, data.Length-pos);

			// We've isolated the request line, now get the headers

			// Make sure we have all the headers
			pos=data.IndexOf("\r\n\r\n");
			while(pos==-1) {
				//Console.WriteLine("Didn't read all the headers");
				data=data+NetRead(sock);
				pos=data.IndexOf("\r\n\r\n");
			}

			String hdr=data.Remove(0, req.Length+2);
			headers=hdr.Split(new char[]{'\r', '\n'});
		}

		private static void Get(Socket sock, String data) {
			String req;
			String[] headers;

			GetHeaders(out req, out headers, data, sock);
			for(int i=0; i<headers.Length; i++) {
				if(headers[i].StartsWith("User-Agent: ")) {
					Console.WriteLine(headers[i]);
				}
			}

			// Remove the method, and prepend the docroot
			req=String.Concat(docroot, req.Remove(0, 4));

			// Trim the trailing protocol info
			int pos=req.IndexOfAny(new char[]{' '});
			if(pos>=0) {
				req=req.Remove(pos, req.Length-pos);
			}

			pos=req.LastIndexOf('.');
			String filetype;
			if (pos != -1)
				filetype = req.Substring(pos);
			else
				filetype = "";
			


			string mime_type = (string) mime_types [filetype];
			if (mime_type == null)
				mime_type = "text/plain";
			
			Console.WriteLine("File is " + req);
			Console.WriteLine("Mime type is " + mime_type);
			
			try {
				FileStream f=new FileStream(req, FileMode.Open, FileAccess.Read);
				byte[] fbuf=new byte[256];

				ReplyHeaders(sock, 200, "OK",
					     mime_type,
					     null, f.Length);

				int count;
				while((count=f.Read(fbuf, 0, 256))>0) {
					// Specify amount, so the last
					// block doesnt send extra crud at
					// the end
					sock.Send(fbuf, count, SocketFlags.None);
				}

				f.Close();
			} catch(FileNotFoundException) {
				Console.WriteLine("File not found");
				NotFound(sock);
			} catch(IOException) {
				Console.WriteLine("IO error");
				NotFound(sock);
			}
		}

		private static void Head(Socket sock, String data) {
			String req;
			String[] headers;

			GetHeaders(out req, out headers, data, sock);
			for(int i=0; i<headers.Length; i++) {
				if(headers[i].StartsWith("User-Agent: ")) {
					Console.WriteLine(headers[i]);
				}
			}

			// Remove the method, and prepend the docroot
			req=String.Concat(docroot, req.Remove(0, 5));

			// Trim the trailing protocol info
			int pos=req.IndexOfAny(new char[]{' '});
			if(pos>=0) {
				req=req.Remove(pos, req.Length-pos);
			}

			pos=req.LastIndexOf('.');
			string filetype;
			if (pos != -1)
				filetype=req.Substring(pos);
			else
				filetype = "";

			string mime_type = (string) mime_types [filetype];
			if (mime_type == null)
				mime_type = "text/plain";
			Console.WriteLine("File is " + req);
			Console.WriteLine("Mime type is " + mime_type);

			try {
				FileStream f=new FileStream(req, FileMode.Open, FileAccess.Read);
				byte[] fbuf=new byte[256];

				ReplyHeaders(sock, 200, "OK",
					     mime_type,
					     null, f.Length);

				f.Close();
			} catch(FileNotFoundException) {
				Console.WriteLine("File not found");
				NotFound(sock);
			} catch(IOException) {
				Console.WriteLine("IO error");
				NotFound(sock);
			}
		}

		public static int Main (string [] args) {
			// Set up mime types
			mime_types.Add(".html", "text/html");
			mime_types.Add(".jpeg", "image/jpeg");
			mime_types.Add(".png", "image/png");
			mime_types.Add(".cs", "text/plain");

			if (args.Length == 2 && args [0] == "--root"){
				docroot = args [1];
			}
			
			Socket s=NetSetup();

			while(true) {
				Socket newsock=s.Accept();
				String req=NetRead(newsock);

				if(String.Compare(req, 0, "GET ", 0, 4)==0) {
					Get(newsock, req);
				} else if(String.Compare(req, 0, "HEAD ", 0, 5)==0) {
					Head(newsock, req);
				} else {
					Console.WriteLine("Unknown method!");
					Console.WriteLine("[" + req + "]");
				}

				newsock.Close();
			}
		}
	}
}
