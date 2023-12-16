FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
EXPOSE 80

COPY WebApplication1/WebApplication1.csproj WebApplication1/
RUN dotnet restore "WebApplication1/WebApplication1.csproj"

COPY . ./
RUN dotnet publish WebApplication1/WebApplication1.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app/WebApplication1
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "WebApplication1.dll"]