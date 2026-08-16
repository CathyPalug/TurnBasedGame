using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 정보(레벨 / HP / MP / 경험치 / 골드)를 실시간으로 표시한다.
/// Canvas/Battle/Status 와 Canvas/MAP/Status 양쪽에 같은 컴포넌트를 붙여 쓴다.
/// </summary>
public class StatusUI : MonoBehaviour
{
    [Header("레벨")]
    public Text levelText;

    [Header("HP")]
    public Slider hpSlider;
    public Text hpText;

    [Header("MP")]
    public Slider mpSlider;
    public Text mpText;

    [Header("경험치")]
    public Slider expSlider;
    public Text expText;

    [Header("기타")]
    public Text goldText;
    public Text weaponText;
    public Text armorText;

    private Player Target
    {
        get { return GameManager.instance != null ? GameManager.instance.player : null; }
    }

    private void LateUpdate()
    {
        Player p = Target;
        if (p == null) return;

        if (levelText != null) levelText.text = p.level.ToString("00");

        SetBar(hpSlider, hpText, p.hp, p.maxHp);
        SetBar(mpSlider, mpText, p.mp, p.maxMp);

        if (p.IsMaxLevel)
        {
            if (expSlider != null) { expSlider.minValue = 0f; expSlider.maxValue = 1f; expSlider.value = 1f; }
            if (expText != null) expText.text = "MAX";
        }
        else
        {
            SetBar(expSlider, expText, p.exp, p.RequiredExp);
        }

        if (goldText != null) goldText.text = string.Format("{0} G", p.gold);

        if (weaponText != null)
        {
            EquipmentData w = p.GetEquipped(EquipSlotType.Weapon);
            weaponText.text = w != null ? w.equipName : "-";
        }

        if (armorText != null)
        {
            EquipmentData a = p.GetEquipped(EquipSlotType.Armor);
            armorText.text = a != null ? a.equipName : "-";
        }
    }

    private static void SetBar(Slider slider, Text text, int current, int max)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
        }

        if (text != null) text.text = string.Format("{0} / {1}", current, max);
    }
}
