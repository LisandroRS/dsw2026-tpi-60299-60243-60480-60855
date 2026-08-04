# TPI Backend - Sistema de Turnos Médicos

Trabajo Práctico Integrador de la materia Desarrollo de Software 2026.

El proyecto consiste en una API para administrar especialidades, médicos, disponibilidades y citas médicas. También permite el inicio de sesión de administradores y pacientes mediante JWT.

## Integrantes

- Adler Lautaro - Legajo 60243 - 3K3
- Fonts Felipe José - Legajo 60855 - 3K3
- Martorell Francisco - Legajo 60480 - 3K3
- Rivas Suñen Lisandro - Legajo 60299 - 3K3

## Requisitos para ejecutar el proyecto

Para trabajar con el proyecto se necesita:

- Visual Studio con la carga de trabajo de ASP.NET y desarrollo web.
- .NET 10 SDK.
- SQL Server LocalDB.
- Git, solamente si se quiere clonar o actualizar el repositorio.

Visual Studio normalmente instala LocalDB junto con las herramientas de desarrollo de .NET.

## Configuración y ejecución local

### 1. Abrir la solución

Abrir el archivo:

```text
dsw2026-tpi-60299-60243-60480-60855/Dsw2026Tpi.slnx
```

Una vez abierta la solución, verificar que `Dsw2026Tpi.Api` sea el proyecto de inicio.

### 2. Revisar la conexión a la base de datos

La conexión local se encuentra en:

```text
Dsw2026Tpi.Api/appsettings.Development.json
```

El proyecto está preparado para utilizar SQL Server LocalDB con una base llamada `Dsw2026Tpi`. En una instalación normal de Visual Studio no debería ser necesario modificar esta configuración.

### 3. Crear o actualizar la base de datos

En Visual Studio, abrir:

```text
Herramientas > Administrador de paquetes NuGet > Consola del Administrador de paquetes
```

Seleccionar `Dsw2026Tpi.Data` como proyecto predeterminado y ejecutar:

```powershell
Update-Database -Context AuthenticationDbContext
Update-Database -Context Dsw2026TpiDbContext
```

El primer comando crea o actualiza las tablas relacionadas con usuarios y roles. El segundo hace lo mismo con especialidades, médicos, pacientes, disponibilidades, slots y citas.

Ambos comandos deben finalizar con el mensaje:

```text
Done.
```

### 4. Ejecutar la API

En Visual Studio, elegir el perfil `https` y ejecutar el proyecto.

Swagger debería abrirse automáticamente. También se puede ingresar manualmente en:

```text
https://localhost:7075/swagger/index.html
```

La dirección HTTP alternativa es:

```text
http://localhost:5278/swagger/index.html
```

Swagger permite probar todos los endpoints sin necesitar otra aplicación.

## Primer uso

### Crear un administrador

Si todavía no existe un administrador, usar:

```http
POST /api/auth/admin/register
```

Ejemplo:

```json
{
  "email": "admin@admin.com",
  "password": "Admin123!"
}
```

La contraseña debe tener al menos 8 caracteres e incluir mayúscula, minúscula, número y carácter especial.

### Iniciar sesión como administrador

Usar:

```http
POST /api/auth/admin/login
```

```json
{
  "email": "admin@admin.com",
  "password": "Admin123!"
}
```

La respuesta contiene un token JWT. Para utilizar los endpoints protegidos desde Swagger:

1. Presionar el botón `Authorize`.
2. Escribir `Bearer` seguido de un espacio y el token.
3. Confirmar la autorización.

Ejemplo:

```text
Bearer eyJhbGciOiJIUzI1NiIs...
```

### Iniciar sesión como paciente

Usar:

```http
POST /api/auth/patient/login
```

```json
{
  "email": "paciente@gmail.com",
  "dni": 40123456
}
```

El paciente no necesita registrarse previamente. Si es su primer ingreso, el sistema lo crea automáticamente y devuelve un token con el rol `PACIENTE`.

## Endpoints implementados

### Autenticación

| Método | Ruta | Uso |
|---|---|---|
| POST | `/api/auth/admin/register` | Crea un usuario administrador. |
| POST | `/api/auth/admin/login` | Inicia sesión como administrador y devuelve un JWT. |
| POST | `/api/auth/patient/login` | Inicia sesión como paciente. Si no existe, lo registra automáticamente. |

### Especialidades

