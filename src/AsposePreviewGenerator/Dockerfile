ARG TASKAGENT_VERSION=latest
FROM sensenetcsp/sn-taskagent:${TASKAGENT_VERSION} AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "AsposePreviewGenerator/AsposePreviewGenerator.csproj"
WORKDIR "/src/AsposePreviewGenerator"
RUN dotnet build "AsposePreviewGenerator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AsposePreviewGenerator.csproj" -c Release -o /app/publish

FROM base AS final
RUN apt-get update \
    && apt-get install -y libc6-dev \
    && apt-get install -y libgdiplus \
    && apt-get install -y libx11-dev \    
    && apt-get install -y libfontconfig1
RUN mkdir /app/TaskExecutors/AsposePreviewGenerator -p
WORKDIR /app/TaskExecutors/AsposePreviewGenerator
COPY --from=publish /app/publish .
WORKDIR /app
ENTRYPOINT ["dotnet", "SenseNetTaskAgent.dll"]