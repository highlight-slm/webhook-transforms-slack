"""Example Highlight to Slack Message webhook transform using Azure Function"""
import logging
import os
import re
from collections import namedtuple
import requests


import azure.functions as func


async def main(req: func.HttpRequest) -> func.HttpResponse:
    """Main function called by Azure inbound httpRequest.

    :param req: Azure HttpRequest object
    :return: HttpResponse object
    """
    logging.info("Python HTTP trigger function processed a request.")
    # Local execution setting in local.settings.json
    # Azure execution setting configuration -> Application settings
    slack_webhook = os.environ["SLACK_WEBHOOK_URL"]

    incoming_payload = req.get_json()
    if incoming_payload:
        slack_message = create_message(incoming_payload)

        post_response = post_message(slack_message, slack_webhook)
        if post_response.status_code == 200:
            return func.HttpResponse("OK", status_code=post_response.status_code)
        return func.HttpResponse(
            f"Error sending webhook. Error {post_response.status_code}: {post_response.text}",
            status_code=post_response.status_code,
        )
    return func.HttpResponse("Error: No Data", status_code=400)


def create_message(payload):
    """Create Slack message.

    :param payload: Highlight webhook json payload
    """
    return {
        "mrkdwn": True,
        "text": "Highlight Alert",
        "attachments": create_attachment(payload),
    }


def create_attachment(payload):
    """Create Slack message attachment with additional problem information.

    :param payload: Highlight webhook json payload
    :return: attachment (array)
    """
    status = get_status(payload.get("problem"))
    return [
        {
            "color": status.color,
            "title": f"{status.icon} {payload.get('alertSummary')}",
            "text": f"{payload.get('problem')} - <{payload.get('linkUrl')}|More information>",
            "mrkdown_in": "title, text",
        }
    ]


def get_status(problem):
    """Parse the Highlight problem text into the status/color and the direction.

    :param problem: Highlight problem json string
                    e.g. "problem": "Link-Availability - Red alert raised"
    :return: alert_status, named tuple for color and a severity icon
    """
    # Example: "problem": "Link-Availability - Red alert raised"
    alert_status = namedtuple("alert_status", ["color", "icon"])

    values = re.search(r"(\w+) alert (\w+)", problem, re.IGNORECASE)
    highlight_color = values[1].lower()
    highlight_direction = values[2].lower()
    if highlight_direction == "raised":
        if highlight_color == "red":
            status = alert_status(color="danger", icon=":warning:")
        elif highlight_color == "amber":
            status = alert_status(color="warning", icon=":warning:")
        else:
            status = alert_status(color=None, icon=":warning:")
    elif highlight_direction == "cleared":
        status = alert_status(color="good", icon=":heavy_check_mark:")
    else:
        status = alert_status(color=None, icon=":grey_question:")

    return status


def post_message(message, webhook_url):
    """Post message to slack webhook endpoint.

    :param message: json encoded slack message
    :param webhook_url: outbound url to post message to
    :return: response from post
    """
    return requests.post(json=message, url=webhook_url)
