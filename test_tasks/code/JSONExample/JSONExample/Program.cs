﻿using System;

using SimpleJson;
using System.Collections.Generic;
using System.Net;



namespace JSONExample
{

	struct XY
	{
		public int x;
		public int y;
	}

	class MainClass
	{
		public static void Main (string[] args)
		{

			JsonObject jsonObject = new JsonObject();


			JsonArray pointsArray = new JsonArray ();

			for (int i = 0; i < 5; i++) {
				for (int j = 0; j < 5; j++) {
					JsonObject point = new JsonObject ();
					point ["x"] = i;
					point ["y"] = j;
					pointsArray.Add (point);
				}
			}

			jsonObject ["objectsArray"] = pointsArray;
			jsonObject["name"] = "foo";
			jsonObject["num"] = 10;
			jsonObject["is_vip"] = true;
			jsonObject["nickname"] = null;

			string jsonString = jsonObject.ToString();

			JsonObject obj = (JsonObject)SimpleJson.SimpleJson.DeserializeObject (jsonString);

			jsonString = "foo=45&data=" + jsonString;

			jsonString = parsePost (jsonString)["data"];


			Console.WriteLine (jsonString);
			Console.WriteLine (obj["name"]);

			XY first = new XY();
			first.y = 21;

			Console.WriteLine (first.y);


			jsonString = "{\"x\": 34.54, \"y\": 65.65 }";
			obj = (JsonObject)SimpleJson.SimpleJson.DeserializeObject (jsonString);
			double x = double.Parse(obj ["x"].ToString());
			Console.Write (x);
		}

		private static Dictionary<string, string> parsePost (string postString) {
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
	}
}
