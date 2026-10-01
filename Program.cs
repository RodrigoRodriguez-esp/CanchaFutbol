var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// ============================================================
// DATOS BASE (en memoria)
// ============================================================

var empresa = new Empresa(
    "Golazo Arena",
    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTt8g7r7HmnLEz2Ut2aLA9ropKq8p6fujJUYbYiplNmYQ&s=10",
    "Tu cancha, tu partido, tu momento.",
    "Complejo deportivo con canchas de grass sintético de última generación, iluminación LED y servicios para que solo te preocupes por jugar.",
    "Av. Giráldez 123, Huancayo, Junín",
    "+51 964 000 111",
    "reservas@golazoarena.pe",
    "Lunes a Domingo, 08:00 - 23:00"
);

var canchas = new List<Cancha>
{
    new(1, "Cancha Principal", "Fútbol 7", "Grass sintético", 14, 80m,  true,  true,
        "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRx122YQ991y7FnfzRUgwH49gfmYhouCVx8txXvCCT3Kg&s=10l",
        "Nuestra cancha estrella: grass sintético profesional, graderías y marcador electrónico."),
    new(2, "Cancha Norte",     "Fútbol 5", "Grass sintético", 10, 60m,  true,  false,
        "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQqXkQEcLYPZQuTVfZSXQc1v7FVQAiSN010jskUiSl7fA&s=10",
        "Ideal para partidos rápidos entre amigos. Iluminación LED nocturna."),
    new(3, "Cancha Techada",   "Fútbol 5", "Loza deportiva",  10, 70m,  true,  true,
        "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTiyro9MfohblYLHBC1RNuSDlKpEUQGYHHp6zznwCOb3Q&s=10",
        "Juega con lluvia o sol. Techada y con piso antideslizante."),
    new(4, "Cancha Grande",    "Fútbol 11", "Grass natural",  22, 180m, true,  false,
        "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQSp8AD-Mm3YSzj4BFQ20kVmoEu9wjgRvHXvP9nKmgehA&s=10",
        "Cancha reglamentaria para campeonatos y partidos de gran formato."),
};

var servicios = new List<Servicio>
{
    new(1, "Alquiler de cancha", "Principal",   "Reserva por hora de cualquiera de nuestras canchas.",            0m,   "⚽", "https://placehold.co/400x250?text=Alquiler"),
    new(2, "Campeonatos",        "Competencia", "Organizamos ligas y torneos relámpago con árbitros incluidos.",  250m, "🏆", "https://placehold.co/400x250?text=Campeonatos"),
    new(3, "Entrenamientos",     "Formación",   "Clases con entrenadores certificados para niños y adultos.",     40m,  "🎯", "https://placehold.co/400x250?text=Entrenamientos"),
    new(4, "Eventos",            "Competencia", "Cumpleaños, integraciones empresariales y eventos deportivos.",  300m, "🎉", "https://placehold.co/400x250?text=Eventos"),
    new(5, "Vestuarios",         "Comodidad",   "Vestuarios con duchas de agua caliente y lockers.",              0m,   "🚿", "https://placehold.co/400x250?text=Vestuarios"),
    new(6, "Iluminación nocturna","Comodidad",  "Iluminación LED para partidos de noche.",                        10m,  "💡", "https://placehold.co/400x250?text=Iluminacion"),
    new(7, "Estacionamiento",    "Comodidad",   "Estacionamiento vigilado para autos y motos.",                   5m,   "🅿️", "https://placehold.co/400x250?text=Parking"),
    new(8, "Alquiler de balón y chalecos","Extras","Balón oficial y juego de chalecos para 2 equipos.",           15m,  "🎽", "https://placehold.co/400x250?text=Extras"),
    new(9, "Cafetería",          "Extras",      "Bebidas, snacks y comida ligera para después del partido.",      0m,   "🥤", "https://placehold.co/400x250?text=Cafeteria"),
};

var promociones = new List<Promocion>
{
    new(1, "Happy Hour", "Descuento en horarios de mañana y tarde (08:00 a 16:00).", 20, "HAPPY20", "Lunes a Viernes", "2026-12-31", "https://placehold.co/500x250?text=Happy+Hour"),
    new(2, "Pack 3 horas", "Reserva 3 horas seguidas y obtén descuento en el total.", 15, "PACK3",   "Todos los días",  "2026-12-31", "https://placehold.co/500x250?text=Pack+3h"),
    new(3, "Cumpleañero", "Cancha gratis por 1 hora para el cumpleañero y su equipo.", 100, "BDAY",   "Con 10+ invitados","2026-11-30", "https://placehold.co/500x250?text=Cumple"),
};

// ============================================================
// HORARIOS (se generan automáticamente para los próximos 7 días)
// ============================================================

const int HORA_APERTURA = 8;
const int HORA_CIERRE = 23;
const int DIAS_ADELANTE = 7;

