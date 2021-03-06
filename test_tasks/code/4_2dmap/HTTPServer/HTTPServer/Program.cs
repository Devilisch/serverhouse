﻿using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using SimpleJson;

// ReSharper restore LoopCanBeConvertedToQuery
// ReSharper restore RedundantExplicitArrayCreation
// ReSharper restore SuggestUseVarKeywordEvident
// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/
// 
// Its combination with server, parsing json and drawing:)
namespace Bend.Util
{
 
    public class HttpProcessor
    {
        public TcpClient socket;
        public HttpServer srv;

        private Stream inputStream;
        public StreamWriter outputStream;

        public String http_method;
        public String http_url;
        public String http_protocol_versionstring;
        public Hashtable httpHeaders = new Hashtable();


        private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

        public HttpProcessor(TcpClient s, HttpServer srv)
        {
            this.socket = s;
            this.srv = srv;
        }


        private string streamReadLine(Stream inputStream)
        {
            int next_char;
            string data = "";
            while (true)
            {
                next_char = inputStream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }
            return data;
        }
        public void process()
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            inputStream = new BufferedStream(socket.GetStream());

            // we probably shouldn't be using a streamwriter for all output from handlers either
            outputStream = new StreamWriter(new BufferedStream(socket.GetStream()));
            try
            {
                parseRequest();
                readHeaders();
                if (http_method.Equals("GET"))
                {
                    handleGETRequest();
                }
                else if (http_method.Equals("POST"))
                {
                    handlePOSTRequest();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
                writeFailure();
            }
            outputStream.Flush();
            // bs.Flush(); // flush any remaining output
            inputStream = null; outputStream = null; // bs = null;            
            socket.Close();
        }

        public void parseRequest()
        {
            String request = streamReadLine(inputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }
            http_method = tokens[0].ToUpper();
            http_url = tokens[1];
            http_protocol_versionstring = tokens[2];

            Console.WriteLine("starting: " + request);
        }

        public void readHeaders()
        {
            Console.WriteLine("readHeaders()");
            String line;
            while ((line = streamReadLine(inputStream)) != null)
            {
                if (line.Equals(""))
                {
                    Console.WriteLine("got headers");
                    return;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                string value = line.Substring(pos, line.Length - pos);
                Console.WriteLine("header: {0}:{1}", name, value);
                httpHeaders[name] = value;
            }
        }

        public void handleGETRequest()
        {
            srv.handleGETRequest(this);
        }

        private const int BUF_SIZE = 4096;
        public void handlePOSTRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            Console.WriteLine("get post data start");
            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (this.httpHeaders.ContainsKey("Content-Length"))
            {
                content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);
                if (content_len > MAX_POST_SIZE)
                {
                    throw new Exception(
                        String.Format("POST Content-Length({0}) too big for this simple server",
                          content_len));
                }
                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;
                while (to_read > 0)
                {
                    Console.WriteLine("starting Read, to_read={0}", to_read);

                    int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                    Console.WriteLine("read finished, numread={0}", numread);
                    if (numread == 0)
                    {
                        if (to_read == 0)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }
            Console.WriteLine("get post data end");
            srv.handlePOSTRequest(this, new StreamReader(ms));

        }

        public void writeSuccess()
        {
            outputStream.Write("HTTP/1.0 200 OK\n");
            outputStream.Write("Content-Type: text/html\n");
            outputStream.Write("Connection: close\n");
            outputStream.Write("\n");
        }

        public void writeFailure()
        {
            outputStream.Write("HTTP/1.0 404 File not found\n");
            outputStream.Write("Connection: close\n");
            outputStream.Write("\n");
        }
    }

    public abstract class HttpServer
    {

        protected int port;
        TcpListener listener;
        bool is_active = true;

        public HttpServer(int port)
        {
            this.port = port;
        }

        public void listen()
        {

            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();
            while (is_active)
            {
                TcpClient s = listener.AcceptTcpClient();
                HttpProcessor processor = new HttpProcessor(s, this);
                Thread thread = new Thread(new ThreadStart(processor.process));
                thread.Start();
                Thread.Sleep(1);
            }
        }

        public abstract void handleGETRequest(HttpProcessor p);
        public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
    }

