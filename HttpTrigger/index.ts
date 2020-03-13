import { AzureFunction, Context, HttpRequest } from "@azure/functions";

const request = require("request");

const httpTrigger: AzureFunction = async function(context: Context, req: HttpRequest): Promise<void> {
  context.log("HTTP trigger function processed a request.");
  // Local execution setting in local.settings.json
  // Azure execution setting configuration -> Applicaiton settings
  const url = process.env["SLACK_WEBHOOK_URL"];

  if (req.body) {
    var getStatus = function(problem) {
      // Example: "problem": "Link-Availability - Red alert raised"

      const values = problem.match(/(\w+) alert (\w+)/i);
      const hl_color = values[1].toLowerCase();
      const hl_direction = values[2].toLowerCase();
      var color = "";
      var direction = "";
      if (hl_direction === "raised") {
        direction = ":warning:";
        if (hl_color === "red") {
          color = "danger";
        } else if (hl_color === "amber") {
          color = "warning";
        }
      } else if (hl_direction === "cleared") {
        direction = ":heavy_check_mark:";
        color = "good";
      } else {
        direction = ":grey_question:";
      }
      return {
        color: color,
        direction: direction
      };
    };

    var messageAttachment = function(message) {
      var attachment = [];
      const json_message = JSON.parse(message);
      var status = getStatus(json_message.problem);
      attachment.push({
        color: status.color,
        title: status.direction + " " + json_message.alertSummary,
        text: json_message.problem + " - <" + json_message.linkUrl + "|More Information>",
        mrkdown_in: ["title"]
      });
      return attachment;
    };

    var msg = {
      mrkdwn: true,
      text: "Highlight Alert",
      attachments: messageAttachment(req.rawBody)
    };

    request(
      {
        method: "POST",
        uri: url,
        json: true,
        body: msg
      },
      function(error, response, body) {
        if (response.statusCode == 200) {
          context.res = {
            body: "OK"
          };
        } else {
          context.res = {
            status: response.statusCode,
            body: "Error (" + response.statusCode + "):" + body
          };
        }
      }
    );
  } else {
    context.res = {
      status: 400, // Bad Request
      body: "Error: No Data"
    };
  }
  context.done();
};

export default httpTrigger;
