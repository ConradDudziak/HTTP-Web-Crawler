// Conrad Dudziak
// This program receives a url and number of hops. The program will then
// make an http GET call on the url, and hop to the first <a href> tagged
// http url in the resulting HTML.
// If a url has already been visited, the next url in the HTML is hopped to.
// Program ends when number of hops is reached, if a 400 / 500 error occurs,
// or if the resulting HTML does not have any embedded references.
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace CSS436Crawler {
	class Program {

		// Main function receives a url and number of hops and starts
		// the crawler.
		// Outputs console error messages if input is bad.
		static void Main(string[] args) {
			List<string> visitedUrls = new List<string>();
			string startingUrl;
			int numHops;

			if (args.Length != 2) {
				Console.WriteLine("Incorrect usage");
				Console.WriteLine("-- Usage: .exe url numHops");
			} else {
				startingUrl = args[0];
			    numHops = 0;
				
				// Start crawling if numHops was valid input
				if (Int32.TryParse(args[1], out numHops)) {
					if (numHops >= 0) {
						Console.WriteLine("Starting URl: " + startingUrl);
						Console.WriteLine("Hopping " + ((numHops > 1) ? (numHops + " times.") : (numHops + " time.")));
						Console.WriteLine(Crawl(startingUrl, numHops, visitedUrls));
					} else {
						Console.WriteLine("NumHops must be greater than or equal to 0.");
					}
				}
			}
		}

		// A recrusive method that makes an http GET request on the string url.
		// The numHops is decremented until no more hops remain.
		// If numHops is 0, only the passed url is visited and the html is returned.
		// If numHops is not 0, the next embedded reference is visited. The List of 
		// visitedUrls is used to prevent visiting the same urls, where the next avialable
		// url in the html will be visited instead.
		static string Crawl(string url, int numHops, List<string> visitedUrls) {
			try {
				using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })) {
					HttpResponseMessage response = client.GetAsync(url).Result;
					try {
						response.EnsureSuccessStatusCode();
						// Get the successful responses result
						string result = response.Content.ReadAsStringAsync().Result;
						// Find the next url to be visited in the responses resulting HTML
						string nextUrl = GetNextHrefFromHTML(result, visitedUrls);
						if (numHops == 0) {
							// Crawlers is finished (all hops performed)
							return "Final Url: " + url + " \nResult:\n" + result;
						} else if (nextUrl == "") {
							// Crawler could not find a url to hop to
							return "No url found to hop to. \nFinal Url: " 
									+ url + "\nNum Hops remaining: " + numHops + "\nResult:\n" + result;
						} else {
							// Crawler is visiting the next url and decrements the remaining hops
							return Crawl(nextUrl, numHops - 1, visitedUrls);
						}
					} catch (Exception e) {
						return e.Message;
					}
				}
			} catch (Exception e) {
				return e.Message;
			}
		}

		// Receives a string of html and a list of previously visited urls.
		// Uses Regex to find http links embedded in <a href> elements.
		// Returns the next available url.
		static string GetNextHrefFromHTML(string html, List<string> visitedUrls) {
			Regex getHref = new Regex(@"<\s*a\s+href=(""|')http://[^""']*(""|')");
			MatchCollection matches = getHref.Matches(html);
			
			// Loops through all the found <a href> http elements.
			foreach (Match match in matches) {
				if (match.Success) {
					// Extracts the url in the <a href> tag.
					Regex getUrl = new Regex(@"(""|').*(""|')");
					Match urlMatch = getUrl.Match(match.Value);
					if (urlMatch.Success) {
						// Removes the remaining quotation marks.
						string nextUrl = urlMatch.Value.Trim('"');
						if (CheckUniqueURL(nextUrl, visitedUrls)) {
							return nextUrl;
						}
					}
				}
			}
			return "";
		}

		// Receives a url and a list of visited urls.
		// Returns true if the url does not exist in the visited url list.
		// All urls are stored with a trailing backslash, so that a
		// non-trailing backslash url is seen the same as a backslashed url.
		static bool CheckUniqueURL(string url, List<string> visitedUrls) {
			string urlToAdd = url;
			if (url[url.Length - 1] != '/') {
				urlToAdd = url + "/";
			}

			if (!visitedUrls.Contains(urlToAdd)) {
				visitedUrls.Add(urlToAdd);
				return true;
			}
			return false;
		}
	}
}
