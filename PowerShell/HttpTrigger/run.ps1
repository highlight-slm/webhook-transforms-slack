using namespace System.Net

# Input bindings are passed in via param block.
param($Request, $TriggerMetadata)

# Write to the Azure Functions log stream.
Write-Host "PowerShell HTTP trigger function processed a request."

# Local execution setting in local.settings.json
# Azure execution setting configuration -> Application settings
$slackWebhook = $env:SLACK_WEBHOOK_URL

if ($Request.Body) {
    $slackMessage = Get-SlackFormatMessage -Payload $Request.Body
    $postResponse = Send-SlackMessage -SlackMessage $slackMessage -SlackUrl $slackWebhook

    if ($postResponse.StatusCode -eq 200) {
        Write-Host("Webhook successfully sent to Slack")
        $status = [HttpStatusCode]::OK
        $body = "OK"
    }
    else {
        Write-Host("Error sending Webhook")
        $status = $postResponse.StatusCode
        $body = "Error sending webhook. Error: " + $postResponse.Content
    }
}
else {
    Write-Host("Error empty incoming Webhook")
    $status = [HttpStatusCode]::BadRequest
    $body = "Error: No Data"
}

# Associate values to output bindings by calling 'Push-OutputBinding'.
Push-OutputBinding -Name Response -Value ([HttpResponseContext]@{
    StatusCode = $status
    Body = $body
})
