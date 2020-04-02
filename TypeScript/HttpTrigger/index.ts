import { AzureFunction, Context, HttpRequest } from "@azure/functions";

import request = require("request");

const httpTrigger: AzureFunction = async (
	context: Context,
	req: HttpRequest
): Promise<void> => {
	context.log("HTTP trigger function processed a request.");
	// Local execution setting in local.settings.json
	// Azure execution setting configuration -> Applicaiton settings
	const slackWebHook = process.env.SLACK_WEBHOOK_URL;

	interface IAlertStatus {
		color?: string;
		alertIcon?: string;
	}

	function getStatus(problem: string): IAlertStatus {
		// Example: "problem": "Link-Availability - Red alert raised"
		const status = {} as IAlertStatus;
		const values = problem.match(/(\w+) alert (\w+)/i);
		if (values) {
			const highlightColor = values[1].toLowerCase();
			const highlightDirection = values[2].toLowerCase();
			if (highlightDirection === "raised") {
				status.alertIcon = ":warning:";
				if (highlightColor === "red") {
					status.color = "danger";
				} else if (highlightColor === "amber") {
					status.color = "warning";
				}
			} else if (highlightDirection === "cleared") {
				status.alertIcon = ":heavy_check_mark:";
				status.color = "good";
			} else {
				status.alertIcon = ":grey_question:";
			}
		}
		return status;
	}

	function messageAttachment(message: string): object {
		const attachment = [];
		const jsonMessage = JSON.parse(message);
		const status = getStatus(jsonMessage.problem);
		attachment.push({
			color: status.color,
			title: status.alertIcon + " " + jsonMessage.alertSummary,
			text:
				jsonMessage.problem +
				" - <" +
				jsonMessage.linkUrl +
				"|More Information>",
			// eslint-disable-next-line @typescript-eslint/camelcase
			mrkdown_in: ["title", "text"]
		});
		return attachment;
	}

	if (req.body) {
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
			(error, response, body) => {
				if (error) {
					context.log("request error: ", error);
				}
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