var horarios = new List<Horario>();
var diasGenerados = new HashSet<(int canchaId, DateOnly fecha)>();
var reservas = new List<Reserva>();
var siguienteHorarioId = 1;
var siguienteReservaId = 1;
var candado = new object();

DateTime AhoraLocal()
{
    try
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Lima"));
    }
    catch
    {
        return DateTime.UtcNow.AddHours(-5); // Perú = UTC-5
    }
}

void AsegurarHorarios()
{
    lock (candado)
    {
        var ahora = AhoraLocal();
        var hoy = DateOnly.FromDateTime(ahora);

        for (var d = 0; d < DIAS_ADELANTE; d++)
        {
            var fecha = hoy.AddDays(d);

            foreach (var c in canchas)
            {
                if (!diasGenerados.Add((c.Id, fecha))) continue;

                for (var hora = HORA_APERTURA; hora < HORA_CIERRE; hora++)
                {
                    // No generar horas que ya pasaron hoy
                    if (d == 0 && hora <= ahora.Hour) continue;

                    var precio = hora >= 18 ? c.PrecioHora * 1.3m : c.PrecioHora;

                    // Ocupación simulada pero estable
                    var semilla = c.Id * 7 + fecha.DayNumber * 3 + hora;
                    var estado = semilla % 5 == 0 ? "Reservado"
                               : (c.Id == 3 && hora == HORA_APERTURA && fecha.DayOfWeek == DayOfWeek.Monday) ? "Mantenimiento"
                               : "Disponible";

                    horarios.Add(new Horario
                    {
                        Id = siguienteHorarioId++,
                        CanchaId = c.Id,
                        CanchaNombre = c.Nombre,
                        TipoCancha = c.Tipo,
                        Fecha = fecha,
                        HoraInicio = $"{hora:00}:00",
                        HoraFin = $"{hora + 1:00}:00",
                        DuracionMinutos = 60,
                        Precio = Math.Round(precio, 2),
                        Estado = estado
                    });
                }
            }
        }
    }
}

// ============================================================
// ENDPOINTS
// ============================================================

app.MapGet("/", () => "API Golazo Arena funcionando");

// ---- Empresa ----
app.MapGet("/api/empresa", () => empresa);

// ---- Canchas ----
// Filtros: ?tipo=Fútbol 5&techada=true&q=norte
app.MapGet("/api/canchas", (string? tipo, bool? techada, string? q) =>
{
    var r = canchas.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(tipo))
        r = r.Where(c => c.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase));
    if (techada.HasValue)
        r = r.Where(c => c.Techada == techada.Value);
    if (!string.IsNullOrWhiteSpace(q))
        r = r.Where(c => c.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase)
                      || c.Descripcion.Contains(q, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(r.ToList());
});

app.MapGet("/api/canchas/{id:int}", (int id) =>
{
    var cancha = canchas.FirstOrDefault(c => c.Id == id);
    return cancha is null
        ? Results.NotFound(new { message = "Cancha no encontrada." })
        : Results.Ok(cancha);
});

// ---- Horarios / disponibilidad ----
// Filtros: ?fecha=2026-10-02&canchaId=1&tipo=Fútbol 5&estado=Disponible
//          &disponible=true&precioMax=80&horaDesde=18:00
app.MapGet("/api/horarios", (string? fecha, int? canchaId, string? tipo, string? estado,
                             bool? disponible, decimal? precioMax, string? horaDesde) =>
{
    AsegurarHorarios();

    DateOnly? fechaFiltro = null;
    if (!string.IsNullOrWhiteSpace(fecha))
    {
        if (!DateOnly.TryParse(fecha, out var f))
            return Results.BadRequest(new { message = "Fecha inválida. Usa el formato yyyy-MM-dd." });
        fechaFiltro = f;
    }

    List<Horario> copia;
    lock (candado) { copia = horarios.ToList(); }

    var r = copia.AsEnumerable();

    if (fechaFiltro.HasValue) r = r.Where(h => h.Fecha == fechaFiltro.Value);
    if (canchaId.HasValue)    r = r.Where(h => h.CanchaId == canchaId.Value);
    if (!string.IsNullOrWhiteSpace(tipo))
        r = r.Where(h => h.TipoCancha.Equals(tipo, StringComparison.OrdinalIgnoreCase));
    if (!string.IsNullOrWhiteSpace(estado))
        r = r.Where(h => h.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase));
    if (disponible.HasValue)  r = r.Where(h => h.Disponible == disponible.Value);
    if (precioMax.HasValue)   r = r.Where(h => h.Precio <= precioMax.Value);
    if (!string.IsNullOrWhiteSpace(horaDesde))
        r = r.Where(h => string.CompareOrdinal(h.HoraInicio, horaDesde) >= 0);

    return Results.Ok(r.OrderBy(h => h.Fecha).ThenBy(h => h.HoraInicio).ThenBy(h => h.CanchaId).ToList());
});

