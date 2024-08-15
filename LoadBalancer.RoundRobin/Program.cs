// See https://aka.ms/new-console-template for more information
using LoadBalancer.Core;
using System.Net;
using System.Net.Http.Headers;

Console.WriteLine("Load balancer, running!");

string url = "http://localhost:8000/";

var httpListener = new HttpListener();
httpListener.Prefixes.Add(url);
httpListener.Start();

Dictionary<string, int> availableServers = File.ReadAllLines(
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "servers.txt"))
    .ToDictionary(s => s.Trim(), s => 0);

foreach (var server in availableServers)
{
    var workServer = new WorkServer(server.Key);
    _ = workServer.RunAsync();
}

var httpClient = new HttpClient();

while (httpListener.IsListening)
{
    var listenerContext = await httpListener.GetContextAsync();
    var request = listenerContext.Request;
    var response = listenerContext.Response;

    var availableServer = availableServers.MinBy(x => x.Value).Key;
    availableServers[availableServer]++;

    var newRequest = new HttpRequestMessage()
    {
        RequestUri = new Uri(availableServer),
        Content = new StreamContent(request.InputStream)
    };

    newRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    newRequest.Content.Headers.ContentLength = request.ContentLength64;

    _ = httpClient.SendAsync(newRequest).ContinueWith(r =>
    {
        availableServers[availableServer]--;

        var newResponse = r.Result;
        response.StatusCode = (int)newResponse.StatusCode;
        newResponse.Content.ReadAsStream().CopyTo(response.OutputStream);
        response.Close();
    });
}