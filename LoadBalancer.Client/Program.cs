// See https://aka.ms/new-console-template for more information
using LoadBalancer.Core;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

Console.WriteLine("Hello, World!");

string url = "http://localhost:8000/";

var httpClient = new HttpClient();

var inputs = File.ReadAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "inputs.txt"));

var stack = new Stack<long>();

foreach (var input in inputs)
{
    if (!long.TryParse(input, out var longInput))
    {
        continue;
    }

    stack.Push(longInput);
}

var finished = new List<long>();

while (finished.Count != inputs.Length)
{
    if (!stack.TryPop(out var input)){
        continue;
    }

    var stream = new MemoryStream();
    var workRequest = new WorkRequest()
    {
        Iterations = input
    };
    stream.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(workRequest)));
    stream.Seek(0, SeekOrigin.Begin);

    var streamContent = new StreamContent(stream);
    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    streamContent.Headers.ContentLength = stream.Length;

    var request = new HttpRequestMessage()
    {
        RequestUri = new Uri(url),
        Content = streamContent
    };

    try
    {
        _ = httpClient.SendAsync(request).ContinueWith(async r =>
        {
            var response = r.Result;
            if (!response.IsSuccessStatusCode)
            {
                stack.Push(input);
            }
            else
            {
                finished.Add(input);

                var content = await response.Content.ReadAsStringAsync();

                var workResponse = JsonSerializer.Deserialize<WorkResponse>(content);
                Console.WriteLine($"{workResponse.Index} - {workResponse.NewIterations}");
            }

            Console.WriteLine(response.StatusCode);
        });
    }
    catch (HttpRequestException)
    {
        continue;
    }
}