app.MapGet("/api/horarios/{id:int}", (int id) =>
{
    AsegurarHorarios();

    Horario? h;
    lock (candado) { h = horarios.FirstOrDefault(x => x.Id == id); }

    return h is null
        ? Results.NotFound(new { message = "Horario no encontrado." })
        : Results.Ok(h);
});

// ---- Servicios ----
// Filtros: ?categoria=Comodidad&q=vestuario
app.MapGet("/api/servicios", (string? categoria, string? q) =>
{
    var r = servicios.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(categoria))
        r = r.Where(s => s.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));
    if (!string.IsNullOrWhiteSpace(q))
        r = r.Where(s => s.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase)
                      || s.Descripcion.Contains(q, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(r.ToList());
});

app.MapGet("/api/servicios/{id:int}", (int id) =>
{
    var s = servicios.FirstOrDefault(x => x.Id == id);
    return s is null
        ? Results.NotFound(new { message = "Servicio no encontrado." })
        : Results.Ok(s);
});

// ---- Promociones ----
app.MapGet("/api/promociones", () => promociones);

// ---- Reservas ----
app.MapGet("/api/reservas", () =>
{
    lock (candado) { return Results.Ok(reservas.OrderByDescending(r => r.Id).ToList()); }
});

app.MapPost("/api/reservas", (ReservaRequest datos) =>
{
    if (string.IsNullOrWhiteSpace(datos.Cliente) || string.IsNullOrWhiteSpace(datos.Telefono))
        return Results.BadRequest(new { message = "Nombre y teléfono son obligatorios." });

    AsegurarHorarios();

    lock (candado)
    {
        var h = horarios.FirstOrDefault(x => x.Id == datos.HorarioId);
        if (h is null)
            return Results.NotFound(new { message = "Horario no encontrado." });
        if (!h.Disponible)
            return Results.Conflict(new { message = $"El horario ya no está disponible (estado: {h.Estado})." });

        var extras = servicios.Where(s => datos.ServiciosIds != null && datos.ServiciosIds.Contains(s.Id)).ToList();
        var total = h.Precio + extras.Sum(s => s.Precio);

        h.Estado = "Reservado";

        var reserva = new Reserva(
            siguienteReservaId++,
            $"GA-{DateTime.UtcNow:yyMMdd}-{siguienteReservaId - 1:000}",
            h.Id, h.CanchaNombre, h.Fecha, h.HoraInicio, h.HoraFin,
            datos.Cliente.Trim(), datos.Telefono.Trim(), datos.Email,
            extras.Select(s => s.Nombre).ToList(),
            total, "Confirmada");

        reservas.Add(reserva);
        return Results.Created($"/api/reservas/{reserva.Id}", reserva);
    }
});

app.MapDelete("/api/reservas/{id:int}", (int id) =>
{
    lock (candado)
    {
        var r = reservas.FirstOrDefault(x => x.Id == id);
        if (r is null)
            return Results.NotFound(new { message = "Reserva no encontrada." });

        var h = horarios.FirstOrDefault(x => x.Id == r.HorarioId);
        if (h is not null) h.Estado = "Disponible";

        reservas.Remove(r);
        return Results.NoContent();
    }
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");

// ============================================================
// MODELOS
// ============================================================

record Empresa(
    string Nombre,
    string Logo,
    string Eslogan,
    string Descripcion,
    string Direccion,
    string Telefono,
    string Email,
    string HorarioAtencion
);

record Cancha(
    int Id,
    string Nombre,
    string Tipo,          // Fútbol 5, Fútbol 7, Fútbol 11
    string Superficie,
    int Capacidad,        // jugadores
    decimal PrecioHora,
    bool Iluminacion,
    bool Techada,
    string Imagen,
    string Descripcion
);

record Servicio(
    int Id,
    string Nombre,
    string Categoria,
    string Descripcion,
    decimal Precio,       // 0 = incluido / gratuito
    string Icono,
    string Imagen
);

record Promocion(
    int Id,
    string Titulo,
    string Descripcion,
    int DescuentoPorcentaje,
    string Codigo,
    string Condicion,
    string VigenteHasta,
    string Imagen
);

class Horario
{
    public int Id { get; set; }
    public int CanchaId { get; set; }
    public string CanchaNombre { get; set; } = "";
    public string TipoCancha { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public string HoraInicio { get; set; } = "";
    public string HoraFin { get; set; } = "";
    public int DuracionMinutos { get; set; }
    public decimal Precio { get; set; }
    public string Estado { get; set; } = "Disponible";   // Disponible | Reservado | Mantenimiento
    public bool Disponible => Estado == "Disponible";
}

record ReservaRequest(
    int HorarioId,
    string Cliente,
    string Telefono,
    string? Email,
    List<int>? ServiciosIds
);

record Reserva(
    int Id,
    string Codigo,
    int HorarioId,
    string Cancha,
    DateOnly Fecha,
    string HoraInicio,
    string HoraFin,
    string Cliente,
    string Telefono,
    string? Email,
    List<string> Servicios,
    decimal Total,
    string Estado
);
