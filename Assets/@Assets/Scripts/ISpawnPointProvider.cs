using System.Collections.Generic;

// Capa de "detección". Implementaciones: MockSpawnPointProvider (editor)
// y, más adelante, AnchorSpawnPointProvider (Quest 3 con anclas espaciales).
public interface ISpawnPointProvider
{
    IReadOnlyList<SpawnPointData> GetPoints();
}