Estos endpoints requieren un token de administrador.

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/specialties?pageSize=10&pageIndex=0&name=` | Lista especialidades con paginación y filtro opcional por nombre. |
| GET | `/api/specialties/{id}` | Obtiene una especialidad por su identificador. |
| POST | `/api/specialties` | Crea una especialidad. |
| PUT | `/api/specialties/{id}` | Actualiza una especialidad existente. |
| DELETE | `/api/specialties/{id}` | Realiza la baja lógica de una especialidad. |

Ejemplo para POST y PUT:

```json
{
  "name": "Cardiología",
  "description": "Atención cardiológica general"
}
```

Las especialidades eliminadas no aparecen en los listados.

### Médicos

Estos endpoints requieren un token de administrador.

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/doctors?pageSize=10&pageIndex=0&name=` | Lista médicos con paginación y filtro opcional por nombre. |
| POST | `/api/doctors` | Crea un médico. |
| PUT | `/api/doctors/{id}` | Actualiza un médico. |
| DELETE | `/api/doctors/{id}` | Realiza la baja lógica de un médico. |
| GET | `/api/doctors/{id}/availabilities` | Muestra la disponibilidad mensual cargada para un médico. |

Ejemplo para POST y PUT:

```json
{
  "name": "Federico Rivas",
  "licenseNumber": "MP12345",
  "specialtyId": "GUID-DE-LA-ESPECIALIDAD"
}
```

Para crear un médico primero debe existir la especialidad indicada.

### Disponibilidades

Estos endpoints requieren un token de administrador.

| Método | Ruta | Uso |
|---|---|---|
| POST | `/api/availabilities` | Carga disponibilidades para el resto del mes actual. |
| PUT | `/api/availabilities` | Reemplaza las disponibilidades futuras que no estén reservadas. |

Ejemplo:

```json
{
  "doctorId": "GUID-DEL-MEDICO",
  "days": [
    {
      "day": "LUNES",
      "startTime": "09:00",
      "endTime": "12:00"
    },
    {
      "day": "MIERCOLES",
      "startTime": "14:00",
      "endTime": "17:00"
    }
  ]
}
```

El sistema crea un slot por cada intervalo de 30 minutos y no genera slots para los feriados definidos en el archivo JSON del proyecto.

### Citas

#### Operaciones del paciente

| Método | Ruta | Uso |
|---|---|---|
| POST | `/api/appointments` | Reserva una cita en un slot disponible. |
| GET | `/api/appointments/patient?dni=40123456` | Lista las citas activas y futuras del paciente autenticado. |
| DELETE | `/api/appointments/{id}` | Cancela una cita reservada y vuelve a liberar el slot. |

Ejemplo de reserva:

```json
{
  "doctorId": "GUID-DEL-MEDICO",
  "availabilitySlotId": "GUID-DEL-SLOT",
  "patient": {
    "dni": 40123456
  },
  "reason": "Consulta médica general"
}
```

El paciente debe utilizar el mismo DNI y email con los que inició sesión. Para las pruebas actuales, el identificador del slot disponible puede consultarse directamente en la tabla `AvailabilitySlots` de la base de datos.

#### Operaciones del administrador

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/appointments?date=2026-08-03` | Lista las citas de una fecha determinada. |
| GET | `/api/appointments/search` | Realiza una búsqueda combinada y paginada. |

Ejemplo de búsqueda:

```text
/api/appointments/search?pageSize=10&pageIndex=0
```

Los filtros son opcionales y pueden combinarse. Por ejemplo:

```text
/api/appointments/search?pageSize=10&pageIndex=0&doctorId=GUID-DEL-MEDICO&date=2026-08-03
```

## Estados utilizados

Los slots pueden encontrarse en los estados:

- `AVAILABLE`: disponible.
- `BOOKED`: reservado.
- `BLOCKED`: bloqueado.

Las citas pueden encontrarse en los estados:

- `BOOKED`: reservada.
- `CANCELLED`: cancelada.
- `ATTENDED`: atendida.
- `NO_SHOW`: el paciente no asistió.

## Consideraciones generales

- La mayoría de los endpoints requieren un token JWT.
- Los endpoints administrativos necesitan el rol `ADMINISTRADOR`.
- Las reservas y consultas del paciente necesitan el rol `PACIENTE`.
- Los DELETE implementados realizan bajas lógicas; los registros no se eliminan físicamente.
- La API utiliza rate limiting. Si se supera el límite de solicitudes, responde con el código HTTP 429.
- Los tokens tienen una duración configurada de 60 minutos. Cuando vencen, es necesario iniciar sesión nuevamente.

## Pruebas

El proyecto incluye pruebas unitarias para distintos caminos de creación de especialidades y médicos. Se pueden ejecutar desde el Explorador de pruebas de Visual Studio.
