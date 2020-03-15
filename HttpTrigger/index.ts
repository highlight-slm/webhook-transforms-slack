import { AzureFunction, Context, HttpRequest } from "@azure/functions";

import request = require("request");

// tslint:disable-next-line: only-arrow-functions
const httpTrigger: AzureFunction = async function(context: Context, req: HttpRequest): Promise<void> {
  context.log("HTTP trigger function processed a request.");
  // Local execution setting in local.settings.json
  // Azure execution setting configuration -> Applicaiton settings
  const slackWebHook: string = process.env.SLACK_WEBHOOK_URL;

  interface AlertStatus {
    color?: string
    direction?: string
  }

  if (req.body) {
    function getStatus(problem: string) {
      // Example: "problem": "Link-Availability - Red alert raised"

      const values: any = problem.match(/(\w+) alert (\w+)/i);
      const highlightAlert: string = values[1].toLowerCase();
      const highlightAlertDirection: string = values[2].toLowerCase();
      const status = {} as AlertStatus
      if (highlightAlertDirection === "raised") {
        status.direction = ":warning:";
        if (highlightAlert === "red") {
          status.color = "danger";
        } else if (highlightAlert === "amber") {
          status.color = "warning";
        }
      } else if (highlightAlertDirection === "cleared") {
        status.direction = ":heavy_check_mark:";
        status.color = "good";
      } else {
        status.direction = ":grey_question:";
      }
      return status
    };

    function messageAttachment(message: string) {
      const attachment: object[] = [];
      const jsonMessage: any = JSON.parse(message);
      const status: AlertStatus = getStatus(jsonMessage.problem);
      attachment.push({
        color: status.color,
        title: status.direction + " " + jsonMessage.alertSummary,
        text: jsonMessage.problem + " - <" + jsonMessage.linkUrl + "|More Information>",
        mrkdown_in: ["title"]
      });
      return attachment;
    };

    const msg = {
      mrkdwn: true,
      text: "Highlight Alert",
      attachments: messageAttachment(req.rawBody)
    };

    request(
      {
        method: "POST",
        uri: slackWebHook,
        json: true,
        body: msg
      },
      // tslint:disable-next-line: only-arrow-functions
      function(error, response, body) {
        if (response.statusCode === 200) {
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
