# webhook-transforms-slack

Azure Function to transform Highlight Webhook post into a slack message.

![Slack Notification](/images/slack_message.jpg)

Requires the Slack Webhook URL is configured as a Azure Function variable via Azure portal. The variable needs to be called `SLACK_WEBHOOK_URL`. When running locally the value is stored in `local.settings.json`.

![Azure Function Variable](/images/azure_function_variable.jpg)

## Examples implementations

* C# (CSharp)
* PowerShell
* Python
* TypeScript (Node.js)