    public class MyHttpServer : HttpServer
    {
        public MyHttpServer(int port)
            : base(port)
        {
        }
        public override void handleGETRequest(HttpProcessor p)
        {
            Console.WriteLine("request: {0}", p.http_url);
            p.writeSuccess();
            p.outputStream.WriteLine("<html><body><h1>It works!</h1>");
            p.outputStream.WriteLine("Current Time: " + DateTime.Now.ToString());
            p.outputStream.WriteLine("url : {0}", p.http_url);

            /*p.outputStream.WriteLine("<form method=post action=/form>");
            p.outputStream.WriteLine("<input type=text name=foo value=foovalue>");
            p.outputStream.WriteLine("<input type=submit name=bar value=barvalue>");
            p.outputStream.WriteLine("</form>");*/
        }

        // its for removing useless world "data" in our json string. It is hard solution from Gosha 

        private static Dictionary<string, string> parsePost(string postString)
        {
            Dictionary<string, string> postParams = new Dictionary<string, string>();
            string[] rawParams = postString.Split('&');
            foreach (string param in rawParams)
            {
                string[] kvPair = param.Split('=');
                string key = kvPair[0];
                string value = WebUtility.UrlDecode(kvPair[1]);
                postParams.Add(key, value);
            }

            return postParams;
        }
      //There is a parsing of json-string, drawing the map and sending it ti client
        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            
            List<double[]> coordinates = new List<double[]>();
            

            Console.WriteLine("POST request: {0}", p.http_url);
            string data = inputData.ReadToEnd();
            Console.WriteLine(data);
            Dictionary<string, string> postParams = parsePost(data);   // Just erase parametres adding to json-string
            string jsonString = postParams["data"];

            //Parsing json-string

            SimpleJson.JsonObject jsobject = (SimpleJson.JsonObject)SimpleJson.SimpleJson.DeserializeObject(jsonString);
            SimpleJson.JsonArray jsarray = (SimpleJson.JsonArray)jsobject["objectsArray"];
            

			double maxX = 0, minX = 0, maxY = 0, minY = 0;

            for (int i = 0; i < jsarray.Count; i++)
            {
                SimpleJson.JsonObject ourjsobject = (SimpleJson.JsonObject)jsarray[i];
				double x = double.Parse (ourjsobject ["x"].ToString ());
				double y = double.Parse (ourjsobject ["y"].ToString ());

                double[] decoordinates = new double[2];
				decoordinates[0] = x;
				decoordinates[1] = y;
                coordinates.Add(decoordinates);
                

				if (x < minX)
					minX = x;				
				if (x > maxX)
					maxX = x;
				if (y < minY)
					minY = y;
				if (y > maxY)
					maxY = y;

            }

			double mapWidth = maxX - minX;
			double mapHeight = maxY - minY;
			double x0 = mapWidth/2;
			double y0 = mapHeight/2;

			Console.WriteLine ("FIELD SIZE: {0}x{1}", mapWidth, mapHeight);

         // Drawing a map
			Bitmap image = new Bitmap((int)(mapWidth), (int)(mapHeight));
			Graphics gr = Graphics.FromImage(image);
			gr.FillRectangle(Brushes.Red, new RectangleF(0, 0, (float)mapWidth, (float)mapHeight));
            
            foreach (double[] pair in coordinates)
            {
				Console.WriteLine ("PUT OBJECT AT ({0}, {1})", x0 + pair[0], y0 + pair[1]);
				gr.FillRectangle(Brushes.White, new RectangleF((float)(x0 + (float)(pair[0])), (float)(y0 + (float)(pair[1])), 10, 10));
            }

			//gr.FillRectangle(Brushes.White, new RectangleF((float)x0, (float)y0, 10, 10));

            // This saving only for testing
			string path = System.IO.Path.Combine (
				Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
				"Example.png"
			);

			/*using (image) {
				image.Save (path, System.Drawing.Imaging.ImageFormat.Png);
			}*/

        //Sending  a map           
       //Make post-headers
       p.outputStream.Write("HTTP/1.0 200 OK\n");
       p.outputStream.Write("Content-Type: image/png\n");
       p.outputStream.Write("Connection: close\n");
       p.outputStream.Write("\n");

       // Sending data with image to client (Unity)
       image.Save(p.outputStream.BaseStream, System.Drawing.Imaging.ImageFormat.Png);



        }
    }

    public class TestMain
    {
        public static int Main(String[] args)
        {
            Console.WriteLine("APP STARTED");
            HttpServer httpServer;
            if (args.GetLength(0) > 0)
            {
                httpServer = new MyHttpServer(Convert.ToInt16(args[0]));
            }
            else
            {
                httpServer = new MyHttpServer(8080);
            }
            Thread thread = new Thread(new ThreadStart(httpServer.listen));
            thread.Start();
            return 0;
        }

    }

}



