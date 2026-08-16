using System;

/// <summary>
/// 소모성 아이템 1개의 정의. GameData.Consumables 배열에 담긴다.
/// </summary>
[Serializable]
public class ConsumableData
{
    public int id;
    public string itemName;

    public float healHpPercent;     // 0.2f = 최대 HP 20% 회복
    public float healMpPercent;     // 0.2f = 최대 MP 20% 회복

    public bool hasEffect;
    public EffectType effectType = EffectType.None;
    public float effectValue;
    public int effectTurns;         // StatusEffect.UntilBattleEnd 면 전투 종료까지

    public int maxStack;            // 최대 누적 개수
    public int price;

    public string description;
}

/// <summary>
/// 배낭 칸 하나. 아이템 id 와 개수를 들고 있다.
/// </summary>
[Serializable]
public class InventorySlot
{
    public int itemId = -1;
    public int count;

    public bool IsEmpty
    {
        get { return itemId < 0 || count <= 0; }
    }

    public void Clear()
    {
        itemId = -1;
        count = 0;
    }
}
