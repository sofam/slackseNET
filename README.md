# slackseNET

Our beloved chat bot based on a 500 year old modified MegaHAL implementation got left behind in the move to Slack.
This prompted a project to quickly wrap the old MegaHAL binary in something more modern, handling the encoding to and from ISO-8859-1 and integrating it all into a bot on Slack.

This is the first iteration. It will autosave the brain every 5 minutes.

The MegaHAL binary requires to be run on Linux, the modified source for MegaHAL is included in the SVETSE directory as is the binary.

## Configuration

Configuration is handled by three environment variables:
`SLACKSE_TOKEN` is your Slack token
`SLACKSE_CHANNEL` is your channel name
`SLACKSE_SendMessageToChannelOnSave` is if the bot should write to the channel every time it saves it's brain (very verbose), defaults to `false`

