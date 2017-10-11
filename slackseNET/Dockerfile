FROM microsoft/dotnet:2.0-sdk-jessie AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet add package SlackAPI --version 1.0.6 --source https://www.myget.org/F/slackapi/api/v3/index.json
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.0-runtime-jessie 
WORKDIR /app
COPY --from=build-env /app/out ./
COPY SVETSE/megahal ./SVETSE/megahal

ENTRYPOINT [ "dotnet", "slackseNET.dll" ]