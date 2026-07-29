using System.Collections.Generic;
using System.IO;
using Terraria.ModLoader.IO;

namespace BreadLibrary.Core.MultiSegment;

public interface IMultiSegmentNPC
{
    /// <summary>
    ///     For the love of all networking DO NOT modify the amount of items in this list after
    ///     SetDefaults, prepare all hitboxes on npc innitialization, use
    ///     <see cref="ExtraNPCSegment.Active" /> to enable and disable hitboxes as needed.
    /// </summary>
    /// <returns></returns>
    public ref List<ExtraNPCSegment> ExtraHitBoxes();

    public void OnHitBoxCollide(int WhoAmI, Projectile origin) { }

    public void UpdateSegments()
    {
        foreach (var hitbox in ExtraHitBoxes())
        {
            if (hitbox.ImmuneTime > 0)
            {
                hitbox.ImmuneTime--;
            }
        }
    }

    public void NetSendExtraHitboxes(BinaryWriter writer)
    {
        foreach (var hitbox in ExtraHitBoxes())
        {
            writer.Write(hitbox.ItemCollide);
            writer.Write(hitbox.ProjectileCollide);
            writer.Write(hitbox.ImmuneTime);
            writer.Write(hitbox.Active);
        }
    }

    public void NetReceiveExtraHitboxes(BinaryReader reader)
    {
        foreach (var hitbox in ExtraHitBoxes())
        {
            hitbox.ItemCollide = reader.ReadBoolean();
            hitbox.ProjectileCollide = reader.ReadBoolean();
            hitbox.ImmuneTime = reader.ReadInt32();
            hitbox.Active = reader.ReadBoolean();
        }
    }
}


public class ExtraNPCSegment
{
    public Rectangle Hitbox;

    /// <summary>
    /// if true, this hitbox can be hit separately from the npc's iframes
    /// </summary>
    public bool UniqueIframes;
    /// <summary>
    /// Self explanatory.a
    /// </summary>
    public bool DealsDamage;

    public int BaseImmunity;

    public bool ItemCollide = true;

    public bool ProjectileCollide = true;

    public int ImmuneTime;

    /// <summary>
    /// whether this segment should take damage or not
    /// </summary>
    public bool Active;

    public ExtraNPCSegment(Rectangle hitbox, bool dealsDamage = true, bool itemCollide = true, bool projectileCollide = true, bool uniqueIframes = false, int Baseimmunity = 60)
    {
        Hitbox = hitbox;
        UniqueIframes = uniqueIframes;
        DealsDamage = dealsDamage;
        ItemCollide = itemCollide;
        ProjectileCollide = projectileCollide;
        BaseImmunity = Baseimmunity;
        Active = true;
    }
}