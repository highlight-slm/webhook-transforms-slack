using System;
using System.Collections.Generic;
using System.IO;
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
        // Single instance for handling all requests. To avoid client being created on each request.
        // https://docs.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/
        private static readonly HttpClient httpClient = new HttpClient ();

        [FunctionName ("HttpTriggerCSharp")]
        public static async Task<IActionResult> Run (
            [HttpTrigger (AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log) {
            log.LogInformation ("C# HTTP trigger function processed a request.");
            // Local execution setting in local.settings.json
            // Azure execution setting configuration -> Application settings
            var slackWebHookUrl = Environment.GetEnvironmentVariable ("SLACK_WEBHOOK_URL");
            if (string.IsNullOrWhiteSpace (slackWebHookUrl)) {
                log.LogError ("Application variable SLACK_WEBHOOK_URL not set");
                return new BadRequestResult ();
            }

            dynamic data = JsonConvert.DeserializeObject (await new StreamReader (req.Body).ReadToEndAsync ());
            if (data == null) {
                log.LogInformation ("Invalid input data");
                return new BadRequestResult ();
            }

            var message = SlackMessage
                .Create (data)
                .ToString ();

            if (await PostSlackMessage (message, slackWebHookUrl, log)) {
                log.LogInformation ("Webhook successfully sent to Slack");
                return new OkResult ();
            } else {
                log.LogInformation ("Error sending Webhook");
                return new BadRequestResult ();
            }
        }

        public static async Task<bool> PostSlackMessage (string message,
            string slackUrl, ILogger log) {
            var body = new StringContent (message, Encoding.UTF8, "application/json");

            try {
                var response = await httpClient.PostAsync (slackUrl, body);

                if (response.IsSuccessStatusCode) {
                    return true;
                } else {
                    log.LogWarning ($"Webhook send failed. Status code: {response.StatusCode}. Message: {await response.Content.ReadAsStringAsync()}");
                }
            } catch (Exception ex) {
                log.LogError ($"Webhook send failed: {ex.Message}");
            }

            // If we got here, something went wrong!
            return false;
        }

        public class SlackAttachment {
            private SlackAttachment () { }

            public string color { get; set; }
            public string title { get; set; }
            public string text { get; set; }
            public string markdown_in { get; set; }

            public static SlackAttachment Create (dynamic payload) {
                if (payload == null) {
                    throw new ArgumentNullException (nameof (payload));
                }

                var highlightAlert = HighlightAlert.Create (payload.problem.ToString ());

                var attachment = new SlackAttachment {
                    title = $"{highlightAlert.alertIcon} {payload.alertSummary}",
                    text = $"{payload.problem} - <{payload.linkUrl}|More information>",
                    color = highlightAlert.color,
                    markdown_in = "title, text"
                };

                return attachment;
            }
        }

        public class SlackMessage {
            private SlackMessage () { }

            public SlackMessage (string text, string mrkdown) {
                this.text = text;
                this.mrkdown = mrkdown;
            }
            public string text { get; set; }
            public IEnumerable<SlackAttachment> attachments { get; set; }
            public string mrkdown { get; set; }

            public override string ToString () {
                return JsonConvert.SerializeObject (this);
            }

            public static SlackMessage Create (dynamic payload) {
                if (payload == null) {
                    throw new ArgumentNullException (nameof (payload));
                }

                var message = new SlackMessage {
                    text = $"Highlight Alert",
                    attachments = new SlackAttachment[] {
                    SlackAttachment.Create (payload)
                    },
                    mrkdown = "True"
                };

                return message;

            }
        }

        public class HighlightAlert {
            private HighlightAlert () { }

            public string color { get; set; }
            public string alertIcon { get; set; }

            public static HighlightAlert Create (string problem) {
                if (string.IsNullOrWhiteSpace (problem)) {
                    throw new ArgumentNullException (nameof (problem));
                }

                string color = string.Empty;
                string alertIcon = ":grey_question:";

                string pattern = @"(\w+) alert (\w+)";
                Match match = Regex.Match (problem.ToLower (), pattern,
                    RegexOptions.IgnoreCase);

                if (match.Success) {
                    var highlightColor = match.Groups[1].Value;
                    var highlightDirection = match.Groups[2].Value;

                    if (highlightDirection == "raised") {
                        alertIcon = ":warning:";

                        if (highlightColor == "red") {
                            color = "danger";
                        } else if (highlightColor == "amber") {
                            color = "warning";
                        }
                    } else if (highlightDirection == "cleared") {
                        color = "good";
                        alertIcon = ":heavy_check_mark:";
                    }
                }

                return new HighlightAlert { color = color, alertIcon = alertIcon };
            }
        }
    }
}