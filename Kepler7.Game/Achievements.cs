using Kepler7.Core;
using static Kepler7.Game.Art;
namespace Kepler7.Game;
public sealed partial class GameForm
{
    private Pantalla achievementsReturn;
    private int operationIndex;
    private void OpenAchievements() { achievementsReturn = screen; operationIndex = Math.Max(0, progress.Operaciones.Count - 1); Change(Pantalla.Logros); }

    private void DrawAchievements(Graphics g)
    {
        Header(g, "Bitácora de la estación", "Tus hazañas dejan huella");

        Button(g, "← Volver", 40, 125, 170, 40,
            () => Change(achievementsReturn));

        var logros = Logros.Obtener(progress);

        TextAt(g,
            logros.Count(l => l.Obtenida) + " / " + logros.Length +
            " insignias disponibles · 12 patrones en la misión completa",
            240, 137, 16, Gold, true);

        // Insignias en dos columnas
        for (int i = 0; i < logros.Length; i++)
        {
            var l = logros[i];

            float x = 40 + i % 2 * 570;
            float y = 180 + i / 2 * 88;

            Box(g, x, y, 550, 80, PanelColor, 12);

            Paw(g, x + 38, y + 32, .45f,
                l.Obtenida ? Gold : Muted);

            TextAt(g, l.Nombre, x + 75, y + 8,
                18, l.Obtenida ? White : Muted, true);

            TextAt(g,
                l.Patron + " · " +
                (l.Obtenida ? "CONSEGUIDA" : "POR DESCUBRIR"),
                x + 75, y + 33, 12,
                l.Obtenida ? Mint : Gold, true);

            Wrap(g, l.Objetivo, x + 75, y + 52,
                455, 25, 12, White);
        }

        // Últimas operaciones debajo de las insignias
        float operacionesY = 180 +
            ((logros.Length + 1) / 2) * 88 + 15;

        Box(g, 40, operacionesY, 1120, 100, PanelColor, 12);

        TextAt(g, "ÚLTIMAS OPERACIONES",
            60, operacionesY + 12, 13, Mint, true);

        if (progress.Operaciones.Count > 0)
        {
            operationIndex = Math.Clamp(
                operationIndex, 0, progress.Operaciones.Count - 1);

            Button(g, "‹", 60, operacionesY + 45, 42, 35,
                () => operationIndex--,
                false, operationIndex > 0);

            TextAt(g,
                (operationIndex + 1) + " / " +
                progress.Operaciones.Count + " · " +
                ShortLabel(
                    progress.Operaciones[operationIndex].Titulo, 53),
                120, operacionesY + 54, 16, White);

            Button(g, "›", 808, operacionesY + 45, 42, 35,
                () => operationIndex++,
                false,
                operationIndex < progress.Operaciones.Count - 1);

            Button(g, "Ver resumen",
                879, operacionesY + 41, 256, 42,
                () => ShowSummary(
                    progress.Operaciones[operationIndex], false),
                true);
        }
        else
        {
            TextAt(g, "Tus próximos lotes aparecerán acá.",
                60, operacionesY + 55, 17, Muted);
        }
    }

    private void DrawRoomIndicators(Graphics g, bool lab)
    {
       
        var stats = GetFleetStats();
        Box(g, 68, 346, 260, 99, Navy, 9);
        TextAt(g, lab ? "PILOTOS DISPONIBLES" : "NAVES SIN PILOTO", 84, 358, 14, lab ? Mint : Gold, true);
        for (int i = 0; i < stats.Length; i++) TextAt(g, stats[i].Tipo + ": " + (lab ? stats[i].PilotosDisponibles : stats[i].NavesSinPiloto), 84, 384 + i * 19, 15, White);
    }
    private EstadoEspecialidad[]? fleetStats;
    private int statsPilots = -1, statsShips = -1, statsAssigned = -1;
    private EstadoEspecialidad[] GetFleetStats()
    {
        int assigned = progress.Flota.Count(n => n.Piloto != null);
        if (fleetStats == null || statsPilots != progress.Pilotos.Count || statsShips != progress.Flota.Count || statsAssigned != assigned)
        { fleetStats = EstadisticasFlota.Obtener(progress); statsPilots = progress.Pilotos.Count; statsShips = progress.Flota.Count; statsAssigned = assigned; }
        return fleetStats;
    }
    private List<string> FleetWaitingLines() => GetFleetStats().Select(s => s.Tipo + ": " + s.NavesSinPiloto + " naves esperan piloto · " + s.PilotosDisponibles + " pilotos libres").ToList();
}
