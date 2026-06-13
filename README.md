# NetGuard GT — API de Gestión de Incidentes de Red

Prototipo de **API REST** para gestionar los incidentes de red de **NetGuard GT** (proveedor de
telecomunicaciones). Reemplaza el manejo actual con Excel y llamadas por un flujo controlado:
registro de incidentes con **SLA automático**, asignación a técnicos con **reglas de negocio**,
**historial de cambios**, **escalamiento automático** y **reportes de cumplimiento de SLA**.

> Proyecto académico — **Análisis de Sistemas I**. Desarrollado en **C# / .NET 8**.

---

## 1. Stack técnico

| Componente | Tecnología |
|---|---|
| Lenguaje / framework | C# · **.NET 8** |
| API | ASP.NET Core Web API (controladores) |
| Persistencia | Entity Framework Core 8 + **SQLite** |
| Documentación / pruebas manuales | **Swagger / OpenAPI** (Swashbuckle) |
| Pruebas unitarias | **xUnit** + EF Core InMemory |
| Despliegue | **Docker** + **Render.com** |

La aplicación está organizada **en capas**, con la **lógica de negocio en los Services**
(los controladores solo orquestan la petición y devuelven el código HTTP correcto).

---

## 2. Requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Opcional) Docker, solo si se desea construir la imagen para Render.

> El proyecto incluye un `global.json` que fija el SDK a la línea **8.0**, por lo que si tienes
> varias versiones de .NET instaladas, se usará .NET 8 automáticamente.

No hace falta instalar ni configurar ninguna base de datos: **SQLite se crea sola** al arrancar
(archivo `netguard.db`) y se siembra con datos de ejemplo.

---

## 3. Cómo ejecutar localmente

Desde la carpeta raíz del proyecto:

```bash
dotnet run --project src/NetGuardGT.Api
```

Al iniciar:

