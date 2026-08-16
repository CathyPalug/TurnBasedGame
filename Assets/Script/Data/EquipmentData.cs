using System;

/// <summary>
/// 장비 아이템 1개의 정의. GameData.Equipments 배열에 담긴다.
/// </summary>
[Serializable]
public class EquipmentData
{
    public int id;
    public string equipName;
    public EquipSlotType slot;

    public int atkBonus;
    public int defBonus;
    public int critBonus;       // %p

    public int price;           // 0 이면 상점 판매 없음
    public bool equippedAtStart;

    public string description;
}
