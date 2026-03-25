using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreadLibrary.Core.SoftBodySim
{
    public class SoftbodySystem : ModSystem
    {
        public static List<SoftbodyInstance> Instances = new();

        public override void PostUpdateEverything()
        {
            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                SoftbodyInstance s = Instances[i];

                if (s.DriverMode == SoftbodyInstance.TransformDriverMode.EntityCenter &&
                    (s.DriverEntity == null || !s.DriverEntity.active))
                {
                    Instances.RemoveAt(i);
                    continue;
                }

                s.UpdateDriverOnly();
                s.Sim.BeginStep();
            }

            int solverIterations = 1;
            for (int i = 0; i < Instances.Count; i++)
                solverIterations = Math.Max(solverIterations, Math.Max(1, Instances[i].Sim.Mat.Iterations));

            for (int iter = 0; iter < solverIterations; iter++)
            {
                for (int i = 0; i < Instances.Count; i++)
                {
                    SoftbodyInstance s = Instances[i];
                    if (iter < Math.Max(1, s.Sim.Mat.Iterations))
                        s.Sim.SolveInternalConstraints();
                }

                for (int i = 0; i < Instances.Count; i++)
                {
                    SoftbodyInstance s = Instances[i];
                    if (s.Collision.CollideWithTiles && iter < Math.Max(1, s.Sim.Mat.Iterations))
                        s.Sim.SolveTileContacts();
                }

                SolveInterSoftbodyCollisions();
                SolveEntityCollisions(3);
            }

            for (int i = 0; i < Instances.Count; i++)
            {
                Instances[i].Sim.EndStep();
                Instances[i].RefreshCenter();
            }
        }

        private static void SolveInterSoftbodyCollisions()
        {
            if (Instances.Count <= 1)
                return;

            for (int i = 0; i < Instances.Count; i++)
            {
                for (int j = i + 1; j < Instances.Count; j++)
                {
                    SoftbodyInstance a = Instances[i];
                    SoftbodyInstance b = Instances[j];

                    if (!ShouldSoftbodiesCollide(a, b))
                        continue;

                    if (!BoundsOverlap(a, b))
                        continue;

                    SolvePairSoftbodyCollisions(a, b);
                }
            }
        }
        private static bool ShouldSoftbodiesCollide(SoftbodyInstance a, SoftbodyInstance b)
        {
            if (!a.Collision.CollideWithSoftbodies || !b.Collision.CollideWithSoftbodies)
                return false;

            bool aHitsB = (a.Collision.CollisionMask & b.Collision.CollisionLayer) != 0;
            bool bHitsA = (b.Collision.CollisionMask & a.Collision.CollisionLayer) != 0;

            return aHitsB && bHitsA;
        }

        private static void SolvePairSoftbodyCollisions(SoftbodyInstance a, SoftbodyInstance b)
        {
            if (a.BoundaryNodes.Count >= 2 && b.BoundaryNodes.Count >= 2)
            {
                ProjectBoundaryNodesAgainstBody(a, b);
                ProjectBoundaryNodesAgainstBody(b, a);
            }

            SolvePairNodeCollisions(a, b);
        }

        private static void ProjectBoundaryNodesAgainstBody(SoftbodyInstance mover, SoftbodyInstance obstacle)
        {
            if (mover.BoundaryNodes.Count == 0 || obstacle.BoundaryNodes.Count < 2)
                return;

            for (int i = 0; i < mover.BoundaryNodes.Count; i++)
            {
                int moverNodeIndex = mover.BoundaryNodes[i];
                ref SoftbodySim.Node moverNode = ref mover.Sim.GetNodeRef(moverNodeIndex);

                if (moverNode.InvMass <= 0f)
                    continue;

                float bestPenetration = 0f;
                Vector2 bestNormal = Vector2.Zero;
                int bestA = -1;
                int bestB = -1;
                float bestT = 0f;

                for (int j = 0; j < obstacle.BoundaryNodes.Count; j++)
                {
                    int segAIndex = obstacle.BoundaryNodes[j];
                    int segBIndex = obstacle.BoundaryNodes[(j + 1) % obstacle.BoundaryNodes.Count];

                    SoftbodySim.Node segA = obstacle.Sim.Nodes[segAIndex];
                    SoftbodySim.Node segB = obstacle.Sim.Nodes[segBIndex];

                    Vector2 nearest = ClosestPointOnSegment(moverNode.Pos, segA.Pos, segB.Pos, out float t);
                    Vector2 delta = moverNode.Pos - nearest;

                    float distSq = delta.LengthSquared();
                    float segmentRadius = (segA.Radius + segB.Radius) * 0.5f;
                    float minDist = moverNode.Radius + segmentRadius;

                    if (distSq >= minDist * minDist)
                        continue;

                    float dist = distSq > 1e-8f ? MathF.Sqrt(distSq) : 0f;
                    Vector2 normal;

                    if (dist > 1e-6f)
                        normal = delta / dist;
                    else
                    {
                        Vector2 edge = segB.Pos - segA.Pos;
                        normal = edge.LengthSquared() > 1e-8f
                            ? Vector2.Normalize(new Vector2(-edge.Y, edge.X))
                            : -Vector2.UnitY;
                    }

                    float penetration = minDist - dist;
                    if (penetration > bestPenetration)
                    {
                        bestPenetration = penetration;
                        bestNormal = normal;
                        bestA = segAIndex;
                        bestB = segBIndex;
                        bestT = t;
                    }
                }

                if (bestA == -1 || bestPenetration <= 0f)
                    continue;

                ref SoftbodySim.Node node = ref mover.Sim.GetNodeRef(moverNodeIndex);
                ref SoftbodySim.Node segNodeA = ref obstacle.Sim.GetNodeRef(bestA);
                ref SoftbodySim.Node segNodeB = ref obstacle.Sim.GetNodeRef(bestB);

                float wNode = node.InvMass;
                float wA = segNodeA.InvMass * (1f - bestT);
                float wB = segNodeB.InvMass * bestT;
                float wSum = wNode + wA + wB;

                if (wSum <= 0f)
                    continue;

                Vector2 correction = bestNormal * bestPenetration;

                if (wNode > 0f)
                    node.Pos += correction * (wNode / wSum);

                if (wA > 0f)
                    segNodeA.Pos -= correction * (wA / wSum);

                if (wB > 0f)
                    segNodeB.Pos -= correction * (wB / wSum);

                ApplyBoundaryContactVelocityResponse(ref node, bestNormal);
                ApplyBoundaryContactVelocityResponse(ref segNodeA, -bestNormal);
                ApplyBoundaryContactVelocityResponse(ref segNodeB, -bestNormal);
            }
        }

        private static void ApplyBoundaryContactVelocityResponse(ref SoftbodySim.Node node, Vector2 normal)
        {
            Vector2 vel = node.Pos - node.PrevPos;
            float vn = Vector2.Dot(vel, normal);

            if (vn < 0f)
            {
                vel -= normal * vn;
                node.PrevPos = node.Pos - vel;
            }
        }

        private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b, out float t)
        {
            Vector2 ab = b - a;
            float abLenSq = ab.LengthSquared();

            if (abLenSq <= 1e-8f)
            {
                t = 0f;
                return a;
            }

            t = Vector2.Dot(point - a, ab) / abLenSq;
            t = MathHelper.Clamp(t, 0f, 1f);
            return a + ab * t;
        } 
        private static void SolvePairNodeCollisions(SoftbodyInstance a, SoftbodyInstance b)
        {
            var nodesA = a.Sim.Nodes;
            var nodesB = b.Sim.Nodes;

            for (int i = 0; i < nodesA.Count; i++)
            {
                ref var na = ref a.Sim.GetNodeRef(i);

                for (int j = 0; j < nodesB.Count; j++)
                {
                    ref var nb = ref b.Sim.GetNodeRef(j);

                    Vector2 delta = nb.Pos - na.Pos;
                    float minDist = na.Radius + nb.Radius;
                    float distSq = delta.LengthSquared();

                    if (distSq >= minDist * minDist)
                        continue;

                    float dist = distSq > 1e-8f ? System.MathF.Sqrt(distSq) : 0f;

                    Vector2 normal;
                    if (dist > 1e-6f)
                        normal = delta / dist;
                    else
                        normal = Vector2.UnitY;

                    float penetration = minDist - dist;
                    if (penetration <= 0f)
                        continue;

                    float wA = na.InvMass;
                    float wB = nb.InvMass;
                    float wSum = wA + wB;
                    if (wSum <= 0f)
                        continue;

                    Vector2 correction = normal * penetration;

                    if (wA > 0f)
                        na.Pos -= correction * (wA / wSum);

                    if (wB > 0f)
                        nb.Pos += correction * (wB / wSum);

                    // Optional small velocity damping along contact normal.
                    ApplyContactVelocityResponse(ref na, ref nb, normal);
                }
            }
        }

        private static void ApplyContactVelocityResponse(ref SoftbodySim.Node a, ref SoftbodySim.Node b, Vector2 normal)
        {
            Vector2 velA = a.Pos - a.PrevPos;
            Vector2 velB = b.Pos - b.PrevPos;

            float relN = Vector2.Dot(velB - velA, normal);
            if (relN >= 0f)
                return;

            float wA = a.InvMass;
            float wB = b.InvMass;
            float wSum = wA + wB;
            if (wSum <= 0f)
                return;

            float impulse = relN * 0.5f;
            Vector2 impulseVec = normal * impulse;

            if (wA > 0f)
                a.PrevPos -= impulseVec * (wA / wSum);

            if (wB > 0f)
                b.PrevPos += impulseVec * (wB / wSum);
        }

        private static bool BoundsOverlap(SoftbodyInstance a, SoftbodyInstance b)
        {
            Rectangle ra = a.Sim.GetWorldAabb();
            Rectangle rb = b.Sim.GetWorldAabb();
            return ra.Intersects(rb);
        }



        private static void SolveEntityCollisions(int iterations)
        {
            if (Instances.Count == 0)
                return;

            iterations = System.Math.Max(1, iterations);

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int i = 0; i < Instances.Count; i++)
                {
                    SoftbodyInstance s = Instances[i];
                    Rectangle bodyBounds = s.Sim.GetWorldAabb();

                    if (s.Collision.CollideWithPlayers)
                    {
                        for (int p = 0; p < Main.maxPlayers; p++)
                        {
                            Player player = Main.player[p];
                            if (!ShouldCollideWithPlayer(s, player))
                                continue;

                            Rectangle hitbox = GetEntityCollisionRect(player, s.Collision.EntityPadding);
                            if (!bodyBounds.Intersects(hitbox))
                                continue;

                            ProjectSoftbodyAgainstEntity(s, player, s.Collision.PlayerPushFactor);
                        }
                    }

                    if (s.Collision.CollideWithNPCs)
                    {
                        for (int n = 0; n < Main.maxNPCs; n++)
                        {
                            NPC npc = Main.npc[n];
                            if (!ShouldCollideWithNPC(s, npc))
                                continue;

                            Rectangle hitbox = GetEntityCollisionRect(npc, s.Collision.EntityPadding);
                            if (!bodyBounds.Intersects(hitbox))
                                continue;

                            ProjectSoftbodyAgainstEntity(s, npc, s.Collision.NPCPushFactor);
                        }
                    }
                }
            }
        }

        private static bool ShouldCollideWithPlayer(SoftbodyInstance s, Player player)
        {
            if (player == null || !player.active || player.dead || player.ghost)
                return false;

            if (s.Collision.IgnoreDriverEntity && ReferenceEquals(player, s.DriverEntity))
                return false;

            return player.width > 0 && player.height > 0;
        }

        private static bool ShouldCollideWithNPC(SoftbodyInstance s, NPC npc)
        {
            if (npc == null || !npc.active || npc.life <= 0)
                return false;

            if (s.Collision.IgnoreDriverEntity && ReferenceEquals(npc, s.DriverEntity))
                return false;

            return npc.width > 0 && npc.height > 0;
        }

        private static Rectangle GetEntityCollisionRect(Entity entity, int padding)
        {
            Rectangle rect = entity.Hitbox;
            if (padding != 0)
                rect.Inflate(padding, padding);
            return rect;
        }

        private static void ProjectSoftbodyAgainstEntity(SoftbodyInstance s, Entity entity, float pushFactor)
        {
            float rawEntityShare = MathHelper.Clamp(pushFactor, 0f, 1f);
            bool canMoveEntity = CanMoveEntity(entity);

            for (int i = 0; i < s.Sim.Nodes.Count; i++)
            {
                ref SoftbodySim.Node node = ref s.Sim.GetNodeRef(i);
                if (node.InvMass <= 0f)
                    continue;

                Rectangle rect = GetEntityCollisionRect(entity, s.Collision.EntityPadding);

                if (!CircleIntersectsAabb(node.Pos, node.Radius, rect))
                    continue;

                if (!TryGetCircleAabbContact(node.Pos, node.Radius, rect, out Vector2 normal, out float penetration))
                    continue;

                if (penetration <= 0f)
                    continue;

                float entityShare = canMoveEntity ? rawEntityShare : 0f;
                float nodeShare = 1f - entityShare;

                Vector2 correction = normal * penetration;

                if (nodeShare > 0f)
                    node.Pos += correction * nodeShare;

                if (entityShare > 0f)
                    MoveEntity(entity, -correction * entityShare);

                ApplyNodeEntityVelocityResponse(ref node, normal, s.Collision.EntityFriction, s.Collision.EntityBounce);

                if (entityShare > 0f)
                    PushEntityVelocity(entity, -normal, penetration * 0.15f * entityShare);
            }
        }

        private static void ApplyNodeEntityVelocityResponse(ref SoftbodySim.Node node, Vector2 normal, float friction, float bounce)
        {
            Vector2 vel = node.Pos - node.PrevPos;
            float vn = Vector2.Dot(vel, normal);

            Vector2 normalVel = vn * normal;
            Vector2 tangentVel = vel - normalVel;

            friction = MathHelper.Clamp(friction, 0f, 1f);
            bounce = MathHelper.Clamp(bounce, 0f, 1f);

            if (vn < 0f)
                vel = tangentVel * (1f - friction) - normalVel * bounce;
            else
                vel = tangentVel * (1f - friction) + normalVel;

            node.PrevPos = node.Pos - vel;
        }

        private static bool CanMoveEntity(Entity entity)
        {
            if (entity is Player player)
                return Main.netMode == Terraria.ID.NetmodeID.SinglePlayer || player.whoAmI == Main.myPlayer;

            if (entity is NPC)
                return Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient;

            return false;
        }

        private static void MoveEntity(Entity entity, Vector2 delta)
        {
            if (delta == Vector2.Zero)
                return;

            entity.position += delta;

            if (entity is NPC npc)
                npc.netUpdate = true;
        }

        private static void PushEntityVelocity(Entity entity, Vector2 dir, float amount)
        {
            if (amount <= 0f || dir == Vector2.Zero)
                return;

            Vector2 push = Vector2.Normalize(dir) * amount;
            entity.velocity += push;

            if (entity is NPC npc)
                npc.netUpdate = true;
        }

        private static bool TryGetCircleAabbContact(Vector2 center, float radius, Rectangle rect, out Vector2 normal, out float penetration)
        {
            Vector2 nearest = new(
                MathHelper.Clamp(center.X, rect.Left, rect.Right),
                MathHelper.Clamp(center.Y, rect.Top, rect.Bottom)
            );

            Vector2 delta = center - nearest;
            float distSq = delta.LengthSquared();

            if (distSq > 1e-8f)
            {
                float dist = System.MathF.Sqrt(distSq);
                if (dist >= radius)
                {
                    normal = Vector2.Zero;
                    penetration = 0f;
                    return false;
                }

                normal = delta / dist;
                penetration = radius - dist;
                return true;
            }

            float left = center.X - rect.Left;
            float right = rect.Right - center.X;
            float top = center.Y - rect.Top;
            float bottom = rect.Bottom - center.Y;

            float min = left;
            normal = -Vector2.UnitX;

            if (right < min)
            {
                min = right;
                normal = Vector2.UnitX;
            }

            if (top < min)
            {
                min = top;
                normal = -Vector2.UnitY;
            }

            if (bottom < min)
            {
                min = bottom;
                normal = Vector2.UnitY;
            }

            penetration = radius + min;
            return true;
        }

        private static bool CircleIntersectsAabb(Vector2 center, float radius, Rectangle rect)
        {
            float x = MathHelper.Clamp(center.X, rect.Left, rect.Right);
            float y = MathHelper.Clamp(center.Y, rect.Top, rect.Bottom);
            float dx = center.X - x;
            float dy = center.Y - y;
            return dx * dx + dy * dy <= radius * radius;
        }   



        public override void PostDrawTiles()
        {
            foreach (var s in Instances)
                s.Draw();
        }
    }
}
