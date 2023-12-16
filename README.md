# docker-compose.yml 
1. [**Postgresql**](#01)
2. [**MySql**](#02)
3. [**Команды**](#03)

## 01. Postgresql<a name="01"></a>

### 1.1 Папка в папке
Если ваш код лежит в отдельной папке с именем вашего проекта:
![image](https://github.com/AleksandrKonst/Administration/assets/40522320/99c8db76-0ba9-405e-9692-4d9138586933)

#### Dockerfile
Файл для вашего API
```Dockerfile
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
```

#### docker-compose.yml
```docker-compose.yml
services:
  api:
    container_name: api_container
    image: webapplication1
    build:
      context: .
      dockerfile: WebApplication1/Dockerfile
    ports:
      - "8080:80"
    networks:
      - services-network
    depends_on:
      - "db"
    environment:
      DATABASE_CONNECT: Server=postgres_container;Port=5432;Database=DataBaseName;User Id=postgres;Password=1243

  db:
    container_name: postgres_container
    image: postgres
    restart: always
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: 1243
      POSTGRES_DB: DataBaseName
      PGDATA: "/var/lib/postgresql/data/pgdata"
    volumes:
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
      - test-data-db:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    networks:
      - services-network
  
  pgadmin:
    container_name: pgadmin_container
    image: dpage/pgadmin4:7.2
    restart: always
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@miit.com
      PGADMIN_DEFAULT_PASSWORD: 1243
    volumes:
      - pgadmin-data:/var/lib/pgadmin
    ports:
      - "5050:80"
    networks:
      - services-network
    depends_on:
      - "db"

networks:
  services-network:
    driver: bridge

volumes:
  test-data-db:
  pgadmin-data:
```

#### init.sql
SQL файл для создания таблиц

```sql
CREATE TABLE weather (
    id SERIAL NOT NULL,
    name character varying NOT NULL
);

INSERT INTO weather (id, name) VALUES (1, 'Пасмурно'), (2, 'Облачно');
```

#### ASP.NET
```c#
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataBaseNameContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("DATABASE_CONNECT")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
```
