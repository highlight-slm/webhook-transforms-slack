using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Highlight.WebHookParserFunction {
    public static class HttpTriggerCSharp {
        private static readonly HttpClient httpClient;

        static HttpTriggerCSharp () {
            // Single instance for handling all requests.
            httpClient = new HttpClient ();
        }

        [FunctionName ("HttpTriggerCSharp")]
        public static async Task<IActionResult> Run (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log) {
            log.LogInformation ("C# HTTP trigger function processed a request.");
            // Local execution setting in local.settings.json
            // Azure execution setting configuration -> Applicaiton settings
            string slackWebHookUrl = Environment.GetEnvironmentVariable ("SLACK_WEBHOOK_URL");

            string requestBody = await new StreamReader (req.Body).ReadToEndAsync ();
            dynamic data = JsonConvert.DeserializeObject (requestBody);
            if (!string.IsNullOrEmpty (Convert.ToString (data))) {
                string message = CreateSlackMessage (data);
                var postResponse = await PostSlackMessage (message, slackWebHookUrl);
                if (postResponse.IsSuccessStatusCode) {
                    log.LogInformation ("Webhook successfully sent to Slack");
                    return new OkObjectResult ("OK");
                }
                log.LogInformation ("Error sending Webhook");
                var content = await postResponse.Content.ReadAsStringAsync ();
                return new BadRequestObjectResult ($"Error ({(int)postResponse.StatusCode} - {postResponse.StatusCode}) posting webhook: {content}");
            }
            log.LogInformation ("Error empty incoming Webhook");
            return new BadRequestObjectResult ("Error: No Data received.");
        }

        public static string CreateSlackMessage (dynamic payload) {
            SlackMessage message = new SlackMessage ();
            message.text = "Highlight Alert";
            message.attachments = CreateSlackAttachment (payload);
            message.mrkdown = "True";
            return JsonConvert.SerializeObject (message);;
        }

        public static async Task<HttpResponseMessage> PostSlackMessage (string message, string slacklUrl) {
            StringContent body = new StringContent (message, Encoding.UTF8, "application/json");
            Uri uri = new Uri (slacklUrl);
            HttpResponseMessage responseMessage = null;
            try {
                responseMessage = await httpClient.PostAsync (uri, body);
            } catch (Exception ex) {
                if (responseMessage == null) {
                    responseMessage = new HttpResponseMessage ();
                }
                responseMessage.StatusCode = HttpStatusCode.InternalServerError;
                responseMessage.ReasonPhrase = string.Format ("Webhook send failed: {0}", ex);
            }
            return responseMessage;
        }

        private static List<SlackAttachment> CreateSlackAttachment (dynamic payload) {
            SlackAttachment attachment = new SlackAttachment ();
            List<SlackAttachment> attachmentList = new List<SlackAttachment> ();
            string problem = Convert.ToString (payload.problem);
            if (!string.IsNullOrEmpty (problem)) {
                HighlightAlert status = GetStatus (problem);
                string alertSummary = Convert.ToString (payload.alertSummary);
                string linkUrl = Convert.ToString (payload.linkUrl);
                attachment.title = $"{status.direction} {alertSummary}";
                attachment.text = $"{problem} - <{linkUrl}|More information>";
                attachment.color = status.color;
                attachment.markdown_in = "title, text";
                attachmentList.Add (attachment);
            }
            return attachmentList;
        }

        private static HighlightAlert GetStatus (string problem) {
            // Example: "problem": "Link-Availability - Red alert raised"
            HighlightAlert status = new HighlightAlert ();
            string pattern = @"(\w+) alert (\w+)";
            Match match = Regex.Match (problem.ToLower (), pattern, RegexOptions.IgnoreCase);
            if (match.Success) {
                var highlightColor = match.Groups[1].Value;
                var highlightDirection = match.Groups[2].Value;
                if (highlightDirection == "raised") {
                    status.direction = ":warning:";
                    if (highlightColor == "red") {
                        status.color = "danger";
                    } else if (highlightColor == "amber") {
                        status.color = "warning";
                    }
                } else if (highlightDirection == "cleared") {
                    status.color = "good";
                    status.direction = ":heavy_check_mark:";
                } else {
                    status.direction = ":grey_question:";
                }
            }
            return status;
        }

        public class SlackAttachment {
            public string color { get; set; }
            public string title { get; set; }
            public string text { get; set; }
            public string markdown_in { get; set; }
        }

        public class SlackMessage {
            public string text { get; set; }
            public List<SlackAttachment> attachments { get; set; }
            public string mrkdown { get; set; }
        }

        public class HighlightAlert {
            public string color { get; set; }
            public string direction { get; set; }
        }
    }
}