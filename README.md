# Plataforma de Créditos - Examen Parcial

Este repositorio contiene la implementación de la Plataforma de Créditos, abarcando modelos de datos con Entity Framework Core, lógica de filtros y catálogos, roles (Analista/Cliente), persistencia en Redis para caché y sesiones, comunicación asíncrona mediante WebSockets (SignalR) y encolamiento de mensajes con Cloud MQ (RabbitMQ).

## 🚀 Correr el proyecto en Local

Para ejecutar el proyecto en tu entorno local, sigue los siguientes pasos:

1. **Clonar y restaurar paquetes:**
   ```bash
   git clone <URL_DEL_REPOSITORIO>
   cd PlataformaCreditos
   dotnet restore
   ```

2. **Aplicar Migraciones y crear Base de Datos SQLite:**
   ```bash
   dotnet ef database update
   ```
   *(Asegúrate de que la cadena de conexión en `appsettings.json` diga `Data Source=app.db` para entorno de desarrollo).*

3. **Ejecutar infraestructura externa:**
   Debes contar con un servidor local de **Redis** (puerto `6379`) y **RabbitMQ** (puerto `5672`). Puedes usar Docker:
   ```bash
   docker run -d -p 6379:6379 --name redis redis
   docker run -d -p 5672:5672 -p 15672:15672 --name rabbitmq rabbitmq:3-management
   ```

4. **Ejecutar la Aplicación:**
   ```bash
   dotnet run
   ```

---

## ☁️ Variables de Entorno (Render)

Al desplegar este Web Service en [Render](https://render.com), debes configurar las siguientes variables de entorno para su correcto funcionamiento:

- `ASPNETCORE_ENVIRONMENT` = `Production`
- `ASPNETCORE_URLS` = `http://0.0.0.0:${PORT}`
- `ConnectionStrings__DefaultConnection` = `Data Source=/data/creditos.db`
- `Redis__ConnectionString` = `<TU_URL_DE_REDIS_EXTERNO>` *(Ej: un servicio administrado de Redis)*
- `RabbitMQ__ConnectionString` = `<TU_URL_DE_CLOUDAMQP>` *(Ej: amqps://user:pass@host/vhost)*
- `RabbitMQ__QueueName` = `solicitudes.notificaciones`
- `RabbitMQ__ConsumerEnabled` = `true`

---

## 💾 Persistencia de Base de Datos SQLite en Render

En los entornos efímeros de Render, cualquier archivo que se cree en disco (como `app.db`) se borrará con cada reinicio o despliegue. Para evitar la pérdida de los datos con SQLite:

1. Ingresa a la configuración (Settings) de tu Web Service en el dashboard de Render.
2. Desplázate hasta la sección **Disks**.
3. Añade un nuevo disco (Add Disk).
4. Configúralo con:
   - **Name:** `db-data` *(o el nombre que prefieras)*
   - **Mount Path:** `/data`
   - **Size:** `1 GB` *(Suficiente para SQLite)*
5. Finalmente, en la sección de "Environment Variables", actualiza la cadena de conexión para que apunte al disco montado:
   - `ConnectionStrings__DefaultConnection` = `Data Source=/data/creditos.db`

---

## 💻 Comando de Inicio en Render

En un Web Service de Render, el sistema asigna el puerto mediante la variable de entorno nativa `$PORT`. Para que Kestrel en ASP.NET Core la tome en cuenta en entornos Linux, el "Start Command" debe inyectar la URL antes del comando de ejecución .NET:

**Start Command:**
```bash
export ASPNETCORE_URLS="http://0.0.0.0:$PORT" && dotnet PlataformaCreditos.dll
```

---

## 🔄 Reenvío de Mensajes y Fallas en RabbitMQ

El servicio implementa estrategias de **resiliencia e idempotencia** mediante las siguientes reglas:

1. **Fallas en la Publicación (Publisher Confirms):**  
   Durante la creación de una solicitud (Analista/Cliente), usamos `ConfirmSelect()` y `WaitForConfirmsOrDie()`. Si la publicación en RabbitMQ falla, atrapamos la excepción para **NO** eliminar la solicitud recién registrada en la BD. La solicitud queda pendiente de ser evaluada, evitando pérdida de datos operacionales.

2. **Reenvío / Reencolado:**  
   Si en un escenario adverso los mensajes no se encolaron, el mismo ID de Solicitud puede volver a ser enviado mediante un script de recuperación. 

3. **Idempotencia en Consumo:**  
   En caso de reenvío de mensajes repetidos o múltiples reintentos de red, el consumidor siempre verifica el `MessageId` (UUID) o `SolicitudId` contra la base de datos `Notificaciones` antes de crear un registro. 
   - Si el `MessageId` ya existe, el consumidor hace `BasicAck` descartándolo silenciosamente para no registrar duplicados, asegurando que solo haya una notificación real por mensaje emitido.