1. Se crea el archivo `netguard.db` (si no existe) con todo el esquema.
2. Se **siembran** 12 técnicos y 5 incidentes de ejemplo.
3. El servidor queda escuchando (por defecto en **http://localhost:5262**).

> Para empezar de cero, basta con **borrar `netguard.db`** y volver a ejecutar: se recreará y
> se volverá a sembrar.

---

## 4. Cómo abrir Swagger

Con la aplicación corriendo, abre en el navegador:

```
http://localhost:5262/swagger
```

La raíz (`http://localhost:5262/`) **redirige automáticamente** a Swagger. Desde ahí puedes
probar todos los endpoints sin herramientas externas.

---

## 5. Cómo ejecutar las pruebas

```bash
dotnet test
```

Las **20 pruebas** cubren la lógica de negocio: cálculo de SLA, máximo de 3 activos, especialidad
coincidente, flujo unidireccional de estados, escalamiento automático, historial y reporte de SLA.

---

## 6. Reglas de negocio implementadas

| # | Regla | Dónde vive |
|---|---|---|
| 1 | **SLA por severidad**: `FechaLimite = FechaReporte + SLA`. Crítico 1h · Urgente 2h · Alta 4h · Media 8h · Baja 24h. | `Services/PoliticaSla.cs` |
| 2 | **Máximo 3 activos** por técnico (estado ≠ Cerrado). Si se supera → **422**. | `IncidenteService.ValidarCupoAsync` |
| 3 | **Flujo unidireccional**: Registrado → Asignado → EnProgreso → Resuelto → Cerrado. Sin retroceder ni saltar → **422**. | `Services/TransicionesEstado.cs` |
| 4 | **Reasignación / liberación** en cualquier momento salvo si está Cerrado. Revalida reglas 2 y 6. | `IncidenteService.ReasignarAsync` |
| 5 | **Escalamiento automático**: un Crítico/Urgente en Registrado por **+2h** pasa a Escalado. | `IncidenteService.EscalarVencidosAsync` |
| 6 | **Especialidad coincidente**: el técnico debe ser de la especialidad requerida → si no, **422**. | `IncidenteService.ValidarEspecialidad` |
| 7 | **Historial**: cada cambio (creación, asignación, avance, reasignación, escalamiento) se registra. | `IncidenteService.RegistrarHistorial` |
| 8 | **Reporte de SLA**: cumplidos, incumplidos, vencidos y % de cumplimiento. | `Services/ReporteService.cs` |

---

## 7. Estructura del proyecto

```
ANALISIS/
├── global.json                 # Fija el SDK a .NET 8
├── NetGuardGT.sln
├── Dockerfile                  # Imagen .NET 8 para Render
├── README.md
├── src/NetGuardGT.Api/
│   ├── Program.cs              # Arranque, DI, Swagger, PORT, crea+siembra la BD
│   ├── Models/                 # Entidades (Tecnico, Incidente, HistorialEstado) + Enums
│   ├── DTOs/                   # Contratos de entrada/salida (no exponen entidades)
│   ├── Data/                   # AppDbContext + DbSeeder
│   ├── Services/               # Lógica de negocio (reglas 1–8) ← el corazón
│   ├── Controllers/            # Capa HTTP (delgada)
│   └── Middleware/             # Manejo de errores → 404 / 422
└── tests/NetGuardGT.Tests/     # Pruebas unitarias (xUnit)
```

---

## 8. Endpoints y ejemplos

Base local: `http://localhost:5262`. Todos los cuerpos son JSON. Los enums se envían/reciben como
**texto** (p. ej. `"Critico"`, `"FibraOptica"`).

> En Windows PowerShell, `curl` es un alias de `Invoke-WebRequest`. Para usar los ejemplos tal cual,
> ejecuta `curl.exe ...` o, más cómodo, usa directamente **Swagger UI**.

### 8.1 Crear incidente — `POST /api/incidentes`
Calcula la `FechaLimite` automáticamente (Regla 1). Devuelve **201**.

```bash
curl -X POST http://localhost:5262/api/incidentes \
  -H "Content-Type: application/json" \
  -d '{
    "sitioRed": "Sitio-Quetzaltenango-01",
    "tipoIncidente": "Corte de fibra",
    "especialidadRequerida": "FibraOptica",
    "severidad": "Critico",
    "descripcion": "Corte total en el enlace principal"
  }'
```

### 8.2 Listar incidentes (con filtros) — `GET /api/incidentes`
Filtros opcionales: `estado`, `severidad`, `tecnicoId`, `sitio`.

```bash
curl "http://localhost:5262/api/incidentes"
curl "http://localhost:5262/api/incidentes?estado=Registrado&severidad=Critico"
curl "http://localhost:5262/api/incidentes?tecnicoId=1"
curl "http://localhost:5262/api/incidentes?sitio=Xela"
```

### 8.3 Detalle de un incidente — `GET /api/incidentes/{id}`
Devuelve **404** si no existe.

```bash
curl "http://localhost:5262/api/incidentes/1"
```

### 8.4 Asignar técnico — `PUT /api/incidentes/{id}/asignar`
Aplica reglas 2 (máx 3) y 6 (especialidad). Errores de negocio → **422**.

```bash
curl -X PUT http://localhost:5262/api/incidentes/1/asignar \
  -H "Content-Type: application/json" \
  -d '{ "tecnicoId": 1, "responsable": "Supervisor Turno A" }'
```

### 8.5 Avanzar estado — `PUT /api/incidentes/{id}/estado`
Respeta el flujo unidireccional (Regla 3). Un retroceso o salto → **422**.

```bash
curl -X PUT http://localhost:5262/api/incidentes/1/estado \
  -H "Content-Type: application/json" \
  -d '{ "nuevoEstado": "EnProgreso", "responsable": "Carlos Méndez", "motivo": "Técnico en sitio" }'
```

### 8.6 Reasignar o liberar — `PUT /api/incidentes/{id}/reasignar`
Con `tecnicoId` reasigna (revalida reglas 2 y 6); con `tecnicoId: null` **libera**.

```bash
# Reasignar a otro técnico
curl -X PUT http://localhost:5262/api/incidentes/1/reasignar \
  -H "Content-Type: application/json" \
  -d '{ "tecnicoId": 2, "responsable": "Supervisor", "motivo": "Balanceo de carga" }'

# Liberar (dejar sin técnico)
curl -X PUT http://localhost:5262/api/incidentes/1/reasignar \
  -H "Content-Type: application/json" \
  -d '{ "tecnicoId": null, "responsable": "Supervisor", "motivo": "Pendiente de reasignar" }'
```

### 8.7 Escalamiento automático — `POST /api/incidentes/escalar`
Evalúa todos los incidentes y escala los Crítico/Urgente vencidos (Regla 5).

```bash
curl -X POST http://localhost:5262/api/incidentes/escalar
```

### 8.8 Historial — `GET /api/incidentes/{id}/historial`
Lista de cambios del incidente (Regla 7).

```bash
curl "http://localhost:5262/api/incidentes/1/historial"
```

### 8.9 Reporte de SLA — `GET /api/reportes/sla`
Totales, cumplidos, incumplidos, vencidos abiertos y % de cumplimiento (Regla 8).

```bash
curl "http://localhost:5262/api/reportes/sla"
```

### 8.10 Técnicos — `GET /api/tecnicos`

```bash
curl "http://localhost:5262/api/tecnicos"
```

### 8.11 Carga de trabajo — `GET /api/tecnicos/carga`
Activos, cupos disponibles y disponibilidad de cada técnico (Regla 2).

```bash
curl "http://localhost:5262/api/tecnicos/carga"
```

---

## 9. Datos de ejemplo (seed)

Al arrancar se crean:

- **12 técnicos** (4 de Fibra Óptica, 4 de Microondas, 4 de Sistemas Eléctricos).
- **5 incidentes** en distintos estados/severidades, pensados para probar de inmediato:
  - Un **Crítico** en Registrado de hace 3h → candidato a **escalamiento** (prueba 8.7).
  - Uno **Resuelto a tiempo** (cumple SLA) y otro **resuelto tarde** (incumple) → para el reporte 8.9.
  - Uno **Asignado** dentro de plazo y uno **EnProgreso** ya vencido.

---

## 10. Despliegue en Render.com (Docker)

La app está lista para desplegarse como **Web Service** en Render:

1. Sube el proyecto a un repositorio Git (GitHub/GitLab).
2. En Render: **New → Web Service**, conecta el repositorio.
3. Render detecta el **`Dockerfile`** y construye la imagen automáticamente.
4. La app lee la variable de entorno **`PORT`** que Render inyecta (configurado en `Program.cs`) y
   escucha en `0.0.0.0:$PORT`.
5. Al terminar, Swagger queda disponible en `https://<tu-servicio>.onrender.com/swagger`.

Para probar la imagen localmente con Docker:

```bash
docker build -t netguardgt .
docker run -e PORT=8080 -p 8080:8080 netguardgt
# Swagger: http://localhost:8080/swagger
```

> Nota: SQLite usa el sistema de archivos del contenedor (efímero en Render). Para un **prototipo**
> es suficiente, pues la BD se recrea y siembra en cada arranque. Para producción se migraría a una
> base administrada (p. ej. PostgreSQL).

---

## 11. Códigos HTTP

| Código | Significado en esta API |
|---|---|
| **200** | Operación exitosa. |
| **201** | Incidente creado. |
| **400** | Petición mal formada (validación de DTO). |
| **404** | Recurso no encontrado (incidente o técnico). |
| **422** | Violación de una regla de negocio (mensaje en español). |
