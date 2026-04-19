using UnityEngine;

// rarity tag for a loot pickup
// пока только для цвета, дальше можно к этому привязать эффекты
public enum LootRarity
{
    Common,
    Uncommon,
    Rare
}

// tiny script on the loot prefab — just colors the sprite by rarity
// (серенький — обычный, голубенький — нормас, золотой — вау ヽ(°〇°)ﾉ)
public class LootItem : MonoBehaviour
{
    public SpriteRenderer sprite; // drag SpriteRenderer of the loot prefab

    // colors per rarity, tweak in inspector if you want
    public Color commonColor = new Color(0.85f, 0.85f, 0.85f);
    public Color uncommonColor = new Color(0.40f, 0.70f, 1.00f);
    public Color rareColor = new Color(1.00f, 0.80f, 0.20f);

    public LootRarity rarity;

    public void SetRarity(LootRarity r)
    {
        rarity = r;
        if (sprite == null) return;

        if (r == LootRarity.Rare)
            sprite.color = rareColor;
        else if (r == LootRarity.Uncommon)
            sprite.color = uncommonColor;
        else
            sprite.color = commonColor;
    }
}
