FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ParserExchanger/ParserExchanger.csproj ParserExchanger/
RUN dotnet restore ParserExchanger/ParserExchanger.csproj
COPY ParserExchanger/YouTrackInput.json ParserExchanger/
COPY . .
WORKDIR /src/ParserExchanger
RUN dotnet build ParserExchanger.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish ParserExchanger.csproj -c Release -o /app
WORKDIR /src
COPY ParserExchanger/YouTrackInput.json /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ParserExchanger.dll"]
