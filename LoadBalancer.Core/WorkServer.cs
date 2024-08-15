using System.Net;
using System.Text;
using System.Text.Json;

namespace LoadBalancer.Core
{
    public class WorkServer(string url)
    {
        public async Task RunAsync()
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(url);
            httpListener.Start();

            Console.WriteLine($"Server {url}, running!");

            while (httpListener.IsListening)
            {
                var listenerContext = await httpListener.GetContextAsync();
                Console.WriteLine($"Server {url}, handling request!");

                var request = listenerContext.Request;
                var response = listenerContext.Response;

                var length = request.ContentLength64;

                if (length is 0)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Close();
                    continue;
                }

                string content;

                using (var sr = new StreamReader(request.InputStream))
                {
                    content = sr.ReadToEnd();
                }

                if (content.Length != length)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Close();
                    continue;
                }

                var workRequest = JsonSerializer.Deserialize<WorkRequest>(content);
                var workResponse = DoWork(workRequest);

                response.StatusCode = (int)HttpStatusCode.OK;
                response.OutputStream.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(workResponse)));
                response.Close();
            }

            WorkResponse DoWork(WorkRequest workRequest)
            {
                var response = new WorkResponse()
                {
                    Index = workRequest.Index
                };

                var newIterations = 0;

                Parallel.For(0, workRequest.Iterations, (i) =>
                {
                    Interlocked.Increment(ref newIterations);
                });

                response.NewIterations = newIterations;

                return response;
            }
        }
    }
}
