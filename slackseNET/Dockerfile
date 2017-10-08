FROM microsoft/dotnet:2.0-sdk-jessie


COPY bin/Debug/netcoreapp2.0/* /opt/slackseNET/
COPY SVETSE/megahal /opt/slackseNET/SVETSE/megahal

WORKDIR /opt/slackseNET/
ENTRYPOINT [ "dotnet", "/opt/slackseNET/slackseNET.dll" ]