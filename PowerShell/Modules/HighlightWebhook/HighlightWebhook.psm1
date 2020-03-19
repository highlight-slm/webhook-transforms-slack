function Get-SlackFormatMessage {
    param (
        [Parameter()]
        [hashtable] $Payload
    )
    $attachement = @(_CreateAttachment -Payload $Payload)
    $message = @{mrkdwn="True";text="Highlight Alert";attachments=$attachement} | ConvertTo-Json

    return $message
}

function _CreateAttachment {
    param (
        [Parameter()]
        [hashtable] $Payload
    )
    $attachement = @{}
    if ($Payload.problem) {
        $status = _GetStatus -Problem $Payload.problem
        $title = $status.direction + " " + $payload.alertSummary
        $text = $Payload.problem + " - <" + $payload.linkUrl + "|More information>"
        $attachement = @{color=$status.color;title=$title;text=$text;mrkdown_in="title, text"}
    }
    return $attachement
}

function _GetStatus {
    param (
        [Parameter()]
        [string] $Problem
    )
    # Example: "problem": "Link-Availability - Red alert raised"
    $Problem -match '(\w+) alert (\w+)'  # Created $matches automatically
    $highlightAlert = ($matches.1).ToLower()
    $highlightAlertDirection = ($matches.2).ToLower()
    if ($highlightAlertDirection -eq "raised") {
        if ($highlightAlert -eq "red") {
            $status = @{color="danger";direction=":warning:"}
        }
        elseif ($highlightAlert -eq "amber") {
            $status = @{color="warning";direction=":warning:"}
        }
        else {
            $status = @{color="";direction=":warning:"}
        }
    }
    elseif ($highlightAlertDirection -eq "cleared") {
        $status = @{color="good";direction=":heavy_check_mark:"}
    }
    else {
        $status = @{color="";direction=":grey_question:"}
    }
    return $status
}

function Send-SlackMessage {
    param (
        [Parameter()]
        [string]$SlackMessage,
        [Parameter()]
        [string]$SlackUrl
    )
    Write-Debug("Slack WebHook URL: $SlackUrl")
    Write-Debug("Slack Message (json): $SlackUrl")

    return Invoke-WebRequest -Method 'Post' -Uri $SlackUrl -Body $SlackMessage
}

Export-ModuleMember -Function Get-SlackFormatMessage
Export-ModuleMember -Function Send-SlackMessage