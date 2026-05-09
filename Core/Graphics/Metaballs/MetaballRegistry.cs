using System.Collections.Generic;
using System.Linq;

namespace BreadLibrary.Core.Graphics.Metaballs
{
    public static class MetaballRegistry
    {
        private static readonly Dictionary<string, Metaball> MetaballsByKey = new();

        public static IReadOnlyCollection<Metaball> Metaballs => MetaballsByKey.Values;

        internal static void Register(Metaball metaball)
        {
            MetaballsByKey[metaball.Key] = metaball;
        }

        public static bool TryGet(string key, out Metaball metaball)
        {
            return MetaballsByKey.TryGetValue(key, out metaball);
        }

        public static T Get<T>() where T : Metaball
        {
            return MetaballsByKey.Values.OfType<T>().FirstOrDefault();
        }

        internal static void Clear()
        {
            MetaballsByKey.Clear();
        }
    }
}