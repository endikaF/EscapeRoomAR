using System.Collections.Generic;

// Tipos de punto. Para añadir uno nuevo: añádelo aquí, dale categoría en GetCategory
// y asigna su prefab en el MonsterRegistry.
public enum MonsterType
{
    // Monstruos
    Zombie,
    Spider,
    Bat,
    Skeleton,
    // Decoración
    WebCorner,
    WebWall,
    WebCircle,
    // Audio (reservados)
    Audio_reloj,
    Audio_demo,
    // Payasos
    Payaso_secuencia,
    Payaso_perseguir,
    Payaso_emboscar
}

public enum PointCategory { Monster, Decoration, Audio }

public static class MonsterTypeExtensions
{
    private static readonly Dictionary<PointCategory, List<MonsterType>> byCategory = Build();

    private static Dictionary<PointCategory, List<MonsterType>> Build()
    {
        var d = new Dictionary<PointCategory, List<MonsterType>>();
        foreach (PointCategory c in System.Enum.GetValues(typeof(PointCategory)))
            d[c] = new List<MonsterType>();
        foreach (MonsterType t in System.Enum.GetValues(typeof(MonsterType)))
            d[t.GetCategory()].Add(t);
        return d;
    }

    public static PointCategory GetCategory(this MonsterType t)
    {
        switch (t)
        {
            case MonsterType.WebCorner:
            case MonsterType.WebWall:
            case MonsterType.WebCircle:
            case MonsterType.Payaso_secuencia:
                return PointCategory.Decoration;
            case MonsterType.Audio_reloj:
            case MonsterType.Audio_demo:
                return PointCategory.Audio;
            default:
                return PointCategory.Monster;
        }
    }

    // Tipos que se orientan pegados a la superficie (up = normal del plano)
    public static bool IsSurfaceAligned(this MonsterType t)
    {
        return t == MonsterType.Spider || t == MonsterType.WebCorner ||
               t == MonsterType.WebWall || t == MonsterType.WebCircle;
    }

    public static IReadOnlyList<MonsterType> TypesOf(PointCategory c) => byCategory[c];
}