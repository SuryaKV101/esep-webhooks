using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<APIGatewayProxyResponse> FunctionHandler(
            APIGatewayProxyRequest request,
            ILambdaContext context)
        {
            try
            {
                // 1) The raw JSON payload from GitHub
                var raw = request.Body;
                context.Logger.LogLine($"RAW BODY: {raw}");

                // 2) Parse with JObject
                var obj = JObject.Parse(raw);
                var issueUrl = (string)obj["issue"]?["html_url"];
                if (string.IsNullOrEmpty(issueUrl))
                    throw new Exception("issue.html_url missing!");

                context.Logger.LogLine($"Found URL: {issueUrl}");

                // 3) Build Slack message
                var slackMsg = new { text = $":rotating_light: New Issue: {issueUrl}" };
                var payload = JObject.FromObject(slackMsg).ToString();
                context.Logger.LogLine($"Slack payload: {payload}");

                // 4) Send to Slack
                var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL")
                               ?? throw new Exception("SLACK_URL env var not set");
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(slackUrl, content);
                context.Logger.LogLine($"Slack returned {resp.StatusCode}");
                resp.EnsureSuccessStatusCode();

                return new APIGatewayProxyResponse {
                    StatusCode = 200,
                    Body = "OK"
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex}");
                return new APIGatewayProxyResponse {
                    StatusCode = 500,
                    Body = ex.Message
                };
            }
        }
    }
}