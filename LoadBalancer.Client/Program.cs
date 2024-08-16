// See https://aka.ms/new-console-template for more information
using LoadBalancer.Core;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

Console.WriteLine("Hello, World!");

string url = "http://localhost:8000/";

var httpClient = new HttpClient();

var inputs = File.ReadAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "inputs.txt")).Select((x, i) => (x, i)).ToArray();

var stack = new Stack<(int, long)>();

foreach (var (x, i) in inputs)
{
    if (!long.TryParse(x, out var longInput))
    {
        continue;
    }

    stack.Push((i, longInput));
}

var finished = new List<int>();

while (finished.Count != inputs.Length)
{
    if (!stack.TryPop(out var input))
    {
        continue;
    }

    var stream = new MemoryStream();
    var workRequest = new WorkRequest()
    {
        Index = input.Item1,
        Iterations = input.Item2
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

    _ = Task.Run(async () =>
    {
        try
        {
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                stack.Push(input);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();

                var workResponse = JsonSerializer.Deserialize<WorkResponse>(content);

                if (workResponse is null)
                {
                    stack.Push(input);
                    return;
                }

                var index = workResponse.Index;

                while (index != finished.Count)
                {

                }

                Console.WriteLine($"{index} - {workResponse.NewIterations}");

                finished.Add(input.Item1);
            }
        }
        catch (HttpRequestException)
        {
            stack.Push(input);
        }
    });
}