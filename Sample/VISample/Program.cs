using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.IO;
using HtmlAgilityPack;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Web.Helpers;


namespace VISample
{
    class Program
    {
        static void Main()
        {

            Console.WriteLine("Hit ENTER to exit...");

            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (StreamReader sr = new StreamReader("C:/Users/antho/Desktop/video_urls.txt"))
                {
                    string line;
                    List<string> videoUrls = new List<string>();
                    
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        videoUrls.Add(line);
                        MakeRequest(line);
                    }
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }


        }

        // import the Urls

        static async void MakeRequest(string line)
        {
            
            var apiUrl = "https://api.videoindexer.ai";
            var accountId = "YourAccountId";
            var location = "westus2";
            var apiKey = "YourApiKey";

            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // create the http client
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            // obtain account access token
            var accountAccessTokenRequestResult = client.GetAsync($"{apiUrl}/auth/{location}/Accounts/{accountId}/AccessToken?allowEdit=true").Result;
            var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            // upload a video
            var content = new MultipartFormDataContent();
            Console.WriteLine("Uploading...");

            try
            {
                var videoUrl = line;
                var videoTitle = (line.Length > 16)? line.Substring(line.Length - 16, 12): line;
                // need to manipulate below line if we don't want them to all be some_name and some_description
                var uploadRequestResult = client.PostAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos?accessToken={accountAccessToken}&name={videoTitle}&description={videoUrl}&privacy=private&partition=some_partition&videoUrl={videoUrl}", content).Result;
                var uploadResult = uploadRequestResult.Content.ReadAsStringAsync().Result;

                // get the video id from the upload result
                var videoId = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];
                Console.WriteLine("Uploaded");
                Console.WriteLine("Video ID: " + videoId);

                Debug.WriteLine("Uploaded");
                //Debug.WriteLine("Video ID: " + videoId);

                // obtain video access token            
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                var uri = $"{apiUrl}/auth/{location}/Accounts/{accountId}/Videos/{videoId}/AccessToken?allowEdit=true";
                var videoTokenRequestResult = await client.GetAsync(uri);
                var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

                // wait for the video index to finish
                while (true)
                {
                    Thread.Sleep(10000);

                    var videoGetIndexRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English").Result;
                    var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

                    var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                    Console.WriteLine("");
                    Console.WriteLine("State: " + processingState);
                    //Console.WriteLine(processingState);

                    Debug.WriteLine("");
                    Debug.WriteLine("State:");
                    //Debug.WriteLine(processingState);

                    // job is finished
                    if (processingState != "Uploaded" && processingState != "Processing")
                    {
                        Console.WriteLine("");
                        Console.WriteLine("Full JSON:");
                        Console.WriteLine(videoGetIndexResult);

                        Debug.WriteLine("");
                        Debug.WriteLine("Full JSON:");
                        Debug.WriteLine(videoGetIndexResult);
                        break;
                    }
                }

                // search for the video
                var searchRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/Search?accessToken={accountAccessToken}&id={videoId}").Result;
                var searchResult = searchRequestResult.Content.ReadAsStringAsync().Result;
                Console.WriteLine("");
                Console.WriteLine("Search:");
                Console.WriteLine(searchResult);

                Debug.WriteLine("");
                Debug.WriteLine("Search:");
                Debug.WriteLine(searchResult);

                // get insights widget url
                var insightsWidgetRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/InsightsWidget?accessToken={videoAccessToken}&widgetType=Keywords&allowEdit=true").Result;
                var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
                Console.WriteLine("Insights Widget url:");
                Console.WriteLine(insightsWidgetLink);

                Debug.WriteLine("Insights Widget url:");
                Debug.WriteLine(insightsWidgetLink);

                // get player widget url
                var playerWidgetRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/PlayerWidget?accessToken={videoAccessToken}").Result;
                var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
                Console.WriteLine("");
                Console.WriteLine("Player Widget url:");
                Console.WriteLine(playerWidgetLink);

                Debug.WriteLine("");
                Debug.WriteLine("Player Widget url:");
                Debug.WriteLine(playerWidgetLink);
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The video file could not be read, exception encountered:");
                Console.WriteLine(e.Message);
            }


        }

    }


}
