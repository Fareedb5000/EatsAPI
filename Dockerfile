FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EatsAPI.csproj", "./"]
RUN dotnet restore "./EatsAPI.csproj"

COPY . .
RUN dotnet publish "./EatsAPI.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=10000
EXPOSE 10000
USER $APP_UID

ENTRYPOINT ["sh", "-c", "exec dotnet EatsAPI.dll --urls http://0.0.0.0:${PORT:-10000}"]
