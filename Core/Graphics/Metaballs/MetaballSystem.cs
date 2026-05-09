using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace BreadLibrary.Core.Graphics.Metaballs
{
    public sealed class MetaballSystem : ModSystem
    {
        public const int AbsoluteMaxShaderInstances = 64;

        private static readonly Dictionary<string, List<MetaballGroup>> Groups = new();

        public static bool AnythingToDraw
        {
            get
            {
                foreach (List<MetaballGroup> groups in Groups.Values)
                {
                    for (int i = 0; i < groups.Count; i++)
                    {
                        if (groups[i].HasActiveInstances)
                            return true;
                    }
                }

                return false;
            }
        }
        public override void Load()
        {
            Groups.Clear();
        }

        public override void Unload()
        {
            Groups.Clear();
            MetaballRegistry.Clear();
        }

        public override void PostUpdateEverything()
        {
            foreach (MetaballGroup group in Groups.Values.SelectMany(groups => groups))
                group.Update();

            RemoveEmptyTrailingGroups();
        }

        public static void Create<T>(
            Vector2 center,
            float radius,
            float strength,
            float opacity,
            int timeLeft,
            Vector2 velocity,
            MetaballInstanceInitializer initializer = null)
            where T : Metaball
        {
            T metaball = ModContent.GetInstance<T>();

            if (metaball is null)
                return;

            MetaballGroup group = GetWritableGroup(metaball);
            group.Add(center, radius, strength, opacity, timeLeft, velocity, initializer);
        }

        private static MetaballGroup GetWritableGroup(Metaball metaball)
        {
            if (!Groups.TryGetValue(metaball.Key, out List<MetaballGroup> groups))
            {
                groups = new List<MetaballGroup>();
                Groups[metaball.Key] = groups;
            }

            int maxInstancesPerGroup = GetMaxInstancesPerGroup(metaball);

            for (int i = 0; i < groups.Count; i++)
            {
                MetaballGroup group = groups[i];

                if (group.Instances.Count < maxInstancesPerGroup)
                    return group;
            }

            MetaballGroup newGroup = new(metaball);
            groups.Add(newGroup);
            return newGroup;
        }

        private static int GetMaxInstancesPerGroup(Metaball metaball)
        {
            return (int)MathHelper.Clamp(
                metaball.MaxShaderInstances,
                1,
                AbsoluteMaxShaderInstances
            );
        }

        internal static IEnumerable<MetaballGroup> GetGroups(MetaballLayer layer)
        {
            foreach (MetaballGroup group in Groups.Values.SelectMany(groups => groups))
            {
                if (group.Type.Layer == layer && group.HasActiveInstances)
                    yield return group;
            }
        }

        internal static IEnumerable<IGrouping<Metaball, MetaballGroup>> GetGroupSets(MetaballLayer layer)
        {
            return Groups.Values
                .SelectMany(groups => groups)
                .Where(group => group.Type.Layer == layer && group.HasActiveInstances)
                .GroupBy(group => group.Type);
        }

        private static void RemoveEmptyTrailingGroups()
        {
            foreach (List<MetaballGroup> groups in Groups.Values)
            {
                for (int i = groups.Count - 1; i >= 1; i--)
                {
                    if (groups[i].HasActiveInstances)
                        continue;

                    groups.RemoveAt(i);
                }
            }
        }

        internal static void ClearAllGroups()
        {
            foreach (MetaballGroup group in Groups.Values.SelectMany(groups => groups))
                group.Clear();
        }
    }
}